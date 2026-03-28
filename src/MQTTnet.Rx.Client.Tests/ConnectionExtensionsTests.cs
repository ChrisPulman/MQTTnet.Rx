// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Extensions.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for resilient client async observable connection extensions.
/// </summary>
public class ConnectionExtensionsTests
{
    /// <summary>
    /// Tests that ObserveApplicationMessageProcessedAsync emits processed message events.
    /// </summary>
    [Test]
    public async Task ObserveApplicationMessageProcessedAsync_EmitsProcessedEvent()
    {
        // Arrange
        var mockClient = new MockResilientMqttClient();

        // Act
        var processedMessageTask = mockClient.ObserveApplicationMessageProcessedAsync()
            .FirstAsync(TimeSpan.FromSeconds(1));

        await mockClient.SimulateApplicationMessageProcessedAsync();
        var processedMessage = await processedMessageTask;

        // Assert
        await Assert.That(processedMessage.ApplicationMessage.ApplicationMessage?.Topic).IsEqualTo("processed/topic");
    }

    /// <summary>
    /// Tests that WhenReady emits the resilient client after a connection event.
    /// </summary>
    [Test]
    public async Task WhenReady_AsyncObservableEmitsConnectedClient()
    {
        // Arrange
        var mockClient = new MockResilientMqttClient();

        // Act
        var readyClientTask = ObservableAsync.Return<IResilientMqttClient>(mockClient)
            .WhenReady()
            .FirstAsync(TimeSpan.FromSeconds(1));

        await mockClient.SimulateConnectedAsync();
        var readyClient = await readyClientTask;

        // Assert
        await Assert.That(readyClient).IsSameReferenceAs(mockClient);
    }
}
