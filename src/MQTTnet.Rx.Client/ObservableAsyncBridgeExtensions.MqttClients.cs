// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client;

/// <summary>Provides asynchronous observable counterparts for classic observable extension APIs.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Provides publishing, subscription, and discovery extensions for MQTT client observables.</summary>
    /// <param name="client">The MQTT client stream.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes string messages with the default quality of service and retain settings.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message) =>
            client.PublishMessage(message, MqttQualityOfServiceLevel.ExactlyOnce, true);

        /// <summary>Publishes string messages with the specified quality of service.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes string messages with the specified quality of service and retain settings.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            return client
                .CombineLatest(message, static (mqttClient, payload) => (mqttClient, payload))
                .SelectMany(item =>
                    CreateObservable.FromAsyncTask(cancellationToken =>
                        item.mqttClient.PublishAsync(
                            BuildMessage(item.payload.Topic, item.payload.Payload, qos, retain),
                            cancellationToken)));
        }

        /// <summary>Publishes byte-array messages with the default quality of service and retain settings.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message) =>
            client.PublishMessage(message, MqttQualityOfServiceLevel.ExactlyOnce, true);

        /// <summary>Publishes byte-array messages with the specified quality of service.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes byte-array messages with the specified quality of service and retain settings.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            return client
                .CombineLatest(message, static (mqttClient, payload) => (mqttClient, payload))
                .SelectMany(item =>
                    CreateObservable.FromAsyncTask(cancellationToken =>
                        item.mqttClient.PublishAsync(
                            BuildMessage(item.payload.Topic, item.payload.Payload, qos, retain),
                            cancellationToken)));
        }

        /// <summary>Publishes string messages configured by a message builder.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder) =>
            client.PublishMessage(
                message,
                messageBuilder,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes builder-configured string messages with the specified quality of service.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, messageBuilder, qos, true);

        /// <summary>Publishes string messages configured by a message builder.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageBuilder);
            return client
                .CombineLatest(message, static (mqttClient, payload) => (mqttClient, payload))
                .SelectMany(item =>
                    CreateObservable.FromAsyncTask(cancellationToken =>
                        item.mqttClient.PublishAsync(
                            BuildMessage(
                                item.payload.Topic,
                                item.payload.Payload,
                                messageBuilder,
                                qos,
                                retain),
                            cancellationToken)));
        }

        /// <summary>Publishes byte-array messages configured by a message builder.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder) =>
            client.PublishMessage(
                message,
                messageBuilder,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes builder-configured byte-array messages with the specified quality of service.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, messageBuilder, qos, true);

        /// <summary>Publishes byte-array messages configured by a message builder.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="messageBuilder">The message configuration callback.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The publish-result observable.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageBuilder);
            return client
                .CombineLatest(message, static (mqttClient, payload) => (mqttClient, payload))
                .SelectMany(item =>
                    CreateObservable.FromAsyncTask(cancellationToken =>
                        item.mqttClient.PublishAsync(
                            BuildMessage(
                                item.payload.Topic,
                                item.payload.Payload,
                                messageBuilder,
                                qos,
                                retain),
                            cancellationToken)));
        }

        /// <summary>Subscribes to multiple MQTT topics.</summary>
        /// <param name="topics">The topic filters.</param>
        /// <returns>The received-message observable.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(
            params string[] topics)
        {
            ArgumentNullException.ThrowIfNull(client);
            return MqttdSubscribeExtensions
                .SubscribeToTopics(client.ToObservable(), topics)
                .ToSignal();
        }

        /// <summary>Subscribes to a single MQTT topic.</summary>
        /// <param name="topic">The topic filter.</param>
        /// <returns>The received-message observable.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(
            string topic)
        {
            ArgumentNullException.ThrowIfNull(client);
            return MqttdSubscribeExtensions
                .SubscribeToTopic(client.ToObservable(), topic)
                .ToSignal();
        }

        /// <summary>Discovers topics without expiration.</summary>
        /// <returns>The discovered-topic observable.</returns>
        public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics() =>
            client.DiscoverTopics(null);

        /// <summary>Discovers topics with the specified expiration interval.</summary>
        /// <param name="topicExpiry">The topic expiration interval.</param>
        /// <returns>The discovered-topic observable.</returns>
        public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry)
        {
            ArgumentNullException.ThrowIfNull(client);
            return MqttdSubscribeExtensions
                .DiscoverTopics(client.ToObservable(), topicExpiry)
                .ToSignal();
        }

        /// <summary>Discovers topics with the specified expiration interval and time provider.</summary>
        /// <param name="topicExpiry">The topic expiration interval.</param>
        /// <param name="timeProvider">The clock used for last-seen and expiry times.</param>
        /// <returns>The discovered-topic observable.</returns>
        public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(client);
            return MqttdSubscribeExtensions
                .DiscoverTopics(client.ToObservable(), topicExpiry, timeProvider)
                .ToSignal();
        }
    }
}
