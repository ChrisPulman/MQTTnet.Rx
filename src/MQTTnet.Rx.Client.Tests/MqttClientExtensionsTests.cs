// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the MqttClientExtensions class.
/// </summary>
public class MqttClientExtensionsTests
{
    /// <summary>
    /// Tests that ApplicationMessageReceived returns an observable that emits when messages are received.
    /// </summary>
    [Test]
    public async Task ApplicationMessageReceived_EmitsWhenMessageReceived()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(args => receivedMessages.Add(args));

        await mockClient.SimulateMessageReceived("test/topic", "test payload");
        await Task.Delay(50);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(1);
        await Assert.That(receivedMessages[0].ApplicationMessage.Topic).IsEqualTo("test/topic");
    }

    /// <summary>
    /// Tests that ApplicationMessageReceived emits multiple messages.
    /// </summary>
    [Test]
    public async Task ApplicationMessageReceived_EmitsMultipleMessages()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(args => receivedMessages.Add(args));

        await mockClient.SimulateMessageReceived("topic1", "payload1");
        await mockClient.SimulateMessageReceived("topic2", "payload2");
        await mockClient.SimulateMessageReceived("topic3", "payload3");
        await Task.Delay(50);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(3);
    }

    /// <summary>
    /// Tests that Connected returns an observable that emits when client connects.
    /// </summary>
    [Test]
    public async Task Connected_EmitsWhenConnected()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var connectedCount = 0;

        // Act
        using var subscription = mockClient.Connected()
            .Subscribe(_ => connectedCount++);

        await mockClient.SimulateConnected();
        await Task.Delay(50);

        // Assert
        await Assert.That(connectedCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that Disconnected returns an observable that emits when client disconnects.
    /// </summary>
    [Test]
    public async Task Disconnected_EmitsWhenDisconnected()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var disconnectedCount = 0;

        // Act
        using var subscription = mockClient.Disconnected()
            .Subscribe(_ => disconnectedCount++);

        await mockClient.SimulateDisconnected();
        await Task.Delay(50);

        // Assert
        await Assert.That(disconnectedCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that Connected and Disconnected can be combined.
    /// </summary>
    [Test]
    public async Task ConnectedAndDisconnected_CanBeCombined()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var events = new List<string>();

        // Act
        using var connectedSub = mockClient.Connected()
            .Subscribe(_ => events.Add("connected"));
        using var disconnectedSub = mockClient.Disconnected()
            .Subscribe(_ => events.Add("disconnected"));

        await mockClient.SimulateConnected();
        await mockClient.SimulateDisconnected();
        await mockClient.SimulateConnected();
        await Task.Delay(50);

        // Assert
        await Assert.That(events).Count().IsEqualTo(3);
        await Assert.That(events[0]).IsEqualTo("connected");
        await Assert.That(events[1]).IsEqualTo("disconnected");
        await Assert.That(events[2]).IsEqualTo("connected");
    }

    /// <summary>
    /// Tests that ApplicationMessageReceived observable can be filtered by topic.
    /// </summary>
    [Test]
    public async Task ApplicationMessageReceived_CanBeFilteredByTopic()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var filteredMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Where(args => args.ApplicationMessage.Topic.StartsWith("sensors/"))
            .Subscribe(args => filteredMessages.Add(args));

        await mockClient.SimulateMessageReceived("sensors/temp", "25");
        await mockClient.SimulateMessageReceived("other/topic", "data");
        await mockClient.SimulateMessageReceived("sensors/humidity", "60");
        await Task.Delay(50);

        // Assert
        await Assert.That(filteredMessages).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that ApplicationMessageReceived observable can be transformed.
    /// </summary>
    [Test]
    public async Task ApplicationMessageReceived_CanBeTransformed()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var topics = new List<string>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Select(args => args.ApplicationMessage.Topic)
            .Subscribe(topic => topics.Add(topic));

        await mockClient.SimulateMessageReceived("topic1", "data");
        await mockClient.SimulateMessageReceived("topic2", "data");
        await Task.Delay(50);

        // Assert
        await Assert.That(topics).Count().IsEqualTo(2);
        await Assert.That(topics).Contains("topic1");
        await Assert.That(topics).Contains("topic2");
    }

    /// <summary>
    /// Tests that observable properly disposes handlers.
    /// </summary>
    [Test]
    public async Task Observable_DisposesHandlersOnUnsubscribe()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var receivedAfterDispose = 0;

        // Act
        var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => receivedAfterDispose++);

        await mockClient.SimulateMessageReceived("topic", "data");
        await Task.Delay(50);

        subscription.Dispose();

        await mockClient.SimulateMessageReceived("topic", "data");
        await Task.Delay(50);

        // Assert
        await Assert.That(receivedAfterDispose).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that multiple subscribers receive the same messages.
    /// </summary>
    [Test]
    public async Task ApplicationMessageReceived_MultipleSubscribersReceiveSameMessages()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var subscriber1Count = 0;
        var subscriber2Count = 0;

        // Act
        using var sub1 = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => subscriber1Count++);
        using var sub2 = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => subscriber2Count++);

        await mockClient.SimulateMessageReceived("topic", "data");
        await Task.Delay(50);

        // Assert
        await Assert.That(subscriber1Count).IsEqualTo(1);
        await Assert.That(subscriber2Count).IsEqualTo(1);
    }
}
