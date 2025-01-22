// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Packets;

namespace MQTTnet.Rx.Client;

/// <summary>
/// IResilientMqttClient.
/// </summary>
/// <seealso cref="IDisposable" />
public interface IResilientMqttClient : IDisposable
{
    /// <summary>
    /// Occurs when [application message processed asynchronous].
    /// </summary>
    event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync;

    /// <summary>
    /// Occurs when [application message received asynchronous].
    /// </summary>
    event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;

    /// <summary>
    /// Occurs when [application message skipped asynchronous].
    /// </summary>
    event Func<ApplicationMessageSkippedEventArgs, Task> ApplicationMessageSkippedAsync;

    /// <summary>
    /// Occurs when [connected asynchronous].
    /// </summary>
    event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;

    /// <summary>
    /// Occurs when [connecting failed asynchronous].
    /// </summary>
    event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync;

    /// <summary>
    /// Occurs when [connection state changed asynchronous].
    /// </summary>
    event Func<EventArgs, Task> ConnectionStateChangedAsync;

    /// <summary>
    /// Occurs when [disconnected asynchronous].
    /// </summary>
    event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;

    /// <summary>
    /// Occurs when [synchronizing subscriptions failed asynchronous].
    /// </summary>
    event Func<ResilientProcessFailedEventArgs, Task> SynchronizingSubscriptionsFailedAsync;

    /// <summary>
    /// Occurs when [subscriptions changed asynchronous].
    /// </summary>
    event Func<SubscriptionsChangedEventArgs, Task> SubscriptionsChangedAsync;

    /// <summary>
    /// Gets application messages processed.
    /// </summary>
    /// <returns>A Application Message Processed Event Args.</returns>
    IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed { get; }

    /// <summary>
    /// Gets connected to the specified client.
    /// </summary>
    /// <returns>A Mqtt Client Connected Event Args.</returns>
    IObservable<MqttClientConnectedEventArgs> Connected { get; }

    /// <summary>
    /// Gets disconnected from the specified client.
    /// </summary>
    /// <returns>A Mqtt Client Disconnected Event Args.</returns>
    IObservable<MqttClientDisconnectedEventArgs> Disconnected { get; }

    /// <summary>
    /// Gets connecting failed.
    /// </summary>
    /// <returns>A Connecting Failed Event Args.</returns>
    IObservable<ConnectingFailedEventArgs> ConnectingFailed { get; }

    /// <summary>
    /// Gets connection state changed.
    /// </summary>
    /// <returns>Event Args.</returns>
    IObservable<EventArgs> ConnectionStateChanged { get; }

    /// <summary>
    /// Gets synchronizing subscriptions failed.
    /// </summary>
    /// <returns>A Resilient Process Failed Event Args.</returns>
    IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }

    /// <summary>
    /// Gets application messages processed.
    /// </summary>
    /// <returns>A Application Message Skipped Event Args.</returns>
    IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped { get; }

    /// <summary>
    /// Gets application messages received.
    /// </summary>
    /// <returns>A Mqtt Application Message Received Event Args.</returns>
    IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; }

    /// <summary>
    /// Gets the internal client.
    /// </summary>
    /// <value>
    /// The internal client.
    /// </value>
    IMqttClient InternalClient { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is connected.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
    /// </value>
    bool IsConnected { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is started.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is started; otherwise, <c>false</c>.
    /// </value>
    bool IsStarted { get; }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <value>
    /// The options.
    /// </value>
    ResilientMqttClientOptions? Options { get; }

    /// <summary>
    /// Gets the pending application messages count.
    /// </summary>
    /// <value>
    /// The pending application messages count.
    /// </value>
    int PendingApplicationMessagesCount { get; }

    /// <summary>
    /// Enqueues the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EnqueueAsync(MqttApplicationMessage applicationMessage);

    /// <summary>
    /// Enqueues the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage);

    /// <summary>
    /// Pings the asynchronous.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the asynchronous.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StartAsync(ResilientMqttClientOptions options);

    /// <summary>
    /// Stops the asynchronous.
    /// </summary>
    /// <param name="cleanDisconnect">if set to <c>true</c> [clean disconnect].</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync(bool cleanDisconnect = true);

    /// <summary>
    /// Subscribes the asynchronous.
    /// </summary>
    /// <param name="topicFilters">The topic filters.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters);

    /// <summary>
    /// Unsubscribes the asynchronous.
    /// </summary>
    /// <param name="topics">The topics.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(IEnumerable<string> topics);
}
