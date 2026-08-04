// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides reactive operations, properties, and fluent configuration for broker client statuses.</summary>
public static class MqttClientStatusExtensions
{
    /// <summary>Provides reactive client-status features.</summary>
    /// <param name="client">The broker client status.</param>
    extension(MqttClientStatus client)
    {
        /// <summary>Captures every public status property immediately.</summary>
        /// <returns>The current status-property snapshot.</returns>
        public MqttClientStatusProperties Properties() => new(
            client.BytesReceived,
            client.BytesSent,
            client.ConnectedTimestamp,
            client.RemoteEndPoint,
            client.RemoteEndPoint.ToString(),
            client.Id,
            client.LastNonKeepAlivePacketReceivedTimestamp,
            client.LastPacketReceivedTimestamp,
            client.LastPacketSentTimestamp,
            client.ProtocolVersion,
            client.ReceivedApplicationMessagesCount,
            client.ReceivedPacketsCount,
            client.SentApplicationMessagesCount,
            client.SentPacketsCount,
            client.Session);

        /// <summary>Reads an arbitrary status property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<MqttClientStatus, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTask(_ => Task.FromResult(selector(client)));
        }

        /// <summary>Reads an arbitrary status property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<MqttClientStatus, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTaskSignal(_ => Task.FromResult(selector(client)));
        }

        /// <summary>Captures every public status property once per subscription.</summary>
        /// <returns>A cold status-property snapshot.</returns>
        public IObservable<MqttClientStatusProperties> PropertySnapshots() =>
            client.Property(static value => value.Properties());

        /// <summary>Captures every public status property once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous status-property snapshot.</returns>
        public IObservableAsync<MqttClientStatusProperties> ObservePropertySnapshots() =>
            client.ObserveProperty(static value => value.Properties());

        /// <summary>Disconnects this client when subscribed.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxVoid> Disconnect(MqttServerClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = client.DisconnectAsync(options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Disconnects this client with fluent option configuration.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxVoid> Disconnect(Action<MqttServerClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerClientDisconnectOptionsBuilder();
            configure(builder);
            return client.Disconnect(builder.Build());
        }

        /// <summary>Disconnects this client through an asynchronous observable.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxVoid> ObserveDisconnect(MqttServerClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = client.DisconnectAsync(options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Disconnects this client asynchronously with fluent option configuration.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxVoid> ObserveDisconnect(
            Action<MqttServerClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerClientDisconnectOptionsBuilder();
            configure(builder);
            return client.ObserveDisconnect(builder.Build());
        }

        /// <summary>Resets statistics when subscribed.</summary>
        /// <returns>A cold reset operation.</returns>
        public IObservable<RxVoid> ResetStatisticsOperation() =>
            CreateObservable.FromTask(_ =>
            {
                client.ResetStatistics();
                return Task.CompletedTask;
            });

        /// <summary>Resets statistics through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous reset operation.</returns>
        public IObservableAsync<RxVoid> ObserveResetStatistics() =>
            CreateObservable.FromTaskSignal(_ =>
            {
                client.ResetStatistics();
                return Task.CompletedTask;
            });

        /// <summary>Sets the associated session while preserving the client-status receiver.</summary>
        /// <param name="session">The associated session.</param>
        /// <returns>The original client status.</returns>
        public MqttClientStatus WithSession(MqttSessionStatus session)
        {
            ArgumentNullException.ThrowIfNull(session);
            client.Session = session;
            return client;
        }
    }
}
