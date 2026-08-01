// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>Provides a resilient MQTT client with queued delivery and automatic reconnection.</summary>
/// <remarks>The ResilientMqttClient is designed to handle transient network failures and broker disconnects by
/// automatically reconnecting and resynchronizing subscriptions. It supports queuing of outgoing messages, configurable
/// overflow strategies, and event-driven notifications for connection and message processing states. This class is
/// intended for scenarios where robust, fault-tolerant MQTT client behavior is required, such as IoT device
/// communication or backend message processing. Thread safety is maintained for all public operations. Dispose the
/// client when it is no longer needed to release resources.</remarks>
internal sealed partial class ResilientMqttClient : Disposable, IResilientMqttClient
{
    /// <summary>Provides diagnostics for this client.</summary>
    private readonly MqttNetSourceLogger _logger;

    /// <summary>Raises message interception notifications.</summary>
    private readonly AsyncEvent<InterceptingPublishMessageEventArgs> _interceptingPublishMessageEvent =
        new();

    /// <summary>Raises processed-message notifications.</summary>
    private readonly AsyncEvent<ApplicationMessageProcessedEventArgs> _applicationMessageProcessedEvent =
        new();

    /// <summary>Raises skipped-message notifications.</summary>
    private readonly AsyncEvent<ApplicationMessageSkippedEventArgs> _applicationMessageSkippedEvent =
        new();

    /// <summary>Raises received-message notifications for awaited registrations.</summary>
    private readonly AsyncEvent<MqttApplicationMessageReceivedEventArgs> _applicationMessageReceivedEvent =
        new();

    /// <summary>Raises connected notifications for awaited registrations.</summary>
    private readonly AsyncEvent<MqttClientConnectedEventArgs> _connectedEvent = new();

    /// <summary>Raises failed-connection notifications.</summary>
    private readonly AsyncEvent<ConnectingFailedEventArgs> _connectingFailedEvent = new();

    /// <summary>Raises connection-state change notifications.</summary>
    private readonly AsyncEvent<EventArgs> _connectionStateChangedEvent = new();

    /// <summary>Raises disconnected notifications for awaited registrations.</summary>
    private readonly AsyncEvent<MqttClientDisconnectedEventArgs> _disconnectedEvent = new();

    /// <summary>Raises subscription synchronization failure notifications.</summary>
    private readonly AsyncEvent<ResilientProcessFailedEventArgs> _synchronizingSubscriptionsFailedEvent =
        new();

    /// <summary>Raises subscription change notifications.</summary>
    private readonly AsyncEvent<SubscriptionsChangedEventArgs> _subscriptionsChangedEvent = new();

    /// <summary>Stores application messages pending publication.</summary>
    private readonly BlockingQueue<ResilientMqttApplicationMessage> _messageQueue = new();

    /// <summary>Synchronizes access to the message queue.</summary>
    private readonly AsyncLock _messageQueueLock = new();

    /// <summary>Stores subscriptions that must be restored after reconnection.</summary>
    private readonly Dictionary<string, MqttTopicFilter> _reconnectSubscriptions = [];

    /// <summary>Stores pending topic subscriptions.</summary>
    private readonly Dictionary<string, MqttTopicFilter> _subscriptions = [];

    /// <summary>Signals pending subscription changes.</summary>
    private readonly SemaphoreSlim _subscriptionsQueuedSignal = new(0);

    /// <summary>Stores pending topic unsubscriptions.</summary>
    private readonly HashSet<string> _unsubscriptions = [];

    /// <summary>Controls the connection maintenance operation.</summary>
    private CancellationTokenSource? _connectionCancellationToken;

    /// <summary>Represents the connection maintenance operation.</summary>
    private Task? _maintainConnectionTask;

    /// <summary>Controls the background publishing operation.</summary>
    private CancellationTokenSource? _publishingCancellationToken;

    /// <summary>Initializes a new instance of the <see cref="ResilientMqttClient"/> class.</summary>
    /// <param name="mqttClient">The underlying MQTT client to be used for communication. Cannot be null.</param>
    /// <param name="logger">The logger instance used for diagnostic and operational logging. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if mqttClient or logger is null.</exception>
    public ResilientMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
    {
        InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(ResilientMqttClient));

        InternalClient.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
        InternalClient.ConnectedAsync += HandleConnectedAsync;
        InternalClient.DisconnectedAsync += HandleDisconnectedAsync;
    }

    /// <summary>Sends a ping request to the server to verify connectivity asynchronously.</summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the ping operation.</param>
    /// <returns>A task that represents the asynchronous ping operation.</returns>
    public Task PingAsync(CancellationToken cancellationToken = default) =>
        InternalClient.PingAsync(cancellationToken);

    /// <summary>Starts the resilient MQTT client.</summary>
    /// <param name="options">The configuration options used to initialize and manage the MQTT client connection. Cannot
    /// be null. The
    /// ClientOptions property of this parameter must also be set.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the ClientOptions property of <paramref name="options"/> is
    /// null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the client has already been started and is currently
    /// running.</exception>
    public async Task StartAsync(ResilientMqttClientOptions options)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(options);

        if (options.ClientOptions is null)
        {
            throw new ArgumentException("The client options are not set.", nameof(options));
        }

        if (!_maintainConnectionTask?.IsCompleted ?? false)
        {
            throw new InvalidOperationException("The managed client is already started.");
        }

        Options = options;

        if (options.Storage is not null)
        {
            _storageManager = new(options.Storage);
            var messages = await _storageManager.LoadQueuedMessagesAsync().ConfigureAwait(false);

            foreach (var message in messages)
            {
                _messageQueue.Enqueue(message);
            }
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _connectionCancellationToken = cancellationTokenSource;

        // The maintenance operation owns cancellation so it can complete deterministic cleanup even when shutdown
        // occurs before the scheduled work begins.
        _maintainConnectionTask = Task.Run(() => MaintainConnectionAsync(cancellationToken));
        _maintainConnectionTask.RunInBackground(_logger);

        _logger.Info("Started");
    }

    /// <summary>Stops the resilient MQTT client.</summary>
    /// <param name="cleanDisconnect">true to perform a clean disconnect and notify the server before disconnecting;
    /// otherwise, false to disconnect
    /// immediately without notification. The default is true.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(bool cleanDisconnect = true)
    {
        ThrowIfDisposed();

        _isCleanDisconnect = cleanDisconnect;

        StopPublishing();
        StopMaintainingConnection();

        _messageQueue.Clear();

        if (_maintainConnectionTask is null)
        {
            return;
        }

        await Task.WhenAny(_maintainConnectionTask);
        _maintainConnectionTask = null;
    }

    /// <summary>Asynchronously subscribes to the specified MQTT topic filters.</summary>
    /// <param name="topicFilters">A collection of <see cref="MqttTopicFilter"/> objects that specify the topics and
    /// associated options to
    /// subscribe to. Cannot be null. Each topic filter must specify a valid topic.</param>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    public Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(topicFilters);

        List<MqttTopicFilter> materializedTopicFilters = [.. topicFilters];
        foreach (var topicFilter in materializedTopicFilters)
        {
            MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter.Topic);
        }

        lock (_subscriptions)
        {
            foreach (var topicFilter in materializedTopicFilters)
            {
                _subscriptions[topicFilter.Topic] = topicFilter;
                _ = _unsubscriptions.Remove(topicFilter.Topic);
            }
        }

        _ = _subscriptionsQueuedSignal.Release();

        return CompletedTask.Instance;
    }

    /// <summary>Asynchronously unsubscribes from the specified topics.</summary>
    /// <param name="topics">A collection of topic names to unsubscribe from. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous unsubscribe operation.</returns>
    public Task UnsubscribeAsync(IEnumerable<string> topics)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(topics);

        lock (_subscriptions)
        {
            foreach (var topic in topics)
            {
                _ = _subscriptions.Remove(topic);
                _ = _unsubscriptions.Add(topic);
            }
        }

        _ = _subscriptionsQueuedSignal.Release();

        return CompletedTask.Instance;
    }

    /// <summary>Adds an awaited publish-message interception handler.</summary>
    /// <param name="handler">The handler to invoke before a queued message is published.</param>
    internal void AddInterceptPublishMessageHandler(
        Func<InterceptingPublishMessageEventArgs, Task> handler) =>
        _interceptingPublishMessageEvent.AddHandler(handler);

    /// <summary>Removes an awaited publish-message interception handler.</summary>
    /// <param name="handler">The handler to remove.</param>
    internal void RemoveInterceptPublishMessageHandler(
        Func<InterceptingPublishMessageEventArgs, Task> handler) =>
        _interceptingPublishMessageEvent.RemoveHandler(handler);

    /// <summary>Continuously maintains the client connection until cancellation is requested.</summary>
    /// <remarks>If the connection is disposed or a clean disconnect is requested, the method attempts to
    /// disconnect the client gracefully before completing. Any exceptions encountered during connection maintenance or
    /// disconnection are logged. This method is intended to be run in the background and should not be called directly
    /// by consumers.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to request termination of the connection
    /// maintenance loop.</param>
    /// <returns>A task that represents the asynchronous operation of maintaining the connection. The task completes
    /// when
    /// cancellation is requested or the connection is disposed.</returns>
    private async Task MaintainConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await TryMaintainConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error exception while maintaining connection.");
        }
        finally
        {
            if (!IsDisposed)
            {
                try
                {
                    if (_isCleanDisconnect)
                    {
                        using var disconnectTimeout = NewTimeoutToken(CancellationToken.None);
                        await InternalClient
                            .DisconnectAsync(new(), disconnectTimeout.Token)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning("Timeout while sending DISCONNECT packet.");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while disconnecting.");
                }

                _logger.Info("Stopped");
            }

            _reconnectSubscriptions.Clear();

            lock (_subscriptions)
            {
                _subscriptions.Clear();
                _unsubscriptions.Clear();
            }
        }
    }

    /// <summary>Publishes queued messages while connected.</summary>
    /// <remarks>This method continuously attempts to publish messages from the internal queue as long as the
    /// client remains connected and cancellation is not requested. If cancellation is requested or the client
    /// disconnects, the operation stops. Exceptions encountered during publishing are logged, and the method completes
    /// gracefully on cancellation.</remarks>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the publishing
    /// operation.</param>
    /// <returns>A task that represents the asynchronous operation of publishing queued messages.</returns>
    private async Task PublishQueuedMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && InternalClient.IsConnected)
            {
                // Peek at the message without dequeueing in order to prevent the
                // possibility of the queue growing beyond the configured cap.
                // Previously, messages could be re-enqueued if there was an
                // exception, and this re-enqueueing did not honor the cap.
                // Furthermore, because re-enqueueing would shuffle the order
                // of the messages, the DropOldestQueuedMessage strategy would
                // be unable to know which message is actually the oldest and would
                // instead drop the first item in the queue.
                var message = _messageQueue.PeekAndWait(cancellationToken);
                if (message is null)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                await TryPublishQueuedMessageAsync(message, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error while publishing queued application messages.");
        }
        finally
        {
            _logger.Verbose("Stopped publishing messages.");
        }
    }

    /// <remarks>This method attempts to resubscribe to all topics that were pending at the time of reconnect.
    /// If an error occurs during the process, the exception is handled internally and does not propagate to the
    /// caller.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <summary>Restores subscriptions that were active before reconnecting.</summary>
    private async Task PublishReconnectSubscriptionsAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Publishing subscriptions at reconnect");

        List<MqttTopicFilter>? topicFilters = null;

        try
        {
            if (_reconnectSubscriptions.Count > 0)
            {
                topicFilters = [];
                SendSubscriptionResults subscribeUnsubscribeResult;

                foreach (var sub in _reconnectSubscriptions)
                {
                    topicFilters.Add(sub.Value);

                    if (topicFilters.Count == Options!.MaxTopicFiltersInSubscribeUnsubscribePackets)
                    {
                        subscribeUnsubscribeResult = await SendSubscribeUnsubscribeAsync(
                                topicFilters,
                                null,
                                cancellationToken)
                            .ConfigureAwait(false);
                        topicFilters.Clear();
                        await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult)
                            .ConfigureAwait(false);
                    }
                }

                subscribeUnsubscribeResult = await SendSubscribeUnsubscribeAsync(
                        topicFilters,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
                await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            await HandleSubscriptionExceptionAsync(exception, topicFilters, null)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Publishes pending subscription changes.</summary>
    /// <remarks>This method processes all queued subscription changes in batches, sending them to the server
    /// until either all are published or the timeout elapses. If the operation is cancelled via the provided token, any
    /// remaining queued subscriptions may not be published.</remarks>
    /// <param name="timeout">The maximum duration to wait for publishing all pending subscriptions and unsubscriptions
    /// before the operation
    /// times out.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before
    /// completion.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    private async Task PublishSubscriptionsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var endTime = TimeProvider.System.GetUtcNow().UtcDateTime + timeout;

        while (
            await _subscriptionsQueuedSignal
                .WaitAsync(GetRemainingTime(endTime), cancellationToken)
                .ConfigureAwait(false))
        {
            List<MqttTopicFilter> subscriptions;
            HashSet<string> unsubscriptions;

            lock (_subscriptions)
            {
                subscriptions = [.. _subscriptions.Values];
                _subscriptions.Clear();

                unsubscriptions = new(_unsubscriptions);
                _unsubscriptions.Clear();
            }

            if (subscriptions.Count == 0 && unsubscriptions.Count == 0)
            {
                continue;
            }

            _logger.Verbose(
                "Publishing {0} added and {1} removed subscriptions",
                subscriptions.Count,
                unsubscriptions.Count);

            await PublishSubscriptionChangesAsync(subscriptions, unsubscriptions, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Publishes pending subscription and unsubscription changes.</summary>
    /// <param name="subscriptions">The subscriptions to add.</param>
    /// <param name="unsubscriptions">The subscriptions to remove.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task representing the publish operation.</returns>
    private async Task PublishSubscriptionChangesAsync(
        List<MqttTopicFilter> subscriptions,
        HashSet<string> unsubscriptions,
        CancellationToken cancellationToken)
    {
        foreach (var unsubscription in unsubscriptions)
        {
            _ = _reconnectSubscriptions.Remove(unsubscription);
        }

        foreach (var subscription in subscriptions)
        {
            _reconnectSubscriptions[subscription.Topic] = subscription;
        }

        await PublishSubscriptionChangesAsync(subscriptions, cancellationToken)
            .ConfigureAwait(false);
        await PublishUnsubscriptionChangesAsync(unsubscriptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Publishes pending subscription changes in broker-sized batches.</summary>
    /// <param name="subscriptions">The subscriptions to add.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task representing the publish operation.</returns>
    private async Task PublishSubscriptionChangesAsync(
        List<MqttTopicFilter> subscriptions,
        CancellationToken cancellationToken)
    {
        var topicFilters = new List<MqttTopicFilter>();
        foreach (var subscription in subscriptions)
        {
            topicFilters.Add(subscription);
            if (topicFilters.Count == Options!.MaxTopicFiltersInSubscribeUnsubscribePackets)
            {
                var result = await SendSubscribeUnsubscribeAsync(
                        topicFilters,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
                topicFilters.Clear();
                await HandleSubscriptionsResultAsync(result).ConfigureAwait(false);
            }
        }

        var finalResult = await SendSubscribeUnsubscribeAsync(topicFilters, null, cancellationToken)
            .ConfigureAwait(false);
        await HandleSubscriptionsResultAsync(finalResult).ConfigureAwait(false);
    }

    /// <summary>Publishes pending unsubscription changes in broker-sized batches.</summary>
    /// <param name="unsubscriptions">The subscriptions to remove.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task representing the publish operation.</returns>
    private async Task PublishUnsubscriptionChangesAsync(
        HashSet<string> unsubscriptions,
        CancellationToken cancellationToken)
    {
        var topicFilters = new List<string>();
        foreach (var unsubscription in unsubscriptions)
        {
            topicFilters.Add(unsubscription);
            if (topicFilters.Count == Options!.MaxTopicFiltersInSubscribeUnsubscribePackets)
            {
                var result = await SendSubscribeUnsubscribeAsync(
                        null,
                        topicFilters,
                        cancellationToken)
                    .ConfigureAwait(false);
                topicFilters.Clear();
                await HandleSubscriptionsResultAsync(result).ConfigureAwait(false);
            }
        }

        var finalResult = await SendSubscribeUnsubscribeAsync(null, topicFilters, cancellationToken)
            .ConfigureAwait(false);
        await HandleSubscriptionsResultAsync(finalResult).ConfigureAwait(false);
    }

    /// <summary>Attempts to re-establish the MQTT client connection if it is not currently connected.</summary>
    /// <remarks>If the client is already connected, no reconnection is attempted. If the reconnection fails,
    /// a connecting failed event is raised before returning <see cref="ReconnectionResult.NotConnected"/>.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the reconnection
    /// attempt.</param>
    /// <returns>A <see cref="ReconnectionResult"/> value indicating the outcome of the reconnection attempt. Returns
    /// <see
    /// cref="ReconnectionResult.StillConnected"/> if the client was already connected, <see
    /// cref="ReconnectionResult.Recovered"/> or <see cref="ReconnectionResult.Reconnected"/> if the connection was
    /// successfully established, or <see cref="ReconnectionResult.NotConnected"/> if the reconnection failed.</returns>
    /// <exception cref="MqttCommunicationException">Thrown if the client connects but the server denies the
    /// connection.</exception>
    private async Task<ReconnectionResult> ReconnectIfRequiredAsync(
        CancellationToken cancellationToken)
    {
        if (InternalClient.IsConnected)
        {
            return ReconnectionResult.StillConnected;
        }

        MqttClientConnectResult? connectResult = null;
        try
        {
            using (var connectTimeout = NewTimeoutToken(cancellationToken))
            {
                connectResult = await InternalClient
                    .ConnectAsync(Options!.ClientOptions, connectTimeout.Token)
                    .ConfigureAwait(false);
            }

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new MqttCommunicationException(
                    $"Client connected but server denied connection with reason '{connectResult.ResultCode}'.");
            }

            return connectResult.IsSessionPresent
                ? ReconnectionResult.Recovered
                : ReconnectionResult.Reconnected;
        }
        catch (Exception exception)
        {
            var eventArgs = new ConnectingFailedEventArgs(connectResult, exception);
            ConnectingFailedEvent?.Invoke(this, eventArgs);
            if (_connectingFailedEvent.HasHandlers)
            {
                await _connectingFailedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }

            return ReconnectionResult.NotConnected;
        }
    }

    /// <summary>Sends a batch of subscription and unsubscription requests.</summary>
    /// <remarks>If both subscriptions and unsubscriptions are requested, unsubscriptions are performed before
    /// subscriptions. If an exception occurs during the operation, the exception is handled internally and the results
    /// reflect only the successful operations completed before the exception.</remarks>
    /// <param name="addedSubscriptions">A list of MQTT topic filters to subscribe to. If null or empty, no
    /// subscriptions are added.</param>
    /// <param name="removedSubscriptions">A list of topic strings to unsubscribe from. If null or empty, no
    /// unsubscriptions are performed.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the subscribe or unsubscribe
    /// operations.</param>
    /// <returns>A SendSubscriptionResults object containing the results of the subscribe and unsubscribe
    /// operations.</returns>
    private async Task<SendSubscriptionResults> SendSubscribeUnsubscribeAsync(
        List<MqttTopicFilter>? addedSubscriptions,
        List<string>? removedSubscriptions,
        CancellationToken cancellationToken)
    {
        var subscribeResults = new List<MqttClientSubscribeResult>();
        var unsubscribeResults = new List<MqttClientUnsubscribeResult>();
        try
        {
            if (removedSubscriptions?.Count > 0)
            {
                var unsubscribeOptionsBuilder = new MqttClientUnsubscribeOptionsBuilder();

                foreach (var removedSubscription in removedSubscriptions)
                {
                    _ = unsubscribeOptionsBuilder.WithTopicFilter(removedSubscription);
                }

                using var unsubscribeTimeout = NewTimeoutToken(cancellationToken);
                var unsubscribeResult = await InternalClient
                    .UnsubscribeAsync(unsubscribeOptionsBuilder.Build(), unsubscribeTimeout.Token)
                    .ConfigureAwait(false);
                unsubscribeResults.Add(unsubscribeResult);

                // clear because these worked, maybe the subscribe below will fail, only report those
                removedSubscriptions.Clear();
            }

            if (addedSubscriptions?.Count > 0)
            {
                var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();

                foreach (var addedSubscription in addedSubscriptions)
                {
                    _ = subscribeOptionsBuilder.WithTopicFilter(addedSubscription);
                }

                using var subscribeTimeout = NewTimeoutToken(cancellationToken);
                var subscribeResult = await InternalClient
                    .SubscribeAsync(subscribeOptionsBuilder.Build(), subscribeTimeout.Token)
                    .ConfigureAwait(false);
                subscribeResults.Add(subscribeResult);
            }
        }
        catch (Exception exception)
        {
            await HandleSubscriptionExceptionAsync(
                    exception,
                    addedSubscriptions,
                    removedSubscriptions)
                .ConfigureAwait(false);
        }

        return new(subscribeResults, unsubscribeResults);
    }

    /// <summary>Begins publishing queued messages in the background.</summary>
    /// <remarks>If publishing is already in progress, this method restarts the publishing process. This
    /// method is intended for internal use and is not thread-safe; callers should ensure appropriate synchronization if
    /// accessed concurrently.</remarks>
    private void StartPublishing()
    {
        StopPublishing();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _publishingCancellationToken = cancellationTokenSource;

        Task.Run(() => PublishQueuedMessagesAsync(cancellationToken), cancellationToken)
            .RunInBackground(_logger);
    }

    /// <summary>Stops maintaining the current connection and releases associated resources.</summary>
    /// <remarks>This method cancels any ongoing connection maintenance operations and disposes of related
    /// resources. After calling this method, the connection maintenance process cannot be resumed until
    /// reinitialized.</remarks>
    private void StopMaintainingConnection()
    {
        try
        {
            _connectionCancellationToken?.Cancel(false);
        }
        finally
        {
            _connectionCancellationToken?.Dispose();
            _connectionCancellationToken = null;
        }
    }

    /// <summary>Stops the current publishing operation and releases associated resources.</summary>
    /// <remarks>This method cancels any ongoing publishing activity and disposes of related resources. After
    /// calling this method, publishing cannot be resumed until a new publishing operation is started.</remarks>
    private void StopPublishing()
    {
        try
        {
            _publishingCancellationToken?.Cancel(false);
        }
        finally
        {
            _publishingCancellationToken?.Dispose();
            _publishingCancellationToken = null;
        }
    }

    /// <summary>Maintains the connection and synchronizes client state.</summary>
    /// <remarks>This method handles reconnection logic, subscription recovery, and publishing state
    /// transitions based on the current connection status. If the connection state changes, a connection state changed
    /// event is raised. Exceptions related to communication errors are logged, but not rethrown.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the connection maintenance
    /// operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task TryMaintainConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var oldConnectionState = InternalClient.IsConnected;
            var connectionState = await ReconnectIfRequiredAsync(cancellationToken)
                .ConfigureAwait(false);

            if (connectionState == ReconnectionResult.NotConnected)
            {
                StopPublishing();
                await Task.Delay(Options!.AutoReconnectDelay, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (connectionState == ReconnectionResult.Reconnected)
            {
                await PublishReconnectSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
                StartPublishing();
            }
            else if (connectionState == ReconnectionResult.Recovered)
            {
                StartPublishing();
            }
            else if (connectionState == ReconnectionResult.StillConnected)
            {
                await PublishSubscriptionsAsync(Options!.ConnectionCheckInterval, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (oldConnectionState != InternalClient.IsConnected)
            {
                ConnectionStateChangedEvent?.Invoke(this, EventArgs.Empty);
                if (_connectionStateChangedEvent.HasHandlers)
                {
                    await _connectionStateChangedEvent
                        .InvokeAsync(EventArgs.Empty)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (MqttCommunicationException exception)
        {
            _logger.Warning(exception, "Communication error while maintaining connection.");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error exception while maintaining connection.");
        }
    }
}
