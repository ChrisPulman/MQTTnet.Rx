// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Finds topic and payload diagnostics for observed MQTT messages.</summary>
public static class TopicDiagnostics
{
    /// <summary>Finds all diagnostics for one received message.</summary>
    /// <param name="message">The received MQTT message to inspect.</param>
    /// <returns>The diagnostics detected for the message.</returns>
    public static IReadOnlyList<TopicIssue> Find(ReceivedMqttMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var issues = new List<TopicIssue>();
        AddTopicIssues(message, issues);
        AddPayloadIssues(message, issues);
        return issues;
    }

    /// <summary>Adds diagnostics for invalid publish topics.</summary>
    /// <param name="message">The received MQTT message to inspect.</param>
    /// <param name="issues">The mutable issue list to append to.</param>
    private static void AddTopicIssues(ReceivedMqttMessage message, List<TopicIssue> issues)
    {
        if (string.IsNullOrEmpty(message.Topic))
        {
            issues.Add(new(message.Timestamp, message.Topic, TopicIssueKind.EmptyTopic, "Topic is empty."));
            return;
        }

        if (message.Topic.Contains("#", StringComparison.Ordinal) || message.Topic.Contains("+", StringComparison.Ordinal))
        {
            issues.Add(new(message.Timestamp, message.Topic, TopicIssueKind.ContainsWildcard, "Publish topic contains a wildcard."));
        }
    }

    /// <summary>Adds diagnostics for payload metadata mismatches.</summary>
    /// <param name="message">The received MQTT message to inspect.</param>
    /// <param name="issues">The mutable issue list to append to.</param>
    private static void AddPayloadIssues(ReceivedMqttMessage message, List<TopicIssue> issues)
    {
        if (message.PayloadFormatIndicator == MqttPayloadFormatIndicator.CharacterData && !PayloadInspector.IsStrictUtf8(message.RawPayload))
        {
            issues.Add(new(
                message.Timestamp,
                message.Topic,
                TopicIssueKind.InvalidPayload,
                "MQTT payload format declares UTF-8 character data, but the payload is not strict UTF-8."));
        }

        if (DeclaresJson(message) && (!PayloadInspector.IsStrictUtf8(message.RawPayload) || !IsValidJson(message.Payload)))
        {
            issues.Add(new(
                message.Timestamp,
                message.Topic,
                TopicIssueKind.InvalidPayload,
                "Content type declares JSON, but the payload is not valid JSON."));
        }
    }

    /// <summary>Determines whether the message content type declares JSON.</summary>
    /// <param name="message">The received MQTT message to inspect.</param>
    /// <returns><see langword="true"/> when the content type contains JSON.</returns>
    private static bool DeclaresJson(ReceivedMqttMessage message) =>
        !string.IsNullOrWhiteSpace(message.ContentType)
        && message.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether payload text is valid JSON.</summary>
    /// <param name="payload">The decoded payload text.</param>
    /// <returns><see langword="true"/> when parsing succeeds.</returns>
    private static bool IsValidJson(string payload)
    {
        try
        {
            using var _ = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
