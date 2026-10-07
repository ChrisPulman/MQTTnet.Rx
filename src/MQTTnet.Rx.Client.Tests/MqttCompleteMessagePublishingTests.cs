// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies complete-message publishing retains every MQTT message property.</summary>
public sealed class MqttCompleteMessagePublishingTests
{
    /// <summary>The payload byte representing all set bits.</summary>
    private const byte PayloadByte = byte.MaxValue;

    /// <summary>The message expiration interval in seconds.</summary>
    private const uint ExpirySeconds = 60;

    /// <summary>The binary application payload.</summary>
    private static readonly byte[] Payload = [0, 1, PayloadByte];

    /// <summary>The request correlation bytes.</summary>
    private static readonly byte[] Correlation = "id"u8.ToArray();

    /// <summary>The maximum wait for a publication result.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies raw client publishing forwards the original complete message.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RawPublishingPreservesMessageAsync(bool asynchronous)
    {
        using var client = new MockMqttClient();
        var message = CreateMessage();
        var clients = Signal.Emit<IMqttClient>(client);
        var messages = Signal.Emit(message);
        var operation = asynchronous
            ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
            : clients.PublishMessage(messages);

        await Assert.That(client.PublishedMessages.Count).IsEqualTo(0);
        _ = await operation.FirstAsync(Timeout);
        await Assert.That(client.PublishedMessages.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(client.PublishedMessages[0], message)).IsTrue();
    }

    /// <summary>Verifies synchronous processing during enqueue is observed without losing metadata.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ResilientPublishingObservesSynchronousProcessingAsync(bool asynchronous)
    {
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        using var client = Substitute.For<IResilientMqttClient>();
        var message = CreateMessage();
        MqttApplicationMessage? enqueued = null;
        _ = client.ApplicationMessageProcessed.Returns(processed);
        _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            enqueued = call.Arg<MqttApplicationMessage>();
            processed.OnNext(new(new() { ApplicationMessage = enqueued }, null));
            return Task.CompletedTask;
        });
        var clients = Signal.Emit(client);
        var messages = Signal.Emit(message);
        var operation = asynchronous
            ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
            : clients.PublishMessage(messages);

        await Assert.That(enqueued).IsNull();
        var result = await operation.FirstAsync(Timeout);
        await Assert.That(ReferenceEquals(enqueued, message)).IsTrue();
        await Assert.That(ReferenceEquals(result.ApplicationMessage.ApplicationMessage, message)).IsTrue();
        await Assert.That(result.Exception).IsNull();
    }

    /// <summary>Verifies all complete-message publishing overloads reject null sources.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task PublishingRejectsNullSourcesAsync()
    {
        var messages = Signal.Emit(CreateMessage());
        var raw = Signal.Empty<IMqttClient>();
        var resilient = Signal.Empty<IResilientMqttClient>();
        IObservable<IMqttClient> nullRaw = null!;
        IObservable<IResilientMqttClient> nullResilient = null!;
        IObservable<MqttApplicationMessage> nullMessages = null!;
        IObservableAsync<IMqttClient> nullAsyncRaw = null!;
        IObservableAsync<IResilientMqttClient> nullAsyncResilient = null!;
        IObservableAsync<MqttApplicationMessage> nullAsyncMessages = null!;
        await Assert.That(() => nullRaw.PublishMessage(messages)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishMessage(nullMessages)).Throws<ArgumentNullException>();
        await Assert.That(() => nullResilient.PublishMessage(messages)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishMessage(nullMessages)).Throws<ArgumentNullException>();
        await Assert.That(() => nullAsyncRaw.PublishMessage(messages.ToSignal())).Throws<ArgumentNullException>();
        await Assert.That(() => raw.ToSignal().PublishMessage(nullAsyncMessages)).Throws<ArgumentNullException>();
        await Assert.That(() => nullAsyncResilient.PublishMessage(messages.ToSignal())).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.ToSignal().PublishMessage(nullAsyncMessages)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies publishing surfaces transport and enqueue failures without repeating a message.</summary>
    /// <param name="resilient">Whether to use resilient enqueue operations.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PublishingPropagatesFailureAsync(bool resilient, bool asynchronous)
    {
        var messages = Signal.Emit(CreateMessage());
        var failure = new InvalidOperationException("publish failed");
        if (resilient)
        {
            using var client = Substitute.For<IResilientMqttClient>();
            _ = client.ApplicationMessageProcessed.Returns(Signal.Empty<ApplicationMessageProcessedEventArgs>());
            _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(Task.FromException(failure));
            var clients = Signal.Emit(client);
            var operation = asynchronous
                ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
                : clients.PublishMessage(messages);
            await Assert.That(async () => await operation.FirstAsync(Timeout)).Throws<InvalidOperationException>();
            await Assert.That(CountCalls(client, nameof(IResilientMqttClient.EnqueueAsync)))
                .IsEqualTo(1);
        }
        else
        {
            using var client = new ScriptedMqttClient
            {
                PublishHandler = (_, _) => Task.FromException<MqttClientPublishResult>(failure),
            };
            var clients = Signal.Emit<IMqttClient>(client);
            var operation = asynchronous
                ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
                : clients.PublishMessage(messages);
            await Assert.That(async () => await operation.FirstAsync(Timeout)).Throws<InvalidOperationException>();
        }
    }

    /// <summary>Verifies disposal cancels a raw publish operation that has not completed.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RawPublishingDisposalCancelsPendingOperationAsync(bool asynchronous)
    {
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new ScriptedMqttClient
        {
            PublishHandler = async (unusedMessage, cancellationToken) =>
            {
                _ = entered.TrySetResult(cancellationToken);
                await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
                return new MqttClientPublishResult(1, MqttClientPublishReasonCode.Success, null, null);
            },
        };
        var clients = Signal.Emit<IMqttClient>(client);
        var messages = Signal.Emit(CreateMessage());
        var operation = asynchronous
            ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
            : clients.PublishMessage(messages);
        var subscription = operation.Subscribe();
        var token = await entered.Task.WaitAsync(Timeout);
        var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = token.Register(
            static state =>
            {
                if (state is TaskCompletionSource<bool> completion)
                {
                    _ = completion.TrySetResult(true);
                }
            },
            canceled);
        subscription.Dispose();
        await Assert.That(await canceled.Task.WaitAsync(Timeout)).IsTrue();
        await Assert.That(token.IsCancellationRequested).IsTrue();
    }

    /// <summary>Verifies disposal releases the processed-event subscription for resilient publishing.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ResilientPublishingDisposalReleasesEventsAsync(bool asynchronous)
    {
        using var client = Substitute.For<IResilientMqttClient>();
        var eventSubscription = Substitute.For<IDisposable>();
        _ = client.ApplicationMessageProcessed.Returns(Signal.Create<ApplicationMessageProcessedEventArgs>(_ => eventSubscription));
        _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(Task.CompletedTask);
        var clients = Signal.Emit(client);
        var messages = Signal.Emit(CreateMessage());
        var operation = asynchronous
            ? clients.ToSignal().PublishMessage(messages.ToSignal()).ToObservable()
            : clients.PublishMessage(messages);
        var subscription = operation.Subscribe();
        subscription.Dispose();
        await Assert.That(CountCalls(eventSubscription, nameof(IDisposable.Dispose)))
            .IsEqualTo(1);
    }

    /// <summary>Counts calls to a substituted method.</summary>
    /// <param name="substitute">The substitute.</param>
    /// <param name="methodName">The method name.</param>
    /// <returns>The number of calls.</returns>
    private static int CountCalls(object substitute, string methodName)
    {
        var count = 0;
        foreach (var call in substitute.ReceivedCalls())
        {
            if (call.GetMethodInfo().Name == methodName)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Creates a message carrying MQTT 5 metadata and non-default delivery settings.</summary>
    /// <returns>The complete message.</returns>
    private static MqttApplicationMessage CreateMessage() => new MqttApplicationMessageBuilder()
        .WithTopic("features/complete")
        .WithPayload(Payload)
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
        .WithRetainFlag()
        .WithContentType("application/octet-stream")
        .WithResponseTopic("features/response")
        .WithCorrelationData(Correlation)
        .WithMessageExpiryInterval(ExpirySeconds)
        .WithTopicAlias(1)
        .WithUserProperty("source", "plc"u8.ToArray().AsMemory())
        .Build();
}
