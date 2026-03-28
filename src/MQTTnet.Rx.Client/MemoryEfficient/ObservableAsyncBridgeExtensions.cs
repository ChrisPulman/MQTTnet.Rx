// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>
/// Provides asynchronous observable counterparts for the low-allocation MQTT helpers.
/// </summary>
public static class ObservableAsyncBridgeExtensions
{
    public static IObservableAsync<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(static e =>
        {
            var payload = e.ApplicationMessage.Payload;
            var buffer = BufferPool.CopyToRented(payload, out var length);
            return (buffer, length, new Action(() => BufferPool.Return(buffer)));
        });
    }

    public static IObservableAsync<int> GetPayloadLength(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(static e => (int)e.ApplicationMessage.Payload.Length);
    }

    public static IObservableAsync<byte[]> ToPayloadArray(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(static e => BufferPool.ToArray(e.ApplicationMessage.Payload));
    }

    public static IObservableAsync<string> ToUtf8StringLowAlloc(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int maxStackSize = 256)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(e =>
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

    public static IObservableAsync<TResult> BatchProcess<TResult>(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, TimeSpan timeSpan, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor, IScheduler? scheduler = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.BatchProcess(source.ToObservable(), timeSpan, batchProcessor, scheduler).ToObservableAsync();
    }

    public static IObservableAsync<TResult> BatchProcess<TResult>(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int count, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.BatchProcess(source.ToObservable(), count, batchProcessor).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> ThrottleMessages(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, TimeSpan dueTime, IScheduler? scheduler = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.ThrottleMessages(source.ToObservable(), dueTime, scheduler).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> SampleMessages(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, TimeSpan interval, IScheduler? scheduler = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.SampleMessages(source.ToObservable(), interval, scheduler).ToObservableAsync();
    }

    public static IObservableAsync<IGroupedObservable<string, MqttApplicationMessageReceivedEventArgs>> GroupByTopic(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.GroupByTopic(source.ToObservable()).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, string topicPrefix)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(e => e.ApplicationMessage.Topic.AsSpan().StartsWith(topicPrefix.AsSpan()));
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, string topicSuffix)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(e => e.ApplicationMessage.Topic.AsSpan().EndsWith(topicSuffix.AsSpan()));
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ObserveOn(ThreadPoolScheduler.Instance);
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, Action<MqttApplicationMessageReceivedEventArgs>? onDrop = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.WithBackPressureDrop(source.ToObservable(), onDrop).ToObservableAsync();
    }

    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(this IObservableAsync<MqttApplicationMessageReceivedEventArgs> source, int maxQueueSize = 1000, Action<MqttApplicationMessageReceivedEventArgs>? onOverflow = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return LowAllocExtensions.WithBackPressureQueue(source.ToObservable(), maxQueueSize, onOverflow).ToObservableAsync();
    }
}
