// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using RxUnit = System.Reactive.Unit;

namespace MQTTnet.Rx.Client;

/// <summary>Provides reactive MQTT client operation extensions.</summary>
/// <remarks>
/// These extensions wrap asynchronous MQTT client operations as observables for seamless integration
/// with reactive programming patterns.
/// </remarks>
public static class ReactiveClientOperationsExtensions
{
    /// <summary>The default interval, in seconds, between periodic ping requests.</summary>
    private const int DefaultPingIntervalSeconds = 30;

    /// <summary>Provides event subscription actions for an MQTT client.</summary>
    /// <param name="client">The MQTT client whose events are subscribed.</param>
    private sealed class MqttClientAsyncEventHandlers(IMqttClient client)
    {
        /// <summary>Adds a connected event handler.</summary>
        /// <param name="handler">The handler to add.</param>
        public void AddConnected(Func<MqttClientConnectedEventArgs, Task> handler) =>
            client.ConnectedAsync += handler;

        /// <summary>Removes a connected event handler.</summary>
        /// <param name="handler">The handler to remove.</param>
        public void RemoveConnected(Func<MqttClientConnectedEventArgs, Task> handler) =>
            client.ConnectedAsync -= handler;

        /// <summary>Adds a disconnected event handler.</summary>
        /// <param name="handler">The handler to add.</param>
        public void AddDisconnected(Func<MqttClientDisconnectedEventArgs, Task> handler) =>
            client.DisconnectedAsync += handler;

        /// <summary>Removes a disconnected event handler.</summary>
        /// <param name="handler">The handler to remove.</param>
        public void RemoveDisconnected(Func<MqttClientDisconnectedEventArgs, Task> handler) =>
            client.DisconnectedAsync -= handler;
    }

    /// <summary>Provides reactive operations for observable MQTT clients.</summary>
    /// <param name="client">The MQTT client stream.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Sends a ping request.</summary>
        /// <returns>An observable that emits unit when the ping completes successfully.</returns>
        public IObservable<RxUnit> Ping() =>
            client
                .SelectMany(static c =>
                    PrimitivesObservableCompatibilityExtensions.FromTask(c.PingAsync))
                .Select(static _ => RxUnit.Default);

        /// <summary>Sends periodic ping requests to maintain the connection.</summary>
        /// <returns>An observable that emits unit for each successful ping.</returns>
        public IObservable<RxUnit> PingPeriodically() => client.PingPeriodically(null);

        /// <summary>Sends periodic ping requests to maintain the connection.</summary>
        /// <param name="interval">The interval between pings. Null uses the default 30-second interval.</param>
        /// <returns>An observable that emits unit for each successful ping.</returns>
        public IObservable<RxUnit> PingPeriodically(TimeSpan? interval) =>
            client
                .SelectMany(c =>
                    Signal
                        .Interval(interval ?? TimeSpan.FromSeconds(DefaultPingIntervalSeconds))
                        .SelectMany(_ =>
                            PrimitivesObservableCompatibilityExtensions.FromTask(c.PingAsync))
                        .Select(static _ => RxUnit.Default))
                .Retry();

        /// <summary>Subscribes to the specified topics and returns an observable of the subscription results.</summary>
        /// <param name="topics">The topics to subscribe to.</param>
        /// <returns>An observable that emits the subscription result.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(string[] topics) =>
            client.Subscribe(topics, MqttQualityOfServiceLevel.AtMostOnce);

        /// <summary>Subscribes to the specified topics and returns an observable of the subscription results.</summary>
        /// <param name="topics">The topics to subscribe to.</param>
        /// <param name="qualityOfServiceLevel">The QoS level for all subscriptions.</param>
        /// <returns>An observable that emits the subscription result.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(
            string[] topics,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                foreach (var topic in topics)
                {
                    _ = optionsBuilder.WithTopicFilter(f =>
                        f.WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel));
                }

                return Signal.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
            });

        /// <summary>Subscribes to the specified topic with custom filter configuration.</summary>
        /// <param name="topicFilterBuilder">An action to configure the topic filter.</param>
        /// <returns>An observable that emits the subscription result.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(
            Action<MqttTopicFilterBuilder> topicFilterBuilder) =>
            client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                _ = optionsBuilder.WithTopicFilter(topicFilterBuilder);
                return Signal.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
            });

        /// <summary>Subscribes to the specified topic filters.</summary>
        /// <param name="topicFilters">The topic filters to subscribe to.</param>
        /// <returns>An observable that emits the subscription result.</returns>
        public IObservable<MqttClientSubscribeResult> Subscribe(
            params MqttTopicFilter[] topicFilters) =>
            client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                foreach (var filter in topicFilters)
                {
                    _ = optionsBuilder.WithTopicFilter(filter);
                }

                return Signal.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
            });

        /// <summary>Unsubscribes from the specified topics.</summary>
        /// <param name="topics">The topics to unsubscribe from.</param>
        /// <returns>An observable that emits the unsubscription result.</returns>
        public IObservable<MqttClientUnsubscribeResult> Unsubscribe(params string[] topics) =>
            client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateUnsubscribeOptionsBuilder();
                foreach (var topic in topics)
                {
                    _ = optionsBuilder.WithTopicFilter(topic);
                }

                return Signal.FromAsync(ct => c.UnsubscribeAsync(optionsBuilder.Build(), ct));
            });

        /// <summary>Disconnects the MQTT client.</summary>
        /// <returns>An observable that completes when disconnection is done.</returns>
        public IObservable<RxUnit> Disconnect() =>
            client.Disconnect(MqttClientDisconnectOptionsReason.NormalDisconnection);

        /// <summary>Disconnects the MQTT client.</summary>
        /// <param name="reason">The disconnect reason.</param>
        /// <returns>An observable that completes when disconnection is done.</returns>
        public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptionsReason reason) =>
            client
                .SelectMany(c =>
                {
                    var options = Create
                        .MqttFactory.CreateClientDisconnectOptionsBuilder()
                        .WithReason(reason)
                        .Build();
                    return PrimitivesObservableCompatibilityExtensions.FromTask(ct =>
                        c.DisconnectAsync(options, ct));
                })
                .Select(static _ => RxUnit.Default);

        /// <summary>Reconnects the MQTT client using the previous connection options.</summary>
        /// <returns>An observable that completes when reconnection is done.</returns>
        public IObservable<RxUnit> Reconnect() =>
            client
                .SelectMany(static c =>
                    PrimitivesObservableCompatibilityExtensions.FromTask(c.ReconnectAsync))
                .Select(static _ => RxUnit.Default);

        /// <summary>Gets an observable that emits the connection status of the client.</summary>
        /// <returns>An observable that emits true when connected and false when disconnected.</returns>
        public IObservable<bool> ConnectionStatus() =>
            client
                .SelectMany(static c =>
                    Signal
                        .Return(c.IsConnected)
                        .Concat(
                            c.Connected()
                                .Select(static _ => true)
                                .Merge(c.Disconnected().Select(static _ => false))))
                .DistinctUntilChanged()
                .Publish()
                .RefCount();

        /// <summary>Waits for the client to become connected.</summary>
        /// <returns>An observable that emits the client when connected.</returns>
        public IObservable<IMqttClient> WaitForConnection() => client.WaitForConnection(null);

        /// <summary>Waits for the client to become connected.</summary>
        /// <param name="timeout">Maximum time to wait for connection. Null means no timeout.</param>
        /// <returns>An observable that emits the client when connected.</returns>
        public IObservable<IMqttClient> WaitForConnection(TimeSpan? timeout) =>
            client.SelectMany(c =>
            {
                if (c.IsConnected)
                {
                    return Signal.Return(c);
                }

                var connected = c.Connected().Take(1).Select(_ => c);

                if (timeout.HasValue)
                {
                    connected = connected.Timeout(timeout.Value);
                }

                return connected;
            });

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(string topic, string payload) =>
            client.Publish(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false);

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos) => client.Publish(topic, payload, qos, false);

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <param name="retain">Whether to retain the message.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos,
            bool retain) =>
            client.SelectMany(c =>
            {
                var message = Create
                    .MqttFactory.CreateApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();
                return Signal.FromAsync(ct => c.PublishAsync(message, ct));
            });

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(string topic, byte[] payload) =>
            client.Publish(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false);

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos) => client.Publish(topic, payload, qos, false);

        /// <summary>Publishes a message and returns an observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <param name="retain">Whether to retain the message.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos,
            bool retain) =>
            client.SelectMany(c =>
            {
                var message = Create
                    .MqttFactory.CreateApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();
                return Signal.FromAsync(ct => c.PublishAsync(message, ct));
            });

        /// <summary>Publishes a message using a builder action.</summary>
        /// <param name="messageBuilder">An action to configure the message.</param>
        /// <returns>An observable that emits the publish result.</returns>
        public IObservable<MqttClientPublishResult> Publish(
            Action<MqttApplicationMessageBuilder> messageBuilder) =>
            client.SelectMany(c =>
            {
                var builder = Create.MqttFactory.CreateApplicationMessageBuilder();
                messageBuilder(builder);
                return Signal.FromAsync(ct => c.PublishAsync(builder.Build(), ct));
            });

        /// <summary>Publishes multiple messages in sequence.</summary>
        /// <param name="messages">The observable sequence of messages to publish.</param>
        /// <returns>An observable that emits the publish result for each message.</returns>
        public IObservable<MqttClientPublishResult> PublishMany(
            IObservable<MqttApplicationMessage> messages) =>
            client
                .CombineLatest(messages, static (c, m) => (Client: c, Message: m))
                .SelectMany(x => Signal.FromAsync(ct => x.Client.PublishAsync(x.Message, ct)));

        /// <summary>Gets the underlying MQTT client options.</summary>
        /// <returns>An observable that emits the client options.</returns>
        public IObservable<MqttClientOptions?> GetOptions() =>
            client.Select(static c => (MqttClientOptions?)c.Options);
    }

    /// <summary>Provides asynchronous reactive operations for observable MQTT clients.</summary>
    /// <param name="client">The asynchronous MQTT client stream.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Sends a ping request.</summary>
        /// <returns>An asynchronous observable that emits unit when the ping completes successfully.</returns>
        public IObservableAsync<RxUnit> Ping()
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(static c => CreateObservable.FromAsyncTask(c.PingAsync));
        }

        /// <summary>Sends periodic ping requests to maintain the connection using asynchronous observables.</summary>
        /// <returns>An asynchronous observable that emits unit for each successful ping.</returns>
        public IObservableAsync<RxUnit> PingPeriodically() => client.PingPeriodically(null);

        /// <summary>Sends periodic ping requests to maintain the connection using asynchronous observables.</summary>
        /// <param name="interval">The interval between pings. Null uses the default 30-second interval.</param>
        /// <returns>An asynchronous observable that emits unit for each successful ping.</returns>
        public IObservableAsync<RxUnit> PingPeriodically(TimeSpan? interval)
        {
            ArgumentNullException.ThrowIfNull(client);
            var resolvedInterval = interval ?? TimeSpan.FromSeconds(DefaultPingIntervalSeconds);

            return client
                .SelectMany(c =>
                    SignalAsync
                        .Interval(
                            resolvedInterval,
                            TimeProvider.System)
                        .SelectMany(_ => CreateObservable.FromAsyncTask(c.PingAsync)))
                .Retry();
        }

        /// <summary>Subscribes to the specified topics.</summary>
        /// <param name="topics">The topics to subscribe to.</param>
        /// <returns>An asynchronous observable that emits the subscription result.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(string[] topics) =>
            client.Subscribe(topics, MqttQualityOfServiceLevel.AtMostOnce);

        /// <summary>Subscribes to topics with the specified quality of service.</summary>
        /// <param name="topics">The topics to subscribe to.</param>
        /// <param name="qualityOfServiceLevel">The QoS level for all subscriptions.</param>
        /// <returns>An asynchronous observable that emits the subscription result.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(
            string[] topics,
            MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                foreach (var topic in topics)
                {
                    _ = optionsBuilder.WithTopicFilter(f =>
                        f.WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel));
                }

                return CreateObservable.FromAsyncTask(ct =>
                    c.SubscribeAsync(optionsBuilder.Build(), ct));
            });
        }

        /// <summary>Subscribes with a caller-configured topic filter.</summary>
        /// <param name="topicFilterBuilder">An action to configure the topic filter.</param>
        /// <returns>An asynchronous observable that emits the subscription result.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(
            Action<MqttTopicFilterBuilder> topicFilterBuilder)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                _ = optionsBuilder.WithTopicFilter(topicFilterBuilder);
                return CreateObservable.FromAsyncTask(ct =>
                    c.SubscribeAsync(optionsBuilder.Build(), ct));
            });
        }

        /// <summary>Subscribes to the specified topic filters using asynchronous observables.</summary>
        /// <param name="topicFilters">The topic filters to subscribe to.</param>
        /// <returns>An asynchronous observable that emits the subscription result.</returns>
        public IObservableAsync<MqttClientSubscribeResult> Subscribe(
            params MqttTopicFilter[] topicFilters)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
                foreach (var filter in topicFilters)
                {
                    _ = optionsBuilder.WithTopicFilter(filter);
                }

                return CreateObservable.FromAsyncTask(ct =>
                    c.SubscribeAsync(optionsBuilder.Build(), ct));
            });
        }

        /// <summary>Unsubscribes from the specified topics using asynchronous observables.</summary>
        /// <param name="topics">The topics to unsubscribe from.</param>
        /// <returns>An asynchronous observable that emits the unsubscription result.</returns>
        public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(params string[] topics)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var optionsBuilder = Create.MqttFactory.CreateUnsubscribeOptionsBuilder();
                foreach (var topic in topics)
                {
                    _ = optionsBuilder.WithTopicFilter(topic);
                }

                return CreateObservable.FromAsyncTask(ct =>
                    c.UnsubscribeAsync(optionsBuilder.Build(), ct));
            });
        }

        /// <summary>Disconnects the MQTT client using asynchronous observables.</summary>
        /// <returns>An asynchronous observable that completes when disconnection is done.</returns>
        public IObservableAsync<RxUnit> Disconnect() =>
            client.Disconnect(MqttClientDisconnectOptionsReason.NormalDisconnection);

        /// <summary>Disconnects the MQTT client using asynchronous observables.</summary>
        /// <param name="reason">The disconnect reason.</param>
        /// <returns>An asynchronous observable that completes when disconnection is done.</returns>
        public IObservableAsync<RxUnit> Disconnect(MqttClientDisconnectOptionsReason reason)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var options = Create
                    .MqttFactory.CreateClientDisconnectOptionsBuilder()
                    .WithReason(reason)
                    .Build();
                return CreateObservable.FromAsyncTask(ct => c.DisconnectAsync(options, ct));
            });
        }

        /// <summary>Reconnects the MQTT client using its previous connection options.</summary>
        /// <returns>An asynchronous observable that completes when reconnection is done.</returns>
        public IObservableAsync<RxUnit> Reconnect()
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(static c =>
                CreateObservable.FromAsyncTask(c.ReconnectAsync).Select(static _ => RxUnit.Default));
        }

        /// <summary>Gets an asynchronous observable that emits the connection status of the client.</summary>
        /// <returns>An asynchronous observable that emits true when connected and false when disconnected.</returns>
        public IObservableAsync<bool> ConnectionStatus()
        {
            ArgumentNullException.ThrowIfNull(client);

            return SignalAsync
                .Create<bool>(async (observer, cancellationToken) =>
                {
                    var subscriptions = new MultipleDisposableAsync();
                    var lastStatus = -1;

                    ValueTask PublishStatusAsync(bool status, CancellationToken token)
                    {
                        var encodedStatus = status ? 1 : 0;
                        return Interlocked.Exchange(ref lastStatus, encodedStatus) == encodedStatus
                            ? default
                            : observer.OnNextAsync(status, token);
                    }

                    await subscriptions.AddAsync(
                        await client.SubscribeAsync(
                            async (mqttClient, token) =>
                            {
                                await PublishStatusAsync(mqttClient.IsConnected, token).ConfigureAwait(false);
                                var eventHandlers = new MqttClientAsyncEventHandlers(mqttClient);
                                await subscriptions.AddAsync(
                                    await CreateObservable
                                        .FromAsyncEventSignal<MqttClientConnectedEventArgs>(
                                            eventHandlers.AddConnected,
                                            eventHandlers.RemoveConnected)
                                        .SubscribeAsync(
                                            (_, handlerToken) => PublishStatusAsync(true, handlerToken),
                                            cancellationToken)
                                        .ConfigureAwait(false)).ConfigureAwait(false);
                                await subscriptions.AddAsync(
                                    await CreateObservable
                                        .FromAsyncEventSignal<MqttClientDisconnectedEventArgs>(
                                            eventHandlers.AddDisconnected,
                                            eventHandlers.RemoveDisconnected)
                                        .SubscribeAsync(
                                            (_, handlerToken) => PublishStatusAsync(false, handlerToken),
                                            cancellationToken)
                                        .ConfigureAwait(false)).ConfigureAwait(false);
                            },
                            cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                    return subscriptions;
                });
        }

        /// <summary>Waits for the client to become connected using asynchronous observables.</summary>
        /// <returns>An asynchronous observable that emits the client when connected.</returns>
        public IObservableAsync<IMqttClient> WaitForConnection() => client.WaitForConnection(null);

        /// <summary>Waits for the client to become connected using asynchronous observables.</summary>
        /// <param name="timeout">Maximum time to wait for connection. Null means no timeout.</param>
        /// <returns>An asynchronous observable that emits the client when connected.</returns>
        public IObservableAsync<IMqttClient> WaitForConnection(TimeSpan? timeout)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var eventHandlers = new MqttClientAsyncEventHandlers(c);
                var connected = c.IsConnected
                    ? SignalAsync.Return(c)
                    : CreateObservable
                        .FromAsyncEventSignal<MqttClientConnectedEventArgs>(
                            eventHandlers.AddConnected,
                            eventHandlers.RemoveConnected)
                        .Take(1)
                        .Select(_ => c);

                return timeout.HasValue
                    ? connected.Timeout(timeout.Value, TimeProvider.System)
                    : connected;
            });
        }

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(string topic, string payload) =>
            client.Publish(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false);

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos) => client.Publish(topic, payload, qos, false);

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as a string.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <param name="retain">Whether to retain the message.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(
            string topic,
            string payload,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var message = Create
                    .MqttFactory.CreateApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();
                return CreateObservable.FromAsyncTask(ct => c.PublishAsync(message, ct));
            });
        }

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(string topic, byte[] payload) =>
            client.Publish(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false);

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos) => client.Publish(topic, payload, qos, false);

        /// <summary>Publishes a message and returns an asynchronous observable of the publish result.</summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="payload">The message payload as bytes.</param>
        /// <param name="qos">The quality of service level.</param>
        /// <param name="retain">Whether to retain the message.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(
            string topic,
            byte[] payload,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var message = Create
                    .MqttFactory.CreateApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();
                return CreateObservable.FromAsyncTask(ct => c.PublishAsync(message, ct));
            });
        }

        /// <summary>Publishes a message using a builder action.</summary>
        /// <param name="messageBuilder">An action to configure the message.</param>
        /// <returns>An asynchronous observable that emits the publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> Publish(
            Action<MqttApplicationMessageBuilder> messageBuilder)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.SelectMany(c =>
            {
                var builder = Create.MqttFactory.CreateApplicationMessageBuilder();
                messageBuilder(builder);
                return CreateObservable.FromAsyncTask(ct => c.PublishAsync(builder.Build(), ct));
            });
        }

        /// <summary>Publishes multiple messages in sequence using asynchronous observables.</summary>
        /// <param name="messages">The asynchronous observable sequence of messages to publish.</param>
        /// <returns>An asynchronous observable that emits the publish result for each message.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMany(
            IObservableAsync<MqttApplicationMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client
                .CombineLatest(messages, static (c, m) => (Client: c, Message: m))
                .SelectMany(x =>
                    CreateObservable.FromAsyncTask(ct => x.Client.PublishAsync(x.Message, ct)));
        }

        /// <summary>Gets an asynchronous observable that emits the underlying MQTT client options.</summary>
        /// <returns>An asynchronous observable that emits the client options.</returns>
        public IObservableAsync<MqttClientOptions?> GetOptions()
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.Select(static c => (MqttClientOptions?)c.Options);
        }
    }
}
