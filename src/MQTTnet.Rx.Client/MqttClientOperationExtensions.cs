// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Protocol;
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

/// <summary>Provides paired cold reactive wrappers for every MQTT client operation.</summary>
public static class MqttClientOperationExtensions
{
    /// <summary>Provides direct reactive MQTT client operations.</summary>
    /// <param name="client">The MQTT client.</param>
    extension(IMqttClient client)
    {
        /// <summary>Connects using prebuilt options when subscribed.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>A cold connection operation.</returns>
        public IObservable<MqttClientConnectResult> Connect(MqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTask(cancellationToken =>
            {
                var operation = client.ConnectAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Connects using fluent option configuration when subscribed.</summary>
        /// <param name="configure">Configures the connection options.</param>
        /// <returns>A cold connection operation.</returns>
        public IObservable<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            return client.Connect(builder.Build());
        }

        /// <summary>Connects using prebuilt options through an asynchronous observable.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>A cold asynchronous connection operation.</returns>
        public IObservableAsync<MqttClientConnectResult> ObserveConnect(MqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.ConnectAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Connects asynchronously using fluent option configuration.</summary>
        /// <param name="configure">Configures the connection options.</param>
        /// <returns>A cold asynchronous connection operation.</returns>
        public IObservableAsync<MqttClientConnectResult> ObserveConnect(Action<MqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            return client.ObserveConnect(builder.Build());
        }

        /// <summary>Disconnects using prebuilt options when subscribed.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTask(cancellationToken =>
            {
                var operation = client.DisconnectAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Disconnects using fluent option configuration when subscribed.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientDisconnectOptionsBuilder();
            configure(builder);
            return client.Disconnect(builder.Build());
        }

        /// <summary>Disconnects using prebuilt options through an asynchronous observable.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxUnit> ObserveDisconnect(MqttClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.DisconnectAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Disconnects asynchronously using fluent option configuration.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxUnit> ObserveDisconnect(Action<MqttClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientDisconnectOptionsBuilder();
            configure(builder);
            return client.ObserveDisconnect(builder.Build());
        }

        /// <summary>Sends a ping when subscribed.</summary>
        /// <returns>A cold ping operation.</returns>
        public IObservable<RxUnit> Ping() => FromTask(client.PingAsync);

        /// <summary>Sends a ping through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous ping operation.</returns>
        public IObservableAsync<RxUnit> ObservePing() => FromTaskSignal(client.PingAsync);

        /// <summary>Publishes a prebuilt application message when subscribed.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> Publish(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTask(cancellationToken =>
            {
                var operation = client.PublishAsync(message, cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a fluently configured application message when subscribed.</summary>
        /// <param name="configure">Configures the application message.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> Publish(Action<MqttApplicationMessageBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttApplicationMessageBuilder();
            configure(builder);
            return client.Publish(builder.Build());
        }

        /// <summary>Publishes a prebuilt application message through an asynchronous observable.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublish(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.PublishAsync(message, cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a fluently configured application message through an asynchronous observable.</summary>
        /// <param name="configure">Configures the application message.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublish(
            Action<MqttApplicationMessageBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttApplicationMessageBuilder();
            configure(builder);
            return client.ObservePublish(builder.Build());
        }

        /// <summary>Publishes a binary payload when subscribed.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishBinary(
            string topic,
            IEnumerable<byte>? payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTask(cancellationToken =>
            {
                var operation = client.PublishBinaryAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a binary payload through an asynchronous observable.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublishBinary(
            string topic,
            IEnumerable<byte>? payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.PublishBinaryAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a sequence payload when subscribed.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload sequence.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishSequence(
            string topic,
            ReadOnlySequence<byte> payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTask(cancellationToken =>
            {
                var operation = client.PublishSequenceAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a sequence payload through an asynchronous observable.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload sequence.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublishSequence(
            string topic,
            ReadOnlySequence<byte> payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.PublishSequenceAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a UTF-8 string payload when subscribed.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishString(
            string topic,
            string? payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTask(cancellationToken =>
            {
                var operation = client.PublishStringAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Publishes a UTF-8 string payload through an asynchronous observable.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="retain">Whether the message is retained.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublishString(
            string topic,
            string? payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.PublishStringAsync(
                    topic,
                    payload,
                    qualityOfServiceLevel,
                    retain,
                    cancellationToken);
                return operation;
            });
        }

        /// <summary>Reconnects using the previous options when subscribed.</summary>
        /// <returns>A cold reconnect operation.</returns>
        public IObservable<RxUnit> Reconnect() => FromTask(client.ReconnectAsync);

        /// <summary>Reconnects using the previous options through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous reconnect operation.</returns>
        public IObservableAsync<RxUnit> ObserveReconnect() => FromTaskSignal(client.ReconnectAsync);

        /// <summary>Sends enhanced-authentication exchange data when subscribed.</summary>
        /// <param name="data">The authentication exchange data.</param>
        /// <returns>A cold authentication operation.</returns>
        public IObservable<RxUnit> SendEnhancedAuthenticationExchangeData(
            MqttEnhancedAuthenticationExchangeData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return FromTask(cancellationToken =>
            {
                var operation = client.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken);
                return operation;
            });
        }

        /// <summary>Sends enhanced-authentication exchange data through an asynchronous observable.</summary>
        /// <param name="data">The authentication exchange data.</param>
        /// <returns>A cold asynchronous authentication operation.</returns>
        public IObservableAsync<RxUnit> ObserveSendEnhancedAuthenticationExchangeData(
            MqttEnhancedAuthenticationExchangeData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken);
                return operation;
            });
        }

        /// <summary>Subscribes using prebuilt options when subscribed.</summary>
        /// <param name="options">The subscribe options.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTask(cancellationToken =>
            {
                var operation = client.SubscribeAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Subscribes using fluent option configuration when subscribed.</summary>
        /// <param name="configure">Configures the subscribe options.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(Action<MqttClientSubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientSubscribeOptionsBuilder();
            configure(builder);
            return client.Subscribe(builder.Build());
        }

        /// <summary>Subscribes using prebuilt options through an asynchronous observable.</summary>
        /// <param name="options">The subscribe options.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribe(MqttClientSubscribeOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.SubscribeAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Subscribes asynchronously using fluent option configuration.</summary>
        /// <param name="configure">Configures the subscribe options.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribe(
            Action<MqttClientSubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientSubscribeOptionsBuilder();
            configure(builder);
            return client.ObserveSubscribe(builder.Build());
        }

        /// <summary>Attempts to disconnect without propagating transport errors.</summary>
        /// <returns>A cold disconnect-attempt operation.</returns>
        public IObservable<bool> TryDisconnect() =>
            client.TryDisconnect(MqttClientDisconnectOptionsReason.NormalDisconnection, null);

        /// <summary>Attempts to disconnect without propagating transport errors.</summary>
        /// <param name="reason">The disconnect reason.</param>
        /// <param name="reasonString">The optional reason text.</param>
        /// <returns>A cold disconnect-attempt operation.</returns>
        public IObservable<bool> TryDisconnect(
            MqttClientDisconnectOptionsReason reason,
            string? reasonString) => FromTask(_ => client.TryDisconnectAsync(reason, reasonString));

        /// <summary>Attempts to disconnect asynchronously without propagating transport errors.</summary>
        /// <returns>A cold asynchronous disconnect-attempt operation.</returns>
        public IObservableAsync<bool> ObserveTryDisconnect() =>
            client.ObserveTryDisconnect(MqttClientDisconnectOptionsReason.NormalDisconnection, null);

        /// <summary>Attempts to disconnect asynchronously without propagating transport errors.</summary>
        /// <param name="reason">The disconnect reason.</param>
        /// <param name="reasonString">The optional reason text.</param>
        /// <returns>A cold asynchronous disconnect-attempt operation.</returns>
        public IObservableAsync<bool> ObserveTryDisconnect(
            MqttClientDisconnectOptionsReason reason,
            string? reasonString) => FromTaskSignal(_ => client.TryDisconnectAsync(reason, reasonString));

        /// <summary>Attempts to ping without propagating transport errors.</summary>
        /// <returns>A cold ping-attempt operation.</returns>
        public IObservable<bool> TryPing() => FromTask(client.TryPingAsync);

        /// <summary>Attempts to ping asynchronously without propagating transport errors.</summary>
        /// <returns>A cold asynchronous ping-attempt operation.</returns>
        public IObservableAsync<bool> ObserveTryPing() => FromTaskSignal(client.TryPingAsync);

        /// <summary>Unsubscribes using prebuilt options when subscribed.</summary>
        /// <param name="options">The unsubscribe options.</param>
        /// <returns>A cold unsubscribe operation.</returns>
        public IObservable<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTask(cancellationToken =>
            {
                var operation = client.UnsubscribeAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Unsubscribes using fluent option configuration when subscribed.</summary>
        /// <param name="configure">Configures the unsubscribe options.</param>
        /// <returns>A cold unsubscribe operation.</returns>
        public IObservable<MqttClientUnsubscribeResult> Unsubscribe(
            Action<MqttClientUnsubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            configure(builder);
            return client.Unsubscribe(builder.Build());
        }

        /// <summary>Unsubscribes using prebuilt options through an asynchronous observable.</summary>
        /// <param name="options">The unsubscribe options.</param>
        /// <returns>A cold asynchronous unsubscribe operation.</returns>
        public IObservableAsync<MqttClientUnsubscribeResult> ObserveUnsubscribe(
            MqttClientUnsubscribeOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTaskSignal(cancellationToken =>
            {
                var operation = client.UnsubscribeAsync(options, cancellationToken);
                return operation;
            });
        }

        /// <summary>Unsubscribes asynchronously using fluent option configuration.</summary>
        /// <param name="configure">Configures the unsubscribe options.</param>
        /// <returns>A cold asynchronous unsubscribe operation.</returns>
        public IObservableAsync<MqttClientUnsubscribeResult> ObserveUnsubscribe(
            Action<MqttClientUnsubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            configure(builder);
            return client.ObserveUnsubscribe(builder.Build());
        }
    }

    /// <summary>Wraps a task result as a cold observable operation.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold observable operation.</returns>
    private static IObservable<T> FromTask<T>(Func<CancellationToken, Task<T>> operation) =>
        Signal.FromAsync(operation);

    /// <summary>Wraps a task as a cold observable operation.</summary>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold observable operation.</returns>
    private static IObservable<RxUnit> FromTask(Func<CancellationToken, Task> operation) =>
        Signal.FromAsync(async cancellationToken =>
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return RxUnit.Default;
        });

    /// <summary>Wraps a task result as a cold asynchronous observable operation.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold asynchronous observable operation.</returns>
    private static IObservableAsync<T> FromTaskSignal<T>(Func<CancellationToken, Task<T>> operation) =>
        CreateObservable.FromAsyncTask(operation);

    /// <summary>Wraps a task as a cold asynchronous observable operation.</summary>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold asynchronous observable operation.</returns>
    private static IObservableAsync<RxUnit> FromTaskSignal(Func<CancellationToken, Task> operation) =>
        CreateObservable.FromAsyncTask(operation);
}
