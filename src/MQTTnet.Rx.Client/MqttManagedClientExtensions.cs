// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqtt Managed Client Extensions.
/// </summary>
public static class MqttManagedClientExtensions
{
    /// <summary>
    /// Application messages processed.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Application Message Processed Event Args.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<ApplicationMessageProcessedEventArgs>(
            handler => client.ApplicationMessageProcessedAsync += handler,
            handler => client.ApplicationMessageProcessedAsync -= handler);

    /// <summary>
    /// Connected to the specified client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Client Connected Event Args.</returns>
    public static IObservable<MqttClientConnectedEventArgs> Connected(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
            handler => client.ConnectedAsync += handler,
            handler => client.ConnectedAsync -= handler);

    /// <summary>
    /// Disconnected from the specified client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Client Disconnected Event Args.</returns>
    public static IObservable<MqttClientDisconnectedEventArgs> Disconnected(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
            handler => client.DisconnectedAsync += handler,
            handler => client.DisconnectedAsync -= handler);

    /// <summary>
    /// Connecting failed.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Connecting Failed Event Args.</returns>
    public static IObservable<ConnectingFailedEventArgs> ConnectingFailed(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<ConnectingFailedEventArgs>(
            handler => client.ConnectingFailedAsync += handler,
            handler => client.ConnectingFailedAsync -= handler);

    /// <summary>
    /// Connection state changed.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>Event Args.</returns>
    public static IObservable<EventArgs> ConnectionStateChanged(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => client.ConnectionStateChangedAsync += handler,
            handler => client.ConnectionStateChangedAsync -= handler);

    /// <summary>
    /// Synchronizing subscriptions failed.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Managed Process Failed Event Args.</returns>
    public static IObservable<ManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailed(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<ManagedProcessFailedEventArgs>(
            handler => client.SynchronizingSubscriptionsFailedAsync += handler,
            handler => client.SynchronizingSubscriptionsFailedAsync -= handler);

    /// <summary>
    /// Application messages processed.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Application Message Skipped Event Args.</returns>
    public static IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<ApplicationMessageSkippedEventArgs>(
            handler => client.ApplicationMessageSkippedAsync += handler,
            handler => client.ApplicationMessageSkippedAsync -= handler);

    /// <summary>
    /// Application messages received.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Application Message Received Event Args.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived(this IManagedMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => client.ApplicationMessageReceivedAsync += handler,
            handler => client.ApplicationMessageReceivedAsync -= handler);
}
