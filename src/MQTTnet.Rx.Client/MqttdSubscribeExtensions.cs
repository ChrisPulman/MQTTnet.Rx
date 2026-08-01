// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides reactive MQTT topic subscription, discovery, and payload conversion extensions.</summary>
/// <remarks>These extensions simplify working with MQTT message streams by enabling topic-based subscriptions,
/// JSON deserialization, and type conversions using reactive programming patterns. Methods are designed to avoid
/// duplicate broker subscriptions by reference counting per client and topic. Thread safety is maintained for shared
/// resources. All methods return observables that can be composed with other reactive operators. Exceptions may be
/// thrown for invalid arguments or deserialization failures; see individual method documentation for details.</remarks>
public static partial class MqttdSubscribeExtensions
{
    /// <summary>Defines the longest interval between topic-expiry cleanup checks.</summary>
    private static readonly TimeSpan MaximumTopicCleanupInterval = TimeSpan.FromMinutes(1);

#if NET9_0_OR_GREATER
    /// <summary>Synchronizes access to shared subscription state.</summary>
    private static readonly Lock Sync = new();
#else
    /// <summary>Synchronizes access to shared subscription state.</summary>
    private static readonly object Sync = new();
#endif

    /// <summary>Caches observed JSON values by key.</summary>
    private static readonly Dictionary<string, IObservable<object?>> DictJsonValues = [];

    /// <summary>Provides value-observation extensions for dictionary message streams.</summary>
    /// <param name="dictionary">The dictionary message stream.</param>
    extension(IObservable<Dictionary<string, object>> dictionary)
    {
        /// <summary>Observes values associated with a dictionary key.</summary>
        /// <remarks>The returned observable replays the most recent value for the specified key to new
        /// subscribers. If the key is not present in a dictionary, no value is emitted for that dictionary. The
        /// observable
        /// will automatically retry on error.</remarks>
        /// <param name="key">The key whose associated values are to be observed in each dictionary. Cannot be
        /// null.</param>
        /// <returns>An observable sequence that emits the value associated with the specified key each time it appears
        /// in the source
        /// sequence. Emits <see langword="null"/> if the value is null.</returns>
        public IObservable<object?> Observe(string key)
        {
            _ = DictJsonValues.TryGetValue(key, out var observable);

            if (observable is null)
            {
                var replay = new ReplaySignal<object?>(1);
                _ = dictionary.Where(x => x.ContainsKey(key)).Select(x => x[key]).Subscribe(replay);
                observable = replay;
                _ = DictJsonValues.TryAdd(key, observable);
            }

            return observable.Retry();
        }
    }

    /// <summary>Provides topic subscription and discovery extensions for raw MQTT clients.</summary>
    /// <param name="client">The raw MQTT client stream.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Subscribes each client to the specified topics.</summary>
        /// <param name="topics">The MQTT topic filters.</param>
        /// <returns>The merged received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(
            params string[] topics)
        {
            ArgumentNullException.ThrowIfNull(topics);
            var subscriptions = new IObservable<MqttApplicationMessageReceivedEventArgs>[
                topics.Length];
            for (var index = 0; index < topics.Length; index++)
            {
                subscriptions[index] = client.SubscribeToTopic(topics[index]);
            }

            return subscriptions.Merge();
        }

        /// <summary>Subscribes each client to one MQTT topic.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(
            string topic) => client.SelectMany(new RawTopicSubscription(topic).Create).Retry().Publish().RefCount();

        /// <summary>Discovers active MQTT topics using the default one-hour expiry.</summary>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics() =>
            client.DiscoverTopics(TimeSpan.FromHours(1), TimeProvider.System);

        /// <summary>Discovers active MQTT topics.</summary>
        /// <param name="topicExpiry">The inactivity duration before a topic expires.</param>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry) => client.DiscoverTopics(topicExpiry, TimeProvider.System);

        /// <summary>Discovers active MQTT topics using a caller-supplied clock.</summary>
        /// <param name="topicExpiry">The inactivity duration before a topic expires.</param>
        /// <param name="timeProvider">The clock used for last-seen and expiry times.</param>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            return DiscoverTopicsCore(
                client
                    .SubscribeToTopic("#")
                    .Select(static message => message.ApplicationMessage.Topic),
                topicExpiry,
                timeProvider);
        }
    }

    /// <summary>Provides topic subscription and discovery extensions for resilient MQTT clients.</summary>
    /// <param name="client">The resilient MQTT client stream.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Subscribes each client to the specified topics.</summary>
        /// <param name="topics">The MQTT topic filters.</param>
        /// <returns>The merged received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(
            params string[] topics)
        {
            ArgumentNullException.ThrowIfNull(topics);
            var subscriptions = new IObservable<MqttApplicationMessageReceivedEventArgs>[
                topics.Length];
            for (var index = 0; index < topics.Length; index++)
            {
                subscriptions[index] = client.SubscribeToTopic(topics[index]);
            }

            return subscriptions.Merge();
        }

        /// <summary>Subscribes each client to one MQTT topic.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(
            string topic) =>
            client
                .SelectMany(new ResilientTopicSubscription(topic).Create)
                .Retry()
                .Publish()
                .RefCount();

        /// <summary>Discovers active MQTT topics using the default one-hour expiry.</summary>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics() =>
            client.DiscoverTopics(TimeSpan.FromHours(1), TimeProvider.System);

        /// <summary>Discovers active MQTT topics.</summary>
        /// <param name="topicExpiry">The inactivity duration before a topic expires.</param>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry) => client.DiscoverTopics(topicExpiry, TimeProvider.System);

        /// <summary>Discovers active MQTT topics using a caller-supplied clock.</summary>
        /// <param name="topicExpiry">The inactivity duration before a topic expires.</param>
        /// <param name="timeProvider">The clock used for last-seen and expiry times.</param>
        /// <returns>The active topics and their last-seen times.</returns>
        public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(
            TimeSpan? topicExpiry,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            return DiscoverTopicsCore(
                client
                    .SubscribeToTopic("#")
                    .Select(static message => message.ApplicationMessage.Topic),
                topicExpiry,
                timeProvider);
        }
    }

    /// <summary>Provides payload conversion extensions for received MQTT messages.</summary>
    /// <param name="message">The received MQTT message stream.</param>
    extension(IObservable<MqttApplicationMessageReceivedEventArgs> message)
    {
        /// <summary>Deserializes JSON object payloads into dictionaries.</summary>
        /// <remarks>If a message payload is not valid JSON or is empty, the resulting dictionary will be <see
        /// langword="null"/>. The returned observable retries on errors encountered during message
        /// processing.</remarks>
        /// <returns>An observable sequence of dictionaries containing the deserialized JSON payloads from each received
        /// MQTT
        /// application message. The dictionary is <see langword="null"/> if the payload is empty or cannot be
        /// deserialized.</returns>
        public IObservable<Dictionary<string, object?>?> ToDictionary() =>
            Signal
                .Create<Dictionary<string, object?>?>(observer =>
                    message
                        .Retry()
                        .Subscribe(m =>
                        {
                            var json = m.ApplicationMessage.ConvertPayloadToString();
                            if (string.IsNullOrWhiteSpace(json))
                            {
                                observer.OnNext(null);
                                return;
                            }

                            try
                            {
                                using var doc = JsonDocument.Parse(json);
                                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                                {
                                    observer.OnNext(null);
                                    return;
                                }

                                var result = new Dictionary<string, object?>(
                                    StringComparer.Ordinal);
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    result[prop.Name] = DeserializeJsonElement(prop.Value);
                                }

                                observer.OnNext(result);
                            }
                            catch
                            {
                                observer.OnNext(null);
                            }
                        }))
                .Retry();

        /// <summary>Deserializes each message payload using source-generated JSON metadata.</summary>
        /// <remarks>The payload of each MQTT message is expected to be a valid JSON string representing an object
        /// of type T. If the payload cannot be deserialized to type T, the resulting value will be null. This method
        /// uses
        /// System.Text.Json for deserialization.</remarks>
        /// <typeparam name="T">The type to which the message payload is deserialized.</typeparam>
        /// <param name="jsonTypeInfo">The source-generated JSON metadata for the payload type.</param>
        /// <returns>An observable sequence containing each deserialized payload, or the default value when
        /// deserialization fails.</returns>
        public IObservable<T?> ToObject<T>(JsonTypeInfo<T> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            return message.Select(m =>
            {
                try
                {
                    return JsonSerializer.Deserialize(
                        m.ApplicationMessage.ConvertPayloadToString(),
                        jsonTypeInfo);
                }
                catch (JsonException)
                {
                    return default;
                }
            });
        }

        /// <summary>Deserializes each message payload using a caller-provided typed converter.</summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="deserialize">The converter that deserializes a JSON payload.</param>
        /// <returns>The deserialized payload sequence.</returns>
        public IObservable<T?> ToObject<T>(Func<string, T?> deserialize)
        {
            ArgumentNullException.ThrowIfNull(deserialize);
            return message.Select(m =>
            {
                try
                {
                    return deserialize(m.ApplicationMessage.ConvertPayloadToString());
                }
                catch (JsonException)
                {
                    return default;
                }
            });
        }

        /// <summary>Filters messages by an MQTT topic filter.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The messages whose topics match the filter.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch(
            string topic) => new TopicFilter(topic).Apply(message);
    }

    /// <summary>Provides conversion extensions for object sequences.</summary>
    /// <param name="observable">The object sequence to convert.</param>
    extension(IObservable<object?> observable)
    {
        /// <summary>Projects each element of an observable sequence to its Boolean representation.</summary>
        /// <remarks>The conversion uses <see cref="Convert.ToBoolean(object?)"/>. If an element in the source
        /// sequence cannot be converted to a Boolean, an exception will be propagated to observers.</remarks>
        /// <returns>An observable sequence of Boolean values, where each value is the result of converting the
        /// corresponding element
        /// in the source sequence to a Boolean.</returns>
        public IObservable<bool> ToBool() => observable.Select(Convert.ToBoolean);

        /// <summary>Projects each element of an observable sequence to a byte value.</summary>
        /// <remarks>If an element in the source sequence cannot be converted to a byte, the resulting observable
        /// will propagate the exception to its observers. This method is typically used when the source sequence
        /// contains
        /// numeric or convertible values represented as objects.</remarks>
        /// <returns>An observable sequence of byte values resulting from converting each element of the source
        /// sequence.</returns>
        public IObservable<byte> ToByte() => observable.Select(Convert.ToByte);

        /// <summary>Projects each element of an observable sequence to a 16-bit signed integer.</summary>
        /// <remarks>If an element in the source sequence cannot be converted to Int16, an exception will be
        /// propagated to observers. This method uses Convert.ToInt16 for the conversion, which supports a variety of
        /// input
        /// types including numeric types and strings that represent numbers.</remarks>
        /// <returns>An observable sequence of 16-bit signed integers resulting from converting each element of the
        /// source sequence.</returns>
        public IObservable<short> ToInt16() => observable.Select(Convert.ToInt16);

        /// <summary>Projects each element of an observable sequence to a 32-bit signed integer.</summary>
        /// <remarks>If an element in the source sequence is null or cannot be converted to an integer, the
        /// resulting observable will propagate the corresponding exception to its observers.</remarks>
        /// <returns>An observable sequence of 32-bit signed integers resulting from converting each element of the
        /// source sequence.</returns>
        public IObservable<int> ToInt32() => observable.Select(Convert.ToInt32);

        /// <summary>Projects each element of an observable sequence to a 64-bit signed integer.</summary>
        /// <remarks>If an element in the source sequence cannot be converted to a 64-bit signed integer, the
        /// resulting observable will signal an error. The conversion uses System.Convert.ToInt64, which supports
        /// standard
        /// conversions for numeric and string types.</remarks>
        /// <returns>An observable sequence of 64-bit signed integers resulting from converting each element of the
        /// source sequence.</returns>
        public IObservable<long> ToInt64() => observable.Select(Convert.ToInt64);

        /// <summary>Converts each value to Single.</summary>
        /// <remarks>If an element in the source sequence cannot be converted to <see cref="float"/>, an
        /// exception will be propagated to observers. This method uses <see cref="Convert.ToSingle(object?)"/> for
        /// conversion.</remarks>
        /// <returns>An observable sequence of single-precision floating-point numbers obtained by converting each
        /// element of the
        /// source sequence.</returns>
        public IObservable<float> ToSingle() => observable.Select(Convert.ToSingle);

        /// <summary>Converts each value to Double.</summary>
        /// <remarks>If an element in the source sequence cannot be converted to a double, an exception is
        /// propagated to observers. This method uses <see cref="Convert.ToDouble(object?)"/> for conversion, which may
        /// throw exceptions for invalid or null values.</remarks>
        /// <returns>An observable sequence of double values resulting from converting each element of the source
        /// sequence.</returns>
        public IObservable<double> ToDouble() => observable.Select(Convert.ToDouble);

        /// <summary>Projects each element of an observable sequence to its string representation.</summary>
        /// <returns>An observable sequence of strings, where each element is the string representation of the
        /// corresponding element
        /// in the source sequence. Returns null for elements that are null.</returns>
        public IObservable<string?> ToString() => observable.Select(Convert.ToString);
    }

    /// <summary>Builds the active-topic sequence from received topic names.</summary>
    /// <param name="topicNames">The received topic-name sequence.</param>
    /// <param name="topicExpiry">The inactivity duration before a topic expires.</param>
    /// <param name="timeProvider">The clock used for last-seen and expiry times.</param>
    /// <returns>The active topics and their last-seen times.</returns>
    private static IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopicsCore(
        IObservable<string> topicNames,
        TimeSpan? topicExpiry,
        TimeProvider timeProvider)
    {
        var expiry = topicExpiry ?? TimeSpan.FromHours(1);
        if (expiry < TimeSpan.FromSeconds(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(topicExpiry),
                "Topic expiry must be greater or equal to one.");
        }

        var cleanupInterval = expiry < MaximumTopicCleanupInterval
            ? expiry
            : MaximumTopicCleanupInterval;

        return Signal
            .Create<IEnumerable<(string Topic, DateTime LastSeen)>>(observer =>
            {
                var state = new TopicDiscoveryState(observer, expiry, timeProvider);
                return topicNames
                    .Merge(
                        Signal.Interval(cleanupInterval).Select(static _ => string.Empty))
                    .Subscribe(state.OnTopic);
            })
            .Retry()
            .Publish()
            .RefCount();
    }

    /// <summary>Converts a JSON element to its corresponding CLR representation.</summary>
    /// <param name="element">The JSON element to convert.</param>
    /// <returns>The converted CLR representation.</returns>
    private static object? DeserializeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
            {
                return element.TryGetInt64(out var integerValue)
                    ? integerValue
                    : (object)element.GetDouble();
            }

            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Object:
            {
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = DeserializeJsonElement(prop.Value);
                }

                return dict;
            }

            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(DeserializeJsonElement(item));
                }

                return list;
            }

            default:
                return null;
        }
    }
}
