// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive.Signals;
using RxLinq = System.Reactive.Linq;
using RxUnit = System.Reactive.Unit;

namespace MQTTnet.Rx.Client;

/// <summary>Provides observable operations retained for System.Reactive compatibility.</summary>
internal static class PrimitivesObservableCompatibilityExtensions
{
    /// <summary>Provides missing compatibility operations for an observable sequence.</summary>
    /// <typeparam name="T">The source observable element type.</typeparam>
    /// <param name="source">The observable sequence to extend.</param>
    extension<T>(IObservable<T> source)
    {

        /// <summary>Resubscribes to the source after every source error.</summary>
        /// <returns>A sequence that resubscribes after every source error.</returns>
        internal IObservable<T> Retry()
        {
            ArgumentNullException.ThrowIfNull(source);
            return Signal.Create<T>(observer => new RetrySubscription<T>(source, observer).Start());
        }

        /// <summary>Moves serialized observer notifications to the default task scheduler.</summary>
        /// <returns>A sequence whose observer callbacks run on the task pool.</returns>
        internal IObservable<T> ObserveOnTaskPool()
        {
            ArgumentNullException.ThrowIfNull(source);
            return Signal.Create<T>(observer => new TaskPoolObserver<T>(source, observer).Start());
        }

        /// <summary>Groups source elements by key while retaining the compatibility grouped-observable type.</summary>
        /// <typeparam name="TKey">The group key type.</typeparam>
        /// <param name="keySelector">The function that selects a key for each source element.</param>
        /// <returns>A sequence that emits one live group for each distinct key.</returns>
        internal IObservable<RxLinq.IGroupedObservable<TKey, T>> GroupBy<TKey>(
            Func<T, TKey> keySelector)
            where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(keySelector);

            return Signal.Create<RxLinq.IGroupedObservable<TKey, T>>(observer =>
            {
                var groupState = new GroupState<TKey, T>(observer, keySelector);
                return new MultipleDisposable(source.Subscribe(groupState), groupState);
            });
        }
    }

    /// <summary>Creates a cancellation-aware unit-valued signal from an asynchronous operation.</summary>
    /// <param name="operation">The operation to invoke for each subscription.</param>
    /// <returns>A signal that emits unit after the operation succeeds.</returns>
    internal static IObservable<RxUnit> FromTask(Func<CancellationToken, Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Signal.FromAsync(async cancellationToken =>
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return RxUnit.Default;
        });
    }

    /// <summary>Creates a unit-valued signal from an asynchronous operation.</summary>
    /// <param name="operation">The operation to invoke for each subscription.</param>
    /// <returns>A signal that emits unit after the operation succeeds.</returns>
    internal static IObservable<RxUnit> FromTask(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Signal.FromAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return RxUnit.Default;
        });
    }

    /// <summary>Represents one queued task-pool notification.</summary>
    /// <typeparam name="T">The notification value type.</typeparam>
    /// <param name="Value">The next value.</param>
    /// <param name="Error">The terminal error.</param>
    /// <param name="IsCompleted">Whether this is a successful terminal notification.</param>
    private readonly record struct TaskPoolNotification<T>(
        T? Value,
        Exception? Error,
        bool IsCompleted)
    {
        /// <summary>Creates a successful terminal notification.</summary>
        /// <returns>The terminal notification.</returns>
        public static TaskPoolNotification<T> Completed() => new(default, null, true);

        /// <summary>Creates a failed terminal notification.</summary>
        /// <param name="error">The terminal error.</param>
        /// <returns>The terminal notification.</returns>
        public static TaskPoolNotification<T> Failure(Exception error) =>
            new(default, error, false);

        /// <summary>Creates a next-value notification.</summary>
        /// <param name="value">The next value.</param>
        /// <returns>The next-value notification.</returns>
        public static TaskPoolNotification<T> Next(T value) => new(value, null, false);
    }

    /// <summary>Represents one live grouped sequence.</summary>
    /// <typeparam name="TKey">The group key type.</typeparam>
    /// <typeparam name="T">The group element type.</typeparam>
    /// <param name="key">The group key.</param>
    private sealed class GroupedSignal<TKey, T>(TKey key)
        : RxLinq.IGroupedObservable<TKey, T>,
            IDisposable
    {
        /// <summary>Distributes elements to group subscribers.</summary>
        private readonly ReactiveUI.Primitives.Signals.Signal<T> _signal = new();

        /// <inheritdoc/>
        public TKey Key { get; } = key;

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer) => _signal.Subscribe(observer);

        /// <inheritdoc/>
        public void Dispose() => _signal.Dispose();

        /// <summary>Completes the group.</summary>
        public void OnCompleted() => _signal.OnCompleted();

        /// <summary>Terminates the group with an error.</summary>
        /// <param name="error">The terminal error.</param>
        public void OnError(Exception error) => _signal.OnError(error);

        /// <summary>Publishes an element to the group.</summary>
        /// <param name="value">The element to publish.</param>
        public void OnNext(T value) => _signal.OnNext(value);
    }

    /// <summary>Owns and serializes the live groups created for one grouped subscription.</summary>
    /// <typeparam name="TKey">The group key type.</typeparam>
    /// <typeparam name="T">The group element type.</typeparam>
    /// <param name="observer">The observer receiving newly created groups.</param>
    /// <param name="keySelector">The function that selects a key for each element.</param>
    private sealed class GroupState<TKey, T>(
        IObserver<RxLinq.IGroupedObservable<TKey, T>> observer,
        Func<T, TKey> keySelector) : IObserver<T>, IDisposable
        where TKey : notnull
    {
        /// <summary>Guards the group dictionary.</summary>
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif

        /// <summary>Stores live groups by key.</summary>
        private readonly Dictionary<TKey, GroupedSignal<TKey, T>> _groups = [];

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_gate)
            {
                DisposeGroups();
            }
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
            lock (_gate)
            {
                foreach (var group in _groups.Values)
                {
                    group.OnCompleted();
                }

                DisposeGroups();
                observer.OnCompleted();
            }
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            lock (_gate)
            {
                foreach (var group in _groups.Values)
                {
                    group.OnError(error);
                }

                DisposeGroups();
                observer.OnError(error);
            }
        }

        /// <inheritdoc/>
        public void OnNext(T value)
        {
            lock (_gate)
            {
                var key = keySelector(value);
                if (!_groups.TryGetValue(key, out var group))
                {
                    group = new(key);
                    _groups.Add(key, group);
                    observer.OnNext(group);
                }

                group.OnNext(value);
            }
        }

        /// <summary>Disposes and removes every live group.</summary>
        private void DisposeGroups()
        {
            foreach (var group in _groups.Values)
            {
                group.Dispose();
            }

            _groups.Clear();
        }
    }

    /// <summary>Coordinates an unbounded, stack-safe retry subscription.</summary>
    /// <typeparam name="T">The sequence element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="observer">The downstream observer.</param>
    private sealed class RetrySubscription<T>(IObservable<T> source, IObserver<T> observer)
        : IObserver<T>,
            IDisposable
    {
        /// <summary>Owns the current source subscription.</summary>
        private readonly SwapDisposable _subscription = new();

        /// <summary>Tracks whether disposal has been requested.</summary>
        private int _disposed;

        /// <summary>Serializes synchronous and asynchronous resubscription requests.</summary>
        private int _work;

        /// <summary>Starts the initial source subscription.</summary>
        /// <returns>This retry lifetime.</returns>
        public RetrySubscription<T> Start()
        {
            Resubscribe();
            return this;
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

        /// <inheritdoc/>
        public void OnCompleted()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            observer.OnCompleted();
            Dispose();
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => Resubscribe();

        /// <inheritdoc/>
        public void OnNext(T value)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            observer.OnNext(value);
        }

        /// <summary>Queues a source subscription without recursive growth for synchronous failures.</summary>
        private void Resubscribe()
        {
            if (Interlocked.Increment(ref _work) != 1)
            {
                return;
            }

            do
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    _ = Interlocked.Exchange(ref _work, 0);
                    return;
                }

                try
                {
                    _subscription.Disposable = source.Subscribe(this);
                }
                catch (Exception)
                {
                    _ = Interlocked.Increment(ref _work);
                }
            } while (Interlocked.Decrement(ref _work) != 0);
        }
    }

    /// <summary>Serializes source notifications through the default task scheduler.</summary>
    /// <typeparam name="T">The sequence element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="observer">The downstream observer.</param>
    private sealed class TaskPoolObserver<T>(IObservable<T> source, IObserver<T> observer)
        : IObserver<T>,
            IDisposable
    {
        /// <summary>Owns the source subscription.</summary>
        private readonly SingleDisposable _subscription = new();

        /// <summary>Queues notifications in their source order.</summary>
        private readonly System.Collections.Concurrent.ConcurrentQueue<
            TaskPoolNotification<T>
        > _queue = new();

        /// <summary>Tracks whether disposal has been requested.</summary>
        private int _disposed;

        /// <summary>Coordinates drain scheduling.</summary>
        private int _work;

        /// <summary>Starts the source subscription.</summary>
        /// <returns>This observer lifetime.</returns>
        public TaskPoolObserver<T> Start()
        {
            _subscription.Create(source.Subscribe(this));
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _subscription.Dispose();
            _queue.Clear();
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
            _queue.Enqueue(TaskPoolNotification<T>.Completed());
            ScheduleDrain();
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            _queue.Enqueue(TaskPoolNotification<T>.Failure(error));
            ScheduleDrain();
        }

        /// <inheritdoc/>
        public void OnNext(T value)
        {
            _queue.Enqueue(TaskPoolNotification<T>.Next(value));
            ScheduleDrain();
        }

        /// <summary>Drains pending notifications on the task pool.</summary>
        private void Drain()
        {
            var missed = 1;
            do
            {
                while (_queue.TryDequeue(out var notification))
                {
                    if (notification.Error is not null)
                    {
                        observer.OnError(notification.Error);
                        Dispose();
                        return;
                    }

                    if (notification.IsCompleted)
                    {
                        observer.OnCompleted();
                        Dispose();
                        return;
                    }

                    observer.OnNext(notification.Value!);
                    if (Volatile.Read(ref _disposed) != 0)
                    {
                        return;
                    }
                }

                missed = Interlocked.Add(ref _work, -missed);
            } while (missed != 0);
        }

        /// <summary>Schedules a drain if no drain is already active.</summary>
        private void ScheduleDrain()
        {
            if (Volatile.Read(ref _disposed) != 0 || Interlocked.Increment(ref _work) != 1)
            {
                return;
            }

            _ = Task.Factory.StartNew(
                static state => ((TaskPoolObserver<T>)state!).Drain(),
                this,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }
    }
}
