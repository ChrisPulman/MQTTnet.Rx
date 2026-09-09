// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides cold reactive wrappers for every public low-level MQTT client operation.</summary>
public static class LowLevelMqttClientOperationExtensions
{
    /// <summary>Provides reactive operations for a low-level MQTT client.</summary>
    /// <param name="client">The low-level MQTT client.</param>
    extension(ILowLevelMqttClient client)
    {
        /// <summary>Captures all public low-level client properties.</summary>
        /// <returns>The current property snapshot.</returns>
        public LowLevelMqttClientProperties Properties() => new(client.IsConnected);

        /// <summary>Reads an arbitrary low-level client property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<ILowLevelMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return Signal.FromAsync(() => Task.FromResult(selector(client)));
        }

        /// <summary>Reads an arbitrary low-level client property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<ILowLevelMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return SignalAsync.FromAsync(_ => new ValueTask<T>(selector(client)));
        }

        /// <summary>Captures all public low-level client properties once per subscription.</summary>
        /// <returns>A cold property snapshot.</returns>
        public IObservable<LowLevelMqttClientProperties> PropertySnapshots() =>
            client.Property(static value => value.Properties());

        /// <summary>Captures all public low-level client properties once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous property snapshot.</returns>
        public IObservableAsync<LowLevelMqttClientProperties> ObservePropertySnapshots() =>
            client.ObserveProperty(static value => value.Properties());

        /// <summary>Reads the current connected state once per subscription.</summary>
        /// <returns>A cold connected-state projection.</returns>
        public IObservable<bool> IsConnectedValue() =>
            client.Property(static value => value.IsConnected);

        /// <summary>Reads the current connected state once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous connected-state projection.</returns>
        public IObservableAsync<bool> ObserveIsConnected() =>
            client.ObserveProperty(static value => value.IsConnected);

        /// <summary>Observes low-level MQTT packet inspection events.</summary>
        /// <returns>An observable sequence of MQTT packet inspection events.</returns>
        public IObservable<InspectMqttPacketEventArgs> InspectPacket() =>
            CreateObservable.FromAsyncEvent<InspectMqttPacketEventArgs>(
                handler => client.InspectPacketAsync += handler,
                handler => client.InspectPacketAsync -= handler);

        /// <summary>Observes low-level MQTT packet inspection events asynchronously.</summary>
        /// <returns>An asynchronous observable sequence of MQTT packet inspection events.</returns>
        public IObservableAsync<InspectMqttPacketEventArgs> ObserveInspectPacket() =>
            CreateObservable.FromAsyncEventSignal<InspectMqttPacketEventArgs>(
                handler => client.InspectPacketAsync += handler,
                handler => client.InspectPacketAsync -= handler);

        /// <summary>Connects the low-level MQTT client when subscribed.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>A cold connect operation.</returns>
        public IObservable<RxVoid> Connect(MqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return Signal.FromAsync(
                async cancellationToken =>
                {
                    await client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }

        /// <summary>Connects the low-level MQTT client through an asynchronous observable.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>A cold asynchronous connect operation.</returns>
        public IObservableAsync<RxVoid> ObserveConnect(MqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return SignalAsync.FromAsync(
                async cancellationToken =>
                {
                    await client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }

        /// <summary>Disconnects the low-level MQTT client when subscribed.</summary>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxVoid> Disconnect() => Signal.FromAsync(
            async cancellationToken =>
            {
                await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Disconnects the low-level MQTT client through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxVoid> ObserveDisconnect() => SignalAsync.FromAsync(
            async cancellationToken =>
            {
                await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Receives one MQTT packet when subscribed.</summary>
        /// <returns>A cold receive operation.</returns>
        public IObservable<MqttPacket> Receive() =>
            Signal.FromAsync(client.ReceiveAsync);

        /// <summary>Receives one MQTT packet through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous receive operation.</returns>
        public IObservableAsync<MqttPacket> ObserveReceive() =>
            SignalAsync.FromAsync(cancellationToken => new ValueTask<MqttPacket>(client.ReceiveAsync(cancellationToken)));

        /// <summary>Sends one MQTT packet when subscribed.</summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>A cold send operation.</returns>
        public IObservable<RxVoid> Send(MqttPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);
            return Signal.FromAsync(
                async cancellationToken =>
                {
                    await client.SendAsync(packet, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }

        /// <summary>Sends one MQTT packet through an asynchronous observable.</summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>A cold asynchronous send operation.</returns>
        public IObservableAsync<RxVoid> ObserveSend(MqttPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);
            return SignalAsync.FromAsync(
                async cancellationToken =>
                {
                    await client.SendAsync(packet, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }
    }
}
