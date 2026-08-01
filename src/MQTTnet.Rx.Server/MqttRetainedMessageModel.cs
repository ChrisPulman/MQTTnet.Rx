// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Server;

/// <summary>Represents the serializable state of a retained MQTT application message.</summary>
public sealed class MqttRetainedMessageModel : IMqttRetainedMessageModel
{
    /// <summary>Gets or sets the message content type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets the message correlation data.</summary>
    public byte[]? CorrelationData { get; init; }

    /// <summary>Gets the message payload.</summary>
    public byte[]? Payload { get; init; }

    /// <summary>Gets or sets the payload format indicator.</summary>
    public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }

    /// <summary>Gets or sets the message quality of service level.</summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

    /// <summary>Gets or sets the response topic.</summary>
    public string? ResponseTopic { get; set; }

    /// <summary>Gets or sets the message topic.</summary>
    public string? Topic { get; set; }

    /// <summary>Gets the message user properties.</summary>
    public List<MqttUserProperty>? UserProperties { get; init; }

    /// <summary>Creates a retained-message model from an MQTT application message.</summary>
    /// <param name="message">The MQTT application message to model.</param>
    /// <returns>The retained-message model.</returns>
    public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MqttRetainedMessageModel
        {
            Topic = message.Topic,

            // Create a copy of the buffer from the payload segment because
            // it cannot be serialized and deserialized with the JSON serializer.
            Payload = message.Payload.ToArray(),
            UserProperties = message.UserProperties,
            ResponseTopic = message.ResponseTopic,
            CorrelationData = message.CorrelationData,
            ContentType = message.ContentType,
            PayloadFormatIndicator = message.PayloadFormatIndicator,
            QualityOfServiceLevel = message.QualityOfServiceLevel,
        };
    }

    /// <summary>Converts this model to an MQTT application message.</summary>
    /// <returns>The MQTT application message.</returns>
    public MqttApplicationMessage ToApplicationMessage()
    {
        var source = this;
        return new()
        {
            Topic = source.Topic,
            PayloadSegment = new(source.Payload ?? []),
            PayloadFormatIndicator = source.PayloadFormatIndicator,
            ResponseTopic = source.ResponseTopic,
            CorrelationData = source.CorrelationData,
            ContentType = source.ContentType,
            UserProperties = source.UserProperties,
            QualityOfServiceLevel = source.QualityOfServiceLevel,
            Dup = false,
            Retain = true,
        };
    }
}
