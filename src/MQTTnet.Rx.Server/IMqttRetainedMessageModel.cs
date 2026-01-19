// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Server;

/// <summary>
/// Defines the contract for a retained MQTT message, including its topic, payload, quality of service, and associated
/// metadata.
/// </summary>
/// <remarks>This interface represents the structure of a retained message as used in MQTT communication.
/// Implementations provide access to message properties such as content type, payload format, user properties, and
/// correlation data, enabling interoperability with MQTT brokers and clients. Retained messages are stored by the
/// broker and delivered to new subscribers upon subscription to the relevant topic.</remarks>
public interface IMqttRetainedMessageModel
{
    /// <summary>
    /// Gets or sets the media type of the content associated with the request or response.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the correlation data associated with the message or operation.
    /// </summary>
    byte[]? CorrelationData { get; set; }

    /// <summary>
    /// Gets or sets the binary data payload associated with this instance.
    /// </summary>
    byte[]? Payload { get; set; }

    /// <summary>
    /// Gets or sets the indicator specifying the format of the message payload.
    /// </summary>
    /// <remarks>Use this property to indicate whether the payload is formatted as UTF-8 encoded text or as
    /// arbitrary binary data, according to the MQTT 5.0 specification. This information may be used by receivers to
    /// correctly interpret the payload contents.</remarks>
    MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }

    /// <summary>
    /// Gets or sets the MQTT Quality of Service (QoS) level for message delivery.
    /// </summary>
    /// <remarks>The Quality of Service level determines the guarantee of delivery for MQTT messages. Higher
    /// QoS levels provide stronger delivery guarantees but may increase network traffic and latency. Choose the
    /// appropriate level based on the reliability requirements of your application.</remarks>
    MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

    /// <summary>
    /// Gets or sets the topic name to which a response message should be sent.
    /// </summary>
    string? ResponseTopic { get; set; }

    /// <summary>
    /// Gets or sets the topic associated with the message or event.
    /// </summary>
    string? Topic { get; set; }

    /// <summary>
    /// Gets or sets the collection of user properties to include with the MQTT message.
    /// </summary>
    /// <remarks>User properties allow custom key-value pairs to be attached to the message for
    /// application-specific metadata. This property corresponds to the MQTT 5.0 user properties feature. If set to null
    /// or an empty list, no user properties are included.</remarks>
    List<MqttUserProperty>? UserProperties { get; set; }

    /// <summary>
    /// Creates a new instance of the retained message model from the specified MQTT application message.
    /// </summary>
    /// <param name="message">The MQTT application message to use as the source for the retained message model. Cannot be null.</param>
    /// <returns>A new <see cref="MqttRetainedMessageModel"/> instance representing the retained state of the specified MQTT
    /// application message.</returns>
    static abstract MqttRetainedMessageModel Create(MqttApplicationMessage message);

    /// <summary>
    /// Converts the current object to an <see cref="MqttApplicationMessage"/> instance representing the MQTT
    /// application message.
    /// </summary>
    /// <returns>An <see cref="MqttApplicationMessage"/> that contains the data and properties of the current object.</returns>
    MqttApplicationMessage ToApplicationMessage();
}
