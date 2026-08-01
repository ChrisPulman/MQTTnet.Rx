// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using RxLinq = System.Reactive.Linq;
#else
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using RxLinq = MQTTnet.Rx.Client.Linq;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
namespace MQTTnet.Rx.Client.MemoryEfficient;
#endif

/// <summary>Provides low-allocation reactive extensions for MQTT message processing.</summary>
/// <remarks>These extension methods minimize allocations in high-throughput scenarios by using pooled buffers, spans,
/// and efficient data transformations.</remarks>
public static class LowAllocExtensions
{
    /// <summary>The default maximum payload size to allocate on the stack.</summary>
    private const int DefaultMaximumStackSize = 256;

    /// <summary>The default maximum number of messages to queue for back-pressure handling.</summary>
    private const int DefaultMaximumQueueSize = 1000;

    /// <summary>Provides low-allocation operations for an MQTT application message source.</summary>
    /// <param name="source">The source of received MQTT application messages.</param>
    extension(IObservable<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Projects each MQTT message to its payload as a pooled buffer, minimizing allocations.</summary>
        /// <returns>An observable sequence of tuples containing the rented buffer, bytes written, and a return
        /// action.</returns>
        /// <remarks>The caller must invoke the return action when done with the buffer.</remarks>
        public IObservable<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload() =>
            source.Select(static e =>
            {
                var payload = e.ApplicationMessage.Payload;
                var buffer = BufferPool.CopyToRented(payload, out var length);
                return (buffer, length, new Action(() => BufferPool.Return(buffer)));
            });

        /// <summary>Processes each MQTT message payload and returns the payload length.</summary>
        /// <returns>An observable sequence of payload lengths.</returns>
        public IObservable<int> GetPayloadLength() =>
            source.Select(static e => (int)e.ApplicationMessage.Payload.Length);

        /// <summary>Processes each MQTT message payload and returns the payload as a byte array.</summary>
        /// <returns>An observable sequence of byte arrays.</returns>
        public IObservable<byte[]> ToPayloadArray() =>
            source.Select(static e => BufferPool.ToArray(e.ApplicationMessage.Payload));

        /// <summary>Decodes each MQTT message payload as UTF-8 using stack allocation for small payloads.</summary>
        /// <returns>An observable sequence of decoded strings.</returns>
        public IObservable<string> ToUtf8StringLowAlloc() =>
            source.ToUtf8StringLowAlloc(DefaultMaximumStackSize);

        /// <summary>Decodes each MQTT message payload as UTF-8 using stack allocation for small payloads.</summary>
        /// <param name="maxStackSize">The largest payload, in bytes, to allocate on the stack; larger payloads use a
        /// pooled buffer.</param>
        /// <returns>An observable sequence of decoded strings.</returns>
        public IObservable<string> ToUtf8StringLowAlloc(int maxStackSize) =>
            source.Select(new Utf8PayloadDecoder(maxStackSize).Decode);

        /// <summary>Batches MQTT messages by time window and processes them together for efficiency.</summary>
        /// <typeparam name="TResult">The type of the result produced by the batch processor.</typeparam>
        /// <param name="timeSpan">The time window for batching messages.</param>
        /// <param name="batchProcessor">A function that processes a batch of messages.</param>
        /// <returns>An observable sequence of batch processing results.</returns>
        public IObservable<TResult> BatchProcess<TResult>(
            TimeSpan timeSpan,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor) =>
            source.BatchProcess(timeSpan, batchProcessor, scheduler: null);

        /// <summary>Batches MQTT messages by time window and processes them together for efficiency.</summary>
        /// <typeparam name="TResult">The type of the result produced by the batch processor.</typeparam>
        /// <param name="timeSpan">The time window for batching messages.</param>
        /// <param name="batchProcessor">A function that processes a batch of messages.</param>
        /// <param name="scheduler">Optional scheduler for timing. Uses the default scheduler if null.</param>
        /// <returns>An observable sequence of batch processing results.</returns>
        public IObservable<TResult> BatchProcess<TResult>(
            TimeSpan timeSpan,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor,
            IScheduler? scheduler) =>
            (scheduler is null ? source.Buffer(timeSpan) : source.Buffer(timeSpan, scheduler))
                .Where(static batch => batch.Count > 0)
                .Select(batchProcessor);

        /// <summary>Batches MQTT messages by count and processes them together for efficiency.</summary>
        /// <typeparam name="TResult">The type of the result produced by the batch processor.</typeparam>
        /// <param name="count">The number of messages per batch.</param>
        /// <param name="batchProcessor">A function that processes a batch of messages.</param>
        /// <returns>An observable sequence of batch processing results.</returns>
        public IObservable<TResult> BatchProcess<TResult>(
            int count,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor) =>
            source.Buffer(count).Where(static batch => batch.Count > 0).Select(batchProcessor);

        /// <summary>Throttles MQTT messages, dropping intermediate messages within the specified duration.</summary>
        /// <param name="dueTime">The duration to throttle messages.</param>
        /// <returns>An observable sequence with throttled messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(
            TimeSpan dueTime) => source.Throttle(dueTime);

        /// <summary>Throttles MQTT messages, dropping intermediate messages within the specified duration.</summary>
        /// <param name="dueTime">The duration to throttle messages.</param>
        /// <param name="scheduler">Optional scheduler for timing.</param>
        /// <returns>An observable sequence with throttled messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(
            TimeSpan dueTime,
            IScheduler? scheduler) =>
            scheduler is null ? source.Throttle(dueTime) : source.Throttle(dueTime, scheduler);

        /// <summary>Samples MQTT messages at the specified interval, taking only the most recent message.</summary>
        /// <param name="interval">The sampling interval.</param>
        /// <returns>An observable sequence with sampled messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SampleMessages(
            TimeSpan interval) => source.Sample(interval);

        /// <summary>Samples MQTT messages at the specified interval, taking only the most recent message.</summary>
        /// <param name="interval">The sampling interval.</param>
        /// <param name="scheduler">Optional scheduler for timing.</param>
        /// <returns>An observable sequence with sampled messages.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SampleMessages(
            TimeSpan interval,
            IScheduler? scheduler) => scheduler is null ? source.Sample(interval) : source.Sample(interval, scheduler);

        /// <summary>Groups MQTT messages by topic for parallel processing.</summary>
        /// <returns>An observable sequence of grouped messages by topic.</returns>
        public IObservable<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopic() => source.GroupBy(static e => e.ApplicationMessage.Topic);

        /// <summary>Filters MQTT messages using efficient span-based topic matching.</summary>
        /// <param name="topicPrefix">The topic prefix to match.</param>
        /// <returns>An observable sequence containing only messages matching the topic prefix.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith(
            string topicPrefix) =>
            source.Where(e => e.ApplicationMessage.Topic.AsSpan().StartsWith(topicPrefix.AsSpan()));

        /// <summary>Filters MQTT messages using efficient span-based topic matching.</summary>
        /// <param name="topicSuffix">The topic suffix to match.</param>
        /// <returns>An observable sequence containing only messages matching the topic suffix.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith(
            string topicSuffix) =>
            source.Where(e => e.ApplicationMessage.Topic.AsSpan().EndsWith(topicSuffix.AsSpan()));

        /// <summary>Observes messages on a thread pool thread to avoid blocking the MQTT client.</summary>
        /// <returns>An observable sequence observed on a thread pool thread.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool() =>
            source.ObserveOnTaskPool();

        /// <summary>Adds back-pressure handling by dropping messages when the subscriber is slow.</summary>
        /// <returns>An observable sequence with back-pressure handling.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop() =>
            source.WithBackPressureDrop(onDrop: null);

        /// <summary>Adds back-pressure handling by dropping messages when the subscriber is slow.</summary>
        /// <param name="onDrop">Optional callback when a message is dropped.</param>
        /// <returns>An observable sequence with back-pressure handling.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(
            Action<MqttApplicationMessageReceivedEventArgs>? onDrop) =>
            Signal.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
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

        /// <summary>Adds back-pressure handling by queueing messages up to the default limit.</summary>
        /// <returns>An observable sequence with bounded queueing.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue() =>
            source.WithBackPressureQueue(DefaultMaximumQueueSize, onOverflow: null);

        /// <summary>Adds back-pressure handling by queueing messages up to a limit.</summary>
        /// <param name="maxQueueSize">Maximum number of messages to queue.</param>
        /// <returns>An observable sequence with bounded queueing.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
            int maxQueueSize) => source.WithBackPressureQueue(maxQueueSize, onOverflow: null);

        /// <summary>Adds back-pressure handling by queueing messages up to a limit.</summary>
        /// <param name="maxQueueSize">Maximum number of messages to queue.</param>
        /// <param name="onOverflow">Optional callback when the queue overflows.</param>
        /// <returns>An observable sequence with bounded queueing.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
            int maxQueueSize,
            Action<MqttApplicationMessageReceivedEventArgs>? onOverflow) =>
            Signal.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
            {
                var queue = new Queue<MqttApplicationMessageReceivedEventArgs>();
                var gate = new object();
                var isProcessing = false;
                var disposable = new MultipleDisposable();

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

                disposable.Add(
                    source.Subscribe(
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

    /// <summary>Decodes MQTT message payloads using stack allocation when it is safe to do so.</summary>
    /// <param name="maxStackSize">The maximum requested size for a stack-allocated payload buffer.</param>
    private sealed class Utf8PayloadDecoder(int maxStackSize)
    {
        /// <summary>The largest payload size that is safe to allocate on the stack.</summary>
        private const int MaximumSafeStackAllocationSize = 1024;

        /// <summary>Decodes the payload of an MQTT application message.</summary>
        /// <param name="eventArgs">The MQTT application message event arguments.</param>
        /// <returns>The UTF-8 decoded payload.</returns>
        public string Decode(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var payload = eventArgs.ApplicationMessage.Payload;
            if (payload.IsEmpty)
            {
                return string.Empty;
            }

            if (payload.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(payload.FirstSpan);
            }

            var payloadLength = (int)payload.Length;
            if (payloadLength <= maxStackSize && payloadLength <= MaximumSafeStackAllocationSize)
            {
                Span<byte> stackBuffer = stackalloc byte[payloadLength];
                payload.CopyTo(stackBuffer);
                return Encoding.UTF8.GetString(stackBuffer);
            }

            var buffer = BufferPool.CopyToRented(payload, out var bytesWritten);
            try
            {
                return Encoding.UTF8.GetString(buffer, 0, bytesWritten);
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        }
    }
}
