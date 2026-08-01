// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes public behavioral coverage for the observable bridge and payload helpers.</summary>
public sealed class BridgePayloadFinalClosureTests
{
    /// <summary>The expected numeric conversion result.</summary>
    private const int ExpectedInteger = 42;

    /// <summary>The expected number of topic levels.</summary>
    private const int ExpectedLevelCount = 3;

    /// <summary>The expected number of raw client publishes.</summary>
    private const int ExpectedPublishCount = 6;

    /// <summary>The expected numeric conversion results.</summary>
    private static readonly int[] ExpectedIntegers = [ExpectedInteger];

    /// <summary>The bounded wait used for asynchronous bridge handoffs.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>Verifies canceled subscriptions do not attach to synchronous sources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ToSignal_CanceledSubscriptionDoesNotAttachSourceAsync()
    {
        using ReactiveUI.Primitives.Signals.Signal<int> source = new();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var delivered = false;

        await using var subscription = await source.ToSignal().SubscribeAsync(
            (value, token) =>
            {
                _ = value;
                _ = token;
                delivered = true;
                return ValueTask.CompletedTask;
            },
            static (_, _) => ValueTask.CompletedTask,
            static _ => ValueTask.CompletedTask,
            cancellation.Token);
        await Assert.That(delivered).IsFalse();
    }

    /// <summary>Verifies both terminal outcomes cross the asynchronous-to-synchronous bridge.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ToObservable_ForwardsSuccessfulAndFailedTerminalsAsync()
    {
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("terminal failure");
        var failed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var completedSubscription = SignalAsync.Return(1).ToObservable().Subscribe(
            static _ => { },
            error => _ = completed.TrySetException(error),
            () => _ = completed.TrySetResult(true));
        using var failedSubscription = SignalAsync.Fail<int>(failure).ToObservable().Subscribe(
            static _ => { },
            error => _ = failed.TrySetResult(error));

        await Assert.That(await completed.Task.WaitAsync(Timeout)).IsTrue();
        await Assert.That(await failed.Task.WaitAsync(Timeout)).IsSameReferenceAs(failure);
    }

    /// <summary>Verifies topic extraction accepts a capture followed by a literal suffix.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ExtractTopicValues_CapturesBeforeLiteralSuffixAsync()
    {
        var message = TestDataHelpers.CreateMessageReceivedArgs("root/alphabetx/value", "payload");

        var extracted = await SignalAsync.Return(message)
            .ExtractTopicValues("root/{name}x/value")
            .ToObservable()
            .CollectAsync(Timeout);

        await Assert.That(extracted).Count().IsEqualTo(1);
        await Assert.That(extracted[0].Values["name"]).IsEqualTo("alphabet");
    }

    /// <summary>Verifies async message and raw-client convenience operations preserve their public contracts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncBridge_ProjectsFiltersConvertsAndPublishesRawMessagesAsync()
    {
        const string Topic = "bridge/level/value";
        const string Payload = "42";
        var message = TestDataHelpers.CreateMessageReceivedArgs(Topic, Payload);
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> messages = SignalAsync.Return(message);
        var values = SignalAsync.Return<object>(Payload).Select(static value => (object?)value);

        var text = await messages.ToUtf8String().ToObservable().CollectAsync(Timeout);
        var matching = await messages.WhereTopicIsMatch("bridge/#").ToObservable().CollectAsync(Timeout);
        var excluded = await messages.WhereTopicIsNotMatch("missing/#")
            .ToObservable()
            .CollectAsync(Timeout);
        var levelMatch = await messages.WhereTopicLevelCount(ExpectedLevelCount)
            .ToObservable()
            .CollectAsync(Timeout);
        var levelMiss = await messages.WhereTopicLevelCount(1).ToObservable().CollectAsync(Timeout);
        var foundLevel = await messages.SelectTopicLevel(1).ToObservable().CollectAsync(Timeout);
        var missingLevel = await messages.SelectTopicLevel(-1).ToObservable().CollectAsync(Timeout);
        var integers = await values.ToInt32().ToObservable().CollectAsync(Timeout);

        using var client = new MockMqttClient();
        IObservableAsync<IMqttClient> clients = SignalAsync.Return<IMqttClient>(client);
        var textPayload = SignalAsync.Return(("bridge/text", Payload));
        var bytePayload = SignalAsync.Return(("bridge/bytes", Payload: "bytes"u8.ToArray()));

        await PublishRawMessagesAsync(clients, textPayload, bytePayload);

        await Assert.That(text).IsEquivalentTo([Payload]);
        await Assert.That(matching).Count().IsEqualTo(1);
        await Assert.That(excluded).Count().IsEqualTo(1);
        await Assert.That(levelMatch).Count().IsEqualTo(1);
        await Assert.That(levelMiss).IsEmpty();
        await Assert.That(foundLevel).IsEquivalentTo(["level"]);
        await Assert.That(missingLevel).IsEmpty();
        await Assert.That(integers).IsEquivalentTo(ExpectedIntegers);
        await Assert.That(client.PublishedMessages).Count().IsEqualTo(ExpectedPublishCount);
    }

    /// <summary>Publishes text and binary payloads through every async raw-client convenience overload.</summary>
    /// <param name="clients">The asynchronous source that emits the MQTT client.</param>
    /// <param name="textPayload">The MQTT topic and text payload to publish.</param>
    /// <param name="bytePayload">The MQTT topic and binary payload to publish.</param>
    /// <returns>A task that represents the asynchronous publish operations.</returns>
    private static async Task PublishRawMessagesAsync(
        IObservableAsync<IMqttClient> clients,
        IObservableAsync<(string Topic, string Payload)> textPayload,
        IObservableAsync<(string Topic, byte[] Payload)> bytePayload)
    {
        _ = await clients.PublishMessage(textPayload).ToObservable().FirstAsync(Timeout);
        _ = await clients.PublishMessage(textPayload, MqttQualityOfServiceLevel.AtLeastOnce)
            .ToObservable()
            .FirstAsync(Timeout);
        _ = await clients.PublishMessage(textPayload, MqttQualityOfServiceLevel.AtMostOnce, false)
            .ToObservable()
            .FirstAsync(Timeout);
        _ = await clients.PublishMessage(bytePayload).ToObservable().FirstAsync(Timeout);
        _ = await clients.PublishMessage(bytePayload, MqttQualityOfServiceLevel.AtLeastOnce)
            .ToObservable()
            .FirstAsync(Timeout);
        _ = await clients.PublishMessage(bytePayload, MqttQualityOfServiceLevel.AtMostOnce, false)
            .ToObservable()
            .FirstAsync(Timeout);
    }
}
