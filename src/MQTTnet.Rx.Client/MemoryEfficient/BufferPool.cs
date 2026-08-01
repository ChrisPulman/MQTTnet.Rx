// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>Provides high-performance buffer management for MQTT operations using pooled memory.</summary>
/// <remarks>
/// This class provides thread-safe access to pooled byte arrays, reducing garbage collection pressure
/// in high-throughput MQTT scenarios. All methods are designed for minimal allocation.
/// </remarks>
public static class BufferPool
{
    /// <summary>The default buffer size used for MQTT message operations.</summary>
    private const int DefaultBufferLength = 4096;

    /// <summary>The shared pool used to rent MQTT message buffers.</summary>
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

    /// <summary>Gets the default buffer size used for MQTT message operations.</summary>
    public static int DefaultBufferSize => DefaultBufferLength;

    /// <summary>Rents a byte array from the pool using the default buffer size.</summary>
    /// <returns>A byte array from the pool with at least the default buffer size.</returns>
    public static byte[] Rent() => Pool.Rent(DefaultBufferLength);

    /// <summary>Rents a byte array from the pool with at least the specified minimum length.</summary>
    /// <param name="minimumLength">The minimum length of the array to rent. Must be non-negative.</param>
    /// <returns>A byte array from the pool with at least the specified length. The array may be larger than
    /// requested.</returns>
    /// <remarks>
    /// The caller is responsible for returning the array to the pool using the Return method.
    /// The contents of the returned array are not cleared and may contain data from previous uses.
    /// </remarks>
    public static byte[] Rent(int minimumLength)
    {
        var size = minimumLength <= 0 ? DefaultBufferSize : minimumLength;
        return Pool.Rent(size);
    }

    /// <summary>Returns a previously rented byte array to the pool without clearing its contents.</summary>
    /// <param name="array">The array to return to the pool. If null, the method does nothing.</param>
    public static void Return(byte[]? array) => Return(array, clearArray: false);

    /// <summary>Returns a previously rented byte array to the pool.</summary>
    /// <param name="array">The array to return to the pool. If null, the method does nothing.</param>
    /// <param name="clearArray">
    /// If true, clears the array before returning it to the pool. Use this when the array contained sensitive data.
    /// Default is false for performance.
    /// </param>
    public static void Return(byte[]? array, bool clearArray)
    {
        if (array is null)
        {
            return;
        }

        Pool.Return(array, clearArray);
    }

    /// <summary>Rents a buffer scope using the default buffer size.</summary>
    /// <returns>A <see cref="BufferScope"/> that manages the lifetime of the rented buffer.</returns>
    public static BufferScope RentScope() => new();

    /// <summary>Rents a buffer scope using a buffer of at least the specified size.</summary>
    /// <param name="minimumLength">The minimum length of the buffer to rent.</param>
    /// <returns>A <see cref="BufferScope"/> that manages the lifetime of the rented buffer.</returns>
    public static BufferScope RentScope(int minimumLength) => new(minimumLength);

    /// <summary>Copies data from a <see cref="ReadOnlySequence{T}"/> to a newly allocated byte array.</summary>
    /// <param name="sequence">The sequence to copy from.</param>
    /// <returns>A new byte array containing the copied data.</returns>
    public static byte[] ToArray(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
        {
            return [];
        }

        if (sequence.IsSingleSegment)
        {
            return sequence.FirstSpan.ToArray();
        }

        var result = new byte[sequence.Length];
        sequence.CopyTo(result);
        return result;
    }

    /// <summary>Copies data from a <see cref="ReadOnlySequence{T}"/> to a rented buffer.</summary>
    /// <param name="sequence">The sequence to copy from.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the buffer.</param>
    /// <returns>A rented byte array containing the copied data. The caller must return this to the pool.</returns>
    public static byte[] CopyToRented(in ReadOnlySequence<byte> sequence, out int bytesWritten)
    {
        if (sequence.IsEmpty)
        {
            bytesWritten = 0;
            return Rent();
        }

        var length = (int)sequence.Length;
        var buffer = Rent(length);
        sequence.CopyTo(buffer);
        bytesWritten = length;
        return buffer;
    }
}
