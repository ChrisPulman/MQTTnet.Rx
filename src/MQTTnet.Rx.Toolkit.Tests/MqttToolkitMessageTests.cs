// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies Toolkit message model and publish builder behavior.</summary>
public sealed class MqttToolkitMessageTests
{
    /// <summary>Stores a duplicate MQTT user-property key used by metadata tests.</summary>
    private const string DuplicatePropertyName = "duplicate";

    /// <summary>Stores the first duplicate MQTT user-property value.</summary>
    private const string FirstPropertyValue = "first";

    /// <summary>Stores the second duplicate MQTT user-property value.</summary>
    private const string SecondPropertyValue = "second";

    /// <summary>Stores the expected count for duplicate user-property assertions.</summary>
    private const int DuplicatePropertyCount = 2;

    /// <summary>Stores a publish topic alias used by metadata assertions.</summary>
    private const ushort PublishTopicAlias = 3;

    /// <summary>Stores a received-message expiry interval used by metadata assertions.</summary>
    private const uint ReceivedMessageExpiryInterval = 120;

    /// <summary>Stores a received subscription identifier used by metadata assertions.</summary>
    private const uint ReceivedSubscriptionIdentifier = 9;

    /// <summary>Stores a received topic alias used by metadata assertions.</summary>
    private const ushort ReceivedTopicAlias = 7;

    /// <summary>Verifies that received messages preserve raw payload display strings and duplicate user property names.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task CreateReceivedMessagePreservesMetadataAndDuplicateUserPropertiesAsync()
    {
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("plant/line1/value")
            .WithPayload("42")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithContentType("text/plain")
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .WithResponseTopic("plant/reply")
            .WithCorrelationData([0x01, 0x02])
            .WithMessageExpiryInterval(ReceivedMessageExpiryInterval)
            .WithTopicAlias(ReceivedTopicAlias)
            .WithSubscriptionIdentifier(ReceivedSubscriptionIdentifier)
            .WithUserProperty(DuplicatePropertyName, FirstPropertyValueu8())
            .WithUserProperty(DuplicatePropertyName, SecondPropertyValueu8())
            .Build();

        var received = service.CreateReceivedMessage(message);

        await Assert.That(received.Source).IsEqualTo("Client received");
        await Assert.That(received.Topic).IsEqualTo("plant/line1/value");
        await Assert.That(received.RawPayloadHex).IsEqualTo("3432");
        await Assert.That(received.RawPayloadBase64).IsEqualTo("NDI=");
        await Assert.That(received.CorrelationDataHex).IsEqualTo("0102");
        await Assert.That(received.SubscriptionIdentifiersText).IsEqualTo("9");
        await Assert.That(received.UserProperties).Count().IsEqualTo(DuplicatePropertyCount);
        await Assert.That(received.UserProperties[0]).IsEqualTo(new(DuplicatePropertyName, FirstPropertyValue));
        await Assert.That(received.UserProperties[1]).IsEqualTo(new(DuplicatePropertyName, SecondPropertyValue));
        await Assert.That(received.UserPropertiesText).IsEqualTo("duplicate=first, duplicate=second");
    }

    /// <summary>Verifies that publish options expose MQTT 5 metadata without lossy conversion.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildMessagePreservesConfiguredMqttFiveMetadataAsync()
    {
        var publisher = new PublishMessageViewModel
        {
            Topic = "plant/write/value",
            PayloadFormat = PayloadFormat.Hex,
            Payload = "0102",
            Retain = false,
            QualityOfService = MqttQualityOfServiceLevel.ExactlyOnce,
            ContentType = "application/octet-stream",
            UsePayloadFormatIndicator = true,
            PayloadFormatIndicator = MqttPayloadFormatIndicator.Unspecified,
            ResponseTopic = "plant/write/reply",
            CorrelationData = "AAE=",
            CorrelationDataFormat = PayloadFormat.Base64,
            MessageExpirySeconds = uint.MaxValue,
            TopicAlias = PublishTopicAlias,
        };
        publisher.UserProperties.Add(new() { Name = DuplicatePropertyName, Value = FirstPropertyValue });
        publisher.UserProperties.Add(new() { Name = DuplicatePropertyName, Value = SecondPropertyValue });

        var message = publisher.BuildMessage();

        await Assert.That(message.Topic).IsEqualTo("plant/write/value");
        await Assert.That(Convert.ToHexString(message.Payload.ToArray())).IsEqualTo("0102");
        await Assert.That(message.QualityOfServiceLevel).IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
        await Assert.That(message.Retain).IsFalse();
        await Assert.That(message.ContentType).IsEqualTo("application/octet-stream");
        await Assert.That(message.PayloadFormatIndicator).IsEqualTo(MqttPayloadFormatIndicator.Unspecified);
        await Assert.That(message.ResponseTopic).IsEqualTo("plant/write/reply");
        await Assert.That(Convert.ToHexString(message.CorrelationData ?? [])).IsEqualTo("0001");
        await Assert.That(message.MessageExpiryInterval).IsEqualTo(uint.MaxValue);
        await Assert.That(message.TopicAlias).IsEqualTo(PublishTopicAlias);
        await Assert.That(message.UserProperties).Count().IsEqualTo(DuplicatePropertyCount);
    }

    /// <summary>Verifies publish validation catches invalid topics and payloads before sending.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ValidateReportsTopicAndPayloadErrorsAsync()
    {
        var publisher = new PublishMessageViewModel { Topic = string.Empty };
        await Assert.That(publisher.Validate()).IsEqualTo("Publish topic is required.");

        publisher.Topic = "plant/+/value";
        await Assert.That(publisher.Validate()).IsEqualTo("Publish topics cannot contain MQTT wildcards.");

        publisher.Topic = "plant/value";
        publisher.PayloadFormat = PayloadFormat.Json;
        publisher.Payload = "{invalid";
        await Assert.That(publisher.Validate()).StartsWith("JSON payload is invalid:");

        publisher.PayloadFormat = PayloadFormat.Hex;
        publisher.Payload = "0";
        await Assert.That(publisher.Validate()).IsEqualTo("The input is not a valid hex string as its length is not a multiple of 2.");

        publisher.PayloadFormat = PayloadFormat.Number;
        publisher.Payload = "Infinity";
        await Assert.That(publisher.Validate()).IsEqualTo("Number payload must be finite.");

        publisher.ContentType = "application/json";
        publisher.UsePayloadFormatIndicator = false;
        publisher.PayloadFormat = PayloadFormat.Hex;
        publisher.Payload = "4142";
        await Assert.That(publisher.Validate()).IsEqualTo("Content type application/json requires JSON payload bytes.");

        publisher.ContentType = string.Empty;
        publisher.UsePayloadFormatIndicator = true;
        publisher.PayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData;
        publisher.Payload = "FF";
        await Assert.That(publisher.Validate()).IsEqualTo("Payload format indicator CharacterData requires valid UTF-8 payload bytes.");
    }

    /// <summary>Gets first property value bytes as UTF-8.</summary>
    /// <returns>The first property value bytes.</returns>
    private static ReadOnlyMemory<byte> FirstPropertyValueu8() => "first"u8.ToArray();

    /// <summary>Gets second property value bytes as UTF-8.</summary>
    /// <returns>The second property value bytes.</returns>
    private static ReadOnlyMemory<byte> SecondPropertyValueu8() => "second"u8.ToArray();
}
