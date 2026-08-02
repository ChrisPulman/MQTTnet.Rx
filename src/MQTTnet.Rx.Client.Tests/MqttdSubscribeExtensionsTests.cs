// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the MqttdSubscribeExtensions class.</summary>
public sealed class MqttdSubscribeExtensionsTests
{
    /// <summary>Gets the standard delay used to allow observable notifications to complete.</summary>
    private const int ObservableNotificationDelayMilliseconds = 50;

    /// <summary>Gets the delay used to allow topic subscriptions to complete.</summary>
    private const int SubscriptionCompletionDelayMilliseconds = 100;

    /// <summary>Gets the expected count when an observable produces one result.</summary>
    private const int SingleResultCount = 1;

    /// <summary>Gets the generic topic used by payload conversion tests.</summary>
    private const string ConversionTopic = "topic";

    /// <summary>Gets the topic used by message subscription tests.</summary>
    private const string MessageTopic = "test/topic";

    /// <summary>Gets the serializer settings used to ignore null values during serialization.</summary>
    private static readonly JsonSerializerOptions IgnoreNullValuesSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Tests that ToDictionary converts a JSON payload to a dictionary.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToDictionary_ConvertsJsonToDictionaryAsync()
    {
        // Arrange
        const string json = """{"name":"sensor1","value":25.5}""";
        const string expectedName = "sensor1";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(ConversionTopic, json),
        };

        var results = new List<Dictionary<string, object?>?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToDictionary()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(SingleResultCount);
        await Assert.That(results[0]).IsNotNull();
        await Assert.That(results[0]!["name"]).IsEqualTo(expectedName);
    }

    /// <summary>Tests that ToObject deserializes JSON to a typed object.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToObject_DeserializesToTypedObjectAsync()
    {
        // Arrange
        const string expectedName = "Test";
        const int expectedValue = 42;
        var testData = new TestPayload { Name = expectedName, Value = expectedValue };
        var messages = new[]
        {
            TestDataHelpers.CreateJsonMessageReceivedArgs(ConversionTopic, testData),
        };

        var results = new List<TestPayload?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToObject(static json => JsonSerializer.Deserialize<TestPayload>(json))
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(SingleResultCount);
        await Assert.That(results[0]).IsNotNull();
        await Assert.That(results[0]!.Name).IsEqualTo(expectedName);
        await Assert.That(results[0]!.Value).IsEqualTo(expectedValue);
    }

    /// <summary>Tests that ToObject uses custom serializer settings.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToObject_UsesCustomSettingsAsync()
    {
        // Arrange
        const string expectedName = "Test";
        const string json = """{"Name":"Test","Value":100}""";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(ConversionTopic, json),
        };

        var results = new List<TestPayload?>();

        // Act
        using var subscription = messages.ToObservable()
            .ToObject(static json => JsonSerializer.Deserialize<TestPayload>(json, IgnoreNullValuesSerializerOptions))
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(SingleResultCount);
        await Assert.That(results[0]!.Name).IsEqualTo(expectedName);
    }

    /// <summary>Tests that ToBool converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToBool_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const int trueIntegerValue = 1;
        const int falseIntegerValue = 0;
        var values = new object?[] { true, false, trueIntegerValue, falseIntegerValue, "true", "false" };
        var expected = new[] { true, false, true, false, true, false };
        var results = new List<bool>();

        // Act
        using var subscription = values.ToObservable()
            .ToBool()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToInt32 converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt32_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const int firstExpectedValue = 42;
        const long secondInputValue = 100L;
        const int secondExpectedValue = 100;
        const string thirdInputValue = "200";
        const int thirdExpectedValue = 200;
        const double fourthInputValue = 3.14;
        const int fourthExpectedValue = 3;
        var values = new object?[]
        {
            firstExpectedValue,
            secondInputValue,
            thirdInputValue,
            fourthInputValue,
        };
        var expected = new[]
        {
            firstExpectedValue,
            secondExpectedValue,
            thirdExpectedValue,
            fourthExpectedValue,
        };
        var results = new List<int>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt32()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToDouble converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToDouble_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const double firstExpectedValue = 42.0;
        const double secondExpectedValue = 3.14;
        const string thirdInputValue = "2.5";
        const double thirdExpectedValue = 2.5;
        var values = new object?[] { firstExpectedValue, secondExpectedValue, thirdInputValue };
        var expected = new[] { firstExpectedValue, secondExpectedValue, thirdExpectedValue };
        var results = new List<double>();

        // Act
        using var subscription = values.ToObservable()
            .ToDouble()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToInt16 converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt16_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const short firstExpectedValue = 100;
        const int secondInputValue = 200;
        const short secondExpectedValue = 200;
        const string thirdInputValue = "300";
        const short thirdExpectedValue = 300;
        var values = new object?[] { firstExpectedValue, secondInputValue, thirdInputValue };
        var expected = new[] { firstExpectedValue, secondExpectedValue, thirdExpectedValue };
        var results = new List<short>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt16()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToInt64 converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToInt64_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const long firstInputValue = 100L;
        const int secondInputValue = 200;
        const string thirdInputValue = "9999999999";
        const long thirdExpectedValue = 9_999_999_999L;
        var values = new object?[] { firstInputValue, secondInputValue, thirdInputValue };
        var results = new List<long>();

        // Act
        using var subscription = values.ToObservable()
            .ToInt64()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(values.Length);
        await Assert.That(results[^1]).IsEqualTo(thirdExpectedValue);
    }

    /// <summary>Tests that ToSingle converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToSingle_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const float firstExpectedValue = 1.5F;
        const double secondInputValue = 2.5;
        const float secondExpectedValue = 2.5F;
        const string thirdInputValue = "3.5";
        const float thirdExpectedValue = 3.5F;
        var values = new object?[] { firstExpectedValue, secondInputValue, thirdInputValue };
        var expected = new[] { firstExpectedValue, secondExpectedValue, thirdExpectedValue };
        var results = new List<float>();

        // Act
        using var subscription = values.ToObservable()
            .ToSingle()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToByte converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToByte_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const byte firstExpectedValue = 100;
        const int secondInputValue = 200;
        const byte secondExpectedValue = 200;
        const string thirdInputValue = "255";
        const byte thirdExpectedValue = 255;
        var values = new object?[] { firstExpectedValue, secondInputValue, thirdInputValue };
        var expected = new[] { firstExpectedValue, secondExpectedValue, thirdExpectedValue };
        var results = new List<byte>();

        // Act
        using var subscription = values.ToObservable()
            .ToByte()
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(results[index]).IsEqualTo(expected[index]);
        }
    }

    /// <summary>Tests that ToStringValue converts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ToStringValue_ConvertsValuesCorrectlyAsync()
    {
        // Arrange
        const int firstInputValue = 42;
        const double secondInputValue = 3.14;
        const string fourthInputValue = "hello";
        const string expectedFirstValue = "42";
        var values = new object?[] { firstInputValue, secondInputValue, true, fourthInputValue };
        var results = new List<string?>();

        // Act
        using var subscription = values.ToObservable()
            .Select(Convert.ToString)
            .Subscribe(results.Add);

        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(values.Length);
        await Assert.That(results[0]).IsEqualTo(expectedFirstValue);
        await Assert.That(results[^1]).IsEqualTo(fourthInputValue);
    }

    /// <summary>Tests SubscribeToTopics with a mock client.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SubscribeToTopics_SubscribesToMultipleTopicsAsync()
    {
        // Arrange
        const string firstTopic = "topic1";
        const string secondTopic = "topic2";
        const string thirdTopic = "topic3";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = clientObservable
            .SubscribeToTopics(firstTopic, secondTopic, thirdTopic)
            .Subscribe(receivedMessages.Add);

        await Task.Delay(SubscriptionCompletionDelayMilliseconds);

        // Verify subscriptions were made
        await Assert.That(mockClient.Subscriptions)
            .Count()
            .IsEqualTo(new[] { firstTopic, secondTopic, thirdTopic }.Length);
    }

    /// <summary>Tests SubscribeToTopic receives messages.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SubscribeToTopic_ReceivesMessagesAsync()
    {
        // Arrange
        const string payload = "test payload";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = clientObservable
            .SubscribeToTopic(MessageTopic)
            .Subscribe(receivedMessages.Add);

        await Task.Delay(SubscriptionCompletionDelayMilliseconds);

        // Simulate message
        await mockClient.SimulateMessageReceivedAsync(MessageTopic, payload);
        await Task.Delay(ObservableNotificationDelayMilliseconds);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(SingleResultCount);
        await Assert.That(receivedMessages[0].ApplicationMessage.Topic).IsEqualTo(MessageTopic);
    }

    /// <summary>Represents the typed JSON payload used by ToObject tests.</summary>
    private sealed class TestPayload
    {
        /// <summary>Gets or sets the payload name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the payload value.</summary>
        public int Value { get; set; }
    }
}
