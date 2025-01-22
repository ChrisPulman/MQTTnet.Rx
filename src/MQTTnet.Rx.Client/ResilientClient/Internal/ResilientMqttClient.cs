// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Resilient Mqtt Client.
/// </summary>
/// <seealso cref="Disposable" />
/// <seealso cref="IResilientMqttClient" />
internal sealed class ResilientMqttClient : Disposable, IResilientMqttClient
{
    private readonly MqttNetSourceLogger _logger;

    private readonly AsyncEvent<InterceptingPublishMessageEventArgs> _interceptingPublishMessageEvent = new();
    private readonly AsyncEvent<ApplicationMessageProcessedEventArgs> _applicationMessageProcessedEvent = new();
    private readonly AsyncEvent<ApplicationMessageSkippedEventArgs> _applicationMessageSkippedEvent = new();
    private readonly AsyncEvent<ConnectingFailedEventArgs> _connectingFailedEvent = new();
    private readonly AsyncEvent<EventArgs> _connectionStateChangedEvent = new();
    private readonly AsyncEvent<ResilientProcessFailedEventArgs> _synchronizingSubscriptionsFailedEvent = new();
    private readonly AsyncEvent<SubscriptionsChangedEventArgs> _subscriptionsChangedEvent = new();

    private readonly BlockingQueue<ResilientMqttApplicationMessage> _messageQueue = new();
    private readonly AsyncLock _messageQueueLock = new();
    private readonly Dictionary<string, MqttTopicFilter> _reconnectSubscriptions = [];

    private readonly Dictionary<string, MqttTopicFilter> _subscriptions = [];
    private readonly SemaphoreSlim _subscriptionsQueuedSignal = new(0);
    private readonly HashSet<string> _unsubscriptions = [];

    private CancellationTokenSource? _connectionCancellationToken;
    private Task? _maintainConnectionTask;
    private CancellationTokenSource? _publishingCancellationToken;

    private ResilientMqttClientStorageManager? _storageManager;
    private bool _isCleanDisconnect;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientMqttClient"/> class.
    /// </summary>
    /// <param name="mqttClient">The MQTT client.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// mqttClient
    /// or
    /// logger.
    /// </exception>
    public ResilientMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
    {
        InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(ResilientMqttClient));
    }

    /// <summary>
    /// Occurs when [application message skipped asynchronous].
    /// </summary>
    public event Func<ApplicationMessageSkippedEventArgs, Task> ApplicationMessageSkippedAsync
    {
        add => _applicationMessageSkippedEvent.AddHandler(value);
        remove => _applicationMessageSkippedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [application message processed asynchronous].
    /// </summary>
    public event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync
    {
        add => _applicationMessageProcessedEvent.AddHandler(value);
        remove => _applicationMessageProcessedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [intercept publish message asynchronous].
    /// </summary>
    public event Func<InterceptingPublishMessageEventArgs, Task> InterceptPublishMessageAsync
    {
        add => _interceptingPublishMessageEvent.AddHandler(value);
        remove => _interceptingPublishMessageEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [application message received asynchronous].
    /// </summary>
    public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
    {
        add => InternalClient.ApplicationMessageReceivedAsync += value;
        remove => InternalClient.ApplicationMessageReceivedAsync -= value;
    }

    /// <summary>
    /// Occurs when [connected asynchronous].
    /// </summary>
    public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
    {
        add => InternalClient.ConnectedAsync += value;
        remove => InternalClient.ConnectedAsync -= value;
    }

    /// <summary>
    /// Occurs when [connecting failed asynchronous].
    /// </summary>
    public event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync
    {
        add => _connectingFailedEvent.AddHandler(value);
        remove => _connectingFailedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [connection state changed asynchronous].
    /// </summary>
    public event Func<EventArgs, Task> ConnectionStateChangedAsync
    {
        add => _connectionStateChangedEvent.AddHandler(value);
        remove => _connectionStateChangedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [disconnected asynchronous].
    /// </summary>
    public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
    {
        add => InternalClient.DisconnectedAsync += value;
        remove => InternalClient.DisconnectedAsync -= value;
    }

    /// <summary>
    /// Occurs when [synchronizing subscriptions failed asynchronous].
    /// </summary>
    public event Func<ResilientProcessFailedEventArgs, Task> SynchronizingSubscriptionsFailedAsync
    {
        add => _synchronizingSubscriptionsFailedEvent.AddHandler(value);
        remove => _synchronizingSubscriptionsFailedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when [subscriptions changed asynchronous].
    /// </summary>
    public event Func<SubscriptionsChangedEventArgs, Task> SubscriptionsChangedAsync
    {
        add => _subscriptionsChangedEvent.AddHandler(value);
        remove => _subscriptionsChangedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Gets application messages processed.
    /// </summary>
    /// <returns>A Application Message Processed Event Args.</returns>
    public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed =>
        CreateObservable.FromAsyncEvent<ApplicationMessageProcessedEventArgs>(
            handler => ApplicationMessageProcessedAsync += handler,
            handler => ApplicationMessageProcessedAsync -= handler);

    /// <summary>
    /// Gets connected to the specified client.
    /// </summary>
    /// <returns>A Mqtt Client Connected Event Args.</returns>
    public IObservable<MqttClientConnectedEventArgs> Connected =>
        CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
            handler => ConnectedAsync += handler,
            handler => ConnectedAsync -= handler);

    /// <summary>
    /// Gets disconnected from the specified client.
    /// </summary>
    /// <returns>A Mqtt Client Disconnected Event Args.</returns>
    public IObservable<MqttClientDisconnectedEventArgs> Disconnected =>
        CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
            handler => DisconnectedAsync += handler,
            handler => DisconnectedAsync -= handler);

    /// <summary>
    /// Gets connecting failed.
    /// </summary>
    /// <returns>A Connecting Failed Event Args.</returns>
    public IObservable<ConnectingFailedEventArgs> ConnectingFailed =>
        CreateObservable.FromAsyncEvent<ConnectingFailedEventArgs>(
            handler => ConnectingFailedAsync += handler,
            handler => ConnectingFailedAsync -= handler);

    /// <summary>
    /// Gets connection state changed.
    /// </summary>
    /// <returns>Event Args.</returns>
    public IObservable<EventArgs> ConnectionStateChanged =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => ConnectionStateChangedAsync += handler,
            handler => ConnectionStateChangedAsync -= handler);

    /// <summary>
    /// Gets synchronizing subscriptions failed.
    /// </summary>
    /// <returns>A Resilient Process Failed Event Args.</returns>
    public IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed =>
        CreateObservable.FromAsyncEvent<ResilientProcessFailedEventArgs>(
            handler => SynchronizingSubscriptionsFailedAsync += handler,
            handler => SynchronizingSubscriptionsFailedAsync -= handler);

    /// <summary>
    /// Gets application messages processed.
    /// </summary>
    /// <returns>A Application Message Skipped Event Args.</returns>
    public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped =>
        CreateObservable.FromAsyncEvent<ApplicationMessageSkippedEventArgs>(
            handler => ApplicationMessageSkippedAsync += handler,
            handler => ApplicationMessageSkippedAsync -= handler);

    /// <summary>
    /// Gets application messages received.
    /// </summary>
    /// <returns>A Mqtt Application Message Received Event Args.</returns>
    public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived =>
        CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => ApplicationMessageReceivedAsync += handler,
            handler => ApplicationMessageReceivedAsync -= handler);

    /// <summary>
    /// Gets the internal client.
    /// </summary>
    /// <value>
    /// The internal client.
    /// </value>
    public IMqttClient InternalClient { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is connected.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
    /// </value>
    public bool IsConnected => InternalClient.IsConnected;

    /// <summary>
    /// Gets a value indicating whether this instance is started.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is started; otherwise, <c>false</c>.
    /// </value>
    public bool IsStarted => _connectionCancellationToken != null;

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <value>
    /// The options.
    /// </value>
    public ResilientMqttClientOptions? Options { get; private set; }

    /// <summary>
    /// Gets the pending application messages count.
    /// </summary>
    /// <value>
    /// The pending application messages count.
    /// </value>
    public int PendingApplicationMessagesCount => _messageQueue.Count;

    /// <summary>
    /// Enqueues the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <exception cref="ArgumentNullException">applicationMessage.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnqueueAsync(MqttApplicationMessage applicationMessage)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(applicationMessage);

        var managedMqttApplicationMessage = new ResilientMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage);
        await EnqueueAsync(managedMqttApplicationMessage.Build()).ConfigureAwait(false);
    }

    /// <summary>
    /// Enqueues the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <exception cref="ArgumentNullException">applicationMessage.</exception>
    /// <exception cref="InvalidOperationException">call StartAsync before publishing messages.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(applicationMessage);

        if (Options == null)
        {
            throw new InvalidOperationException("call StartAsync before publishing messages");
        }

        MqttTopicValidator.ThrowIfInvalid(applicationMessage.ApplicationMessage);

        ResilientMqttApplicationMessage? removedMessage = null;
        ApplicationMessageSkippedEventArgs? applicationMessageSkippedEventArgs = null;

        try
        {
            using (await _messageQueueLock.EnterAsync().ConfigureAwait(false))
            {
                if (_messageQueue.Count >= Options.MaxPendingMessages)
                {
                    if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                    {
                        _logger.Verbose("Skipping publish of new application message because internal queue is full.");
                        applicationMessageSkippedEventArgs = new ApplicationMessageSkippedEventArgs(applicationMessage);
                        return;
                    }

                    if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                    {
                        removedMessage = _messageQueue.RemoveFirst();
                        _logger.Verbose("Removed oldest application message from internal queue because it is full.");
                        applicationMessageSkippedEventArgs = new ApplicationMessageSkippedEventArgs(removedMessage);
                    }
                }

                _messageQueue.Enqueue(applicationMessage);

                if (_storageManager != null)
                {
                    if (removedMessage != null)
                    {
                        await _storageManager.RemoveAsync(removedMessage).ConfigureAwait(false);
                    }

                    await _storageManager.AddAsync(applicationMessage).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (applicationMessageSkippedEventArgs != null && _applicationMessageSkippedEvent.HasHandlers)
            {
                await _applicationMessageSkippedEvent.InvokeAsync(applicationMessageSkippedEventArgs).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Pings the asynchronous.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public Task PingAsync(CancellationToken cancellationToken = default) => InternalClient.PingAsync(cancellationToken);

    /// <summary>
    /// Starts the asynchronous.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options.</exception>
    /// <exception cref="ArgumentException">The client options are not set. - options.</exception>
    /// <exception cref="InvalidOperationException">The managed client is already started.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(ResilientMqttClientOptions options)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(options);

        if (options.ClientOptions == null)
        {
            throw new ArgumentException("The client options are not set.", nameof(options));
        }

        if (!_maintainConnectionTask?.IsCompleted ?? false)
        {
            throw new InvalidOperationException("The managed client is already started.");
        }

        Options = options;

        if (options.Storage != null)
        {
            _storageManager = new ResilientMqttClientStorageManager(options.Storage);
            var messages = await _storageManager.LoadQueuedMessagesAsync().ConfigureAwait(false);

            foreach (var message in messages)
            {
                _messageQueue.Enqueue(message);
            }
        }

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _connectionCancellationToken = cancellationTokenSource;

        _maintainConnectionTask = Task.Run(() => MaintainConnectionAsync(cancellationToken), cancellationToken);
        _maintainConnectionTask.RunInBackground(_logger);

        _logger.Info("Started");
    }

    /// <summary>
    /// Stops the asynchronous.
    /// </summary>
    /// <param name="cleanDisconnect">if set to <c>true</c> [clean disconnect].</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync(bool cleanDisconnect = true)
    {
        ThrowIfDisposed();

        _isCleanDisconnect = cleanDisconnect;

        StopPublishing();
        StopMaintainingConnection();

        _messageQueue.Clear();

        if (_maintainConnectionTask != null)
        {
            await Task.WhenAny(_maintainConnectionTask);
            _maintainConnectionTask = null;
        }
    }

    /// <summary>
    /// Subscribes the asynchronous.
    /// </summary>
    /// <param name="topicFilters">The topic filters.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">topicFilters.</exception>
    public Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(topicFilters);

        foreach (var topicFilter in topicFilters)
        {
            MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter.Topic);
        }

        lock (_subscriptions)
        {
            foreach (var topicFilter in topicFilters)
            {
                _subscriptions[topicFilter.Topic] = topicFilter;
                _unsubscriptions.Remove(topicFilter.Topic);
            }
        }

        _subscriptionsQueuedSignal.Release();

        return CompletedTask.Instance;
    }

    /// <summary>
    /// Unsubscribes the asynchronous.
    /// </summary>
    /// <param name="topics">The topics.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">topics.</exception>
    public Task UnsubscribeAsync(IEnumerable<string> topics)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(topics);

        lock (_subscriptions)
        {
            foreach (var topic in topics)
            {
                _subscriptions.Remove(topic);
                _unsubscriptions.Add(topic);
            }
        }

        _subscriptionsQueuedSignal.Release();

        return CompletedTask.Instance;
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopPublishing();
            StopMaintainingConnection();

            if (_maintainConnectionTask != null)
            {
                _maintainConnectionTask.GetAwaiter().GetResult();
                _maintainConnectionTask = null;
            }

            _messageQueue.Dispose();
            _messageQueueLock.Dispose();
            InternalClient.Dispose();
            _subscriptionsQueuedSignal.Dispose();
            _storageManager?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static TimeSpan GetRemainingTime(in DateTime endTime)
    {
        var remainingTime = endTime - DateTime.UtcNow;
        return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
    }

    private CancellationTokenSource NewTimeoutToken(in CancellationToken linkedToken)
    {
        var newTimeoutToken = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        newTimeoutToken.CancelAfter(Options!.ClientOptions!.Timeout);
        return newTimeoutToken;
    }

    private async Task HandleSubscriptionExceptionAsync(Exception exception, List<MqttTopicFilter>? addedSubscriptions, List<string>? removedSubscriptions)
    {
        _logger.Warning(exception, "Synchronizing subscriptions failed.");

        if (_synchronizingSubscriptionsFailedEvent.HasHandlers)
        {
            await _synchronizingSubscriptionsFailedEvent.InvokeAsync(new ResilientProcessFailedEventArgs(exception, addedSubscriptions, removedSubscriptions)).ConfigureAwait(false);
        }
    }

    private async Task HandleSubscriptionsResultAsync(SendSubscriptionResults subscribeUnsubscribeResult)
    {
        if (_subscriptionsChangedEvent.HasHandlers)
        {
            await _subscriptionsChangedEvent.InvokeAsync(new SubscriptionsChangedEventArgs(subscribeUnsubscribeResult.SubscribeResults, subscribeUnsubscribeResult.UnsubscribeResults)).ConfigureAwait(false);
        }
    }

    private async Task MaintainConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await TryMaintainConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
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
                        using (var disconnectTimeout = NewTimeoutToken(CancellationToken.None))
                        {
                            await InternalClient.DisconnectAsync(new MqttClientDisconnectOptions(), disconnectTimeout.Token).ConfigureAwait(false);
                        }
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
                if (message == null)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                await TryPublishQueuedMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error while publishing queued application messages.");
        }
        finally
        {
            _logger.Verbose("Stopped publishing messages.");
        }
    }

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
                        subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(topicFilters, null, cancellationToken).ConfigureAwait(false);
                        topicFilters.Clear();
                        await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);
                    }
                }

                subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(topicFilters, null, cancellationToken).ConfigureAwait(false);
                await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            await HandleSubscriptionExceptionAsync(exception, topicFilters, null).ConfigureAwait(false);
        }
    }

    private async Task PublishSubscriptionsAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var endTime = DateTime.UtcNow + timeout;

        while (await _subscriptionsQueuedSignal.WaitAsync(GetRemainingTime(endTime), cancellationToken).ConfigureAwait(false))
        {
            List<MqttTopicFilter> subscriptions;
            SendSubscriptionResults subscribeUnsubscribeResult;
            HashSet<string> unsubscriptions;

            lock (_subscriptions)
            {
                subscriptions = [.. _subscriptions.Values];
                _subscriptions.Clear();

                unsubscriptions = new HashSet<string>(_unsubscriptions);
                _unsubscriptions.Clear();
            }

            if (subscriptions.Count == 0 && unsubscriptions.Count == 0)
            {
                continue;
            }

            _logger.Verbose("Publishing {0} added and {1} removed subscriptions", subscriptions.Count, unsubscriptions.Count);

            foreach (var unsubscription in unsubscriptions)
            {
                _reconnectSubscriptions.Remove(unsubscription);
            }

            foreach (var subscription in subscriptions)
            {
                _reconnectSubscriptions[subscription.Topic] = subscription;
            }

            var addedTopicFilters = new List<MqttTopicFilter>();
            foreach (var subscription in subscriptions)
            {
                addedTopicFilters.Add(subscription);

                if (addedTopicFilters.Count == Options!.MaxTopicFiltersInSubscribeUnsubscribePackets)
                {
                    subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(addedTopicFilters, null, cancellationToken).ConfigureAwait(false);
                    addedTopicFilters.Clear();
                    await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);
                }
            }

            subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(addedTopicFilters, null, cancellationToken).ConfigureAwait(false);
            await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);

            var removedTopicFilters = new List<string>();
            foreach (var unSub in unsubscriptions)
            {
                removedTopicFilters.Add(unSub);

                if (removedTopicFilters.Count == Options!.MaxTopicFiltersInSubscribeUnsubscribePackets)
                {
                    subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(null, removedTopicFilters, cancellationToken).ConfigureAwait(false);
                    removedTopicFilters.Clear();
                    await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);
                }
            }

            subscribeUnsubscribeResult = await SendSubscribeUnsubscribe(null, removedTopicFilters, cancellationToken).ConfigureAwait(false);
            await HandleSubscriptionsResultAsync(subscribeUnsubscribeResult).ConfigureAwait(false);
        }
    }

    private async Task<ReconnectionResult> ReconnectIfRequiredAsync(CancellationToken cancellationToken)
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
                connectResult = await InternalClient.ConnectAsync(Options!.ClientOptions, connectTimeout.Token).ConfigureAwait(false);
            }

            if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new MqttCommunicationException($"Client connected but server denied connection with reason '{connectResult.ResultCode}'.");
            }

            return connectResult.IsSessionPresent ? ReconnectionResult.Recovered : ReconnectionResult.Reconnected;
        }
        catch (Exception exception)
        {
            await _connectingFailedEvent.InvokeAsync(new ConnectingFailedEventArgs(connectResult, exception));
            return ReconnectionResult.NotConnected;
        }
    }

    private async Task<SendSubscriptionResults> SendSubscribeUnsubscribe(List<MqttTopicFilter>? addedSubscriptions, List<string>? removedSubscriptions, CancellationToken cancellationToken)
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
                    unsubscribeOptionsBuilder.WithTopicFilter(removedSubscription);
                }

                using (var unsubscribeTimeout = NewTimeoutToken(cancellationToken))
                {
                    var unsubscribeResult = await InternalClient.UnsubscribeAsync(unsubscribeOptionsBuilder.Build(), unsubscribeTimeout.Token).ConfigureAwait(false);
                    unsubscribeResults.Add(unsubscribeResult);
                }

                // clear because these worked, maybe the subscribe below will fail, only report those
                removedSubscriptions.Clear();
            }

            if (addedSubscriptions?.Count > 0)
            {
                var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();

                foreach (var addedSubscription in addedSubscriptions)
                {
                    subscribeOptionsBuilder.WithTopicFilter(addedSubscription);
                }

                using (var subscribeTimeout = NewTimeoutToken(cancellationToken))
                {
                    var subscribeResult = await InternalClient.SubscribeAsync(subscribeOptionsBuilder.Build(), subscribeTimeout.Token).ConfigureAwait(false);
                    subscribeResults.Add(subscribeResult);
                }
            }
        }
        catch (Exception exception)
        {
            await HandleSubscriptionExceptionAsync(exception, addedSubscriptions, removedSubscriptions).ConfigureAwait(false);
        }

        return new SendSubscriptionResults(subscribeResults, unsubscribeResults);
    }

    private void StartPublishing()
    {
        StopPublishing();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _publishingCancellationToken = cancellationTokenSource;

        Task.Run(() => PublishQueuedMessagesAsync(cancellationToken), cancellationToken).RunInBackground(_logger);
    }

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

    private async Task TryMaintainConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var oldConnectionState = InternalClient.IsConnected;
            var connectionState = await ReconnectIfRequiredAsync(cancellationToken).ConfigureAwait(false);

            if (connectionState == ReconnectionResult.NotConnected)
            {
                StopPublishing();
                await Task.Delay(Options!.AutoReconnectDelay, cancellationToken).ConfigureAwait(false);
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
                await PublishSubscriptionsAsync(Options!.ConnectionCheckInterval, cancellationToken).ConfigureAwait(false);
            }

            if (oldConnectionState != InternalClient.IsConnected)
            {
                await _connectionStateChangedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (MqttCommunicationException exception)
        {
            _logger.Warning(exception, "Communication error while maintaining connection.");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error exception while maintaining connection.");
        }
    }

    private async Task TryPublishQueuedMessageAsync(ResilientMqttApplicationMessage message, CancellationToken cancellationToken)
    {
        Exception? transmitException = null;
        var acceptPublish = true;
        try
        {
            if (_interceptingPublishMessageEvent.HasHandlers)
            {
                var interceptEventArgs = new InterceptingPublishMessageEventArgs(message);
                await _interceptingPublishMessageEvent.InvokeAsync(interceptEventArgs).ConfigureAwait(false);
                acceptPublish = interceptEventArgs.AcceptPublish;
            }

            if (acceptPublish)
            {
                using (var publishTimeout = NewTimeoutToken(cancellationToken))
                {
                    await InternalClient.PublishAsync(message.ApplicationMessage, publishTimeout.Token).ConfigureAwait(false);
                }
            }

            using (await _messageQueueLock.EnterAsync(CancellationToken.None).ConfigureAwait(false)) // lock to avoid conflict with this.PublishAsync
            {
                // While publishing this message, this.PublishAsync could have booted this
                // message off the queue to make room for another (when using a cap
                // with the DropOldestQueuedMessage strategy).  If the first item
                // in the queue is equal to this message, then it's safe to remove
                // it from the queue.  If not, that means this.PublishAsync has already
                // removed it, in which case we don't want to do anything.
                _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));

                if (_storageManager != null)
                {
                    await _storageManager.RemoveAsync(message).ConfigureAwait(false);
                }
            }
        }
        catch (MqttCommunicationException exception)
        {
            transmitException = exception;

            _logger.Warning(exception, "Publishing application message ({0}) failed.", message.Id);

            if (message.ApplicationMessage?.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                // If QoS 0, we don't want this message to stay on the queue.
                // If QoS 1 or 2, it's possible that, when using a cap, this message
                // has been booted off the queue by this.PublishAsync, in which case this
                // thread will not continue to try to publish it. While this does
                // contradict the expected behavior of QoS 1 and 2, that's also true
                // for the usage of a message queue cap, so it's still consistent
                // with prior behavior in that way.
                using (await _messageQueueLock.EnterAsync(CancellationToken.None).ConfigureAwait(false)) // lock to avoid conflict with this.PublishAsync
                {
                    _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));

                    if (_storageManager != null)
                    {
                        await _storageManager.RemoveAsync(message).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            transmitException = exception;
            _logger.Error(exception, "Error while publishing application message ({0}).", message.Id);
        }
        finally
        {
            if (_applicationMessageProcessedEvent.HasHandlers)
            {
                var eventArgs = new ApplicationMessageProcessedEventArgs(message, transmitException);
                await _applicationMessageProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
        }
    }
}
