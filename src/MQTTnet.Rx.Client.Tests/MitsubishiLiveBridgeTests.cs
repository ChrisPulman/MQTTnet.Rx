// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.MitsubishiRx.Reactive;
#else
using IoT.Driver.MitsubishiRx;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using MQTTnet.Rx.Mitsubishi.Reactive;
#else
using MQTTnet.Rx.Mitsubishi;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using MitsubishiClient = IoT.Driver.MitsubishiRx.Reactive.MitsubishiRx;
#else
using MitsubishiClient = IoT.Driver.MitsubishiRx.MitsubishiRx;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the Mitsubishi bridge through a real in-process MQTT broker and simulator memory.</summary>
public class MitsubishiLiveBridgeTests
{
    /// <summary>The synchronous publisher's simulator value.</summary>
    private const ushort SyncPublishedValue = 321;

    /// <summary>The asynchronous publisher's simulator value.</summary>
    private const ushort AsyncPublishedValue = 654;

    /// <summary>The synchronous subscriber's expected value.</summary>
    private const ushort SyncSubscribedValue = 777;

    /// <summary>The asynchronous subscriber's expected value.</summary>
    private const ushort AsyncSubscribedValue = 888;

    /// <summary>The first queued write value.</summary>
    private const ushort FirstOrderedValue = 10;

    /// <summary>The second queued write value.</summary>
    private const ushort SecondOrderedValue = 20;

    /// <summary>The third queued write value.</summary>
    private const ushort ThirdOrderedValue = 30;

    /// <summary>The expected queued write count.</summary>
    private const int OrderedWriteCount = 3;

    /// <summary>The simulator word address used by the logical tag.</summary>
    private const string Address = "D100";

    /// <summary>The protocol data type registered for the logical tag.</summary>
    private const string DataType = "UInt16";

    /// <summary>The logical tag name used by the live bridge.</summary>
    private const string TagName = "Line.Speed";

    /// <summary>The topic used by the synchronous publish bridge.</summary>
    private const string SyncPublishTopic = "tests/mitsubishi/sync/publish";

    /// <summary>The topic used by the synchronous subscribe bridge.</summary>
    private const string SyncSubscribeTopic = "tests/mitsubishi/sync/subscribe";

    /// <summary>The topic used by the asynchronous publish bridge.</summary>
    private const string AsyncPublishTopic = "tests/mitsubishi/async/publish";

    /// <summary>The topic used by the asynchronous subscribe bridge.</summary>
    private const string AsyncSubscribeTopic = "tests/mitsubishi/async/subscribe";

    /// <summary>The topic used by ordered-write coverage.</summary>
    private const string OrderingTopic = "tests/mitsubishi/ordering";

    /// <summary>The topic used by parser-error coverage.</summary>
    private const string ParserErrorTopic = "tests/mitsubishi/parser-error";

    /// <summary>The topic used by logical-write error coverage.</summary>
    private const string WriteErrorTopic = "tests/mitsubishi/write-error";

    /// <summary>The topic used by cancellation coverage.</summary>
    private const string CancellationTopic = "tests/mitsubishi/cancellation";

    /// <summary>The topic used by disposal coverage.</summary>
    private const string DisposalTopic = "tests/mitsubishi/disposal";

    /// <summary>The maximum duration of a live operation.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Proves simulator tag observation publishes through the bridge and real broker to the probe.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncPublish_SimulatorTagFlowsThroughRealBrokerToProbeAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        fixture.Memory.WriteWord(Address, SyncPublishedValue);
        await using var probe = await broker.SubscribeProbeAsync(SyncPublishTopic);

        var publishResult = await broker.Bridge
            .PublishMitsubishiTag(
                SyncPublishTopic,
                fixture.Tag,
                fixture.LogicalTags,
                static value => $"speed={value.ToString(CultureInfo.InvariantCulture)}")
            .FirstAsync(OperationTimeout);
        var received = await probe.MessageReceived.WaitAsync(OperationTimeout);

        await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(received.Topic).IsEqualTo(SyncPublishTopic);
        await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo("speed=321");
        await Assert.That(fixture.Transport.Requests).IsNotEmpty();
        await Assert.That(fixture.Transport.Requests[0].Description).Contains("Read");
    }

    /// <summary>Proves an asynchronous observable publishes the formatted simulator tag through the broker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncPublish_SimulatorTagFlowsThroughRealBrokerToProbeAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        fixture.Memory.WriteWord(Address, AsyncPublishedValue);
        await using var probe = await broker.SubscribeProbeAsync(AsyncPublishTopic);
        IObservableAsync<IMqttClient> asyncBridge = broker.Bridge.ToSignal();

        var publishResult = await asyncBridge
            .PublishMitsubishiTag(
                AsyncPublishTopic,
                fixture.Tag,
                fixture.LogicalTags,
                static value => value.ToString(CultureInfo.InvariantCulture))
            .FirstAsync(OperationTimeout);
        var received = await probe.MessageReceived.WaitAsync(OperationTimeout);

        await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo("654");
        await Assert.That(fixture.Memory.ReadWord(Address)).IsEqualTo(AsyncPublishedValue);
    }

    /// <summary>Proves a probe message reaches the simulator and logical readback through the live broker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SyncSubscribe_ProbeFlowsThroughRealBrokerToSimulatorAndLogicalReadbackAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        using var subscription = broker.Bridge.SubscribeMitsubishiTag(
            SyncSubscribeTopic,
            fixture.Tag,
            fixture.LogicalTags,
            static payload => ushort.Parse(payload, CultureInfo.InvariantCulture),
            null,
            CancellationToken.None);
        await EnsureBridgeSubscriptionAsync(broker, SyncSubscribeTopic);

        _ = await PublishProbeAsync(broker, SyncSubscribeTopic, "777");
        await WaitUntilAsync(() => fixture.Memory.ReadWord(Address) == SyncSubscribedValue);
        var readback = await fixture.LogicalTags.ReadAsync(fixture.Tag, CancellationToken.None);

        await Assert.That(readback.Succeeded).IsTrue();
        await Assert.That(readback.Value).IsEqualTo(SyncSubscribedValue);
        await Assert.That(ContainsRequest(fixture.Transport, "Write")).IsTrue();
        await Assert.That(ContainsRequest(fixture.Transport, "Read")).IsTrue();
    }

    /// <summary>Proves the asynchronous subscribe facade writes to simulator memory through the broker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncSubscribe_ProbeFlowsThroughRealBrokerToSimulatorAndLogicalReadbackAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        IObservableAsync<IMqttClient> asyncBridge = broker.Bridge.ToSignal();
        using var subscription = asyncBridge.SubscribeMitsubishiTag(
            AsyncSubscribeTopic,
            fixture.Tag,
            fixture.LogicalTags,
            static payload => ushort.Parse(payload, NumberStyles.None, CultureInfo.InvariantCulture),
            null,
            CancellationToken.None);
        await EnsureBridgeSubscriptionAsync(broker, AsyncSubscribeTopic);

        _ = await PublishProbeAsync(broker, AsyncSubscribeTopic, "888");
        await WaitUntilAsync(() => fixture.Memory.ReadWord(Address) == AsyncSubscribedValue);
        var readback = await fixture.LogicalTags.ReadAsync(fixture.Tag, CancellationToken.None);

        await Assert.That(readback.Succeeded).IsTrue();
        await Assert.That(readback.Value).IsEqualTo(AsyncSubscribedValue);
        await Assert.That(fixture.Memory.Version).IsGreaterThan(0);
    }

    /// <summary>Verifies queued MQTT writes preserve broker receive order before reaching simulator memory.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Subscribe_QueuedWritesPreserveReceiveOrderAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        var parsed = new List<ushort>();
        var parsedGate = new object();
        using var subscription = broker.Bridge.SubscribeMitsubishiTag(
            OrderingTopic,
            fixture.Tag,
            fixture.LogicalTags,
            payload =>
            {
                var value = ushort.Parse(payload, CultureInfo.InvariantCulture);
                lock (parsedGate)
                {
                    parsed.Add(value);
                }

                return value;
            },
            null,
            CancellationToken.None);
        await EnsureBridgeSubscriptionAsync(broker, OrderingTopic);

        _ = await PublishProbeAsync(broker, OrderingTopic, "10");
        _ = await PublishProbeAsync(broker, OrderingTopic, "20");
        _ = await PublishProbeAsync(broker, OrderingTopic, "30");
        await WaitUntilAsync(() => fixture.Memory.ReadWord(Address) == ThirdOrderedValue);

        ushort[] parsedSnapshot;
        lock (parsedGate)
        {
            parsedSnapshot = [.. parsed];
        }

        await Assert.That(parsedSnapshot).IsEquivalentTo(
            [FirstOrderedValue, SecondOrderedValue, ThirdOrderedValue]);
        await Assert.That(fixture.Memory.ReadWord(Address)).IsEqualTo(ThirdOrderedValue);
        await Assert.That(CountRequests(fixture.Transport, "Write")).IsEqualTo(OrderedWriteCount);
    }

    /// <summary>Verifies parser and logical-write failures are delivered through the error callback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Subscribe_ParserAndLogicalWriteFailuresReachOnErrorAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        var parserError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var parserSubscription = broker.Bridge.SubscribeMitsubishiTag(
            ParserErrorTopic,
            fixture.Tag,
            fixture.LogicalTags,
            static payload => ushort.Parse(payload, CultureInfo.InvariantCulture),
            error => _ = parserError.TrySetResult(error),
            CancellationToken.None);
        await EnsureBridgeSubscriptionAsync(broker, ParserErrorTopic);

        _ = await PublishProbeAsync(broker, ParserErrorTopic, "not-a-number");
        var parserException = await parserError.Task.WaitAsync(OperationTimeout);

        await Assert.That(parserException).IsTypeOf<FormatException>();
        await Assert.That(fixture.Memory.ReadWord(Address)).IsEqualTo((ushort)0);

        await using var readOnlyFixture = CreateFixture(broker.Port, LogicalTagAccessMode.Read);
        var writeError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var writeSubscription = broker.Bridge.SubscribeMitsubishiTag(
            WriteErrorTopic,
            readOnlyFixture.Tag,
            readOnlyFixture.LogicalTags,
            static payload => ushort.Parse(payload, CultureInfo.InvariantCulture),
            error => _ = writeError.TrySetResult(error),
            CancellationToken.None);
        await EnsureBridgeSubscriptionAsync(broker, WriteErrorTopic);

        _ = await PublishProbeAsync(broker, WriteErrorTopic, "42");
        var writeException = await writeError.Task.WaitAsync(OperationTimeout);

        await Assert.That(writeException).IsTypeOf<InvalidOperationException>();
        await Assert.That(writeException.Message).Contains("read-only");
        await Assert.That(readOnlyFixture.Memory.ReadWord(Address)).IsEqualTo((ushort)0);
    }

    /// <summary>Verifies cancellation and disposal prevent later simulator writes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Subscribe_CancellationAndDisposalPreventWritesAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var fixture = CreateFixture(broker.Port);
        using var cancellation = new CancellationTokenSource();
        var cancelled = broker.Bridge.SubscribeMitsubishiTag(
            CancellationTopic,
            fixture.Tag,
            fixture.LogicalTags,
            static payload => ushort.Parse(payload, CultureInfo.InvariantCulture),
            null,
            cancellation.Token);
        await EnsureBridgeSubscriptionAsync(broker, CancellationTopic);
        await cancellation.CancelAsync();

        _ = await PublishProbeAsync(broker, CancellationTopic, "101");
        await DrainContinuationsAsync();
        await Assert.That(fixture.Memory.ReadWord(Address)).IsEqualTo((ushort)0);

        cancelled.Dispose();
        cancelled.Dispose();
        var disposed = broker.Bridge.SubscribeMitsubishiTag(
            DisposalTopic,
            fixture.Tag,
            fixture.LogicalTags,
            static payload => ushort.Parse(payload, CultureInfo.InvariantCulture),
            null,
            CancellationToken.None);
        disposed.Dispose();
        disposed.Dispose();
        await DrainContinuationsAsync();
        await EnsureBridgeSubscriptionAsync(broker, DisposalTopic);

        _ = await PublishProbeAsync(broker, DisposalTopic, "202");
        await DrainContinuationsAsync();

        await Assert.That(fixture.Memory.ReadWord(Address)).IsEqualTo((ushort)0);
        await Assert.That(fixture.Transport.Requests).IsEmpty();
    }

    /// <summary>Creates a simulator-backed Mitsubishi logical-tag fixture.</summary>
    /// <param name="port">An ephemeral port value used only to satisfy the simulator options contract.</param>
    /// <param name="accessMode">The logical tag access mode.</param>
    /// <returns>The configured fixture.</returns>
    private static MitsubishiFixture CreateFixture(
        int port,
        LogicalTagAccessMode accessMode = LogicalTagAccessMode.ReadWrite)
    {
        var memory = new MitsubishiSimulatorMemory();
        var transport = new MitsubishiSimulatorTransport(memory);
        var options = new MitsubishiClientOptions(
            "127.0.0.1",
            port,
            MitsubishiFrameType.ThreeE,
            CommunicationDataCode.Binary,
            MitsubishiTransportKind.Tcp);
        var owner = new MitsubishiClient(options, transport, scheduler: null);
        var logicalTags = owner.CreateLogicalTagClient(
            catalog: null,
            defaultScanInterval: TimeSpan.FromHours(1),
            store: null);
        var tag = new LogicalTagKey<ushort>(TagName);
        logicalTags.RegisterTag(new(
            TagName,
            Address,
            DataType,
            new LogicalTagOptions
            {
                AccessMode = accessMode,
                ScanInterval = TimeSpan.FromHours(1),
            }));
        return new(memory, transport, owner, logicalTags, tag);
    }

    /// <summary>Publishes one probe payload through the real broker.</summary>
    /// <param name="broker">The live broker fixture.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The UTF-8 payload.</param>
    /// <returns>The real MQTT publish result.</returns>
    private static Task<MqttClientPublishResult> PublishProbeAsync(
        LiveMqttBroker broker,
        string topic,
        string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        return broker.ProbeClient.PublishAsync(message, CancellationToken.None);
    }

    /// <summary>Waits for a real bridge-client subscription before a probe publish.</summary>
    /// <param name="broker">The live broker fixture.</param>
    /// <param name="topic">The exact bridge topic.</param>
    /// <returns>A task that completes after the broker acknowledges the subscription.</returns>
    private static async Task EnsureBridgeSubscriptionAsync(LiveMqttBroker broker, string topic)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        var result = await broker.BridgeClient
            .SubscribeAsync(options, CancellationToken.None)
            .ConfigureAwait(false);
        foreach (var item in result.Items)
        {
            if (item.ResultCode is not MqttClientSubscribeResultCode.GrantedQoS0
                and not MqttClientSubscribeResultCode.GrantedQoS1
                and not MqttClientSubscribeResultCode.GrantedQoS2)
            {
                throw new InvalidOperationException("The live broker rejected the Mitsubishi bridge subscription.");
            }
        }
    }

    /// <summary>Determines whether a simulator request contains the operation text.</summary>
    /// <param name="transport">The simulator transport.</param>
    /// <param name="operation">The operation text.</param>
    /// <returns><see langword="true"/> when a matching request exists.</returns>
    private static bool ContainsRequest(MitsubishiSimulatorTransport transport, string operation) =>
        CountRequests(transport, operation) != 0;

    /// <summary>Counts simulator requests containing the operation text.</summary>
    /// <param name="transport">The simulator transport.</param>
    /// <param name="operation">The operation text.</param>
    /// <returns>The matching request count.</returns>
    private static int CountRequests(MitsubishiSimulatorTransport transport, string operation)
    {
        var count = 0;
        foreach (var request in transport.Requests)
        {
            if (request.Description.Contains(operation, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Waits without fixed sleeps until a deterministic simulator condition is true.</summary>
    /// <param name="condition">The condition to await.</param>
    /// <returns>A task that completes when the condition becomes true.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        using var timeout = new CancellationTokenSource(OperationTimeout);
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    /// <summary>Allows already-queued asynchronous callbacks to finish without a time-based delay.</summary>
    /// <returns>A task representing continuation draining.</returns>
    private static async Task DrainContinuationsAsync()
    {
        const int continuationCount = 32;
        for (var index = 0; index < continuationCount; index++)
        {
            await Task.Yield();
        }
    }

    /// <summary>Owns the exact upstream simulator, transport, owner, and logical tag client combination.</summary>
    /// <param name="Memory">The stateful simulator memory.</param>
    /// <param name="Transport">The simulator transport.</param>
    /// <param name="Owner">The Mitsubishi client.</param>
    /// <param name="LogicalTags">The logical tag client.</param>
    /// <param name="Tag">The typed logical tag.</param>
    private sealed record MitsubishiFixture(
        MitsubishiSimulatorMemory Memory,
        MitsubishiSimulatorTransport Transport,
        MitsubishiClient Owner,
        MitsubishiLogicalTagClient LogicalTags,
        LogicalTagKey<ushort> Tag) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            LogicalTags.Dispose();
            await Owner.DisposeAsync();
        }
    }
}
