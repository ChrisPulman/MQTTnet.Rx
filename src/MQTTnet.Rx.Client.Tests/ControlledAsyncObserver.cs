// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Controls asynchronous delivery and reports when the delivery fault has propagated.</summary>
/// <typeparam name="T">The observed value type.</typeparam>
/// <param name="failure">The failure raised by the next callback.</param>
/// <param name="cancellation">The optional source cancelled immediately before the failure.</param>
/// <param name="release">The optional gate awaited before failing.</param>
internal sealed class ControlledAsyncObserver<T>(
    Exception failure,
    CancellationTokenSource? cancellation = null,
    Task? release = null) : IObserverAsync<T>
{
    /// <summary>Signals entry to the next callback.</summary>
    private readonly TaskCompletionSource<bool> _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals exit from the next callback.</summary>
    private readonly TaskCompletionSource<bool> _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the callback-entry task.</summary>
    internal Task Entered => _entered.Task;

    /// <summary>Gets the callback-exit task.</summary>
    internal Task Finished => _finished.Task;

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    ValueTask IObserverAsync<T>.OnCompletedAsync(Result result)
    {
        _ = result;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    ValueTask IObserverAsync<T>.OnErrorResumeAsync(Exception error, CancellationToken cancellationToken)
    {
        _ = error;
        _ = cancellationToken;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    async ValueTask IObserverAsync<T>.OnNextAsync(T value, CancellationToken cancellationToken)
    {
        GC.KeepAlive(value);
        _ = cancellationToken;
        _ = _entered.TrySetResult(true);
        try
        {
            if (release is not null)
            {
                await release.ConfigureAwait(false);
            }

            if (cancellation is not null)
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
            }

            throw failure;
        }
        finally
        {
            _ = _finished.TrySetResult(true);
        }
    }
}
