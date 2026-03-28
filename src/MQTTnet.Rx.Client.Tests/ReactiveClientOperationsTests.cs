// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Extensions.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for async observable client operation extensions.
/// </summary>
public class ReactiveClientOperationsTests
{
    /// <summary>
    /// Tests that Ping on an async observable client invokes the underlying MQTT ping operation.
    /// </summary>
    [Test]
    public async Task Ping_AsyncObservableInvokesPing()
    {
        // Arrange
        var mockClient = new MockMqttClient();

        // Act
        _ = await ObservableAsync.Return<IMqttClient>(mockClient)
            .Ping()
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(mockClient.PingCount).IsEqualTo(1);
    }

    /// <summary>
    /// Tests that PublishMany on async observables publishes every supplied message.
    /// </summary>
    [Test]
    public async Task PublishMany_AsyncObservablePublishesMessages()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var messages = new[]
        {
            new MqttApplicationMessage { Topic = "topic/1" },
            new MqttApplicationMessage { Topic = "topic/2" }
        }.ToObservableAsync();

        // Act
        _ = await ObservableAsync.Return<IMqttClient>(mockClient)
            .PublishMany(messages)
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(2);
        await Assert.That(mockClient.PublishedMessages.Select(message => message.Topic).ToArray())
            .IsEquivalentTo(new[] { "topic/1", "topic/2" });
    }

    /// <summary>
    /// Tests that WaitForConnection on an async observable client emits the client after a connection event.
    /// </summary>
    [Test]
    public async Task WaitForConnection_AsyncObservableEmitsConnectedClient()
    {
        // Arrange
        var mockClient = new MockMqttClient();

        // Act
        var connectedClientTask = ObservableAsync.Return<IMqttClient>(mockClient)
            .WaitForConnection(TimeSpan.FromSeconds(1))
            .FirstAsync(TimeSpan.FromSeconds(1));

        await mockClient.SimulateConnected();
        var connectedClient = await connectedClientTask;

        // Assert
        await Assert.That(connectedClient).IsSameReferenceAs(mockClient);
    }
}
