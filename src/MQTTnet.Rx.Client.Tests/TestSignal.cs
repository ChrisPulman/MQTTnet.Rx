// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides a mutable observable test source compatible with both package variants.</summary>
/// <typeparam name="T">The emitted value type.</typeparam>
internal sealed class TestSignal<T> : IObservable<T>, IObserver<T>, IDisposable
{
#if REACTIVE_SHIM
    /// <summary>Stores the reactive implementation of this test source.</summary>
    private readonly System.Reactive.Subjects.Subject<T> _inner = new();
#else
    /// <summary>Stores the lean implementation of this test source.</summary>
    private readonly ReactiveUI.Primitives.Signals.Signal<T> _inner = new();
#endif

    /// <inheritdoc/>
    public void Dispose() => _inner.Dispose();

    /// <inheritdoc/>
    public void OnCompleted() => _inner.OnCompleted();

    /// <inheritdoc/>
    public void OnError(Exception error) => _inner.OnError(error);

    /// <inheritdoc/>
    public void OnNext(T value) => _inner.OnNext(value);

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer) => _inner.Subscribe(observer);
}
