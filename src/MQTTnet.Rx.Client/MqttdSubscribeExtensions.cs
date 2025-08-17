// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqttd Subscribe Extensions.
/// </summary>
public static class MqttdSubscribeExtensions
{
    private static readonly object _sync = new();
    private static readonly Dictionary<string, IObservable<object?>> _dictJsonValues = [];
    private static readonly Dictionary<IResilientMqttClient, Dictionary<string, SubscriptionHub>> _resilientTopicHubs = [];
    private static readonly Dictionary<IMqttClient, Dictionary<string, SubscriptionHub>> _rawTopicHubs = [];

    /// <summary>
    /// Converts to dictionary.
    /// </summary>
    /// <param name="message">The incoming messages whose payload contains JSON formatted key/value pairs.</param>
    /// <returns>A dictionary of key/value pairs for each incoming message or null if deserialization fails.</returns>
    public static IObservable<Dictionary<string, object>?> ToDictionary(this IObservable<MqttApplicationMessageReceivedEventArgs> message) =>
        Observable.Create<Dictionary<string, object>?>(observer => message.Retry().Subscribe(m => observer.OnNext(JsonConvert.DeserializeObject<Dictionary<string, object>?>(m.ApplicationMessage.ConvertPayloadToString())))).Retry();

    /// <summary>
    /// Deserializes JSON payload to the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize to.</typeparam>
    /// <param name="message">The incoming messages whose payload contains JSON.</param>
    /// <param name="settings">Optional Json.NET serializer settings.</param>
    /// <returns>An observable sequence of deserialized values.</returns>
    public static IObservable<T?> ToObject<T>(this IObservable<MqttApplicationMessageReceivedEventArgs> message, JsonSerializerSettings? settings = null) =>
        message.Select(m =>
        {
            var json = m.ApplicationMessage.ConvertPayloadToString();
            return settings is null ? JsonConvert.DeserializeObject<T>(json) : JsonConvert.DeserializeObject<T>(json, settings);
        });

    /// <summary>
    /// Observes the specified key in a stream of dictionaries and emits its values.
    /// </summary>
    /// <param name="dictionary">The dictionary stream.</param>
    /// <param name="key">The key to observe.</param>
    /// <returns>An observable sequence of values for the key.</returns>
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
    /// Maps an object sequence to bool.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of bool.</returns>
    public static IObservable<bool> ToBool(this IObservable<object?> observable) => observable.Select(Convert.ToBoolean);

    /// <summary>
    /// Maps an object sequence to byte.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of byte.</returns>
    public static IObservable<byte> ToByte(this IObservable<object?> observable) => observable.Select(Convert.ToByte);

    /// <summary>
    /// Maps an object sequence to Int16.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of Int16.</returns>
    public static IObservable<short> ToInt16(this IObservable<object?> observable) => observable.Select(Convert.ToInt16);

    /// <summary>
    /// Maps an object sequence to Int32.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of Int32.</returns>
    public static IObservable<int> ToInt32(this IObservable<object?> observable) => observable.Select(Convert.ToInt32);

    /// <summary>
    /// Maps an object sequence to Int64.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of Int64.</returns>
    public static IObservable<long> ToInt64(this IObservable<object?> observable) => observable.Select(Convert.ToInt64);

    /// <summary>
    /// Maps an object sequence to Single.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of Single.</returns>
    public static IObservable<float> ToSingle(this IObservable<object?> observable) => observable.Select(Convert.ToSingle);

    /// <summary>
    /// Maps an object sequence to Double.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of Double.</returns>
    public static IObservable<double> ToDouble(this IObservable<object?> observable) => observable.Select(Convert.ToDouble);

    /// <summary>
    /// Maps an object sequence to string.
    /// </summary>
    /// <param name="observable">The source observable.</param>
    /// <returns>Sequence of string.</returns>
    public static IObservable<string?> ToString(this IObservable<object?> observable) => observable.Select(Convert.ToString);

    /// <summary>
    /// Subscribe to multiple topics and merge messages for the raw client.
    /// </summary>
    /// <param name="client">The raw client observable.</param>
    /// <param name="topics">Topic filters.</param>
    /// <returns>Merged stream of received messages matching the given topics.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservable<IMqttClient> client, params string[] topics) =>
        topics.Select(t => client.SubscribeToTopic(t)).Merge();

    /// <summary>
    /// Subscribe to multiple topics and merge messages for the resilient client.
    /// </summary>
    /// <param name="client">The resilient client observable.</param>
    /// <param name="topics">Topic filters.</param>
    /// <returns>Merged stream of received messages matching the given topics.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics(this IObservable<IResilientMqttClient> client, params string[] topics) =>
        topics.Select(t => client.SubscribeToTopic(t)).Merge();

    /// <summary>
    /// Subscribes to a topic (raw client). Ref-counted per client/topic to avoid duplicate broker subscriptions.
    /// </summary>
    /// <param name="client">The raw client observable.</param>
    /// <param name="topic">Topic filter (supports + and # wildcards).</param>
    /// <returns>Message stream matching the topic.</returns>
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
    /// Discovers topics seen by the client, with an optional expiry for inactive topics.
    /// </summary>
    /// <param name="client">The raw client observable.</param>
    /// <param name="topicExpiry">Topic expiry; topics are removed if they do not publish a value within this time.</param>
    /// <returns>A stream of topic lists with last-seen timestamps.</returns>
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
    /// Discovers topics seen by the resilient client, with an optional expiry for inactive topics.
    /// </summary>
    /// <param name="client">The resilient client observable.</param>
    /// <param name="topicExpiry">Topic expiry; topics are removed if they do not publish a value within this time.</param>
    /// <returns>A stream of topic lists with last-seen timestamps.</returns>
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
    /// Subscribes to a topic (resilient client). Ref-counted per client/topic to avoid duplicate broker subscriptions.
    /// </summary>
    /// <param name="client">The resilient client observable.</param>
    /// <param name="topic">Topic filter (supports + and # wildcards).</param>
    /// <returns>Message stream matching the topic.</returns>
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
    /// Filters allowing only the topics which match the specified topic.
    /// </summary>
    /// <param name="observable">The observable source of messages.</param>
    /// <param name="topic">The topic filter to match.</param>
    /// <returns>A message stream where the topic matches the filter.</returns>
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

    private static bool DetectCorrectTopicWithOrWithoutWildcard(this MqttApplicationMessageReceivedEventArgs message, string topic) =>
        MqttTopicFilterComparer.Compare(message.ApplicationMessage.Topic, topic) == MqttTopicFilterCompareResult.IsMatch;

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
