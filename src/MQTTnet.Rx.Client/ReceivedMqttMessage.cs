// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents one MQTT application message observed by a client, bridge, or broker hook.</summary>
/// <param name="Timestamp">The local timestamp assigned when the message was observed.</param>
/// <param name="Source">The source that observed the message.</param>
/// <param name="Topic">The MQTT topic name from the application message.</param>
/// <param name="Payload">The decoded payload display value.</param>
/// <param name="DetectedFormat">The detected payload display format.</param>
/// <param name="QualityOfService">The MQTT quality of service level.</param>
/// <param name="Retain">A value indicating whether the retain flag was set.</param>
/// <param name="PayloadBytes">The number of payload bytes.</param>
/// <param name="RawPayload">The copied raw payload bytes.</param>
/// <param name="ContentType">The MQTT 5 content type metadata.</param>
/// <param name="PayloadFormatIndicator">The MQTT payload format indicator.</param>
/// <param name="ResponseTopic">The MQTT response topic metadata.</param>
/// <param name="CorrelationData">The MQTT correlation data metadata.</param>
/// <param name="MessageExpiryInterval">The MQTT message expiry interval metadata.</param>
/// <param name="SubscriptionIdentifiers">The MQTT subscription identifiers metadata.</param>
/// <param name="TopicAlias">The MQTT topic alias metadata.</param>
/// <param name="Dup">A value indicating whether this is a duplicate delivery.</param>
/// <param name="UserProperties">The MQTT user properties decoded for display.</param>
public sealed record ReceivedMqttMessage(
    DateTimeOffset Timestamp,
    string Source,
    string Topic,
    string Payload,
    PayloadFormat DetectedFormat,
    MqttQualityOfServiceLevel QualityOfService,
    bool Retain,
    long PayloadBytes,
    byte[] RawPayload,
    string? ContentType,
    MqttPayloadFormatIndicator PayloadFormatIndicator,
    string? ResponseTopic,
    byte[]? CorrelationData,
    uint MessageExpiryInterval,
    IReadOnlyList<uint> SubscriptionIdentifiers,
    ushort TopicAlias,
    bool Dup,
    IReadOnlyList<MqttUserPropertyValue> UserProperties)
{
    /// <summary>Gets the correlation data as hexadecimal text.</summary>
    public string CorrelationDataHex => CorrelationData is { Length: > 0 } ? Convert.ToHexString(CorrelationData) : string.Empty;

    /// <summary>Gets the raw payload as base64 text.</summary>
    public string RawPayloadBase64 => Convert.ToBase64String(RawPayload);

    /// <summary>Gets the raw payload as hexadecimal text.</summary>
    public string RawPayloadHex => Convert.ToHexString(RawPayload);

    /// <summary>Gets the subscription identifiers as display text.</summary>
    public string SubscriptionIdentifiersText => string.Join(", ", SubscriptionIdentifiers);

    /// <summary>Gets the user properties as display text.</summary>
    public string UserPropertiesText
    {
        get
        {
            var values = new string[UserProperties.Count];
            for (var index = 0; index < values.Length; index++)
            {
                var property = UserProperties[index];
                values[index] = $"{property.Name}={property.Value}";
            }

            return string.Join(", ", values);
        }
    }
}
