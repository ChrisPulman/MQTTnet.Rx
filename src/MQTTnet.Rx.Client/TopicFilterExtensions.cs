// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using RxLinq = System.Reactive.Linq;
#else
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using RxLinq = MQTTnet.Rx.Client.Linq;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides MQTT topic-filtering extensions.</summary>
public static class TopicFilterExtensions
{
    /// <summary>Provides topic-filtering extensions for MQTT message observables.</summary>
    /// <param name="source">The MQTT messages to filter.</param>
    extension(IObservable<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Filters messages matching any supplied topic filter.</summary>
        /// <param name="topicFilters">The topic filters to match.</param>
        /// <returns>An observable sequence containing matching messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny(
            params string[] topicFilters)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicFilters);

            return topicFilters.Length switch
            {
                0 => Signal.Empty<MqttApplicationMessageReceivedEventArgs>(),
                1 => source.WhereTopicIsMatch(topicFilters[0]),
                _ => source.Where(message => MatchesAny(message, topicFilters)),
            };
        }

        /// <summary>Filters messages that do not match a topic filter.</summary>
        /// <param name="topicFilter">The topic filter to exclude.</param>
        /// <returns>An observable sequence containing non-matching messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch(
            string topicFilter)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicFilter);
            return source.Where(message =>
                !IsTopicMatch(message.ApplicationMessage.Topic, topicFilter));
        }

        /// <summary>Extracts named topic-level values from messages matching a pattern.</summary>
        /// <param name="topicPattern">The topic pattern containing named placeholders.</param>
        /// <returns>An observable sequence containing each matching message and its extracted values.</returns>
        public IObservable<(
            MqttApplicationMessageReceivedEventArgs Message,
            Dictionary<string, string> Values)> ExtractTopicValues(string topicPattern)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicPattern);

            return source
                .Select(message => TryExtractTopicValues(message, topicPattern))
                .Where(static result => result is not null)
                .Select(static result => result!.Value);
        }

        /// <summary>Filters messages by their topic-level count.</summary>
        /// <param name="levelCount">The required number of topic levels.</param>
        /// <returns>An observable sequence containing messages with the required level count.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount(
            int levelCount)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Where(message =>
                CountTopicLevels(message.ApplicationMessage.Topic) == levelCount);
        }

        /// <summary>Selects a topic level from each message.</summary>
        /// <param name="levelIndex">The zero-based topic-level index.</param>
        /// <returns>An observable sequence containing available topic levels.</returns>
        public IObservable<string> SelectTopicLevel(int levelIndex) =>
            source
                .Select(message => GetTopicLevel(message.ApplicationMessage.Topic, levelIndex))
                .Where(static level => level is not null)
                .Select(static level => level!);

        /// <summary>Groups messages by their complete topic.</summary>
        /// <returns>An observable sequence of topic groups.</returns>
        public IObservable<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopic() => source.GroupBy(static message => message.ApplicationMessage.Topic);

        /// <summary>Groups messages by a topic level.</summary>
        /// <param name="levelIndex">The zero-based topic-level index.</param>
        /// <returns>An observable sequence of topic-level groups.</returns>
        public IObservable<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopicLevel(int levelIndex) =>
            source.GroupBy(message =>
                GetTopicLevel(message.ApplicationMessage.Topic, levelIndex) ?? string.Empty);
    }

    /// <summary>Determines whether a message matches any topic filter.</summary>
    /// <param name="message">The received MQTT message.</param>
    /// <param name="topicFilters">The topic filters to test.</param>
    /// <returns><see langword="true"/> when any filter matches; otherwise, <see langword="false"/>.</returns>
    private static bool MatchesAny(
        MqttApplicationMessageReceivedEventArgs message,
        string[] topicFilters)
    {
        foreach (var topicFilter in topicFilters)
        {
            if (IsTopicMatch(message.ApplicationMessage.Topic, topicFilter))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a topic matches a topic filter.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="topicFilter">The MQTT topic filter.</param>
    /// <returns><see langword="true"/> when the topic matches; otherwise, <see langword="false"/>.</returns>
    private static bool IsTopicMatch(string topic, string topicFilter) =>
        MqttTopicFilterComparer.Compare(topic, topicFilter) == MqttTopicFilterCompareResult.IsMatch;

    /// <summary>Extracts named full-level placeholders from a topic pattern.</summary>
    /// <param name="message">The received MQTT message.</param>
    /// <param name="topicPattern">The topic pattern containing placeholders.</param>
    /// <returns>The extracted values when the topic matches; otherwise, <see langword="null"/>.</returns>
    private static (
        MqttApplicationMessageReceivedEventArgs Message,
        Dictionary<string, string> Values)? TryExtractTopicValues(
            MqttApplicationMessageReceivedEventArgs message,
            string topicPattern)
    {
        var topicLevels = message.ApplicationMessage.Topic.Split('/');
        var patternLevels = topicPattern.Split('/');
        if (topicLevels.Length != patternLevels.Length)
        {
            return null;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < patternLevels.Length; index++)
        {
            if (!TryMatchTopicLevel(topicLevels[index], patternLevels[index], values))
            {
                return null;
            }
        }

        return (message, values);
    }

    /// <summary>Matches a topic level against literals and named placeholders.</summary>
    /// <param name="topicLevel">The incoming topic level.</param>
    /// <param name="patternLevel">The topic-pattern level.</param>
    /// <param name="values">The placeholder values collected while matching.</param>
    /// <returns><see langword="true"/> when the level matches; otherwise, <see langword="false"/>.</returns>
    private static bool TryMatchTopicLevel(
        string topicLevel,
        string patternLevel,
        Dictionary<string, string> values) => TryMatchTopicLevelCore(topicLevel, patternLevel, values, 0, 0);

    /// <summary>Matches a topic level from the supplied character offsets.</summary>
    /// <param name="topicLevel">The incoming topic level.</param>
    /// <param name="patternLevel">The topic-pattern level.</param>
    /// <param name="values">The placeholder values collected while matching.</param>
    /// <param name="topicIndex">The current incoming-topic offset.</param>
    /// <param name="patternIndex">The current topic-pattern offset.</param>
    /// <returns><see langword="true"/> when the remaining content matches; otherwise, <see
    /// langword="false"/>.</returns>
    private static bool TryMatchTopicLevelCore(
        string topicLevel,
        string patternLevel,
        Dictionary<string, string> values,
        int topicIndex,
        int patternIndex)
    {
        if (patternIndex == patternLevel.Length)
        {
            return topicIndex == topicLevel.Length;
        }

        if (
            TryReadPlaceholder(
                patternLevel,
                patternIndex,
                out var placeholderName,
                out var nextPatternIndex))
        {
            for (var candidateEnd = topicLevel.Length; candidateEnd > topicIndex; candidateEnd--)
            {
                var candidate = topicLevel[topicIndex..candidateEnd];
                var hadPreviousValue = values.TryGetValue(placeholderName, out var previousValue);
                values[placeholderName] = candidate;
                if (
                    TryMatchTopicLevelCore(
                        topicLevel,
                        patternLevel,
                        values,
                        candidateEnd,
                        nextPatternIndex))
                {
                    return true;
                }

                if (hadPreviousValue)
                {
                    values[placeholderName] = previousValue!;
                }
                else
                {
                    _ = values.Remove(placeholderName);
                }
            }

            return false;
        }

        return topicIndex != topicLevel.Length
            && topicLevel[topicIndex] == patternLevel[patternIndex]
            && TryMatchTopicLevelCore(
                topicLevel,
                patternLevel,
                values,
                topicIndex + 1,
                patternIndex + 1);
    }

    /// <summary>Reads a valid placeholder at a pattern offset.</summary>
    /// <param name="patternLevel">The topic-pattern level.</param>
    /// <param name="patternIndex">The offset to inspect.</param>
    /// <param name="placeholderName">The parsed placeholder name.</param>
    /// <param name="nextPatternIndex">The offset following the parsed placeholder.</param>
    /// <returns><see langword="true"/> when a valid placeholder was read; otherwise, <see langword="false"/>.</returns>
    private static bool TryReadPlaceholder(
        string patternLevel,
        int patternIndex,
        out string placeholderName,
        out int nextPatternIndex)
    {
        placeholderName = string.Empty;
        nextPatternIndex = patternIndex;
        if (patternLevel[patternIndex] != '{')
        {
            return false;
        }

        var closingBrace = patternLevel.IndexOf('}', patternIndex + 1);
        if (closingBrace < 0)
        {
            return false;
        }

        var candidate = patternLevel[(patternIndex + 1)..closingBrace];
        if (candidate.Length == 0)
        {
            return false;
        }

        foreach (var character in candidate)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        placeholderName = candidate;
        nextPatternIndex = closingBrace + 1;
        return true;
    }

    /// <summary>Counts the levels in a topic.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <returns>The number of topic levels.</returns>
    private static int CountTopicLevels(string topic)
    {
        var levels = 1;
        foreach (var character in topic)
        {
            if (character == '/')
            {
                levels++;
            }
        }

        return levels;
    }

    /// <summary>Gets a topic level by its zero-based index.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="levelIndex">The zero-based topic-level index.</param>
    /// <returns>The topic level when it exists; otherwise, <see langword="null"/>.</returns>
    private static string? GetTopicLevel(string topic, int levelIndex)
    {
        var currentLevel = 0;
        var start = 0;
        for (var index = 0; index <= topic.Length; index++)
        {
            if (index != topic.Length && topic[index] != '/')
            {
                continue;
            }

            if (currentLevel == levelIndex)
            {
                return topic[start..index];
            }

            currentLevel++;
            start = index + 1;
        }

        return null;
    }
}
