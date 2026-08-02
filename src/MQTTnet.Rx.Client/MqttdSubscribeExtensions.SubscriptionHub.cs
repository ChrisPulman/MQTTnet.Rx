// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
#endif
#if REACTIVE_SHIM
using MqttFactoryProvider = MQTTnet.Rx.Client.Reactive.Create;
#else
using MqttFactoryProvider = MQTTnet.Rx.Client.Create;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Contains shared subscription hub infrastructure for MQTT topic streams.</summary>
public static partial class MqttdSubscribeExtensions
{
    /// <summary>Stores a topic filter and creates raw-client subscriptions for it.</summary>
    /// <param name="topic">The MQTT topic filter.</param>
    private sealed class RawTopicSubscription(string topic)
    {
        /// <summary>Tracks raw-client topic subscription hubs.</summary>
        private static readonly Dictionary<IMqttClient, Dictionary<string, SubscriptionHub>>
            RawTopicHubs = [];

        /// <summary>Creates the subscription for a client.</summary>
        /// <param name="client">The raw MQTT client.</param>
        /// <returns>The received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> Create(IMqttClient client) =>
            Signal.Create<MqttApplicationMessageReceivedEventArgs>(
                async (observer, cancellationToken) =>
                {
                    var (hub, needsSubscribe) = AcquireRawHub(client, topic);
                    using var subscription = hub.Subject.Subscribe(observer);
                    try
                    {
                        if (needsSubscribe)
                        {
                            var options = MqttFactoryProvider
                                .MqttFactory.CreateSubscribeOptionsBuilder()
                                .WithTopicFilter(filter => filter.WithTopic(topic))
                                .Build();
                            await client
                                .SubscribeAsync(options, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        var cancellation = new TaskCompletionSource(
                            TaskCreationOptions.RunContinuationsAsynchronously);
                        await using var registration = cancellationToken.Register(
                            cancellation.SetResult);
                        await cancellation.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        await ReleaseRawHubAsync(client, topic).ConfigureAwait(false);
                    }

                    return EmptyDisposable.Instance;
                });

        /// <summary>Acquires a raw-client subscription hub.</summary>
        /// <param name="client">The raw MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The acquired hub and whether it requires a broker subscription.</returns>
        private static (SubscriptionHub Hub, bool NeedsSubscribe) AcquireRawHub(
            IMqttClient client,
            string topic)
        {
            lock (Sync)
            {
                ref var topics = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    RawTopicHubs,
                    client,
                    out var clientExists);
                if (!clientExists)
                {
                    topics = new(StringComparer.Ordinal);
                }

                ref var hub = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    topics!,
                    topic,
                    out var hubExists);
                if (!hubExists)
                {
                    hub = new();
                }

                hub!.Count++;
                if (hub.Count == 1)
                {
                    hub.SourceTap = client
                        .ApplicationMessageReceived()
                        .WhereTopicIsMatch(topic)
                        .Subscribe(hub.Subject);
                    return (hub, true);
                }

                return (hub, false);
            }
        }

        /// <summary>Releases a raw-client subscription hub and broker subscription.</summary>
        /// <param name="client">The raw MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>A task that completes after the hub is released.</returns>
        private static async Task ReleaseRawHubAsync(IMqttClient client, string topic)
        {
            if (!RemoveRawHub(client, topic))
            {
                return;
            }

            try
            {
                await client.UnsubscribeAsync(topic).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        /// <summary>Removes the final raw-client hub reference.</summary>
        /// <param name="client">The raw MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns><see langword="true"/> when the broker subscription must be removed.</returns>
        private static bool RemoveRawHub(IMqttClient client, string topic)
        {
            lock (Sync)
            {
                if (
                    !RawTopicHubs.TryGetValue(client, out var topics)
                    || !topics.TryGetValue(topic, out var hub))
                {
                    return false;
                }

                hub.Count--;
                if (hub.Count > 0)
                {
                    return false;
                }

                _ = topics.Remove(topic);
                if (topics.Count == 0)
                {
                    _ = RawTopicHubs.Remove(client);
                }

                hub.Dispose();
                return true;
            }
        }
    }

    /// <summary>Stores a topic filter and creates resilient-client subscriptions for it.</summary>
    /// <param name="topic">The MQTT topic filter.</param>
    private sealed class ResilientTopicSubscription(string topic)
    {
        /// <summary>Tracks resilient-client topic subscription hubs.</summary>
        private static readonly Dictionary<
            IResilientMqttClient,
            Dictionary<string, SubscriptionHub>
        > ResilientTopicHubs = [];

        /// <summary>Creates the subscription for a client.</summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <returns>The received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> Create(
            IResilientMqttClient client) =>
            Signal.Create<MqttApplicationMessageReceivedEventArgs>(
                async (observer, cancellationToken) =>
                {
                    var (hub, needsSubscribe) = AcquireResilientHub(client, topic);
                    using var subscription = hub.Subject.Subscribe(observer);
                    try
                    {
                        if (needsSubscribe)
                        {
                            var options = MqttFactoryProvider
                                .MqttFactory.CreateTopicFilterBuilder()
                                .WithTopic(topic)
                                .Build();
                            await client.SubscribeAsync([options]).ConfigureAwait(false);
                        }

                        var cancellation = new TaskCompletionSource(
                            TaskCreationOptions.RunContinuationsAsynchronously);
                        await using var registration = cancellationToken.Register(
                            cancellation.SetResult);
                        await cancellation.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        await ReleaseResilientHubAsync(client, topic).ConfigureAwait(false);
                    }

                    return EmptyDisposable.Instance;
                });

        /// <summary>Acquires a resilient-client subscription hub.</summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>The acquired hub and whether it requires a broker subscription.</returns>
        private static (SubscriptionHub Hub, bool NeedsSubscribe) AcquireResilientHub(
            IResilientMqttClient client,
            string topic)
        {
            lock (Sync)
            {
                ref var topics = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    ResilientTopicHubs,
                    client,
                    out var clientExists);
                if (!clientExists)
                {
                    topics = new(StringComparer.Ordinal);
                }

                ref var hub = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    topics!,
                    topic,
                    out var hubExists);
                if (!hubExists)
                {
                    hub = new();
                }

                hub!.Count++;
                if (hub.Count == 1)
                {
                    hub.SourceTap = client
                        .ApplicationMessageReceived.WhereTopicIsMatch(topic)
                        .Subscribe(hub.Subject);
                    return (hub, true);
                }

                return (hub, false);
            }
        }

        /// <summary>Releases a resilient-client subscription hub and broker subscription.</summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns>A task that completes after the hub is released.</returns>
        private static async Task ReleaseResilientHubAsync(
            IResilientMqttClient client,
            string topic)
        {
            if (!RemoveResilientHub(client, topic))
            {
                return;
            }

            try
            {
                await client.UnsubscribeAsync([topic]).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        /// <summary>Removes the final resilient-client hub reference.</summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <returns><see langword="true"/> when the broker subscription must be removed.</returns>
        private static bool RemoveResilientHub(IResilientMqttClient client, string topic)
        {
            lock (Sync)
            {
                if (
                    !ResilientTopicHubs.TryGetValue(client, out var topics)
                    || !topics.TryGetValue(topic, out var hub))
                {
                    return false;
                }

                hub.Count--;
                if (hub.Count > 0)
                {
                    return false;
                }

                _ = topics.Remove(topic);
                if (topics.Count == 0)
                {
                    _ = ResilientTopicHubs.Remove(client);
                }

                hub.Dispose();
                return true;
            }
        }
    }

    /// <summary>Caches topic-filter comparisons for one observable sequence.</summary>
    /// <param name="topic">The MQTT topic filter.</param>
    private sealed class TopicFilter(string topic)
    {
        /// <summary>Caches topic-filter comparison results by incoming topic.</summary>
        private readonly Dictionary<string, bool> _matches = new(StringComparer.Ordinal);

        /// <summary>Applies the topic filter to a message sequence.</summary>
        /// <param name="messages">The received MQTT message sequence.</param>
        /// <returns>The matching messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> Apply(
            IObservable<MqttApplicationMessageReceivedEventArgs> messages) => messages.Where(IsMatch).Retry();

        /// <summary>Determines whether a received message matches the topic filter.</summary>
        /// <param name="message">The received MQTT message.</param>
        /// <returns><see langword="true"/> when the message topic matches the filter.</returns>
        private bool IsMatch(MqttApplicationMessageReceivedEventArgs message)
        {
            var incomingTopic = message.ApplicationMessage.Topic;
            ref var result = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _matches,
                incomingTopic,
                out var exists);
            if (!exists)
            {
                result =
                    MqttTopicFilterComparer.Compare(incomingTopic, topic)
                    == MqttTopicFilterCompareResult.IsMatch;
            }

            return result;
        }
    }

    /// <summary>Tracks active topic names and timestamps for one discovery subscription.</summary>
    /// <param name="observer">The observer receiving topic updates.</param>
    /// <param name="expiry">The topic inactivity duration.</param>
    /// <param name="timeProvider">The clock used to timestamp messages.</param>
    private sealed class TopicDiscoveryState(
        IObserver<IEnumerable<(string Topic, DateTime LastSeen)>> observer,
        TimeSpan expiry,
        TimeProvider timeProvider)
    {
#if NET9_0_OR_GREATER
        /// <summary>Synchronizes access to topic discovery state.</summary>
        private readonly Lock _gate = new();
#else
        /// <summary>Synchronizes access to topic discovery state.</summary>
        private readonly object _gate = new();
#endif

        /// <summary>Receives topic discovery updates.</summary>
        private readonly IObserver<IEnumerable<(string Topic, DateTime LastSeen)>> _observer =
            observer;

        /// <summary>Defines the inactivity duration before topic expiry.</summary>
        private readonly TimeSpan _expiry = expiry;

        /// <summary>Provides the current time for topic discovery.</summary>
        private readonly TimeProvider _timeProvider = timeProvider;

        /// <summary>Stores active topics and their last-seen times.</summary>
        private readonly List<(string Topic, DateTime LastSeen)> _topics = [];

        /// <summary>Indicates that expired topics should be removed.</summary>
        private bool _cleanupTopics;

        /// <summary>Stores the topic count emitted most recently.</summary>
        private int _lastCount = -1;

        /// <summary>Records a received topic name and emits an update when required.</summary>
        /// <param name="topic">The received topic name, or an empty value for the periodic cleanup tick.</param>
        public void OnTopic(string topic)
        {
            lock (_gate)
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                if (string.IsNullOrEmpty(topic))
                {
                    _cleanupTopics = true;
                }
                else
                {
                    for (var index = 0; index < _topics.Count; index++)
                    {
                        if (_topics[index].Topic == topic)
                        {
                            _topics.RemoveAt(index);
                            break;
                        }
                    }

                    _topics.Add((topic, now));
                }

                if (_cleanupTopics || _lastCount != _topics.Count)
                {
                    _ = _topics.RemoveAll(entry => now - entry.LastSeen > _expiry);
                    _lastCount = _topics.Count;
                    _cleanupTopics = false;
                    _observer.OnNext(_topics);
                }
            }
        }
    }

    /// <summary>Manages a subscription's state and message stream for MQTT application message events.</summary>
    /// <remarks>This class encapsulates the message subject and related resources for a single subscription.
    /// It is intended for internal use to coordinate message delivery and resource cleanup. Instances of this class are
    /// not thread-safe.</remarks>
    private sealed class SubscriptionHub : IDisposable
    {
        /// <summary>Gets or sets the active subscriber count.</summary>
        public int Count { get; set; }

        /// <summary>Gets the replaying subject that distributes received messages.</summary>
        public ReplaySignal<MqttApplicationMessageReceivedEventArgs> Subject { get; } =
            new(bufferSize: 1);

        /// <summary>Gets or sets the subscription that forwards source messages to <see cref="Subject"/>.</summary>
        public IDisposable SourceTap { get; set; } = EmptyDisposable.Instance;

        /// <summary>Disposes the forwarding subscription and message subject.</summary>
        public void Dispose()
        {
            SourceTap.Dispose();
            Subject.Dispose();
        }
    }
}
