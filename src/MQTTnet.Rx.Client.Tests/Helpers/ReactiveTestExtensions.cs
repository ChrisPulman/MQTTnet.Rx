// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Provides reactive test extensions.</summary>
public static class ReactiveTestExtensions
{
    /// <summary>Provides helpers for synchronous observables.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    extension<T>(IObservable<T> observable)
    {
        /// <summary>Subscribes to an observable and collects all values.</summary>
        /// <returns>A task that produces the collected values.</returns>
        public Task<List<T>> CollectAsync() => observable.CollectAsync((TimeSpan?)null);

        /// <summary>Subscribes to an observable and collects all values.</summary>
        /// <param name="timeout">The optional timeout for collecting values.</param>
        /// <returns>A task that produces the collected values.</returns>
        public async Task<List<T>> CollectAsync(TimeSpan? timeout)
        {
            ArgumentNullException.ThrowIfNull(observable);

            var values = new List<T>();
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();

            if (timeout.HasValue)
            {
                cancellation.CancelAfter(timeout.Value);
            }

            await using var registration = cancellation.Token.Register(
                static state =>
                {
                    if (state is TaskCompletionSource<bool> completionSource)
                    {
                        _ = completionSource.TrySetResult(true);
                    }
                },
                completion);
            using var subscription = observable.Subscribe(
                values.Add,
                exception => _ = completion.TrySetException(exception),
                () => _ = completion.TrySetResult(true));

            await completion.Task.ConfigureAwait(false);
            return values;
        }

        /// <summary>Subscribes to an observable and returns its first value.</summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>A task that produces the first value.</returns>
        public async Task<T> FirstAsync(TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(observable);

            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource(timeout);

            await using var registration = cancellation.Token.Register(
                static state =>
                {
                    if (state is TaskCompletionSource<T> completionSource)
                    {
                        _ = completionSource.TrySetException(
                            new TimeoutException("Observable did not produce a value within the timeout."));
                    }
                },
                completion);
            using var subscription = observable.Subscribe(
                value => _ = completion.TrySetResult(value),
                exception => _ = completion.TrySetException(exception),
                () => _ = completion.TrySetException(
                    new InvalidOperationException("Observable completed without producing a value.")));

            return await completion.Task.ConfigureAwait(false);
        }
    }

    /// <summary>Provides helpers for asynchronous observables.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The asynchronous observable to subscribe to.</param>
    extension<T>(IObservableAsync<T> observable)
    {
        /// <summary>Subscribes to an asynchronous observable and returns its first value.</summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <returns>A task that produces the first value.</returns>
        public async Task<T> FirstAsync(TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(observable);

            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource(timeout);

            await using var registration = cancellation.Token.Register(
                static state =>
                {
                    if (state is TaskCompletionSource<T> completionSource)
                    {
                        _ = completionSource.TrySetException(
                            new TimeoutException("Observable did not produce a value within the timeout."));
                    }
                },
                completion);

            await using var subscription = await observable.SubscribeAsync(
                (value, cancellationToken) =>
                {
                    _ = completion.TrySetResult(value);
                    return ValueTask.CompletedTask;
                },
                (exception, cancellationToken) =>
                {
                    _ = completion.TrySetException(exception);
                    return ValueTask.CompletedTask;
                },
                result =>
                {
                    _ = completion.TrySetException(
                        new InvalidOperationException("Observable completed without producing a value."));
                    return ValueTask.CompletedTask;
                },
                cancellation.Token).ConfigureAwait(false);

            return await completion.Task.ConfigureAwait(false);
        }
    }
}
