﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
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
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="qos">The QoS.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>
    /// A Mqtt Client Publish Result.
    /// </returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
            client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                var result = await c.cli.PublishAsync(applicationMessage, CancellationToken.None);
                observer.OnNext(result);
            })).Retry();

    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="qos">The qos.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>A Mqtt Client Publish Result.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservable<IManagedMqttClient> client, IObservable<(string topic, string payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<ApplicationMessageProcessedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            var setup = false;
            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                if (!setup)
                {
                    setup = true;
                    disposable.Add(c.cli.ApplicationMessageProcessed().Retry().Subscribe(args => observer.OnNext(args)));
                }

                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                await c.cli.EnqueueAsync(applicationMessage);
            }));

            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="qos">The qos.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>A Mqtt Client Publish Result.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(this IObservable<IManagedMqttClient> client, IObservable<(string topic, byte[] payLoad)> message, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<ApplicationMessageProcessedEventArgs>(observer =>
        {
            var disposable = new CompositeDisposable();
            var setup = false;
            disposable.Add(client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                if (!setup)
                {
                    setup = true;
                    disposable.Add(c.cli.ApplicationMessageProcessed().Retry().Subscribe(args => observer.OnNext(args)));
                }

                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain)
                                .Build();

                await c.cli.EnqueueAsync(applicationMessage);
            }));

            return disposable;
        }).Retry();

    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageBuilder">The message builder.</param>
    /// <param name="qos">The qos.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>A Mqtt Client Publish Result.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, string payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
            client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain);
                messageBuilder(applicationMessage);

                var result = await c.cli.PublishAsync(applicationMessage.Build(), CancellationToken.None);
                observer.OnNext(result);
            })).Retry();

    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="qos">The qos.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>A Mqtt Client Publish Result.</returns>
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

                var result = await c.cli.PublishAsync(applicationMessage, CancellationToken.None);
                observer.OnNext(result);
            })).Retry();

    /// <summary>
    /// Publishes the message.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="message">The message.</param>
    /// <param name="messageBuilder">The message builder.</param>
    /// <param name="qos">The qos.</param>
    /// <param name="retain">if set to <c>true</c> [retain].</param>
    /// <returns>A Mqtt Client Publish Result.</returns>
    public static IObservable<MqttClientPublishResult> PublishMessage(this IObservable<IMqttClient> client, IObservable<(string topic, byte[] payLoad)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.ExactlyOnce, bool retain = true) =>
        Observable.Create<MqttClientPublishResult>(observer =>
            client.CombineLatest(message, (cli, mess) => (cli, mess)).Subscribe(async c =>
            {
                var applicationMessage = Create.MqttFactory.CreateApplicationMessageBuilder()
                                .WithTopic(c.mess.topic)
                                .WithPayload(c.mess.payLoad)
                                .WithQualityOfServiceLevel(qos)
                                .WithRetainFlag(retain);
                messageBuilder(applicationMessage);

                var result = await c.cli.PublishAsync(applicationMessage.Build(), CancellationToken.None);
                observer.OnNext(result);
            })).Retry();
}
