// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;
using PrimitivesResult = ReactiveUI.Primitives.Result;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides System.Reactive bridges removed from the replacement asynchronous package.</summary>
public static class ObservableBridgeCompatibilityExtensions
{
    /// <summary>Provides bridges from System.Reactive observables.</summary>
    /// <typeparam name="T">The source observable element type.</typeparam>
    /// <param name="source">The synchronous observable to bridge.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>Converts a System.Reactive observable to an asynchronous observable.</summary>
        /// <returns>An asynchronous observable that mirrors the source.</returns>
        public IObservableAsync<T> ToSignal()
        {
            ArgumentNullException.ThrowIfNull(source);

            return SignalAsync.Create<T>(
                (observer, cancellationToken) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new ValueTask<IAsyncDisposable>(DisposableAsync.Empty);
                    }

                    var subscription = source.Subscribe(
                        new SynchronousBridgeObserver<T>(observer, cancellationToken));
                    return new ValueTask<IAsyncDisposable>(
                        new SynchronousSubscription(subscription));
                });
        }
    }

    /// <summary>Provides bridges to System.Reactive observables.</summary>
    /// <typeparam name="T">The source asynchronous observable element type.</typeparam>
    /// <param name="source">The asynchronous observable to bridge.</param>
    extension<T>(IObservableAsync<T> source)
    {
        /// <summary>Converts an asynchronous observable to a System.Reactive observable.</summary>
        /// <returns>A System.Reactive observable that mirrors the source.</returns>
        public IObservable<T> ToObservable()
        {
            ArgumentNullException.ThrowIfNull(source);

            return new AsObservable<T>(source);
        }
    }

    /// <summary>Represents one notification queued by the synchronous-to-asynchronous bridge.</summary>
    /// <typeparam name="T">The notification element type.</typeparam>
    /// <param name="Value">The value for a next notification.</param>
    /// <param name="Error">The error for a failed terminal notification.</param>
    /// <param name="IsTerminal">Whether the notification is terminal.</param>
    private readonly record struct Notification<T>(T? Value, Exception? Error, bool IsTerminal);

    /// <summary>Represents a System.Reactive subscription that is disposed asynchronously.</summary>
    /// <param name="subscription">The System.Reactive subscription to dispose.</param>
    private sealed class SynchronousSubscription(IDisposable subscription) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            subscription.Dispose();
            return default;
        }
    }

    /// <summary>Serializes synchronous source notifications before forwarding them asynchronously.</summary>
    /// <typeparam name="T">The notification element type.</typeparam>
    /// <param name="observer">The asynchronous observer that receives notifications.</param>
    /// <param name="cancellationToken">The token that cancels the bridge subscription.</param>
    private sealed class SynchronousBridgeObserver<T>(
        IObserverAsync<T> observer,
        CancellationToken cancellationToken) : IObserver<T>
    {
        /// <summary>Guards queued notifications and the drain state.</summary>
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif

        /// <summary>Stores notifications awaiting asynchronous delivery.</summary>
        private readonly Queue<Notification<T>> _queue = new();

        /// <summary>Indicates whether a notification drain is active.</summary>
        private bool _isDraining;

        /// <inheritdoc/>
        public void OnCompleted() => Enqueue(new(default, null, true));

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Enqueue(new(default, error, true));
        }

        /// <inheritdoc/>
        public void OnNext(T value) => Enqueue(new(value, null, false));

        /// <summary>Queues a notification and starts delivery when necessary.</summary>
        /// <param name="notification">The notification to deliver.</param>
        private void Enqueue(Notification<T> notification)
        {
            lock (_gate)
            {
                _queue.Enqueue(notification);
                if (_isDraining)
                {
                    return;
                }

                _isDraining = true;
            }

            _ = DrainAsync();
        }

        /// <summary>Delivers queued notifications in source order.</summary>
        /// <returns>A task that completes when the active drain completes.</returns>
        private async Task DrainAsync()
        {
            try
            {
                while (true)
                {
                    Notification<T> notification;
                    lock (_gate)
                    {
                        if (_queue.Count == 0)
                        {
                            _isDraining = false;
                            return;
                        }

                        notification = _queue.Dequeue();
                    }

                    if (notification.IsTerminal)
                    {
                        await observer
                            .OnCompletedAsync(
                                notification.Error is null
                                    ? PrimitivesResult.Success
                                    : PrimitivesResult.Failure(notification.Error))
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await observer
                            .OnNextAsync(notification.Value!, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Subscription cancellation ends pending asynchronous delivery.
            }
            catch (Exception exception)
            {
                Trace.TraceError("Async observable notification delivery failed: {0}", exception);
            }
            finally
            {
                lock (_gate)
                {
                    _queue.Clear();
                    _isDraining = false;
                }
            }
        }
    }

    /// <summary>Adapts an asynchronous observable to System.Reactive.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="source">The asynchronous observable to adapt.</param>
    private sealed class AsObservable<T>(IObservableAsync<T> source) : IObservable<T>
    {
        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            var cancellation = new CancellationTokenSource();
            return new AsynchronousSubscription(
                cancellation,
                SubscribeAsync(observer, cancellation.Token));
        }

        /// <summary>Subscribes the System.Reactive observer to the asynchronous source.</summary>
        /// <param name="observer">The System.Reactive observer to subscribe.</param>
        /// <param name="cancellationToken">The token that cancels the subscription.</param>
        /// <returns>A task that produces the asynchronous subscription.</returns>
        private async Task<IAsyncDisposable?> SubscribeAsync(
            IObserver<T> observer,
            CancellationToken cancellationToken)
        {
            try
            {
                return await source
                    .SubscribeAsync(new AsynchronousBridgeObserver<T>(observer), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                observer.OnError(exception);
                return null;
            }
        }
    }

    /// <summary>Disposes an asynchronous subscription when its System.Reactive counterpart is disposed.</summary>
    /// <param name="cancellation">The source used to cancel the asynchronous subscription.</param>
    /// <param name="subscription">The task that produces the asynchronous subscription.</param>
    private sealed class AsynchronousSubscription(
        CancellationTokenSource cancellation,
        Task<IAsyncDisposable?> subscription) : IDisposable
    {
        /// <summary>Tracks whether synchronous disposal has already started.</summary>
        private int _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            cancellation.Cancel();
            _ = CleanupAsync(subscription, cancellation);
        }

        /// <summary>Disposes an asynchronous subscription after its cancellation source has been cancelled.</summary>
        /// <param name="subscription">The task that produces the asynchronous subscription.</param>
        /// <param name="cancellation">The source used to cancel the asynchronous subscription.</param>
        /// <returns>A task that completes when disposal has finished.</returns>
        private static async Task CleanupAsync(
            Task<IAsyncDisposable?> subscription,
            CancellationTokenSource cancellation)
        {
            try
            {
                var disposable = await subscription.ConfigureAwait(false);
                if (disposable is not null)
                {
                    await disposable.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation is the expected disposal path.
            }
            catch (Exception exception)
            {
                Trace.TraceError("Async observable disposal failed: {0}", exception);
            }
            finally
            {
                cancellation.Dispose();
            }
        }
    }

    /// <summary>Forwards asynchronous notifications to a System.Reactive observer.</summary>
    /// <typeparam name="T">The notification element type.</typeparam>
    /// <param name="observer">The System.Reactive observer that receives notifications.</param>
    private sealed class AsynchronousBridgeObserver<T>(IObserver<T> observer) : WitnessAsync<T>
    {
        /// <inheritdoc/>
        protected override ValueTask OnCompletedAsyncCore(PrimitivesResult result)
        {
            if (result.IsFailure)
            {
                observer.OnError(result.Exception);
            }
            else
            {
                observer.OnCompleted();
            }

            return default;
        }

        /// <inheritdoc/>
        protected override ValueTask OnErrorResumeAsyncCore(
            Exception error,
            CancellationToken cancellationToken)
        {
            observer.OnError(error);
            return default;
        }

        /// <inheritdoc/>
        protected override ValueTask OnNextAsyncCore(T value, CancellationToken cancellationToken)
        {
            observer.OnNext(value);
            return default;
        }
    }
}
