// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;
using RxLinq = System.Reactive.Linq;

namespace MQTTnet.Rx.Client;

/// <summary>Provides asynchronous observable counterparts for classic observable extension APIs.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Provides extensions for received MQTT application message observables.</summary>
    /// <param name="source">The received MQTT message stream.</param>
    extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Projects each message payload as a UTF-8 string.</summary>
        /// <returns>The UTF-8 payload observable.</returns>
        public IObservableAsync<string> ToUtf8String()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static message => message.PayloadUtf8());
        }

        /// <summary>Filters messages by an MQTT topic filter.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The messages whose topics match the filter.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch(
            string topic)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topic);
            return MqttdSubscribeExtensions
                .WhereTopicIsMatch(source.ToObservable(), topic)
                .ToSignal();
        }

        /// <summary>Filters messages that match at least one MQTT topic filter.</summary>
        /// <param name="topicFilters">The MQTT topic filters.</param>
        /// <returns>The matching message observable.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny(
            params string[] topicFilters)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicFilters);

            return topicFilters.Length switch
            {
                0 => SignalAsync.Empty<MqttApplicationMessageReceivedEventArgs>(),
                1 => FilterTopicMatches(source, topicFilters[0]),
                _ => source.Where(message =>
                    MatchesAnyTopic(message.ApplicationMessage.Topic, topicFilters)),
            };
        }

        /// <summary>Filters messages whose topics do not match an MQTT topic filter.</summary>
        /// <param name="topicFilter">The MQTT topic filter to exclude.</param>
        /// <returns>The non-matching message observable.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch(
            string topicFilter)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicFilter);
            return source.Where(message =>
                MqttTopicFilterComparer.Compare(message.ApplicationMessage.Topic, topicFilter)
                != MqttTopicFilterCompareResult.IsMatch);
        }

        /// <summary>Extracts named topic placeholder values from matching messages.</summary>
        /// <param name="topicPattern">The topic pattern containing named placeholders.</param>
        /// <returns>The messages and their extracted values.</returns>
        public IObservableAsync<(
            MqttApplicationMessageReceivedEventArgs Message,
            Dictionary<string, string> Values)> ExtractTopicValues(string topicPattern)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicPattern);

            var patternSegments = CreateTopicPatternSegments(topicPattern);
            return source
                .Select(message => TryExtractTopicValues(message, patternSegments))
                .Where(static result => result is not null)
                .Select(static result => result!.Value);
        }

        /// <summary>Filters messages whose topics contain exactly the specified number of levels.</summary>
        /// <param name="levelCount">The required number of topic levels.</param>
        /// <returns>The matching message observable.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount(
            int levelCount)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Where(message =>
                CountTopicLevels(message.ApplicationMessage.Topic) == levelCount);
        }

        /// <summary>Projects messages to their topic level at the specified zero-based index.</summary>
        /// <param name="levelIndex">The zero-based level index.</param>
        /// <returns>The topic-level observable.</returns>
        public IObservableAsync<string> SelectTopicLevel(int levelIndex)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source
                .Select(message => GetTopicLevel(message.ApplicationMessage.Topic, levelIndex))
                .Where(static level => level is not null)
                .Select(static level => level!);
        }

        /// <summary>Groups messages by topic.</summary>
        /// <returns>The grouped message observable.</returns>
        public IObservableAsync<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopic()
        {
            ArgumentNullException.ThrowIfNull(source);
            return TopicFilterExtensions.GroupByTopic(source.ToObservable()).ToSignal();
        }

        /// <summary>Groups messages by the specified topic level.</summary>
        /// <param name="levelIndex">The zero-based level index.</param>
        /// <returns>The grouped message observable.</returns>
        public IObservableAsync<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopicLevel(int levelIndex)
        {
            ArgumentNullException.ThrowIfNull(source);
            return TopicFilterExtensions
                .GroupByTopicLevel(source.ToObservable(), levelIndex)
                .ToSignal();
        }

        /// <summary>Parses each JSON-object payload into a dictionary.</summary>
        /// <returns>The dictionary observable.</returns>
        public IObservableAsync<Dictionary<string, object?>?> ToDictionary()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static message =>
                ParsePayloadDictionary(message.ApplicationMessage.ConvertPayloadToString()));
        }

        /// <summary>Deserializes each message payload with source-generated JSON metadata.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="jsonTypeInfo">The source-generated JSON metadata for the payload type.</param>
        /// <returns>The deserialized payload observable.</returns>
        public IObservableAsync<T?> ToObject<T>(JsonTypeInfo<T> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            return source.Select(message =>
                DeserializePayload(
                    message.ApplicationMessage.ConvertPayloadToString(),
                    jsonTypeInfo));
        }

        /// <summary>Deserializes each message payload with a caller-provided typed converter.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="deserialize">The converter that deserializes a JSON payload.</param>
        /// <returns>The deserialized payload observable.</returns>
        public IObservableAsync<T?> ToObject<T>(Func<string, T?> deserialize)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(deserialize);
            return source.Select(message =>
                DeserializePayload(message.ApplicationMessage.ConvertPayloadToString(), deserialize));
        }
    }
}

/// <summary>Provides asynchronous observable counterparts for classic observable extension APIs.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Provides dictionary-observation extensions.</summary>
    /// <param name="dictionary">The dictionary stream.</param>
    extension(IObservableAsync<Dictionary<string, object>> dictionary)
    {
        /// <summary>Observes values associated with a key in each dictionary.</summary>
        /// <param name="key">The dictionary key.</param>
        /// <returns>The observed values.</returns>
        public IObservableAsync<object?> Observe(string key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            return MqttdSubscribeExtensions.Observe(dictionary.ToObservable(), key).ToSignal();
        }
    }

    /// <summary>Provides conversion extensions for object observables.</summary>
    /// <param name="observable">The object stream.</param>
    extension(IObservableAsync<object?> observable)
    {
        /// <summary>Converts each value to Boolean.</summary>
        /// <returns>The Boolean observable.</returns>
        public IObservableAsync<bool> ToBool()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToBoolean);
        }

        /// <summary>Converts each value to Byte.</summary>
        /// <returns>The Byte observable.</returns>
        public IObservableAsync<byte> ToByte()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToByte);
        }

        /// <summary>Converts each value to Int16.</summary>
        /// <returns>The Int16 observable.</returns>
        public IObservableAsync<short> ToInt16()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToInt16);
        }

        /// <summary>Converts each value to Int32.</summary>
        /// <returns>The Int32 observable.</returns>
        public IObservableAsync<int> ToInt32()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToInt32);
        }

        /// <summary>Converts each value to Int64.</summary>
        /// <returns>The Int64 observable.</returns>
        public IObservableAsync<long> ToInt64()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToInt64);
        }

        /// <summary>Converts each value to Single.</summary>
        /// <returns>The Single observable.</returns>
        public IObservableAsync<float> ToSingle()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToSingle);
        }

        /// <summary>Converts each value to Double.</summary>
        /// <returns>The Double observable.</returns>
        public IObservableAsync<double> ToDouble()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToDouble);
        }

        /// <summary>Converts each value to String.</summary>
        /// <returns>The String observable.</returns>
        public IObservableAsync<string?> ToString()
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Select(Convert.ToString);
        }
    }

    /// <summary>Parses a payload JSON object into a dictionary.</summary>
    /// <param name="json">The JSON payload.</param>
    /// <returns>The parsed dictionary, or <see langword="null"/>.</returns>
    private static Dictionary<string, object?>? ParsePayloadDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = DeserializeJsonElement(property.Value);
            }

            return result;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Deserializes a payload and returns default when it is invalid JSON.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="json">The JSON payload.</param>
        /// <param name="jsonTypeInfo">The source-generated JSON metadata.</param>
        /// <returns>The deserialized value, or the default value.</returns>
    private static T? DeserializePayload<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
    {
        try
        {
            return JsonSerializer.Deserialize(json, jsonTypeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>Deserializes a payload with a typed converter.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="json">The JSON payload.</param>
    /// <param name="deserialize">The typed converter.</param>
    /// <returns>The converted value, or the default value.</returns>
    private static T? DeserializePayload<T>(string json, Func<string, T?> deserialize)
    {
        try
        {
            return deserialize(json);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>Builds a string-payload MQTT application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The string payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the message is retained.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage BuildMessage(
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

    /// <summary>Builds a byte-array-payload MQTT application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The byte-array payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the message is retained.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage BuildMessage(
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos,
        bool retain) =>
        Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

    /// <summary>Builds a configured string-payload MQTT application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The string payload.</param>
    /// <param name="messageBuilder">The message configuration callback.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the message is retained.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage BuildMessage(
        string topic,
        string payload,
        Action<MqttApplicationMessageBuilder> messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain)
    {
        var builder = Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);
        messageBuilder(builder);
        return builder.Build();
    }

    /// <summary>Builds a configured byte-array-payload MQTT application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The byte-array payload.</param>
    /// <param name="messageBuilder">The message configuration callback.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the message is retained.</param>
    /// <returns>The configured MQTT application message.</returns>
    private static MqttApplicationMessage BuildMessage(
        string topic,
        byte[] payload,
        Action<MqttApplicationMessageBuilder> messageBuilder,
        MqttQualityOfServiceLevel qos,
        bool retain)
    {
        var builder = Create
            .MqttFactory.CreateApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain);
        messageBuilder(builder);
        return builder.Build();
    }

    /// <summary>Converts a JSON element to a scalar, array, dictionary, or null.</summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The converted value.</returns>
    private static object? DeserializeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                return element.TryGetInt64(out var integer) ? (object)integer : element.GetDouble();

            case JsonValueKind.True
            or JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Array:
            {
                var items = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    items.Add(DeserializeJsonElement(item));
                }

                return items.ToArray();
            }

            case JsonValueKind.Object:
            {
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    result[property.Name] = DeserializeJsonElement(property.Value);
                }

                return result;
            }

            default:
                return null;
        }
    }
}
