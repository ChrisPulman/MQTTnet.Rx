// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqttd Publish Extensions.
/// </summary>
public static class MqttdPublishExtensions
{
    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">Client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>Publish results.</returns>
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
    /// Publishes the message and projects to ApplicationMessageProcessed for resilient client.
    /// </summary>
    /// <param name="client">Resilient client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>ApplicationMessageProcessed events.</returns>
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
    /// Publishes the message using byte[] payloads with resilient client.
    /// </summary>
    /// <param name="client">Resilient client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>ApplicationMessageProcessed events.</returns>
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
    /// Publishes the message with custom builder.
    /// </summary>
    /// <param name="client">Client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="messageBuilder">Callback to customize the MQTT application message.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>Publish results.</returns>
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
    /// Publishes the message using byte[] payloads.
    /// </summary>
    /// <param name="client">Client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>Publish results.</returns>
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
    /// Publishes the message using byte[] payloads with custom builder.
    /// </summary>
    /// <param name="client">Client observable.</param>
    /// <param name="message">Stream of (topic, payload) tuples.</param>
    /// <param name="messageBuilder">Callback to customize the MQTT application message.</param>
    /// <param name="qos">Quality of Service.</param>
    /// <param name="retain">Retain flag.</param>
    /// <returns>Publish results.</returns>
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
