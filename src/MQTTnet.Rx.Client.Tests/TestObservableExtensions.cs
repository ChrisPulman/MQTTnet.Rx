// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides subscription overloads that work with both Primitives variants.</summary>
internal static class TestObservableExtensions
{
    /// <summary>Collects all values from a synchronously completing observable sequence.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The sequence to collect.</param>
    /// <returns>A task that completes with the collected values.</returns>
    internal static Task<IList<T>> ToListAsync<T>(IObservable<T> observable)
    {
        ArgumentNullException.ThrowIfNull(observable);

        IList<T> values = [];
        var completion = new TaskCompletionSource<IList<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Subscribe(
            observable,
            values.Add,
            exception => _ = completion.TrySetException(exception),
            () => _ = completion.TrySetResult(values));
        return completion.Task;
    }

    /// <summary>Extends synchronous observables with callback subscription overloads.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    extension<T>(IObservable<T> observable)
    {
        /// <summary>Subscribes without observing notifications.</summary>
        /// <returns>The subscription lifetime.</returns>
        internal IDisposable Subscribe() => Subscribe(observable, onNext: null, onError: null, onCompleted: null);

        /// <summary>Subscribes with a value callback.</summary>
        /// <param name="onNext">The value callback.</param>
        /// <returns>The subscription lifetime.</returns>
        internal IDisposable Subscribe(Action<T> onNext) => Subscribe(observable, onNext, onError: null, onCompleted: null);

        /// <summary>Subscribes with value and error callbacks.</summary>
        /// <param name="onNext">The optional value callback.</param>
        /// <param name="onError">The optional error callback.</param>
        /// <returns>The subscription lifetime.</returns>
        internal IDisposable Subscribe(Action<T>? onNext, Action<Exception>? onError) =>
            Subscribe(observable, onNext, onError, onCompleted: null);

        /// <summary>Subscribes with value, error, and completion callbacks.</summary>
        /// <param name="onNext">The optional value callback.</param>
        /// <param name="onError">The optional error callback.</param>
        /// <param name="onCompleted">The optional completion callback.</param>
        /// <returns>The subscription lifetime.</returns>
        internal IDisposable Subscribe(Action<T>? onNext, Action<Exception>? onError, Action? onCompleted)
        {
            ArgumentNullException.ThrowIfNull(observable);
            return observable.Subscribe(new DelegatingObserver<T>(onNext, onError, onCompleted));
        }
    }

    /// <summary>Invokes supplied callbacks for an observable notification sequence.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="onNext">The optional value callback.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="onCompleted">The optional completion callback.</param>
    private sealed class DelegatingObserver<T>(Action<T>? onNext, Action<Exception>? onError, Action? onCompleted) : IObserver<T>
    {
        /// <summary>Handles normal completion.</summary>
        public void OnCompleted() => onCompleted?.Invoke();

        /// <summary>Handles a terminal error.</summary>
        /// <param name="error">The terminal error.</param>
        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
            if (onError is null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
            }

            onError?.Invoke(error);
        }

        /// <summary>Handles a value notification.</summary>
        /// <param name="value">The emitted value.</param>
        public void OnNext(T value) => onNext?.Invoke(value);
    }
}
