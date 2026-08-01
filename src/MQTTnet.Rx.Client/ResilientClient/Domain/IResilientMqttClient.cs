// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Defines a resilient MQTT client with queued delivery and automatic reconnection.</summary>
/// <remarks>This interface extends <see cref="IDisposable"/> and exposes both event-based and observable patterns
/// for monitoring client state and message flow. It is designed for scenarios where robust MQTT connectivity and
/// message delivery are required, including automatic reconnection and message queuing. Implementations are expected to
/// handle transient failures and maintain reliable operation in unstable network environments.</remarks>
public interface IResilientMqttClient : IDisposable
{
    /// <summary>Occurs when an application message has been processed.</summary>
    event EventHandler<ApplicationMessageProcessedEventArgs>? ApplicationMessageProcessedEvent;

    /// <summary>Occurs when an application message is received.</summary>
    event EventHandler<MqttApplicationMessageReceivedEventArgs>? ApplicationMessageReceivedEvent;

    /// <summary>Occurs when an application message is skipped.</summary>
    event EventHandler<ApplicationMessageSkippedEventArgs>? ApplicationMessageSkippedEvent;

    /// <summary>Occurs when the client connects.</summary>
    event EventHandler<MqttClientConnectedEventArgs>? ConnectedEvent;

    /// <summary>Occurs when a connection attempt fails.</summary>
    event EventHandler<ConnectingFailedEventArgs>? ConnectingFailedEvent;

    /// <summary>Occurs when the connection state changes.</summary>
    event EventHandler<EventArgs>? ConnectionStateChangedEvent;

    /// <summary>Occurs when the client disconnects.</summary>
    event EventHandler<MqttClientDisconnectedEventArgs>? DisconnectedEvent;

    /// <summary>Occurs when subscription synchronization fails.</summary>
    event EventHandler<ResilientProcessFailedEventArgs>? SynchronizingSubscriptionsFailedEvent;

    /// <summary>Occurs when the subscription set changes.</summary>
    event EventHandler<SubscriptionsChangedEventArgs>? SubscriptionsChangedEvent;

    /// <summary>Gets application messages processed.</summary>
    /// <returns>A Application Message Processed Event Args.</returns>
    IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed { get; }

    /// <summary>Gets application messages processed as an asynchronous observable sequence.</summary>
    IObservableAsync<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedAsyncObservable { get; }

    /// <summary>Gets connected to the specified client.</summary>
    /// <returns>A Mqtt Client Connected Event Args.</returns>
    IObservable<MqttClientConnectedEventArgs> Connected { get; }

    /// <summary>Gets connected notifications as an asynchronous observable sequence.</summary>
    IObservableAsync<MqttClientConnectedEventArgs> ConnectedAsyncObservable { get; }

    /// <summary>Gets disconnected from the specified client.</summary>
    /// <returns>A Mqtt Client Disconnected Event Args.</returns>
    IObservable<MqttClientDisconnectedEventArgs> Disconnected { get; }

    /// <summary>Gets disconnected notifications as an asynchronous observable sequence.</summary>
    IObservableAsync<MqttClientDisconnectedEventArgs> DisconnectedAsyncObservable { get; }

    /// <summary>Gets connecting failed.</summary>
    /// <returns>A Connecting Failed Event Args.</returns>
    IObservable<ConnectingFailedEventArgs> ConnectingFailed { get; }

    /// <summary>Gets connection failures as an asynchronous observable sequence.</summary>
    IObservableAsync<ConnectingFailedEventArgs> ConnectingFailedAsyncObservable { get; }

    /// <summary>Gets connection state changed.</summary>
    /// <returns>Event Args.</returns>
    IObservable<EventArgs> ConnectionStateChanged { get; }

    /// <summary>Gets connection state changes as an asynchronous observable sequence.</summary>
    IObservableAsync<EventArgs> ConnectionStateChangedAsyncObservable { get; }

    /// <summary>Gets synchronizing subscriptions failed.</summary>
    /// <returns>A Resilient Process Failed Event Args.</returns>
    IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }

    /// <summary>Gets subscription synchronization failures as an asynchronous observable sequence.</summary>
    IObservableAsync<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailedAsyncObservable { get; }

    /// <summary>Gets application messages processed.</summary>
    /// <returns>A Application Message Skipped Event Args.</returns>
    IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped { get; }

    /// <summary>Gets skipped application messages as an asynchronous observable sequence.</summary>
    IObservableAsync<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedAsyncObservable { get; }

    /// <summary>Gets application messages received.</summary>
    /// <returns>A Mqtt Application Message Received Event Args.</returns>
    IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; }

    /// <summary>Gets received application messages as an asynchronous observable sequence.</summary>
    IObservableAsync<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedAsyncObservable { get; }

    /// <summary>Gets the internal client.</summary>
    /// <value>
    /// The internal client.
    /// </value>
    IMqttClient InternalClient { get; }

    /// <summary>Gets a value indicating whether this instance is connected.</summary>
    /// <value>
    ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
    /// </value>
    bool IsConnected { get; }

    /// <summary>Gets a value indicating whether this instance is started.</summary>
    /// <value>
    ///   <c>true</c> if this instance is started; otherwise, <c>false</c>.
    /// </value>
    bool IsStarted { get; }

    /// <summary>Gets the options.</summary>
    /// <value>
    /// The options.
    /// </value>
    ResilientMqttClientOptions? Options { get; }

    /// <summary>Gets the pending application messages count.</summary>
    /// <value>
    /// The pending application messages count.
    /// </value>
    int PendingApplicationMessagesCount { get; }

    /// <summary>Registers an awaited handler for processed application messages.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterApplicationMessageProcessedHandler(
        Func<ApplicationMessageProcessedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for received application messages.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterApplicationMessageReceivedHandler(
        Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for skipped application messages.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterApplicationMessageSkippedHandler(
        Func<ApplicationMessageSkippedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for successful connections.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterConnectedHandler(
        Func<MqttClientConnectedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for failed connection attempts.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterConnectingFailedHandler(
        Func<ConnectingFailedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for connection-state changes.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterConnectionStateChangedHandler(
        Func<EventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for disconnections.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterDisconnectedHandler(
        Func<MqttClientDisconnectedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for subscription-synchronization failures.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterSynchronizingSubscriptionsFailedHandler(
        Func<ResilientProcessFailedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Registers an awaited handler for subscription changes.</summary>
    /// <param name="handler">The handler to await for each notification.</param>
    /// <returns>A registration that removes the handler when disposed.</returns>
    IDisposable RegisterSubscriptionsChangedHandler(
        Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask> handler);

    /// <summary>Enqueues the asynchronous.</summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EnqueueAsync(MqttApplicationMessage applicationMessage);

    /// <summary>Enqueues the asynchronous.</summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage);

    /// <summary>Pings asynchronously without cancellation.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PingAsync() => PingAsync(default);

    /// <summary>Pings the asynchronous.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PingAsync(CancellationToken cancellationToken);

    /// <summary>Starts the asynchronous.</summary>
    /// <param name="options">The options.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync(ResilientMqttClientOptions options);

    /// <summary>Stops asynchronously using a clean disconnect.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync() => StopAsync(true);

    /// <summary>Stops the asynchronous.</summary>
    /// <param name="cleanDisconnect">if set to <c>true</c> [clean disconnect].</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync(bool cleanDisconnect);

    /// <summary>Subscribes the asynchronous.</summary>
    /// <param name="topicFilters">The topic filters.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters);

    /// <summary>Unsubscribes the asynchronous.</summary>
    /// <param name="topics">The topics.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(IEnumerable<string> topics);
}
