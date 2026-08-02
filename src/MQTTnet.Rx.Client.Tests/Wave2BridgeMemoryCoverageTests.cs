// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Packets;
using MQTTnet.Protocol;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
using MQTTnet.Rx.Client.MemoryEfficient;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using AsyncMemoryBridge = MQTTnet.Rx.Client.Reactive.MemoryEfficient.ObservableAsyncBridgeExtensions;
#else
using AsyncMemoryBridge = MQTTnet.Rx.Client.MemoryEfficient.ObservableAsyncBridgeExtensions;
#endif
#if REACTIVE_SHIM
using SynchronousMemoryBridge = MQTTnet.Rx.Client.Reactive.MemoryEfficient.LowAllocExtensions;
#else
using SynchronousMemoryBridge = MQTTnet.Rx.Client.MemoryEfficient.LowAllocExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises residual payload, low-allocation, and synchronous/asynchronous bridge paths.</summary>
public class Wave2BridgeMemoryCoverageTests
{
    /// <summary>The number of messages used by batching and grouping tests.</summary>
    private const int MessageCount = 2;

    /// <summary>The payload text shared by publishing and decoding tests.</summary>
    private const string PayloadText = "payload";

    /// <summary>The first conversion test value.</summary>
    private const int ConversionValue = 7;

    /// <summary>The number of messages expected from the configured publishing overloads.</summary>
    private const int ExpectedPublishedMessageCount = 4;

    /// <summary>The UTF-8 payload bytes shared by publishing and decoding tests.</summary>
    private static readonly byte[] PayloadBytes = "payload"u8.ToArray();

    /// <summary>The timeout used for deterministic asynchronous bridge transitions.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>The non-zero interval used by scheduler-based operators.</summary>
    private static readonly TimeSpan OperatorInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>Exercises synchronous convenience overloads and both scheduler-selection branches.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LowAllocationConvenienceOverloads_ForwardAndCompleteAsync()
    {
        var first = CreateMessage("devices/alpha", new("one"u8.ToArray()));
        var second = CreateMessage("devices/beta", new("two"u8.ToArray()));
        var source = Signal.FromEnumerable([first, second]);

        var defaultBatch = await SynchronousMemoryBridge
            .BatchProcess(source, OperatorInterval, static batch => batch.Count)
            .CollectAsync(Timeout);
        var scheduledBatch = await SynchronousMemoryBridge
            .BatchProcess(source, OperatorInterval, static batch => batch.Count, TestSchedulers.TaskPool)
            .CollectAsync(Timeout);
        var throttled = await SynchronousMemoryBridge
            .ThrottleMessages(source, OperatorInterval)
            .CollectAsync(Timeout);
        var throttledWithDefault = await SynchronousMemoryBridge
            .ThrottleMessages(source, OperatorInterval, null)
            .CollectAsync(Timeout);
        var throttledScheduled = await SynchronousMemoryBridge
            .ThrottleMessages(source, OperatorInterval, TestSchedulers.TaskPool)
            .CollectAsync(Timeout);
        var sampled = await SynchronousMemoryBridge
            .SampleMessages(source, OperatorInterval)
            .CollectAsync(Timeout);
        var sampledWithDefault = await SynchronousMemoryBridge
            .SampleMessages(source, OperatorInterval, null)
            .CollectAsync(Timeout);
        var sampledScheduled = await SynchronousMemoryBridge
            .SampleMessages(source, OperatorInterval, TestSchedulers.TaskPool)
            .CollectAsync(Timeout);
        var groups = await TestLinqExtensions.Select(
                SynchronousMemoryBridge.GroupByTopic(source),
                static group => group.Key)
            .CollectAsync(Timeout);
        var observed = await SynchronousMemoryBridge.ObserveOnThreadPool(source).CollectAsync(Timeout);
        var dropped = await SynchronousMemoryBridge.WithBackPressureDrop(source).CollectAsync(Timeout);
        var queuedDefault = await SynchronousMemoryBridge.WithBackPressureQueue(source).CollectAsync(Timeout);
        var queuedSized = await SynchronousMemoryBridge
            .WithBackPressureQueue(source, MessageCount)
            .CollectAsync(Timeout);

        await Assert.That(defaultBatch).IsNotEmpty();
        await Assert.That(scheduledBatch).IsNotEmpty();
        await Assert.That(throttled).IsNotEmpty();
        await Assert.That(throttledWithDefault).IsNotEmpty();
        await Assert.That(throttledScheduled).IsNotEmpty();
        await Assert.That(sampled).IsNotNull();
        await Assert.That(sampledWithDefault).IsNotNull();
        await Assert.That(sampledScheduled).IsNotNull();
        await Assert.That(groups).Count().IsEqualTo(MessageCount);
        await Assert.That(observed).Count().IsEqualTo(MessageCount);
        await Assert.That(dropped).Count().IsEqualTo(MessageCount);
        await Assert.That(queuedDefault).Count().IsEqualTo(MessageCount);
        await Assert.That(queuedSized).Count().IsEqualTo(MessageCount);
    }

    /// <summary>Exercises every residual asynchronous low-allocation forwarding overload.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncLowAllocationConvenienceOverloads_ForwardAndCompleteAsync()
    {
        var empty = CreateMessage("empty", ReadOnlySequence<byte>.Empty);
        var multi = CreateMessage("devices/multi", CreateSequence("mul"u8.ToArray(), "ti"u8.ToArray()));
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source =
            TestObservableBridge.ToSignal(Signal.FromEnumerable([empty, multi]));

        await VerifyAsyncPayloadTransformsAsync(source);
        await VerifyAsyncFlowOperatorsAsync(source);
    }

    /// <summary>Exercises topic grouping, extraction failures, JSON shapes, and scalar conversions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncPayloadBridge_ExercisesResidualProjectionBranchesAsync()
    {
        const string complexJson =
            "{\"text\":\"value\",\"decimal\":1.5,\"flag\":false,\"none\":null,\"items\":[1,\"two\"]," +
            "\"child\":{\"key\":3}}";
        var first = Helpers.TestDataHelpers.CreateMessageReceivedArgs("root/alpha/value", complexJson);
        var second = Helpers.TestDataHelpers.CreateMessageReceivedArgs("root/beta/value", "[]");
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source =
            TestObservableBridge.ToSignal(Signal.FromEnumerable([first, second]));

        var topics = await TestLinqExtensions.Select(
                ClientAsyncBridge.GroupByTopic(source).ToObservable(),
                static group => group.Key)
            .CollectAsync(Timeout);
        var levels = await TestLinqExtensions.Select(
                source.GroupByTopicLevel(1).ToObservable(),
                static group => group.Key)
            .CollectAsync(Timeout);
        var lengthMismatch = await source.ExtractTopicValues("root/{name}").ToObservable().CollectAsync(Timeout);
        var literalMismatch = await source
            .ExtractTopicValues("other/{name}/value")
            .ToObservable().CollectAsync(Timeout);
        var repeatedFailure = await source
            .ExtractTopicValues("root/{name}{name}x/value")
            .ToObservable().CollectAsync(Timeout);
        var noTopicMatches = await source.WhereTopicMatchesAny("unmatched/#").ToObservable().CollectAsync(Timeout);
        var dictionaries = await source.ToDictionary().ToObservable().CollectAsync(Timeout);
        var observed = await SignalAsync
            .Return(new Dictionary<string, object> { ["answer"] = ConversionValue })
            .Observe("answer").FirstAsync(Timeout);
        await VerifyScalarConversionsAsync();

        await Assert.That(topics).Count().IsEqualTo(MessageCount);
        await Assert.That(levels).Count().IsEqualTo(MessageCount);
        await Assert.That(lengthMismatch).IsEmpty();
        await Assert.That(literalMismatch).IsEmpty();
        await Assert.That(repeatedFailure).IsEmpty();
        await Assert.That(noTopicMatches).IsEmpty();
        await Assert.That(dictionaries[0]).IsNotNull();
        await Assert.That(dictionaries[1]).IsNull();
        await Assert.That(observed).IsEqualTo(ConversionValue);
    }

    /// <summary>Exercises raw-client configured-publish convenience overloads.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncPublishBridge_ConfiguredConvenienceOverloadsPublishAsync()
    {
        using var mqttClient = new MockMqttClient();
        var client = SignalAsync.Return<IMqttClient>(mqttClient);
        var text = SignalAsync.Return((Topic: "bridge/text", Payload: PayloadText));
        var bytes = SignalAsync.Return((Topic: "bridge/bytes", Payload: PayloadBytes));

        _ = await client.PublishMessage(text, static builder => _ = builder.WithRetainFlag(false))
            .FirstAsync(Timeout);
        _ = await client.PublishMessage(
            text,
            static builder => _ = builder.WithRetainFlag(false),
            MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(Timeout);
        _ = await client.PublishMessage(bytes, static builder => _ = builder.WithRetainFlag(false))
            .FirstAsync(Timeout);
        _ = await client.PublishMessage(
            bytes,
            static builder => _ = builder.WithRetainFlag(false),
            MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(Timeout);

        await Assert.That(mqttClient.PublishedMessages).Count().IsEqualTo(ExpectedPublishedMessageCount);
    }

    /// <summary>Exercises bridge cancellation, queued delivery, errors, completion, and disposal.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_HandlesCancellationErrorsCompletionAndDisposalAsync()
    {
        await VerifyCanceledSubscriptionAsync();

        using var subject = new TestSignal<int>();
        var firstEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<TestResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        List<int> received = [];
        await using var subscription = await TestObservableBridge.ToSignal(subject).SubscribeAsync(
            async (value, cancellationToken) =>
            {
                received.Add(value);
                if (value == 1)
                {
                    _ = firstEntered.TrySetResult(true);
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                }
            },
            static (_, _) => ValueTask.CompletedTask,
            result =>
            {
                _ = completed.TrySetResult(result);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        subject.OnNext(1);
        await firstEntered.Task.WaitAsync(Timeout);
        subject.OnNext(MessageCount);
        subject.OnCompleted();
        _ = releaseFirst.TrySetResult(true);
        var completion = await completed.Task.WaitAsync(Timeout);

        var failure = new InvalidOperationException("bridge failure");
        var observedFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var failedSubscription = TestObservableExtensions.Subscribe(
            TestObservableBridge.ToSignal(Signal.Fail<int>(failure)).ToObservable(),
            static _ => { },
            exception => _ = observedFailure.TrySetResult(exception));
        var forwardedFailure = await observedFailure.Task.WaitAsync(Timeout);

        var completedSignal = SignalAsync.Return(ConversionValue).ToObservable();
        var completedValues = await completedSignal.CollectAsync(Timeout);

        var disposal = new RecordingAsyncDisposable();
        var disposableSignal = SignalAsync.Create<int>((_, _) => new ValueTask<IAsyncDisposable>(disposal));
        var disposableSubscription = TestObservableExtensions.Subscribe(disposableSignal.ToObservable());
        disposableSubscription.Dispose();
        await disposal.Disposed.Task.WaitAsync(Timeout);

        await Assert.That(completion.IsSuccess).IsTrue();
        await Assert.That(received).Count().IsEqualTo(MessageCount);
        await Assert.That(forwardedFailure).IsSameReferenceAs(failure);
        await Assert.That(completedValues[0]).IsEqualTo(ConversionValue);
        await Assert.That(disposal.Disposed.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Verifies the synchronous payload helper decodes a multi-segment sequence.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PayloadUtf8_MultiSegmentPayloadUsesSequenceCopyAsync()
    {
        var message = CreateMessage("payload/multi", CreateSequence("pay"u8.ToArray(), "load"u8.ToArray()));

        await Assert.That(message.PayloadUtf8()).IsEqualTo(PayloadText);
        await Assert.That(message.Payload().Length).IsEqualTo(PayloadBytes.Length);
    }

    /// <summary>Verifies the asynchronous payload projection and batching operators.</summary>
    /// <param name="source">The source MQTT messages.</param>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyAsyncPayloadTransformsAsync(
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        var lengths = await AsyncMemoryBridge.GetPayloadLength(source).ToObservable().CollectAsync(Timeout);
        var arrays = await AsyncMemoryBridge.ToPayloadArray(source).ToObservable().CollectAsync(Timeout);
        var texts = await AsyncMemoryBridge.ToUtf8StringLowAlloc(source).ToObservable().CollectAsync(Timeout);
        var batches = await AsyncMemoryBridge
            .BatchProcess(source, MessageCount, static batch => batch.Count)
            .ToObservable().CollectAsync(Timeout);

        await Assert.That(lengths[1]).IsEqualTo("multi"u8.Length);
        await Assert.That(arrays[0]).IsEmpty();
        await Assert.That(texts[0]).IsEmpty();
        await Assert.That(texts[1]).IsEqualTo("multi");
        await Assert.That(batches[0]).IsEqualTo(MessageCount);
    }

    /// <summary>Verifies the asynchronous timing, grouping, and back-pressure operators.</summary>
    /// <param name="source">The source MQTT messages.</param>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyAsyncFlowOperatorsAsync(
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        var timed = await AsyncMemoryBridge
            .BatchProcess(source, OperatorInterval, static batch => batch.Count)
            .ToObservable().CollectAsync(Timeout);
        var timedScheduled = await AsyncMemoryBridge
            .BatchProcess(source, OperatorInterval, static batch => batch.Count, TestSchedulers.TaskPool)
            .ToObservable().CollectAsync(Timeout);
        var throttled = await AsyncMemoryBridge
            .ThrottleMessages(source, OperatorInterval)
            .ToObservable().CollectAsync(Timeout);
        var throttledScheduled = await AsyncMemoryBridge
            .ThrottleMessages(source, OperatorInterval, TestSchedulers.TaskPool)
            .ToObservable().CollectAsync(Timeout);
        var sampled = await AsyncMemoryBridge
            .SampleMessages(source, OperatorInterval)
            .ToObservable().CollectAsync(Timeout);
        var sampledScheduled = await AsyncMemoryBridge
            .SampleMessages(source, OperatorInterval, TestSchedulers.TaskPool)
            .ToObservable().CollectAsync(Timeout);
        var groups = await TestLinqExtensions.Select(
                AsyncMemoryBridge.GroupByTopic(source).ToObservable(),
                static group => group.Key)
            .CollectAsync(Timeout);
        var prefixes = await AsyncMemoryBridge
            .WhereTopicStartsWith(source, "devices/")
            .ToObservable().CollectAsync(Timeout);
        var observed = await AsyncMemoryBridge.ObserveOnThreadPool(source).ToObservable().CollectAsync(Timeout);
        var dropped = await AsyncMemoryBridge.WithBackPressureDrop(source).ToObservable().CollectAsync(Timeout);
        var queuedDefault = await AsyncMemoryBridge
            .WithBackPressureQueue(source)
            .ToObservable().CollectAsync(Timeout);
        var queuedSized = await AsyncMemoryBridge
            .WithBackPressureQueue(source, MessageCount)
            .ToObservable().CollectAsync(Timeout);
        var queuedCallback = await AsyncMemoryBridge
            .WithBackPressureQueue(source, static _ => { })
            .ToObservable().CollectAsync(Timeout);

        await Assert.That(timed).IsNotEmpty();
        await Assert.That(timedScheduled).IsNotEmpty();
        await Assert.That(throttled).IsNotEmpty();
        await Assert.That(throttledScheduled).IsNotEmpty();
        await Assert.That(sampled).IsNotNull();
        await Assert.That(sampledScheduled).IsNotNull();
        await Assert.That(groups).Count().IsEqualTo(MessageCount);
        await Assert.That(prefixes).Count().IsEqualTo(1);
        await Assert.That(observed).Count().IsEqualTo(MessageCount);
        await Assert.That(dropped).Count().IsEqualTo(MessageCount);
        await Assert.That(queuedDefault).Count().IsEqualTo(MessageCount);
        await Assert.That(queuedSized).Count().IsEqualTo(MessageCount);
        await Assert.That(queuedCallback).Count().IsEqualTo(MessageCount);
    }

    /// <summary>Verifies conversions of heterogeneous values through asynchronous observable operators.</summary>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyScalarConversionsAsync()
    {
        IObservableAsync<object?> values = TestObservableBridge.ToSignal(Signal
            .FromEnumerable<object?>([
                true,
                ConversionValue,
                ConversionValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ]));
        IObservableAsync<object?> booleanValues = TestObservableBridge.ToSignal(Signal
            .FromEnumerable<object?>([true, false, "true"]));
        var booleans = await booleanValues.ToBool().ToObservable().CollectAsync(Timeout);
        var bytes = await values.ToByte().ToObservable().CollectAsync(Timeout);
        var int16s = await values.ToInt16().ToObservable().CollectAsync(Timeout);
        var int64s = await values.ToInt64().ToObservable().CollectAsync(Timeout);
        var singles = await values.ToSingle().ToObservable().CollectAsync(Timeout);
        var doubles = await values.ToDouble().ToObservable().CollectAsync(Timeout);
        var strings = await ClientAsyncBridge
            .ToString(values)
            .ToObservable().CollectAsync(Timeout);

        await Assert.That(booleans).IsNotEmpty();
        await Assert.That(bytes).IsNotEmpty();
        await Assert.That(int16s).IsNotEmpty();
        await Assert.That(int64s).IsNotEmpty();
        await Assert.That(singles).IsNotEmpty();
        await Assert.That(doubles).IsNotEmpty();
        await Assert.That(strings).IsNotEmpty();
    }

    /// <summary>Verifies cancellation before subscription prevents source activation.</summary>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyCanceledSubscriptionAsync()
    {
        var sourceSubscribed = false;
        var canceledSource = Signal.Create<int>(_ =>
        {
            sourceSubscribed = true;
            return EmptyDisposable.Instance;
        });
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        await using var canceledSubscription = await TestObservableBridge
            .ToSignal(canceledSource)
            .SubscribeAsync(static (_, _) => ValueTask.CompletedTask, canceled.Token);

        await Assert.That(sourceSubscribed).IsFalse();
    }

    /// <summary>Creates message event arguments for an exact payload sequence.</summary>
    /// <param name="topic">The MQTT message topic.</param>
    /// <param name="payload">The exact payload sequence.</param>
    /// <returns>The constructed message event arguments.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateMessage(string topic, ReadOnlySequence<byte> payload)
    {
        MqttApplicationMessage message = new() { Topic = topic, Payload = payload };
        MqttPublishPacket packet = new() { Topic = topic, Payload = payload };
        return new("wave-two-bridge-memory", message, packet, null);
    }

    /// <summary>Creates a two-segment read-only sequence.</summary>
    /// <param name="first">The first segment.</param>
    /// <param name="second">The second segment.</param>
    /// <returns>The linked sequence.</returns>
    private static ReadOnlySequence<byte> CreateSequence(byte[] first, byte[] second)
    {
        var firstSegment = new ByteSequenceSegment(first);
        var secondSegment = firstSegment.Append(second);
        return new(firstSegment, 0, secondSegment, second.Length);
    }

    /// <summary>Represents one linked sequence segment.</summary>
    private sealed class ByteSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        /// <summary>Initializes a new instance of the <see cref="ByteSequenceSegment"/> class.</summary>
        /// <param name="memory">The segment memory.</param>
        public ByteSequenceSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        /// <summary>Appends and returns a linked segment.</summary>
        /// <param name="memory">The appended memory.</param>
        /// <returns>The appended segment.</returns>
        public ByteSequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var runningIndex = RunningIndex;
            var segment = new ByteSequenceSegment(memory) { RunningIndex = runningIndex };
            segment.RunningIndex += Memory.Length;
            Next = segment;
            return segment;
        }
    }

    /// <summary>Records asynchronous disposal completion.</summary>
    private sealed class RecordingAsyncDisposable : IAsyncDisposable
    {
        /// <summary>Gets the disposal completion signal.</summary>
        public TaskCompletionSource<bool> Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            _ = Disposed.TrySetResult(true);
            return ValueTask.CompletedTask;
        }
    }
}
