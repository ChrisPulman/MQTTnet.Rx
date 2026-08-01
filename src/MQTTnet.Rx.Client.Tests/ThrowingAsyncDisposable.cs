// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Signals subscription and raises a configured failure from asynchronous disposal.</summary>
/// <param name="failure">The disposal failure.</param>
internal sealed class ThrowingAsyncDisposable(Exception failure) : IAsyncDisposable
{
    /// <summary>Signals that the source returned this lifetime.</summary>
    private readonly TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Signals that disposal was attempted.</summary>
    private readonly TaskCompletionSource<bool> _attempted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets the disposal-attempt task.</summary>
    internal Task Attempted => _attempted.Task;

    /// <summary>Gets the source-subscription task.</summary>
    internal Task Subscribed => _subscribed.Task;

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _ = _attempted.TrySetResult(true);
        return ValueTask.FromException(failure);
    }

    /// <summary>Signals that this instance has been returned from source subscription.</summary>
    /// <returns>Whether this call completed the signal.</returns>
    internal bool SignalSubscribed() => _subscribed.TrySetResult(true);
}
