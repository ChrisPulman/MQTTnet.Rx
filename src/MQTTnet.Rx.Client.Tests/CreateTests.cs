// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests for the Create class.</summary>
public class CreateTests
{
    /// <summary>Delay used to allow an asynchronous client operation to complete.</summary>
    private const int AsyncOperationDelayMilliseconds = 50;

    /// <summary>Delay used to allow client option configuration to complete.</summary>
    private const int ClientOptionsConfigurationDelayMilliseconds = 100;

    /// <summary>Port used by the test MQTT broker.</summary>
    private const int TestBrokerPort = 1883;

    /// <summary>Host used by the test MQTT broker.</summary>
    private const string TestBrokerHost = "test.mqtt.broker";

    /// <summary>Tests that MqttFactory returns a valid factory instance.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task MqttFactory_ReturnsValidFactoryAsync()
    {
        // Arrange & Act
        var factory = Create.MqttFactory;

        // Assert
        await Assert.That(factory).IsNotNull();
        await Assert.That(factory).IsTypeOf<MqttClientFactory>();
    }

    /// <summary>Tests that NewMqttFactory sets a custom factory.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task NewMqttFactory_SetsCustomFactoryAsync()
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

    /// <summary>Tests that MqttClient returns an observable that emits a client.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task MqttClient_ReturnsObservableThatEmitsClientAsync()
    {
        // Arrange & Act
        var clientObservable = Create.MqttClient();
        IMqttClient? receivedClient = null;

        using var subscription = clientObservable.Subscribe(client => receivedClient = client);

        // Give time for async operations
        await Task.Delay(AsyncOperationDelayMilliseconds);

        // Assert
        await Assert.That(receivedClient).IsNotNull();
    }

    /// <summary>Tests that MqttClient shares the same client among subscribers.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task MqttClient_SharesClientAmongSubscribersAsync()
    {
        // Arrange
        var clientObservable = Create.MqttClient();
        IMqttClient? client1 = null;
        IMqttClient? client2 = null;

        // Act
        using var subscription1 = clientObservable.Subscribe(client => client1 = client);
        using var subscription2 = clientObservable.Subscribe(client => client2 = client);

        await Task.Delay(AsyncOperationDelayMilliseconds);

        // Assert
        await Assert.That(client1).IsNotNull();
        await Assert.That(client2).IsNotNull();
        await Assert.That(client1).IsSameReferenceAs(client2);
    }

    /// <summary>Tests that MqttClientSignal returns an async observable that emits a client.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task MqttClientSignal_ReturnsObservableThatEmitsClientAsync()
    {
        // Arrange & Act
        var receivedClient = await Create.MqttClientSignal().FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(receivedClient).IsNotNull();
    }

    /// <summary>Tests that ResilientMqttClient returns an observable that emits a resilient client.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ResilientMqttClient_ReturnsObservableThatEmitsResilientClientAsync()
    {
        // Arrange & Act
        var clientObservable = Create.ResilientMqttClient();
        IResilientMqttClient? receivedClient = null;

        using var subscription = clientObservable.Subscribe(client => receivedClient = client);

        await Task.Delay(AsyncOperationDelayMilliseconds);

        // Assert
        await Assert.That(receivedClient).IsNotNull();
    }

    /// <summary>Tests that ResilientMqttClient shares the same client among subscribers.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ResilientMqttClient_SharesClientAmongSubscribersAsync()
    {
        // Arrange
        var clientObservable = Create.ResilientMqttClient();
        IResilientMqttClient? client1 = null;
        IResilientMqttClient? client2 = null;

        // Act
        using var subscription1 = clientObservable.Subscribe(client => client1 = client);
        using var subscription2 = clientObservable.Subscribe(client => client2 = client);

        await Task.Delay(AsyncOperationDelayMilliseconds);

        // Assert
        await Assert.That(client1).IsNotNull();
        await Assert.That(client2).IsNotNull();
        await Assert.That(client1).IsSameReferenceAs(client2);
    }

    /// <summary>Tests that WithClientOptions configures the client with provided options.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WithClientOptions_ConfiguresClientAsync()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = Signal.Emit<IMqttClient>(mockClient);

        // Act
        using var subscription = clientObservable
            .WithClientOptions(static options => options.WithTcpServer(TestBrokerHost, TestBrokerPort))
            .Subscribe(
                onNext: static _ => { },
                onError: static _ => { });

        await Task.Delay(ClientOptionsConfigurationDelayMilliseconds);

        // Assert
        await Assert.That(mockClient.Options).IsNotNull();
        await Assert.That(mockClient.Options?.ChannelOptions).IsNotNull();
    }

    /// <summary>Tests that WithClientOptions configures the client for asynchronous observable sources.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WithClientOptionsAsync_ConfiguresClientAsync()
    {
        // Arrange
        var mockClient = new MockMqttClient();
        var clientObservable = SignalAsync.Return<IMqttClient>(mockClient);

        // Act
        var configuredClient = await clientObservable
            .WithClientOptions(static options => options.WithTcpServer(TestBrokerHost, TestBrokerPort))
            .FirstAsync(TimeSpan.FromSeconds(1));

        // Assert
        await Assert.That(configuredClient).IsSameReferenceAs(mockClient);
        await Assert.That(mockClient.Options).IsNotNull();
        await Assert.That(mockClient.Options?.ChannelOptions).IsNotNull();
    }

    /// <summary>Tests that CreateResilientClientOptionsBuilder creates a builder.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CreateResilientClientOptionsBuilder_CreatesBuilderAsync()
    {
        // Arrange
        var factory = Create.MqttFactory;

        // Act
        var builder = factory.CreateResilientClientOptionsBuilder();

        // Assert
        await Assert.That(builder).IsNotNull();
        await Assert.That(builder).IsTypeOf<ResilientMqttClientOptionsBuilder>();
    }

    /// <summary>Tests that WithClientOptions on ResilientMqttClientOptionsBuilder works correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ResilientBuilder_WithClientOptions_WorksAsync()
    {
        // Arrange
        var builder = Create.MqttFactory.CreateResilientClientOptionsBuilder();

        // Act
        var result = builder.WithClientOptions(
            static clientBuilder => clientBuilder.WithTcpServer("test.broker", TestBrokerPort));

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(builder);
    }
}
