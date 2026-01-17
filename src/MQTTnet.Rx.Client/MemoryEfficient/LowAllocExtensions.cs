// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>
/// Provides low-allocation reactive extensions for MQTT message processing.
/// </summary>
/// <remarks>
/// These extension methods are designed to minimize memory allocations in high-throughput
/// scenarios by using pooled buffers, spans, and efficient data transformations.
/// </remarks>
public static class LowAllocExtensions
{
    /// <summary>
    /// Projects each MQTT message to its payload as a pooled buffer, minimizing allocations.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>
    /// An observable sequence of tuples containing the rented buffer, bytes written, and a return action.
    /// The caller must invoke the return action when done with the buffer.
    /// </returns>
    /// <remarks>
    /// Important: The return action must be called to return the buffer to the pool.
    /// </remarks>
    public static IObservable<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.Select(e =>
        {
            var payload = e.ApplicationMessage.Payload;
            var buffer = BufferPool.CopyToRented(payload, out var length);
            return (buffer, length, new Action(() => BufferPool.Return(buffer)));
        });

    /// <summary>
    /// Processes each MQTT message payload and returns the payload length.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>An observable sequence of payload lengths.</returns>
    public static IObservable<int> GetPayloadLength(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.Select(e => (int)e.ApplicationMessage.Payload.Length);

    /// <summary>
    /// Processes each MQTT message payload and returns the payload as a byte array.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>An observable sequence of byte arrays.</returns>
    public static IObservable<byte[]> ToPayloadArray(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.Select(e => BufferPool.ToArray(e.ApplicationMessage.Payload));

    /// <summary>
    /// Decodes each MQTT message payload as UTF-8 using stack allocation for small payloads.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="maxStackSize">
    /// Maximum payload size (in bytes) to allocate on the stack. Larger payloads use pooled buffers.
    /// Default is 256 bytes.
    /// </param>
    /// <returns>An observable sequence of decoded strings.</returns>
    public static IObservable<string> ToUtf8StringLowAlloc(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int maxStackSize = 256) =>
        source.Select(e =>
        {
            var payload = e.ApplicationMessage.Payload;

            if (payload.IsEmpty)
            {
                return string.Empty;
            }

            if (payload.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(payload.FirstSpan);
            }

            var length = (int)payload.Length;

            // Large payload: use pooled buffer
            var buffer = BufferPool.CopyToRented(payload, out var bytesWritten);
            try
            {
                return Encoding.UTF8.GetString(buffer, 0, bytesWritten);
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        });

    /// <summary>
    /// Batches MQTT messages by time window and processes them together for efficiency.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the batch processor.</typeparam>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="timeSpan">The time window for batching messages.</param>
    /// <param name="batchProcessor">A function that processes a batch of messages.</param>
    /// <param name="scheduler">Optional scheduler for timing. Uses default scheduler if null.</param>
    /// <returns>An observable sequence of batch processing results.</returns>
    public static IObservable<TResult> BatchProcess<TResult>(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        TimeSpan timeSpan,
        Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor,
        IScheduler? scheduler = null) =>
        (scheduler is null
            ? source.Buffer(timeSpan)
            : source.Buffer(timeSpan, scheduler))
        .Where(batch => batch.Count > 0)
        .Select(batchProcessor);

    /// <summary>
    /// Batches MQTT messages by count and processes them together for efficiency.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the batch processor.</typeparam>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="count">The number of messages per batch.</param>
    /// <param name="batchProcessor">A function that processes a batch of messages.</param>
    /// <returns>An observable sequence of batch processing results.</returns>
    public static IObservable<TResult> BatchProcess<TResult>(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int count,
        Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor) =>
        source.Buffer(count)
            .Where(batch => batch.Count > 0)
            .Select(batchProcessor);

    /// <summary>
    /// Throttles MQTT messages, dropping intermediate messages within the specified duration.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="dueTime">The duration to throttle messages.</param>
    /// <param name="scheduler">Optional scheduler for timing.</param>
    /// <returns>An observable sequence with throttled messages.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        TimeSpan dueTime,
        IScheduler? scheduler = null) =>
        scheduler is null
            ? source.Throttle(dueTime)
            : source.Throttle(dueTime, scheduler);

    /// <summary>
    /// Samples MQTT messages at the specified interval, taking only the most recent message.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="interval">The sampling interval.</param>
    /// <param name="scheduler">Optional scheduler for timing.</param>
    /// <returns>An observable sequence with sampled messages.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> SampleMessages(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        TimeSpan interval,
        IScheduler? scheduler = null) =>
        scheduler is null
            ? source.Sample(interval)
            : source.Sample(interval, scheduler);

    /// <summary>
    /// Groups MQTT messages by topic for parallel processing.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>An observable sequence of grouped messages by topic.</returns>
    public static IObservable<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopic(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.GroupBy(e => e.ApplicationMessage.Topic);

    /// <summary>
    /// Filters MQTT messages using efficient span-based topic matching.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="topicPrefix">The topic prefix to match.</param>
    /// <returns>An observable sequence containing only messages matching the topic prefix.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        string topicPrefix) =>
        source.Where(e => e.ApplicationMessage.Topic.AsSpan().StartsWith(topicPrefix.AsSpan()));

    /// <summary>
    /// Filters MQTT messages using efficient span-based topic matching.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="topicSuffix">The topic suffix to match.</param>
    /// <returns>An observable sequence containing only messages matching the topic suffix.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        string topicSuffix) =>
        source.Where(e => e.ApplicationMessage.Topic.AsSpan().EndsWith(topicSuffix.AsSpan()));

    /// <summary>
    /// Observes messages on a thread pool thread to avoid blocking the MQTT client.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <returns>An observable sequence observed on a thread pool thread.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.ObserveOn(ThreadPoolScheduler.Instance);

    /// <summary>
    /// Adds back-pressure handling by dropping messages when the subscriber is slow.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="onDrop">Optional callback when a message is dropped.</param>
    /// <returns>An observable sequence with back-pressure handling.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        Action<MqttApplicationMessageReceivedEventArgs>? onDrop = null) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var gate = new object();
            var isProcessing = false;

            return source.Subscribe(
                message =>
                {
                    lock (gate)
                    {
                        if (isProcessing)
                        {
                            onDrop?.Invoke(message);
                            return;
                        }

                        isProcessing = true;
                    }

                    try
                    {
                        observer.OnNext(message);
                    }
                    finally
                    {
                        lock (gate)
                        {
                            isProcessing = false;
                        }
                    }
                },
                observer.OnError,
                observer.OnCompleted);
        });

    /// <summary>
    /// Adds back-pressure handling by queueing messages up to a limit.
    /// </summary>
    /// <param name="source">The source sequence of MQTT application messages.</param>
    /// <param name="maxQueueSize">Maximum number of messages to queue.</param>
    /// <param name="onOverflow">Optional callback when queue overflows.</param>
    /// <returns>An observable sequence with bounded queueing.</returns>
    public static IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
        this IObservable<MqttApplicationMessageReceivedEventArgs> source,
        int maxQueueSize = 1000,
        Action<MqttApplicationMessageReceivedEventArgs>? onOverflow = null) =>
        Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            var queue = new Queue<MqttApplicationMessageReceivedEventArgs>();
            var gate = new object();
            var isProcessing = false;
            var disposable = new CompositeDisposable();

            void ProcessQueue()
            {
                while (true)
                {
                    MqttApplicationMessageReceivedEventArgs? message;
                    lock (gate)
                    {
                        if (queue.Count == 0)
                        {
                            isProcessing = false;
                            return;
                        }

                        message = queue.Dequeue();
                    }

                    observer.OnNext(message);
                }
            }

            disposable.Add(source.Subscribe(
                message =>
                {
                    lock (gate)
                    {
                        if (queue.Count >= maxQueueSize)
                        {
                            onOverflow?.Invoke(message);
                            return;
                        }

                        queue.Enqueue(message);

                        if (isProcessing)
                        {
                            return;
                        }

                        isProcessing = true;
                    }

                    ProcessQueue();
                },
                observer.OnError,
                observer.OnCompleted));

            return disposable;
        });
}
