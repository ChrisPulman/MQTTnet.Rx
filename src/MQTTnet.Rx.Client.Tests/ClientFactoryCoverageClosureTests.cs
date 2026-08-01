// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Covers public factory and event-observation composition paths.</summary>
public sealed class ClientFactoryCoverageClosureTests
{
    /// <summary>Gets the broker host used to build client options without connecting to a network.</summary>
    private const string BrokerHost = "127.0.0.1";

    /// <summary>Verifies that every raw MQTT event projection attaches and detaches its handler.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawEventProjections_AttachAndDetachHandlersAsync()
    {
        using var client = new MockMqttClient();

        using var received = client.ApplicationMessageReceived().Subscribe(static _ => { });
        var receivedAsync = await client.ObserveApplicationMessageReceived().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var connected = client.Connected().Subscribe(static _ => { });
        var connectedAsync = await client.ObserveConnected().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var connecting = client.Connecting().Subscribe(static _ => { });
        var connectingAsync = await client.ObserveConnecting().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var disconnected = client.Disconnected().Subscribe(static _ => { });
        var disconnectedAsync = await client.ObserveDisconnected().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var inspection = client.InspectPackage().Subscribe(static _ => { });
        var inspectionAsync = await client.ObserveInspectPackage().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        await client.SimulateMessageReceivedAsync("coverage/events", "payload");
        await client.SimulateConnectedAsync();
        await client.SimulateDisconnectedAsync();
        await receivedAsync.DisposeAsync();
        await connectedAsync.DisposeAsync();
        await connectingAsync.DisposeAsync();
        await disconnectedAsync.DisposeAsync();
        await inspectionAsync.DisposeAsync();

        await Assert.That(client.IsConnected).IsFalse();
    }

    /// <summary>Verifies synchronous and asynchronous option wrappers connect unconnected clients.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ClientOptionWrappers_ConnectUnconnectedClientsAsync()
    {
        using var synchronousClient = new MockMqttClient();
        IMqttClient? synchronousResult = null;
        using var synchronousSubscription = Create.WithClientOptions(
                Signal.Emit<IMqttClient>(synchronousClient),
                static options => options.WithClientId("factory-coverage-sync").WithTcpServer(BrokerHost))
            .Subscribe(client => synchronousResult = client);

        await Task.Yield();

        using var asynchronousClient = new MockMqttClient();
        var asynchronousResult = await Create.WithClientOptions(
                SignalAsync.Return<IMqttClient>(asynchronousClient),
                static options => options.WithClientId("factory-coverage-async").WithTcpServer(BrokerHost))
            .FirstAsync(TimeSpan.FromSeconds(1));

        await Assert.That(synchronousResult).IsSameReferenceAs(synchronousClient);
        await Assert.That(synchronousClient.IsConnected).IsTrue();
        await Assert.That(asynchronousResult).IsSameReferenceAs(asynchronousClient);
        await Assert.That(asynchronousClient.IsConnected).IsTrue();
    }

    /// <summary>
    /// Verifies that resilient client option wrappers start an unstarted client for both observable
    /// variants.
    /// </summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientOptionWrappers_StartUnstartedClientsAsync()
    {
        using var synchronousClient = new MockResilientMqttClient();
        IResilientMqttClient? synchronousResult = null;
        using var synchronousSubscription = Create.WithResilientClientOptions(
                Signal.Emit<IResilientMqttClient>(synchronousClient),
                static options => options.WithClientOptions(
                    static client => client.WithClientId("resilient-factory-sync").WithTcpServer(BrokerHost)))
            .Subscribe(client => synchronousResult = client);

        await Task.Yield();

        using var asynchronousClient = new MockResilientMqttClient();
        var asynchronousResult = await Create.WithResilientClientOptions(
                SignalAsync.Return<IResilientMqttClient>(asynchronousClient),
                static options => options.WithClientOptions(
                    static client => client.WithClientId("resilient-factory-async").WithTcpServer(BrokerHost)))
            .FirstAsync(TimeSpan.FromSeconds(1));

        await Assert.That(synchronousResult).IsSameReferenceAs(synchronousClient);
        await Assert.That(synchronousClient.IsStarted).IsTrue();
        await Assert.That(asynchronousResult).IsSameReferenceAs(asynchronousClient);
        await Assert.That(asynchronousClient.IsStarted).IsTrue();
    }

    /// <summary>Verifies that both factory signal variants produce a usable client instance.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task FactorySignals_ProduceClientsAsync()
    {
        var mqttClient = await Create.MqttClientSignal().FirstAsync(TimeSpan.FromSeconds(1));
        var resilientClient = await Create.ResilientMqttClientSignal().FirstAsync(TimeSpan.FromSeconds(1));

        await Assert.That(mqttClient).IsNotNull();
        await Assert.That(resilientClient).IsNotNull();
    }

    /// <summary>Verifies factory lifetime subscriptions retain the client until the last observer leaves.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task FactorySubscriptions_ReleaseOnlyAfterTheLastObserverAsync()
    {
        var raw = Create.MqttClient();
        var resilient = Create.ResilientMqttClient();
        IDisposable rawFirst = System.ObservableExtensions.Subscribe(raw, static _ => { });
        IDisposable rawSecond = System.ObservableExtensions.Subscribe(raw, static _ => { });
        IDisposable resilientFirst = resilient.Subscribe(static _ => { });
        IDisposable resilientSecond = resilient.Subscribe(static _ => { });

        rawFirst.Dispose();
        resilientFirst.Dispose();
        rawSecond.Dispose();
        resilientSecond.Dispose();

        var rawSignalFirst = await Create.MqttClientSignal().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        var rawSignalSecond = await Create.MqttClientSignal().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        var resilientSignalFirst = await Create.ResilientMqttClientSignal().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        var resilientSignalSecond = await Create.ResilientMqttClientSignal().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        await rawSignalFirst.DisposeAsync();
        await resilientSignalFirst.DisposeAsync();
        await rawSignalSecond.DisposeAsync();
        await resilientSignalSecond.DisposeAsync();

        await Assert.That(raw).IsNotNull();
    }

    /// <summary>Verifies synchronous readiness waits for connection and exposes resilient event projections.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientReadinessAndEventProjections_AreComposedAsync()
    {
        using var client = new MockResilientMqttClient();
        var ready = Signal.Emit<IResilientMqttClient>(client).WhenReady().FirstAsync(TimeSpan.FromSeconds(1));
        var asynchronousReady = SignalAsync.Return<IResilientMqttClient>(client)
            .WhenReady()
            .FirstAsync(TimeSpan.FromSeconds(1));

        _ = client.ObserveApplicationMessageProcessed();
        _ = client.ObserveApplicationMessageReceived();
        _ = client.ObserveApplicationMessageSkipped();
        _ = client.ObserveConnected();
        _ = client.ObserveConnectingFailed();
        _ = client.ObserveConnectionStateChanged();
        _ = client.ObserveDisconnected();
        _ = client.ObserveSynchronizingSubscriptionsFailed();
        var subscriptions = await client.ObserveSubscriptionsChanged().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        await client.SimulateConnectedAsync();
        await subscriptions.DisposeAsync();

        await Assert.That(await ready).IsSameReferenceAs(client);
        await Assert.That(await asynchronousReady).IsSameReferenceAs(client);
    }

    /// <summary>Verifies public factory builder wrappers retain fluent configuration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task FactoryBuilderWrappers_ReturnConfiguredBuilderAsync()
    {
        Create.NewMqttFactory(Create.MqttFactory);
        var builder = Create.CreateResilientClientOptionsBuilder(Create.MqttFactory);
        var configured = Create.WithClientOptions(
            builder,
            static options => options.WithClientId("factory-builder").WithTcpServer(BrokerHost));

        var clientOptions = configured.Build().ClientOptions;

        await Assert.That(configured).IsSameReferenceAs(builder);
        await Assert.That(clientOptions).IsNotNull();
        ArgumentNullException.ThrowIfNull(clientOptions);
        await Assert.That(clientOptions.ClientId).IsEqualTo("factory-builder");
    }
}
