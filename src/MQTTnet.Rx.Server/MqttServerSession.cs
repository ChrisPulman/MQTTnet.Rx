// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;
using ReactiveUI.Primitives.Disposables;

namespace MQTTnet.Rx.Server;

/// <summary>Owns one subscription to a shared MQTT server and its associated resources.</summary>
/// <remarks>
/// Dispose the session to remove subscriber-owned resources and release the shared server. Synchronous disposal waits
/// for server shutdown to finish; asynchronous callers can use <see cref="DisposeAsync"/> directly.
/// </remarks>
public sealed class MqttServerSession : IDisposable, IAsyncDisposable
{
    /// <summary>Releases this session from the shared server lifetime.</summary>
    private readonly Func<ValueTask> _releaseAsync;

    /// <summary>Contains subscriber-owned resources.</summary>
    private readonly MultipleDisposable _resources = [];

    /// <summary>Indicates whether disposal has already started.</summary>
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="MqttServerSession"/> class.</summary>
    /// <param name="server">The server shared by this session.</param>
    /// <param name="releaseAsync">The callback that releases the session from the shared server lifetime.</param>
    internal MqttServerSession(MqttServer server, Func<ValueTask> releaseAsync)
    {
        Server = server;
        _releaseAsync = releaseAsync;
    }

    /// <summary>Gets whether this session has been disposed.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Gets the MQTT server shared by this session.</summary>
    public MqttServer Server { get; }

    /// <summary>Adds a subscriber-owned resource to this session.</summary>
    /// <param name="resource">The resource to dispose with the session.</param>
    public void Add(IDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _resources.Add(resource);
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _resources.Dispose();
        }
        finally
        {
            await _releaseAsync().ConfigureAwait(false);
        }
    }
}
