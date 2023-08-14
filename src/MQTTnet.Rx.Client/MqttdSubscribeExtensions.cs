﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqttd Subscribe Extensions.
/// </summary>
public static class MqttdSubscribeExtensions
{
    private static readonly Dictionary<string, IObservable<object?>> _dictJsonValues = new();

    /// <summary>
    /// Converts to dictionary.
    /// </summary>
    /// <param name="message">The message with Json formated key data pairs.</param>
    /// <returns>A Dictionary of key data pairs.</returns>
    public static IObservable<Dictionary<string, object>?> ToDictionary(this IObservable<MqttApplicationMessageReceivedEventArgs> message) =>
        Observable.Create<Dictionary<string, object>?>(observer => message.Retry().Subscribe(m => observer.OnNext(JsonConvert.DeserializeObject<Dictionary<string, object>?>(m.ApplicationMessage.ConvertPayloadToString())))).Retry();

    /// <summary>
    /// Observes the specified key.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <returns>An Observable object.</returns>
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
    /// Converts to bool.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of bool.</returns>
    public static IObservable<bool> ToBool(this IObservable<object?> observable) =>
        observable.Select(Convert.ToBoolean);

    /// <summary>
    /// Converts to byte.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of byte.</returns>
    public static IObservable<byte> ToByte(this IObservable<object?> observable) =>
        observable.Select(Convert.ToByte);

    /// <summary>
    /// Converts to short.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of short.</returns>
    public static IObservable<short> ToInt16(this IObservable<object?> observable) =>
        observable.Select(Convert.ToInt16);

    /// <summary>
    /// Converts to int.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of int.</returns>
    public static IObservable<int> ToInt32(this IObservable<object?> observable) =>
        observable.Select(Convert.ToInt32);

    /// <summary>
    /// Converts to long.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of long.</returns>
    public static IObservable<long> ToInt64(this IObservable<object?> observable) =>
        observable.Select(Convert.ToInt64);

    /// <summary>
    /// Converts to single.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of float.</returns>
    public static IObservable<float> ToSingle(this IObservable<object?> observable) =>
        observable.Select(Convert.ToSingle);

    /// <summary>
    /// Converts to double.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of double.</returns>
    public static IObservable<double> ToDouble(this IObservable<object?> observable) =>
        observable.Select(Convert.ToDouble);

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <param name="observable">The observable.</param>
    /// <returns>Observable of string.</returns>
    public static IObservable<string?> ToString(this IObservable<object?> observable) =>
        observable.Select(Convert.ToString);

    /// <summary>
    /// Subscribes to topic.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="topic">The topic.</param>
    /// <returns>An Observable Mqtt Client Subscribe Result.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservable<IMqttClient> client, string topic) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            IMqttClient? mqttClient = null;
            disposable.Add(client.Subscribe(async c =>
            {
                mqttClient = c;
                disposable.Add(mqttClient.ApplicationMessageReceived().Subscribe(observer));
                var mqttSubscribeOptions = Create.MqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(topic))
                    .Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            }));

            return Disposable.Create(async () =>
                {
                    try
                    {
                        await mqttClient!.UnsubscribeAsync(topic).ConfigureAwait(false);
                        disposable.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                    }
                });
        }).Retry();

    /// <summary>
    /// Discovers the topics.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="topicExpiry">The topic expiry, topics are removed if they do not publish a value within this time.</param>
    /// <returns>
    /// A List of topics.
    /// </returns>
    public static IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservable<IMqttClient> client, TimeSpan? topicExpiry = null) =>
        Observable.Create<IEnumerable<(string Topic, DateTime LastSeen)>>(observer =>
            {
                if (topicExpiry == null)
                {
                    topicExpiry = TimeSpan.FromHours(1);
                }

                if (topicExpiry.Value.TotalSeconds < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(topicExpiry), "Topic expiry must be greater or equal to one.");
                }

                var disposable = new CompositeDisposable();
                var semaphore = new SemaphoreSlim(1);
                disposable.Add(semaphore);
                var topics = new List<(string Topic, DateTime LastSeen)>();
                var cleanupTopics = false;
                var lastCount = -1;
                disposable.Add(client.SubscribeToTopic("#").Select(m => m.ApplicationMessage.Topic)
                    .Merge(Observable.Interval(TimeSpan.FromMinutes(1)).Select(_ => string.Empty)).Subscribe(topic =>
                {
                    semaphore.Wait();
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

                    semaphore.Release();
                }));

                return disposable;
            }).Retry().Publish().RefCount();

    /// <summary>
    /// Discovers the topics.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="topicExpiry">The topic expiry, topics are removed if they do not publish a value within this time.</param>
    /// <returns>
    /// A List of topics.
    /// </returns>
    public static IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(this IObservable<IManagedMqttClient> client, TimeSpan? topicExpiry = null) =>
        Observable.Create<IEnumerable<(string Topic, DateTime LastSeen)>>(observer =>
            {
                if (topicExpiry == null)
                {
                    topicExpiry = TimeSpan.FromHours(1);
                }

                if (topicExpiry.Value.TotalSeconds < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(topicExpiry), "Topic expiry must be greater or equal to one.");
                }

                var disposable = new CompositeDisposable();
                var semaphore = new SemaphoreSlim(1);
                disposable.Add(semaphore);
                var topics = new List<(string Topic, DateTime LastSeen)>();
                var cleanupTopics = false;
                var lastCount = -1;
                disposable.Add(client.SubscribeToTopic("#").Select(m => m.ApplicationMessage.Topic)
                    .Merge(Observable.Interval(TimeSpan.FromMinutes(1)).Select(_ => string.Empty)).Subscribe(topic =>
                {
                    semaphore.Wait();
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

                    semaphore.Release();
                }));

                return disposable;
            }).Retry().Publish().RefCount();

    /// <summary>
    /// Subscribes to topic.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="topic">The topic.</param>
    /// <returns>An Observable Mqtt Client Subscribe Result.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(this IObservable<IManagedMqttClient> client, string topic) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            IManagedMqttClient? mqttClient = null;
            disposable.Add(client.Subscribe(async c =>
            {
                mqttClient = c;
                disposable.Add(mqttClient.ApplicationMessageReceived().Subscribe(observer));
                var mqttSubscribeOptions = Create.MqttFactory.CreateTopicFilterBuilder()
                    .WithTopic(topic)
                    .Build();

                await mqttClient.SubscribeAsync(new[] { mqttSubscribeOptions });
            }));

            return Disposable.Create(async () =>
                {
                    try
                    {
                        await mqttClient!.UnsubscribeAsync(new[] { topic }).ConfigureAwait(false);
                        disposable.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                    }
                });
        }).Retry().Publish().RefCount();
}
