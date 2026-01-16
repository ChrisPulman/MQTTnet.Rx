// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for publishing MQTT messages using observable MQTT clients and message streams.
/// </summary>
/// <remarks>These extension methods enable reactive publishing of MQTT messages by combining client and message
/// observables. Overloads support both string and byte array payloads, as well as custom message configuration via a
/// builder callback. Methods are provided for both standard and resilient MQTT client interfaces, allowing integration
/// with different client implementations. All methods return observables that emit results or events corresponding to
/// the publish operation, supporting reactive programming patterns.</remarks>
public static class MqttdPublishExtensions
{
    /// <summary>
    /// Publishes messages to an MQTT broker using the specified client and message streams.
    /// </summary>
    /// <remarks>The method combines the latest values from the client and message observables to publish
    /// messages as they arrive. The returned observable will retry on errors. Each message is published with the
    /// specified quality of service and retain flag.</remarks>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="message">An observable sequence of tuples containing the topic and payload for each message to be published.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of MqttClientPublishResult objects representing the result of each publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
        {
            var disposable = new CompositeDisposable();
            var cts = new CancellationTokenSource();
            disposable.Add(cts);
            var token = cts.Token;

            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess))
                    .Subscribe(async c =>
                    {
                        var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                        .WithTopic(c.mess.topic)
                                        .WithPayload(c.mess.payLoad)
                                        .WithQualityOfServiceLevel(qos)
                                        .WithRetainFlag(retain)
                                        .Build();

                        var result = await c.cli.PublishAsync(applicationMessage, token).ConfigureAwait(false);
                        observer.OnNext(result);
                    }));
            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes MQTT messages to the specified topics using the provided resilient MQTT client and returns an
    /// observable sequence of message processing results.
    /// </summary>
    /// <remarks>The returned observable will retry on errors and will emit an event for each message
    /// processed by the MQTT client. Subscribers should handle potential errors that may occur during message
    /// publishing.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="message">An observable sequence of tuples, each containing the topic and payload for the message to be published.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs that provides the result of each published
    /// message. The sequence completes when all messages have been processed or an error occurs.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservable<IResilientMqttClient> client, IObservable<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<ApplicationMessageProcessedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            var setup = false;
            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                if (!setup)
                {
                    setup = true;
                    disposable.Add(c.cli.ApplicationMessageProcessed.Retry().Subscribe(observer));
                }

                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                try
                {
                    await c.cli.EnqueueAsync(applicationMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }));

            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes MQTT messages to the specified topics using the provided resilient MQTT client and returns an
    /// observable sequence of message processing results.
    /// </summary>
    /// <remarks>The returned observable emits a result for each message published and will propagate errors
    /// if message publishing fails. The method subscribes to the client's message processed events and automatically
    /// retries on errors. The caller is responsible for managing the lifetime of the subscription.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="message">An observable sequence of tuples, each containing the topic and payload to be published.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of <see cref="ApplicationMessageProcessedEventArgs"/> that provides the result of each
    /// published message.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservable<IResilientMqttClient> client, IObservable<(string topic, byte[] payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<ApplicationMessageProcessedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            var setup = false;
            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                if (!setup)
                {
                    setup = true;
                    disposable.Add(c.cli.ApplicationMessageProcessed.Retry().Subscribe(observer));
                }

                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                try
                {
                    await c.cli.EnqueueAsync(applicationMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }));

            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes MQTT messages to the specified topics using the provided client and message streams.
    /// </summary>
    /// <remarks>The returned observable will retry publishing on error. The method combines the latest client
    /// and message values, and publishes each message as they arrive. The caller is responsible for managing the
    /// lifetimes of the input observables.</remarks>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="message">An observable sequence of tuples containing the topic and payload for each message to be published.</param>
    /// <param name="messageBuilder">A delegate that configures additional properties of the MQTT application message before publishing.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of results for each published message, indicating the outcome of the publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, string payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
        {
            var disposable = new CompositeDisposable();
            var cts = new CancellationTokenSource();
            disposable.Add(cts);
            var token = cts.Token;

            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess))
                .Subscribe(async c =>
                {
                    var builder = Create.MqttFactory.CreateApplicationMessageBuilder()
                                    .WithTopic(c.mess.topic)
                                    .WithPayload(c.mess.payLoad)
                                    .WithQualityOfServiceLevel(qos)
                                    .WithRetainFlag(retain);
                    messageBuilder(builder);

                    var result = await c.cli.PublishAsync(builder.Build(), token).ConfigureAwait(false);
                    observer.OnNext(result);
                }));
            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes messages to an MQTT broker for each client-message pair in the source sequences.
    /// </summary>
    /// <remarks>The method combines the latest values from the client and message sequences, publishing each
    /// message to the specified topic using the provided client. The returned observable emits a result for each
    /// publish operation and automatically retries on error. This method is intended for use in reactive programming
    /// scenarios with MQTT.</remarks>
    /// <param name="client">An observable sequence of MQTT clients to use for publishing messages.</param>
    /// <param name="message">An observable sequence of tuples containing the topic and payload for each message to publish. The topic
    /// specifies the destination topic, and the payload contains the message data as a byte array.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of MqttClientPublishResult objects representing the result of each publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, byte[] payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
            client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                var result = await c.cli.PublishAsync(applicationMessage, CancellationToken.None).ConfigureAwait(false);
                observer.OnNext(result);
            })).Retry();

    /// <summary>
    /// Publishes MQTT messages to the specified topics for each client in the observable sequence.
    /// </summary>
    /// <remarks>The method combines each client with each message and publishes the message using the
    /// provided configuration. The returned observable will emit a result for each publish attempt and will
    /// automatically retry on errors. The caller is responsible for managing the lifetime of the
    /// subscriptions.</remarks>
    /// <param name="client">An observable sequence of MQTT clients to which the messages will be published.</param>
    /// <param name="message">An observable sequence of tuples containing the topic and payload for each message to be published.</param>
    /// <param name="messageBuilder">A delegate that configures additional properties of the MQTT application message before publishing.</param>
    /// <param name="qos">The quality of service level to use when publishing the message. The default is
    /// MqttQualityOfServiceLevel.ExactlyOnce.</param>
    /// <param name="retain">A value indicating whether the published message should be retained by the broker. The default is <see
    /// langword="true"/>.</param>
    /// <returns>An observable sequence of results indicating the outcome of each publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, byte[] payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
            client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                var builder = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain);
                messageBuilder(builder);

                var result = await c.cli.PublishAsync(builder.Build(), CancellationToken.None).ConfigureAwait(false);
                observer.OnNext(result);
            })).Retry();
}
