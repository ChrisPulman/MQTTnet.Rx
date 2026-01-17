// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the Create class.
/// </summary>
public class CreateTests
{
    /// <summary>
    /// Tests that MqttFactory returns a valid factory instance.
    /// </summary>
    [Test]
    public async Task MqttFactory_ReturnsValidFactory()
    {
        // Arrange & Act
        var factory = Create.MqttFactory;

        // Assert
        await Assert.That(factory).IsNotNull();
        await Assert.That(factory).IsTypeOf<MqttClientFactory>();
    }

    /// <summary>
    /// Tests that NewMqttFactory sets a custom factory.
    /// </summary>
    [Test]
    public async Task NewMqttFactory_SetsCustomFactory()
    {
        // Arrange
        var originalFactory = Create.MqttFactory;
        var customFactory = new MqttClientFactory();

        try
        {
            // Act
            Create.NewMqttFactory(customFactory);

            // Assert
            await Assert.That(Create.MqttFactory).IsSameReferenceAs(customFactory);
        }
        finally
        {
            // Restore
            Create.NewMqttFactory(originalFactory);
        }
    }

    /// <summary>
    /// Tests that MqttClient returns an observable that emits a client.
    /// </summary>
    [Test]
    public async Task MqttClient_ReturnsObservableThatEmitsClient()
    {
        // Arrange & Act
        var clientObservable = Create.MqttClient();
        IMqttClient? receivedClient = null;

        using var subscription = clientObservable.Subscribe(client =>
        {
            receivedClient = client;
        });

        // Give time for async operations
        await Task.Delay(50);

        // Assert
        await Assert.That(receivedClient).IsNotNull();
    }

    /// <summary>
    /// Tests that MqttClient shares the same client among subscribers.
    /// </summary>
    [Test]
    public async Task MqttClient_SharesClientAmongSubscribers()
    {
        // Arrange
        var clientObservable = Create.MqttClient();
        IMqttClient? client1 = null;
        IMqttClient? client2 = null;

        // Act
        using var subscription1 = clientObservable.Subscribe(client => client1 = client);
        using var subscription2 = clientObservable.Subscribe(client => client2 = client);

        await Task.Delay(50);

        // Assert
        await Assert.That(client1).IsNotNull();
        await Assert.That(client2).IsNotNull();
        await Assert.That(client1).IsSameReferenceAs(client2);
    }

    /// <summary>
    /// Tests that ResilientMqttClient returns an observable that emits a resilient client.
    /// </summary>
    [Test]
    public async Task ResilientMqttClient_ReturnsObservableThatEmitsResilientClient()
    {
        // Arrange & Act
        var clientObservable = Create.ResilientMqttClient();
        IResilientMqttClient? receivedClient = null;

        using var subscription = clientObservable.Subscribe(client =>
        {
            receivedClient = client;
        });

        await Task.Delay(50);

        // Assert
        await Assert.That(receivedClient).IsNotNull();
    }

    /// <summary>
    /// Tests that ResilientMqttClient shares the same client among subscribers.
    /// </summary>
    [Test]
    public async Task ResilientMqttClient_SharesClientAmongSubscribers()
    {
        // Arrange
        var clientObservable = Create.ResilientMqttClient();
        IResilientMqttClient? client1 = null;
        IResilientMqttClient? client2 = null;

        // Act
        using var subscription1 = clientObservable.Subscribe(client => client1 = client);
        using var subscription2 = clientObservable.Subscribe(client => client2 = client);

        await Task.Delay(50);

        // Assert
        await Assert.That(client1).IsNotNull();
        await Assert.That(client2).IsNotNull();
        await Assert.That(client1).IsSameReferenceAs(client2);
    }

    /// <summary>
    /// Tests that WithClientOptions configures the client with provided options.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WithClientOptions_ConfiguresClient()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Observable.Return<IMqttClient>(mockClient);

        // Act
        using var subscription = clientObservable
            .WithClientOptions(options =>
            {
                options.WithTcpServer("test.mqtt.broker", 1883);
            })
            .Subscribe(
                onNext: _ => { },
                onError: _ => { });

        await Task.Delay(100);

        // Assert
        await Assert.That(mockClient.Options).IsNotNull();
        await Assert.That(mockClient.Options?.ChannelOptions).IsNotNull();
    }

    /// <summary>
    /// Tests that CreateResilientClientOptionsBuilder creates a builder.
    /// </summary>
    [Test]
    public async Task CreateResilientClientOptionsBuilder_CreatesBuilder()
    {
        // Arrange
        var factory = Create.MqttFactory;

        // Act
        var builder = factory.CreateResilientClientOptionsBuilder();

        // Assert
        await Assert.That(builder).IsNotNull();
        await Assert.That(builder).IsTypeOf<ResilientMqttClientOptionsBuilder>();
    }

    /// <summary>
    /// Tests that WithClientOptions on ResilientMqttClientOptionsBuilder works correctly.
    /// </summary>
    [Test]
    public async Task ResilientBuilder_WithClientOptions_Works()
    {
        // Arrange
        var builder = Create.MqttFactory.CreateResilientClientOptionsBuilder();

        // Act
        var result = builder.WithClientOptions(clientBuilder =>
        {
            clientBuilder.WithTcpServer("test.broker", 1883);
        });

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(builder);
    }
}
