// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Rx.Server;

/// <summary>
/// Provides factory methods for creating observable sequences from asynchronous event patterns.
/// </summary>
/// <remarks>This class is intended for internal use to bridge asynchronous event-based patterns with the
/// IObservable{T} interface. All members are static and thread safety is guaranteed by design.</remarks>
internal static class CreateObservable
{
    /// <summary>
    /// Creates an observable sequence from an asynchronous event pattern, using the specified handlers to subscribe and
    /// unsubscribe from the event source.
    /// </summary>
    /// <remarks>The returned observable is shared among all subscribers and manages event handler
    /// registration automatically. The event handler is attached when the first subscription is made and detached when
    /// the last subscription is disposed.</remarks>
    /// <typeparam name="T">The type of the event argument passed to the observable sequence.</typeparam>
    /// <param name="addHandler">An action that attaches the provided asynchronous event handler to the event source. The handler receives event
    /// arguments of type T and returns a Task.</param>
    /// <param name="removeHandler">An action that detaches the provided asynchronous event handler from the event source.</param>
    /// <returns>An observable sequence that emits values of type T each time the underlying asynchronous event is raised.</returns>
    internal static IObservable<T> FromAsyncEvent<T>(Action<Func<T, Task>> addHandler, Action<Func<T, Task>> removeHandler) =>
        Observable.Create<T>(observer =>
            {
                Task Delegate(T args)
                {
                    observer.OnNext(args);
                    return Task.CompletedTask;
                }

                addHandler(Delegate);
                return Disposable.Create(() => removeHandler(Delegate));
            })
            .Publish()
            .RefCount();
}
