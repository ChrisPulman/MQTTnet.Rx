// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the MqttClientExtensions class.</summary>
public sealed class MqttClientExtensionsTests
{
    /// <summary>The number of events expected from a single operation.</summary>
    private const int ExpectedSingleEventCount = 1;

    /// <summary>The number of messages expected after topic filtering.</summary>
    private const int ExpectedFilteredMessageCount = 2;

    /// <summary>The number of events expected from the combined connection sequence.</summary>
    private const int ExpectedCombinedEventCount = 3;

    /// <summary>The index of the first received item.</summary>
    private const int FirstItemIndex = 0;

    /// <summary>The index of the second received item.</summary>
    private const int SecondItemIndex = 1;

    /// <summary>The index of the third received item.</summary>
    private const int ThirdItemIndex = 2;

    /// <summary>The delay used to allow observable handlers to process test events.</summary>
    private const int ObservableProcessingDelayMilliseconds = 50;

    /// <summary>The timeout for receiving the first asynchronous message.</summary>
    private const int FirstMessageTimeoutSeconds = 1;

    /// <summary>The event name recorded when the client connects.</summary>
    private const string ConnectedEventName = "connected";

    /// <summary>The event name recorded when the client disconnects.</summary>
    private const string DisconnectedEventName = "disconnected";

    /// <summary>The payload used by generic message tests.</summary>
    private const string DataPayload = "data";

    /// <summary>The first topic used by multi-message tests.</summary>
    private const string FirstTopic = "topic1";

    /// <summary>The second topic used by multi-message tests.</summary>
    private const string SecondTopic = "topic2";

    /// <summary>The third topic used by multi-message tests.</summary>
    private const string ThirdTopic = "topic3";

    /// <summary>The prefix used to identify sensor topics.</summary>
    private const string SensorsTopicPrefix = "sensors/";

    /// <summary>The topic used by single-message tests.</summary>
    private const string TestTopic = "test/topic";

    /// <summary>The payload used by single-message tests.</summary>
    private const string TestPayload = "test payload";

    /// <summary>The generic topic used by disposal tests.</summary>
    private const string Topic = "topic";

    /// <summary>Tests that ApplicationMessageReceived emits when messages are received.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ApplicationMessageReceived_EmitsWhenMessageReceivedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(receivedMessages.Add);

        await mockClient.SimulateMessageReceivedAsync(TestTopic, TestPayload);
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(ExpectedSingleEventCount);
        await Assert.That(receivedMessages[FirstItemIndex].ApplicationMessage.Topic).IsEqualTo(TestTopic);
    }

    /// <summary>Tests that ApplicationMessageReceived emits multiple messages.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ApplicationMessageReceived_EmitsMultipleMessagesAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var receivedMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(receivedMessages.Add);

        await mockClient.SimulateMessageReceivedAsync(FirstTopic, "payload1");
        await mockClient.SimulateMessageReceivedAsync(SecondTopic, "payload2");
        await mockClient.SimulateMessageReceivedAsync(ThirdTopic, "payload3");
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(receivedMessages).Count().IsEqualTo(ExpectedCombinedEventCount);
    }

    /// <summary>Tests that Connected returns an observable that emits when client connects.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Connected_EmitsWhenConnectedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var connectedCount = 0;

        // Act
        using var subscription = mockClient.Connected()
            .Subscribe(_ => connectedCount++);

        await mockClient.SimulateConnectedAsync();
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(connectedCount).IsEqualTo(ExpectedSingleEventCount);
    }

    /// <summary>Tests that Disconnected returns an observable that emits when client disconnects.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Disconnected_EmitsWhenDisconnectedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var disconnectedCount = 0;

        // Act
        using var subscription = mockClient.Disconnected()
            .Subscribe(_ => disconnectedCount++);

        await mockClient.SimulateDisconnectedAsync();
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(disconnectedCount).IsEqualTo(ExpectedSingleEventCount);
    }

    /// <summary>Tests that Connected and Disconnected can be combined.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ConnectedAndDisconnected_CanBeCombinedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var events = new List<string>();

        // Act
        using var connectedSub = mockClient.Connected()
            .Subscribe(_ => events.Add(ConnectedEventName));
        using var disconnectedSub = mockClient.Disconnected()
            .Subscribe(_ => events.Add(DisconnectedEventName));

        await mockClient.SimulateConnectedAsync();
        await mockClient.SimulateDisconnectedAsync();
        await mockClient.SimulateConnectedAsync();
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(events).Count().IsEqualTo(ExpectedCombinedEventCount);
        await Assert.That(events[FirstItemIndex]).IsEqualTo(ConnectedEventName);
        await Assert.That(events[SecondItemIndex]).IsEqualTo(DisconnectedEventName);
        await Assert.That(events[ThirdItemIndex]).IsEqualTo(ConnectedEventName);
    }

    /// <summary>Tests that ApplicationMessageReceived observable can be filtered by topic.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ApplicationMessageReceived_CanBeFilteredByTopicAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var filteredMessages = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Where(static args => args.ApplicationMessage.Topic.StartsWith(
                SensorsTopicPrefix,
                StringComparison.Ordinal))
            .Subscribe(filteredMessages.Add);

        await mockClient.SimulateMessageReceivedAsync("sensors/temp", "25");
        await mockClient.SimulateMessageReceivedAsync("other/topic", DataPayload);
        await mockClient.SimulateMessageReceivedAsync("sensors/humidity", "60");
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(filteredMessages).Count().IsEqualTo(ExpectedFilteredMessageCount);
    }

    /// <summary>Tests that ApplicationMessageReceived observable can be transformed.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ApplicationMessageReceived_CanBeTransformedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var topics = new List<string>();

        // Act
        using var subscription = mockClient.ApplicationMessageReceived()
            .Select(static args => args.ApplicationMessage.Topic)
            .Subscribe(topics.Add);

        await mockClient.SimulateMessageReceivedAsync(FirstTopic, DataPayload);
        await mockClient.SimulateMessageReceivedAsync(SecondTopic, DataPayload);
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(topics).Count().IsEqualTo(ExpectedFilteredMessageCount);
        await Assert.That(topics).Contains(FirstTopic);
        await Assert.That(topics).Contains(SecondTopic);
    }

    /// <summary>Tests that observable properly disposes handlers.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Observable_DisposesHandlersOnUnsubscribeAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var receivedAfterDispose = 0;

        // Act
        var subscription = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => receivedAfterDispose++);

        await mockClient.SimulateMessageReceivedAsync(Topic, DataPayload);
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        subscription.Dispose();

        await mockClient.SimulateMessageReceivedAsync(Topic, DataPayload);
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(receivedAfterDispose).IsEqualTo(ExpectedSingleEventCount);
    }

    /// <summary>Tests that ObserveApplicationMessageReceived emits when messages are received.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ObserveApplicationMessageReceived_EmitsWhenMessageReceivedAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();

        // Act
        var receivedTask = mockClient.ObserveApplicationMessageReceived()
            .FirstAsync(TimeSpan.FromSeconds(FirstMessageTimeoutSeconds));
        await mockClient.SimulateMessageReceivedAsync(TestTopic, TestPayload);
        var receivedMessage = await receivedTask;

        // Assert
        await Assert.That(receivedMessage.ApplicationMessage.Topic).IsEqualTo(TestTopic);
    }

    /// <summary>Tests that ObserveConnected stops emitting after the async subscription is disposed.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ObserveConnectedEvent_DisposesHandlersOnUnsubscribeAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var connectedCount = 0;

        // Act
        var subscription = await mockClient.ObserveConnected().SubscribeAsync(
            (args, _) =>
            {
                connectedCount++;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        await mockClient.SimulateConnectedAsync();
        await subscription.DisposeAsync();
        await mockClient.SimulateConnectedAsync();
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(connectedCount).IsEqualTo(ExpectedSingleEventCount);
    }

    /// <summary>Tests that multiple subscribers receive the same messages.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ApplicationMessageReceived_MultipleSubscribersReceiveSameMessagesAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var subscriber1Count = 0;
        var subscriber2Count = 0;

        // Act
        using var sub1 = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => subscriber1Count++);
        using var sub2 = mockClient.ApplicationMessageReceived()
            .Subscribe(_ => subscriber2Count++);

        await mockClient.SimulateMessageReceivedAsync(Topic, DataPayload);
        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(subscriber1Count).IsEqualTo(ExpectedSingleEventCount);
        await Assert.That(subscriber2Count).IsEqualTo(ExpectedSingleEventCount);
    }
}
