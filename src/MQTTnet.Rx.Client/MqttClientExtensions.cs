// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqtt Client Extensions.
/// </summary>
public static class MqttClientExtensions
{
    /// <summary>
    /// Applications the message received.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Application Message Received Event Args.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
            handler => client.ApplicationMessageReceivedAsync += handler,
            handler => client.ApplicationMessageReceivedAsync -= handler);

    /// <summary>
    /// Connecteds the specified client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Client Connected Event Args.</returns>
    public static IObservable<MqttClientConnectedEventArgs> Connected(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
            handler => client.ConnectedAsync += handler,
            handler => client.ConnectedAsync -= handler);

    /// <summary>
    /// Connectings the specified client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Client Connecting Event Args.</returns>
    public static IObservable<MqttClientConnectingEventArgs> Connecting(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientConnectingEventArgs>(
            handler => client.ConnectingAsync += handler,
            handler => client.ConnectingAsync -= handler);

    /// <summary>
    /// Disconnecteds the specified client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Mqtt Client Disconnected Event Args.</returns>
    public static IObservable<MqttClientDisconnectedEventArgs> Disconnected(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
            handler => client.DisconnectedAsync += handler,
            handler => client.DisconnectedAsync -= handler);

    /// <summary>
    /// Inspects the package.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A Inspect Mqtt Packet Event Args.</returns>
    public static IObservable<InspectMqttPacketEventArgs> InspectPackage(this IMqttClient client) =>
        CreateObservable.FromAsyncEvent<InspectMqttPacketEventArgs>(
            handler => client.InspectPacketAsync += handler,
            handler => client.InspectPacketAsync -= handler);
}
