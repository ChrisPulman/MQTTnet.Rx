// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>
/// A disposable scope that manages a rented buffer from the <see cref="BufferPool"/>.
/// </summary>
/// <remarks>
/// Use this struct in a using statement or declaration to ensure the buffer is returned to the pool.
/// </remarks>
public readonly record struct BufferScope : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BufferScope"/> struct.
    /// </summary>
    /// <param name="minimumLength">The minimum length of the buffer to rent.</param>
    public BufferScope(int minimumLength = 0)
    {
        Buffer = BufferPool.Rent(minimumLength);
    }

    /// <summary>
    /// Gets the rented buffer.
    /// </summary>
    public byte[] Buffer { get; }

    /// <summary>
    /// Gets a span over the buffer for efficient access.
    /// </summary>
    public Span<byte> Span => Buffer.AsSpan();

    /// <summary>
    /// Gets a memory over the buffer for async operations.
    /// </summary>
    public Memory<byte> Memory => Buffer.AsMemory();

    /// <summary>
    /// Returns the buffer to the pool.
    /// </summary>
    public void Dispose() => BufferPool.Return(Buffer);
}
