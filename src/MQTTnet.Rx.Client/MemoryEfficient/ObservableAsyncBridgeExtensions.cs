// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Async.Reactive;
using RxLinq = System.Reactive.Linq;
#else
using RxLinq = MQTTnet.Rx.Client.Linq;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
namespace MQTTnet.Rx.Client.MemoryEfficient;
#endif

    /// <summary>Provides asynchronous observable counterparts for the low-allocation MQTT helpers.</summary>
public static class ObservableAsyncBridgeExtensions
{
    /// <summary>Gets the default stack buffer size used for UTF-8 payload decoding.</summary>
    private const int DefaultMaxStackSize = 256;

    /// <summary>Gets the default maximum number of messages retained by queue-based backpressure.</summary>
    private const int DefaultMaxQueueSize = 1000;

    /// <summary>Provides low-allocation MQTT helpers for asynchronous observable message sequences.</summary>
    /// <param name="source">The asynchronous source sequence of received MQTT application messages.</param>
    extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Projects each received MQTT application message into a rented payload buffer.</summary>
        /// <remarks>The caller is responsible for invoking the provided return action after processing each
        /// buffer to ensure proper resource management. Failing to return the buffer may exhaust resources.</remarks>
        /// <returns>An observable sequence of a rented buffer, its payload length, and its return action.</returns>
        public IObservableAsync<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static e =>
            {
                var payload = e.ApplicationMessage.Payload;
                var buffer = BufferPool.CopyToRented(payload, out var length);
                return (buffer, length, new Action(() => BufferPool.Return(buffer)));
            });
        }

        /// <summary>Projects each received MQTT application message into the length of its payload in bytes.</summary>
        /// <returns>An observable sequence of payload lengths in bytes.</returns>
        public IObservableAsync<int> GetPayloadLength()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static e => (int)e.ApplicationMessage.Payload.Length);
        }

        /// <summary>Projects each received MQTT application message into its payload as a byte array.</summary>
        /// <returns>An observable sequence of byte arrays representing received message payloads.</returns>
        public IObservableAsync<byte[]> ToPayloadArray()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static e => BufferPool.ToArray(e.ApplicationMessage.Payload));
        }

        /// <summary>Projects each message payload as a UTF-8 string using the default stack buffer size.</summary>
        /// <returns>An observable sequence of UTF-8 decoded message payloads.</returns>
        public IObservableAsync<string> ToUtf8StringLowAlloc() =>
            source.ToUtf8StringLowAlloc(DefaultMaxStackSize);

        /// <summary>Decodes each message payload as UTF-8 with a configured stack-buffer threshold.</summary>
        /// <param name="maxStackSize">The maximum payload size to decode on the stack.</param>
        /// <returns>An observable sequence of UTF-8 decoded message payloads.</returns>
        public IObservableAsync<string> ToUtf8StringLowAlloc(int maxStackSize)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Select(static e =>
            {
                var payload = e.ApplicationMessage.Payload;

                if (payload.IsEmpty)
                {
                    return string.Empty;
                }

                if (payload.IsSingleSegment)
                {
                    return System.Text.Encoding.UTF8.GetString(payload.FirstSpan);
                }

                var buffer = BufferPool.CopyToRented(payload, out var bytesWritten);
                try
                {
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesWritten);
                }
                finally
                {
                    BufferPool.Return(buffer);
                }
            });
        }

        /// <summary>Processes MQTT messages in time-based batches using the default scheduler.</summary>
        /// <typeparam name="TResult">The type of the result produced by the batch processor for each batch of
        /// messages.</typeparam>
        /// <param name="timeSpan">The time interval over which to collect messages into a batch before processing. Must
        /// be greater than
        /// TimeSpan.Zero.</param>
        /// <param name="batchProcessor">A function that processes each batch of received messages and returns a result
        /// for the batch. Cannot be null.</param>
        /// <returns>An observable sequence that emits the result of processing each batch of received
        /// messages.</returns>
        public IObservableAsync<TResult> BatchProcess<TResult>(
            TimeSpan timeSpan,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor) =>
            source.BatchProcess(timeSpan, batchProcessor, null);

        /// <summary>Processes MQTT messages in time-based batches using the specified scheduler.</summary>
        /// <typeparam name="TResult">The type produced by the batch processor.</typeparam>
        /// <param name="timeSpan">The interval over which to collect messages.</param>
        /// <param name="batchProcessor">The function that processes each batch.</param>
        /// <param name="scheduler">The scheduler used for batching operations.</param>
        /// <returns>An observable sequence of processed batch results.</returns>
        public IObservableAsync<TResult> BatchProcess<TResult>(
            TimeSpan timeSpan,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor,
            IScheduler? scheduler)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .BatchProcess(source.ToObservable(), timeSpan, batchProcessor, scheduler)
                .ToSignal();
        }

        /// <summary>Processes MQTT messages in fixed-size batches.</summary>
        /// <remarks>If the number of messages in the source sequence is not a multiple of the batch size, the
        /// final batch may contain fewer messages. The method does not process empty batches.</remarks>
        /// <typeparam name="TResult">The type of the result produced by the batch processing function.</typeparam>
        /// <param name="count">The number of messages to include in each batch. Must be greater than zero.</param>
        /// <param name="batchProcessor">A function that processes each batch and returns a result.</param>
        /// <returns>An observable sequence containing results from the batch processor.</returns>
        public IObservableAsync<TResult> BatchProcess<TResult>(
            int count,
            Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .BatchProcess(source.ToObservable(), count, batchProcessor)
                .ToSignal();
        }

        /// <summary>Emits the most recent message after the specified throttle interval.</summary>
        /// <param name="dueTime">The time interval to wait before emitting the most recent message. Messages received
        /// within this interval are suppressed except for the last one.</param>
        /// <returns>An observable sequence that emits only the most recent message in each interval.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(
            TimeSpan dueTime) => source.ThrottleMessages(dueTime, null);

        /// <summary>Emits the most recent message after the specified throttle interval using a scheduler.</summary>
        /// <param name="dueTime">The interval to wait before emitting the latest message.</param>
        /// <param name="scheduler">The scheduler used for throttle timers.</param>
        /// <returns>An observable sequence of throttled messages.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(
            TimeSpan dueTime,
            IScheduler? scheduler)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .ThrottleMessages(source.ToObservable(), dueTime, scheduler)
                .ToSignal();
        }

        /// <summary>Samples MQTT messages at the specified interval using the default scheduler.</summary>
        /// <param name="interval">The time interval at which to sample the source sequence.</param>
        /// <returns>An observable sequence that emits the latest message at each sampling interval.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SampleMessages(
            TimeSpan interval) => source.SampleMessages(interval, null);

        /// <summary>Samples MQTT messages at the specified interval using the specified scheduler.</summary>
        /// <param name="interval">The interval at which to sample the source sequence.</param>
        /// <param name="scheduler">The scheduler used for sampling timers.</param>
        /// <returns>An observable sequence of sampled messages.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SampleMessages(
            TimeSpan interval,
            IScheduler? scheduler)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .SampleMessages(source.ToObservable(), interval, scheduler)
                .ToSignal();
        }

        /// <summary>Groups received messages by their MQTT topic.</summary>
        /// <remarks>Each group represents messages sharing the same topic. Subscribers can process messages for
        /// specific topics independently by subscribing to the corresponding group.</remarks>
        /// <returns>An observable sequence of grouped message sequences, one per topic.</returns>
        public IObservableAsync<RxLinq.IGroupedObservable<
            string,
            MqttApplicationMessageReceivedEventArgs
        >> GroupByTopic()
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions.GroupByTopic(source.ToObservable()).ToSignal();
        }

        /// <summary>Filters received messages whose topic starts with the specified prefix.</summary>
        /// <param name="topicPrefix">The topic prefix to match. Only messages with topics that start with this prefix
        /// are included. Cannot be null.</param>
        /// <returns>An observable sequence that emits only those messages whose topic begins with the specified
        /// prefix.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith(
            string topicPrefix)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Where(e =>
                e.ApplicationMessage.Topic.AsSpan().StartsWith(topicPrefix.AsSpan()));
        }

        /// <summary>Filters received messages whose topic ends with the specified suffix.</summary>
        /// <param name="topicSuffix">The topic suffix to match. Only messages with topics ending with this suffix are
        /// included. Cannot be null.</param>
        /// <returns>An observable sequence that contains only the messages whose topic ends with the specified
        /// suffix.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith(
            string topicSuffix)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Where(e =>
                e.ApplicationMessage.Topic.AsSpan().EndsWith(topicSuffix.AsSpan()));
        }

        /// <summary>Configures the observable sequence to invoke observer callbacks on a thread pool thread.</summary>
        /// <remarks>Use this method to ensure that event handlers for received MQTT application messages are
        /// executed on a thread pool thread, which can help avoid blocking the calling thread or UI thread.</remarks>
        /// <returns>An observable sequence that notifies observers on a thread pool thread.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool()
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.ObserveOnSafe(TaskScheduler.Default);
        }

        /// <summary>Applies drop-based backpressure without a dropped-message action.</summary>
        /// <returns>An observable sequence that drops messages when the consumer cannot keep up.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop() =>
            source.WithBackPressureDrop(null);

        /// <summary>Applies drop-based backpressure with an optional action for each dropped message.</summary>
        /// <param name="onDrop">The action invoked for each dropped message.</param>
        /// <returns>An observable sequence that drops messages when the consumer cannot keep up.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(
            Action<MqttApplicationMessageReceivedEventArgs>? onDrop)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .WithBackPressureDrop(source.ToObservable(), onDrop)
                .ToSignal();
        }

        /// <summary>Applies queue-based backpressure with the default queue size and no overflow action.</summary>
        /// <returns>An observable sequence that applies queue-based backpressure.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue() =>
            source.WithBackPressureQueue(DefaultMaxQueueSize, null);

        /// <summary>Applies queue-based backpressure with the specified maximum queue size.</summary>
        /// <param name="maxQueueSize">The maximum number of messages to buffer.</param>
        /// <returns>An observable sequence that applies queue-based backpressure.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
            int maxQueueSize) => source.WithBackPressureQueue(maxQueueSize, null);

        /// <summary>Applies queue-based backpressure with the default queue size and no overflow action.</summary>
        /// <param name="onOverflow">The action invoked for each message dropped from a full queue.</param>
        /// <returns>An observable sequence that applies queue-based backpressure.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
            Action<MqttApplicationMessageReceivedEventArgs>? onOverflow) =>
            source.WithBackPressureQueue(DefaultMaxQueueSize, onOverflow);

        /// <summary>Applies queue-based backpressure with the specified queue size and overflow action.</summary>
        /// <param name="maxQueueSize">The maximum number of messages to buffer.</param>
        /// <param name="onOverflow">The action invoked for each message dropped from a full queue.</param>
        /// <returns>An observable sequence that applies queue-based backpressure.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(
            int maxQueueSize,
            Action<MqttApplicationMessageReceivedEventArgs>? onOverflow)
        {
            ArgumentNullException.ThrowIfNull(source);
            return LowAllocExtensions
                .WithBackPressureQueue(source.ToObservable(), maxQueueSize, onOverflow)
                .ToSignal();
        }
    }
}
