// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Packets;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
using MQTTnet.Rx.Client.MemoryEfficient;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides focused behavioral coverage for memory-efficient client helpers.</summary>
public sealed class MemoryEfficientCoverageTests
{
    /// <summary>The maximum payload size that uses the stack allocation path.</summary>
    private const int StackAllocationThreshold = 32;

    /// <summary>The bounded queue capacity used by the overflow test.</summary>
    private const int QueueCapacity = 1;

    /// <summary>The expected count produced by two-message test sequences.</summary>
    private const int ExpectedItemCount = 2;

    /// <summary>The matching message topic.</summary>
    private const string MatchingTopic = "sensors/temperature";

    /// <summary>The non-matching message topic.</summary>
    private const string OtherTopic = "devices/status";

    /// <summary>The multi-segment UTF-8 payload text.</summary>
    private const string MultiPayloadText = "hello";

    /// <summary>The first single-byte payload.</summary>
    private static readonly byte[] SinglePayload = [1];

    /// <summary>The first segment of the multi-segment payload.</summary>
    private static readonly byte[] MultiPayloadFirstSegment = "hel"u8.ToArray();

    /// <summary>The final segment of the multi-segment payload.</summary>
    private static readonly byte[] MultiPayloadFinalSegment = "lo"u8.ToArray();

    /// <summary>The payload used by re-entrant message tests.</summary>
    private static readonly byte[] FirstPayload = "first"u8.ToArray();

    /// <summary>Verifies buffer pooling handles empty, single, and multi-segment sequences.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task BufferPool_CopiesEverySequenceShapeAndReturnsScopedBuffersAsync()
    {
        var empty = ReadOnlySequence<byte>.Empty;
        var single = new ReadOnlySequence<byte>(SinglePayload);
        var multi = CreateSequence(MultiPayloadFirstSegment, MultiPayloadFinalSegment);
        var buffer = BufferPool.CopyToRented(multi, out var bytesWritten);
        var emptyBuffer = BufferPool.CopyToRented(empty, out var emptyBytesWritten);
        var zeroLength = BufferPool.Rent(0);

        try
        {
            await Assert.That(BufferPool.ToArray(empty)).IsEmpty();
            await Assert.That(BufferPool.ToArray(single)[0]).IsEqualTo(SinglePayload[0]);
            await Assert.That(System.Text.Encoding.UTF8.GetString(BufferPool.ToArray(multi)))
                .IsEqualTo(MultiPayloadText);
            await Assert.That(bytesWritten).IsEqualTo(MultiPayloadText.Length);
            await Assert.That(emptyBytesWritten).IsEqualTo(0);
            await Assert.That(zeroLength.Length).IsGreaterThanOrEqualTo(BufferPool.DefaultBufferSize);

            using var scope = BufferPool.RentScope();
            scope.Span[0] = SinglePayload[0];
            await Assert.That(scope.Memory.Span[0]).IsEqualTo(SinglePayload[0]);
        }
        finally
        {
            BufferPool.Return(buffer);
            BufferPool.Return(emptyBuffer);
            BufferPool.Return(zeroLength, true);
            BufferPool.Return(null);
        }
    }

    /// <summary>Verifies synchronous low-allocation projections use stack and pooled multi-segment decoding.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LowAllocExtensions_ProjectPayloadsAndFilterTopicsAsync()
    {
        var first = CreateMessage(MatchingTopic, new(SinglePayload));
        var second = CreateMessage(OtherTopic, CreateSequence(MultiPayloadFirstSegment, MultiPayloadFinalSegment));
        MqttApplicationMessageReceivedEventArgs[] messageItems = [first, second];
        var messages = messageItems.ToObservable();
        List<(byte[] Buffer, int Length, Action ReturnBuffer)> pooled = [];
        List<int> lengths = [];
        List<byte[]> arrays = [];
        List<string> stackText = [];
        List<string> pooledText = [];
        List<MqttApplicationMessageReceivedEventArgs> prefixed = [];
        List<MqttApplicationMessageReceivedEventArgs> suffixed = [];
        List<string> groups = [];
        List<int> batches = [];

        using var pooledSubscription = messages.ToPooledPayload().Subscribe(pooled.Add);
        using var lengthSubscription = messages.GetPayloadLength().Subscribe(lengths.Add);
        using var arraySubscription = messages.ToPayloadArray().Subscribe(arrays.Add);
        using var stackSubscription = messages.ToUtf8StringLowAlloc(StackAllocationThreshold).Subscribe(stackText.Add);
        using var poolSubscription = messages.ToUtf8StringLowAlloc(0).Subscribe(pooledText.Add);
        using var prefixSubscription = messages.WhereTopicStartsWith("sensors/").Subscribe(prefixed.Add);
        using var suffixSubscription = messages.WhereTopicEndsWith("status").Subscribe(suffixed.Add);
        using var groupingSubscription = LowAllocExtensions.GroupByTopic(messages)
            .Select(static group => group.Key)
            .Subscribe(groups.Add);
        using var batchingSubscription = messages.BatchProcess(QueueCapacity, static batch => batch.Count)
            .Subscribe(batches.Add);

        try
        {
            await Assert.That(pooled).Count().IsEqualTo(ExpectedItemCount);
            await Assert.That(lengths[1]).IsEqualTo(MultiPayloadText.Length);
            await Assert.That(System.Text.Encoding.UTF8.GetString(arrays[1])).IsEqualTo(MultiPayloadText);
            await Assert.That(stackText[1]).IsEqualTo(MultiPayloadText);
            await Assert.That(pooledText[1]).IsEqualTo(MultiPayloadText);
            await Assert.That(prefixed[0]).IsSameReferenceAs(first);
            await Assert.That(suffixed[0]).IsSameReferenceAs(second);
            await Assert.That(groups).Count().IsEqualTo(ExpectedItemCount);
            await Assert.That(batches).Count().IsEqualTo(ExpectedItemCount);
        }
        finally
        {
            ReturnPooledPayloads(pooled);
        }
    }

    /// <summary>Verifies re-entrant messages take the drop and bounded queue overflow paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LowAllocExtensions_BackPressureDropAndQueueHandleReentrantMessagesAsync()
    {
        using var dropSource = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        var first = CreateMessage(MatchingTopic, new(FirstPayload));
        var dropped = CreateMessage(OtherTopic, new("dropped"u8.ToArray()));
        List<MqttApplicationMessageReceivedEventArgs> delivered = [];
        List<MqttApplicationMessageReceivedEventArgs> dropNotifications = [];

        using var dropSubscription = dropSource.WithBackPressureDrop(dropNotifications.Add).Subscribe(message =>
        {
            delivered.Add(message);
            dropSource.OnNext(dropped);
        });
        dropSource.OnNext(first);

        using var queueSource = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        var queued = CreateMessage(MatchingTopic, new("queued"u8.ToArray()));
        var overflow = CreateMessage(OtherTopic, new("overflow"u8.ToArray()));
        List<MqttApplicationMessageReceivedEventArgs> queuedDelivered = [];
        List<MqttApplicationMessageReceivedEventArgs> overflowNotifications = [];
        using var queueSubscription = queueSource
            .WithBackPressureQueue(QueueCapacity, overflowNotifications.Add)
            .Subscribe(message =>
            {
                queuedDelivered.Add(message);
                if (!ReferenceEquals(message, first))
                {
                    return;
                }

                queueSource.OnNext(queued);
                queueSource.OnNext(overflow);
            });
        queueSource.OnNext(first);

        await Assert.That(delivered).Count().IsEqualTo(1);
        await Assert.That(dropNotifications[0]).IsSameReferenceAs(dropped);
        await Assert.That(queuedDelivered).Count().IsEqualTo(ExpectedItemCount);
        await Assert.That(overflowNotifications[0]).IsSameReferenceAs(overflow);
    }

    /// <summary>Verifies the asynchronous bridge projects multi-segment payloads and validates null sources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableAsyncBridgeExtensions_ProjectPayloadsAndValidateNullSourcesAsync()
    {
        var first = CreateMessage(MatchingTopic, new(FirstPayload));
        var second = CreateMessage(OtherTopic, CreateSequence(MultiPayloadFirstSegment, MultiPayloadFinalSegment));
        MqttApplicationMessageReceivedEventArgs[] messageItems = [first, second];
        var source = TestObservableBridge.ToSignal(messageItems.ToObservable());
        List<(byte[] Buffer, int Length, Action ReturnBuffer)> pooled = [];
        List<string> text = [];
        List<MqttApplicationMessageReceivedEventArgs> filtered = [];

        using var pooledSubscription = source.ToPooledPayload().ToObservable().Subscribe(pooled.Add);
        using var textSubscription = source.ToUtf8StringLowAlloc(StackAllocationThreshold)
            .ToObservable()
            .Subscribe(text.Add);
        using var filteringSubscription = source.WhereTopicEndsWith("status").ToObservable().Subscribe(filtered.Add);

        try
        {
            await Assert.That(pooled).Count().IsEqualTo(ExpectedItemCount);
            await Assert.That(text[1]).IsEqualTo(MultiPayloadText);
            await Assert.That(filtered[0]).IsSameReferenceAs(second);
        }
        finally
        {
            ReturnPooledPayloads(pooled);
        }

        IObservableAsync<MqttApplicationMessageReceivedEventArgs>? missing = null;
        await Assert.That(() => missing!.ToPayloadArray()).Throws<ArgumentNullException>();
        await Assert.That(() => missing!.WithBackPressureQueue()).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies span parser delegates receive their unallocated byte span argument.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SpanParser_ReceivesPayloadSpanAsync()
    {
        await Assert.That(ParseLength(MultiPayloadFirstSegment)).IsEqualTo(MultiPayloadFirstSegment.Length);

        static int ParseLength(ReadOnlySpan<byte> data) => data.Length;
    }

    /// <summary>Returns all payload buffers to their owning pool.</summary>
    /// <param name="payloads">The pooled payloads to return.</param>
    private static void ReturnPooledPayloads(List<(byte[] Buffer, int Length, Action ReturnBuffer)> payloads)
    {
        foreach (var payload in payloads)
        {
            payload.ReturnBuffer();
        }
    }

    /// <summary>Creates message event arguments for an exact payload sequence.</summary>
    /// <param name="topic">The MQTT message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <returns>The constructed event arguments.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateMessage(string topic, ReadOnlySequence<byte> payload)
    {
        MqttApplicationMessage message = new() { Topic = topic, Payload = payload };
        MqttPublishPacket packet = new() { Topic = topic, Payload = payload };
        return new("memory-efficient-test", message, packet, null);
    }

    /// <summary>Creates a two-segment read-only byte sequence.</summary>
    /// <param name="first">The first segment data.</param>
    /// <param name="second">The second segment data.</param>
    /// <returns>The constructed multi-segment sequence.</returns>
    private static ReadOnlySequence<byte> CreateSequence(byte[] first, byte[] second)
    {
        var firstSegment = new ByteSequenceSegment(first);
        var secondSegment = firstSegment.Append(second);
        return new(firstSegment, 0, secondSegment, second.Length);
    }

    /// <summary>Represents a linked read-only sequence segment for test payload creation.</summary>
    private sealed class ByteSequenceSegment : ReadOnlySequenceSegment<byte>
    {
        /// <summary>Initializes a new instance of the <see cref="ByteSequenceSegment"/> class.</summary>
        /// <param name="memory">The memory represented by the segment.</param>
        public ByteSequenceSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        /// <summary>Appends a segment and returns the appended segment.</summary>
        /// <param name="memory">The appended memory.</param>
        /// <returns>The appended segment.</returns>
        public ByteSequenceSegment Append(ReadOnlyMemory<byte> memory)
        {
            var runningIndex = RunningIndex;
            var segment = new ByteSequenceSegment(memory)
            {
                RunningIndex = runningIndex,
            };
            segment.RunningIndex += Memory.Length;
            Next = segment;
            return segment;
        }
    }
}
