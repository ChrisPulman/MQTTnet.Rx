// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Mocks <see cref="IResilientMqttClient"/> for testing asynchronous observable extensions.</summary>
public sealed class MockResilientMqttClient : IResilientMqttClient
{
    /// <summary>Stores awaited processed-message handlers.</summary>
    private readonly List<Func<ApplicationMessageProcessedEventArgs, CancellationToken, ValueTask>>
        _applicationMessageProcessedHandlers = [];

    /// <summary>Stores awaited received-message handlers.</summary>
    private readonly List<Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, ValueTask>>
        _applicationMessageReceivedHandlers = [];

    /// <summary>Stores awaited skipped-message handlers.</summary>
    private readonly List<Func<ApplicationMessageSkippedEventArgs, CancellationToken, ValueTask>>
        _applicationMessageSkippedHandlers = [];

    /// <summary>Stores awaited connected handlers.</summary>
    private readonly List<Func<MqttClientConnectedEventArgs, CancellationToken, ValueTask>> _connectedHandlers = [];

    /// <summary>Stores awaited connection-failure handlers.</summary>
    private readonly List<Func<ConnectingFailedEventArgs, CancellationToken, ValueTask>> _connectingFailedHandlers = [];

    /// <summary>Stores awaited connection-state handlers.</summary>
    private readonly List<Func<EventArgs, CancellationToken, ValueTask>> _connectionStateChangedHandlers = [];

    /// <summary>Stores awaited disconnected handlers.</summary>
    private readonly List<Func<MqttClientDisconnectedEventArgs, CancellationToken, ValueTask>>
        _disconnectedHandlers = [];

    /// <summary>Stores awaited subscription-synchronization failure handlers.</summary>
    private readonly List<Func<ResilientProcessFailedEventArgs, CancellationToken, ValueTask>>
        _synchronizingSubscriptionsFailedHandlers = [];

    /// <summary>Stores awaited subscription-change handlers.</summary>
    private readonly List<Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask>>
        _subscriptionsChangedHandlers = [];

    /// <summary>Initializes a new instance of the <see cref="MockResilientMqttClient"/> class.</summary>
    public MockResilientMqttClient()
    {
        ApplicationMessageProcessed = CreateObservable<ApplicationMessageProcessedEventArgs>(
            handler => ApplicationMessageProcessedEvent += handler,
            handler => ApplicationMessageProcessedEvent -= handler);
        ApplicationMessageProcessedAsyncObservable = ApplicationMessageProcessed.ToSignal();
        ApplicationMessageReceived = CreateObservable<MqttApplicationMessageReceivedEventArgs>(
            handler => ApplicationMessageReceivedEvent += handler,
            handler => ApplicationMessageReceivedEvent -= handler);
        ApplicationMessageReceivedAsyncObservable = ApplicationMessageReceived.ToSignal();
        ApplicationMessageSkipped = CreateObservable<ApplicationMessageSkippedEventArgs>(
            handler => ApplicationMessageSkippedEvent += handler,
            handler => ApplicationMessageSkippedEvent -= handler);
        ApplicationMessageSkippedAsyncObservable = ApplicationMessageSkipped.ToSignal();
        Connected = CreateObservable<MqttClientConnectedEventArgs>(
            handler => ConnectedEvent += handler,
            handler => ConnectedEvent -= handler);
        ConnectedAsyncObservable = Connected.ToSignal();
        ConnectingFailed = CreateObservable<ConnectingFailedEventArgs>(
            handler => ConnectingFailedEvent += handler,
            handler => ConnectingFailedEvent -= handler);
        ConnectingFailedAsyncObservable = ConnectingFailed.ToSignal();
        ConnectionStateChanged = CreateObservable<EventArgs>(
            handler => ConnectionStateChangedEvent += handler,
            handler => ConnectionStateChangedEvent -= handler);
        ConnectionStateChangedAsyncObservable = ConnectionStateChanged.ToSignal();
        Disconnected = CreateObservable<MqttClientDisconnectedEventArgs>(
            handler => DisconnectedEvent += handler,
            handler => DisconnectedEvent -= handler);
        DisconnectedAsyncObservable = Disconnected.ToSignal();
        SynchronizingSubscriptionsFailed = CreateObservable<ResilientProcessFailedEventArgs>(
            handler => SynchronizingSubscriptionsFailedEvent += handler,
            handler => SynchronizingSubscriptionsFailedEvent -= handler);
        SynchronizingSubscriptionsFailedAsyncObservable = SynchronizingSubscriptionsFailed.ToSignal();
    }

    /// <inheritdoc/>
    public event EventHandler<ApplicationMessageProcessedEventArgs>? ApplicationMessageProcessedEvent;

    /// <inheritdoc/>
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? ApplicationMessageReceivedEvent;

    /// <inheritdoc/>
    public event EventHandler<ApplicationMessageSkippedEventArgs>? ApplicationMessageSkippedEvent;

    /// <inheritdoc/>
    public event EventHandler<MqttClientConnectedEventArgs>? ConnectedEvent;

    /// <inheritdoc/>
    public event EventHandler<ConnectingFailedEventArgs>? ConnectingFailedEvent;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? ConnectionStateChangedEvent;

    /// <inheritdoc/>
    public event EventHandler<MqttClientDisconnectedEventArgs>? DisconnectedEvent;

    /// <inheritdoc/>
    public event EventHandler<ResilientProcessFailedEventArgs>? SynchronizingSubscriptionsFailedEvent;

    /// <inheritdoc/>
    public event EventHandler<SubscriptionsChangedEventArgs>? SubscriptionsChangedEvent;

    /// <inheritdoc/>
    public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttClientConnectedEventArgs> Connected { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttClientConnectedEventArgs> ConnectedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttClientDisconnectedEventArgs> Disconnected { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttClientDisconnectedEventArgs> DisconnectedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ConnectingFailedEventArgs> ConnectingFailed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ConnectingFailedEventArgs> ConnectingFailedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<EventArgs> ConnectionStateChanged { get; }

    /// <inheritdoc/>
    public IObservableAsync<EventArgs> ConnectionStateChangedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped { get; }

    /// <inheritdoc/>
    public IObservableAsync<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedAsyncObservable { get; }

    /// <inheritdoc/>
    public IMqttClient InternalClient { get; } = new MockMqttClient();

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public bool IsStarted { get; private set; }

    /// <inheritdoc/>
    public ResilientMqttClientOptions? Options { get; private set; }

    /// <inheritdoc/>
    public int PendingApplicationMessagesCount => 0;

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageProcessedHandler(
        Func<ApplicationMessageProcessedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageProcessedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageReceivedHandler(
        Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageReceivedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageSkippedHandler(
        Func<ApplicationMessageSkippedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageSkippedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectedHandler(
        Func<MqttClientConnectedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectingFailedHandler(
        Func<ConnectingFailedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectingFailedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectionStateChangedHandler(Func<EventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectionStateChangedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterDisconnectedHandler(
        Func<MqttClientDisconnectedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_disconnectedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterSynchronizingSubscriptionsFailedHandler(
        Func<ResilientProcessFailedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_synchronizingSubscriptionsFailedHandlers, handler);

    /// <inheritdoc/>
    public IDisposable RegisterSubscriptionsChangedHandler(
        Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_subscriptionsChangedHandlers, handler);

    /// <inheritdoc/>
    public Task EnqueueAsync(MqttApplicationMessage applicationMessage) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(ResilientMqttClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
        IsStarted = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(bool cleanDisconnect = true)
    {
        IsStarted = false;
        IsConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task UnsubscribeAsync(IEnumerable<string> topics) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.KeepAlive(ApplicationMessageProcessedEvent);
        GC.KeepAlive(ApplicationMessageReceivedEvent);
        GC.KeepAlive(ApplicationMessageSkippedEvent);
        GC.KeepAlive(ConnectedEvent);
        GC.KeepAlive(ConnectingFailedEvent);
        GC.KeepAlive(ConnectionStateChangedEvent);
        GC.KeepAlive(DisconnectedEvent);
        GC.KeepAlive(SynchronizingSubscriptionsFailedEvent);
        GC.KeepAlive(SubscriptionsChangedEvent);
        ApplicationMessageProcessedEvent = null;
        ApplicationMessageReceivedEvent = null;
        ApplicationMessageSkippedEvent = null;
        ConnectedEvent = null;
        ConnectingFailedEvent = null;
        ConnectionStateChangedEvent = null;
        DisconnectedEvent = null;
        SynchronizingSubscriptionsFailedEvent = null;
        SubscriptionsChangedEvent = null;
    }

    /// <summary>Raises the connected event.</summary>
    /// <returns>A task that completes when event handlers have run.</returns>
    public async Task SimulateConnectedAsync()
    {
        IsConnected = true;
        var args = new MqttClientConnectedEventArgs(new());
        ConnectedEvent?.Invoke(this, args);
        await InvokeHandlersAsync(_connectedHandlers, args).ConfigureAwait(false);
    }

    /// <summary>Raises the disconnected event.</summary>
    /// <returns>A task that completes when event handlers have run.</returns>
    public async Task SimulateDisconnectedAsync()
    {
        IsConnected = false;
        var args = new MqttClientDisconnectedEventArgs(
            clientWasConnected: true,
            connectResult: null,
            reason: MqttClientDisconnectReason.NormalDisconnection,
            reasonString: "Test disconnection",
            userProperties: null,
            exception: null);
        DisconnectedEvent?.Invoke(this, args);
        await InvokeHandlersAsync(_disconnectedHandlers, args).ConfigureAwait(false);
    }

    /// <summary>Raises the application message processed event.</summary>
    /// <returns>A task that completes when event handlers have run.</returns>
    public async Task SimulateApplicationMessageProcessedAsync()
    {
        var args = new ApplicationMessageProcessedEventArgs(
            new ResilientMqttApplicationMessage
            {
                ApplicationMessage = new MqttApplicationMessage { Topic = "processed/topic" },
            },
            null);

        ApplicationMessageProcessedEvent?.Invoke(this, args);
        await InvokeHandlersAsync(_applicationMessageProcessedHandlers, args).ConfigureAwait(false);
    }

    /// <summary>Raises the application message received event.</summary>
    /// <param name="topic">The received MQTT topic.</param>
    /// <param name="payload">The received MQTT payload.</param>
    /// <returns>A task that completes when event handlers have run.</returns>
    public async Task SimulateMessageReceivedAsync(string topic, string payload)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);

        var args = TestDataHelpers.CreateMessageReceivedArgs(topic, payload);
        ApplicationMessageReceivedEvent?.Invoke(this, args);
        await InvokeHandlersAsync(_applicationMessageReceivedHandlers, args).ConfigureAwait(false);
    }

    /// <summary>Registers an awaited handler in the supplied handler collection.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="handlers">The handler collection.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    private static HandlerRegistration<T> RegisterHandler<T>(
        List<Func<T, CancellationToken, ValueTask>> handlers,
        Func<T, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new(handlers, handler);
    }

    /// <summary>Invokes a stable snapshot of awaited handlers.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="handlers">The handlers to invoke.</param>
    /// <param name="args">The event arguments.</param>
    /// <returns>A task that completes after every handler completes.</returns>
    private static async ValueTask InvokeHandlersAsync<T>(List<Func<T, CancellationToken, ValueTask>> handlers, T args)
    {
        Func<T, CancellationToken, ValueTask>[] snapshot;
        lock (handlers)
        {
            snapshot = [.. handlers];
        }

        foreach (var handler in snapshot)
        {
            await handler(args, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Creates an observable backed by a standard event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="addHandler">Adds an event handler.</param>
    /// <param name="removeHandler">Removes an event handler.</param>
    /// <returns>An observable that forwards the event notifications.</returns>
    private static EventObservable<T> CreateObservable<T>(
        Action<EventHandler<T>> addHandler,
        Action<EventHandler<T>> removeHandler)
        where T : EventArgs =>
        new(addHandler, removeHandler);

    /// <summary>Represents an observable backed by an asynchronous event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    private sealed class EventObservable<T> : IObservable<T>
        where T : EventArgs
    {
        /// <summary>Adds an asynchronous event handler.</summary>
        private readonly Action<EventHandler<T>> _addHandler;

        /// <summary>Removes an asynchronous event handler.</summary>
        private readonly Action<EventHandler<T>> _removeHandler;

        /// <summary>Guards the subscribed observers.</summary>
        private readonly SynchronizationLock _gate = new();

        /// <summary>Stores the subscribed observers.</summary>
        private readonly List<IObserver<T>> _observers = [];

        /// <summary>Stores the handler registered with the asynchronous event.</summary>
        private readonly EventHandler<T> _handler;

        /// <summary>Initializes a new instance of the <see cref="EventObservable{T}"/> class.</summary>
        /// <param name="addHandler">Adds an asynchronous event handler.</param>
        /// <param name="removeHandler">Removes an asynchronous event handler.</param>
        public EventObservable(Action<EventHandler<T>> addHandler, Action<EventHandler<T>> removeHandler)
        {
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _handler = Handle;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            lock (_gate)
            {
                if (_observers.Count == 0)
                {
                    _addHandler(_handler);
                }

                _observers.Add(observer);
                return new EventSubscription<T>(this, observer);
            }
        }

        /// <summary>Removes an observer and disconnects the event when none remain.</summary>
        /// <param name="observer">The observer to remove.</param>
        public void Unsubscribe(IObserver<T> observer)
        {
            lock (_gate)
            {
                _ = _observers.Remove(observer);
                if (_observers.Count == 0)
                {
                    _removeHandler(_handler);
                }
            }
        }

        /// <summary>Forwards an event argument to every subscribed observer.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event argument to forward.</param>
        private void Handle(object? sender, T args)
        {
            GC.KeepAlive(sender);
            IObserver<T>[] observers;
            lock (_gate)
            {
                observers = [.. _observers];
            }

            foreach (var observer in observers)
            {
                observer.OnNext(args);
            }
        }
    }

    /// <summary>Removes an awaited handler registration.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="handlers">The collection that owns the handler.</param>
    /// <param name="handler">The registered handler.</param>
    private sealed class HandlerRegistration<T>(
        List<Func<T, CancellationToken, ValueTask>> handlers,
        Func<T, CancellationToken, ValueTask> handler) : IDisposable
    {
        /// <summary>Indicates whether this registration has been disposed.</summary>
        private bool _isDisposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            lock (handlers)
            {
                _ = handlers.Remove(handler);
            }
        }
    }

    /// <summary>Provides a dedicated monitor-compatible synchronization lock.</summary>
    private sealed class SynchronizationLock
    {
        /// <inheritdoc/>
        public override string ToString() => nameof(SynchronizationLock);
    }

    /// <summary>Represents a subscription to an asynchronous event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="source">The event observable that owns the subscription.</param>
    /// <param name="observer">The observer receiving event notifications.</param>
    private sealed class EventSubscription<T>(EventObservable<T> source, IObserver<T> observer) : IDisposable
        where T : EventArgs
    {
        /// <summary>Stores the event observable that owns the subscription.</summary>
        private readonly EventObservable<T> _source = source;

        /// <summary>Stores the observer receiving event notifications.</summary>
        private readonly IObserver<T> _observer = observer;

        /// <summary>Indicates whether the subscription has been disposed.</summary>
        private bool _isDisposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _source.Unsubscribe(_observer);
        }
    }
}
