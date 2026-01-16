// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides factory methods for creating observable sequences from asynchronous event patterns.
/// </summary>
/// <remarks>This class is intended for internal use to bridge asynchronous event-based patterns with the
/// IObservable{T} interface. All members are static and thread safety is guaranteed by design.</remarks>
internal static class CreateObservable
{
    /// <summary>
    /// Creates an observable sequence from an asynchronous event pattern, allowing event data to be observed as an
    /// IObservable{T}.
    /// </summary>
    /// <remarks>The observable sequence is shared among all subscribers and remains active as long as there
    /// is at least one subscription. Event handlers are attached and detached automatically as subscriptions are added
    /// or removed.</remarks>
    /// <typeparam name="T">The type of the event data provided to observers.</typeparam>
    /// <param name="addHandler">An action that attaches the specified asynchronous event handler to the event source. The handler receives event
    /// data of type T and returns a Task.</param>
    /// <param name="removeHandler">An action that detaches the specified asynchronous event handler from the event source.</param>
    /// <returns>An observable sequence that emits event data of type T each time the underlying event is raised.</returns>
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
