// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client;

/// <summary>Provides asynchronous observable counterparts for classic observable extension APIs.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Provides publishing, subscription, and discovery extensions for resilient client observables.</summary>
    /// <param name="client">The resilient MQTT client stream.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes string messages with the default quality of service and retain settings.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message) =>
            client.PublishMessage(message, MqttQualityOfServiceLevel.ExactlyOnce, true);

        /// <summary>Publishes string messages with the specified quality of service.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes string messages with the specified quality of service and retain settings.</summary>
        /// <param name="message">The string messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            return MqttdPublishExtensions
                .PublishMessage(client.ToObservable(), message.ToObservable(), qos, retain)
                .ToSignal();
        }

        /// <summary>Publishes byte-array messages with the default quality of service and retain settings.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message) =>
            client.PublishMessage(message, MqttQualityOfServiceLevel.ExactlyOnce, true);

        /// <summary>Publishes byte-array messages with the specified quality of service.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes byte-array messages with the specified quality of service and retain settings.</summary>
        /// <param name="message">The byte-array messages to publish.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether messages are retained.</param>
        /// <returns>The processed-message observable.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservableAsync<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            return MqttdPublishExtensions
                .PublishMessage(client.ToObservable(), message.ToObservable(), qos, retain)
                .ToSignal();
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
