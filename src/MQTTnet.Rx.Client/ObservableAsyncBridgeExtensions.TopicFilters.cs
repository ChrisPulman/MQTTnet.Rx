// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.InteropServices;
using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides asynchronous observable counterparts for classic observable extension APIs.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Creates parsed topic-pattern segments.</summary>
    /// <param name="topicPattern">The topic pattern to compile.</param>
    /// <returns>The parsed topic-pattern segments.</returns>
    private static string[] CreateTopicPatternSegments(string topicPattern)
    {
        var matches = FindPlaceholders(topicPattern);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var placeholder = match.Name;
            if (!IsValidRegexGroupName(placeholder))
            {
                throw new ArgumentException(
                    "Topic placeholder names must begin with a letter or underscore and contain only letters, " +
                    "digits, or underscores.",
                    nameof(topicPattern));
            }
        }

        return topicPattern.Split('/');
    }

    /// <summary>Returns whether a placeholder is a legal regular-expression group name.</summary>
    /// <param name="placeholder">The placeholder name.</param>
    /// <returns><see langword="true"/> when the name is valid.</returns>
    private static bool IsValidRegexGroupName(string placeholder)
    {
        if (placeholder.Length == 0 || (!char.IsLetter(placeholder[0]) && placeholder[0] != '_'))
        {
            return false;
        }

        foreach (var character in placeholder)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Attempts to extract placeholder values from a received message.</summary>
    /// <param name="message">The received message.</param>
    /// <param name="patternSegments">The parsed topic-pattern segments.</param>
    /// <returns>The extracted values, or <see langword="null"/> when unmatched.</returns>
    private static (
        MqttApplicationMessageReceivedEventArgs Message,
        Dictionary<string, string> Values)? TryExtractTopicValues(
        MqttApplicationMessageReceivedEventArgs message,
        string[] patternSegments)
    {
        var topicSegments = message.ApplicationMessage.Topic.Split('/');
        if (topicSegments.Length != patternSegments.Length)
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < patternSegments.Length; index++)
        {
            if (!TryMatchTopicSegment(patternSegments[index], topicSegments[index], values))
            {
                return null;
            }
        }

        return (message, values);
    }

    /// <summary>Matches one topic segment against literals and named placeholders.</summary>
    /// <param name="patternSegment">The topic-pattern segment.</param>
    /// <param name="topicSegment">The received topic segment.</param>
    /// <param name="values">The placeholder values to populate.</param>
    /// <returns><see langword="true"/> when the segment matches.</returns>
    private static bool TryMatchTopicSegment(
        string patternSegment,
        string topicSegment,
        Dictionary<string, string> values)
    {
        var placeholders = FindPlaceholders(patternSegment);
        return placeholders.Count == 0
            ? string.Equals(patternSegment, topicSegment, StringComparison.Ordinal)
            : TryMatchTopicSegment(patternSegment, topicSegment, placeholders, 0, 0, 0, values);
    }

    /// <summary>Matches a topic segment from the specified placeholder and character positions.</summary>
    /// <param name="patternSegment">The topic-pattern segment.</param>
    /// <param name="topicSegment">The received topic segment.</param>
    /// <param name="placeholders">The placeholders in the pattern segment.</param>
    /// <param name="placeholderIndex">The current placeholder index.</param>
    /// <param name="patternIndex">The current pattern character index.</param>
    /// <param name="topicIndex">The current topic character index.</param>
    /// <param name="values">The placeholder values to populate.</param>
    /// <returns><see langword="true"/> when the remaining segment matches.</returns>
    private static bool TryMatchTopicSegment(
        string patternSegment,
        string topicSegment,
        IReadOnlyList<TopicPlaceholder> placeholders,
        int placeholderIndex,
        int patternIndex,
        int topicIndex,
        Dictionary<string, string> values)
    {
        if (placeholderIndex == placeholders.Count)
        {
            return topicSegment
                .AsSpan(topicIndex)
                .SequenceEqual(patternSegment.AsSpan(patternIndex));
        }

        var placeholderMatch = placeholders[placeholderIndex];
        var literal = patternSegment.AsSpan(patternIndex, placeholderMatch.Index - patternIndex);
        if (!topicSegment.AsSpan(topicIndex).StartsWith(literal, StringComparison.Ordinal))
        {
            return false;
        }

        var captureStart = topicIndex + literal.Length;
        var placeholder = placeholderMatch.Name;
        var containsPreviousValue = values.TryGetValue(placeholder, out var previousValue);
        for (var captureEnd = topicSegment.Length; captureEnd > captureStart; captureEnd--)
        {
            values[placeholder] = topicSegment[captureStart..captureEnd];
            if (
                TryMatchTopicSegment(
                    patternSegment,
                    topicSegment,
                    placeholders,
                    placeholderIndex + 1,
                    placeholderMatch.Index + placeholderMatch.Length,
                    captureEnd,
                    values))
            {
                return true;
            }
        }

        if (containsPreviousValue)
        {
            values[placeholder] = previousValue!;
        }
        else
        {
            _ = values.Remove(placeholder);
        }

        return false;
    }

    /// <summary>Returns whether a topic matches any configured topic filter.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="topicFilters">The topic filters.</param>
    /// <returns><see langword="true"/> when any filter matches.</returns>
    private static bool MatchesAnyTopic(string topic, string[] topicFilters)
    {
        foreach (var topicFilter in topicFilters)
        {
            if (
                MqttTopicFilterComparer.Compare(topic, topicFilter)
                == MqttTopicFilterCompareResult.IsMatch)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Counts the slash-delimited levels in a topic.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <returns>The number of levels.</returns>
    private static int CountTopicLevels(string topic)
    {
        var levelCount = 1;
        foreach (var character in topic)
        {
            if (character == '/')
            {
                levelCount++;
            }
        }

        return levelCount;
    }

    /// <summary>Returns a topic level or null when the index is unavailable.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="levelIndex">The zero-based level index.</param>
    /// <returns>The topic level, or <see langword="null"/>.</returns>
    private static string? GetTopicLevel(string topic, int levelIndex)
    {
        var topicSpan = topic.AsSpan();
        var currentLevel = 0;
        var start = 0;
        for (var index = 0; index <= topicSpan.Length; index++)
        {
            if (index != topicSpan.Length && topicSpan[index] != '/')
            {
                continue;
            }

            if (currentLevel == levelIndex)
            {
                return topicSpan[start..index].ToString();
            }

            currentLevel++;
            start = index + 1;
        }

        return null;
    }

    /// <summary>Filters messages whose MQTT topics match the specified filter.</summary>
    /// <param name="observable">The received-message observable.</param>
    /// <param name="topic">The MQTT topic filter.</param>
    /// <returns>The matching-message observable.</returns>
    private static IObservableAsync<MqttApplicationMessageReceivedEventArgs> FilterTopicMatches(
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> observable,
        string topic)
    {
        ArgumentNullException.ThrowIfNull(observable);
        ArgumentNullException.ThrowIfNull(topic);

        var matchingTopics = new Dictionary<string, bool>(StringComparer.Ordinal);
        return observable
            .Where(message =>
            {
                var incomingTopic = message.ApplicationMessage.Topic;
                ref var matches = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    matchingTopics,
                    incomingTopic,
                    out var exists);
                if (!exists)
                {
                    matches =
                        MqttTopicFilterComparer.Compare(incomingTopic, topic)
                        == MqttTopicFilterCompareResult.IsMatch;
                }

                return matches;
            })
            .Retry();
    }

    /// <summary>Finds placeholders that match the previous regular-expression contract.</summary>
    /// <param name="pattern">The topic pattern to scan.</param>
    /// <returns>The discovered placeholders in source order.</returns>
    private static List<TopicPlaceholder> FindPlaceholders(string pattern)
    {
        var placeholders = new List<TopicPlaceholder>();
        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] != '{')
            {
                continue;
            }

            var nameStart = index + 1;
            var cursor = nameStart;
            while (cursor < pattern.Length && IsRegexWordCharacter(pattern[cursor]))
            {
                cursor++;
            }

            if (cursor == nameStart || cursor >= pattern.Length || pattern[cursor] != '}')
            {
                continue;
            }

            placeholders.Add(new(index, cursor - index + 1, pattern[nameStart..cursor]));
            index = cursor;
        }

        return placeholders;
    }

    /// <summary>Determines whether a character belongs to the Unicode regular-expression word class.</summary>
    /// <param name="character">The character to classify.</param>
    /// <returns><see langword="true"/> when the character is a regular-expression word character.</returns>
    private static bool IsRegexWordCharacter(char character) =>
        char.GetUnicodeCategory(character) is
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.ConnectorPunctuation;

    /// <summary>Identifies a named placeholder within a topic-pattern segment.</summary>
    /// <param name="Index">The zero-based placeholder offset.</param>
    /// <param name="Length">The placeholder length including braces.</param>
    /// <param name="Name">The placeholder name without braces.</param>
    private readonly record struct TopicPlaceholder(int Index, int Length, string Name);
}
