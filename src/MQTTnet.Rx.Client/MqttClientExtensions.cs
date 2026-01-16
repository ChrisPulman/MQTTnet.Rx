// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for the IMqttClient interface to expose MQTT client events as observable sequences.
/// </summary>
/// <remarks>These extension methods enable reactive programming patterns by allowing consumers to subscribe to
/// MQTT client events using IObservable{T}. This facilitates integration with reactive frameworks and simplifies event
/// handling for MQTT client operations.</remarks>
public static class MqttClientExtensions
{
    /// <summary>
    /// Returns an observable sequence that signals when the MQTT client receives an application message.
    /// </summary>
    /// <remarks>The returned observable reflects the <see
    /// cref="IMqttClient.ApplicationMessageReceivedAsync"/> event. Subscribers will receive notifications for each
    /// message received by the client while subscribed. Unsubscribing from the observable detaches the event
    /// handler.</remarks>
    /// <param name="client">The MQTT client instance to observe for incoming application messages. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="MqttApplicationMessageReceivedEventArgs"/> that emits a value each time the
    /// client receives an application message.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => client.ApplicationMessageReceivedAsync += handler,
            handler => client.ApplicationMessageReceivedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT client establishes a connection to the broker.
    /// </summary>
    /// <remarks>The returned observable emits a value for each successful connection, including initial
    /// connections and any subsequent reconnections. Subscribers can use this to react to connection events in a
    /// reactive programming style.</remarks>
    /// <param name="client">The MQTT client instance to monitor for connection events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time the client successfully connects to the broker. Each
    /// notification contains event data describing the connection.</returns>
    public static IObservable<MqttClientConnectedEventArgs> Connected(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
            handler => client.ConnectedAsync += handler,
            handler => client.ConnectedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT client is initiating a connection to the server.
    /// </summary>
    /// <remarks>Subscribers are notified each time the client starts a connection attempt. The observable
    /// completes when the client is disposed. This method enables reactive handling of connection initiation events,
    /// such as logging or custom connection logic.</remarks>
    /// <param name="client">The MQTT client instance to observe for connecting events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="MqttClientConnectingEventArgs"/> that emits a value each time the client
    /// begins connecting.</returns>
    public static IObservable<MqttClientConnectingEventArgs> Connecting(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientConnectingEventArgs>(
            handler => client.ConnectingAsync += handler,
            handler => client.ConnectingAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT client is disconnected.
    /// </summary>
    /// <remarks>The observable completes when the client is disposed. Subscribers receive notifications for
    /// each disconnection event, allowing reactive handling of connection state changes.</remarks>
    /// <param name="client">The MQTT client instance to observe for disconnection events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time the client is disconnected. The sequence emits a value of
    /// type MqttClientDisconnectedEventArgs for each disconnection event.</returns>
    public static IObservable<MqttClientDisconnectedEventArgs> Disconnected(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
            handler => client.DisconnectedAsync += handler,
            handler => client.DisconnectedAsync -= handler);

    /// <summary>
    /// Creates an observable sequence that emits events when MQTT packets are inspected by the client.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time the <see cref="IMqttClient.InspectPacketAsync"/>
    /// event is raised. The observable completes when the client is disposed or the event is unsubscribed.</remarks>
    /// <param name="client">The MQTT client instance for which to observe packet inspection events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InspectMqttPacketEventArgs"/> that provides information about each
    /// inspected MQTT packet.</returns>
    public static IObservable<InspectMqttPacketEventArgs> InspectPackage(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<InspectMqttPacketEventArgs>(
            handler => client.InspectPacketAsync += handler,
            handler => client.InspectPacketAsync -= handler);
}
