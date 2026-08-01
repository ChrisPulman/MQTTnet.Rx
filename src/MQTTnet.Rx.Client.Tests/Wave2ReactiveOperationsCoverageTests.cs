// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Protocol;
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

/// <summary>Exercises the remaining reactive MQTT client operation paths.</summary>
public class Wave2ReactiveOperationsCoverageTests
{
    /// <summary>The topic used by protocol-operation coverage.</summary>
    private const string TestTopic = "coverage/reactive";

    /// <summary>The payload used by protocol-operation coverage.</summary>
    private const string TestPayload = "payload";

    /// <summary>The placeholder host used to initialize reconnect state.</summary>
    private const string BrokerHost = "localhost";

    /// <summary>The expected connection-state value count.</summary>
    private const int ExpectedStatusCount = 3;

    /// <summary>The expected synchronous subscription count.</summary>
    private const int ExpectedSynchronousSubscriptionCount = 4;

    /// <summary>The expected asynchronous subscription count.</summary>
    private const int ExpectedAsynchronousSubscriptionCount = 2;

    /// <summary>The expected synchronous disconnect count.</summary>
    private const int ExpectedSynchronousDisconnectCount = 2;

    /// <summary>The expected synchronous publish count.</summary>
    private const int ExpectedSynchronousPublishCount = 8;

    /// <summary>The expected asynchronous publish count.</summary>
    private const int ExpectedAsynchronousPublishCount = 4;

    /// <summary>The expected number of initial and reconnect connection attempts.</summary>
    private const int ExpectedConnectCount = 2;

    /// <summary>The bounded timeout used by reactive-operation coverage.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Exercises every synchronous compatibility-facade overload.</summary>
    [Test]
    public void SynchronousFacade_ForwardsEveryOverload()
    {
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var topics = new[] { TestTopic };
        var filter = new MqttTopicFilter { Topic = TestTopic };
        var message = new MqttApplicationMessage { Topic = TestTopic };

        _ = ReactiveClientOperations.Ping(clients);
        _ = ReactiveClientOperations.PingPeriodically(clients);
        _ = ReactiveClientOperations.PingPeriodically(clients, TimeSpan.FromMilliseconds(1));
        _ = ReactiveClientOperations.Subscribe(clients, topics);
        _ = ReactiveClientOperations.Subscribe(clients, topics, MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Subscribe(clients, static builder => builder.WithTopic(TestTopic));
        _ = ReactiveClientOperations.Subscribe(clients, filter);
        _ = ReactiveClientOperations.Unsubscribe(clients, topics);
        _ = ReactiveClientOperations.Disconnect(clients);
        _ = ReactiveClientOperations.Disconnect(clients, MqttClientDisconnectOptionsReason.AdministrativeAction);
        _ = ReactiveClientOperations.Reconnect(clients);
        _ = ReactiveClientOperations.ConnectionStatus(clients);
        _ = ReactiveClientOperations.WaitForConnection(clients);
        _ = ReactiveClientOperations.WaitForConnection(clients, OperationTimeout);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, TestPayload);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, TestPayload, MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Publish(
            clients,
            TestTopic,
            TestPayload,
            MqttQualityOfServiceLevel.ExactlyOnce,
            true);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, []);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, [], MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Publish(
            clients,
            TestTopic,
            [],
            MqttQualityOfServiceLevel.ExactlyOnce,
            true);
        _ = ReactiveClientOperations.Publish(clients, static builder => builder.WithTopic(TestTopic));
        _ = ReactiveClientOperations.PublishMany(clients, Signal.Emit(message));
        _ = ReactiveClientOperations.GetOptions(clients);
    }

    /// <summary>Exercises every asynchronous compatibility-facade overload.</summary>
    [Test]
    public void AsynchronousFacade_ForwardsEveryOverload()
    {
        using var mqttClient = new MockMqttClient();
        var clients = SignalAsync.Return<IMqttClient>(mqttClient);
        var topics = new[] { TestTopic };
        var filter = new MqttTopicFilter { Topic = TestTopic };
        var message = new MqttApplicationMessage { Topic = TestTopic };

        _ = ReactiveClientOperations.Ping(clients);
        _ = ReactiveClientOperations.PingPeriodically(clients);
        _ = ReactiveClientOperations.PingPeriodically(clients, TimeSpan.FromMilliseconds(1));
        _ = ReactiveClientOperations.Subscribe(clients, topics);
        _ = ReactiveClientOperations.Subscribe(clients, topics, MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Subscribe(clients, static builder => builder.WithTopic(TestTopic));
        _ = ReactiveClientOperations.Subscribe(clients, filter);
        _ = ReactiveClientOperations.Unsubscribe(clients, topics);
        _ = ReactiveClientOperations.Disconnect(clients);
        _ = ReactiveClientOperations.Disconnect(clients, MqttClientDisconnectOptionsReason.AdministrativeAction);
        _ = ReactiveClientOperations.Reconnect(clients);
        _ = ReactiveClientOperations.ConnectionStatus(clients);
        _ = ReactiveClientOperations.WaitForConnection(clients);
        _ = ReactiveClientOperations.WaitForConnection(clients, OperationTimeout);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, TestPayload);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, TestPayload, MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Publish(
            clients,
            TestTopic,
            TestPayload,
            MqttQualityOfServiceLevel.ExactlyOnce,
            true);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, []);
        _ = ReactiveClientOperations.Publish(clients, TestTopic, [], MqttQualityOfServiceLevel.AtLeastOnce);
        _ = ReactiveClientOperations.Publish(
            clients,
            TestTopic,
            [],
            MqttQualityOfServiceLevel.ExactlyOnce,
            true);
        _ = ReactiveClientOperations.Publish(clients, static builder => builder.WithTopic(TestTopic));
        _ = ReactiveClientOperations.PublishMany(clients, SignalAsync.Return(message));
        _ = ReactiveClientOperations.GetOptions(clients);
    }

    /// <summary>Executes synchronous protocol operations and their connected and event-driven branches.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SynchronousOperations_ExecuteProtocolAndConnectionBranchesAsync()
    {
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var filter = new MqttTopicFilter { Topic = TestTopic };

        _ = await mqttClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(BrokerHost).Build());

        _ = await clients.Ping().FirstAsync(OperationTimeout);
        _ = await clients.PingPeriodically(TimeSpan.FromMilliseconds(1)).Take(1).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe([TestTopic]).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe([TestTopic], MqttQualityOfServiceLevel.AtLeastOnce).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe(static builder => builder.WithTopic(TestTopic)).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe(filter).FirstAsync(OperationTimeout);
        _ = await clients.Unsubscribe(TestTopic).FirstAsync(OperationTimeout);
        _ = await clients.Disconnect().FirstAsync(OperationTimeout);
        _ = await clients.Disconnect(MqttClientDisconnectOptionsReason.AdministrativeAction)
            .FirstAsync(OperationTimeout);
        _ = await clients.Reconnect().FirstAsync(OperationTimeout);

        await mqttClient.SimulateDisconnectedAsync();
        var statusesTask = clients.ConnectionStatus().Take(ExpectedStatusCount).CollectAsync(OperationTimeout);
        await mqttClient.SimulateConnectedAsync();
        await mqttClient.SimulateDisconnectedAsync();
        var statuses = await statusesTask;

        await mqttClient.SimulateConnectedAsync();
        var alreadyConnected = await clients.WaitForConnection().FirstAsync(OperationTimeout);
        await mqttClient.SimulateDisconnectedAsync();
        var connectionTask = clients.WaitForConnection(null).FirstAsync(OperationTimeout);
        await mqttClient.SimulateConnectedAsync();
        var eventConnected = await connectionTask;

        await Assert.That(statuses).IsEquivalentTo([false, true, false]);
        await Assert.That(alreadyConnected).IsSameReferenceAs(mqttClient);
        await Assert.That(eventConnected).IsSameReferenceAs(mqttClient);
        await Assert.That(mqttClient.PingCount).IsGreaterThanOrEqualTo(ExpectedSynchronousDisconnectCount);
        await Assert.That(mqttClient.Subscriptions.Count).IsEqualTo(ExpectedSynchronousSubscriptionCount);
        await Assert.That(mqttClient.Unsubscriptions).Contains(TestTopic);
        await Assert.That(mqttClient.DisconnectCount).IsEqualTo(ExpectedSynchronousDisconnectCount);
        await Assert.That(mqttClient.ConnectCount).IsEqualTo(ExpectedConnectCount);
    }

    /// <summary>Executes every synchronous publishing overload and options projection.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SynchronousOperations_ExecutePublishingOverloadsAsync()
    {
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var bytes = new byte[] { 1, 2, 3 };

        _ = await clients.Publish(TestTopic, TestPayload).FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, TestPayload, MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, TestPayload, MqttQualityOfServiceLevel.ExactlyOnce, true)
            .FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, bytes).FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, bytes, MqttQualityOfServiceLevel.AtLeastOnce).FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, bytes, MqttQualityOfServiceLevel.ExactlyOnce, true)
            .FirstAsync(OperationTimeout);
        _ = await clients.Publish(static builder => builder.WithTopic(TestTopic).WithPayload(TestPayload))
            .FirstAsync(OperationTimeout);
        _ = await clients.PublishMany(Signal.Emit(new MqttApplicationMessage { Topic = TestTopic }))
            .FirstAsync(OperationTimeout);
        var options = await clients.GetOptions().FirstAsync(OperationTimeout);

        await Assert.That(mqttClient.PublishedMessages).Count().IsEqualTo(ExpectedSynchronousPublishCount);
        await Assert.That(options).IsNull();
    }

    /// <summary>Executes the remaining asynchronous overloads and connection-state branches.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AsynchronousOperations_ExecuteRemainingBranchesAsync()
    {
        using var mqttClient = new MockMqttClient();
        var clients = SignalAsync.Return<IMqttClient>(mqttClient);
        var filter = new MqttTopicFilter { Topic = TestTopic };

        _ = await mqttClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(BrokerHost).Build());

        _ = await clients.PingPeriodically(TimeSpan.FromMilliseconds(1)).Take(1).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe([TestTopic]).FirstAsync(OperationTimeout);
        _ = await clients.Subscribe(filter).FirstAsync(OperationTimeout);
        _ = await clients.Disconnect().FirstAsync(OperationTimeout);
        _ = await clients.Reconnect().FirstAsync(OperationTimeout);

        await mqttClient.SimulateDisconnectedAsync();
        var statuses = await clients.ConnectionStatus().Take(1).ToObservable().CollectAsync(OperationTimeout);

        await mqttClient.SimulateConnectedAsync();
        var alreadyConnected = await clients.WaitForConnection().FirstAsync(OperationTimeout);
        await mqttClient.SimulateDisconnectedAsync();
        var connectionTask = clients.WaitForConnection(null).FirstAsync(OperationTimeout);
        await mqttClient.SimulateConnectedAsync();
        var eventConnected = await connectionTask;

        _ = await clients.Publish(TestTopic, TestPayload).FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, TestPayload, MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, []).FirstAsync(OperationTimeout);
        _ = await clients.Publish(TestTopic, [], MqttQualityOfServiceLevel.AtLeastOnce).FirstAsync(OperationTimeout);

        await Assert.That(statuses).IsEquivalentTo([false]);
        await Assert.That(alreadyConnected).IsSameReferenceAs(mqttClient);
        await Assert.That(eventConnected).IsSameReferenceAs(mqttClient);
        await Assert.That(mqttClient.PingCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(mqttClient.Subscriptions.Count).IsEqualTo(ExpectedAsynchronousSubscriptionCount);
        await Assert.That(mqttClient.DisconnectCount).IsEqualTo(1);
        await Assert.That(mqttClient.ConnectCount).IsEqualTo(ExpectedConnectCount);
        await Assert.That(mqttClient.PublishedMessages).Count().IsEqualTo(ExpectedAsynchronousPublishCount);
    }
}
