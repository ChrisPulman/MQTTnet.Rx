// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests resilient client asynchronous observable connection extensions.</summary>
public class ConnectionExtensionsTests
{
    /// <summary>Tests that ObserveApplicationMessageProcessed emits processed message events.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObserveApplicationMessageProcessed_EmitsProcessedEventAsync()
    {
        // Arrange
        using var mockClient = new MockResilientMqttClient();

        // Act
        var processedMessageTask = mockClient.ObserveApplicationMessageProcessed()
            .FirstAsync(TimeSpan.FromSeconds(1));

        await mockClient.SimulateApplicationMessageProcessedAsync();
        var processedMessage = await processedMessageTask;

        // Assert
        await Assert.That(processedMessage.ApplicationMessage.ApplicationMessage?.Topic).IsEqualTo("processed/topic");
    }

    /// <summary>Tests that WhenReady emits the resilient client after a connection event.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WhenReady_AsyncObservableEmitsConnectedClientAsync()
    {
        // Arrange
        using var mockClient = new MockResilientMqttClient();

        // Act
        var readyClientTask = SignalAsync.Return<IResilientMqttClient>(mockClient)
            .WhenReady()
            .FirstAsync(TimeSpan.FromSeconds(1));

        await mockClient.SimulateConnectedAsync();
        var readyClient = await readyClientTask;

        // Assert
        await Assert.That(readyClient).IsSameReferenceAs(mockClient);
    }
}
