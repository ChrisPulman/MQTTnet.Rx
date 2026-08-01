// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Authentication;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides focused coverage for MQTT client options, subscriptions, and facade overloads.</summary>
public sealed class Wave2MqttdOptionsCoverageTests
{
    /// <summary>Specifies the expected fan-out count for duplicate subscriptions.</summary>
    private const int ExpectedDuplicateSubscriptionCount = 2;

    /// <summary>Specifies the unavailable topic level used by filtering tests.</summary>
    private const int UnavailableTopicLevel = 3;

    /// <summary>Specifies the payload used by topic and subscription tests.</summary>
    private const string Payload = "payload";

    /// <summary>Specifies the topic filter shared by duplicate-subscription tests.</summary>
    private const string SharedTopicFilter = "wave2/#";

    /// <summary>Specifies a topic that has no associated subscription hub.</summary>
    private const string MissingTopic = "wave2/missing";

    /// <summary>Specifies the placeholder broker host used by option builders.</summary>
    private const string BrokerHost = "localhost";

    /// <summary>Specifies the delay used for asynchronous subscription setup and publishing.</summary>
    private const int ProcessingDelayMilliseconds = 50;

    /// <summary>Specifies the maximum duration for a single signal operation.</summary>
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Exercises option convenience overloads and reconnect configuration overloads.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task OptionsAndReconnectOverloads_ConfigureAndEmitConnectedClientAsync()
    {
        // Arrange
        using var client = new MockMqttClient();
        var builder = new MqttClientOptionsBuilder();
        await client.SimulateConnectedAsync();

        // Act
        await Assert.That(builder.WithTlsEnabled()).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsProtocols(SslProtocols.None)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithConnectionSettings(null, null)).IsSameReferenceAs(builder);
        var defaultReconnect = await Signal.Emit<IMqttClient>(client).WithAutoReconnect().FirstAsync();
        var explicitReconnect = await Signal.Emit<IMqttClient>(client).WithAutoReconnect(TimeSpan.Zero).FirstAsync();
        var boundedReconnect = await Signal.Emit<IMqttClient>(client).WithAutoReconnect(TimeSpan.Zero, 1).FirstAsync();

        // Assert
        await Assert.That(defaultReconnect).IsSameReferenceAs(client);
        await Assert.That(explicitReconnect).IsSameReferenceAs(client);
        await Assert.That(boundedReconnect).IsSameReferenceAs(client);
    }

    /// <summary>Exercises the static creation facade and both start-required configuration paths.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task CreateFacade_ConfiguresDisconnectedAndUnstartedClientsAsync()
    {
        // Arrange
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();

        // Act
        var configuredRaw = await Create.WithClientOptions(
                Signal.Emit<IMqttClient>(rawClient),
                static builder => builder.WithClientId("wave2-raw").WithTcpServer(BrokerHost))
            .FirstAsync();
        var configuredRawAsync = await Create.WithClientOptions(
                SignalAsync.Return<IMqttClient>(rawClient),
                static builder => builder.WithClientId("wave2-raw-async").WithTcpServer(BrokerHost))
            .FirstAsync(SignalTimeout);
        var configuredResilient = await Create.WithResilientClientOptions(
                Signal.Emit<IResilientMqttClient>(resilientClient),
                static builder => builder.WithClientOptions(
                    static options => options.WithClientId("wave2-resilient").WithTcpServer(BrokerHost)))
            .FirstAsync();
        var configuredResilientAsync = await Create.WithResilientClientOptions(
                SignalAsync.Return<IResilientMqttClient>(resilientClient),
                static builder => builder.WithClientOptions(
                    static options => options.WithClientId("wave2-resilient-async").WithTcpServer(BrokerHost)))
            .FirstAsync(SignalTimeout);
        var factoryBuilder = Create.CreateResilientClientOptionsBuilder(new());

        // Assert
        await Assert.That(configuredRaw).IsSameReferenceAs(rawClient);
        await Assert.That(configuredRawAsync).IsSameReferenceAs(rawClient);
        await Assert.That(configuredResilient).IsSameReferenceAs(resilientClient);
        await Assert.That(configuredResilientAsync).IsSameReferenceAs(resilientClient);
        await Assert.That(rawClient.ConnectCount).IsEqualTo(1);
        await Assert.That(resilientClient.IsStarted).IsTrue();
        await Assert.That(factoryBuilder).IsNotNull();
    }

    /// <summary>Exercises the asynchronous resilient event bridge add and remove handlers.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ResilientEventBridges_SubscribeAndReleaseEveryProjectionAsync()
    {
        // Arrange
        using var client = new MockResilientMqttClient();

        // Act
        await using var processed = await SubscribeAsync(client.ObserveApplicationMessageProcessed());
        await using var received = await SubscribeAsync(client.ObserveApplicationMessageReceived());
        await using var skipped = await SubscribeAsync(client.ObserveApplicationMessageSkipped());
        await using var connected = await SubscribeAsync(client.ObserveConnected());
        await using var failed = await SubscribeAsync(client.ObserveConnectingFailed());
        await using var stateChanged = await SubscribeAsync(client.ObserveConnectionStateChanged());
        await using var disconnected = await SubscribeAsync(client.ObserveDisconnected());
        await using var synchronizationFailed = await SubscribeAsync(client.ObserveSynchronizingSubscriptionsFailed());
        await using var subscriptionsChanged = await SubscribeAsync(client.ObserveSubscriptionsChanged());

        // Assert
        await Assert.That(processed).IsNotNull();
        await Assert.That(received).IsNotNull();
        await Assert.That(skipped).IsNotNull();
        await Assert.That(connected).IsNotNull();
        await Assert.That(failed).IsNotNull();
        await Assert.That(stateChanged).IsNotNull();
        await Assert.That(disconnected).IsNotNull();
        await Assert.That(synchronizationFailed).IsNotNull();
        await Assert.That(subscriptionsChanged).IsNotNull();
    }

    /// <summary>Exercises the remaining raw-client synchronous and asynchronous event projections.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RawClientEventProjections_SubscribeAndReleaseEveryProjectionAsync()
    {
        // Arrange
        using var client = new MockMqttClient();

        // Act
        using var received = client.ApplicationMessageReceived().Subscribe(static _ => { });
        await using var receivedAsync = await SubscribeAsync(client.ObserveApplicationMessageReceived());
        using var connected = client.Connected().Subscribe(static _ => { });
        using var connecting = client.Connecting().Subscribe(static _ => { });
        await using var connectingAsync = await SubscribeAsync(client.ObserveConnecting());
        using var inspected = client.InspectPackage().Subscribe(static _ => { });

        // Assert
        await Assert.That(received).IsNotNull();
        await Assert.That(receivedAsync).IsNotNull();
        await Assert.That(connected).IsNotNull();
        await Assert.That(connecting).IsNotNull();
        await Assert.That(connectingAsync).IsNotNull();
        await Assert.That(inspected).IsNotNull();
    }

    /// <summary>Exercises topic filter zero, single, mismatch, and unavailable-level branches.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TopicFilters_HandleEmptySingleMismatchAndUnavailableLevelsAsync()
    {
        // Arrange
        var message = TestDataHelpers.CreateMessageReceivedArgs("wave2/one", Payload);
        var messages = new[] { message }.ToObservable();

        // Act
        var empty = await messages.WhereTopicMatchesAny().ToList();
        var single = await messages.WhereTopicMatchesAny(SharedTopicFilter).ToList();
        var mismatch = await messages.ExtractTopicValues("other/{value}").ToList();
        var unavailable = await messages.SelectTopicLevel(UnavailableTopicLevel).ToList();

        // Assert
        await Assert.That(empty).IsEmpty();
        await Assert.That(single).Count().IsEqualTo(1);
        await Assert.That(mismatch).IsEmpty();
        await Assert.That(unavailable).IsEmpty();
    }

    /// <summary>Exercises duplicate raw and resilient topic subscriptions and their release paths.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TopicSubscriptions_ShareHubsAndReleaseAfterTheLastSubscriberAsync()
    {
        // Arrange
        using var raw = new MockMqttClient();
        using var resilient = new MockResilientMqttClient();
        var rawMessages = new List<MqttApplicationMessageReceivedEventArgs>();
        var resilientMessages = new List<MqttApplicationMessageReceivedEventArgs>();
        var rawClients = Signal.Emit<IMqttClient>(raw);
        var resilientClients = Signal.Emit<IResilientMqttClient>(resilient);

        // Act
        using var rawFirst = rawClients.SubscribeToTopic(SharedTopicFilter).Subscribe(rawMessages.Add);
        var rawSecond = rawClients.SubscribeToTopic(SharedTopicFilter).Subscribe(rawMessages.Add);
        using var resilientFirst = resilientClients.SubscribeToTopic(SharedTopicFilter)
            .Subscribe(resilientMessages.Add);
        var resilientSecond = resilientClients.SubscribeToTopic(SharedTopicFilter).Subscribe(resilientMessages.Add);
        await Task.Delay(ProcessingDelayMilliseconds);
        await raw.SimulateMessageReceivedAsync("wave2/topic", Payload);
        await resilient.SimulateMessageReceivedAsync("wave2/topic", Payload);
        rawSecond.Dispose();
        resilientSecond.Dispose();

        // Assert
        await Assert.That(rawMessages).Count().IsEqualTo(ExpectedDuplicateSubscriptionCount);
        await Assert.That(resilientMessages).Count().IsEqualTo(ExpectedDuplicateSubscriptionCount);
    }

    /// <summary>Exercises the no-hub release paths for raw and resilient subscription hubs.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SubscriptionHubRelease_HandlesMissingRawAndResilientHubsAsync()
    {
        // Arrange
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var rawSubscriptionType = typeof(MqttdSubscribeExtensions)
            .GetNestedType("RawTopicSubscription", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The raw topic subscription type was not found.");
        var resilientSubscriptionType = typeof(MqttdSubscribeExtensions)
            .GetNestedType("ResilientTopicSubscription", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The resilient topic subscription type was not found.");
        var rawRelease = rawSubscriptionType.GetMethod(
                "ReleaseRawHubAsync",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The raw hub release method was not found.");
        var resilientRelease = resilientSubscriptionType.GetMethod(
                "ReleaseResilientHubAsync",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The resilient hub release method was not found.");

        // Act
        var rawTask = rawRelease.Invoke(null, [rawClient, MissingTopic]) as Task
            ?? throw new InvalidOperationException("The raw hub release method did not return a task.");
        var resilientTask = resilientRelease.Invoke(null, [resilientClient, MissingTopic]) as Task
            ?? throw new InvalidOperationException("The resilient hub release method did not return a task.");
        await rawTask;
        await resilientTask;

        // Assert
        await Assert.That(rawTask.IsCompletedSuccessfully).IsTrue();
        await Assert.That(resilientTask.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Exercises the configured byte publish overload that supplies the default retain flag.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task PublishConfiguredBytes_DefaultRetainOverload_PublishesAsync()
    {
        // Arrange
        using var client = new MockMqttClient();
        using var messages = new ReactiveUI.Primitives.Signals.Signal<(string Topic, byte[] Payload)>();

        // Act
        using var subscription = Signal.Emit<IMqttClient>(client)
            .PublishMessage(
                messages,
                static builder => builder.WithContentType("application/octet-stream"),
                MqttQualityOfServiceLevel.AtLeastOnce)
            .Subscribe();
        messages.OnNext(("wave2/bytes", [1, ExpectedDuplicateSubscriptionCount]));
        await Task.Delay(ProcessingDelayMilliseconds);

        // Assert
        await Assert.That(client.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(client.PublishedMessages[0].Retain).IsTrue();
        await Assert.That(client.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    /// <summary>Subscribes to an asynchronous observable using a no-op observer.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <returns>The asynchronous subscription.</returns>
    private static ValueTask<IAsyncDisposable> SubscribeAsync<T>(IObservableAsync<T> observable) =>
        observable.SubscribeAsync(static (_, _) => ValueTask.CompletedTask, CancellationToken.None);
}
