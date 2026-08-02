// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides extension methods for configuring MQTT Last Will and Testament (LWT) messages.</summary>
/// <remarks>
/// Last Will and Testament is an MQTT feature that publishes a message to a specified topic
/// when the client disconnects unexpectedly. These extensions simplify LWT configuration.
/// </remarks>
public static class LastWillExtensions
{
    /// <summary>Provides Last Will and Testament configuration extensions.</summary>
    /// <param name="builder">The options builder to configure.</param>
    extension(MqttClientOptionsBuilder builder)
    {
        /// <summary>Configures a string Last Will using default quality-of-service and retention settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWill(string topic, string payload) =>
            builder.WithLastWill(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true);

        /// <summary>Configures a string Last Will using the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWill(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos) => builder.WithLastWill(topic, payload, qos, true);

        /// <summary>Configures a byte-array Last Will with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWill(string topic, byte[] payload) =>
            builder.WithLastWill(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce, true);

        /// <summary>Configures a JSON Last Will using default quality-of-service and retention settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWill(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos) => builder.WithLastWill(topic, payload, qos, true);

        /// <summary>Configures a byte-array Last Will using the default retention setting.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillJson<T>(string topic, T payload) =>
            builder.WithLastWillJson(
                topic,
                payload,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true,
                null);

        /// <summary>Configures a JSON Last Will using default serializer and retention settings.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillJson<T>(
            string topic,
            T payload,
            MqttQualityOfServiceLevel qos) => builder.WithLastWillJson(topic, payload, qos, true, null);

        /// <summary>Configures a JSON Last Will using default serializer settings.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="qos">The quality of service.</param>
        /// <param name="retain">Whether the will is retained.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillJson<T>(
            string topic,
            T payload,
            MqttQualityOfServiceLevel qos,
            bool retain) => builder.WithLastWillJson(topic, payload, qos, retain, null);

        /// <summary>Configures a presence Last Will with default message and quality of service.</summary>
        /// <param name="statusTopic">The status topic.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithPresenceLastWill(string statusTopic) =>
            builder.WithPresenceLastWill(
                statusTopic,
                "offline",
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Configures a presence Last Will with the default quality of service.</summary>
        /// <param name="statusTopic">The status topic.</param>
        /// <param name="offlineMessage">The offline message.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithPresenceLastWill(
            string statusTopic,
            string offlineMessage) =>
            builder.WithPresenceLastWill(
                statusTopic,
                offlineMessage,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Configures a JSON presence Last Will with the default quality of service.</summary>
        /// <param name="statusTopic">The status topic.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithPresenceLastWillJson(
            string statusTopic,
            string clientId) =>
            builder.WithPresenceLastWillJson(
                statusTopic,
                clientId,
                MqttQualityOfServiceLevel.AtLeastOnce,
                TimeProvider.System);

        /// <summary>Configures a JSON presence Last Will using the system clock.</summary>
        /// <param name="statusTopic">The status topic.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithPresenceLastWillJson(
            string statusTopic,
            string clientId,
            MqttQualityOfServiceLevel qos) =>
            builder.WithPresenceLastWillJson(statusTopic, clientId, qos, TimeProvider.System);

        /// <summary>Configures a delayed Last Will with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="delay">The publication delay.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithDelayedLastWill(
            string topic,
            string payload,
            in TimeSpan delay) =>
            builder.WithDelayedLastWill(
                topic,
                payload,
                delay,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures a delayed Last Will with the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="delay">The publication delay.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithDelayedLastWill(
            string topic,
            string payload,
            in TimeSpan delay,
            MqttQualityOfServiceLevel qos) => builder.WithDelayedLastWill(topic, payload, delay, qos, true);

        /// <summary>Configures Last Will metadata with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="contentType">The payload content type.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillMetadata(
            string topic,
            string payload,
            string contentType) =>
            builder.WithLastWillMetadata(
                topic,
                payload,
                contentType,
                null,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures Last Will metadata using default quality-of-service and retention settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="contentType">The payload content type.</param>
        /// <param name="correlationData">The optional correlation data.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillMetadata(
            string topic,
            string payload,
            string contentType,
            byte[]? correlationData) =>
            builder.WithLastWillMetadata(
                topic,
                payload,
                contentType,
                correlationData,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures Last Will metadata using the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="contentType">The payload content type.</param>
        /// <param name="correlationData">The optional correlation data.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillMetadata(
            string topic,
            string payload,
            string contentType,
            byte[]? correlationData,
            MqttQualityOfServiceLevel qos) =>
            builder.WithLastWillMetadata(topic, payload, contentType, correlationData, qos, true);

        /// <summary>Configures string Last Will user properties with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, string> userProperties) =>
            builder.WithLastWillUserProperties(
                topic,
                payload,
                userProperties,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures string Last Will user properties using the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, string> userProperties,
            MqttQualityOfServiceLevel qos) =>
            builder.WithLastWillUserProperties(topic, payload, userProperties, qos, true);

        /// <summary>Configures byte-segment Last Will user properties with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ArraySegment<byte>> userProperties) =>
            builder.WithLastWillUserProperties(
                topic,
                payload,
                userProperties,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures byte-segment Last Will user properties using the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ArraySegment<byte>> userProperties,
            MqttQualityOfServiceLevel qos) =>
            builder.WithLastWillUserProperties(topic, payload, userProperties, qos, true);

        /// <summary>Configures memory-based Last Will user properties with default settings.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ReadOnlyMemory<byte>> userProperties) =>
            builder.WithLastWillUserProperties(
                topic,
                payload,
                userProperties,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true);

        /// <summary>Configures memory-based Last Will user properties using the default retention setting.</summary>
        /// <param name="topic">The topic to publish.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The user properties.</param>
        /// <param name="qos">The quality of service.</param>
        /// <returns>The configured options builder.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ReadOnlyMemory<byte>> userProperties,
            MqttQualityOfServiceLevel qos) =>
            builder.WithLastWillUserProperties(topic, payload, userProperties, qos, true);

        /// <summary>Configures a Last Will and Testament message with a string payload.</summary>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithLastWill(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);

            return builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain);
        }

        /// <summary>Configures a Last Will and Testament message with a byte array payload.</summary>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithLastWill(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);

            return builder
                .WithWillTopic(topic)
                .WithWillPayload(payload)
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain);
        }

        /// <summary>Configures a Last Will and Testament message with a JSON-serialized object payload.</summary>
        /// <typeparam name="T">The type of the payload object.</typeparam>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The object to serialize as JSON for the payload.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <param name="options">Optional JSON serializer settings.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithLastWillJson<T>(
            string topic,
            T payload,
            MqttQualityOfServiceLevel qos,
            bool retain,
            JsonSerializerOptions? options)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);

            var json = JsonSerializer.Serialize(payload, options);

            return builder.WithLastWill(topic, json, qos, retain);
        }

        /// <summary>Configures a status-based Last Will and Testament for presence detection.</summary>
        /// <param name="statusTopic">The topic for status messages (e.g., "clients/{clientId}/status").</param>
        /// <param name="offlineMessage">The message to publish when the client goes offline. Default is
        /// "offline".</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        /// <remarks>
        /// This is commonly used with a pattern where the client publishes "online" to the status topic
        /// on connect, and the LWT publishes "offline" if the client disconnects unexpectedly.
        /// </remarks>
        public MqttClientOptionsBuilder WithPresenceLastWill(
            string statusTopic,
            string offlineMessage,
            MqttQualityOfServiceLevel qos)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(statusTopic);

            return builder.WithLastWill(statusTopic, offlineMessage, qos);
        }

        /// <summary>Configures a JSON-based presence Last Will and Testament.</summary>
        /// <param name="statusTopic">The topic for status messages.</param>
        /// <param name="clientId">The client identifier to include in the status.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="timeProvider">The clock used to timestamp the offline status.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithPresenceLastWillJson(
            string statusTopic,
            string clientId,
            MqttQualityOfServiceLevel qos,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(statusTopic);
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(timeProvider);

            var status = new ClientStatus
            {
                ClientId = clientId,
                Status = "offline",
                Timestamp = timeProvider.GetUtcNow().UtcDateTime,
            };

            return builder.WithLastWillJson(statusTopic, status, qos);
        }

        /// <summary>Configures a Last Will with a delay before publishing (MQTT 5.0 feature).</summary>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="delay">The delay before publishing the will message after disconnect.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        /// <remarks>
        /// The delay feature allows time for the client to reconnect before the will message is published.
        /// This is only supported in MQTT 5.0.
        /// </remarks>
        public MqttClientOptionsBuilder WithDelayedLastWill(
            string topic,
            string payload,
            in TimeSpan delay,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);

            return builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain)
                .WithWillDelayInterval((uint)delay.TotalSeconds);
        }

        /// <summary>Configures a Last Will with content type and correlation data (MQTT 5.0 features).</summary>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="contentType">The content type of the payload (e.g., "application/json").</param>
        /// <param name="correlationData">Optional correlation data for request/response patterns.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithLastWillMetadata(
            string topic,
            string payload,
            string contentType,
            byte[]? correlationData,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(contentType);

            _ = builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain)
                .WithWillContentType(contentType);

            if (correlationData is not null)
            {
                _ = builder.WithWillCorrelationData(correlationData);
            }

            return builder;
        }

        /// <summary>Configures a Last Will with user properties (MQTT 5.0 feature).</summary>
        /// <param name="topic">The topic to publish the LWT message to.</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">Dictionary of user property key-value pairs.</param>
        /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
        /// <param name="retain">Whether to retain the message. Default is true.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, string> userProperties,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(userProperties);

            _ = builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain);

            foreach (var property in userProperties)
            {
                _ = builder.WithWillUserProperty(
                    property.Key,
                    Encoding.UTF8.GetBytes(property.Value));
            }

            return builder;
        }

        /// <summary>Configures Last Will user properties represented by byte segments.</summary>
        /// <remarks>This method allows you to specify custom user properties for the last will message, which can
        /// be used to convey additional metadata to subscribers. The payload is encoded as UTF-8. All parameters must
        /// be
        /// non-null.</remarks>
        /// <param name="topic">The topic on which the last will message will be published. Cannot be null.</param>
        /// <param name="payload">The payload content of the last will message. Cannot be null.</param>
        /// <param name="userProperties">A collection of user properties to include with the last will message. Each
        /// key-value pair represents a property
        /// name and its associated value. Cannot be null.</param>
        /// <param name="qos">The quality of service level to use for the last will message. The default is
        /// MqttQualityOfServiceLevel.AtLeastOnce.</param>
        /// <param name="retain">true to retain the last will message on the broker; otherwise, false. The default is
        /// true.</param>
        /// <returns>The same MQTT client options builder instance with the last will message configured.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ArraySegment<byte>> userProperties,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(userProperties);

            _ = builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain);

            foreach (var property in userProperties)
            {
                _ = builder.WithWillUserProperty(property.Key, property.Value);
            }

            return builder;
        }

        /// <summary>Configures Last Will user properties represented by read-only memory.</summary>
        /// <remarks>This method sets the last will topic, payload, quality of service level, retain flag, and
        /// adds all specified user properties to the last will message. Calling this method will overwrite any
        /// previously
        /// set last will configuration on the builder.</remarks>
        /// <param name="topic">The topic on which the last will message will be published. Cannot be null.</param>
        /// <param name="payload">The payload of the last will message, encoded as UTF-8. Cannot be null.</param>
        /// <param name="userProperties">A collection of user properties to include with the last will message. Each
        /// key-value pair represents a property
        /// name and its value as a byte array. Cannot be null.</param>
        /// <param name="qos">The quality of service level to use for the last will message. The default is
        /// AtLeastOnce.</param>
        /// <param name="retain">A value indicating whether the last will message should be retained by the broker. The
        /// default is <see
        /// langword="true"/>.</param>
        /// <returns>The same <see cref="MqttClientOptionsBuilder"/> instance with the last will message
        /// configured.</returns>
        public MqttClientOptionsBuilder WithLastWillUserProperties(
            string topic,
            string payload,
            IDictionary<string, ReadOnlyMemory<byte>> userProperties,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(userProperties);

            _ = builder
                .WithWillTopic(topic)
                .WithWillPayload(Encoding.UTF8.GetBytes(payload))
                .WithWillQualityOfServiceLevel(qos)
                .WithWillRetain(retain);

            foreach (var property in userProperties)
            {
                _ = builder.WithWillUserProperty(property.Key, property.Value);
            }

            return builder;
        }
    }

    /// <summary>Represents a client status message for presence detection.</summary>
    private sealed class ClientStatus
    {
        /// <summary>Gets or sets the client identifier.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Gets or sets the status (e.g., "online" or "offline").</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Gets or sets the timestamp of the status change.</summary>
        public DateTime Timestamp { get; set; }
    }
}
