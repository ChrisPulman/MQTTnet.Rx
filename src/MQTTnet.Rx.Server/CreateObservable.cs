// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Creates observable projections for asynchronous events.</summary>
internal static class CreateObservable
{
    /// <summary>Creates a shared observable sequence from an asynchronous event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="addHandler">Adds an asynchronous event handler.</param>
    /// <param name="removeHandler">Removes an asynchronous event handler.</param>
    /// <returns>A shared observable event sequence.</returns>
    internal static IObservable<T> FromAsyncEvent<T>(
        Action<Func<T, Task>> addHandler,
        Action<Func<T, Task>> removeHandler) =>
        SignalFactory.Create<T>(observer =>
            {
                Task Delegate(T args)
                {
                    new Action<T>(observer.OnNext)(args);
                    return Task.CompletedTask;
                }

                addHandler(Delegate);
                return Scope.Create(
                    (removeHandler, Handler: (Func<T, Task>)Delegate),
                    static state => state.removeHandler(state.Handler));
            })
            .Publish()
            .RefCount();

    /// <summary>Creates an asynchronous observable sequence from an asynchronous event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="addHandler">Adds an asynchronous event handler.</param>
    /// <param name="removeHandler">Removes an asynchronous event handler.</param>
    /// <returns>An asynchronous observable event sequence.</returns>
    internal static IObservableAsync<T> FromAsyncEventSignal<T>(
        Action<Func<T, Task>> addHandler,
        Action<Func<T, Task>> removeHandler) =>
        SignalAsync.Create<T>((observer, cancellationToken) =>
        {
            Task Delegate(T args) => observer.OnNextAsync(args, cancellationToken).AsTask();

            addHandler(Delegate);

            return new ValueTask<IAsyncDisposable>(DisposableAsync.Create(
                (removeHandler, Handler: (Func<T, Task>)Delegate),
                static state =>
                {
                    state.removeHandler(state.Handler);
                    return default;
                }));
        });
}
