// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
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

/// <summary>Tests asynchronous observable client operation extensions.</summary>
public class ReactiveClientOperationsTests
{
    /// <summary>The expected number of published messages.</summary>
    private const int ExpectedPublishedMessageCount = 2;

    /// <summary>Tests that the compatibility facade forwards static calls to the extension implementation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task Ping_CompatibilityFacadeInvokesPingAsync()
    {
        using var mockClient = new MockMqttClient();

        _ = await ReactiveClientOperations
            .Ping(Signal.Emit<IMqttClient>(mockClient))
            .FirstAsync();

        await Assert.That(mockClient.PingCount).IsEqualTo(1);
    }

    /// <summary>Tests that Ping on an async observable client invokes the underlying MQTT ping operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task Ping_AsyncObservableInvokesPingAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();

        // Act
        _ = await SignalAsync.Return<IMqttClient>(mockClient)
            .Ping()
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(mockClient.PingCount).IsEqualTo(1);
    }

    /// <summary>Tests that PublishMany on async observables publishes every supplied message.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PublishMany_AsyncObservablePublishesMessagesAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();
        var messages = TestObservableBridge.ToSignal(
            new[]
            {
                new MqttApplicationMessage { Topic = "topic/1" },
                new MqttApplicationMessage { Topic = "topic/2" },
            }.ToObservable());

        // Act
        _ = await SignalAsync.Return<IMqttClient>(mockClient)
            .PublishMany(messages)
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(mockClient.PublishedMessages).Count().IsEqualTo(ExpectedPublishedMessageCount);
        await Assert.That(mockClient.PublishedMessages[0].Topic).IsEqualTo("topic/1");
        await Assert.That(mockClient.PublishedMessages[1].Topic).IsEqualTo("topic/2");
    }

    /// <summary>Tests that WaitForConnection emits the client after a connection event.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WaitForConnection_AsyncObservableEmitsConnectedClientAsync()
    {
        // Arrange
        using var mockClient = new MockMqttClient();

        // Act
        await mockClient.SimulateConnectedAsync();
        var connectedClient = await SignalAsync.Return<IMqttClient>(mockClient)
            .WaitForConnection(TimeSpan.FromSeconds(1))
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(connectedClient).IsSameReferenceAs(mockClient);
    }
}
