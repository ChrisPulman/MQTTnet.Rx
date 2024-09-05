// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Mqtt Extensions.
/// </summary>
internal static class CreateObservable
{
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
