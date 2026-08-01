// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using MQTTnet.Packets;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Provides helper methods for creating test data.</summary>
public static class TestDataHelpers
{
    /// <summary>Stores the default MQTT client identifier.</summary>
    private const string DefaultClientId = "test-client";

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(string topic, string payload) =>
        CreateMessageReceivedArgs(topic, payload, DefaultClientId);

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(
        string topic,
        string payload,
        string clientId)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(clientId);

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadSequence = new ReadOnlySequence<byte>(payloadBytes);
        MqttApplicationMessage message = new()
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        MqttPublishPacket publishPacket = new()
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        return new(clientId, message, publishPacket, null);
    }

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs with byte payload.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(string topic, byte[] payload) =>
        CreateMessageReceivedArgs(topic, payload, DefaultClientId);

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs with byte payload.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(
        string topic,
        byte[] payload,
        string clientId)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(clientId);

        var payloadSequence = new ReadOnlySequence<byte>(payload);
        MqttApplicationMessage message = new()
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        MqttPublishPacket publishPacket = new()
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        return new(clientId, message, publishPacket, null);
    }

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs with JSON payload.</summary>
    /// <typeparam name="T">The type of the payload object.</typeparam>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The object to serialize as JSON.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateJsonMessageReceivedArgs<T>(string topic, T payload) =>
        CreateJsonMessageReceivedArgs(topic, payload, DefaultClientId);

    /// <summary>Creates a mock MqttApplicationMessageReceivedEventArgs with JSON payload.</summary>
    /// <typeparam name="T">The type of the payload object.</typeparam>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The object to serialize as JSON.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateJsonMessageReceivedArgs<T>(
        string topic,
        T payload,
        string clientId)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(clientId);

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return CreateMessageReceivedArgs(topic, json, clientId);
    }
}
