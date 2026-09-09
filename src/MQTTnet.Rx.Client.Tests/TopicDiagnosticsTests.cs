// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies diagnostics for received MQTT topics and payload metadata.</summary>
public sealed class TopicDiagnosticsTests
{
    /// <summary>Stores invalid UTF-8 bytes for metadata mismatch tests.</summary>
    private static readonly byte[] InvalidUtf8Payload = [0x83];

    /// <summary>Checks malformed topic names without treating ordinary levels as wildcards.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DetectsEmptyAndWildcardTopicsAsync()
    {
        var message = new MqttApplicationMessage { Topic = string.Empty }.ToReceivedMqttMessage();
        await Assert.That(TopicDiagnostics.Find(message)[0].Kind).IsEqualTo(TopicIssueKind.EmptyTopic);
        await Assert.That(TopicDiagnostics.Find(message with { Topic = "plant/+/value" })[0].Kind)
            .IsEqualTo(TopicIssueKind.ContainsWildcard);
        await Assert.That(TopicDiagnostics.Find(message with { Topic = "plant/#" })[0].Kind)
            .IsEqualTo(TopicIssueKind.ContainsWildcard);
        await Assert.That(TopicDiagnostics.Find(message with { Topic = "plant//value" })).IsEmpty();
    }

    /// <summary>Checks invalid UTF-8 only when character data is declared.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DetectsCharacterDataMismatchWithoutRejectingBinaryAsync()
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("plant/value")
            .WithPayload(InvalidUtf8Payload)
            .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
            .Build()
            .ToReceivedMqttMessage();

        await Assert.That(TopicDiagnostics.Find(message)[0].Detail)
            .Contains("UTF-8");
        await Assert.That(TopicDiagnostics.Find(message with { PayloadFormatIndicator = MqttPayloadFormatIndicator.Unspecified }))
            .IsEmpty();
        await Assert.That(TopicDiagnostics.Find(message with
        {
            PayloadFormatIndicator = MqttPayloadFormatIndicator.Unspecified,
            ContentType = "application/json",
        })[0].Kind).IsEqualTo(TopicIssueKind.InvalidPayload);
    }

    /// <summary>Checks JSON metadata independently of payload display inference.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DetectsMalformedDeclaredJsonAsync()
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("plant/value")
            .WithPayload("{invalid")
            .WithContentType("application/problem+json; charset=utf-8")
            .Build()
            .ToReceivedMqttMessage();

        await Assert.That(TopicDiagnostics.Find(message)[0].Kind).IsEqualTo(TopicIssueKind.InvalidPayload);
        await Assert.That(TopicDiagnostics.Find(message with { Payload = "{\"value\":42}" })).IsEmpty();
        await Assert.That(TopicDiagnostics.Find(message with { ContentType = "text/plain" })).IsEmpty();
    }
}
