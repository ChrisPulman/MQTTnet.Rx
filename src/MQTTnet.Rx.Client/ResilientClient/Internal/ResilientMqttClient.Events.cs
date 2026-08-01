// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Packets;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive.ResilientClient.Internal;
#else
namespace MQTTnet.Rx.Client.ResilientClient.Internal;
#endif

/// <summary>Contains event-facing members of the resilient MQTT client.</summary>
internal sealed partial class ResilientMqttClient
{
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

    /// <summary>Gets an observable sequence that signals when an application message has been processed.</summary>
    /// <remarks>Subscribers to this observable are notified each time an application message is processed.
    /// The sequence completes when the underlying event source is disposed or no longer available.</remarks>
    public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed =>
        CreateObservable.FromEvent<ApplicationMessageProcessedEventArgs>(
            handler => ApplicationMessageProcessedEvent += handler,
            handler => ApplicationMessageProcessedEvent -= handler);

    /// <summary>Gets processed-message notifications as an asynchronous observable sequence.</summary>
    public IObservableAsync<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<ApplicationMessageProcessedEventArgs>(
            RegisterApplicationMessageProcessedHandler);

    /// <summary>Gets an observable sequence that signals successful connections.</summary>
    /// <remarks>Subscribers receive a notification each time a connection to the broker is established. The
    /// observable emits a value for every successful connection event, including initial and subsequent
    /// reconnects.</remarks>
    public IObservable<MqttClientConnectedEventArgs> Connected =>
        CreateObservable.FromEvent<MqttClientConnectedEventArgs>(
            handler => ConnectedEvent += handler,
            handler => ConnectedEvent -= handler);

    /// <summary>Gets successful-connection notifications as an asynchronous observable sequence.</summary>
    public IObservableAsync<MqttClientConnectedEventArgs> ConnectedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<MqttClientConnectedEventArgs>(
            RegisterConnectedHandler);

    /// <summary>Gets an observable sequence that signals disconnections.</summary>
    /// <remarks>Subscribers receive a notification each time the client disconnects, along with details about
    /// the disconnection event. The sequence completes when the underlying client is disposed.</remarks>
    public IObservable<MqttClientDisconnectedEventArgs> Disconnected =>
        CreateObservable.FromEvent<MqttClientDisconnectedEventArgs>(
            handler => DisconnectedEvent += handler,
            handler => DisconnectedEvent -= handler);

    /// <summary>Gets disconnection notifications as an asynchronous observable sequence.</summary>
    public IObservableAsync<MqttClientDisconnectedEventArgs> DisconnectedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<MqttClientDisconnectedEventArgs>(
            RegisterDisconnectedHandler);

    /// <summary>Gets an observable sequence that signals when a connection attempt fails.</summary>
    /// <remarks>Subscribers are notified each time a connection attempt does not succeed. The observable
    /// emits a value containing details about the failure. This can be used to implement custom error handling or retry
    /// logic in response to connection failures.</remarks>
    public IObservable<ConnectingFailedEventArgs> ConnectingFailed =>
        CreateObservable.FromEvent<ConnectingFailedEventArgs>(
            handler => ConnectingFailedEvent += handler,
            handler => ConnectingFailedEvent -= handler);

    /// <summary>Gets an asynchronous observable sequence that signals when a connection attempt fails.</summary>
    public IObservableAsync<ConnectingFailedEventArgs> ConnectingFailedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<ConnectingFailedEventArgs>(
            RegisterConnectingFailedHandler);

    /// <summary>Gets an observable sequence that signals when the connection state changes.</summary>
    /// <remarks>Subscribers are notified each time the connection state transitions, such as when connecting
    /// or disconnecting. The sequence completes when the underlying object is disposed, if applicable.</remarks>
    public IObservable<EventArgs> ConnectionStateChanged =>
        CreateObservable.FromEvent<EventArgs>(
            handler => ConnectionStateChangedEvent += handler,
            handler => ConnectionStateChangedEvent -= handler);

    /// <summary>Gets an asynchronous observable sequence that signals when the connection state changes.</summary>
    public IObservableAsync<EventArgs> ConnectionStateChangedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<EventArgs>(RegisterConnectionStateChangedHandler);

    /// <summary>Gets an observable sequence that signals subscription synchronization failures.</summary>
    public IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed =>
        CreateObservable.FromEvent<ResilientProcessFailedEventArgs>(
            handler => SynchronizingSubscriptionsFailedEvent += handler,
            handler => SynchronizingSubscriptionsFailedEvent -= handler);

    /// <summary>Gets subscription synchronization failures as an asynchronous observable sequence.</summary>
    public IObservableAsync<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<ResilientProcessFailedEventArgs>(
            RegisterSynchronizingSubscriptionsFailedHandler);

    /// <summary>Gets an observable sequence that signals skipped application messages.</summary>
    /// <remarks>Subscribers receive a notification each time an application message is not processed and is
    /// skipped. This can be used to monitor or log skipped messages for diagnostic or auditing purposes.</remarks>
    public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped =>
        CreateObservable.FromEvent<ApplicationMessageSkippedEventArgs>(
            handler => ApplicationMessageSkippedEvent += handler,
            handler => ApplicationMessageSkippedEvent -= handler);

    /// <summary>Gets an asynchronous observable sequence that signals when an application message is skipped.</summary>
    public IObservableAsync<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<ApplicationMessageSkippedEventArgs>(
            RegisterApplicationMessageSkippedHandler);

    /// <summary>Gets an observable sequence that signals received application messages.</summary>
    /// <remarks>Subscribers to this observable are notified each time an application message is received. The
    /// sequence completes when the underlying client is disposed or disconnected. This property enables reactive
    /// handling of incoming MQTT messages using the observer pattern.</remarks>
    public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived =>
        CreateObservable.FromEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => ApplicationMessageReceivedEvent += handler,
            handler => ApplicationMessageReceivedEvent -= handler);

    /// <summary>Gets received-message notifications as an asynchronous observable sequence.</summary>
    public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedAsyncObservable =>
        CreateObservable.FromHandlerRegistration<MqttApplicationMessageReceivedEventArgs>(
            RegisterApplicationMessageReceivedHandler);

    /// <summary>Gets the underlying MQTT client instance used for low-level operations.</summary>
    /// <remarks>This property exposes the internal client for advanced scenarios where direct access to the
    /// MQTT protocol features is required. Modifying or interacting with the internal client may affect the overall
    /// connection state and should be done with caution.</remarks>
    public IMqttClient InternalClient { get; }

    /// <summary>Gets a value indicating whether the client is currently connected.</summary>
    public bool IsConnected => InternalClient.IsConnected;

    /// <summary>Gets a value indicating whether the connection has been started.</summary>
    public bool IsStarted => _connectionCancellationToken is not null;

    /// <summary>Gets the options used to configure the resilient MQTT client.</summary>
    public ResilientMqttClientOptions? Options { get; private set; }

    /// <summary>Gets the number of messages pending publication.</summary>
    public int PendingApplicationMessagesCount => _messageQueue.Count;

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageProcessedHandler(
        Func<ApplicationMessageProcessedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageProcessedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageReceivedHandler(
        Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageReceivedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterApplicationMessageSkippedHandler(
        Func<ApplicationMessageSkippedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_applicationMessageSkippedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectedHandler(
        Func<MqttClientConnectedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectingFailedHandler(
        Func<ConnectingFailedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectingFailedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterConnectionStateChangedHandler(
        Func<EventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_connectionStateChangedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterDisconnectedHandler(
        Func<MqttClientDisconnectedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_disconnectedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterSynchronizingSubscriptionsFailedHandler(
        Func<ResilientProcessFailedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_synchronizingSubscriptionsFailedEvent, handler);

    /// <inheritdoc/>
    public IDisposable RegisterSubscriptionsChangedHandler(
        Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask> handler) =>
        RegisterHandler(_subscriptionsChangedEvent, handler);

    /// <summary>Registers an awaited event handler and returns its removal registration.</summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <param name="eventSource">The awaited event coordinator.</param>
    /// <param name="handler">The typed handler to invoke.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    private static ActionDisposable RegisterHandler<T>(
        AsyncEvent<T> eventSource,
        Func<T, CancellationToken, ValueTask> handler)
        where T : EventArgs
    {
        ArgumentNullException.ThrowIfNull(handler);

        Task AwaitedHandler(T args) => handler(args, CancellationToken.None).AsTask();

        eventSource.AddHandler(AwaitedHandler);
        return new(() => eventSource.RemoveHandler(AwaitedHandler));
    }

    /// <summary>Forwards a received-message notification to standard and awaited handlers.</summary>
    /// <param name="args">The received-message details.</param>
    /// <returns>A task that completes after all awaited handlers complete.</returns>
    private async Task HandleApplicationMessageReceivedAsync(
        MqttApplicationMessageReceivedEventArgs args)
    {
        ApplicationMessageReceivedEvent?.Invoke(this, args);
        if (!_applicationMessageReceivedEvent.HasHandlers)
        {
            return;
        }

        await _applicationMessageReceivedEvent.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>Forwards a connected notification to standard and awaited handlers.</summary>
    /// <param name="args">The connection details.</param>
    /// <returns>A task that completes after all awaited handlers complete.</returns>
    private async Task HandleConnectedAsync(MqttClientConnectedEventArgs args)
    {
        ConnectedEvent?.Invoke(this, args);
        if (!_connectedEvent.HasHandlers)
        {
            return;
        }

        await _connectedEvent.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>Forwards a disconnected notification to standard and awaited handlers.</summary>
    /// <param name="args">The disconnection details.</param>
    /// <returns>A task that completes after all awaited handlers complete.</returns>
    private async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        DisconnectedEvent?.Invoke(this, args);
        if (!_disconnectedEvent.HasHandlers)
        {
            return;
        }

        await _disconnectedEvent.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>Creates a timeout token linked to the specified token.</summary>
    /// <param name="linkedToken">The cancellation token to link to the new token source.</param>
    /// <returns>A timeout token source linked to <paramref name="linkedToken"/>.</returns>
    private CancellationTokenSource NewTimeoutToken(in CancellationToken linkedToken)
    {
        var newTimeoutToken = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        newTimeoutToken.CancelAfter(Options!.ClientOptions!.Timeout);
        return newTimeoutToken;
    }

    /// <summary>Handles an exception raised while synchronizing subscriptions.</summary>
    /// <param name="exception">The exception that was thrown during synchronization.</param>
    /// <param name="addedSubscriptions">The subscriptions that were added, if any.</param>
    /// <param name="removedSubscriptions">The subscriptions that were removed, if any.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task HandleSubscriptionExceptionAsync(
        Exception exception,
        List<MqttTopicFilter>? addedSubscriptions,
        List<string>? removedSubscriptions)
    {
        _logger.Warning(exception, "Synchronizing subscriptions failed.");

        var eventArgs = new ResilientProcessFailedEventArgs(
            exception,
            addedSubscriptions,
            removedSubscriptions);
        SynchronizingSubscriptionsFailedEvent?.Invoke(this, eventArgs);
        if (!_synchronizingSubscriptionsFailedEvent.HasHandlers)
        {
            return;
        }

        await _synchronizingSubscriptionsFailedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
    }

    /// <summary>Publishes the results of subscription synchronization.</summary>
    /// <param name="subscribeUnsubscribeResult">The result of the subscription operations.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task HandleSubscriptionsResultAsync(
        SendSubscriptionResults subscribeUnsubscribeResult)
    {
        var eventArgs = new SubscriptionsChangedEventArgs(
            subscribeUnsubscribeResult.SubscribeResults,
            subscribeUnsubscribeResult.UnsubscribeResults);
        SubscriptionsChangedEvent?.Invoke(this, eventArgs);
        if (!_subscriptionsChangedEvent.HasHandlers)
        {
            return;
        }

        await _subscriptionsChangedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
    }
}
