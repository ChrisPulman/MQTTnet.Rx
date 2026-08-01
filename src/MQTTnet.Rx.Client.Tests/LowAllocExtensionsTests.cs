// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.MemoryEfficient;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Reactive;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the LowAllocExtensions class.</summary>
public sealed class LowAllocExtensionsTests
{
    /// <summary>Gets the topic used by tests that do not require topic-specific behavior.</summary>
    private const string DefaultTopic = "topic";

    /// <summary>Gets the first topic used by topic-grouping tests.</summary>
    private const string FirstGroupingTopic = "topic1";

    /// <summary>Gets the delay used to allow observable notifications to complete.</summary>
    private const int ObservableNotificationDelayMilliseconds = 50;

    /// <summary>Gets the expected count for tests that produce a single result.</summary>
    private const int ExpectedSingleItemCount = 1;

    /// <summary>Gets the expected count for tests that produce multiple results.</summary>
    private const int ExpectedMultipleItemCount = 2;

    /// <summary>Gets the expected byte length of the test payload.</summary>
    private const int ExpectedPayloadLength = 5;

    /// <summary>Tests that ToUtf8StringLowAlloc decodes strings correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8StringLowAlloc_DecodesStringsCorrectlyAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "Hello World"),
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "Unicode: ???"),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8StringLowAlloc()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedMultipleItemCount);
        await Assert.That(results[0]).IsEqualTo("Hello World");
        await Assert.That(results[1]).IsEqualTo("Unicode: ???");
    }

    /// <summary>Tests that ToUtf8StringLowAlloc handles an empty payload.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8StringLowAlloc_HandlesEmptyPayloadAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, string.Empty),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8StringLowAlloc()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedSingleItemCount);
        await Assert.That(results[0]).IsEqualTo(string.Empty);
    }

    /// <summary>Tests that GetPayloadLength returns the correct length.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetPayloadLength_ReturnsCorrectLengthAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "12345"),
        };

        var results = new List<int>();

        // Act
        using var subscription = messages.ToObservable()
            .GetPayloadLength()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedSingleItemCount);
        await Assert.That(results[0]).IsEqualTo(ExpectedPayloadLength);
    }

    /// <summary>Tests that WhereTopicStartsWith filters correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicStartsWith_FiltersCorrectlyAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("devices/status", "online"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60"),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicStartsWith("sensors/")
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedMultipleItemCount);
    }

    /// <summary>Tests that WhereTopicEndsWith filters correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicEndsWith_FiltersCorrectlyAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("room1/temperature", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("room2/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("room3/temperature", "22"),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicEndsWith("/temperature")
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedMultipleItemCount);
    }

    /// <summary>Tests that GroupByTopic groups messages by topic.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GroupByTopic_GroupsMessagesByTopicAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(FirstGroupingTopic, "a"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "b"),
            TestDataHelpers.CreateMessageReceivedArgs(FirstGroupingTopic, "c"),
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopic()
            .Select(static group => group.Key)
            .Subscribe(groupKeys.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(ExpectedMultipleItemCount);
        await Assert.That(groupKeys).Contains(FirstGroupingTopic);
        await Assert.That(groupKeys).Contains("topic2");
    }

    /// <summary>Tests that ToPayloadArray converts a payload to a byte array.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToPayloadArray_ConvertsPayloadToArrayAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "Hello"),
        };

        var results = new List<byte[]>();

        // Act
        using var subscription = messages.ToObservable()
            .ToPayloadArray()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedSingleItemCount);
        await Assert.That(results[0].Length).IsEqualTo(ExpectedPayloadLength);
    }

    /// <summary>Tests that ToPooledPayload returns a pooled buffer with the correct data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToPooledPayload_ReturnsPooledBufferAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "Test data"),
        };

        var results = new List<(byte[] Buffer, int Length, Action ReturnBuffer)>();

        // Act
        using var subscription = messages.ToObservable()
            .ToPooledPayload()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedSingleItemCount);
        await Assert.That(results[0].Length).IsEqualTo("Test data".Length);

        // Clean up - return buffer to pool
        results[0].ReturnBuffer();
    }

    /// <summary>Tests that BatchProcess processes batches by count.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task BatchProcess_ProcessesBatchesByCountAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("t1", "1"),
            TestDataHelpers.CreateMessageReceivedArgs("t2", "2"),
            TestDataHelpers.CreateMessageReceivedArgs("t3", "3"),
            TestDataHelpers.CreateMessageReceivedArgs("t4", "4"),
        };

        var batchSizes = new List<int>();

        // Act
        using var subscription = messages.ToObservable()
            .BatchProcess(ExpectedMultipleItemCount, static batch => batch.Count)
            .Subscribe(batchSizes.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(batchSizes).Count().IsEqualTo(ExpectedMultipleItemCount);
        await Assert.That(batchSizes[0]).IsEqualTo(ExpectedMultipleItemCount);
        await Assert.That(batchSizes[1]).IsEqualTo(ExpectedMultipleItemCount);
    }
}
