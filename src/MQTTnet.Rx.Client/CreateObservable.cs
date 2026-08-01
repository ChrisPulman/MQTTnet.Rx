// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using RxVoid = System.Reactive.Unit;

namespace MQTTnet.Rx.Client;

/// <summary>Provides factory methods for creating observable sequences from asynchronous event patterns.</summary>
/// <remarks>This class bridges asynchronous event patterns with observable sequences.</remarks>
internal static class CreateObservable
{
    /// <summary>Resubscribes to a source indefinitely after errors.</summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <param name="source">The source to resubscribe.</param>
    /// <returns>An observable that preserves unbounded retry behavior.</returns>
    internal static IObservable<T> RetryForever<T>(IObservable<T> source) =>
        new RetryForeverObservable<T>(source);

    /// <summary>Creates an observable sequence from a standard .NET event.</summary>
    /// <typeparam name="T">The type of the event data provided to observers.</typeparam>
    /// <param name="addHandler">Attaches the event handler.</param>
    /// <param name="removeHandler">Detaches the event handler.</param>
    /// <returns>A shared observable sequence for the specified event.</returns>
    internal static IObservable<T> FromEvent<T>(
        Action<EventHandler<T>> addHandler,
        Action<EventHandler<T>> removeHandler) =>
        Signal
            .Create<T>(observer =>
            {
                void Handler(object? _, T args) => observer.OnNext(args);

                addHandler(Handler);
                return Scope.Create(
                    (removeHandler, Handler: (EventHandler<T>)Handler),
                    static state => state.removeHandler(state.Handler));
            })
            .Publish()
            .RefCount();

    /// <summary>Creates an asynchronous observable sequence from an awaited-handler registration.</summary>
    /// <typeparam name="T">The type of notification to observe.</typeparam>
    /// <param name="registerHandler">Registers a handler and returns its disposable registration.</param>
    /// <returns>An asynchronous observable sequence for the registered handler.</returns>
    internal static IObservableAsync<T> FromHandlerRegistration<T>(
        Func<Func<T, CancellationToken, ValueTask>, IDisposable> registerHandler) =>
        SignalAsync
            .Create<T>(
                (observer, cancellationToken) =>
                {
                    var registration = registerHandler(
                        (args, _) => observer.OnNextAsync(args, cancellationToken));
                    return new ValueTask<IAsyncDisposable>(
                        DisposableAsync.Create(
                            registration,
                            static value =>
                            {
                                value.Dispose();
                                return default;
                            }));
                });

    /// <summary>Creates an observable sequence from an asynchronous event pattern.</summary>
    /// <typeparam name="T">The type of the event data provided to observers.</typeparam>
    /// <param name="addHandler">Attaches the asynchronous event handler.</param>
    /// <param name="removeHandler">Detaches the asynchronous event handler.</param>
    /// <returns>A shared observable sequence for the specified event.</returns>
    internal static IObservable<T> FromAsyncEvent<T>(
        Action<Func<T, Task>> addHandler,
        Action<Func<T, Task>> removeHandler) =>
        Signal
            .Create<T>(observer =>
            {
                Task Handler(T args)
                {
                    observer.OnNext(args);
                    return Task.CompletedTask;
                }

                addHandler(Handler);
                return Scope.Create(
                    (removeHandler, Handler: (Func<T, Task>)Handler),
                    static state => state.removeHandler(state.Handler));
            })
            .Publish()
            .RefCount();

    /// <summary>Creates an asynchronous observable sequence from an asynchronous event pattern.</summary>
    /// <typeparam name="T">The type of event data to observe.</typeparam>
    /// <param name="addHandler">Attaches the asynchronous event handler.</param>
    /// <param name="removeHandler">Detaches the asynchronous event handler.</param>
    /// <returns>An asynchronous observable sequence for the specified event.</returns>
    internal static IObservableAsync<T> FromAsyncEventSignal<T>(
        Action<Func<T, Task>> addHandler,
        Action<Func<T, Task>> removeHandler) =>
        SignalAsync
            .Create<T>(
                (observer, cancellationToken) =>
                {
                    Task Handler(T args) => observer.OnNextAsync(args, cancellationToken).AsTask();

                    addHandler(Handler);

                    return new ValueTask<IAsyncDisposable>(
                        DisposableAsync.Create(
                            (removeHandler, Handler: (Func<T, Task>)Handler),
                            static state =>
                            {
                                state.removeHandler(state.Handler);
                                return default;
                            }));
                });

    /// <summary>Wraps an asynchronous operation that returns a value as an asynchronous observable sequence.</summary>
    /// <typeparam name="T">The value type returned by the operation.</typeparam>
    /// <param name="taskFactory">The asynchronous operation to execute.</param>
    /// <returns>An asynchronous observable sequence that emits the operation result.</returns>
    internal static IObservableAsync<T> FromAsyncTask<T>(
        Func<CancellationToken, Task<T>> taskFactory) =>
        SignalAsync.FromAsync(cancellationToken => new ValueTask<T>(
            taskFactory(cancellationToken)));

    /// <summary>Wraps an asynchronous operation without a result as an asynchronous observable sequence.</summary>
    /// <param name="taskFactory">The asynchronous operation to execute.</param>
    /// <returns>An asynchronous observable sequence that emits <see cref="RxVoid.Default"/> when the operation
    /// completes.</returns>
    internal static IObservableAsync<RxVoid> FromAsyncTask(
        Func<CancellationToken, Task> taskFactory) =>
        SignalAsync.FromAsync(async cancellationToken =>
        {
            await taskFactory(cancellationToken).ConfigureAwait(false);
            return RxVoid.Default;
        });

    /// <summary>Resubscribes to a source indefinitely while owning only the current subscription.</summary>
    /// <typeparam name="T">The source element type.</typeparam>
    /// <param name="source">The source to resubscribe.</param>
    private sealed class RetryForeverObservable<T>(IObservable<T> source) : IObservable<T>
    {
        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            var subscription = new RetrySubscription(source, observer);
            subscription.Start();
            return subscription;
        }

        /// <summary>Owns the active source subscription and replaces it after each error.</summary>
        /// <param name="source">The source to observe.</param>
        /// <param name="observer">The downstream observer.</param>
        private sealed class RetrySubscription(IObservable<T> source, IObserver<T> observer)
            : IObserver<T>,
                IDisposable
        {
            /// <summary>Holds the active source subscription.</summary>
            private readonly MutableDisposable _subscription = new();

            /// <summary>Tracks whether this subscription has been disposed.</summary>
            private int _disposed;

            /// <summary>Starts observing the source.</summary>
            public void Start() => _subscription.Disposable = source.Subscribe(this);

            /// <inheritdoc/>
            public void OnNext(T value)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }

                observer.OnNext(value);
            }

            /// <inheritdoc/>
            public void OnError(Exception error)
            {
                _ = error;
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }

                _subscription.Disposable = source.Subscribe(this);
            }

            /// <inheritdoc/>
            public void OnCompleted()
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }

                observer.OnCompleted();
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                _subscription.Dispose();
            }
        }
    }
}
