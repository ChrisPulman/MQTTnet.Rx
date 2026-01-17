// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Reactive.Testing;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>
/// Provides extension methods for reactive testing.
/// </summary>
public static class ReactiveTestHelpers
{
    /// <summary>
    /// Subscribes to an observable and collects all values.
    /// </summary>
    /// <typeparam name="T">The type of values.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <param name="timeout">Optional timeout for collecting values.</param>
    /// <returns>A list of collected values.</returns>
    public static async Task<List<T>> CollectAsync<T>(
        this IObservable<T> observable,
        TimeSpan? timeout = null)
    {
        var values = new List<T>();
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource();

        if (timeout.HasValue)
        {
            cts.CancelAfter(timeout.Value);
        }

        using var registration = cts.Token.Register(() => tcs.TrySetResult(true));
        using var subscription = observable.Subscribe(
            onNext: v => values.Add(v),
            onError: ex => tcs.TrySetException(ex),
            onCompleted: () => tcs.TrySetResult(true));

        await tcs.Task.ConfigureAwait(false);
        return values;
    }

    /// <summary>
    /// Subscribes to an observable and returns the first value or throws on timeout.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The first value.</returns>
    public static async Task<T> FirstAsync<T>(
        this IObservable<T> observable,
        TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<T>();
        var cts = new CancellationTokenSource(timeout);

        using var registration = cts.Token.Register(() =>
            tcs.TrySetException(new TimeoutException("Observable did not produce a value within the timeout.")));

        using var subscription = observable.Subscribe(
            onNext: v => tcs.TrySetResult(v),
            onError: ex => tcs.TrySetException(ex),
            onCompleted: () => tcs.TrySetException(new InvalidOperationException("Observable completed without producing a value.")));

        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a TestScheduler and advances time by the specified amount.
    /// </summary>
    /// <param name="advanceBy">The time to advance.</param>
    /// <returns>The test scheduler.</returns>
    public static TestScheduler CreateScheduler(TimeSpan? advanceBy = null)
    {
        var scheduler = new TestScheduler();
        if (advanceBy.HasValue)
        {
            scheduler.AdvanceBy(advanceBy.Value.Ticks);
        }

        return scheduler;
    }
}
