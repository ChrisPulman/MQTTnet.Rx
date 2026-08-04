// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.AspNetCore.Reactive;
#else
namespace MQTTnet.Rx.AspNetCore;
#endif

/// <summary>Provides reactive operations and property snapshots for MQTT ASP.NET Core connections.</summary>
public static class MqttConnectionContextExtensions
{
    /// <summary>Provides reactive connection operations.</summary>
    /// <param name="connection">The MQTT connection context.</param>
    extension(MqttConnectionContext connection)
    {
        /// <summary>Captures all public connection properties.</summary>
        /// <returns>The current connection-property snapshot.</returns>
        public MqttConnectionProperties Properties() => new(
            connection.BytesReceived,
            connection.BytesSent,
            connection.ClientCertificate,
            connection.IsSecureConnection,
            connection.LocalEndPoint,
            connection.RemoteEndPoint,
            connection.PacketFormatterAdapter);

        /// <summary>Resets the connection byte counters.</summary>
        /// <returns>The same connection context.</returns>
        public MqttConnectionContext ResetConnectionStatistics()
        {
            connection.ResetStatistics();
            return connection;
        }

        /// <summary>Creates a cold observable that connects when subscribed.</summary>
        /// <returns>A connection operation sequence.</returns>
        public IObservable<RxVoid> Connect() => SignalFactory.FromAsync(
            async cancellationToken =>
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Creates a cold asynchronous observable that connects when subscribed.</summary>
        /// <returns>An asynchronous connection operation sequence.</returns>
        public IObservableAsync<RxVoid> ConnectSignal() => SignalAsync.FromAsync(
            async cancellationToken =>
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Creates a cold observable that disconnects when subscribed.</summary>
        /// <returns>A disconnection operation sequence.</returns>
        public IObservable<RxVoid> Disconnect() => SignalFactory.FromAsync(
            async cancellationToken =>
            {
                await connection.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Creates a cold asynchronous observable that disconnects when subscribed.</summary>
        /// <returns>An asynchronous disconnection operation sequence.</returns>
        public IObservableAsync<RxVoid> DisconnectSignal() => SignalAsync.FromAsync(
            async cancellationToken =>
            {
                await connection.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                return RxVoid.Default;
            });

        /// <summary>Creates a cold observable that receives one packet when subscribed.</summary>
        /// <returns>A packet receive operation sequence.</returns>
        public IObservable<MqttPacket> ReceivePacket() =>
            SignalFactory.FromAsync(connection.ReceivePacketAsync);

        /// <summary>Creates a cold asynchronous observable that receives one packet when subscribed.</summary>
        /// <returns>An asynchronous packet receive operation sequence.</returns>
        public IObservableAsync<MqttPacket> ReceivePacketSignal() => SignalAsync.FromAsync(
            cancellationToken => new ValueTask<MqttPacket>(connection.ReceivePacketAsync(cancellationToken)));

        /// <summary>Creates a cold observable that sends a packet when subscribed.</summary>
        /// <param name="packet">The MQTT packet to send.</param>
        /// <returns>A packet send operation sequence.</returns>
        public IObservable<RxVoid> SendPacket(MqttPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);
            return SignalFactory.FromAsync(
                async cancellationToken =>
                {
                    await connection.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }

        /// <summary>Creates a cold asynchronous observable that sends a packet when subscribed.</summary>
        /// <param name="packet">The MQTT packet to send.</param>
        /// <returns>An asynchronous packet send operation sequence.</returns>
        public IObservableAsync<RxVoid> SendPacketSignal(MqttPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);
            return SignalAsync.FromAsync(
                async cancellationToken =>
                {
                    await connection.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                    return RxVoid.Default;
                });
        }
    }
}
