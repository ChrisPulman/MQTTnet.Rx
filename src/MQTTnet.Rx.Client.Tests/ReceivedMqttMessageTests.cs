// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies received MQTT message snapshot behavior.</summary>
public sealed class ReceivedMqttMessageTests
{
    /// <summary>Stores a duplicate MQTT user-property key used by metadata tests.</summary>
    private const string DuplicatePropertyName = "duplicate";

    /// <summary>Stores the non-default snapshot source used by metadata tests.</summary>
    private const string BrokerIngressSource = "Broker ingress";

    /// <summary>Stores the common value topic used by snapshot tests.</summary>
    private const string PlantValueTopic = "plant/value";

    /// <summary>Stores the expected count for duplicate user-property assertions.</summary>
    private const int DuplicatePropertyCount = 2;

    /// <summary>Stores the expected count for binary payload bytes.</summary>
    private const int BinaryPayloadByteCount = 2;

    /// <summary>Stores the mutated correlation byte value used by snapshot copy assertions.</summary>
    private const byte MutatedCorrelationByte = 0xFF;

    /// <summary>Stores a received-message expiry interval used by metadata assertions.</summary>
    private const uint ReceivedMessageExpiryInterval = 120;

    /// <summary>Stores a received subscription identifier used by metadata assertions.</summary>
    private const uint ReceivedSubscriptionIdentifier = 9;

    /// <summary>Stores a received topic alias used by metadata assertions.</summary>
    private const ushort ReceivedTopicAlias = 7;

    /// <summary>Verifies that received messages preserve raw payload display strings and duplicate user property names.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessagePreservesMetadataAndDuplicateUserPropertiesAsync()
    {
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
            .WithUserProperty(DuplicatePropertyName, "first"u8.ToArray())
            .WithUserProperty(DuplicatePropertyName, "second"u8.ToArray())
            .Build();

        var received = message.ToReceivedMqttMessage(BrokerIngressSource, TimeProvider.System);

        await Assert.That(received.Source).IsEqualTo(BrokerIngressSource);
        await Assert.That(received.Topic).IsEqualTo("plant/line1/value");
        await Assert.That(received.RawPayloadHex).IsEqualTo("3432");
        await Assert.That(received.RawPayloadBase64).IsEqualTo("NDI=");
        await Assert.That(received.CorrelationDataHex).IsEqualTo("0102");
        await Assert.That(received.SubscriptionIdentifiersText).IsEqualTo("9");
        await Assert.That(received.UserProperties).Count().IsEqualTo(DuplicatePropertyCount);
        await Assert.That(received.UserProperties[0]).IsEqualTo(new(DuplicatePropertyName, "first"));
        await Assert.That(received.UserProperties[1]).IsEqualTo(new(DuplicatePropertyName, "second"));
        await Assert.That(received.UserPropertiesText).IsEqualTo("duplicate=first, duplicate=second");
    }

    /// <summary>Verifies correlation display returns empty text when no data is present.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task CorrelationDataHexReturnsEmptyForNullAndEmptyDataAsync()
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(PlantValueTopic)
            .WithPayload("42")
            .Build()
            .ToReceivedMqttMessage();

        await Assert.That(message.CorrelationDataHex).IsEmpty();
        await Assert.That((message with { CorrelationData = [] }).CorrelationDataHex).IsEmpty();
    }

    /// <summary>Verifies that binary payloads are displayed as hexadecimal snapshots.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessageDisplaysBinaryPayloadsAsHexAsync()
    {
        var received = new MqttApplicationMessageBuilder()
            .WithTopic("plant/blob")
            .WithPayload([0x00, 0x83])
            .Build()
            .ToReceivedMqttMessage();

        await Assert.That(received.Payload).IsEqualTo("0083");
        await Assert.That(received.DetectedFormat).IsEqualTo(PayloadFormat.Hex);
        await Assert.That(received.PayloadBytes).IsEqualTo(BinaryPayloadByteCount);
    }

    /// <summary>Verifies multi-segment MQTT payloads are copied into snapshots.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessageCopiesMultiSegmentPayloadAsync()
    {
        var message = new MqttApplicationMessage
        {
            Topic = PlantValueTopic,
            Payload = CreateMultiSegmentPayload("4"u8.ToArray(), "2"u8.ToArray()),
        };

        var received = message.ToReceivedMqttMessage();

        await Assert.That(received.Payload).IsEqualTo("42");
        await Assert.That(received.RawPayloadHex).IsEqualTo("3432");
    }

    /// <summary>Verifies a missing MQTT topic is represented as an empty snapshot topic.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessageMapsNullTopicToEmptyTextAsync()
    {
        var message = new MqttApplicationMessage
        {
            Payload = new("42"u8.ToArray()),
        };

        var received = message.ToReceivedMqttMessage();

        await Assert.That(received.Topic).IsEmpty();
    }

    /// <summary>Verifies correlation data is copied into the snapshot.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessageCopiesCorrelationDataAsync()
    {
        var correlationData = new byte[] { 0x01, 0x02 };
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(PlantValueTopic)
            .WithPayload("42")
            .WithCorrelationData(correlationData)
            .Build();

        var received = message.ToReceivedMqttMessage();
        correlationData[0] = MutatedCorrelationByte;
        message.CorrelationData![1] = MutatedCorrelationByte;

        await Assert.That(received.CorrelationDataHex).IsEqualTo("0102");
    }

    /// <summary>Verifies event-argument overloads create received-message snapshots.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ToReceivedMqttMessageSupportsReceivedEventArgumentsAsync()
    {
        var payload = new ReadOnlySequence<byte>("42"u8.ToArray());
        var message = new MqttApplicationMessage
        {
            Topic = PlantValueTopic,
            Payload = payload,
        };
        var packet = new MqttPublishPacket
        {
            Topic = message.Topic,
            Payload = payload,
        };
        var args = new MqttApplicationMessageReceivedEventArgs("client", message, packet, null);

        var defaultSnapshot = args.ToReceivedMqttMessage();
        var sourcedSnapshot = args.ToReceivedMqttMessage(BrokerIngressSource, TimeProvider.System);

        await Assert.That(defaultSnapshot.Source).IsEqualTo("Client received");
        await Assert.That(sourcedSnapshot.Source).IsEqualTo(BrokerIngressSource);
        await Assert.That(sourcedSnapshot.Payload).IsEqualTo("42");
    }

    /// <summary>Creates a two-segment read-only payload sequence.</summary>
    /// <param name="first">The first segment bytes.</param>
    /// <param name="second">The second segment bytes.</param>
    /// <returns>The multi-segment sequence.</returns>
    private static ReadOnlySequence<byte> CreateMultiSegmentPayload(byte[] first, byte[] second)
    {
        var firstSegment = new Segment(first);
        var secondSegment = firstSegment.Append(second);
        return new(firstSegment, 0, secondSegment, secondSegment.Memory.Length);
    }

    /// <summary>Stores one read-only sequence segment.</summary>
    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        /// <summary>Initializes a new instance of the <see cref="Segment"/> class.</summary>
        /// <param name="memory">The segment memory.</param>
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        /// <summary>Appends another segment.</summary>
        /// <param name="memory">The appended segment memory.</param>
        /// <returns>The appended segment.</returns>
        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new Segment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length,
            };
            Next = segment;
            return segment;
        }
    }
}
