// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using MQTTnet.Adapter;
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using MQTTnet.Rx.Server.Reactive;
#else
using MQTTnet.Rx.Server;
#endif
using MQTTnet.Server;
using MQTTnet.Server.EnhancedAuthentication;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ServerCreate = MQTTnet.Rx.Server.Reactive.Create;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using ServerCreate = MQTTnet.Rx.Server.Create;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the complete direct MQTT broker wrapper surface.</summary>
[NotInParallel]
public sealed class ServerCompleteSurfaceTests
{
    /// <summary>The enhanced-authentication method used by the adapter test.</summary>
    private const string AuthenticationMethodValue = "test-authentication";

    /// <summary>The value used for server and session item tests.</summary>
    private const string SessionItemValue = "value";

    /// <summary>The expected lifecycle state count after six start-stop cycles.</summary>
    private const int ExpectedLifecycleStateCount = 13;

    /// <summary>The number of connected clients hosted by the live fixture.</summary>
    private const int LiveClientCount = 2;

    /// <summary>The maximum time allowed for a cold operation to produce its result.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies direct, sequence, builder, and property configuration surfaces.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerConfigurationAndProperties_ExposeCompleteFluentSurfaceAsync()
    {
        using var server = CreateServer();
        var configured = false;
        var sameServer = server
            .WithAcceptNewConnections(false)
            .WithServerSessionItem("one", SessionItemValue)
            .WithoutServerSessionItem("missing")
            .ConfigureServer(value =>
            {
                configured = true;
                value.AcceptNewConnections = true;
            });
        _ = server.Properties();
        _ = server.WithoutServerSessionItem("one").WithServerSessionItem("two", SessionItemValue);
        _ = server.ClearServerSessionItems();
        var syncConfigured = await Signal.Emit(server)
            .ConfigureServer(static value => value.AcceptNewConnections = false)
            .FirstAsync(OperationTimeout);
        var asyncConfigured = await SignalAsync.Return(server)
            .ConfigureServer(static value => value.AcceptNewConnections = true)
            .FirstAsync(OperationTimeout);

        var immediate = server.Properties();
        var property = await server.Property(static value => value.IsStarted).FirstAsync(OperationTimeout);
        var observedProperty = await server.ObserveProperty(static value => value.AcceptNewConnections)
            .FirstAsync(OperationTimeout);
        var snapshot = await server.PropertySnapshots().FirstAsync(OperationTimeout);
        var observedSnapshot = await server.ObservePropertySnapshots().FirstAsync(OperationTimeout);
        var accepts = await server.AcceptNewConnectionsValue().FirstAsync(OperationTimeout);
        var observedAccepts = await server.ObserveAcceptNewConnections().FirstAsync(OperationTimeout);
        var items = await server.ServerSessionItemsSnapshot().FirstAsync(OperationTimeout);
        var observedItems = await server.ObserveServerSessionItemsSnapshot().FirstAsync(OperationTimeout);

        await Assert.That(sameServer).IsSameReferenceAs(server);
        await Assert.That(syncConfigured).IsSameReferenceAs(server);
        await Assert.That(asyncConfigured).IsSameReferenceAs(server);
        await Assert.That(configured).IsTrue();
        await Assert.That(immediate.IsStarted).IsFalse();
        await Assert.That(property).IsFalse();
        await Assert.That(observedProperty).IsTrue();
        await Assert.That(snapshot.AcceptNewConnections).IsEqualTo(immediate.AcceptNewConnections);
        await Assert.That(observedSnapshot.IsStarted).IsEqualTo(immediate.IsStarted);
        await Assert.That(snapshot.ServerSessionItems).IsEmpty();
        await Assert.That(observedSnapshot.ServerSessionItems).IsEmpty();
        await Assert.That(accepts).IsTrue();
        await Assert.That(observedAccepts).IsTrue();
        await Assert.That(items).IsEmpty();
        await Assert.That(observedItems).IsEmpty();
    }

    /// <summary>Verifies every server options builder exposes its underlying option object fluently.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerOptionBuilders_ExposeUnderlyingConfigurationAsync()
    {
        var factory = new MqttServerFactory();
        var disconnectConfigured = false;
        var serverConfigured = false;
        var stopConfigured = false;
        var disconnectBuilder = new MqttServerClientDisconnectOptionsBuilder();
        var serverBuilder = factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint();
        var stopBuilder = new MqttServerStopOptionsBuilder();

        var sameDisconnectBuilder = disconnectBuilder.ConfigureOptions(options =>
        {
            disconnectConfigured = true;
            GC.KeepAlive(options);
        });
        var sameServerBuilder = serverBuilder.ConfigureOptions(options =>
        {
            serverConfigured = true;
            GC.KeepAlive(options);
        });
        var sameStopBuilder = stopBuilder.ConfigureOptions(options =>
        {
            stopConfigured = true;
            GC.KeepAlive(options);
        });

        await Assert.That(sameDisconnectBuilder).IsSameReferenceAs(disconnectBuilder);
        await Assert.That(sameServerBuilder).IsSameReferenceAs(serverBuilder);
        await Assert.That(sameStopBuilder).IsSameReferenceAs(stopBuilder);
        await Assert.That(disconnectConfigured).IsTrue();
        await Assert.That(serverConfigured).IsTrue();
        await Assert.That(stopConfigured).IsTrue();
    }

    /// <summary>Verifies lifecycle state and every paired start-stop operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerLifecycle_EmitsStateForEveryOperationFormAsync()
    {
        using var server = CreateServer();
        var synchronousStates = new List<bool>();
        var asynchronousStates = new List<bool>();
        using var synchronousSubscription = server.IsStartedChanges().Subscribe(synchronousStates.Add);
        await using var asynchronousSubscription = await server.ObserveIsStartedChanges().SubscribeAsync(
            (value, cancellationToken) =>
            {
                GC.KeepAlive(cancellationToken);
                asynchronousStates.Add(value);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        await ExerciseLifecycleOperationsAsync(server);
        synchronousSubscription.Dispose();
        synchronousSubscription.Dispose();
        await asynchronousSubscription.DisposeAsync();
        await asynchronousSubscription.DisposeAsync();

        await Assert.That(synchronousStates).Count().IsEqualTo(ExpectedLifecycleStateCount);
        await Assert.That(asynchronousStates).Count().IsEqualTo(ExpectedLifecycleStateCount);
        await Assert.That(synchronousStates[0]).IsFalse();
        await Assert.That(asynchronousStates[0]).IsFalse();
    }

    /// <summary>Verifies every broker data and client-control operation in both reactive forms.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LiveServerOperations_ExposeCompletePairedSurfaceAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        var server = broker.Server;
        var firstRetained = CreateMessage("server/retained-one", true);
        var secondRetained = CreateMessage("server/retained-two", true);
        var filters = new List<MqttTopicFilter> { new MqttTopicFilterBuilder().WithTopic("server/#").Build() };
        var topics = new List<string> { "server/#" };

        _ = await server.UpdateRetainedMessage(firstRetained).FirstAsync(OperationTimeout);
        _ = await server.ObserveUpdateRetainedMessage(secondRetained).FirstAsync(OperationTimeout);
        var retained = await server.GetRetainedMessage(firstRetained.Topic).FirstAsync(OperationTimeout);
        var observedRetained = await server.ObserveRetainedMessage(secondRetained.Topic).FirstAsync(OperationTimeout);
        var retainedMessages = await server.GetRetainedMessages().FirstAsync(OperationTimeout);
        var observedRetainedMessages = await server.ObserveRetainedMessages().FirstAsync(OperationTimeout);
        var clients = await server.GetClients().FirstAsync(OperationTimeout);
        var observedClients = await server.ObserveClients().FirstAsync(OperationTimeout);
        var session = await server.GetSession(broker.BridgeClientId).FirstAsync(OperationTimeout);
        var observedSession = await server.ObserveSession(broker.ProbeClientId).FirstAsync(OperationTimeout);
        var sessions = await server.GetSessions().FirstAsync(OperationTimeout);
        var observedSessions = await server.ObserveSessions().FirstAsync(OperationTimeout);
        _ = await server.SubscribeClient(broker.BridgeClientId, filters).FirstAsync(OperationTimeout);
        _ = await server.SubscribeClient(
                broker.ProbeClientId,
                static builder => builder.WithTopic("server/configured"))
            .FirstAsync(OperationTimeout);
        _ = await server.ObserveSubscribeClient(broker.BridgeClientId, filters).FirstAsync(OperationTimeout);
        _ = await server.ObserveSubscribeClient(
                broker.ProbeClientId,
                static builder => builder.WithTopic("server/observed"))
            .FirstAsync(OperationTimeout);
        _ = await server.UnsubscribeClient(broker.BridgeClientId, topics).FirstAsync(OperationTimeout);
        _ = await server.ObserveUnsubscribeClient(broker.ProbeClientId, topics).FirstAsync(OperationTimeout);

        await Assert.That(retained.Topic).IsEqualTo(firstRetained.Topic);
        await Assert.That(observedRetained.Topic).IsEqualTo(secondRetained.Topic);
        await Assert.That(retainedMessages).Count().IsEqualTo(LiveClientCount);
        await Assert.That(observedRetainedMessages).Count().IsEqualTo(LiveClientCount);
        await Assert.That(clients).Count().IsEqualTo(LiveClientCount);
        await Assert.That(observedClients).Count().IsEqualTo(LiveClientCount);
        await Assert.That(session.Id).IsEqualTo(broker.BridgeClientId);
        await Assert.That(observedSession.Id).IsEqualTo(broker.ProbeClientId);
        await Assert.That(sessions).Count().IsEqualTo(LiveClientCount);
        await Assert.That(observedSessions).Count().IsEqualTo(LiveClientCount);

        await ExerciseMessageAndRetainedOperationsAsync(server, firstRetained);
        await ExerciseServerDisconnectOperationsAsync(broker);
    }

    /// <summary>Verifies client and session statuses expose all properties, configuration, and operations.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LiveStatuses_ExposeCompletePairedSurfaceAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        var statuses = await broker.Server.GetClients().FirstAsync(OperationTimeout);
        var sessions = await broker.Server.GetSessions().FirstAsync(OperationTimeout);
        var client = FindClientStatus(statuses, broker.BridgeClientId);
        var secondClient = FindClientStatus(statuses, broker.ProbeClientId);
        var session = FindSessionStatus(sessions, broker.BridgeClientId);
        var secondSession = FindSessionStatus(sessions, broker.ProbeClientId);

        await ExerciseClientStatusAsync(client, session);
        await ExerciseSessionStatusAsync(session, "server/session-one");
        await ExerciseSessionStatusAsync(secondSession, "server/session-two");
        _ = await client.Disconnect(new MqttServerClientDisconnectOptionsBuilder().Build())
            .FirstAsync(OperationTimeout);
        _ = await client.Disconnect(static _ => { }).FirstAsync(OperationTimeout);
        _ = await secondClient.ObserveDisconnect(new MqttServerClientDisconnectOptionsBuilder().Build())
            .FirstAsync(OperationTimeout);
        _ = await secondClient.ObserveDisconnect(static _ => { }).FirstAsync(OperationTimeout);
        await WaitUntilAsync(() => !broker.BridgeClient.IsConnected && !broker.ProbeClient.IsConnected);
        _ = session.Properties();
        _ = secondSession.Properties();
        _ = await session.Delete().FirstAsync(OperationTimeout);
        _ = await secondSession.ObserveDelete().FirstAsync(OperationTimeout);

        await Assert.That(broker.BridgeClient.IsConnected).IsFalse();
        await Assert.That(broker.ProbeClient.IsConnected).IsFalse();
    }

    /// <summary>Verifies enhanced authentication is exposed through both cold reactive forms.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ValidationEnhancedAuthentication_ExposesPairedColdFormsAsync()
    {
        var adapter = Substitute.For<IMqttChannelAdapter>();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _ = adapter.ReceivePacketAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<MqttPacket>(new MqttAuthPacket { AuthenticationMethod = AuthenticationMethodValue }));
        var connectPacket = new MqttConnectPacket { AuthenticationMethod = AuthenticationMethodValue };
        var eventArgs = new ValidatingConnectionEventArgs(
            connectPacket,
            adapter,
            new Hashtable(),
            CancellationToken.None);
        var options = new ExchangeEnhancedAuthenticationOptions();

        var result = await eventArgs.ExchangeEnhancedAuthentication(options).FirstAsync(OperationTimeout);
        var observedResult = await eventArgs.ObserveExchangeEnhancedAuthentication(options)
            .FirstAsync(OperationTimeout);

        await Assert.That(result).IsNotNull();
        await Assert.That(observedResult).IsNotNull();
    }

    /// <summary>Verifies lifecycle and factory observer failures clean up their acquired resources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObserverRejection_CleansUpLifecycleAndFactoryResourcesAsync()
    {
        using var server = CreateServer();
        await Assert.That(() =>
            {
                using var subscription = server.IsStartedChanges().Subscribe(
                    static _ => throw new InvalidOperationException("Reject state."));
            })
            .Throws<InvalidOperationException>();
        var observer = new ControlledAsyncObserver<bool>(new InvalidOperationException("Reject async state."));
        await Assert.That(async () =>
                _ = await server.ObserveIsStartedChanges().SubscribeAsync(observer, CancellationToken.None))
            .Throws<InvalidOperationException>();
        await ExerciseServerNotifyObserverFailureAsync(server);
    }

    /// <summary>Verifies state subscriptions deduplicate notifications and dispose idempotently.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LifecycleStateSubscriptions_HandleDeduplicationAndIdempotenceAsync()
    {
        using var server = CreateServer();
        var owner = typeof(MqttServerPropertyExtensions);
        var synchronousType = owner.GetNestedType("ServerStateSubscription", BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Synchronous server state subscription type was not found.");
        var synchronousObserver = Substitute.For<IObserver<bool>>();
        var synchronous = Activator.CreateInstance(
            synchronousType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [server, synchronousObserver],
            culture: null) ?? throw new InvalidOperationException("Synchronous state subscription was not created.");
        var synchronousPublish = synchronousType.GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Synchronous publish method was not found.");
        _ = synchronousPublish.Invoke(synchronous, [false]);
        ((IDisposable)synchronous).Dispose();
        ((IDisposable)synchronous).Dispose();
        _ = synchronousPublish.Invoke(synchronous, [true]);

        var asynchronousType = owner.GetNestedType("ServerStateAsyncSubscription", BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Asynchronous server state subscription type was not found.");
        var asynchronousObserver = Substitute.For<IObserverAsync<bool>>();
        var asynchronous = Activator.CreateInstance(
            asynchronousType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [server, asynchronousObserver, CancellationToken.None],
            culture: null) ?? throw new InvalidOperationException("Asynchronous state subscription was not created.");
        await InvokeValueTaskAsync(asynchronousType, asynchronous, "InitializeAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        await InvokeValueTaskAsync(asynchronousType, asynchronous, "PublishAsync", BindingFlags.Instance | BindingFlags.NonPublic, false);
        await ((IAsyncDisposable)asynchronous).DisposeAsync();
        await ((IAsyncDisposable)asynchronous).DisposeAsync();
        await InvokeValueTaskAsync(asynchronousType, asynchronous, "PublishAsync", BindingFlags.Instance | BindingFlags.NonPublic, true);

        synchronousObserver.Received(1).OnNext(false);
        await asynchronousObserver.Received(1).OnNextAsync(false, Arg.Any<CancellationToken>());
        await Assert.That(server.IsStarted).IsFalse();
    }

    /// <summary>Verifies server sessions always release their owner when resource or release cleanup fails.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerSessions_ReleaseOwnerAcrossCleanupFailuresAsync()
    {
        using var server = CreateServer();
        var releaseFailure = new InvalidOperationException("Release failed.");
        var resourceFailure = new InvalidOperationException("Resource failed.");
        var releasedAfterResourceFailure = false;
        var releaseOnly = CreateServerSession(
            server,
            () => ValueTask.FromException(releaseFailure));
        var resourceOnly = CreateServerSession(
            server,
            () =>
            {
                releasedAfterResourceFailure = true;
                return ValueTask.CompletedTask;
            });
        var resource = Substitute.For<IDisposable>();
        resource.When(static value => value.Dispose()).Do(_ => throw resourceFailure);
        resourceOnly.Add(resource);
        var both = CreateServerSession(server, () => ValueTask.FromException(releaseFailure));
        var secondResource = Substitute.For<IDisposable>();
        secondResource.When(static value => value.Dispose()).Do(_ => throw resourceFailure);
        both.Add(secondResource);

        await Assert.That(async () => await releaseOnly.DisposeAsync()).Throws<InvalidOperationException>();
        await Assert.That(async () => await resourceOnly.DisposeAsync()).Throws<InvalidOperationException>();
        await Assert.That(async () => await both.DisposeAsync()).Throws<InvalidOperationException>();

        await Assert.That(releasedAfterResourceFailure).IsTrue();
        await Assert.That(releaseOnly.IsDisposed).IsTrue();
        await Assert.That(resourceOnly.IsDisposed).IsTrue();
        await Assert.That(both.IsDisposed).IsTrue();
    }

    /// <summary>Exercises all start and stop overloads.</summary>
    /// <param name="server">The server under test.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ExerciseLifecycleOperationsAsync(MqttServer server)
    {
        _ = await server.Start().FirstAsync(OperationTimeout);
        _ = await server.Stop().FirstAsync(OperationTimeout);
        _ = await server.ObserveStart().FirstAsync(OperationTimeout);
        _ = await server.Stop(new MqttServerStopOptions()).FirstAsync(OperationTimeout);
        _ = await server.Start().FirstAsync(OperationTimeout);
        _ = await server.Stop(static _ => { }).FirstAsync(OperationTimeout);
        _ = await server.ObserveStart().FirstAsync(OperationTimeout);
        _ = await server.ObserveStop().FirstAsync(OperationTimeout);
        _ = await server.Start().FirstAsync(OperationTimeout);
        _ = await server.ObserveStop(new MqttServerStopOptions()).FirstAsync(OperationTimeout);
        _ = await server.ObserveStart().FirstAsync(OperationTimeout);
        _ = await server.ObserveStop(static _ => { }).FirstAsync(OperationTimeout);
    }

    /// <summary>Exercises message injection and retained-message clearing.</summary>
    /// <param name="server">The live server.</param>
    /// <param name="message">The message to inject.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ExerciseMessageAndRetainedOperationsAsync(
        MqttServer server,
        MqttApplicationMessage message)
    {
        var injected = new InjectedMqttApplicationMessage(message);
        _ = await server.InjectApplicationMessageOperation(injected).FirstAsync(OperationTimeout);
        _ = await server.ObserveInjectApplicationMessage(injected).FirstAsync(OperationTimeout);
        _ = await server.DeleteRetainedMessages().FirstAsync(OperationTimeout);
        _ = await server.ObserveUpdateRetainedMessage(message).FirstAsync(OperationTimeout);
        _ = await server.ObserveDeleteRetainedMessages().FirstAsync(OperationTimeout);
    }

    /// <summary>Exercises all direct server client-disconnect overloads.</summary>
    /// <param name="broker">The live broker.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ExerciseServerDisconnectOperationsAsync(LiveMqttBroker broker)
    {
        var bridgeOptions = broker.BridgeClient.Options ?? throw new InvalidOperationException("Bridge options missing.");
        var probeOptions = broker.ProbeClient.Options ?? throw new InvalidOperationException("Probe options missing.");
        var disconnectOptions = new MqttServerClientDisconnectOptionsBuilder().Build();
        _ = await broker.Server.DisconnectClient(broker.BridgeClientId, disconnectOptions)
            .FirstAsync(OperationTimeout);
        await WaitUntilAsync(() => !broker.BridgeClient.IsConnected);
        await ConnectWhenReadyAsync(broker.BridgeClient, bridgeOptions);
        _ = await broker.Server.DisconnectClient(broker.ProbeClientId, static _ => { })
            .FirstAsync(OperationTimeout);
        await WaitUntilAsync(() => !broker.ProbeClient.IsConnected);
        await ConnectWhenReadyAsync(broker.ProbeClient, probeOptions);
        _ = await broker.Server.ObserveDisconnectClient(broker.BridgeClientId, disconnectOptions)
            .FirstAsync(OperationTimeout);
        await WaitUntilAsync(() => !broker.BridgeClient.IsConnected);
        await ConnectWhenReadyAsync(broker.BridgeClient, bridgeOptions);
        _ = await broker.Server.ObserveDisconnectClient(broker.ProbeClientId, static _ => { })
            .FirstAsync(OperationTimeout);
        await WaitUntilAsync(() => !broker.ProbeClient.IsConnected);
    }

    /// <summary>Exercises a broker client status.</summary>
    /// <param name="client">The client status.</param>
    /// <param name="session">The session assigned to the client.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ExerciseClientStatusAsync(MqttClientStatus client, MqttSessionStatus session)
    {
        var immediate = client.Properties();
        var property = await client.Property(static value => value.Id).FirstAsync(OperationTimeout);
        var observedProperty = await client.ObserveProperty(static value => value.ProtocolVersion)
            .FirstAsync(OperationTimeout);
        var snapshot = await client.PropertySnapshots().FirstAsync(OperationTimeout);
        var observedSnapshot = await client.ObservePropertySnapshots().FirstAsync(OperationTimeout);
        var sameClient = client.WithSession(session);
        _ = await client.ResetStatisticsOperation().FirstAsync(OperationTimeout);
        _ = await client.ObserveResetStatistics().FirstAsync(OperationTimeout);

        await Assert.That(immediate.Id).IsEqualTo(client.Id);
        await Assert.That(property).IsEqualTo(client.Id);
        await Assert.That(observedProperty).IsEqualTo(client.ProtocolVersion);
        await Assert.That(snapshot.Id).IsEqualTo(client.Id);
        await Assert.That(observedSnapshot.Id).IsEqualTo(client.Id);
        await Assert.That(sameClient).IsSameReferenceAs(client);
    }

    /// <summary>Exercises a broker session status.</summary>
    /// <param name="session">The session status.</param>
    /// <param name="topic">The message topic.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ExerciseSessionStatusAsync(MqttSessionStatus session, string topic)
    {
        var sameSession = session.WithSessionItem("one", SessionItemValue).WithoutSessionItem("missing");
        var immediate = session.Properties();
        var property = await session.Property(static value => value.Id).FirstAsync(OperationTimeout);
        var observedProperty = await session.ObserveProperty(static value => value.PendingApplicationMessagesCount)
            .FirstAsync(OperationTimeout);
        var snapshot = await session.PropertySnapshots().FirstAsync(OperationTimeout);
        var observedSnapshot = await session.ObservePropertySnapshots().FirstAsync(OperationTimeout);
        var message = CreateMessage(topic, false);
        var enqueued = await session.TryEnqueueApplicationMessage(message).FirstAsync(OperationTimeout);
        var observedEnqueued = await session.ObserveTryEnqueueApplicationMessage(message).FirstAsync(OperationTimeout);
        _ = await session.DeliverApplicationMessage(message).FirstAsync(OperationTimeout);
        _ = await session.ObserveDeliverApplicationMessage(message).FirstAsync(OperationTimeout);
        await Assert.That(async () =>
                _ = await session.ClearApplicationMessagesQueue().FirstAsync(OperationTimeout))
            .Throws<NotImplementedException>();
        await Assert.That(async () =>
                _ = await session.ObserveClearApplicationMessagesQueue().FirstAsync(OperationTimeout))
            .Throws<InvalidOperationException>();
        _ = session.ClearSessionItems().Properties();

        await Assert.That(sameSession).IsSameReferenceAs(session);
        await Assert.That(immediate.Id).IsEqualTo(session.Id);
        await Assert.That(property).IsEqualTo(session.Id);
        await Assert.That(observedProperty).IsGreaterThanOrEqualTo(0);
        await Assert.That(snapshot.Id).IsEqualTo(session.Id);
        await Assert.That(observedSnapshot.Id).IsEqualTo(session.Id);
        await Assert.That(enqueued.InjectResult).IsNotNull();
        await Assert.That(observedEnqueued.InjectResult).IsNotNull();
    }

    /// <summary>Finds a client status by identifier without allocating a LINQ iterator.</summary>
    /// <param name="statuses">The client statuses.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The matching status.</returns>
    private static MqttClientStatus FindClientStatus(IEnumerable<MqttClientStatus> statuses, string clientId)
    {
        foreach (var status in statuses)
        {
            if (status.Id == clientId)
            {
                return status;
            }
        }

        throw new InvalidOperationException($"Client status '{clientId}' was not found.");
    }

    /// <summary>Finds a session status by identifier without allocating a LINQ iterator.</summary>
    /// <param name="statuses">The session statuses.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The matching status.</returns>
    private static MqttSessionStatus FindSessionStatus(IEnumerable<MqttSessionStatus> statuses, string clientId)
    {
        foreach (var status in statuses)
        {
            if (status.Id == clientId)
            {
                return status;
            }
        }

        throw new InvalidOperationException($"Session status '{clientId}' was not found.");
    }

    /// <summary>Creates a server without network endpoints.</summary>
    /// <returns>The configured server.</returns>
    private static MqttServer CreateServer()
    {
        var factory = new MqttServerFactory();
        var options = factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build();
        return factory.CreateMqttServer(options);
    }

    /// <summary>Creates an application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="retain">Whether the message is retained.</param>
    /// <returns>The configured application message.</returns>
    private static MqttApplicationMessage CreateMessage(string topic, bool retain) =>
        new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload("payload").WithRetainFlag(retain).Build();

    /// <summary>Invokes the server factory notification boundary with a rejecting asynchronous observer.</summary>
    /// <param name="server">The server owned by the rejected session.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    private static async Task ExerciseServerNotifyObserverFailureAsync(MqttServer server)
    {
        var session = CreateServerSession(server, static () => ValueTask.CompletedTask);
        var notify = typeof(ServerCreate).GetMethod(
            "NotifyObserverAsync",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Server notification method was not found.");
        var observer = new ControlledAsyncObserver<(MqttServer Server, MqttServerSession Disposable)>(
            new InvalidOperationException("Reject server lease."));
        var operation = notify.Invoke(null, [observer, session, CancellationToken.None]) ??
            throw new InvalidOperationException("Server notification operation was not created.");
        var asTask = operation.GetType().GetMethod("AsTask", BindingFlags.Instance | BindingFlags.Public) ??
            throw new InvalidOperationException("Server notification task adapter was not found.");
        var task = (Task)(asTask.Invoke(operation, null) ??
            throw new InvalidOperationException("Server notification task was not created."));

        await Assert.That(async () => await task).Throws<InvalidOperationException>();
        await Assert.That(session.IsDisposed).IsTrue();
    }

    /// <summary>Creates a server session through its package-internal constructor.</summary>
    /// <param name="server">The session server.</param>
    /// <param name="release">The session release callback.</param>
    /// <returns>The created server session.</returns>
    private static MqttServerSession CreateServerSession(MqttServer server, Func<ValueTask> release)
    {
        var constructor = typeof(MqttServerSession).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(MqttServer), typeof(Func<ValueTask>)],
            modifiers: null) ?? throw new InvalidOperationException("Server session constructor was not found.");
        return (MqttServerSession)constructor.Invoke([server, release]);
    }

    /// <summary>Invokes a reflected method that returns a non-generic value task.</summary>
    /// <param name="ownerType">The method owner type.</param>
    /// <param name="owner">The method owner instance.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="bindingFlags">The reflection binding flags.</param>
    /// <param name="argument">The optional Boolean argument.</param>
    /// <returns>A task that represents the reflected operation.</returns>
    private static async Task InvokeValueTaskAsync(
        Type ownerType,
        object owner,
        string methodName,
        BindingFlags bindingFlags,
        bool? argument = null)
    {
        var method = ownerType.GetMethod(methodName, bindingFlags) ??
            throw new InvalidOperationException($"Value-task method '{methodName}' was not found.");
        object?[]? arguments = argument.HasValue ? [argument.Value] : null;
        var operation = (ValueTask)(method.Invoke(owner, arguments) ??
            throw new InvalidOperationException($"Value-task method '{methodName}' returned no operation."));
        await operation;
    }

    /// <summary>Reconnects after MQTTnet's previous disconnect state transition has fully settled.</summary>
    /// <param name="client">The client to reconnect.</param>
    /// <param name="options">The connection options.</param>
    /// <returns>A task that completes when the client reconnects.</returns>
    private static async Task ConnectWhenReadyAsync(IMqttClient client, MqttClientOptions options)
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        while (true)
        {
            try
            {
                _ = await client.ConnectAsync(options, cancellation.Token);
                return;
            }
            catch (InvalidOperationException) when (!cancellation.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }
    }

    /// <summary>Waits for a bounded broker lifecycle condition.</summary>
    /// <param name="condition">The condition to observe.</param>
    /// <returns>A task that completes when the condition becomes true.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));
        while (!condition())
        {
            _ = await timer.WaitForNextTickAsync(cancellation.Token);
        }
    }
}
