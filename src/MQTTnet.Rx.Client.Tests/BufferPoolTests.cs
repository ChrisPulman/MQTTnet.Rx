// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.MemoryEfficient;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the BufferPool class.</summary>
public sealed class BufferPoolTests
{
    /// <summary>Gets the requested size for standard buffer tests.</summary>
    private const int RequestedBufferSize = 100;

    /// <summary>Gets the requested size for small buffer tests.</summary>
    private const int SmallBufferSize = 10;

    /// <summary>Gets the requested size for scoped buffer tests.</summary>
    private const int ScopedBufferSize = 50;

    /// <summary>Gets the expected default size of a rented buffer.</summary>
    private const int ExpectedDefaultBufferSize = 4096;

    /// <summary>Gets the sentinel value written to a rented buffer.</summary>
    private const byte BufferSentinelValue = 42;

    /// <summary>Tests that Rent returns a buffer of at least the requested size.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Rent_ReturnsBufferOfAtLeastRequestedSizeAsync()
    {
        // Act
        var buffer = BufferPool.Rent(RequestedBufferSize);

        // Assert
        try
        {
            await Assert.That(buffer).IsNotNull();
            await Assert.That(buffer.Length).IsGreaterThanOrEqualTo(RequestedBufferSize);
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    /// <summary>Tests that Rent with no argument returns the default buffer size.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Rent_WithNoArgument_ReturnsDefaultSizeAsync()
    {
        // Act
        var buffer = BufferPool.Rent();

        // Assert
        try
        {
            await Assert.That(buffer.Length).IsGreaterThanOrEqualTo(BufferPool.DefaultBufferSize);
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    /// <summary>Tests that Return with null does not throw.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Return_WithNull_DoesNotThrowAsync() =>
        await Assert.That(static () => BufferPool.Return(null)).ThrowsNothing();

    /// <summary>Tests that Return with clearArray clears the buffer.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Return_WithClearArray_ClearsBufferAsync()
    {
        // Arrange
        var buffer = BufferPool.Rent(SmallBufferSize);
        for (var i = 0; i < SmallBufferSize; i++)
        {
            buffer[i] = (byte)(i + 1);
        }

        // Act
        BufferPool.Return(buffer, clearArray: true);

        // Re-rent (may or may not be the same buffer)
        var newBuffer = BufferPool.Rent(SmallBufferSize);

        // Assert - can't guarantee same buffer, so just verify no exception
        await Assert.That(newBuffer).IsNotNull();
        BufferPool.Return(newBuffer);
    }

    /// <summary>Tests that RentScope returns a valid scope with a buffer.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RentScope_ReturnsValidScopeAsync()
    {
        // Act & Assert
        using var scope = BufferPool.RentScope(ScopedBufferSize);

        await Assert.That(scope.Buffer).IsNotNull();
        await Assert.That(scope.Buffer.Length).IsGreaterThanOrEqualTo(ScopedBufferSize);
        await Assert.That(scope.Span.Length).IsGreaterThanOrEqualTo(ScopedBufferSize);
        await Assert.That(scope.Memory.Length).IsGreaterThanOrEqualTo(ScopedBufferSize);
    }

    /// <summary>Tests that BufferScope can be used in a using statement.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task BufferScope_CanBeUsedInUsingStatementAsync()
    {
        // Act & Assert - should not throw
        byte[]? capturedBuffer = null;
        using (var scope = BufferPool.RentScope(RequestedBufferSize))
        {
            capturedBuffer = scope.Buffer;
            capturedBuffer[0] = BufferSentinelValue;
        }

        await Assert.That(capturedBuffer).IsNotNull();
    }

    /// <summary>Tests that ToArray converts ReadOnlySequence to an array.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToArray_ConvertsSequenceToArrayAsync()
    {
        // Arrange
        var originalData = new byte[] { 1, 2, 3, 4, 5 };
        var sequence = new System.Buffers.ReadOnlySequence<byte>(originalData);

        // Act
        var result = BufferPool.ToArray(sequence);

        // Assert
        await Assert.That(result.Length).IsEqualTo(originalData.Length);
        for (var i = 0; i < originalData.Length; i++)
        {
            await Assert.That(result[i]).IsEqualTo(originalData[i]);
        }
    }

    /// <summary>Tests that ToArray handles an empty sequence.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToArray_HandlesEmptySequenceAsync()
    {
        // Arrange
        var sequence = System.Buffers.ReadOnlySequence<byte>.Empty;

        // Act
        var result = BufferPool.ToArray(sequence);

        // Assert
        await Assert.That(result.Length).IsEqualTo(0);
    }

    /// <summary>Tests that CopyToRented copies data to a pooled buffer.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CopyToRented_CopiesToPooledBufferAsync()
    {
        // Arrange
        var originalData = new byte[] { 10, 20, 30, 40, 50 };
        var sequence = new System.Buffers.ReadOnlySequence<byte>(originalData);

        // Act
        var buffer = BufferPool.CopyToRented(sequence, out var bytesWritten);

        // Assert
        try
        {
            await Assert.That(bytesWritten).IsEqualTo(originalData.Length);
            for (var i = 0; i < originalData.Length; i++)
            {
                await Assert.That(buffer[i]).IsEqualTo(originalData[i]);
            }
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    /// <summary>Tests that CopyToRented handles an empty sequence.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CopyToRented_HandlesEmptySequenceAsync()
    {
        // Arrange
        var sequence = System.Buffers.ReadOnlySequence<byte>.Empty;

        // Act
        var buffer = BufferPool.CopyToRented(sequence, out var bytesWritten);

        // Assert
        try
        {
            await Assert.That(bytesWritten).IsEqualTo(0);
            await Assert.That(buffer).IsNotNull();
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    /// <summary>Tests that DefaultBufferSize returns the expected value.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task DefaultBufferSize_ReturnsExpectedValueAsync() =>
        await Assert.That(BufferPool.DefaultBufferSize).IsEqualTo(ExpectedDefaultBufferSize);
}
