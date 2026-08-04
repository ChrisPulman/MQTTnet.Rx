// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides paired cold reactive wrappers for every resilient MQTT client operation.</summary>
public static class ResilientMqttClientOperationExtensions
{
    /// <summary>Provides direct reactive resilient-client operations.</summary>
    /// <param name="client">The resilient MQTT client.</param>
    extension(IResilientMqttClient client)
    {
        /// <summary>Enqueues an application message when subscribed.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold enqueue operation.</returns>
        public IObservable<RxUnit> Enqueue(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTask(() => client.EnqueueAsync(message));
        }

        /// <summary>Enqueues a resilient application message when subscribed.</summary>
        /// <param name="message">The resilient application message.</param>
        /// <returns>A cold enqueue operation.</returns>
        public IObservable<RxUnit> Enqueue(ResilientMqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTask(() => client.EnqueueAsync(message));
        }

        /// <summary>Enqueues an application message through an asynchronous observable.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold asynchronous enqueue operation.</returns>
        public IObservableAsync<RxUnit> ObserveEnqueue(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTaskSignal(() => client.EnqueueAsync(message));
        }

        /// <summary>Enqueues a resilient application message through an asynchronous observable.</summary>
        /// <param name="message">The resilient application message.</param>
        /// <returns>A cold asynchronous enqueue operation.</returns>
        public IObservableAsync<RxUnit> ObserveEnqueue(ResilientMqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return FromTaskSignal(() => client.EnqueueAsync(message));
        }

        /// <summary>Sends a ping when subscribed.</summary>
        /// <returns>A cold ping operation.</returns>
        public IObservable<RxUnit> Ping() => FromTask(client.PingAsync);

        /// <summary>Sends a ping through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous ping operation.</returns>
        public IObservableAsync<RxUnit> ObservePing() => FromTaskSignal(client.PingAsync);

        /// <summary>Starts the resilient client with prebuilt options.</summary>
        /// <param name="options">The resilient-client options.</param>
        /// <returns>A cold start operation.</returns>
        public IObservable<RxUnit> Start(ResilientMqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTask(() => client.StartAsync(options));
        }

        /// <summary>Starts the resilient client with fluent option configuration.</summary>
        /// <param name="configure">Configures the resilient-client options.</param>
        /// <returns>A cold start operation.</returns>
        public IObservable<RxUnit> Start(Action<ResilientMqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new ResilientMqttClientOptionsBuilder();
            configure(builder);
            return client.Start(builder.Build());
        }

        /// <summary>Starts the resilient client asynchronously with prebuilt options.</summary>
        /// <param name="options">The resilient-client options.</param>
        /// <returns>A cold asynchronous start operation.</returns>
        public IObservableAsync<RxUnit> ObserveStart(ResilientMqttClientOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return FromTaskSignal(() => client.StartAsync(options));
        }

        /// <summary>Starts the resilient client asynchronously with fluent option configuration.</summary>
        /// <param name="configure">Configures the resilient-client options.</param>
        /// <returns>A cold asynchronous start operation.</returns>
        public IObservableAsync<RxUnit> ObserveStart(Action<ResilientMqttClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new ResilientMqttClientOptionsBuilder();
            configure(builder);
            return client.ObserveStart(builder.Build());
        }

        /// <summary>Stops the resilient client with a clean disconnect.</summary>
        /// <returns>A cold stop operation.</returns>
        public IObservable<RxUnit> Stop() => client.Stop(true);

        /// <summary>Stops the resilient client.</summary>
        /// <param name="cleanDisconnect">Whether to perform a clean MQTT disconnect.</param>
        /// <returns>A cold stop operation.</returns>
        public IObservable<RxUnit> Stop(bool cleanDisconnect) =>
            FromTask(() => client.StopAsync(cleanDisconnect));

        /// <summary>Stops the resilient client asynchronously with a clean disconnect.</summary>
        /// <returns>A cold asynchronous stop operation.</returns>
        public IObservableAsync<RxUnit> ObserveStop() => client.ObserveStop(true);

        /// <summary>Stops the resilient client asynchronously.</summary>
        /// <param name="cleanDisconnect">Whether to perform a clean MQTT disconnect.</param>
        /// <returns>A cold asynchronous stop operation.</returns>
        public IObservableAsync<RxUnit> ObserveStop(bool cleanDisconnect) =>
            FromTaskSignal(() => client.StopAsync(cleanDisconnect));

        /// <summary>Subscribes the resilient client when subscribed.</summary>
        /// <param name="topicFilters">The topic filters.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<RxUnit> Subscribe(IEnumerable<MqttTopicFilter> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(topicFilters);
            return FromTask(() => client.SubscribeAsync(topicFilters));
        }

        /// <summary>Subscribes the resilient client through an asynchronous observable.</summary>
        /// <param name="topicFilters">The topic filters.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<RxUnit> ObserveSubscribe(IEnumerable<MqttTopicFilter> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(topicFilters);
            return FromTaskSignal(() => client.SubscribeAsync(topicFilters));
        }

        /// <summary>Unsubscribes the resilient client when subscribed.</summary>
        /// <param name="topics">The topic names.</param>
        /// <returns>A cold unsubscribe operation.</returns>
        public IObservable<RxUnit> Unsubscribe(IEnumerable<string> topics)
        {
            ArgumentNullException.ThrowIfNull(topics);
            return FromTask(() => client.UnsubscribeAsync(topics));
        }

        /// <summary>Unsubscribes the resilient client through an asynchronous observable.</summary>
        /// <param name="topics">The topic names.</param>
        /// <returns>A cold asynchronous unsubscribe operation.</returns>
        public IObservableAsync<RxUnit> ObserveUnsubscribe(IEnumerable<string> topics)
        {
            ArgumentNullException.ThrowIfNull(topics);
            return FromTaskSignal(() => client.UnsubscribeAsync(topics));
        }
    }

    /// <summary>Wraps a task as a cold observable operation.</summary>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold observable operation.</returns>
    private static IObservable<RxUnit> FromTask(Func<Task> operation) => Signal.FromAsync(async () =>
    {
        await operation().ConfigureAwait(false);
        return RxUnit.Default;
    });

    /// <summary>Wraps a task as a cold asynchronous observable operation.</summary>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold asynchronous observable operation.</returns>
    private static IObservableAsync<RxUnit> FromTaskSignal(Func<Task> operation) =>
        SignalAsync.FromAsync(async _ =>
        {
            await operation().ConfigureAwait(false);
            return RxUnit.Default;
        });
}
