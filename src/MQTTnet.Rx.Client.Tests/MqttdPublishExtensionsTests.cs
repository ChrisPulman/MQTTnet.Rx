// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the MqttdPublishExtensions class.
/// </summary>
public class MqttdPublishExtensionsTests
{
    /// <summary>
    /// Tests that PublishMessage publishes with string payload.
    /// </summary>
    [Test]
    public async Task PublishMessage_PublishesStringPayload()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();
        var results = new List<MqttClientPublishResult>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe(result => results.Add(result));

        messageSubject.OnNext(("test/topic", "test payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].Topic).IsEqualTo("test/topic");
    }

    /// <summary>
    /// Tests that PublishMessage uses correct QoS.
    /// </summary>
    [Test]
    public async Task PublishMessage_UsesCorrectQoS()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject, MqttQualityOfServiceLevel.AtLeastOnce, false)
            .Subscribe();

        messageSubject.OnNext(("topic", "payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    /// <summary>
    /// Tests that PublishMessage uses correct retain flag.
    /// </summary>
    [Test]
    public async Task PublishMessage_UsesCorrectRetainFlag()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject, retain: true)
            .Subscribe();

        messageSubject.OnNext(("topic", "payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].Retain).IsTrue();
    }

    /// <summary>
    /// Tests that PublishMessage publishes multiple messages.
    /// </summary>
    [Test]
    public async Task PublishMessage_PublishesMultipleMessages()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("topic1", "payload1"));
        messageSubject.OnNext(("topic2", "payload2"));
        messageSubject.OnNext(("topic3", "payload3"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(3);
    }

    /// <summary>
    /// Tests that PublishMessage with builder configures message correctly.
    /// </summary>
    [Test]
    public async Task PublishMessage_WithBuilder_ConfiguresMessage()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(
                messageSubject,
                builder => builder.WithContentType("application/json"),
                MqttQualityOfServiceLevel.ExactlyOnce)
            .Subscribe();

        messageSubject.OnNext(("topic", """{"key":"value"}"""));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient.PublishedMessages[0].ContentType).IsEqualTo("application/json");
    }

    /// <summary>
    /// Tests that PublishMessage with byte array payload works.
    /// </summary>
    [Test]
    public async Task PublishMessage_WithByteArrayPayload_Works()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, byte[] payLoad)>();
        var binaryData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("topic", binaryData));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
        var payloadSequence = mockClient.PublishedMessages[0].Payload;
        var payload = payloadSequence.ToArray();
        await Assert.That(payload.Length).IsEqualTo(binaryData.Length);
    }

    /// <summary>
    /// Tests that PublishMessage returns result for each publish.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PublishMessage_ReturnsResultForEachPublish()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();
        var results = new List<MqttClientPublishResult>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe(result => results.Add(result));

        messageSubject.OnNext(("topic1", "payload1"));
        messageSubject.OnNext(("topic2", "payload2"));
        await Task.Delay(100);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(results[1].ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
    }

    /// <summary>
    /// Tests that PublishMessage can be combined with filtering.
    /// </summary>
    [Test]
    public async Task PublishMessage_CanBeCombinedWithFiltering()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act - only publish messages with non-empty payloads
        using var subscription = clientObservable
            .PublishMessage(
                messageSubject.Where(m => !string.IsNullOrEmpty(m.payLoad)))
            .Subscribe();

        messageSubject.OnNext(("topic1", "valid payload"));
        messageSubject.OnNext(("topic2", ""));
        messageSubject.OnNext(("topic3", "another valid"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that multiple clients can publish independently.
    /// </summary>
    [Test]
    public async Task PublishMessage_MultipleClients_PublishIndependently()
    {
        // Arrange
        var mockClient1 = new MockMqttClient();
        var mockClient2 = new MockMqttClient();
        var clientObservable1 = Observable.Return<IMqttClient>(mockClient1);
        var clientObservable2 = Observable.Return<IMqttClient>(mockClient2);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription1 = clientObservable1
            .PublishMessage(messageSubject)
            .Subscribe();
        using var subscription2 = clientObservable2
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("shared/topic", "shared payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient1.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(mockClient2.PublishedMessages).Count().IsEqualTo(1);
    }

    /// <summary>
    /// Tests that PublishMessage disposes correctly.
    /// </summary>
    [Test]
    public async Task PublishMessage_DisposesCorrectly()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("topic1", "payload1"));
        await Task.Delay(50);

        subscription.Dispose();

        messageSubject.OnNext(("topic2", "should not be published"));
        await Task.Delay(50);

        // Assert - only first message should be published
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(1);
    }

    /// <summary>
    /// Tests with default QoS values.
    /// </summary>
    [Test]
    public async Task PublishMessage_DefaultQoS_IsExactlyOnce()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("topic", "payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    /// <summary>
    /// Tests with default retain value.
    /// </summary>
    [Test]
    public async Task PublishMessage_DefaultRetain_IsTrue()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);
        var messageSubject = new Subject<(string topic, string payLoad)>();

        // Act
        using var subscription = clientObservable
            .PublishMessage(messageSubject)
            .Subscribe();

        messageSubject.OnNext(("topic", "payload"));
        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.PublishedMessages[0].Retain).IsTrue();
    }
}
