// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using MQTTnet.Protocol;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides asynchronous observable counterparts for the classic observable extension APIs.
/// </summary>
public static class ObservableAsyncBridgeExtensions
{
    private static readonly System.Text.RegularExpressions.Regex PlaceholderRegex = new(@"\{(\w+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static IObservableAsync<string> ToUtf8String(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(static e => e.PayloadUtf8());
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, params string[] topicFilters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicFilters);

        if (topicFilters.Length == 0)
        {
            return ObservableAsync.Empty<MqttApplicationMessageReceivedEventArgs>();
        }

        if (topicFilters.Length == 1)
        {
            return source.WhereTopicIsMatch(topicFilters[0]);
        }

        return source.Where(e => topicFilters.Any(filter =>
            MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, filter) == MqttTopicFilterCompareResult.IsMatch));
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, string topicFilter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicFilter);
        return source.Where(e =>
            MqttTopicFilterComparer.Compare(e.ApplicationMessage.Topic, topicFilter) != MqttTopicFilterCompareResult.IsMatch);
    }

    public static IObservableAsync<(MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)> ExtractTopicValues(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, string topicPattern)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(topicPattern);

        var placeholders = PlaceholderRegex.Matches(topicPattern)
            .Select(m => m.Groups[1].Value)
            .ToArray();

        var regexPattern = "^" + PlaceholderRegex.Replace(
            System.Text.RegularExpressions.Regex.Escape(topicPattern).Replace(@"\{", "{").Replace(@"\}", "}"),
            "(?<$1>[^/]+)") + "$";

        var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.Compiled);

        return source
            .Select(e =>
            {
                var match = regex.Match(e.ApplicationMessage.Topic);
                if (!match.Success)
                {
                    return default((MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)?);
                }

                var values = placeholders.ToDictionary(
                    p => p,
                    p => match.Groups[p].Value);

                return (Message: e, Values: values);
            })
            .Where(static x => x.HasValue)
            .Select(static x => x!.Value);
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int levelCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(e => e.ApplicationMessage.Topic.Count(c => c == '/') + 1 == levelCount);
    }

    public static IObservableAsync<string> SelectTopicLevel(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int levelIndex)
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

                return default(string);
            })
            .Where(static level => level != null)
            .Select(static level => level!);
    }

    public static IObservableAsync<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopic(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return TopicFilterExtensions.GroupByTopic(source.ToObservable()).ToObservableAsync();
    }

    public static IObservableAsync<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopicLevel(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int levelIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        return TopicFilterExtensions.GroupByTopicLevel(source.ToObservable(), levelIndex).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishMessage(this IObservableAsync<IMqttClient> client, IObservableAsync<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);

        return client.CombineLatest(message, static (cli, mess) => (cli, mess))
            .SelectMany(c => CreateObservable.FromAsyncTask(token => c.cli.PublishAsync(BuildMessage(c.mess.topic, c.mess.payLoad, qos, retain), token)));
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);
        return MqttdPublishExtensions.PublishMessage(client.ToObservable(), message.ToObservable(), qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(string topic, byte[] payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);
        return MqttdPublishExtensions.PublishMessage(client.ToObservable(), message.ToObservable(), qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishMessage(this IObservableAsync<IMqttClient> client, IObservableAsync<(string topic, string payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageBuilder);

        return client.CombineLatest(message, static (cli, mess) => (cli, mess))
            .SelectMany(c => CreateObservable.FromAsyncTask(token => c.cli.PublishAsync(BuildMessage(c.mess.topic, c.mess.payLoad, messageBuilder, qos, retain), token)));
    }

    public static IObservableAsync<MqttClientPublishResult> PublishMessage(this IObservableAsync<IMqttClient> client, IObservableAsync<(string topic, byte[] payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);

        return client.CombineLatest(message, static (cli, mess) => (cli, mess))
            .SelectMany(c => CreateObservable.FromAsyncTask(token => c.cli.PublishAsync(BuildMessage(c.mess.topic, c.mess.payLoad, qos, retain), token)));
    }

    public static IObservableAsync<MqttClientPublishResult> PublishMessage(this IObservableAsync<IMqttClient> client, IObservableAsync<(string topic, byte[] payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageBuilder);

        return client.CombineLatest(message, static (cli, mess) => (cli, mess))
            .SelectMany(c => CreateObservable.FromAsyncTask(token => c.cli.PublishAsync(BuildMessage(c.mess.topic, c.mess.payLoad, messageBuilder, qos, retain), token)));
    }

    public static IObservableAsync<Dictionary<string, object?>?> ToDictionary(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.Select(static m =>
        {
            var json = m.ApplicationMessage.ConvertPayloadToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    result[prop.Name] = DeserializeJsonElement(prop.Value);
                }

                return result;
            }
            catch
            {
                return null;
            }
        });
    }

    public static IObservableAsync<T?> ToObject<T>(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> message, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.Select(m =>
        {
            var json = m.ApplicationMessage.ConvertPayloadToString();
            try
            {
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch
            {
                return default;
            }
        });
    }

    public static IObservableAsync<object?> Observe(this IObservableAsync<Dictionary<string, object>> dictionary, string key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return MqttdSubscribeExtensions.Observe(dictionary.ToObservable(), key).ToObservableAsync();
    }

    public static IObservableAsync<bool> ToBool(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToBoolean(value));
    }

    public static IObservableAsync<byte> ToByte(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToByte(value));
    }

    public static IObservableAsync<short> ToInt16(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToInt16(value));
    }

    public static IObservableAsync<int> ToInt32(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToInt32(value));
    }

    public static IObservableAsync<long> ToInt64(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToInt64(value));
    }

    public static IObservableAsync<float> ToSingle(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToSingle(value));
    }

    public static IObservableAsync<double> ToDouble(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToDouble(value));
    }

    public static IObservableAsync<string?> ToString(this IObservableAsync<object?> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);
        return observable.Select(static value => Convert.ToString(value));
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservableAsync<IMqttClient> client, params string[] topics)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.SubscribeToTopics(client.ToObservable(), topics).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservableAsync<IResilientMqttClient> client, params string[] topics)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.SubscribeToTopics(client.ToObservable(), topics).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservableAsync<IMqttClient> client, string topic)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.SubscribeToTopic(client.ToObservable(), topic).ToObservableAsync();
    }

    public static IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservableAsync<IMqttClient> client, TimeSpan? topicExpiry = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.DiscoverTopics(client.ToObservable(), topicExpiry).ToObservableAsync();
    }

    public static IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservableAsync<IResilientMqttClient> client, TimeSpan? topicExpiry = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.DiscoverTopics(client.ToObservable(), topicExpiry).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservableAsync<IResilientMqttClient> client, string topic)
    {
        ArgumentNullException.ThrowIfNull(client);
        return MqttdSubscribeExtensions.SubscribeToTopic(client.ToObservable(), topic).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> observable, string topic)
    {
        ArgumentNullException.ThrowIfNull(observable);
        ArgumentNullException.ThrowIfNull(topic);

        var isValidTopics = new Dictionary<string, bool>(StringComparer.Ordinal);
        return observable.Where(x =>
        {
            var incomingTopic = x.ApplicationMessage.Topic;
            if (!isValidTopics.TryGetValue(incomingTopic, out var isValid))
            {
                isValid = MqttTopicFilterComparer.Compare(x.ApplicationMessage.Topic, topic) == MqttTopicFilterCompareResult.IsMatch;
                isValidTopics[incomingTopic] = isValid;
            }

            return isValid;
        }).Retry();
    }

    private static MqttApplicationMessage BuildMessage(string topic, string payload, MqttQualityOfServiceLevel qos, bool retain) =>
        Create.MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

    private static MqttApplicationMessage BuildMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain) =>
        Create.MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

    private static MqttApplicationMessage BuildMessage(string topic, string payload, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain)
    {
        var builder = Create.MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);
        messageBuilder(builder);
        return builder.Build();
    }

    private static MqttApplicationMessage BuildMessage(string topic, byte[] payload, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain)
    {
        var builder = Create.MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);
        messageBuilder(builder);
        return builder.Build();
    }

    private static object? DeserializeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                {
                    return l;
                }

                if (element.TryGetDouble(out var d))
                {
                    return d;
                }

                return null;
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(DeserializeJsonElement).ToArray();
            case JsonValueKind.Object:
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    result[property.Name] = DeserializeJsonElement(property.Value);
                }

                return result;
            default:
                return null;
        }
    }
}
