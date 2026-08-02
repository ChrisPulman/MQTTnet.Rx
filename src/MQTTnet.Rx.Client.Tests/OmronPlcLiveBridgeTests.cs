// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.OmronPlcRx.Reactive;
#else
using IoT.Driver.OmronPlcRx;
#endif
#if REACTIVE_SHIM
using IoT.Driver.OmronPlcRx.Reactive.Tags;
#else
using IoT.Driver.OmronPlcRx.Tags;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using OmronAsyncCreate = MQTTnet.Rx.OmronPlc.Reactive.ObservableAsyncCreateExtensions;
#else
using OmronAsyncCreate = MQTTnet.Rx.OmronPlc.ObservableAsyncCreateExtensions;
#endif
#if REACTIVE_SHIM
using OmronCreate = MQTTnet.Rx.OmronPlc.Reactive.OmronPlcCreateExtensions;
#else
using OmronCreate = MQTTnet.Rx.OmronPlc.OmronPlcCreateExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises every Omron bridge surface through a real loopback MQTT broker.</summary>
public sealed class OmronPlcLiveBridgeTests
{
    /// <summary>The maximum duration allowed for a live bridge operation.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Proves both directions of the synchronous raw-client Omron bridge.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawSynchronousBridge_RoundTripsTypedSimulatorValuesThroughLiveBrokerAsync()
    {
        const string publishTopic = "omron/raw/sync/publish";
        const string subscribeTopic = "omron/raw/sync/subscribe";
        const decimal publishedValue = 12.5M;
        const int writtenValue = 73;
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var simulator = new OmronPlcSimulator();
        var publishTag = new PlcTag<decimal>("RawSyncPublished", "D100");
        var publishKey = new LogicalTagKey<decimal>(publishTag.TagName);
        simulator.Seed(publishTag, 0M);
        await simulator.WriteValueAsync(publishKey, publishedValue, CancellationToken.None);
        await using var probeSubscription = await broker.SubscribeProbeAsync(publishTopic);

        var publishResult = await OmronCreate
            .PublishOmronPlcTag(broker.Bridge, publishTopic, publishKey, simulator)
            .FirstAsync(OperationTimeout);
        var publishedMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        var writeTag = new PlcTag<int>("RawSyncWritten", "D101");
        var writeKey = new LogicalTagKey<int>(writeTag.TagName);
        simulator.Seed(writeTag, 0);
        using var bridgeSubscription = await broker.SubscribeWhenReadyAsync(
            subscribeTopic,
            () => OmronCreate.SubscribeOmronPlcTag(
                broker.Bridge,
                subscribeTopic,
                writeKey,
                simulator,
                static payload => int.Parse(payload, System.Globalization.CultureInfo.InvariantCulture)));
        var observedWrite = await WaitForSimulatorValueAsync(
            simulator,
            writeKey,
            writtenValue,
            () => PublishRetainedAsync(
                broker.ProbeClient,
                subscribeTopic,
                writtenValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var readBack = await simulator.ReadValueAsync(writeKey, CancellationToken.None);

        await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(publishedMessage.Payload)).IsEqualTo("12.5");
        await Assert.That(observedWrite).IsEqualTo(writtenValue);
        await Assert.That(readBack).IsEqualTo(writtenValue);
        await Assert.That(HasSuccessfulWrite(simulator, writtenValue)).IsTrue();
    }

    /// <summary>Proves both directions of the asynchronous-observable raw-client Omron bridge.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawAsyncBridge_RoundTripsTypedSimulatorValuesThroughLiveBrokerAsync()
    {
        const string publishTopic = "omron/raw/async/publish";
        const string subscribeTopic = "omron/raw/async/subscribe";
        const bool publishedValue = true;
        const int writtenValue = 91;
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var simulator = new OmronPlcSimulator();
        IObservableAsync<IMqttClient> clients = broker.Bridge.ToSignal();
        var publishTag = new PlcTag<bool>("RawAsyncPublished", "D110.0");
        var publishKey = new LogicalTagKey<bool>(publishTag.TagName);
        simulator.Seed(publishTag, false);
        await simulator.WriteValueAsync(publishKey, publishedValue, CancellationToken.None);
        await using var probeSubscription = await broker.SubscribeProbeAsync(publishTopic);

        var publishResult = await OmronAsyncCreate
            .PublishOmronPlcTag(clients, publishTopic, publishKey, simulator)
            .FirstAsync(OperationTimeout);
        var publishedMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        var writeTag = new PlcTag<int>("RawAsyncWritten", "D111");
        var writeKey = new LogicalTagKey<int>(writeTag.TagName);
        simulator.Seed(writeTag, 0);
        using var bridgeSubscription = await broker.SubscribeWhenReadyAsync(
            subscribeTopic,
            () => OmronAsyncCreate.SubscribeOmronPlcTag(
                clients,
                subscribeTopic,
                writeKey,
                simulator,
                static payload => int.Parse(payload, System.Globalization.CultureInfo.InvariantCulture)));
        var observedWrite = await WaitForSimulatorValueAsync(
            simulator,
            writeKey,
            writtenValue,
            () => PublishRetainedAsync(
                broker.ProbeClient,
                subscribeTopic,
                writtenValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var readBack = await simulator.ReadValueAsync(writeKey, CancellationToken.None);

        await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(publishedMessage.Payload)).IsEqualTo(bool.TrueString);
        await Assert.That(observedWrite).IsEqualTo(writtenValue);
        await Assert.That(readBack).IsEqualTo(writtenValue);
        await Assert.That(HasSuccessfulWrite(simulator, writtenValue)).IsTrue();
    }

    /// <summary>Proves both directions of the synchronous resilient-client Omron bridge.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientSynchronousBridge_RoundTripsTypedSimulatorValuesThroughLiveBrokerAsync()
    {
        const string publishTopic = "omron/resilient/sync/publish";
        const string subscribeTopic = "omron/resilient/sync/subscribe";
        const double publishedValue = 98.75D;
        const short writtenValue = 26;
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var simulator = new OmronPlcSimulator();
        var clients = CreateResilientClients(broker, "omron-resilient-sync");
        var clientReady = new TaskCompletionSource<IResilientMqttClient>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var clientLease = clients.Subscribe(
            client => _ = clientReady.TrySetResult(client),
            exception => _ = clientReady.TrySetException(exception));
        var resilientClient = await clientReady.Task.WaitAsync(OperationTimeout);
        await WaitForConnectionAsync(resilientClient);
        var publishTag = new PlcTag<double>("ResilientSyncPublished", "D120");
        var publishKey = new LogicalTagKey<double>(publishTag.TagName);
        simulator.Seed(publishTag, 0D);
        await simulator.WriteValueAsync(publishKey, publishedValue, CancellationToken.None);
        await using var probeSubscription = await broker.SubscribeProbeAsync(publishTopic);

        var publishResult = await OmronCreate
            .PublishOmronPlcTag(clients, publishTopic, publishKey, simulator)
            .FirstAsync(OperationTimeout);
        var publishedMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        var writeTag = new PlcTag<short>("ResilientSyncWritten", "D121");
        var writeKey = new LogicalTagKey<short>(writeTag.TagName);
        simulator.Seed(writeTag, (short)0);
        using var bridgeSubscription = await broker.SubscribeWhenReadyAsync(
            subscribeTopic,
            () => OmronCreate.SubscribeOmronPlcTag(
                clients,
                subscribeTopic,
                writeKey,
                simulator,
                static payload => short.Parse(payload, System.Globalization.CultureInfo.InvariantCulture)));
        var observedWrite = await WaitForSimulatorValueAsync(
            simulator,
            writeKey,
            writtenValue,
            () => PublishRetainedAsync(
                broker.ProbeClient,
                subscribeTopic,
                writtenValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var readBack = await simulator.ReadValueAsync(writeKey, CancellationToken.None);

        await Assert.That(publishResult.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(publishedMessage.Payload)).IsEqualTo("98.75");
        await Assert.That(observedWrite).IsEqualTo(writtenValue);
        await Assert.That(readBack).IsEqualTo(writtenValue);
        await Assert.That(HasSuccessfulWrite(simulator, writtenValue)).IsTrue();
        bridgeSubscription.Dispose();
        await resilientClient.StopAsync();
    }

    /// <summary>Proves both directions of the asynchronous-observable resilient-client Omron bridge.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientAsyncBridge_RoundTripsTypedSimulatorValuesThroughLiveBrokerAsync()
    {
        const string publishTopic = "omron/resilient/async/publish";
        const string subscribeTopic = "omron/resilient/async/subscribe";
        const int publishedValue = -41;
        const long writtenValue = 123_456_789L;
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var simulator = new OmronPlcSimulator();
        var clients = CreateResilientAsyncClients(broker, "omron-resilient-async");
        var clientReady = new TaskCompletionSource<IResilientMqttClient>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var clientLease = await SubscribeForFirstClientAsync(clients, clientReady);
        var resilientClient = await clientReady.Task.WaitAsync(OperationTimeout);
        await WaitForConnectionAsync(resilientClient);
        var publishTag = new PlcTag<int>("ResilientAsyncPublished", "D130");
        var publishKey = new LogicalTagKey<int>(publishTag.TagName);
        simulator.Seed(publishTag, 0);
        await simulator.WriteValueAsync(publishKey, publishedValue, CancellationToken.None);
        await using var probeSubscription = await broker.SubscribeProbeAsync(publishTopic);

        var publishResult = await OmronAsyncCreate
            .PublishOmronPlcTag(clients, publishTopic, publishKey, simulator)
            .FirstAsync(OperationTimeout);
        var publishedMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        var writeTag = new PlcTag<long>("ResilientAsyncWritten", "D131");
        var writeKey = new LogicalTagKey<long>(writeTag.TagName);
        simulator.Seed(writeTag, 0L);
        using var bridgeSubscription = await broker.SubscribeWhenReadyAsync(
            subscribeTopic,
            () => OmronAsyncCreate.SubscribeOmronPlcTag(
                clients,
                subscribeTopic,
                writeKey,
                simulator,
                static payload => long.Parse(payload, System.Globalization.CultureInfo.InvariantCulture)));
        var observedWrite = await WaitForSimulatorValueAsync(
            simulator,
            writeKey,
            writtenValue,
            () => PublishRetainedAsync(
                broker.ProbeClient,
                subscribeTopic,
                writtenValue.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var readBack = await simulator.ReadValueAsync(writeKey, CancellationToken.None);

        await Assert.That(publishResult.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(publishedMessage.Payload)).IsEqualTo("-41");
        await Assert.That(observedWrite).IsEqualTo(writtenValue);
        await Assert.That(readBack).IsEqualTo(writtenValue);
        await Assert.That(HasSuccessfulWrite(simulator, writtenValue)).IsTrue();
        bridgeSubscription.Dispose();
        await resilientClient.StopAsync();
    }

    /// <summary>Creates a live configured synchronous resilient-client stream.</summary>
    /// <param name="broker">The broker that receives the resilient connection.</param>
    /// <param name="clientName">The unique client-name prefix.</param>
    /// <returns>The configured resilient-client stream.</returns>
    private static IObservable<IResilientMqttClient> CreateResilientClients(
        LiveMqttBroker broker,
        string clientName) =>
        Create.ResilientMqttClient().WithResilientClientOptions(options =>
            options.WithClientOptions(client => client
                .WithClientId($"{clientName}-{Guid.NewGuid():N}")
                .WithTcpServer(IPAddress.Loopback.ToString(), broker.Port)));

    /// <summary>Creates a live configured asynchronous resilient-client stream.</summary>
    /// <param name="broker">The broker that receives the resilient connection.</param>
    /// <param name="clientName">The unique client-name prefix.</param>
    /// <returns>The configured asynchronous resilient-client stream.</returns>
    private static IObservableAsync<IResilientMqttClient> CreateResilientAsyncClients(
        LiveMqttBroker broker,
        string clientName) =>
        Create.ResilientMqttClientSignal().WithResilientClientOptions(options =>
            options.WithClientOptions(client => client
                .WithClientId($"{clientName}-{Guid.NewGuid():N}")
                .WithTcpServer(IPAddress.Loopback.ToString(), broker.Port)));

    /// <summary>Waits until a resilient client has completed its live broker connection.</summary>
    /// <param name="client">The resilient client to observe.</param>
    /// <returns>A task that completes when the client is connected.</returns>
    private static async Task WaitForConnectionAsync(IResilientMqttClient client)
    {
        if (client.IsConnected)
        {
            return;
        }

        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = client.RegisterConnectedHandler((_, _) =>
        {
            _ = connected.TrySetResult();
            return ValueTask.CompletedTask;
        });
        if (client.IsConnected)
        {
            return;
        }

        await connected.Task.WaitAsync(OperationTimeout);
    }

    /// <summary>Publishes a retained command so a concurrently installing subscription cannot miss it.</summary>
    /// <param name="client">The real MQTT probe client.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The command payload.</param>
    /// <returns>A task that represents the publish operation.</returns>
    private static async Task PublishRetainedAsync(IMqttClient client, string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();
        var result = await client.PublishAsync(message, CancellationToken.None);
        await Assert.That(result.ReasonCode is
            MqttClientPublishReasonCode.Success or
            MqttClientPublishReasonCode.NoMatchingSubscribers).IsTrue();
    }

    /// <summary>Waits for one exact typed simulator value while an MQTT command is published.</summary>
    /// <typeparam name="T">The registered tag type.</typeparam>
    /// <param name="simulator">The deterministic Omron simulator.</param>
    /// <param name="tag">The typed tag key.</param>
    /// <param name="expected">The expected written value.</param>
    /// <param name="trigger">The MQTT publish that triggers the bridge write.</param>
    /// <returns>The matching observed simulator value.</returns>
    private static async Task<T?> WaitForSimulatorValueAsync<T>(
        OmronPlcSimulator simulator,
        LogicalTagKey<T> tag,
        T expected,
        Func<Task> trigger)
    {
        var observed = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = simulator.Observe(tag).Subscribe(
            value =>
            {
                if (!EqualityComparer<T?>.Default.Equals(value, expected))
                {
                    return;
                }

                _ = observed.TrySetResult(value);
            },
            exception => _ = observed.TrySetException(exception),
            () => _ = observed.TrySetException(
                new InvalidOperationException("The simulator completed before the expected write.")));
        await trigger();
        return await observed.Task.WaitAsync(OperationTimeout);
    }

    /// <summary>Determines whether the simulator recorded an acknowledged typed write.</summary>
    /// <typeparam name="T">The written tag type.</typeparam>
    /// <param name="simulator">The deterministic Omron simulator.</param>
    /// <param name="expected">The expected operation value.</param>
    /// <returns><see langword="true"/> when the expected successful write was recorded.</returns>
    private static bool HasSuccessfulWrite<T>(OmronPlcSimulator simulator, T expected)
    {
        foreach (var operation in simulator.Operations)
        {
            if (operation.Operation == OmronSimulatorOperation.Write
                && operation.Succeeded
                && Equals(operation.Value, expected))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Subscribes to an asynchronous resilient stream and captures its first client.</summary>
    /// <param name="clients">The configured resilient-client stream.</param>
    /// <param name="clientReady">The completion source that receives the first client.</param>
    /// <returns>The subscription that keeps the resilient client alive.</returns>
    private static async Task<IAsyncDisposable> SubscribeForFirstClientAsync(
        IObservableAsync<IResilientMqttClient> clients,
        TaskCompletionSource<IResilientMqttClient> clientReady) =>
        await clients.SubscribeAsync(
            (client, cancellationToken) =>
            {
                _ = clientReady.TrySetResult(client);
                return ValueTask.CompletedTask;
            },
            (exception, cancellationToken) =>
            {
                _ = clientReady.TrySetException(exception);
                return ValueTask.CompletedTask;
            },
            result =>
            {
                _ = clientReady.TrySetException(
                    new InvalidOperationException("The resilient stream completed before emitting a client."));
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
}
