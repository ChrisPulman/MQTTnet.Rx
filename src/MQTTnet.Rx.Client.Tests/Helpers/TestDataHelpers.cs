// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using MQTTnet.Packets;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>
/// Provides helper methods for creating test data.
/// </summary>
public static class TestDataHelpers
{
    /// <summary>
    /// Creates a mock MqttApplicationMessageReceivedEventArgs.
    /// </summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(
        string topic,
        string payload,
        string clientId = "test-client")
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadSequence = new ReadOnlySequence<byte>(payloadBytes);
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payloadSequence
        };

        var publishPacket = new MqttPublishPacket
        {
            Topic = topic,
            Payload = payloadSequence
        };

        return new MqttApplicationMessageReceivedEventArgs(clientId, message, publishPacket, null);
    }

    /// <summary>
    /// Creates a mock MqttApplicationMessageReceivedEventArgs with byte payload.
    /// </summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateMessageReceivedArgs(
        string topic,
        byte[] payload,
        string clientId = "test-client")
    {
        var payloadSequence = new ReadOnlySequence<byte>(payload);
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payloadSequence
        };

        var publishPacket = new MqttPublishPacket
        {
            Topic = topic,
            Payload = payloadSequence
        };

        return new MqttApplicationMessageReceivedEventArgs(clientId, message, publishPacket, null);
    }

    /// <summary>
    /// Creates a mock MqttApplicationMessageReceivedEventArgs with JSON payload.
    /// </summary>
    /// <typeparam name="T">The type of the payload object.</typeparam>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The object to serialize as JSON.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The event args.</returns>
    public static MqttApplicationMessageReceivedEventArgs CreateJsonMessageReceivedArgs<T>(
        string topic,
        T payload,
        string clientId = "test-client")
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        return CreateMessageReceivedArgs(topic, json, clientId);
    }
}
