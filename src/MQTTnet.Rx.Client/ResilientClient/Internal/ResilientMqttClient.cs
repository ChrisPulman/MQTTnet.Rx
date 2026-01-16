// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Provides a resilient MQTT client that manages automatic reconnection, message queuing, and subscription
/// synchronization for reliable MQTT communication.
/// </summary>
/// <remarks>The ResilientMqttClient is designed to handle transient network failures and broker disconnects by
/// automatically reconnecting and resynchronizing subscriptions. It supports queuing of outgoing messages, configurable
/// overflow strategies, and event-driven notifications for connection and message processing states. This class is
/// intended for scenarios where robust, fault-tolerant MQTT client behavior is required, such as IoT device
/// communication or backend message processing. Thread safety is maintained for all public operations. Dispose the
/// client when it is no longer needed to release resources.</remarks>
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
    /// Initializes a new instance of the <see cref="ResilientMqttClient"/> class with the specified MQTT client and logger.
    /// </summary>
    /// <param name="mqttClient">The underlying MQTT client to be used for communication. Cannot be null.</param>
    /// <param name="logger">The logger instance used for diagnostic and operational logging. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if mqttClient or logger is null.</exception>
    public ResilientMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
    {
        InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger.WithSource(nameof(ResilientMqttClient));
    }

    /// <summary>
    /// Occurs when an application message is skipped and not delivered to any subscribers.
    /// </summary>
    /// <remarks>This event allows subscribers to handle scenarios where an application message could not be
    /// processed or routed. Handlers can use the provided event arguments to inspect the skipped message and take
    /// appropriate action, such as logging or custom error handling. The event is invoked asynchronously and may be
    /// triggered on background threads.</remarks>
    public event Func<ApplicationMessageSkippedEventArgs, Task> ApplicationMessageSkippedAsync
    {
        add => _applicationMessageSkippedEvent.AddHandler(value);
        remove => _applicationMessageSkippedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when an application message has been processed asynchronously.
    /// </summary>
    /// <remarks>Subscribers can use this event to perform additional actions after an application message has
    /// been handled. The event handler is invoked asynchronously and receives details about the processed message
    /// through the <see cref="ApplicationMessageProcessedEventArgs"/> parameter.</remarks>
    public event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync
    {
        add => _applicationMessageProcessedEvent.AddHandler(value);
        remove => _applicationMessageProcessedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when a client publishes a message, allowing the message to be intercepted and optionally modified or
    /// blocked asynchronously before it is processed by the server.
    /// </summary>
    /// <remarks>Use this event to inspect, modify, or prevent the delivery of published messages. Handlers
    /// can perform asynchronous operations and must complete before the message is processed further. If multiple
    /// handlers are registered, they are invoked in the order added. Exceptions thrown by handlers may affect message
    /// processing.</remarks>
    public event Func<InterceptingPublishMessageEventArgs, Task> InterceptPublishMessageAsync
    {
        add => _interceptingPublishMessageEvent.AddHandler(value);
        remove => _interceptingPublishMessageEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when an application message is received from the MQTT broker and allows asynchronous processing of the
    /// message.
    /// </summary>
    /// <remarks>Subscribe to this event to handle incoming MQTT application messages. The event handler
    /// receives an instance of <see cref="MqttApplicationMessageReceivedEventArgs"/> containing details about the
    /// received message. The handler can perform asynchronous operations and should return a <see cref="Task"/> that
    /// completes when processing is finished. If multiple handlers are attached, they are invoked sequentially in the
    /// order they were added.</remarks>
    public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
    {
        add => InternalClient.ApplicationMessageReceivedAsync += value;
        remove => InternalClient.ApplicationMessageReceivedAsync -= value;
    }

    /// <summary>
    /// Occurs when the client has successfully established a connection to the MQTT broker.
    /// </summary>
    /// <remarks>This event is invoked after the client completes the connection handshake with the broker.
    /// Handlers can perform post-connection logic, such as subscribing to topics or initializing resources. The event
    /// is asynchronous; all registered handlers are awaited before the connection process continues.</remarks>
    public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
    {
        add => InternalClient.ConnectedAsync += value;
        remove => InternalClient.ConnectedAsync -= value;
    }

    /// <summary>
    /// Occurs when an attempt to establish a connection fails, allowing asynchronous handling of the failure event.
    /// </summary>
    /// <remarks>Subscribers can use this event to perform custom logic, such as logging or retrying, when a
    /// connection attempt does not succeed. The event handler receives a <see cref="ConnectingFailedEventArgs"/>
    /// instance containing details about the failure. Handlers are invoked asynchronously and should return a <see
    /// cref="Task"/> that completes when processing is finished.</remarks>
    public event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync
    {
        add => _connectingFailedEvent.AddHandler(value);
        remove => _connectingFailedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs asynchronously when the connection state changes.
    /// </summary>
    /// <remarks>Subscribers are invoked asynchronously in the order they were added. Use this event to
    /// perform actions in response to changes in the connection's state, such as connecting or disconnecting. Handlers
    /// should not perform long-running operations synchronously to avoid delaying other subscribers.</remarks>
    public event Func<EventArgs, Task> ConnectionStateChangedAsync
    {
        add => _connectionStateChangedEvent.AddHandler(value);
        remove => _connectionStateChangedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs when the client is disconnected from the MQTT broker asynchronously.
    /// </summary>
    /// <remarks>Subscribe to this event to handle cleanup or reconnection logic when the client disconnects.
    /// The event handler receives an <see cref="MqttClientDisconnectedEventArgs"/> instance containing details about
    /// the disconnection. The event is invoked on a background thread; ensure any UI updates are marshaled
    /// appropriately.</remarks>
    public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
    {
        add => InternalClient.DisconnectedAsync += value;
        remove => InternalClient.DisconnectedAsync -= value;
    }

    /// <summary>
    /// Occurs when an error is encountered while synchronizing subscriptions asynchronously.
    /// </summary>
    /// <remarks>Subscribers can use this event to handle failures that occur during the subscription
    /// synchronization process. The event is invoked with details about the failure, allowing custom error handling or
    /// logging. The event handler should return a completed task when processing is finished.</remarks>
    public event Func<ResilientProcessFailedEventArgs, Task> SynchronizingSubscriptionsFailedAsync
    {
        add => _synchronizingSubscriptionsFailedEvent.AddHandler(value);
        remove => _synchronizingSubscriptionsFailedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Occurs asynchronously when the set of subscriptions changes.
    /// </summary>
    /// <remarks>Event handlers are invoked asynchronously and should return a completed Task when finished.
    /// Use this event to respond to additions, removals, or updates to the current subscriptions. Handlers may be
    /// executed on a thread pool thread; ensure any shared resources are accessed in a thread-safe manner.</remarks>
    public event Func<SubscriptionsChangedEventArgs, Task> SubscriptionsChangedAsync
    {
        add => _subscriptionsChangedEvent.AddHandler(value);
        remove => _subscriptionsChangedEvent.RemoveHandler(value);
    }

    /// <summary>
    /// Gets an observable sequence that signals when an application message has been processed.
    /// </summary>
    /// <remarks>Subscribers to this observable are notified each time an application message is processed.
    /// The sequence completes when the underlying event source is disposed or no longer available.</remarks>
    public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed =>
        CreateObservable.FromAsyncEvent<ApplicationMessageProcessedEventArgs>(
            handler => ApplicationMessageProcessedAsync += handler,
            handler => ApplicationMessageProcessedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when the client successfully connects to the MQTT broker.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time a connection to the broker is established. The
    /// observable emits a value for every successful connection event, including initial and subsequent
    /// reconnects.</remarks>
    public IObservable<MqttClientConnectedEventArgs> Connected =>
        CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
            handler => ConnectedAsync += handler,
            handler => ConnectedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when the client is disconnected from the MQTT broker.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time the client disconnects, along with details about
    /// the disconnection event. The sequence completes when the underlying client is disposed.</remarks>
    public IObservable<MqttClientDisconnectedEventArgs> Disconnected =>
        CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
            handler => DisconnectedAsync += handler,
            handler => DisconnectedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when a connection attempt fails.
    /// </summary>
    /// <remarks>Subscribers are notified each time a connection attempt does not succeed. The observable
    /// emits a value containing details about the failure. This can be used to implement custom error handling or retry
    /// logic in response to connection failures.</remarks>
    public IObservable<ConnectingFailedEventArgs> ConnectingFailed =>
        CreateObservable.FromAsyncEvent<ConnectingFailedEventArgs>(
            handler => ConnectingFailedAsync += handler,
            handler => ConnectingFailedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when the connection state changes.
    /// </summary>
    /// <remarks>Subscribers are notified each time the connection state transitions, such as when connecting
    /// or disconnecting. The sequence completes when the underlying object is disposed, if applicable.</remarks>
    public IObservable<EventArgs> ConnectionStateChanged =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => ConnectionStateChangedAsync += handler,
            handler => ConnectionStateChangedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when a failure occurs during the synchronization of subscriptions.
    /// </summary>
    public IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed =>
        CreateObservable.FromAsyncEvent<ResilientProcessFailedEventArgs>(
            handler => SynchronizingSubscriptionsFailedAsync += handler,
            handler => SynchronizingSubscriptionsFailedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when an application message is skipped during processing.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time an application message is not processed and is
    /// skipped. This can be used to monitor or log skipped messages for diagnostic or auditing purposes.</remarks>
    public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped =>
        CreateObservable.FromAsyncEvent<ApplicationMessageSkippedEventArgs>(
            handler => ApplicationMessageSkippedAsync += handler,
            handler => ApplicationMessageSkippedAsync -= handler);

    /// <summary>
    /// Gets an observable sequence that signals when an application message is received from the MQTT broker.
    /// </summary>
    /// <remarks>Subscribers to this observable are notified each time an application message is received. The
    /// sequence completes when the underlying client is disposed or disconnected. This property enables reactive
    /// handling of incoming MQTT messages using the observer pattern.</remarks>
    public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived =>
        CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => ApplicationMessageReceivedAsync += handler,
            handler => ApplicationMessageReceivedAsync -= handler);

    /// <summary>
    /// Gets the underlying MQTT client instance used for low-level operations.
    /// </summary>
    /// <remarks>This property exposes the internal client for advanced scenarios where direct access to the
    /// MQTT protocol features is required. Modifying or interacting with the internal client may affect the overall
    /// connection state and should be done with caution.</remarks>
    public IMqttClient InternalClient { get; }

    /// <summary>
    /// Gets a value indicating whether the client is currently connected.
    /// </summary>
    public bool IsConnected => InternalClient.IsConnected;

    /// <summary>
    /// Gets a value indicating whether the connection has been started.
    /// </summary>
    public bool IsStarted => _connectionCancellationToken != null;

    /// <summary>
    /// Gets the options used to configure the resilient MQTT client.
    /// </summary>
    public ResilientMqttClientOptions? Options { get; private set; }

    /// <summary>
    /// Gets the number of application messages that are currently pending in the queue and awaiting processing.
    /// </summary>
    public int PendingApplicationMessagesCount => _messageQueue.Count;

    /// <summary>
    /// Enqueues an MQTT application message for asynchronous publishing by the managed client.
    /// </summary>
    /// <remarks>The message is added to the internal queue and will be published according to the client's
    /// configured delivery and retry policies. This method does not guarantee immediate delivery.</remarks>
    /// <param name="applicationMessage">The MQTT application message to enqueue. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous enqueue operation.</returns>
    public async Task EnqueueAsync(MqttApplicationMessage applicationMessage)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(applicationMessage);

        var managedMqttApplicationMessage = new ResilientMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage);
        await EnqueueAsync(managedMqttApplicationMessage.Build()).ConfigureAwait(false);
    }

    /// <summary>
    /// Enqueues an application message for publishing to the MQTT broker, applying overflow strategies if the internal
    /// message queue is full.
    /// </summary>
    /// <remarks>If the internal message queue has reached its maximum capacity, the behavior depends on the
    /// configured overflow strategy: the new message may be dropped, or the oldest queued message may be removed to
    /// make space. If a message is skipped or removed due to overflow, the ApplicationMessageSkipped event is raised.
    /// This method is thread-safe and can be called concurrently.</remarks>
    /// <param name="applicationMessage">The application message to enqueue for publishing. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous enqueue operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the client has not been started. Call StartAsync before publishing messages.</exception>
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
    /// Sends a ping request to the server to verify connectivity asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the ping operation.</param>
    /// <returns>A task that represents the asynchronous ping operation.</returns>
    public Task PingAsync(CancellationToken cancellationToken = default) => InternalClient.PingAsync(cancellationToken);

    /// <summary>
    /// Starts the MQTT client and begins maintaining a resilient connection using the specified options.
    /// </summary>
    /// <param name="options">The configuration options used to initialize and manage the MQTT client connection. Cannot be null. The
    /// ClientOptions property of this parameter must also be set.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the ClientOptions property of <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the client has already been started and is currently running.</exception>
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
    /// Asynchronously stops the service and disconnects from the server, optionally performing a clean disconnect.
    /// </summary>
    /// <param name="cleanDisconnect">true to perform a clean disconnect and notify the server before disconnecting; otherwise, false to disconnect
    /// immediately without notification. The default is true.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
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
    /// Asynchronously subscribes to the specified MQTT topic filters.
    /// </summary>
    /// <param name="topicFilters">A collection of <see cref="MqttTopicFilter"/> objects that specify the topics and associated options to
    /// subscribe to. Cannot be null. Each topic filter must specify a valid topic.</param>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
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
    /// Asynchronously unsubscribes from the specified topics.
    /// </summary>
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

    /// <summary>
    /// Calculates the remaining time until the specified end time, returning zero if the end time has already passed.
    /// </summary>
    /// <remarks>The calculation is based on the current UTC time. If <paramref name="endTime"/> is earlier
    /// than the current time, the method returns <see cref="TimeSpan.Zero"/>.</remarks>
    /// <param name="endTime">The end time, expressed as a UTC <see cref="DateTime"/>, for which to calculate the remaining duration.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the time remaining until <paramref name="endTime"/>. Returns <see
    /// cref="TimeSpan.Zero"/> if the end time is in the past.</returns>
    private static TimeSpan GetRemainingTime(in DateTime endTime)
    {
        var remainingTime = endTime - DateTime.UtcNow;
        return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
    }

    /// <summary>
    /// Creates a new CancellationTokenSource that is linked to the specified token and configured to cancel after the
    /// client timeout period.
    /// </summary>
    /// <remarks>The returned CancellationTokenSource will be canceled either when the specified linked token
    /// is canceled or when the client timeout elapses, whichever occurs first.</remarks>
    /// <param name="linkedToken">The CancellationToken to link to the new CancellationTokenSource. Cancellation of this token will also cancel
    /// the returned token source.</param>
    /// <returns>A CancellationTokenSource that is linked to the specified token and will be canceled after the configured
    /// timeout.</returns>
    private CancellationTokenSource NewTimeoutToken(in CancellationToken linkedToken)
    {
        var newTimeoutToken = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        newTimeoutToken.CancelAfter(Options!.ClientOptions!.Timeout);
        return newTimeoutToken;
    }

    /// <summary>
    /// Handles an exception that occurs during the synchronization of MQTT topic subscriptions and notifies registered
    /// event handlers of the failure.
    /// </summary>
    /// <remarks>This method logs the exception and asynchronously invokes any registered handlers for the
    /// subscription synchronization failure event. If no handlers are registered, the method completes without further
    /// action.</remarks>
    /// <param name="exception">The exception that was thrown during the subscription synchronization process. Cannot be null.</param>
    /// <param name="addedSubscriptions">A list of MQTT topic filters that were attempted to be added during synchronization, or null if not applicable.</param>
    /// <param name="removedSubscriptions">A list of topic filter strings that were attempted to be removed during synchronization, or null if not
    /// applicable.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleSubscriptionExceptionAsync(Exception exception, List<MqttTopicFilter>? addedSubscriptions, List<string>? removedSubscriptions)
    {
        _logger.Warning(exception, "Synchronizing subscriptions failed.");

        if (_synchronizingSubscriptionsFailedEvent.HasHandlers)
        {
            await _synchronizingSubscriptionsFailedEvent.InvokeAsync(new ResilientProcessFailedEventArgs(exception, addedSubscriptions, removedSubscriptions)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the result of a subscription or unsubscription operation by raising the subscriptions changed event
    /// asynchronously if there are registered handlers.
    /// </summary>
    /// <param name="subscribeUnsubscribeResult">The result of the subscribe and unsubscribe operations, containing details about which subscriptions were added
    /// or removed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleSubscriptionsResultAsync(SendSubscriptionResults subscribeUnsubscribeResult)
    {
        if (_subscriptionsChangedEvent.HasHandlers)
        {
            await _subscriptionsChangedEvent.InvokeAsync(new SubscriptionsChangedEventArgs(subscribeUnsubscribeResult.SubscribeResults, subscribeUnsubscribeResult.UnsubscribeResults)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Continuously maintains the client connection until cancellation is requested.
    /// </summary>
    /// <remarks>If the connection is disposed or a clean disconnect is requested, the method attempts to
    /// disconnect the client gracefully before completing. Any exceptions encountered during connection maintenance or
    /// disconnection are logged. This method is intended to be run in the background and should not be called directly
    /// by consumers.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to request termination of the connection maintenance loop.</param>
    /// <returns>A task that represents the asynchronous operation of maintaining the connection. The task completes when
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

    /// <summary>
    /// Publishes all messages currently queued for delivery until cancellation is requested or the client disconnects.
    /// </summary>
    /// <remarks>This method continuously attempts to publish messages from the internal queue as long as the
    /// client remains connected and cancellation is not requested. If cancellation is requested or the client
    /// disconnects, the operation stops. Exceptions encountered during publishing are logged, and the method completes
    /// gracefully on cancellation.</remarks>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the publishing operation.</param>
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

    /// <summary>
    /// Publishes all pending subscriptions that need to be re-established after a reconnect operation.
    /// </summary>
    /// <remarks>This method attempts to resubscribe to all topics that were pending at the time of reconnect.
    /// If an error occurs during the process, the exception is handled internally and does not propagate to the
    /// caller.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Publishes all queued subscription and unsubscription requests to the server within the specified timeout period.
    /// </summary>
    /// <remarks>This method processes all queued subscription changes in batches, sending them to the server
    /// until either all are published or the timeout elapses. If the operation is cancelled via the provided token, any
    /// remaining queued subscriptions may not be published.</remarks>
    /// <param name="timeout">The maximum duration to wait for publishing all pending subscriptions and unsubscriptions before the operation
    /// times out.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation before completion.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
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

    /// <summary>
    /// Attempts to re-establish the MQTT client connection if it is not currently connected.
    /// </summary>
    /// <remarks>If the client is already connected, no reconnection is attempted. If the reconnection fails,
    /// a connecting failed event is raised before returning <see cref="ReconnectionResult.NotConnected"/>.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the reconnection attempt.</param>
    /// <returns>A <see cref="ReconnectionResult"/> value indicating the outcome of the reconnection attempt. Returns <see
    /// cref="ReconnectionResult.StillConnected"/> if the client was already connected, <see
    /// cref="ReconnectionResult.Recovered"/> or <see cref="ReconnectionResult.Reconnected"/> if the connection was
    /// successfully established, or <see cref="ReconnectionResult.NotConnected"/> if the reconnection failed.</returns>
    /// <exception cref="MqttCommunicationException">Thrown if the client connects but the server denies the connection.</exception>
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

    /// <summary>
    /// Subscribes to the specified MQTT topic filters and unsubscribes from the specified topics in a single operation.
    /// </summary>
    /// <remarks>If both subscriptions and unsubscriptions are requested, unsubscriptions are performed before
    /// subscriptions. If an exception occurs during the operation, the exception is handled internally and the results
    /// reflect only the successful operations completed before the exception.</remarks>
    /// <param name="addedSubscriptions">A list of MQTT topic filters to subscribe to. If null or empty, no subscriptions are added.</param>
    /// <param name="removedSubscriptions">A list of topic strings to unsubscribe from. If null or empty, no unsubscriptions are performed.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the subscribe or unsubscribe operations.</param>
    /// <returns>A SendSubscriptionResults object containing the results of the subscribe and unsubscribe operations.</returns>
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

    /// <summary>
    /// Begins publishing queued messages in the background.
    /// </summary>
    /// <remarks>If publishing is already in progress, this method restarts the publishing process. This
    /// method is intended for internal use and is not thread-safe; callers should ensure appropriate synchronization if
    /// accessed concurrently.</remarks>
    private void StartPublishing()
    {
        StopPublishing();

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _publishingCancellationToken = cancellationTokenSource;

        Task.Run(() => PublishQueuedMessagesAsync(cancellationToken), cancellationToken).RunInBackground(_logger);
    }

    /// <summary>
    /// Stops maintaining the current connection and releases associated resources.
    /// </summary>
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

    /// <summary>
    /// Stops the current publishing operation and releases associated resources.
    /// </summary>
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

    /// <summary>
    /// Attempts to maintain an active connection to the server, performing reconnection and subscription recovery as
    /// needed.
    /// </summary>
    /// <remarks>This method handles reconnection logic, subscription recovery, and publishing state
    /// transitions based on the current connection status. If the connection state changes, a connection state changed
    /// event is raised. Exceptions related to communication errors are logged, but not rethrown.</remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the connection maintenance operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Attempts to publish a queued MQTT application message asynchronously, handling message interception, publishing,
    /// and queue management.
    /// </summary>
    /// <remarks>If message interception is enabled, the message may be filtered or modified before
    /// publishing. After publishing or if publishing fails, the message is removed from the queue and, if persistent
    /// storage is used, from storage as well. The method notifies subscribers when the message has been processed,
    /// regardless of success or failure.</remarks>
    /// <param name="message">The queued MQTT application message to be published. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
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
