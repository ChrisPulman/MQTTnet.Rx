// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Server
{
    /// <summary>
    /// Represents a retained MQTT message, including its topic, payload, and associated metadata, for storage or
    /// processing within an MQTT broker or application.
    /// </summary>
    /// <remarks>This model is typically used to persist or transfer retained messages in a format suitable
    /// for serialization. It encapsulates all relevant MQTT message properties except those not required for storage,
    /// such as the retain flag itself. Use the <see cref="Create(MqttApplicationMessage)"/> method to construct an
    /// instance from an existing <see cref="MqttApplicationMessage"/>, and <see cref="ToApplicationMessage"/> to
    /// convert back when publishing or forwarding the message.</remarks>
    public sealed class MqttRetainedMessageModel : IMqttRetainedMessageModel
    {
        /// <summary>
        /// Gets or sets the media type of the content associated with the object.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the correlation data associated with the message or operation.
        /// </summary>
        /// <remarks>Correlation data is typically used to link related messages or operations for
        /// tracking or diagnostic purposes. The format and interpretation of the data are
        /// application-specific.</remarks>
        public byte[]? CorrelationData { get; set; }

        /// <summary>
        /// Gets or sets the binary payload data associated with this instance.
        /// </summary>
        public byte[]? Payload { get; set; }

        /// <summary>
        /// Gets or sets the indicator specifying the format of the message payload.
        /// </summary>
        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }

        /// <summary>
        /// Gets or sets the MQTT Quality of Service (QoS) level to use when publishing messages.
        /// </summary>
        /// <remarks>The Quality of Service level determines the guarantee of delivery for MQTT messages.
        /// Higher QoS levels provide stronger delivery guarantees but may increase network traffic and latency. Choose
        /// the appropriate level based on the reliability requirements of your application.</remarks>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        /// <summary>
        /// Gets or sets the topic name to which response messages are published.
        /// </summary>
        public string? ResponseTopic { get; set; }

        /// <summary>
        /// Gets or sets the topic associated with the current context.
        /// </summary>
        public string? Topic { get; set; }

        /// <summary>
        /// Gets or sets the collection of user properties to include with the MQTT message.
        /// </summary>
        /// <remarks>User properties allow custom key-value pairs to be attached to an MQTT message for
        /// application-specific metadata. This property is typically used to convey additional information that is not
        /// covered by standard MQTT headers.</remarks>
        public List<MqttUserProperty>? UserProperties { get; set; }

        /// <summary>
        /// Creates a new instance of the MqttRetainedMessageModel class from the specified MQTT application message.
        /// </summary>
        /// <remarks>The payload is copied from the original message to ensure compatibility with JSON
        /// serialization. All other properties are mapped directly from the provided message.</remarks>
        /// <param name="message">The MQTT application message to convert into a retained message model. Cannot be null.</param>
        /// <returns>A new MqttRetainedMessageModel instance containing the data from the specified message.</returns>
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
                QualityOfServiceLevel = message.QualityOfServiceLevel
            };
        }

        /// <summary>
        /// Converts the current object to an <see cref="MqttApplicationMessage"/> instance for publishing to an MQTT
        /// broker.
        /// </summary>
        /// <remarks>The returned message has the <see cref="MqttApplicationMessage.Retain"/> property set
        /// to <see langword="true"/> and the <see cref="MqttApplicationMessage.Dup"/> property set to <see
        /// langword="false"/> by default.</remarks>
        /// <returns>An <see cref="MqttApplicationMessage"/> that represents the current message, with properties mapped to the
        /// corresponding MQTT application message fields.</returns>
        public MqttApplicationMessage ToApplicationMessage() => new()
        {
            Topic = Topic,
            PayloadSegment = new ArraySegment<byte>(Payload ?? []),
            PayloadFormatIndicator = PayloadFormatIndicator,
            ResponseTopic = ResponseTopic,
            CorrelationData = CorrelationData,
            ContentType = ContentType,
            UserProperties = UserProperties,
            QualityOfServiceLevel = QualityOfServiceLevel,
            Dup = false,
            Retain = true
        };
    }
}
