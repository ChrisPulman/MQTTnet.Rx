// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for subscribing to MQTT topics, processing message streams, and deserializing payloads in
/// observable sequences for both raw and resilient MQTT clients.
/// </summary>
/// <remarks>These extensions simplify working with MQTT message streams by enabling topic-based subscriptions,
/// JSON deserialization, and type conversions using reactive programming patterns. Methods are designed to avoid
/// duplicate broker subscriptions by reference counting per client and topic. Thread safety is maintained for shared
/// resources. All methods return observables that can be composed with other reactive operators. Exceptions may be
/// thrown for invalid arguments or deserialization failures; see individual method documentation for details.</remarks>
public static class MqttdSubscribeExtensions
{
    private static readonly object _sync = new();
    private static readonly Dictionary<string, IObservable<object?>> _dictJsonValues = [];
    private static readonly Dictionary<IResilientMqttClient, Dictionary<string, SubscriptionHub>> _resilientTopicHubs = [];
    private static readonly Dictionary<IMqttClient, Dictionary<string, SubscriptionHub>> _rawTopicHubs = [];

    /// <summary>
    /// Converts an observable sequence of MQTT application message received events into an observable sequence of
    /// dictionaries representing the deserialized JSON payloads.
    /// </summary>
    /// <remarks>If a message payload is not valid JSON or is empty, the resulting dictionary will be <see
    /// langword="null"/>. The returned observable retries on errors encountered during message processing.</remarks>
    /// <param name="message">The observable sequence of <see cref="MqttApplicationMessageReceivedEventArgs"/> instances to convert. Each
    /// event's payload is expected to be a JSON-encoded object.</param>
    /// <returns>An observable sequence of dictionaries containing the deserialized JSON payloads from each received MQTT
    /// application message. The dictionary is <see langword="null"/> if the payload is empty or cannot be deserialized.</returns>
    public static IObservable<Dictionary<string, object>?> ToDictionary(this IObservable<MqttApplicationMessageReceivedEventArgs> message) =>
        Observable.Create<Dictionary<string, object>?>(observer => message.Retry().Subscribe(m => observer.OnNext(JsonConvert.DeserializeObject<Dictionary<string, object>?>(m.ApplicationMessage.ConvertPayloadToString())))).Retry();

    /// <summary>
    /// Deserializes the payload of each received MQTT application message to an object of type T using JSON
    /// deserialization.
    /// </summary>
    /// <remarks>The payload of each MQTT message is expected to be a valid JSON string representing an object
    /// of type T. If the payload cannot be deserialized to type T, the resulting value will be null. This method uses
    /// Newtonsoft.Json for deserialization.</remarks>
    /// <typeparam name="T">The type to which the message payload is deserialized.</typeparam>
    /// <param name="message">An observable sequence of MQTT application message received event arguments whose payloads will be deserialized.</param>
    /// <param name="settings">Optional JSON serializer settings to use during deserialization. If null, default settings are applied.</param>
    /// <returns>An observable sequence of objects of type T, where each item represents the deserialized payload of a received
    /// message. If deserialization fails, the result is null.</returns>
    public static IObservable<T?> ToObject<T>(this IObservable<MqttApplicationMessageReceivedEventArgs> message, JsonSerializerSettings? settings = null) =>
        message.Select(m =>
        {
            var json = m.ApplicationMessage.ConvertPayloadToString();
            return settings is null ? JsonConvert.DeserializeObject<T>(json) : JsonConvert.DeserializeObject<T>(json, settings);
        });

    /// <summary>
    /// Returns an observable sequence that emits the values associated with the specified key from the source
    /// dictionary observable.
    /// </summary>
    /// <remarks>The returned observable replays the most recent value for the specified key to new
    /// subscribers. If the key is not present in a dictionary, no value is emitted for that dictionary. The observable
    /// will automatically retry on error.</remarks>
    /// <param name="dictionary">The source observable sequence of dictionaries to monitor for changes.</param>
    /// <param name="key">The key whose associated values are to be observed in each dictionary. Cannot be null.</param>
    /// <returns>An observable sequence that emits the value associated with the specified key each time it appears in the source
    /// sequence. Emits <see langword="null"/> if the value is null.</returns>
    public static IObservable<object?> Observe(this IObservable<Dictionary<string, object>> dictionary, string key)
    {
        _dictJsonValues.TryGetValue(key, out var observable);

        if (observable is null)
        {
            var replay = new ReplaySubject<object?>(1);
            dictionary.Where(x => x.ContainsKey(key)).Select(x => x[key]).Subscribe(replay);
            observable = replay.AsObservable();
            _dictJsonValues.Add(key, observable);
        }

        return observable.Retry();
    }

    /// <summary>
    /// Projects each element of an observable sequence to its Boolean representation.
    /// </summary>
    /// <remarks>The conversion uses <see cref="Convert.ToBoolean(object?)"/>. If an element in the source
    /// sequence cannot be converted to a Boolean, an exception will be propagated to observers.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to Boolean values.</param>
    /// <returns>An observable sequence of Boolean values, where each value is the result of converting the corresponding element
    /// in the source sequence to a Boolean.</returns>
    public static IObservable<bool> ToBool(this IObservable<object?> observable) => observable.Select(Convert.ToBoolean);

    /// <summary>
    /// Projects each element of an observable sequence to a byte value by converting each element using <see
    /// cref="Convert.ToByte(object?)"/>.
    /// </summary>
    /// <remarks>If an element in the source sequence cannot be converted to a byte, the resulting observable
    /// will propagate the exception to its observers. This method is typically used when the source sequence contains
    /// numeric or convertible values represented as objects.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to byte values. Each element must be convertible to <see
    /// langword="byte"/>; otherwise, an exception is thrown.</param>
    /// <returns>An observable sequence of byte values resulting from converting each element of the source sequence.</returns>
    public static IObservable<byte> ToByte(this IObservable<object?> observable) => observable.Select(Convert.ToByte);

    /// <summary>
    /// Projects each element of an observable sequence to a 16-bit signed integer by converting each value to an Int16.
    /// </summary>
    /// <remarks>If an element in the source sequence cannot be converted to Int16, an exception will be
    /// propagated to observers. This method uses Convert.ToInt16 for the conversion, which supports a variety of input
    /// types including numeric types and strings that represent numbers.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to 16-bit signed integers. Each element must be convertible
    /// to Int16.</param>
    /// <returns>An observable sequence of 16-bit signed integers resulting from converting each element of the source sequence.</returns>
    public static IObservable<short> ToInt16(this IObservable<object?> observable) => observable.Select(Convert.ToInt16);

    /// <summary>
    /// Projects each element of an observable sequence to its 32-bit signed integer representation.
    /// </summary>
    /// <remarks>If an element in the source sequence is null or cannot be converted to an integer, the
    /// resulting observable will propagate the corresponding exception to its observers.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to 32-bit signed integers. Each element must be convertible
    /// to an integer using Convert.ToInt32; otherwise, an exception is thrown.</param>
    /// <returns>An observable sequence of 32-bit signed integers resulting from converting each element of the source sequence.</returns>
    public static IObservable<int> ToInt32(this IObservable<object?> observable) => observable.Select(Convert.ToInt32);

    /// <summary>
    /// Projects each element of an observable sequence to a 64-bit signed integer by converting each value to an Int64.
    /// </summary>
    /// <remarks>If an element in the source sequence cannot be converted to a 64-bit signed integer, the
    /// resulting observable will signal an error. The conversion uses System.Convert.ToInt64, which supports standard
    /// conversions for numeric and string types.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to 64-bit signed integers. Cannot be null.</param>
    /// <returns>An observable sequence of 64-bit signed integers resulting from converting each element of the source sequence.</returns>
    public static IObservable<long> ToInt64(this IObservable<object?> observable) => observable.Select(Convert.ToInt64);

    /// <summary>
    /// Projects each element of an observable sequence to a single-precision floating-point number.
    /// </summary>
    /// <remarks>If an element in the source sequence cannot be converted to <see cref="float"/>, an
    /// exception will be propagated to observers. This method uses <see cref="System.Convert.ToSingle(object?)"/> for
    /// conversion.</remarks>
    /// <param name="observable">The source sequence whose elements will be converted to <see cref="float"/> values. Each element must be
    /// convertible to <see cref="float"/>.</param>
    /// <returns>An observable sequence of single-precision floating-point numbers obtained by converting each element of the
    /// source sequence.</returns>
    public static IObservable<float> ToSingle(this IObservable<object?> observable) => observable.Select(Convert.ToSingle);

    /// <summary>
    /// Projects each element of an observable sequence to a double-precision floating-point number.
    /// </summary>
    /// <remarks>If an element in the source sequence cannot be converted to a double, an exception is
    /// propagated to observers. This method uses <see cref="Convert.ToDouble(object?)"/> for conversion, which may
    /// throw exceptions for invalid or null values.</remarks>
    /// <param name="observable">The source sequence whose elements are to be converted to double values. Each element must be convertible to a
    /// double using <see cref="Convert.ToDouble(object?)"/>.</param>
    /// <returns>An observable sequence of double values resulting from converting each element of the source sequence.</returns>
    public static IObservable<double> ToDouble(this IObservable<object?> observable) => observable.Select(Convert.ToDouble);

    /// <summary>
    /// Projects each element of an observable sequence to its string representation.
    /// </summary>
    /// <param name="observable">The source sequence of objects to convert to strings. Cannot be null.</param>
    /// <returns>An observable sequence of strings, where each element is the string representation of the corresponding element
    /// in the source sequence. Returns null for elements that are null.</returns>
    public static IObservable<string?> ToString(this IObservable<object?> observable) => observable.Select(Convert.ToString);

    /// <summary>
    /// Subscribes each MQTT client in the observable sequence to the specified topics and returns a merged observable
    /// of received application messages.
    /// </summary>
    /// <param name="client">An observable sequence of MQTT clients to subscribe to the specified topics.</param>
    /// <param name="topics">An array of topic filters to which each client will subscribe. Each topic must be a valid MQTT topic string.</param>
    /// <returns>An observable sequence that emits application message received events from all subscribed topics across all
    /// clients.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservable<IMqttClient> client, params string[] topics) =>
        topics.Select(t => client.SubscribeToTopic(t)).Merge();

    /// <summary>
    /// Subscribes the MQTT client to multiple topics and returns an observable sequence of received application
    /// messages.
    /// </summary>
    /// <remarks>The returned observable merges message streams from all specified topics. Subscribing to the
    /// same topic multiple times may result in duplicate messages, depending on the MQTT broker's behavior.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients to use for subscribing to topics.</param>
    /// <param name="topics">An array of topic filters to subscribe to. Each topic specifies a filter for messages to receive.</param>
    /// <returns>An observable sequence that emits an event argument object each time a message is received on any of the
    /// subscribed topics.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservable<IResilientMqttClient> client, params string[] topics) =>
        topics.Select(t => client.SubscribeToTopic(t)).Merge();

    /// <summary>
    /// Subscribes to the specified MQTT topic for each client in the observable sequence and returns an observable
    /// sequence of received application messages matching that topic.
    /// </summary>
    /// <remarks>The subscription is reference-counted: the MQTT broker subscription for a given client and
    /// topic is established when the first observer subscribes, and is removed when the last observer unsubscribes.
    /// Multiple observers to the same topic and client share a single broker subscription and message stream. The
    /// returned observable is hot and will replay messages to all active subscribers. If the client disconnects or an
    /// error occurs, the observable will automatically retry the subscription.</remarks>
    /// <param name="client">An observable sequence of connected MQTT clients to subscribe to the topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Must be a valid topic string supported by the MQTT broker.</param>
    /// <returns>An observable sequence that emits an event each time an application message is received on the specified topic
    /// by any of the subscribed clients.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservable<IMqttClient> client, string topic) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            IMqttClient? mqttClient = null;

            var inner = client.Subscribe(async c =>
            {
                mqttClient = c;
                SubscriptionHub? hub = null;
                var needSubscribe = false;
                lock (_sync)
                {
                    if (!_rawTopicHubs.TryGetValue(c, out var topicMap))
                    {
                        topicMap = new Dictionary<string, SubscriptionHub>(StringComparer.Ordinal);
                        _rawTopicHubs[c] = topicMap;
                    }

                    if (!topicMap.TryGetValue(topic, out hub))
                    {
                        hub = new SubscriptionHub();
                        topicMap[topic] = hub;
                    }

                    hub.Count++;
                    if (hub.Count == 1)
                    {
                        // First subscriber for this client/topic: set up a single tap and request broker subscription
                        hub.SourceTap = c.ApplicationMessageReceived().WhereTopicIsMatch(topic).Subscribe(hub.Subject);
                        needSubscribe = true;
                    }
                }

                // connect observer to shared subject
                if (hub != null)
                {
                    disposable.Add(hub.Subject.Subscribe(observer));
                }

                if (needSubscribe)
                {
                    var subscribeOptions = Create.MqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(f => f.WithTopic(topic)).Build();
                    await c.SubscribeAsync(subscribeOptions).ConfigureAwait(false);
                }
            });

            disposable.Add(inner);

            return Disposable.Create(async () =>
            {
                try
                {
                    if (mqttClient != null)
                    {
                        SubscriptionHub? hub = null;
                        var needUnsubscribe = false;
                        lock (_sync)
                        {
                            if (_rawTopicHubs.TryGetValue(mqttClient, out var topicMap) && topicMap.TryGetValue(topic, out hub))
                            {
                                hub.Count--;
                                if (hub.Count <= 0)
                                {
                                    topicMap.Remove(topic);

                                    // tear down
                                    hub.SourceTap?.Dispose();
                                    hub.Subject.OnCompleted();
                                    hub.Subject.Dispose();
                                    needUnsubscribe = true;
                                }
                            }
                        }

                        if (needUnsubscribe)
                        {
                            try
                            {
                                await mqttClient.UnsubscribeAsync(topic).ConfigureAwait(false);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                finally
                {
                    disposable.Dispose();
                }
            });
        }).Retry().Publish().RefCount();

    /// <summary>
    /// Discovers active MQTT topics observed by the client, emitting updates as topics are seen or expire.
    /// </summary>
    /// <remarks>The returned observable emits updates whenever the set of active topics changes, either due
    /// to new topics being observed or existing topics expiring. Topics are considered active if they have been seen
    /// within the specified expiry duration. The observable is shared among subscribers and automatically manages
    /// subscriptions to the underlying client sequence.</remarks>
    /// <param name="client">An observable sequence of connected MQTT clients to monitor for topic activity.</param>
    /// <param name="topicExpiry">The duration after which a topic is considered expired if not seen again. If null, defaults to one hour. Must be
    /// at least one second.</param>
    /// <returns>An observable sequence that emits the current set of active topics, each paired with the time it was last seen.
    /// The sequence is updated whenever topics are added or expire.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if topicExpiry is specified and is less than one second.</exception>
    public static IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservable<IMqttClient> client, TimeSpan? topicExpiry = null) =>
        Observable.Create<IEnumerable<(string Topic, DateTime LastSeen)>>(observer =>
        {
            topicExpiry ??= TimeSpan.FromHours(1);
            if (topicExpiry.Value.TotalSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(topicExpiry), "Topic expiry must be greater or equal to one.");
            }

            var gate = new object();
            var disposable = new CompositeDisposable();
            var topics = new List<(string Topic, DateTime LastSeen)>();
            var cleanupTopics = false;
            var lastCount = -1;

            disposable.Add(client.SubscribeToTopic("#").Select(m => m.ApplicationMessage.Topic)
                .Merge(Observable.Interval(TimeSpan.FromMinutes(1)).Select(_ => string.Empty))
                .Subscribe(topic =>
                {
                    lock (gate)
                    {
                        if (string.IsNullOrEmpty(topic))
                        {
                            cleanupTopics = true;
                        }
                        else if (topics.Select(x => x.Topic).Contains(topic))
                        {
                            topics.RemoveAll(x => x.Topic == topic);
                            topics.Add((topic, DateTime.UtcNow));
                        }
                        else
                        {
                            topics.Add((topic, DateTime.UtcNow));
                        }

                        if (cleanupTopics || lastCount != topics.Count)
                        {
                            topics.RemoveAll(x => DateTime.UtcNow.Subtract(x.LastSeen) > topicExpiry);
                            lastCount = topics.Count;
                            cleanupTopics = false;
                            observer.OnNext(topics);
                        }
                    }
                }));

            return disposable;
        }).Retry().Publish().RefCount();

    /// <summary>
    /// Discovers active MQTT topics observed by the client, emitting updates as topics are seen or expire.
    /// </summary>
    /// <remarks>The returned observable emits a new collection whenever the set of active topics changes,
    /// either due to new topics being observed or existing topics expiring. Topics are considered active if they have
    /// been seen within the specified expiry duration. The method subscribes to all topics using the wildcard
    /// '#'.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients to monitor for topic activity.</param>
    /// <param name="topicExpiry">The duration after which a topic is considered expired if not seen again. If null, defaults to one hour. Must be
    /// at least one second.</param>
    /// <returns>An observable sequence that emits collections of topic names and their last seen timestamps. Each collection
    /// represents the current set of active topics, updated as topics are discovered or expire.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if topicExpiry is specified and is less than one second.</exception>
    public static IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservable<IResilientMqttClient> client, TimeSpan? topicExpiry = null) =>
        Observable.Create<IEnumerable<(string Topic, DateTime LastSeen)>>(observer =>
        {
            topicExpiry ??= TimeSpan.FromHours(1);
            if (topicExpiry.Value.TotalSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(topicExpiry), "Topic expiry must be greater or equal to one.");
            }

            var gate = new object();
            var disposable = new CompositeDisposable();
            var topics = new List<(string Topic, DateTime LastSeen)>();
            var cleanupTopics = false;
            var lastCount = -1;

            disposable.Add(client.SubscribeToTopic("#").Select(m => m.ApplicationMessage.Topic)
                .Merge(Observable.Interval(TimeSpan.FromMinutes(1)).Select(_ => string.Empty))
                .Subscribe(topic =>
                {
                    lock (gate)
                    {
                        if (string.IsNullOrEmpty(topic))
                        {
                            cleanupTopics = true;
                        }
                        else if (topics.Select(x => x.Topic).Contains(topic))
                        {
                            topics.RemoveAll(x => x.Topic == topic);
                            topics.Add((topic, DateTime.UtcNow));
                        }
                        else
                        {
                            topics.Add((topic, DateTime.UtcNow));
                        }

                        if (cleanupTopics || lastCount != topics.Count)
                        {
                            topics.RemoveAll(x => DateTime.UtcNow.Subtract(x.LastSeen) > topicExpiry);
                            lastCount = topics.Count;
                            cleanupTopics = false;
                            observer.OnNext(topics);
                        }
                    }
                }));

            return disposable;
        }).Retry().Publish().RefCount();

    /// <summary>
    /// Subscribes to the specified MQTT topic for each resilient MQTT client in the observable sequence and returns an
    /// observable sequence of received application messages.
    /// </summary>
    /// <remarks>The subscription is reference-counted: the MQTT broker subscription is established when the
    /// first observer subscribes and is removed when the last observer unsubscribes. If multiple observers subscribe to
    /// the same topic on the same client, only a single broker subscription is maintained. The returned observable is
    /// resilient and will automatically resubscribe in case of client reconnections.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients to subscribe with. Each client in the sequence will be used to
    /// manage the topic subscription.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Must be a non-null, non-empty string. Supports MQTT topic wildcards.</param>
    /// <returns>An observable sequence that emits an event each time an application message is received on the specified topic
    /// by any of the resilient MQTT clients. The sequence completes when the subscription is disposed.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservable<IResilientMqttClient> client, string topic) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            IResilientMqttClient? mqttClient = null;

            var inner = client.Subscribe(async c =>
            {
                mqttClient = c;
                SubscriptionHub? hub = null;
                var needSubscribe = false;
                lock (_sync)
                {
                    if (!_resilientTopicHubs.TryGetValue(c, out var topicMap))
                    {
                        topicMap = new Dictionary<string, SubscriptionHub>(StringComparer.Ordinal);
                        _resilientTopicHubs[c] = topicMap;
                    }

                    if (!topicMap.TryGetValue(topic, out hub))
                    {
                        hub = new SubscriptionHub();
                        topicMap[topic] = hub;
                    }

                    hub.Count++;
                    if (hub.Count == 1)
                    {
                        // First subscriber for this client/topic: set up a single tap and request broker subscription
                        hub.SourceTap = c.ApplicationMessageReceived.WhereTopicIsMatch(topic).Subscribe(hub.Subject);
                        needSubscribe = true;
                    }
                }

                if (hub != null)
                {
                    disposable.Add(hub.Subject.Subscribe(observer));
                }

                if (needSubscribe)
                {
                    var mqttSubscribeOptions = Create.MqttFactory.CreateTopicFilterBuilder().WithTopic(topic).Build();
                    await c.SubscribeAsync([mqttSubscribeOptions]).ConfigureAwait(false);
                }
            });

            disposable.Add(inner);

            return Disposable.Create(async () =>
            {
                try
                {
                    if (mqttClient != null)
                    {
                        SubscriptionHub? hub = null;
                        var needUnsubscribe = false;
                        lock (_sync)
                        {
                            if (_resilientTopicHubs.TryGetValue(mqttClient, out var topicMap) && topicMap.TryGetValue(topic, out hub))
                            {
                                hub.Count--;
                                if (hub.Count <= 0)
                                {
                                    topicMap.Remove(topic);
                                    hub.SourceTap?.Dispose();
                                    hub.Subject.OnCompleted();
                                    hub.Subject.Dispose();
                                    needUnsubscribe = true;
                                }
                            }
                        }

                        if (needUnsubscribe)
                        {
                            try
                            {
                                await mqttClient.UnsubscribeAsync([topic]).ConfigureAwait(false);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                finally
                {
                    disposable.Dispose();
                }
            });
        }).Retry().Publish().RefCount();

    /// <summary>
    /// Filters the observable sequence to include only messages whose topic matches the specified topic filter,
    /// supporting MQTT wildcards.
    /// </summary>
    /// <remarks>This method uses a cache to optimize repeated topic matching and automatically retries the
    /// sequence if an error occurs. The topic filter supports standard MQTT wildcard syntax.</remarks>
    /// <param name="observable">The source sequence of MQTT application message received event arguments to filter.</param>
    /// <param name="topic">The MQTT topic filter to match against each message's topic. May include MQTT wildcards such as '+' or '#'.</param>
    /// <returns>An observable sequence that emits only those messages from the source whose topic matches the specified topic
    /// filter.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch(this IObservable<MqttApplicationMessageReceivedEventArgs> observable, string topic)
    {
        // This is a simple cache to avoid re-evaluating the same topic multiple times.
        var isValidTopics = new Dictionary<string, bool>(StringComparer.Ordinal);
        return Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer => observable.Where(x =>
        {
            // Check if the topic is valid.
            var incomingTopic = x.ApplicationMessage.Topic;
            if (!isValidTopics.TryGetValue(incomingTopic, out var isValid))
            {
                isValid = x.DetectCorrectTopicWithOrWithoutWildcard(topic);

                // Cache the result.
                isValidTopics[incomingTopic] = isValid;
            }

            return isValid;
        }).Subscribe(observer)).Retry();
    }

    /// <summary>
    /// Determines whether the topic of the received MQTT application message matches the specified topic filter,
    /// supporting wildcards.
    /// </summary>
    /// <remarks>This method uses MQTT topic filter comparison rules, including support for single-level ('+')
    /// and multi-level ('#') wildcards, to determine if the message's topic matches the provided filter.</remarks>
    /// <param name="message">The event arguments containing the received MQTT application message to evaluate. Cannot be null.</param>
    /// <param name="topic">The topic filter to compare against the message's topic. May include MQTT wildcards ('+' or '#'). Cannot be
    /// null.</param>
    /// <returns>true if the message's topic matches the specified topic filter; otherwise, false.</returns>
    private static bool DetectCorrectTopicWithOrWithoutWildcard(this MqttApplicationMessageReceivedEventArgs message, string topic) =>
        MqttTopicFilterComparer.Compare(message.ApplicationMessage.Topic, topic) == MqttTopicFilterCompareResult.IsMatch;

    /// <summary>
    /// Manages a subscription's state and message stream for MQTT application message events.
    /// </summary>
    /// <remarks>This class encapsulates the message subject and related resources for a single subscription.
    /// It is intended for internal use to coordinate message delivery and resource cleanup. Instances of this class are
    /// not thread-safe.</remarks>
    private sealed class SubscriptionHub : IDisposable
    {
        public int Count { get; set; }

        public ReplaySubject<MqttApplicationMessageReceivedEventArgs> Subject { get; } = new(bufferSize: 1);

        public IDisposable? SourceTap { get; set; }

        public void Dispose()
        {
            SourceTap?.Dispose();
            Subject.Dispose();
        }
    }
}
