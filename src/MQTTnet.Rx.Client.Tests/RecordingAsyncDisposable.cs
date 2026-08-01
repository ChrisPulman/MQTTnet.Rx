// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Records disposal of an asynchronous bridge subscription.</summary>
/// <param name="disposed">The signal completed when disposal occurs.</param>
internal sealed class RecordingAsyncDisposable(TaskCompletionSource<bool> disposed) : IAsyncDisposable
{
    /// <summary>Signals that disposal occurred.</summary>
    private readonly TaskCompletionSource<bool> _disposed = disposed;

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _ = _disposed.TrySetResult(true);
        return default;
    }
}
