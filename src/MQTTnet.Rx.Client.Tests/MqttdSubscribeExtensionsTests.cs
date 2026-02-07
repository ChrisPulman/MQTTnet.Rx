// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the MqttdSubscribeExtensions class.
/// </summary>
public class MqttdSubscribeExtensionsTests
{
    /// <summary>
    /// Tests that ToDictionary converts JSON payload to dictionary.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToDictionary_ConvertsJsonToDictionary()
    {
        // Arrange
        const string json = """{"name":"sensor1","value":25.5}""";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", json)
        };

        var results = new List<Dictionary<string, object?>?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToDictionary()
            .Subscribe(d => results.Add(d));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]).IsNotNull();
        await Assert.That(results?[0]?["name"]).IsEqualTo("sensor1");
    }

    /// <summary>
    /// Tests that ToObject deserializes JSON to typed object.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToObject_DeserializesToTypedObject()
    {
        // Arrange
        var testData = new TestPayload { Name = "Test", Value = 42 };
        var messages = new[]
        {
            TestDataHelpers.CreateJsonMessageReceivedArgs("topic", testData)
        };

        var results = new List<TestPayload?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToObject<TestPayload>()
            .Subscribe(obj => results.Add(obj));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]).IsNotNull();
        await Assert.That(results[0]!.Name).IsEqualTo("Test");
        await Assert.That(results[0]!.Value).IsEqualTo(42);
    }

    /// <summary>
    /// Tests that ToObject uses custom serializer settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToObject_UsesCustomSettings()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        const string json = """{"Name":"Test","Value":100}""";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic", json)
        };

        var results = new List<TestPayload?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToObject<TestPayload>(options)
            .Subscribe(obj => results.Add(obj));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]!.Name).IsEqualTo("Test");
    }

    /// <summary>
    /// Tests that ToBool converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToBool_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { true, false, 1, 0, "true", "false" };
        var expected = new[] { true, false, true, false, true, false };
        var results = new List<bool>();

        // Act
        using var subscription = values.ToObservable()
            .ToBool()
            .Subscribe(b => results.Add(b));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            await Assert.That(results[i]).IsEqualTo(expected[i]);
        }
    }

    /// <summary>
    /// Tests that ToInt32 converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt32_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { 42, 100L, "200", 3.14 };
        var expected = new[] { 42, 100, 200, 3 };
        var results = new List<int>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt32()
            .Subscribe(i => results.Add(i));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            await Assert.That(results[i]).IsEqualTo(expected[i]);
        }
    }

    /// <summary>
    /// Tests that ToDouble converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToDouble_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { 42, 3.14, "2.5" };
        var results = new List<double>();

        // Act
        using var subscription = values.ToObservable()
            .ToDouble()
            .Subscribe(d => results.Add(d));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsEqualTo(42.0);
        await Assert.That(results[1]).IsEqualTo(3.14);
        await Assert.That(results[2]).IsEqualTo(2.5);
    }

    /// <summary>
    /// Tests that ToInt16 converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt16_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { (short)100, 200, "300" };
        var results = new List<short>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt16()
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsEqualTo((short)100);
        await Assert.That(results[1]).IsEqualTo((short)200);
        await Assert.That(results[2]).IsEqualTo((short)300);
    }

    /// <summary>
    /// Tests that ToInt64 converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt64_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { 100L, 200, "9999999999" };
        var results = new List<long>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt64()
            .Subscribe(l => results.Add(l));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[2]).IsEqualTo(9999999999L);
    }

    /// <summary>
    /// Tests that ToSingle converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToSingle_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { 1.5f, 2.5, "3.5" };
        var results = new List<float>();

        // Act
        using var subscription = values.ToObservable()
            .ToSingle()
            .Subscribe(f => results.Add(f));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsEqualTo(1.5f);
        await Assert.That(results[1]).IsEqualTo(2.5f);
        await Assert.That(results[2]).IsEqualTo(3.5f);
    }

    /// <summary>
    /// Tests that ToByte converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToByte_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { (byte)100, 200, "255" };
        var results = new List<byte>();

        // Act
        using var subscription = values.ToObservable()
            .ToByte()
            .Subscribe(b => results.Add(b));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0]).IsEqualTo((byte)100);
        await Assert.That(results[1]).IsEqualTo((byte)200);
        await Assert.That(results[2]).IsEqualTo((byte)255);
    }

    /// <summary>
    /// Tests that ToStringValue converts values correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToStringValue_ConvertsValuesCorrectly()
    {
        // Arrange
        var values = new object?[] { 42, 3.14, true, "hello" };
        var results = new List<string?>();

        // Act
        using var subscription = values.ToObservable()
            .Select(Convert.ToString)
            .Subscribe(s => results.Add(s));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(4);
        await Assert.That(results[0]).IsEqualTo("42");
        await Assert.That(results[3]).IsEqualTo("hello");
    }

    /// <summary>
    /// Tests SubscribeToTopics with mock client.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SubscribeToTopics_SubscribesToMultipleTopics()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = clientObservable
            .SubscribeToTopics("topic1", "topic2", "topic3")
            .Subscribe(args => receivedMessages.Add(args));

        await Task.Delay(100);

        // Verify subscriptions were made
        await Assert.That(mockClient.Subscriptions).Count().IsEqualTo(3);
    }

    /// <summary>
    /// Tests SubscribeToTopic receives messages.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SubscribeToTopic_ReceivesMessages()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = clientObservable
            .SubscribeToTopic("test/topic")
            .Subscribe(args => receivedMessages.Add(args));

        await Task.Delay(100);

        // Simulate message
        await mockClient.SimulateMessageReceived("test/topic", "test payload");
        await Task.Delay(50);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(1);
        await Assert.That(receivedMessages[0].ApplicationMessage.Topic).IsEqualTo("test/topic");
    }

    /// <summary>
    /// Test payload class.
    /// </summary>
    private sealed class TestPayload
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }
}
