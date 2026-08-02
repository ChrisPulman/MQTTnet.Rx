// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Protocol;
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

/// <summary>Tests for the MqttdPublishExtensions class.</summary>
public sealed class MqttdPublishExtensionsTests
{
    /// <summary>Tests that PublishMessage publishes with string payload.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_PublishesStringPayloadAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("test/topic", "test payload"));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].Topic).IsEqualTo("test/topic");
    }

    /// <summary>Tests that PublishMessage uses correct QoS.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_UsesCorrectQoSAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        const string Payload = "payload";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject, MqttQualityOfServiceLevel.AtLeastOnce, false)
            .Subscribe();

        messageSubject.OnNext((Topic, Payload));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    /// <summary>Tests that PublishMessage uses correct retain flag.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_UsesCorrectRetainFlagAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        const string Payload = "payload";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject, MqttQualityOfServiceLevel.ExactlyOnce, retain: true)
            .Subscribe();

        messageSubject.OnNext((Topic, Payload));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].Retain).IsTrue();
    }

    /// <summary>Tests that PublishMessage publishes multiple messages.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_PublishesMultipleMessagesAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const int ExpectedThreeMessages = 3;
        const string FirstTopic = "topic1";
        const string FirstPayload = "payload1";
        const string SecondTopic = "topic2";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext((FirstTopic, FirstPayload));
        messageSubject.OnNext((SecondTopic, "payload2"));
        messageSubject.OnNext(("topic3", "payload3"));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(ExpectedThreeMessages);
    }

    /// <summary>Tests that PublishMessage with builder configures message correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_WithBuilder_ConfiguresMessageAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(
                messageSubject,
                ConfigureJsonContentType,
                MqttQualityOfServiceLevel.ExactlyOnce)
            .Subscribe();

        messageSubject.OnNext((Topic, """{"key":"value"}"""));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].ContentType).IsEqualTo("application/json");
    }

    /// <summary>Tests that PublishMessage with byte array payload works.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_WithByteArrayPayload_WorksAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, byte[] Payload)>();
        var binaryData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext((Topic, binaryData));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        var payloadSequence = mockClient.PublishedMessages[0].Payload;
        var payload = payloadSequence.ToArray();
        await Assert.That(payload.Length).IsEqualTo(binaryData.Length);
    }

    /// <summary>Tests that PublishMessage returns result for each publish.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_ReturnsResultForEachPublishAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const int ExpectedTwoMessages = 2;
        const string FirstTopic = "topic1";
        const string FirstPayload = "payload1";
        const string SecondTopic = "topic2";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();
        var results = new List<MqttClientPublishResult>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe(results.Add);

        messageSubject.OnNext((FirstTopic, FirstPayload));
        messageSubject.OnNext((SecondTopic, "payload2"));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedTwoMessages);
        await Assert.That(results[0].ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(results[1].ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
    }

    /// <summary>Tests that PublishMessage can be combined with filtering.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_CanBeCombinedWithFilteringAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const int ExpectedTwoMessages = 2;
        const string FirstTopic = "topic1";
        const string SecondTopic = "topic2";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act - only publish messages with non-empty payloads
        using var subscription = clientObservable
            .PublishMessage(
                messageSubject.Where(static message => !string.IsNullOrEmpty(message.Payload)))
            .Subscribe();

        messageSubject.OnNext((FirstTopic, "valid payload"));
        messageSubject.OnNext((SecondTopic, string.Empty));
        messageSubject.OnNext(("topic3", "another valid"));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(ExpectedTwoMessages);
    }

    /// <summary>Tests that multiple clients can publish independently.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_MultipleClients_PublishIndependentlyAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        var mockClient1 = new MockMqttClient();
        var mockClient2 = new MockMqttClient();
        var clientObservable1 = Signal.Emit<IMqttClient>(mockClient1);
        var clientObservable2 = Signal.Emit<IMqttClient>(mockClient2);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription1 = clientObservable1
            .PublishMessage(messageSubject)
            .Subscribe();
        using var subscription2 = clientObservable2
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("shared/topic", "shared payload"));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient1.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient2.PublishedMessages).Count().IsEqualTo(1);
    }

    /// <summary>Tests that PublishMessage disposes correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_DisposesCorrectlyAsync()
    {
        // Arrange
        const int SubscriptionDisposalDelayMilliseconds = 50;
        const string FirstTopic = "topic1";
        const string FirstPayload = "payload1";
        const string SecondTopic = "topic2";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext((FirstTopic, FirstPayload));
        await Task.Delay(SubscriptionDisposalDelayMilliseconds);

        subscription.Dispose();

        messageSubject.OnNext((SecondTopic, "should not be published"));
        await Task.Delay(SubscriptionDisposalDelayMilliseconds);

        // Assert - only first message should be published
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
    }

    /// <summary>Tests with default QoS values.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_DefaultQoS_IsExactlyOnceAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        const string Payload = "payload";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext((Topic, Payload));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    /// <summary>Tests with default retain value.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_DefaultRetain_IsTrueAsync()
    {
        // Arrange
        const int PublishCompletionDelayMilliseconds = 100;
        const string Topic = "topic";
        const string Payload = "payload";
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);
        using var messageSubject = new TestSignal<(string Topic, string Payload)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext((Topic, Payload));
        await Task.Delay(PublishCompletionDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.PublishedMessages[0].Retain).IsTrue();
    }

    /// <summary>Configures a message with JSON content metadata.</summary>
    /// <param name="builder">The builder to configure.</param>
    private static void ConfigureJsonContentType(MqttApplicationMessageBuilder builder)
    {
        _ = builder.WithContentType("application/json");
    }
}
