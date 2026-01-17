// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Rx.Client.MemoryEfficient;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the LowAllocExtensions class.
/// </summary>
public class LowAllocExtensionsTests
{
    /// <summary>
    /// Tests that ToUtf8StringLowAlloc decodes strings correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8StringLowAlloc_DecodesStringsCorrectly()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", "Hello World"),
            TestDataHelpers.CreateMessageReceivedArgs("topic", "Unicode: ???")
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8StringLowAlloc()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0]).IsEqualTo("Hello World");
        await Assert.That(results[1]).IsEqualTo("Unicode: ???");
    }

    /// <summary>
    /// Tests that ToUtf8StringLowAlloc handles empty payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8StringLowAlloc_HandlesEmptyPayload()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", string.Empty)
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8StringLowAlloc()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// Tests that GetPayloadLength returns correct length.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetPayloadLength_ReturnsCorrectLength()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", "12345")
        };

        var results = new List<int>();

        // Act
        using var subscription = messages.ToObservable()
            .GetPayloadLength()
            .Subscribe(i => results.Add(i));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]).IsEqualTo(5);
    }

    /// <summary>
    /// Tests that WhereTopicStartsWith filters correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicStartsWith_FiltersCorrectly()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("devices/status", "online"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicStartsWith("sensors/")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that WhereTopicEndsWith filters correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicEndsWith_FiltersCorrectly()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("room1/temperature", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("room2/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("room3/temperature", "22")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicEndsWith("/temperature")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that GroupByTopic groups messages by topic.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GroupByTopic_GroupsMessagesByTopic()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "a"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "b"),
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "c")
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopic()
            .Subscribe(group => groupKeys.Add(group.Key));

        await Task.Delay(50);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(2);
        await Assert.That(groupKeys).Contains("topic1");
        await Assert.That(groupKeys).Contains("topic2");
    }

    /// <summary>
    /// Tests that ToPayloadArray converts payload to byte array.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToPayloadArray_ConvertsPayloadToArray()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", "Hello")
        };

        var results = new List<byte[]>();

        // Act
        using var subscription = messages.ToObservable()
            .ToPayloadArray()
            .Subscribe(arr => results.Add(arr));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Length).IsEqualTo(5);
    }

    /// <summary>
    /// Tests that ToPooledPayload returns pooled buffer with correct data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToPooledPayload_ReturnsPooledBuffer()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", "Test data")
        };

        var results = new List<(byte[] Buffer, int Length, Action ReturnBuffer)>();

        // Act
        using var subscription = messages.ToObservable()
            .ToPooledPayload()
            .Subscribe(data =>
            {
                results.Add(data);
            });

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Length).IsEqualTo("Test data".Length);

        // Clean up - return buffer to pool
        results[0].ReturnBuffer();
    }

    /// <summary>
    /// Tests that BatchProcess processes batches by count.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task BatchProcess_ProcessesBatchesByCount()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("t1", "1"),
            TestDataHelpers.CreateMessageReceivedArgs("t2", "2"),
            TestDataHelpers.CreateMessageReceivedArgs("t3", "3"),
            TestDataHelpers.CreateMessageReceivedArgs("t4", "4")
        };

        var batchSizes = new List<int>();

        // Act
        using var subscription = messages.ToObservable()
            .BatchProcess(2, batch => batch.Count)
            .Subscribe(size => batchSizes.Add(size));

        await Task.Delay(50);

        // Assert
        await Assert.That(batchSizes).Count().IsEqualTo(2);
        await Assert.That(batchSizes[0]).IsEqualTo(2);
        await Assert.That(batchSizes[1]).IsEqualTo(2);
    }
}
