// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client;

/// <summary>Provides observable MQTT message publishing extensions.</summary>
public static class MqttdPublishExtensions
{
    /// <summary>Provides publishing extensions for observable MQTT clients.</summary>
    /// <param name="client">The observable MQTT client stream.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes string-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message) =>
            client.PublishMessage(
                message,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes string-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes string-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, null, qos, retain);

        /// <summary>Publishes configured string-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder) =>
            client.PublishMessage(
                message,
                messageBuilder,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes configured string-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, messageBuilder, qos, true);

        /// <summary>Publishes configured string-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and string payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, messageBuilder, qos, retain);

        /// <summary>Publishes byte-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message) =>
            client.PublishMessage(
                message,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes byte-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Publishes byte-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, null, qos, retain);

        /// <summary>Publishes configured byte-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder) =>
            client.PublishMessage(
                message,
                messageBuilder,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Publishes configured byte-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, messageBuilder, qos, true);

        /// <summary>Publishes configured byte-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to publish.</param>
        /// <param name="messageBuilder">Configures each application-message builder.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            Action<MqttApplicationMessageBuilder> messageBuilder,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, messageBuilder, qos, retain);
    }

    /// <summary>Provides publishing extensions for observable resilient MQTT clients.</summary>
    /// <param name="client">The observable resilient MQTT client stream.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Enqueues string-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and string payloads to enqueue.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, string Payload)> message) =>
            client.PublishMessage(
                message,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Enqueues string-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and string payloads to enqueue.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Enqueues string-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and string payloads to enqueue.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, string Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, qos, retain);

        /// <summary>Enqueues byte-payload messages using the default MQTT delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to enqueue.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message) =>
            client.PublishMessage(
                message,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true);

        /// <summary>Enqueues byte-payload messages using the specified quality of service.</summary>
        /// <param name="message">The topics and byte payloads to enqueue.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos) => client.PublishMessage(message, qos, true);

        /// <summary>Enqueues byte-payload messages using the specified delivery settings.</summary>
        /// <param name="message">The topics and byte payloads to enqueue.</param>
        /// <param name="qos">The quality of service level to use.</param>
        /// <param name="retain">Whether the broker should retain each message.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(
            IObservable<(string Topic, byte[] Payload)> message,
            MqttQualityOfServiceLevel qos,
            bool retain) => PublishMessageCore(client, message, qos, retain);
    }

    /// <summary>Publishes string-payload messages from an observable MQTT client.</summary>
    /// <param name="client">The observable MQTT clients.</param>
    /// <param name="message">The observable messages.</param>
    /// <param name="messageBuilder">An optional message-builder customizer.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain each message.</param>
    /// <returns>The publish results.</returns>
    private static IObservable<MqttClientPublishResult> PublishMessageCore(
        IObservable<IMqttClient> client,
        IObservable<(string Topic, string Payload)> message,
        Action<MqttApplicationMessageBuilder>? messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        client
            .CombineLatest(message, static (mqttClient, mqttMessage) => (mqttClient, mqttMessage))
            .SelectMany(publish =>
                Signal.FromAsync(cancellationToken =>
                    publish.mqttClient.PublishAsync(
                        CreateApplicationMessage(
                            publish.mqttMessage.Topic,
                            publish.mqttMessage.Payload,
                            messageBuilder,
                            qos,
                            retain),
                        cancellationToken)))
            .Retry();

    /// <summary>Publishes byte-payload messages from an observable MQTT client.</summary>
    /// <param name="client">The observable MQTT clients.</param>
    /// <param name="message">The observable messages.</param>
    /// <param name="messageBuilder">An optional message-builder customizer.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain each message.</param>
    /// <returns>The publish results.</returns>
    private static IObservable<MqttClientPublishResult> PublishMessageCore(
        IObservable<IMqttClient> client,
        IObservable<(string Topic, byte[] Payload)> message,
        Action<MqttApplicationMessageBuilder>? messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        client
            .CombineLatest(message, static (mqttClient, mqttMessage) => (mqttClient, mqttMessage))
            .SelectMany(publish =>
                Signal.FromAsync(cancellationToken =>
                    publish.mqttClient.PublishAsync(
                        CreateApplicationMessage(
                            publish.mqttMessage.Topic,
                            publish.mqttMessage.Payload,
                            messageBuilder,
                            qos,
                            retain),
                        cancellationToken)))
            .Retry();

    /// <summary>Enqueues string-payload messages through an observable resilient MQTT client.</summary>
    /// <param name="client">The observable resilient MQTT clients.</param>
    /// <param name="message">The observable messages.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain each message.</param>
    /// <returns>The processed-message events.</returns>
    private static IObservable<ApplicationMessageProcessedEventArgs> PublishMessageCore(
        IObservable<IResilientMqttClient> client,
        IObservable<(string Topic, string Payload)> message,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        client
            .CombineLatest(message, static (mqttClient, mqttMessage) => (mqttClient, mqttMessage))
            .Publish(shared =>
                shared
                    .Take(1)
                    .SelectMany(static publish =>
                        publish.mqttClient.ApplicationMessageProcessed.Retry())
                    .Merge(
                        shared.SelectMany(publish =>
                            PrimitivesObservableCompatibilityExtensions
                                .FromTask(() =>
                                    publish.mqttClient.EnqueueAsync(
                                        CreateApplicationMessage(
                                            publish.mqttMessage.Topic,
                                            publish.mqttMessage.Payload,
                                            null,
                                            qos,
                                            retain)))
                                .SelectMany(static _ =>
                                    Signal.Empty<ApplicationMessageProcessedEventArgs>()))))
            .Retry();

    /// <summary>Enqueues byte-payload messages through an observable resilient MQTT client.</summary>
    /// <param name="client">The observable resilient MQTT clients.</param>
    /// <param name="message">The observable messages.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain each message.</param>
    /// <returns>The processed-message events.</returns>
    private static IObservable<ApplicationMessageProcessedEventArgs> PublishMessageCore(
        IObservable<IResilientMqttClient> client,
        IObservable<(string Topic, byte[] Payload)> message,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        client
            .CombineLatest(message, static (mqttClient, mqttMessage) => (mqttClient, mqttMessage))
            .Publish(shared =>
                shared
                    .Take(1)
                    .SelectMany(static publish =>
                        publish.mqttClient.ApplicationMessageProcessed.Retry())
                    .Merge(
                        shared.SelectMany(publish =>
                            PrimitivesObservableCompatibilityExtensions
                                .FromTask(() =>
                                    publish.mqttClient.EnqueueAsync(
                                        CreateApplicationMessage(
                                            publish.mqttMessage.Topic,
                                            publish.mqttMessage.Payload,
                                            null,
                                            qos,
                                            retain)))
                                .SelectMany(static _ =>
                                    Signal.Empty<ApplicationMessageProcessedEventArgs>()))))
            .Retry();

    /// <summary>Creates a string-payload MQTT application message with the specified delivery settings.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="messageBuilder">An optional message-builder customizer.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain the message.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage CreateApplicationMessage(
        string topic,
        string payload,
        Action<MqttApplicationMessageBuilder>? messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain)
    {
        var builder = Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);

        messageBuilder?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>Creates a byte-payload MQTT application message with the specified delivery settings.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="messageBuilder">An optional message-builder customizer.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether the broker should retain the message.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage CreateApplicationMessage(
        string topic,
        byte[] payload,
        Action<MqttApplicationMessageBuilder>? messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain)
    {
        var builder = Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);

        messageBuilder?.Invoke(builder);
        return builder.Build();
    }
}
