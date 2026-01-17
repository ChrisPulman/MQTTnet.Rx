// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reactive.Linq;
using System.Text;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the PayloadExtensions class.
/// </summary>
public class PayloadExtensionsTests
{
    /// <summary>
    /// Tests that Payload returns the byte sequence.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ReturnsByteSequence()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", "test payload");

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That(payload.IsEmpty).IsFalse();
        await Assert.That((int)payload.Length).IsEqualTo("test payload".Length);
    }

    /// <summary>
    /// Tests that Payload returns empty sequence for empty payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ReturnsEmptyForEmptyPayload()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", string.Empty);

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That(payload.IsEmpty).IsTrue();
    }

    /// <summary>
    /// Tests that PayloadUtf8 decodes UTF-8 string correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_DecodesString()
    {
        // Arrange
        var originalText = "Hello, World! ?????";
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", originalText);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(originalText);
    }

    /// <summary>
    /// Tests that PayloadUtf8 returns empty string for empty payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_ReturnsEmptyForEmptyPayload()
    {
        // Arrange
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", string.Empty);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(string.Empty);
    }

    /// <summary>
    /// Tests that PayloadUtf8 handles single segment payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_HandlesSingleSegment()
    {
        // Arrange
        var text = "Simple text";
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", text);

        // Act
        var payload = args.PayloadUtf8();

        // Assert
        await Assert.That(payload).IsEqualTo(text);
    }

    /// <summary>
    /// Tests that ToUtf8String extension converts observable of messages to strings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_ConvertsObservableToStrings()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "Message 1"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "Message 2"),
            TestDataHelpers.CreateMessageReceivedArgs("topic3", "Message 3")
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8String()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).HasCount().EqualTo(3);
        await Assert.That(results[0]).IsEqualTo("Message 1");
        await Assert.That(results[1]).IsEqualTo("Message 2");
        await Assert.That(results[2]).IsEqualTo("Message 3");
    }

    /// <summary>
    /// Tests that ToUtf8String handles unicode characters.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_HandlesUnicode()
    {
        // Arrange
        var unicodeText = "?? Rocket emoji and ???";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", unicodeText)
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .ToUtf8String()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).HasCount().EqualTo(1);
        await Assert.That(results[0]).IsEqualTo(unicodeText);
    }

    /// <summary>
    /// Tests that Payload throws for null args.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_ThrowsForNullArgs()
    {
        // Arrange
        MqttApplicationMessageReceivedEventArgs? args = null;

        // Act & Assert
        await Assert.That(() => args!.Payload()).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that PayloadUtf8 throws for null args.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PayloadUtf8_ThrowsForNullArgs()
    {
        // Arrange
        MqttApplicationMessageReceivedEventArgs? args = null;

        // Act & Assert
        await Assert.That(() => args!.PayloadUtf8()).Throws<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ToUtf8String works with filtering.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToUtf8String_WorksWithFiltering()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25.5"),
            TestDataHelpers.CreateMessageReceivedArgs("other/topic", "ignored"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60.0")
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .Where(m => m.ApplicationMessage.Topic.StartsWith("sensors/"))
            .ToUtf8String()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).HasCount().EqualTo(2);
        await Assert.That(results).Contains("25.5");
        await Assert.That(results).Contains("60.0");
    }

    /// <summary>
    /// Tests payload with binary data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Payload_WorksWithBinaryData()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };
        var args = TestDataHelpers.CreateMessageReceivedArgs("topic", binaryData);

        // Act
        var payload = args.Payload();

        // Assert
        await Assert.That((int)payload.Length).IsEqualTo(binaryData.Length);

        var resultArray = payload.ToArray();
        for (var i = 0; i < binaryData.Length; i++)
        {
            await Assert.That(resultArray[i]).IsEqualTo(binaryData[i]);
        }
    }
}
