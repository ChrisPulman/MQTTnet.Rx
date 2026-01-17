// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for filtering MQTT messages by topic patterns including wildcard support.
/// </summary>
/// <remarks>
/// These extensions support MQTT topic wildcards:
/// - '+' matches a single topic level.
/// - '#' matches any number of topic levels (must be at the end).
/// </remarks>
public static partial class TopicFilterExtensions
{
    /// <summary>
    /// Filters MQTT messages where the topic matches any of the specified patterns.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="topicFilters">The topic filter patterns to match against.</param>
    /// <returns>An observable sequence containing messages matching any of the topic filters.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        params string[] topicFilters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicFilters);

        if (topicFilters.Length == 0)
        {
            return Observable.Empty<MqttApplicationMessageReceivedEventArgs>();
        }

        if (topicFilters.Length == 1)
        {
            return source.WhereTopicIsMatch(topicFilters[0]);
        }

        return source.Where(e => topicFilters.Any(filter =>
            MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, filter) == MqttTopicFilterCompareResult.IsMatch));
    }

    /// <summary>
    /// Filters MQTT messages where the topic does not match the specified pattern.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="topicFilter">The topic filter pattern to exclude.</param>
    /// <returns>An observable sequence containing messages not matching the topic filter.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        string topicFilter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicFilter);

        return source.Where(e =>
            MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, topicFilter) != MqttTopicFilterCompareResult.IsMatch);
    }

    /// <summary>
    /// Extracts values from topic levels based on a pattern with named placeholders.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="topicPattern">
    /// The topic pattern with named placeholders in the format {name}.
    /// Example: "sensors/{sensorId}/readings/{type}".
    /// </param>
    /// <returns>
    /// An observable sequence of tuples containing the message and extracted values.
    /// </returns>
    /// <example>
    /// <code>
    /// client.ApplicationMessageReceived()
    ///     .ExtractTopicValues("sensors/{sensorId}/readings/{type}")
    ///     .Subscribe(x =>
    ///     {
    ///         Console.WriteLine($"Sensor: {x.Values["sensorId"]}, Type: {x.Values["type"]}");
    ///     });
    /// </code>
    /// </example>
    public static IObservable<(MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)> ExtractTopicValues(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        string topicPattern)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicPattern);

        // Find all placeholders
        var placeholders = PlaceholderRegex().Matches(topicPattern)
            .Select(m => m.Groups[1].Value)
            .ToArray();

        // Convert pattern to regex
        var regexPattern = "^" + PlaceholderRegex().Replace(
            Regex.Escape(topicPattern).Replace(@"\{", "{").Replace(@"\}", "}"),
            "(?<$1>[^/]+)") + "$";

        var regex = new Regex(regexPattern, RegexOptions.Compiled);

        return source
            .Select(e =>
            {
                var match = regex.Match(e.ApplicationMessage.Topic);
                if (!match.Success)
                {
                    return default;
                }

                var values = placeholders.ToDictionary(
                    p => p,
                    p => match.Groups[p].Value);

                return (Message: e, Values: values)!;
            })
            .Where(x => x.Message != null);
    }

    /// <summary>
    /// Filters messages by topic level count.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="levelCount">The exact number of topic levels required.</param>
    /// <returns>An observable sequence containing messages with the specified number of topic levels.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int levelCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(e => e.ApplicationMessage.Topic.Count(c => c == '/') + 1 == levelCount);
    }

    /// <summary>
    /// Gets a specific topic level from each message.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="levelIndex">The zero-based index of the topic level to extract.</param>
    /// <returns>An observable sequence of topic level strings.</returns>
    public static IObservable<string> SelectTopicLevel(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int levelIndex)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source
            .Select(e =>
            {
                var topic = e.ApplicationMessage.Topic.AsSpan();
                var currentLevel = 0;
                var start = 0;

                for (var i = 0; i <= topic.Length; i++)
                {
                    if (i == topic.Length || topic[i] == '/')
                    {
                        if (currentLevel == levelIndex)
                        {
                            return topic[start..i].ToString();
                        }

                        currentLevel++;
                        start = i + 1;
                    }
                }

                return null;
            })
            .Where(level => level != null)!;
    }

    /// <summary>
    /// Groups messages by topic.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>An observable sequence of grouped messages by topic.</returns>
    public static IObservable<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopic(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.GroupBy(e => e.ApplicationMessage.Topic);
    }

    /// <summary>
    /// Groups messages by a specific topic level.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="levelIndex">The zero-based index of the topic level to group by.</param>
    /// <returns>An observable sequence of grouped messages.</returns>
    public static IObservable<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopicLevel(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int levelIndex)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GroupBy(e =>
        {
            var levels = e.ApplicationMessage.Topic.Split('/');
            return levelIndex < levels.Length ? levels[levelIndex] : string.Empty;
        });
    }

    /// <summary>
    /// Creates a compiled regex from an MQTT topic filter pattern.
    /// </summary>
    private static Regex CreateTopicRegex(string topicFilter)
    {
        // Escape special regex characters except + and #
        var pattern = Regex.Escape(topicFilter)
            .Replace(@"\+", "[^/]+") // + matches single level
            .Replace(@"\#", ".*");     // # matches multiple levels

        return new Regex("^" + pattern + "$", RegexOptions.Compiled);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderRegex();
}
