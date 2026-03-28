// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using ReactiveUI.Extensions.Async;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides reactive extension methods for IMqttClient operations such as ping, subscribe, and unsubscribe.
/// </summary>
/// <remarks>
/// These extensions wrap asynchronous MQTT client operations as observables for seamless integration
/// with reactive programming patterns.
/// </remarks>
public static class ReactiveClientOperations
{
    /// <summary>
    /// Sends a ping request to the MQTT broker and returns an observable that completes when the ping response is received.
    /// </summary>
    /// <param name="client">The observable MQTT client to send the ping from.</param>
    /// <returns>An observable that emits unit when the ping completes successfully.</returns>
    public static IObservable<System.Reactive.Unit> Ping(this IObservable<IMqttClient> client) =>
        client.SelectMany(c => Observable.FromAsync(ct => c.PingAsync(ct)))
            .Select(_ => System.Reactive.Unit.Default);

    /// <summary>
    /// Sends a ping request to the MQTT broker and returns an asynchronous observable that completes when the ping response is received.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client to send the ping from.</param>
    /// <returns>An asynchronous observable that emits unit when the ping completes successfully.</returns>
    public static IObservableAsync<System.Reactive.Unit> Ping(this IObservableAsync<IMqttClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c => CreateObservable.FromAsyncTask(ct => c.PingAsync(ct)));
    }

    /// <summary>
    /// Sends periodic ping requests to maintain the connection.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="interval">The interval between pings. Default is 30 seconds.</param>
    /// <returns>An observable that emits unit for each successful ping.</returns>
    public static IObservable<System.Reactive.Unit> PingPeriodically(
        this IObservable<IMqttClient> client,
        TimeSpan? interval = null) =>
        client.SelectMany(c =>
            Observable.Interval(interval ?? TimeSpan.FromSeconds(30))
                .SelectMany(_ => Observable.FromAsync(ct => c.PingAsync(ct)))
                .Select(_ => System.Reactive.Unit.Default))
            .Retry();

    /// <summary>
    /// Sends periodic ping requests to maintain the connection using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="interval">The interval between pings. Default is 30 seconds.</param>
    /// <returns>An asynchronous observable that emits unit for each successful ping.</returns>
    public static IObservableAsync<System.Reactive.Unit> PingPeriodically(
        this IObservableAsync<IMqttClient> client,
        TimeSpan? interval = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
                ObservableAsync.Interval(interval ?? TimeSpan.FromSeconds(30), TimeProvider.System)
                    .SelectMany(_ => CreateObservable.FromAsyncTask(ct => c.PingAsync(ct))))
            .Retry();
    }

    /// <summary>
    /// Subscribes to the specified topics and returns an observable of the subscription results.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topics">The topics to subscribe to.</param>
    /// <param name="qualityOfServiceLevel">The QoS level for all subscriptions. Default is AtMostOnce.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        this IObservable<IMqttClient> client,
        string[] topics,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce) =>
        client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                optionsBuilder.WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel));
            }

            return Observable.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });

    /// <summary>
    /// Subscribes to the specified topics and returns an asynchronous observable of the subscription results.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topics">The topics to subscribe to.</param>
    /// <param name="qualityOfServiceLevel">The QoS level for all subscriptions.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        this IObservableAsync<IMqttClient> client,
        string[] topics,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                optionsBuilder.WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel));
            }

            return CreateObservable.FromAsyncTask(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });
    }

    /// <summary>
    /// Subscribes to the specified topic with custom filter configuration.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topicFilterBuilder">An action to configure the topic filter.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        this IObservable<IMqttClient> client,
        Action<MqttTopicFilterBuilder> topicFilterBuilder) =>
        client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            optionsBuilder.WithTopicFilter(topicFilterBuilder);
            return Observable.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });

    /// <summary>
    /// Subscribes to the specified topic with custom filter configuration using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topicFilterBuilder">An action to configure the topic filter.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        this IObservableAsync<IMqttClient> client,
        Action<MqttTopicFilterBuilder> topicFilterBuilder)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            optionsBuilder.WithTopicFilter(topicFilterBuilder);
            return CreateObservable.FromAsyncTask(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });
    }

    /// <summary>
    /// Subscribes to the specified topic filters.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topicFilters">The topic filters to subscribe to.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        this IObservable<IMqttClient> client,
        params MqttTopicFilter[] topicFilters) =>
        client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            foreach (var filter in topicFilters)
            {
                optionsBuilder.WithTopicFilter(filter);
            }

            return Observable.FromAsync(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });

    /// <summary>
    /// Subscribes to the specified topic filters using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topicFilters">The topic filters to subscribe to.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        this IObservableAsync<IMqttClient> client,
        params MqttTopicFilter[] topicFilters)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateSubscribeOptionsBuilder();
            foreach (var filter in topicFilters)
            {
                optionsBuilder.WithTopicFilter(filter);
            }

            return CreateObservable.FromAsyncTask(ct => c.SubscribeAsync(optionsBuilder.Build(), ct));
        });
    }

    /// <summary>
    /// Unsubscribes from the specified topics.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topics">The topics to unsubscribe from.</param>
    /// <returns>An observable that emits the unsubscription result.</returns>
    public static IObservable<MqttClientUnsubscribeResult> Unsubscribe(
        this IObservable<IMqttClient> client,
        params string[] topics) =>
        client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateUnsubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                optionsBuilder.WithTopicFilter(topic);
            }

            return Observable.FromAsync(ct => c.UnsubscribeAsync(optionsBuilder.Build(), ct));
        });

    /// <summary>
    /// Unsubscribes from the specified topics using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topics">The topics to unsubscribe from.</param>
    /// <returns>An asynchronous observable that emits the unsubscription result.</returns>
    public static IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(
        this IObservableAsync<IMqttClient> client,
        params string[] topics)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var optionsBuilder = Create.MqttFactory.CreateUnsubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                optionsBuilder.WithTopicFilter(topic);
            }

            return CreateObservable.FromAsyncTask(ct => c.UnsubscribeAsync(optionsBuilder.Build(), ct));
        });
    }

    /// <summary>
    /// Disconnects the MQTT client.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="reason">The disconnect reason. Default is NormalDisconnection.</param>
    /// <returns>An observable that completes when disconnection is done.</returns>
    public static IObservable<System.Reactive.Unit> Disconnect(
        this IObservable<IMqttClient> client,
        MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection) =>
        client.SelectMany(c =>
        {
            var options = Create.MqttFactory.CreateClientDisconnectOptionsBuilder()
                .WithReason(reason)
                .Build();
            return Observable.FromAsync(ct => c.DisconnectAsync(options, ct));
        }).Select(_ => System.Reactive.Unit.Default);

    /// <summary>
    /// Disconnects the MQTT client using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="reason">The disconnect reason.</param>
    /// <returns>An asynchronous observable that completes when disconnection is done.</returns>
    public static IObservableAsync<System.Reactive.Unit> Disconnect(
        this IObservableAsync<IMqttClient> client,
        MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var options = Create.MqttFactory.CreateClientDisconnectOptionsBuilder()
                .WithReason(reason)
                .Build();
            return CreateObservable.FromAsyncTask(ct => c.DisconnectAsync(options, ct));
        });
    }

    /// <summary>
    /// Reconnects the MQTT client using the previous connection options.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <returns>An observable that completes when reconnection is done.</returns>
    public static IObservable<System.Reactive.Unit> Reconnect(this IObservable<IMqttClient> client) =>
        client.SelectMany(c => Observable.FromAsync(ct => c.ReconnectAsync(ct)))
            .Select(_ => System.Reactive.Unit.Default);

    /// <summary>
    /// Reconnects the MQTT client using the previous connection options and asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <returns>An asynchronous observable that completes when reconnection is done.</returns>
    public static IObservableAsync<System.Reactive.Unit> Reconnect(this IObservableAsync<IMqttClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c => CreateObservable.FromAsyncTask(ct => c.ReconnectAsync(ct))
            .Select(_ => System.Reactive.Unit.Default));
    }

    /// <summary>
    /// Gets an observable that emits the connection status of the client.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <returns>An observable that emits true when connected and false when disconnected.</returns>
    public static IObservable<bool> ConnectionStatus(this IObservable<IMqttClient> client) =>
        Observable.Create<bool>(observer =>
        {
            var disposable = new CompositeDisposable();

            disposable.Add(client.Subscribe(c =>
            {
                observer.OnNext(c.IsConnected);
                disposable.Add(c.Connected().Subscribe(_ => observer.OnNext(true)));
                disposable.Add(c.Disconnected().Subscribe(_ => observer.OnNext(false)));
            }));

            return disposable;
        }).DistinctUntilChanged().Publish().RefCount();

    /// <summary>
    /// Gets an asynchronous observable that emits the connection status of the client.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <returns>An asynchronous observable that emits true when connected and false when disconnected.</returns>
    public static IObservableAsync<bool> ConnectionStatus(this IObservableAsync<IMqttClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
                ObservableAsync.Return(c.IsConnected)
                    .Concat(ObservableAsync.Merge(
                        c.ObserveConnectedAsync().Select(_ => true),
                        c.ObserveDisconnectedAsync().Select(_ => false))))
            .DistinctUntilChanged()
            .Publish()
            .RefCount();
    }

    /// <summary>
    /// Waits for the client to become connected.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="timeout">Maximum time to wait for connection. Null for no timeout.</param>
    /// <returns>An observable that emits the client when connected.</returns>
    public static IObservable<IMqttClient> WaitForConnection(
        this IObservable<IMqttClient> client,
        TimeSpan? timeout = null) =>
        Observable.Create<IMqttClient>(observer =>
        {
            var disposable = new CompositeDisposable();

            disposable.Add(client.Subscribe(c =>
            {
                if (c.IsConnected)
                {
                    observer.OnNext(c);
                    observer.OnCompleted();
                    return;
                }

                var connected = c.Connected()
                    .Take(1)
                    .Select(_ => c);

                if (timeout.HasValue)
                {
                    connected = connected.Timeout(timeout.Value);
                }

                disposable.Add(connected.Subscribe(observer));
            }));

            return disposable;
        });

    /// <summary>
    /// Waits for the client to become connected using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="timeout">Maximum time to wait for connection. Null for no timeout.</param>
    /// <returns>An asynchronous observable that emits the client when connected.</returns>
    public static IObservableAsync<IMqttClient> WaitForConnection(
        this IObservableAsync<IMqttClient> client,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var connected = c.IsConnected
                ? ObservableAsync.Return(c)
                : c.ObserveConnectedAsync()
                    .Take(1)
                    .Select(_ => c);

            return timeout.HasValue
                ? connected.Timeout(timeout.Value, TimeProvider.System)
                : connected;
        });
    }

    /// <summary>
    /// Publishes a message and returns an observable of the publish result.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The message payload as a string.</param>
    /// <param name="qos">The quality of service level. Default is AtMostOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is false.</param>
    /// <returns>An observable that emits the publish result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        this IObservable<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false) =>
        client.SelectMany(c =>
        {
            var message = Create.MqttFactory.CreateApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain)
                .Build();
            return Observable.FromAsync(ct => c.PublishAsync(message, ct));
        });

    /// <summary>
    /// Publishes a message and returns an asynchronous observable of the publish result.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The message payload as a string.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether to retain the message.</param>
    /// <returns>An asynchronous observable that emits the publish result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        this IObservableAsync<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var message = Create.MqttFactory.CreateApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain)
                .Build();
            return CreateObservable.FromAsyncTask(ct => c.PublishAsync(message, ct));
        });
    }

    /// <summary>
    /// Publishes a message and returns an observable of the publish result.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <param name="qos">The quality of service level. Default is AtMostOnce.</param>
    /// <param name="retain">Whether to retain the message. Default is false.</param>
    /// <returns>An observable that emits the publish result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        this IObservable<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false) =>
        client.SelectMany(c =>
        {
            var message = Create.MqttFactory.CreateApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain)
                .Build();
            return Observable.FromAsync(ct => c.PublishAsync(message, ct));
        });

    /// <summary>
    /// Publishes a message and returns an asynchronous observable of the publish result.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The message payload as bytes.</param>
    /// <param name="qos">The quality of service level.</param>
    /// <param name="retain">Whether to retain the message.</param>
    /// <returns>An asynchronous observable that emits the publish result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        this IObservableAsync<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.SelectMany(c =>
        {
            var message = Create.MqttFactory.CreateApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain)
                .Build();
            return CreateObservable.FromAsyncTask(ct => c.PublishAsync(message, ct));
        });
    }

    /// <summary>
    /// Publishes a message using a builder action.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="messageBuilder">An action to configure the message.</param>
    /// <returns>An observable that emits the publish result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        this IObservable<IMqttClient> client,
        Action<MqttApplicationMessageBuilder> messageBuilder) =>
        client.SelectMany(c =>
        {
            var builder = Create.MqttFactory.CreateApplicationMessageBuilder();
            messageBuilder(builder);
            return Observable.FromAsync(ct => c.PublishAsync(builder.Build(), ct));
        });

    /// <summary>
    /// Publishes a message using a builder action and returns an asynchronous observable of the publish result.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="messageBuilder">An action to configure the message.</param>
    /// <returns>An asynchronous observable that emits the publish result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        this IObservableAsync<IMqttClient> client,
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

    /// <summary>
    /// Publishes multiple messages in sequence.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <param name="messages">The observable sequence of messages to publish.</param>
    /// <returns>An observable that emits the publish result for each message.</returns>
    public static IObservable<MqttClientPublishResult> PublishMany(
        this IObservable<IMqttClient> client,
        IObservable<MqttApplicationMessage> messages) =>
        client.CombineLatest(messages, (c, m) => (Client: c, Message: m))
            .SelectMany(x => Observable.FromAsync(ct => x.Client.PublishAsync(x.Message, ct)));

    /// <summary>
    /// Publishes multiple messages in sequence using asynchronous observables.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <param name="messages">The asynchronous observable sequence of messages to publish.</param>
    /// <returns>An asynchronous observable that emits the publish result for each message.</returns>
    public static IObservableAsync<MqttClientPublishResult> PublishMany(
        this IObservableAsync<IMqttClient> client,
        IObservableAsync<MqttApplicationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.CombineLatest(messages, (c, m) => (Client: c, Message: m))
            .SelectMany(x => CreateObservable.FromAsyncTask(ct => x.Client.PublishAsync(x.Message, ct)));
    }

    /// <summary>
    /// Gets the underlying MQTT client options.
    /// </summary>
    /// <param name="client">The observable MQTT client.</param>
    /// <returns>An observable that emits the client options.</returns>
    public static IObservable<MqttClientOptions?> GetOptions(this IObservable<IMqttClient> client) =>
        client.Select(c => c.Options);

    /// <summary>
    /// Gets an asynchronous observable that emits the underlying MQTT client options.
    /// </summary>
    /// <param name="client">The asynchronous observable MQTT client.</param>
    /// <returns>An asynchronous observable that emits the client options.</returns>
    public static IObservableAsync<MqttClientOptions?> GetOptions(this IObservableAsync<IMqttClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.Select(c => c.Options);
    }
}
