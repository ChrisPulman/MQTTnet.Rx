// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Adds full-options operation overloads to reactive MQTT client sequences.</summary>
public static class MqttClientSequenceOperationExtensions
{
    /// <summary>Provides full-options operations for synchronous client sequences.</summary>
    /// <param name="clients">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> clients)
    {
        /// <summary>Connects each client with prebuilt options.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>The connection-result sequence.</returns>
        public IObservable<MqttClientConnectResult> Connect(MqttClientOptions options) =>
            clients.SelectMany(client => client.Connect(options));

        /// <summary>Connects each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the connection options.</param>
        /// <returns>The connection-result sequence.</returns>
        public IObservable<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            return clients.Connect(builder.Build());
        }

        /// <summary>Disconnects each client with prebuilt options.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>The disconnect-operation sequence.</returns>
        public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptions options) =>
            clients.SelectMany(client => client.Disconnect(options));

        /// <summary>Disconnects each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>The disconnect-operation sequence.</returns>
        public IObservable<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientDisconnectOptionsBuilder();
            configure(builder);
            return clients.Disconnect(builder.Build());
        }

        /// <summary>Publishes a prebuilt message from each client.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>The publish-result sequence.</returns>
        public IObservable<MqttClientPublishResult> Publish(MqttApplicationMessage message) =>
            clients.SelectMany(client => client.Publish(message));

        /// <summary>Sends enhanced-authentication exchange data from each client.</summary>
        /// <param name="data">The authentication exchange data.</param>
        /// <returns>The authentication-operation sequence.</returns>
        public IObservable<RxUnit> SendEnhancedAuthenticationExchangeData(
            MqttEnhancedAuthenticationExchangeData data) =>
            clients.SelectMany(client => client.SendEnhancedAuthenticationExchangeData(data));

        /// <summary>Subscribes each client with prebuilt options.</summary>
        /// <param name="options">The subscribe options.</param>
        /// <returns>The subscribe-result sequence.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options) =>
            clients.SelectMany(client => client.Subscribe(options));

        /// <summary>Subscribes each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the subscribe options.</param>
        /// <returns>The subscribe-result sequence.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(Action<MqttClientSubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientSubscribeOptionsBuilder();
            configure(builder);
            return clients.Subscribe(builder.Build());
        }

        /// <summary>Unsubscribes each client with prebuilt options.</summary>
        /// <param name="options">The unsubscribe options.</param>
        /// <returns>The unsubscribe-result sequence.</returns>
        public IObservable<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options) =>
            clients.SelectMany(client => client.Unsubscribe(options));

        /// <summary>Unsubscribes each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the unsubscribe options.</param>
        /// <returns>The unsubscribe-result sequence.</returns>
        public IObservable<MqttClientUnsubscribeResult> Unsubscribe(
            Action<MqttClientUnsubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            configure(builder);
            return clients.Unsubscribe(builder.Build());
        }
    }

    /// <summary>Provides full-options operations for asynchronous client sequences.</summary>
    /// <param name="clients">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> clients)
    {
        /// <summary>Connects each client with prebuilt options.</summary>
        /// <param name="options">The connection options.</param>
        /// <returns>The asynchronous connection-result sequence.</returns>
        public IObservableAsync<MqttClientConnectResult> Connect(MqttClientOptions options) =>
            clients.SelectMany(client => client.ObserveConnect(options));

        /// <summary>Connects each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the connection options.</param>
        /// <returns>The asynchronous connection-result sequence.</returns>
        public IObservableAsync<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientOptionsBuilder();
            configure(builder);
            return clients.Connect(builder.Build());
        }

        /// <summary>Disconnects each client with prebuilt options.</summary>
        /// <param name="options">The disconnect options.</param>
        /// <returns>The asynchronous disconnect-operation sequence.</returns>
        public IObservableAsync<RxUnit> Disconnect(MqttClientDisconnectOptions options) =>
            clients.SelectMany(client => client.ObserveDisconnect(options));

        /// <summary>Disconnects each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>The asynchronous disconnect-operation sequence.</returns>
        public IObservableAsync<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientDisconnectOptionsBuilder();
            configure(builder);
            return clients.Disconnect(builder.Build());
        }

        /// <summary>Publishes a prebuilt message from each client.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>The asynchronous publish-result sequence.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(MqttApplicationMessage message) =>
            clients.SelectMany(client => client.ObservePublish(message));

        /// <summary>Sends enhanced-authentication exchange data from each client.</summary>
        /// <param name="data">The authentication exchange data.</param>
        /// <returns>The asynchronous authentication-operation sequence.</returns>
        public IObservableAsync<RxUnit> SendEnhancedAuthenticationExchangeData(
            MqttEnhancedAuthenticationExchangeData data) =>
            clients.SelectMany(client => client.ObserveSendEnhancedAuthenticationExchangeData(data));

        /// <summary>Subscribes each client with prebuilt options.</summary>
        /// <param name="options">The subscribe options.</param>
        /// <returns>The asynchronous subscribe-result sequence.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options) =>
            clients.SelectMany(client => client.ObserveSubscribe(options));

        /// <summary>Subscribes each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the subscribe options.</param>
        /// <returns>The asynchronous subscribe-result sequence.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(
            Action<MqttClientSubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientSubscribeOptionsBuilder();
            configure(builder);
            return clients.Subscribe(builder.Build());
        }

        /// <summary>Unsubscribes each client with prebuilt options.</summary>
        /// <param name="options">The unsubscribe options.</param>
        /// <returns>The asynchronous unsubscribe-result sequence.</returns>
        public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options) =>
            clients.SelectMany(client => client.ObserveUnsubscribe(options));

        /// <summary>Unsubscribes each client with fluent option configuration.</summary>
        /// <param name="configure">Configures the unsubscribe options.</param>
        /// <returns>The asynchronous unsubscribe-result sequence.</returns>
        public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(
            Action<MqttClientUnsubscribeOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            configure(builder);
            return clients.Unsubscribe(builder.Build());
        }
    }
}
