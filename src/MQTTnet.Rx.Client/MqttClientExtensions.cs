// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.PacketInspection;
using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides observable projections of MQTT client events.</summary>
public static class MqttClientExtensions
{
    /// <summary>Provides observable projections for an MQTT client.</summary>
    /// <param name="client">The MQTT client whose events are observed.</param>
    extension(IMqttClient client)
    {
        /// <summary>Observes received application messages.</summary>
        /// <returns>An observable sequence of received application messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived() =>
            CreateObservable.FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
                handler => client.ApplicationMessageReceivedAsync += handler,
                handler => client.ApplicationMessageReceivedAsync -= handler);

        /// <summary>Observes received application messages asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of received application messages.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveApplicationMessageReceived() =>
            CreateObservable.FromAsyncEventSignal<MqttApplicationMessageReceivedEventArgs>(
                handler => client.ApplicationMessageReceivedAsync += handler,
                handler => client.ApplicationMessageReceivedAsync -= handler);

        /// <summary>Observes client connections.</summary>
        /// <returns>An observable sequence of client connection events.</returns>
        public IObservable<MqttClientConnectedEventArgs> Connected() =>
            CreateObservable.FromAsyncEvent<MqttClientConnectedEventArgs>(
                handler => client.ConnectedAsync += handler,
                handler => client.ConnectedAsync -= handler);

        /// <summary>Observes client connections asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of client connection events.</returns>
        public IObservableAsync<MqttClientConnectedEventArgs> ObserveConnected() =>
            CreateObservable.FromAsyncEventSignal<MqttClientConnectedEventArgs>(
                handler => client.ConnectedAsync += handler,
                handler => client.ConnectedAsync -= handler);

        /// <summary>Observes client connection attempts.</summary>
        /// <returns>An observable sequence of client connection attempt events.</returns>
        public IObservable<MqttClientConnectingEventArgs> Connecting() =>
            CreateObservable.FromAsyncEvent<MqttClientConnectingEventArgs>(
                handler => client.ConnectingAsync += handler,
                handler => client.ConnectingAsync -= handler);

        /// <summary>Observes client connection attempts asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of client connection attempt events.</returns>
        public IObservableAsync<MqttClientConnectingEventArgs> ObserveConnecting() =>
            CreateObservable.FromAsyncEventSignal<MqttClientConnectingEventArgs>(
                handler => client.ConnectingAsync += handler,
                handler => client.ConnectingAsync -= handler);

        /// <summary>Observes client disconnections.</summary>
        /// <returns>An observable sequence of client disconnection events.</returns>
        public IObservable<MqttClientDisconnectedEventArgs> Disconnected() =>
            CreateObservable.FromAsyncEvent<MqttClientDisconnectedEventArgs>(
                handler => client.DisconnectedAsync += handler,
                handler => client.DisconnectedAsync -= handler);

        /// <summary>Observes client disconnections asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of client disconnection events.</returns>
        public IObservableAsync<MqttClientDisconnectedEventArgs> ObserveDisconnected() =>
            CreateObservable.FromAsyncEventSignal<MqttClientDisconnectedEventArgs>(
                handler => client.DisconnectedAsync += handler,
                handler => client.DisconnectedAsync -= handler);

        /// <summary>Observes MQTT packet inspection events.</summary>
        /// <returns>An observable sequence of MQTT packet inspection events.</returns>
        public IObservable<InspectMqttPacketEventArgs> InspectPacket() =>
            CreateObservable.FromAsyncEvent<InspectMqttPacketEventArgs>(
                handler => client.InspectPacketAsync += handler,
                handler => client.InspectPacketAsync -= handler);

        /// <summary>Observes MQTT packet inspection events asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of MQTT packet inspection events.</returns>
        public IObservableAsync<InspectMqttPacketEventArgs> ObserveInspectPacket() =>
            CreateObservable.FromAsyncEventSignal<InspectMqttPacketEventArgs>(
                handler => client.InspectPacketAsync += handler,
                handler => client.InspectPacketAsync -= handler);

    }
}
