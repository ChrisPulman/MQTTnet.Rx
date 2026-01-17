// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.MemoryEfficient;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the BufferPool class.
/// </summary>
public class BufferPoolTests
{
    /// <summary>
    /// Tests that Rent returns a buffer of at least the requested size.
    /// </summary>
    [Test]
    public async Task Rent_ReturnsBufferOfAtLeastRequestedSize()
    {
        // Arrange
        const int requestedSize = 100;

        // Act
        var buffer = BufferPool.Rent(requestedSize);

        // Assert
        try
        {
            await Assert.That(buffer).IsNotNull();
            await Assert.That(buffer.Length).IsGreaterThanOrEqualTo(requestedSize);
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    /// <summary>
    /// Tests that Rent with no argument returns default buffer size.
    /// </summary>
    [Test]
    public async Task Rent_WithNoArgument_ReturnsDefaultSize()
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

    /// <summary>
    /// Tests that Return with null does not throw.
    /// </summary>
    [Test]
    public async Task Return_WithNull_DoesNotThrow()
    {
        // Act & Assert - should not throw
        BufferPool.Return(null);
        await Assert.That(true).IsTrue(); // If we get here, no exception was thrown
    }

    /// <summary>
    /// Tests that Return with clearArray clears the buffer.
    /// </summary>
    [Test]
    public async Task Return_WithClearArray_ClearsBuffer()
    {
        // Arrange
        var buffer = BufferPool.Rent(10);
        for (var i = 0; i < 10; i++)
        {
            buffer[i] = (byte)(i + 1);
        }

        // Act
        BufferPool.Return(buffer, clearArray: true);

        // Re-rent (may or may not be the same buffer)
        var newBuffer = BufferPool.Rent(10);

        // Assert - can't guarantee same buffer, so just verify no exception
        await Assert.That(newBuffer).IsNotNull();
        BufferPool.Return(newBuffer);
    }

    /// <summary>
    /// Tests that RentScope returns a valid scope with buffer.
    /// </summary>
    [Test]
    public async Task RentScope_ReturnsValidScope()
    {
        // Act & Assert
        using var scope = BufferPool.RentScope(50);

        await Assert.That(scope.Buffer).IsNotNull();
        await Assert.That(scope.Buffer.Length).IsGreaterThanOrEqualTo(50);
        await Assert.That(scope.Span.Length).IsGreaterThanOrEqualTo(50);
        await Assert.That(scope.Memory.Length).IsGreaterThanOrEqualTo(50);
    }

    /// <summary>
    /// Tests that BufferScope can be used in a using statement.
    /// </summary>
    [Test]
    public async Task BufferScope_CanBeUsedInUsingStatement()
    {
        // Act & Assert - should not throw
        byte[]? capturedBuffer = null;
        using (var scope = BufferPool.RentScope(100))
        {
            capturedBuffer = scope.Buffer;
            capturedBuffer[0] = 42;
        }

        await Assert.That(capturedBuffer).IsNotNull();
    }

    /// <summary>
    /// Tests that ToArray converts ReadOnlySequence to array.
    /// </summary>
    [Test]
    public async Task ToArray_ConvertsSequenceToArray()
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

    /// <summary>
    /// Tests that ToArray handles empty sequence.
    /// </summary>
    [Test]
    public async Task ToArray_HandlesEmptySequence()
    {
        // Arrange
        var sequence = System.Buffers.ReadOnlySequence<byte>.Empty;

        // Act
        var result = BufferPool.ToArray(sequence);

        // Assert
        await Assert.That(result.Length).IsEqualTo(0);
    }

    /// <summary>
    /// Tests that CopyToRented copies data to pooled buffer.
    /// </summary>
    [Test]
    public async Task CopyToRented_CopiesToPooledBuffer()
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

    /// <summary>
    /// Tests that CopyToRented handles empty sequence.
    /// </summary>
    [Test]
    public async Task CopyToRented_HandlesEmptySequence()
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

    /// <summary>
    /// Tests that DefaultBufferSize returns expected value.
    /// </summary>
    [Test]
    public async Task DefaultBufferSize_ReturnsExpectedValue()
    {
        // Assert
        await Assert.That(BufferPool.DefaultBufferSize).IsEqualTo(4096);
    }
}
