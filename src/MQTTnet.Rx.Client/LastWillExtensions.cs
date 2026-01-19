// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for configuring MQTT Last Will and Testament (LWT) messages.
/// </summary>
/// <remarks>
/// Last Will and Testament is an MQTT feature that publishes a message to a specified topic
/// when the client disconnects unexpectedly. These extensions simplify LWT configuration.
/// </remarks>
public static class LastWillExtensions
{
    /// <summary>
    /// Configures a Last Will and Testament message with a string payload.
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="topic">The topic to publish the LWT message to.</param>
    /// <param name="payload">The message payload as a string.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is true.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithLastWill(
        this MqttClientOptionsBuilder builder,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);

        return builder.WithWillTopic(topic)
            .WithWillPayload(Encoding.UTF8.GetBytes(payload))
            .WithWillQualityOfServiceLevel(qos)
            .WithWillRetain(retain);
    }

    /// <summary>
    /// Configures a Last Will and Testament message with a byte array payload.
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="topic">The topic to publish the LWT message to.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is true.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithLastWill(
        this MqttClientOptionsBuilder builder,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);

        return builder.WithWillTopic(topic)
            .WithWillPayload(payload)
            .WithWillQualityOfServiceLevel(qos)
            .WithWillRetain(retain);
    }

    /// <summary>
    /// Configures a Last Will and Testament message with a JSON-serialized object payload.
    /// </summary>
    /// <typeparam name="T">The type of the payload object.</typeparam>
    /// <param name="builder">The client options builder.</param>
    /// <param name="topic">The topic to publish the LWT message to.</param>
    /// <param name="payload">The object to serialize as JSON for the payload.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is true.</param>
    /// <param name="options">Optional JSON serializer settings.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithLastWillJson<T>(
        this MqttClientOptionsBuilder builder,
        string topic,
        T payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);

        var json = JsonSerializer.Serialize(payload, options);

        return builder.WithLastWill(topic, json, qos, retain);
    }

    /// <summary>
    /// Configures a status-based Last Will and Testament for presence detection.
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="statusTopic">The topic for status messages (e.g., "clients/{clientId}/status").</param>
    /// <param name="offlineMessage">The message to publish when the client goes offline. Default is "offline".</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    /// <remarks>
    /// This is commonly used with a pattern where the client publishes "online" to the status topic
    /// on connect, and the LWT publishes "offline" if the client disconnects unexpectedly.
    /// </remarks>
    public static MqttClientOptionsBuilder WithPresenceLastWill(
        this MqttClientOptionsBuilder builder,
        string statusTopic,
        string offlineMessage = "offline",
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(statusTopic);

        return builder.WithLastWill(statusTopic, offlineMessage, qos, retain: true);
    }

    /// <summary>
    /// Configures a JSON-based presence Last Will and Testament.
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="statusTopic">The topic for status messages.</param>
    /// <param name="clientId">The client identifier to include in the status.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithPresenceLastWillJson(
        this MqttClientOptionsBuilder builder,
        string statusTopic,
        string clientId,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(statusTopic);
        ArgumentNullException.ThrowIfNull(clientId);

        var status = new ClientStatus
        {
            ClientId = clientId,
            Status = "offline",
            Timestamp = DateTime.UtcNow
        };

        return builder.WithLastWillJson(statusTopic, status, qos, retain: true);
    }

    /// <summary>
    /// Configures a Last Will with a delay before publishing (MQTT 5.0 feature).
    /// </summary>
    /// <param name="builder">The client options builder.</param>
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
    public static MqttClientOptionsBuilder WithDelayedLastWill(
        this MqttClientOptionsBuilder builder,
        string topic,
        string payload,
        TimeSpan delay,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);

        return builder.WithWillTopic(topic)
            .WithWillPayload(Encoding.UTF8.GetBytes(payload))
            .WithWillQualityOfServiceLevel(qos)
            .WithWillRetain(retain)
            .WithWillDelayInterval((uint)delay.TotalSeconds);
    }

    /// <summary>
    /// Configures a Last Will with content type and correlation data (MQTT 5.0 features).
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="topic">The topic to publish the LWT message to.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="contentType">The content type of the payload (e.g., "application/json").</param>
    /// <param name="correlationData">Optional correlation data for request/response patterns.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is true.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithLastWillMetadata(
        this MqttClientOptionsBuilder builder,
        string topic,
        string payload,
        string contentType,
        byte[]? correlationData = null,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(contentType);

        builder.WithWillTopic(topic)
            .WithWillPayload(Encoding.UTF8.GetBytes(payload))
            .WithWillQualityOfServiceLevel(qos)
            .WithWillRetain(retain)
            .WithWillContentType(contentType);

        if (correlationData != null)
        {
            builder.WithWillCorrelationData(correlationData);
        }

        return builder;
    }

    /// <summary>
    /// Configures a Last Will with user properties (MQTT 5.0 feature).
    /// </summary>
    /// <param name="builder">The client options builder.</param>
    /// <param name="topic">The topic to publish the LWT message to.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="userProperties">Dictionary of user property key-value pairs.</param>
    /// <param name="qos">The quality of service level. Default is AtLeastOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is true.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithLastWillUserProperties(
        this MqttClientOptionsBuilder builder,
        string topic,
        string payload,
        IDictionary<string, string> userProperties,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(userProperties);

        builder.WithWillTopic(topic)
            .WithWillPayload(Encoding.UTF8.GetBytes(payload))
            .WithWillQualityOfServiceLevel(qos)
            .WithWillRetain(retain);

        foreach (var property in userProperties)
        {
            builder.WithWillUserProperty(property.Key, property.Value);
        }

        return builder;
    }

    /// <summary>
    /// Represents a client status message for presence detection.
    /// </summary>
    private sealed class ClientStatus
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status (e.g., "online" or "offline").
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the status change.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
