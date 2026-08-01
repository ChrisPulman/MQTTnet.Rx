// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the PayloadExtensions class.</summary>
public class PayloadExtensionsTests
{
    /// <summary>Gets the topic used by tests that do not require topic-specific behavior.</summary>
    private const string DefaultTopic = "topic";

    /// <summary>Gets the delay used to allow observable notifications to complete.</summary>
    private const int ObservableNotificationDelayMilliseconds = 50;

    /// <summary>Gets the expected count for tests that produce a single result.</summary>
    private const int ExpectedSingleItemCount = 1;

    /// <summary>Gets the expected count for tests that produce multiple results.</summary>
    private const int ExpectedMultipleItemCount = 2;

    /// <summary>Gets the expected count for the three-message observable test.</summary>
    private const int ExpectedThreeItemCount = 3;

    /// <summary>Gets the expected byte length of the binary test payload.</summary>
    private const int ExpectedPayloadLength = 5;

    /// <summary>Tests that Payload returns the byte sequence.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ReturnsByteSequenceAsync()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, "test payload");

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That(payload.IsEmpty).IsFalse();
        await Assert.That((int)payload.Length).IsEqualTo("test payload".Length);
    }

    /// <summary>Tests that Payload returns an empty sequence for an empty payload.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ReturnsEmptyForEmptyPayloadAsync()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, string.Empty);

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That(payload.IsEmpty).IsTrue();
    }

    /// <summary>Tests that PayloadUtf8 decodes a UTF-8 string correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_DecodesStringAsync()
    {
        // Arrange
        const string originalText = "Hello, World! ?????";
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, originalText);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(originalText);
    }

    /// <summary>Tests that PayloadUtf8 returns an empty string for an empty payload.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_ReturnsEmptyForEmptyPayloadAsync()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, string.Empty);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(string.Empty);
    }

    /// <summary>Tests that PayloadUtf8 handles a single-segment payload.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_HandlesSingleSegmentAsync()
    {
        // Arrange
        const string text = "Simple text";
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, text);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(text);
    }

    /// <summary>Tests that ToUtf8String converts an observable of messages to strings.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_ConvertsObservableToStringsAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "Message 1"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "Message 2"),
            TestDataHelpers.CreateMessageReceivedArgs("topic3", "Message 3"),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8String()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedThreeItemCount);
        await Assert.That(results[0]).IsEqualTo("Message 1");
        await Assert.That(results[1]).IsEqualTo("Message 2");
        await Assert.That(results[2]).IsEqualTo("Message 3");
    }

    /// <summary>Tests that ToUtf8String handles Unicode characters.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_HandlesUnicodeAsync()
    {
        // Arrange
        const string unicodeText = "?? Rocket emoji and ???";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, unicodeText),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8String()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedSingleItemCount);
        await Assert.That(results[0]).IsEqualTo(unicodeText);
    }

    /// <summary>Tests that Payload throws for null arguments.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ThrowsForNullArgsAsync()
    {
        // Arrange
        MqttApplicationMessageReceivedEventArgs? args = null;

        // Act & Assert
        await Assert.That(() => args!.Payload()).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that PayloadUtf8 throws for null arguments.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_ThrowsForNullArgsAsync()
    {
        // Arrange
        MqttApplicationMessageReceivedEventArgs? args = null;

        // Act & Assert
        await Assert.That(() => args!.PayloadUtf8()).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that ToUtf8String works with filtering.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_WorksWithFilteringAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25.5"),
            TestDataHelpers.CreateMessageReceivedArgs("other/topic", "ignored"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60.0"),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .Where(static m => m.ApplicationMessage.Topic.StartsWith("sensors/", StringComparison.Ordinal))
            .ToUtf8String()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedMultipleItemCount);
        await Assert.That(results).Contains("25.5");
        await Assert.That(results).Contains("60.0");
    }

    /// <summary>Tests a payload with binary data.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_WorksWithBinaryDataAsync()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };
        var args = TestDataHelpers.CreateMessageReceivedArgs(DefaultTopic, binaryData);

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That((int)payload.Length).IsEqualTo(ExpectedPayloadLength);

        var resultArray = payload.ToArray();
        for (var i = 0; i < binaryData.Length; i++)
        {
            await Assert.That(resultArray[i]).IsEqualTo(binaryData[i]);
        }
    }
}
