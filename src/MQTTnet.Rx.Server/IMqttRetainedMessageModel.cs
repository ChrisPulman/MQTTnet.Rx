// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Defines the serializable state of a retained MQTT application message.</summary>
public interface IMqttRetainedMessageModel
{
    /// <summary>Gets or sets the message content type.</summary>
    string? ContentType { get; set; }

    /// <summary>Gets the message correlation data.</summary>
    byte[]? CorrelationData { get; init; }

    /// <summary>Gets the message payload.</summary>
    byte[]? Payload { get; init; }

    /// <summary>Gets or sets the payload format indicator.</summary>
    MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }

    /// <summary>Gets or sets the message quality of service level.</summary>
    MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

    /// <summary>Gets or sets the response topic.</summary>
    string? ResponseTopic { get; set; }

    /// <summary>Gets or sets the message topic.</summary>
    string? Topic { get; set; }

    /// <summary>Gets the message user properties.</summary>
    List<MqttUserProperty>? UserProperties { get; init; }

    /// <summary>Creates a retained-message model from an MQTT application message.</summary>
    /// <param name="message">The MQTT application message to model.</param>
    /// <returns>The retained-message model.</returns>
    static abstract MqttRetainedMessageModel Create(MqttApplicationMessage message);

    /// <summary>Converts this model to an MQTT application message.</summary>
    /// <returns>The MQTT application message.</returns>
    MqttApplicationMessage ToApplicationMessage();
}
