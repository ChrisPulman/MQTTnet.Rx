// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reflection;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the complete direct and sequence-oriented MQTT client wrapper surface.</summary>
public sealed class ClientCompleteSurfaceTests
{
    /// <summary>The non-routable test broker host stored in client option snapshots.</summary>
    private const string BrokerHost = "localhost";

    /// <summary>The number of direct publish operations exercised.</summary>
    private const int ExpectedDirectPublishCount = 10;

    /// <summary>The number of raw and fluent operation variants exercised.</summary>
    private const int ExpectedOperationVariantCount = 4;

    /// <summary>The number of sequence publish operations exercised.</summary>
    private const int ExpectedSequencePublishCount = 2;

    /// <summary>The expected successful auto-reconnect notification count.</summary>
    private const int ExpectedReconnectNotificationCount = 2;

    /// <summary>The retry limit used by the successful reconnect test.</summary>
    private const int ReconnectAttemptLimit = 3;

    /// <summary>The binary payload used by the publish overload tests.</summary>
    private static readonly byte[] BinaryPayload = [1, 2];

    /// <summary>The sequence payload used by the publish overload tests.</summary>
    private static readonly byte[] SequencePayload = [3, 4];

    /// <summary>The delay that keeps one reconnect handler active while a duplicate event arrives.</summary>
    private static readonly TimeSpan DuplicateReconnectDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>The delay cancelled by the cancellation-path reconnect test.</summary>
    private static readonly TimeSpan CancellationReconnectDelay = TimeSpan.FromSeconds(1);

    /// <summary>The maximum time allowed for a cold operation to produce its result.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Verifies every direct standard-client operation has synchronous and asynchronous forms.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DirectClientConnectionAndPublishOperations_ExposePairedColdFormsAsync()
    {
        using var concreteClient = new MockMqttClient();
        IMqttClient client = concreteClient;
        var connectOptions = CreateClientOptions("direct-client");
        var message = new MqttApplicationMessageBuilder().WithTopic("direct/message").WithPayload("one").Build();
        var authenticationData = new MqttEnhancedAuthenticationExchangeData();

        _ = await client.Connect(connectOptions).FirstAsync(OperationTimeout);
        _ = await client.Connect(static options => options.WithClientId("direct-configured").WithTcpServer(BrokerHost))
            .FirstAsync(OperationTimeout);
        _ = await client.ObserveConnect(connectOptions).FirstAsync(OperationTimeout);
        _ = await client.ObserveConnect(static options => options.WithClientId("direct-observed").WithTcpServer(BrokerHost))
            .FirstAsync(OperationTimeout);
        _ = await client.Ping().FirstAsync(OperationTimeout);
        _ = await client.ObservePing().FirstAsync(OperationTimeout);
        _ = await client.Publish(message).FirstAsync(OperationTimeout);
        _ = await client.Publish(static builder => builder.WithTopic("direct/configured").WithPayload("two"))
            .FirstAsync(OperationTimeout);
        _ = await client.ObservePublish(message).FirstAsync(OperationTimeout);
        _ = await client.ObservePublish(static builder => builder.WithTopic("direct/observed").WithPayload("three"))
            .FirstAsync(OperationTimeout);
        _ = await client.PublishBinary("direct/binary", BinaryPayload, MqttQualityOfServiceLevel.AtLeastOnce, true)
            .FirstAsync(OperationTimeout);
        _ = await client.ObservePublishBinary("direct/binary-async", null, MqttQualityOfServiceLevel.AtMostOnce, false)
            .FirstAsync(OperationTimeout);
        var payload = new ReadOnlySequence<byte>(SequencePayload);
        _ = await client.PublishSequence("direct/sequence", payload, MqttQualityOfServiceLevel.ExactlyOnce, false)
            .FirstAsync(OperationTimeout);
        _ = await client.ObservePublishSequence(
                "direct/sequence-async",
                payload,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true)
            .FirstAsync(OperationTimeout);
        _ = await client.PublishString("direct/string", "four", MqttQualityOfServiceLevel.AtMostOnce, false)
            .FirstAsync(OperationTimeout);
        _ = await client.ObservePublishString("direct/string-async", null, MqttQualityOfServiceLevel.AtLeastOnce, true)
            .FirstAsync(OperationTimeout);
        _ = await client.Reconnect().FirstAsync(OperationTimeout);
        _ = await client.ObserveReconnect().FirstAsync(OperationTimeout);
        _ = await client.SendEnhancedAuthenticationExchangeData(authenticationData).FirstAsync(OperationTimeout);
        _ = await client.ObserveSendEnhancedAuthenticationExchangeData(authenticationData).FirstAsync(OperationTimeout);

        await Assert.That(concreteClient.PublishedMessages).Count().IsEqualTo(ExpectedDirectPublishCount);
    }

    /// <summary>Verifies every direct subscription and disconnection operation has paired cold forms.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DirectClientSubscriptionAndDisconnectOperations_ExposePairedColdFormsAsync()
    {
        using var concreteClient = new MockMqttClient();
        IMqttClient client = concreteClient;
        var connectOptions = CreateClientOptions("direct-subscriptions");
        var disconnectOptions = new MqttClientDisconnectOptionsBuilder().Build();
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter("direct/#").Build();
        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter("direct/#").Build();

        _ = await client.Subscribe(subscribeOptions).FirstAsync(OperationTimeout);
        _ = await client.Subscribe(static builder => builder.WithTopicFilter("direct/configured/#"))
            .FirstAsync(OperationTimeout);
        _ = await client.ObserveSubscribe(subscribeOptions).FirstAsync(OperationTimeout);
        _ = await client.ObserveSubscribe(static builder => builder.WithTopicFilter("direct/observed/#"))
            .FirstAsync(OperationTimeout);
        _ = await client.Unsubscribe(unsubscribeOptions).FirstAsync(OperationTimeout);
        _ = await client.Unsubscribe(static builder => builder.WithTopicFilter("direct/configured/#"))
            .FirstAsync(OperationTimeout);
        _ = await client.ObserveUnsubscribe(unsubscribeOptions).FirstAsync(OperationTimeout);
        _ = await client.ObserveUnsubscribe(static builder => builder.WithTopicFilter("direct/observed/#"))
            .FirstAsync(OperationTimeout);
        var tryPing = await client.TryPing().FirstAsync(OperationTimeout);
        var observeTryPing = await client.ObserveTryPing().FirstAsync(OperationTimeout);
        var tryDisconnect = await client.TryDisconnect().FirstAsync(OperationTimeout);
        var observeTryDisconnect = await client.ObserveTryDisconnect(
                MqttClientDisconnectOptionsReason.NormalDisconnection,
                "observed")
            .FirstAsync(OperationTimeout);
        _ = await client.Connect(connectOptions).FirstAsync(OperationTimeout);
        _ = await client.TryDisconnect(MqttClientDisconnectOptionsReason.NormalDisconnection, "direct")
            .FirstAsync(OperationTimeout);
        _ = await client.Connect(connectOptions).FirstAsync(OperationTimeout);
        _ = await client.ObserveTryDisconnect().FirstAsync(OperationTimeout);
        _ = await client.Disconnect(disconnectOptions).FirstAsync(OperationTimeout);
        _ = await client.Disconnect(static _ => { }).FirstAsync(OperationTimeout);
        _ = await client.ObserveDisconnect(disconnectOptions).FirstAsync(OperationTimeout);
        _ = await client.ObserveDisconnect(static _ => { }).FirstAsync(OperationTimeout);

        await Assert.That(tryPing).IsTrue();
        await Assert.That(observeTryPing).IsTrue();
        await Assert.That(tryDisconnect).IsTrue();
        await Assert.That(observeTryDisconnect).IsTrue();
        await Assert.That(concreteClient.Subscriptions).Count().IsEqualTo(ExpectedOperationVariantCount);
        await Assert.That(concreteClient.Unsubscriptions).Count().IsEqualTo(ExpectedOperationVariantCount);
    }

    /// <summary>Verifies every direct standard-client property has paired cold projections.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task DirectClientProperties_ExposeCompleteSnapshotsAsync()
    {
        using var concreteClient = new MockMqttClient();
        IMqttClient client = concreteClient;
        var options = CreateClientOptions("properties-client");
        _ = await client.Connect(options).FirstAsync(OperationTimeout);

        var immediate = client.Properties();
        var property = await client.Property(static value => value.IsConnected).FirstAsync(OperationTimeout);
        var observedProperty = await client.ObserveProperty(static value => value.Options).FirstAsync(OperationTimeout);
        var snapshot = await client.PropertySnapshots().FirstAsync(OperationTimeout);
        var observedSnapshot = await client.ObservePropertySnapshots().FirstAsync(OperationTimeout);
        var connected = await client.IsConnectedValue().FirstAsync(OperationTimeout);
        var observedConnected = await client.ObserveIsConnected().FirstAsync(OperationTimeout);
        var optionsSnapshot = await client.OptionsSnapshot().FirstAsync(OperationTimeout);
        var observedOptionsSnapshot = await client.ObserveOptionsSnapshot().FirstAsync(OperationTimeout);

        await Assert.That(immediate.IsConnected).IsTrue();
        await Assert.That(immediate.Options).IsSameReferenceAs(options);
        await Assert.That(property).IsTrue();
        await Assert.That(observedProperty).IsSameReferenceAs(options);
        await Assert.That(snapshot).IsEqualTo(immediate);
        await Assert.That(observedSnapshot).IsEqualTo(immediate);
        await Assert.That(connected).IsTrue();
        await Assert.That(observedConnected).IsTrue();
        await Assert.That(optionsSnapshot).IsSameReferenceAs(options);
        await Assert.That(observedOptionsSnapshot).IsSameReferenceAs(options);
    }

    /// <summary>Verifies raw options flow through both standard-client sequence variants.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ClientSequenceOperations_ExposeRawAndFluentOptionsAsync()
    {
        using var concreteClient = new MockMqttClient();
        IMqttClient client = concreteClient;
        var clients = Signal.Emit(client);
        var asyncClients = SignalAsync.Return(client);
        var connectOptions = CreateClientOptions("sequence-client");
        var disconnectOptions = new MqttClientDisconnectOptionsBuilder().Build();
        var message = new MqttApplicationMessageBuilder().WithTopic("sequence/message").Build();
        var authenticationData = new MqttEnhancedAuthenticationExchangeData();
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter("sequence/#").Build();
        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter("sequence/#").Build();

        _ = await clients.Connect(connectOptions).FirstAsync(OperationTimeout);
        _ = await clients.Connect(static options => options.WithClientId("sequence-configured").WithTcpServer(BrokerHost))
            .FirstAsync(OperationTimeout);
        _ = await clients.Publish(message).FirstAsync(OperationTimeout);
        _ = await clients.SendEnhancedAuthenticationExchangeData(authenticationData).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe(subscribeOptions).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe(static builder => builder.WithTopicFilter("sequence/configured/#"))
            .FirstAsync(OperationTimeout);
        _ = await clients.Unsubscribe(unsubscribeOptions).FirstAsync(OperationTimeout);
        _ = await clients.Unsubscribe(static builder => builder.WithTopicFilter("sequence/configured/#"))
            .FirstAsync(OperationTimeout);
        _ = await clients.Disconnect(disconnectOptions).FirstAsync(OperationTimeout);
        _ = await clients.Disconnect(static _ => { }).FirstAsync(OperationTimeout);

        _ = await asyncClients.Connect(connectOptions).FirstAsync(OperationTimeout);
        _ = await asyncClients.Connect(static options => options.WithClientId("sequence-observed").WithTcpServer(BrokerHost))
            .FirstAsync(OperationTimeout);
        _ = await asyncClients.Publish(message).FirstAsync(OperationTimeout);
        _ = await asyncClients.SendEnhancedAuthenticationExchangeData(authenticationData).FirstAsync(OperationTimeout);
        _ = await asyncClients.Subscribe(subscribeOptions).FirstAsync(OperationTimeout);
        _ = await asyncClients.Subscribe(static builder => builder.WithTopicFilter("sequence/observed/#"))
            .FirstAsync(OperationTimeout);
        _ = await asyncClients.Unsubscribe(unsubscribeOptions).FirstAsync(OperationTimeout);
        _ = await asyncClients.Unsubscribe(static builder => builder.WithTopicFilter("sequence/observed/#"))
            .FirstAsync(OperationTimeout);
        _ = await asyncClients.Disconnect(disconnectOptions).FirstAsync(OperationTimeout);
        _ = await asyncClients.Disconnect(static _ => { }).FirstAsync(OperationTimeout);

        await Assert.That(concreteClient.PublishedMessages).Count().IsEqualTo(ExpectedSequencePublishCount);
        await Assert.That(concreteClient.Subscriptions).Count().IsEqualTo(ExpectedOperationVariantCount);
        await Assert.That(concreteClient.Unsubscriptions).Count().IsEqualTo(ExpectedOperationVariantCount);
    }

    /// <summary>Verifies every resilient-client operation and property has paired projections.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClient_ExposesCompletePairedSurfaceAsync()
    {
        using var concreteClient = new MockResilientMqttClient();
        var client = concreteClient;
        var options = CreateResilientOptions("resilient-client");
        var message = new MqttApplicationMessageBuilder().WithTopic("resilient/message").Build();
        var managedMessage = new ResilientMqttApplicationMessage { ApplicationMessage = message };
        MqttTopicFilter[] filters = [new MqttTopicFilterBuilder().WithTopic("resilient/#").Build()];
        string[] topics = ["resilient/#"];

        _ = await client.Start(options).FirstAsync(OperationTimeout);
        _ = await client.Stop().FirstAsync(OperationTimeout);
        _ = await client.Start(static builder =>
                builder.WithClientOptions(CreateClientOptions("resilient-configured")))
            .FirstAsync(OperationTimeout);
        _ = await client.Enqueue(message).FirstAsync(OperationTimeout);
        _ = await client.Enqueue(managedMessage).FirstAsync(OperationTimeout);
        _ = await client.Ping().FirstAsync(OperationTimeout);
        _ = await client.Subscribe(filters).FirstAsync(OperationTimeout);
        _ = await client.Unsubscribe(topics).FirstAsync(OperationTimeout);
        _ = await client.Stop(false).FirstAsync(OperationTimeout);
        _ = await client.ObserveStart(options).FirstAsync(OperationTimeout);
        _ = await client.ObserveStop().FirstAsync(OperationTimeout);
        _ = await client.ObserveStart(static builder =>
                builder.WithClientOptions(CreateClientOptions("resilient-observed")))
            .FirstAsync(OperationTimeout);
        _ = await client.ObserveEnqueue(message).FirstAsync(OperationTimeout);
        _ = await client.ObserveEnqueue(managedMessage).FirstAsync(OperationTimeout);
        _ = await client.ObservePing().FirstAsync(OperationTimeout);
        _ = await client.ObserveSubscribe(filters).FirstAsync(OperationTimeout);
        _ = await client.ObserveUnsubscribe(topics).FirstAsync(OperationTimeout);
        _ = await client.ObserveStop(false).FirstAsync(OperationTimeout);

        var immediate = client.Properties();
        var property = await client.Property(static value => value.IsStarted).FirstAsync(OperationTimeout);
        var observedProperty = await client.ObserveProperty(static value => value.InternalClient)
            .FirstAsync(OperationTimeout);
        var snapshot = await client.PropertySnapshots().FirstAsync(OperationTimeout);
        var observedSnapshot = await client.ObservePropertySnapshots().FirstAsync(OperationTimeout);
        var changed = new TaskCompletionSource<SubscriptionsChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = client.SubscriptionsChanged().Subscribe(value => _ = changed.TrySetResult(value));
        await concreteClient.SimulateSubscriptionsChangedAsync();
        var eventArgs = await changed.Task.WaitAsync(OperationTimeout);

        await Assert.That(immediate.InternalClient).IsSameReferenceAs(client.InternalClient);
        await Assert.That(property).IsFalse();
        await Assert.That(observedProperty).IsSameReferenceAs(client.InternalClient);
        await Assert.That(snapshot).IsEqualTo(immediate);
        await Assert.That(observedSnapshot).IsEqualTo(immediate);
        await Assert.That(eventArgs.SubscribeResult).IsEmpty();
        await Assert.That(eventArgs.UnsubscribeResult).IsEmpty();
    }

    /// <summary>Verifies asynchronous auto reconnect retries, emits, and reports exhausted retry policies.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncAutoReconnect_HandlesSuccessAndExhaustionAsync()
    {
        using var successfulClient = new MockMqttClient();
        _ = await successfulClient.ConnectAsync(CreateClientOptions("auto-success"));
        successfulClient.ReconnectFailuresRemaining = 1;
        IObservableAsync<IMqttClient> successfulSource = SignalAsync.Return<IMqttClient>(successfulClient);
        _ = successfulSource.WithAutoReconnect();
        _ = successfulSource.WithAutoReconnect(TimeSpan.Zero);
        var emitted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        await using var successfulSubscription = await successfulSource
            .WithAutoReconnect(TimeSpan.Zero, ReconnectAttemptLimit)
            .SubscribeAsync(
                (value, cancellationToken) =>
                {
                    GC.KeepAlive(value);
                    GC.KeepAlive(cancellationToken);
                    if (Interlocked.Increment(ref count) == ExpectedReconnectNotificationCount)
                    {
                        _ = emitted.TrySetResult(count);
                    }

                    return ValueTask.CompletedTask;
                },
                CancellationToken.None);
        await successfulClient.SimulateDisconnectedAsync();
        var observedCount = await emitted.Task.WaitAsync(OperationTimeout);

        using var failingClient = new MockMqttClient { ReconnectFailuresRemaining = ReconnectAttemptLimit };
        IObservableAsync<IMqttClient> failingSource = SignalAsync.Return<IMqttClient>(failingClient);
        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var failingSubscription = await failingSource
            .WithAutoReconnect(TimeSpan.Zero, 1)
            .SubscribeAsync(
                static (_, _) => ValueTask.CompletedTask,
                (exception, cancellationToken) =>
                {
                    GC.KeepAlive(cancellationToken);
                    _ = failure.TrySetResult(exception);
                    return ValueTask.CompletedTask;
                },
                static _ => ValueTask.CompletedTask,
                CancellationToken.None);
        await failingClient.SimulateDisconnectedAsync();
        var observedFailure = await failure.Task.WaitAsync(OperationTimeout);

        await Assert.That(observedCount).IsEqualTo(ExpectedReconnectNotificationCount);
        await Assert.That(successfulClient.ConnectCount).IsEqualTo(ReconnectAttemptLimit);
        await Assert.That(observedFailure).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies duplicate disconnect events are serialized and cancellation stops a pending reconnect.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncAutoReconnect_SerializesDuplicatesAndHonorsCancellationAsync()
    {
        using var duplicateClient = new MockMqttClient();
        _ = await duplicateClient.ConnectAsync(CreateClientOptions("auto-duplicate"));
        duplicateClient.ReconnectFailuresRemaining = 1;
        var duplicateNotifications = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var notificationCount = 0;
        await using var duplicateSubscription = await SignalAsync.Return<IMqttClient>(duplicateClient)
            .WithAutoReconnect(DuplicateReconnectDelay)
            .SubscribeAsync(
                (value, cancellationToken) =>
                {
                    GC.KeepAlive(value);
                    GC.KeepAlive(cancellationToken);
                    if (Interlocked.Increment(ref notificationCount) == ExpectedReconnectNotificationCount)
                    {
                        _ = duplicateNotifications.TrySetResult();
                    }

                    return ValueTask.CompletedTask;
                },
                CancellationToken.None);
        await Task.WhenAll(duplicateClient.SimulateDisconnectedAsync(), duplicateClient.SimulateDisconnectedAsync());
        await duplicateNotifications.Task.WaitAsync(OperationTimeout);

        using var cancelledClient = new MockMqttClient();
        _ = await cancelledClient.ConnectAsync(CreateClientOptions("auto-cancelled"));
        using var cancellation = new CancellationTokenSource();
        await using var cancelledSubscription = await SignalAsync.Return<IMqttClient>(cancelledClient)
            .WithAutoReconnect(CancellationReconnectDelay, 1)
            .SubscribeAsync(static (_, _) => ValueTask.CompletedTask, cancellation.Token);
        var disconnect = cancelledClient.SimulateDisconnectedAsync();
        await Task.Yield();
        await cancellation.CancelAsync();
        await disconnect;

        await Assert.That(notificationCount).IsEqualTo(ExpectedReconnectNotificationCount);
        await Assert.That(cancelledClient.IsConnected).IsFalse();
    }

    /// <summary>Verifies factory observer rejection releases the rejected lease before retrying.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task FactoryObserverRejection_ReleasesRejectedLeaseAsync()
    {
        await Assert.That(static () =>
            {
                using var subscription = TestClientCreate.MqttClient().Subscribe(static value =>
                {
                    GC.KeepAlive(value);
                    throw new InvalidOperationException("Reject the synchronous lease.");
                });
            })
            .Throws<InvalidOperationException>();

        await ExerciseClientNotifyObserverFailureAsync();
    }

    /// <summary>Creates standard client options for a test client.</summary>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The configured options.</returns>
    private static MqttClientOptions CreateClientOptions(string clientId) =>
        new MqttClientOptionsBuilder().WithClientId(clientId).WithTcpServer(BrokerHost).Build();

    /// <summary>Creates resilient-client options for a test client.</summary>
    /// <param name="clientId">The client identifier.</param>
    /// <returns>The configured options.</returns>
    private static ResilientMqttClientOptions CreateResilientOptions(string clientId) =>
        new ResilientMqttClientOptionsBuilder().WithClientOptions(CreateClientOptions(clientId)).Build();

    /// <summary>Invokes the factory notification boundary with a rejecting asynchronous observer.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    private static async Task ExerciseClientNotifyObserverFailureAsync()
    {
        var lifetimeDefinition = typeof(TestClientCreate).GetNestedType(
            "SharedClientLifetime`1",
            BindingFlags.NonPublic) ?? throw new InvalidOperationException("Client lifetime type was not found.");
        var lifetimeType = lifetimeDefinition.MakeGenericType(typeof(IMqttClient));
        var lifetime = Activator.CreateInstance(
            lifetimeType,
            (Func<IMqttClient>)(static () => new MockMqttClient())) ??
            throw new InvalidOperationException("Client lifetime was not created.");
        var acquire = lifetimeType.GetMethod("Acquire", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Client acquisition method was not found.");
        var lease = acquire.Invoke(lifetime, null) ?? throw new InvalidOperationException("Client lease was not acquired.");
        var notifyDefinition = typeof(TestClientCreate).GetMethod(
            "NotifyObserverAsync",
            BindingFlags.Static | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Client notification method was not found.");
        var notify = notifyDefinition.MakeGenericMethod(typeof(IMqttClient));
        var observer = new ControlledAsyncObserver<IMqttClient>(new InvalidOperationException("Reject client lease."));
        var operation = notify.Invoke(null, [observer, lease, CancellationToken.None]) ??
            throw new InvalidOperationException("Client notification operation was not created.");
        var asTask = operation.GetType().GetMethod("AsTask", BindingFlags.Instance | BindingFlags.Public) ??
            throw new InvalidOperationException("Client notification task adapter was not found.");
        var task = (Task)(asTask.Invoke(operation, null) ??
            throw new InvalidOperationException("Client notification task was not created."));

        await Assert.That(async () => await task).Throws<InvalidOperationException>();
    }
}
