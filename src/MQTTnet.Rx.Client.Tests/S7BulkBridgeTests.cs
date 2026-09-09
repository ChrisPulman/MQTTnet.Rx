// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Net;
using System.Text;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.S7PlcRx.Reactive;
using IoT.Driver.S7PlcRx.Reactive.Enums;
using IoT.Driver.S7PlcRx.Reactive.LogicalTags;
using MQTTnet.Rx.S7Plc.Reactive;
#else
using IoT.Driver.S7PlcRx;
using IoT.Driver.S7PlcRx.Enums;
using IoT.Driver.S7PlcRx.LogicalTags;
using MQTTnet.Rx.S7Plc;
#endif
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

/// <summary>Exercises S7 batch and logical-tag bulk MQTT bridge behavior.</summary>
public sealed partial class S7PlcLiveBridgeTests
{
    /// <summary>The first batch variable.</summary>
    private const string BulkFirstVariable = "S7.Bulk.First";

    /// <summary>The ignored MQTT payload used by subscribe tests.</summary>
    private const string IgnoredPayload = "ignored";

    /// <summary>The number of writes expected for the two-variable batch.</summary>
    private const int BatchWriteCount = 2;

    /// <summary>The second batch variable.</summary>
    private const string BulkSecondVariable = "S7.Bulk.Second";

    /// <summary>The prefix shared by the S7 bulk test variables.</summary>
    private const string BulkVariablePrefix = "S7.Bulk.";

    /// <summary>The first logical-tag address.</summary>
    private const string BulkFirstAddress = "DB1.DBW0";

    /// <summary>The second logical-tag address.</summary>
    private const string BulkSecondAddress = "DB1.DBW2";

    /// <summary>The S7 logical tag type name.</summary>
    private const string BulkDataType = "Int32";

    /// <summary>The raw batch publish topic.</summary>
    private const string RawBatchPublishTopic = "s7/bulk/raw/publish";

    /// <summary>The raw batch subscribe topic.</summary>
    private const string RawBatchSubscribeTopic = "s7/bulk/raw/subscribe";

    /// <summary>The asynchronous batch publish topic.</summary>
    private const string AsyncBatchPublishTopic = "s7/bulk/async/publish";

    /// <summary>The asynchronous batch subscribe topic.</summary>
    private const string AsyncBatchSubscribeTopic = "s7/bulk/async/subscribe";

    /// <summary>The resilient batch publish topic.</summary>
    private const string ResilientBatchPublishTopic = "s7/bulk/resilient/publish";

    /// <summary>The resilient batch subscribe topic.</summary>
    private const string ResilientBatchSubscribeTopic = "s7/bulk/resilient/subscribe";

    /// <summary>The logical raw publish topic.</summary>
    private const string LogicalRawPublishTopic = "s7/logical/raw/publish";

    /// <summary>The logical raw subscribe topic.</summary>
    private const string LogicalRawSubscribeTopic = "s7/logical/raw/subscribe";

    /// <summary>The logical resilient publish topic.</summary>
    private const string LogicalResilientPublishTopic = "s7/logical/resilient/publish";

    /// <summary>The logical resilient subscribe topic.</summary>
    private const string LogicalResilientSubscribeTopic = "s7/logical/resilient/subscribe";

    /// <summary>The logical asynchronous publish topic.</summary>
    private const string LogicalAsyncPublishTopic = "s7/logical/async/publish";

    /// <summary>The logical asynchronous subscribe topic.</summary>
    private const string LogicalAsyncSubscribeTopic = "s7/logical/async/subscribe";

    /// <summary>The parser failure topic.</summary>
    private const string BatchParserFailureTopic = "s7/bulk/parser-failure";

    /// <summary>The cancellation topic.</summary>
    private const string BatchCancellationTopic = "s7/bulk/cancellation";

    /// <summary>The initial first batch value.</summary>
    private const int BulkInitialFirstValue = 501;

    /// <summary>The initial second batch value.</summary>
    private const int BulkInitialSecondValue = 502;

    /// <summary>The raw first write value.</summary>
    private const int RawFirstWriteValue = 601;

    /// <summary>The raw second write value.</summary>
    private const int RawSecondWriteValue = 602;

    /// <summary>The asynchronous first write value.</summary>
    private const int AsyncFirstWriteValue = 701;

    /// <summary>The asynchronous second write value.</summary>
    private const int AsyncSecondWriteValue = 702;

    /// <summary>The resilient first write value.</summary>
    private const int ResilientFirstWriteValue = 801;

    /// <summary>The resilient second write value.</summary>
    private const int ResilientSecondWriteValue = 802;

    /// <summary>The logical raw write value.</summary>
    private const int LogicalRawWriteValue = 901;

    /// <summary>The logical resilient write value.</summary>
    private const int LogicalResilientWriteValue = 902;

    /// <summary>The logical asynchronous write value.</summary>
    private const int LogicalAsyncWriteValue = 903;

    /// <summary>The standard batch variable order.</summary>
    private static readonly string[] BulkVariables = [BulkFirstVariable, BulkSecondVariable];

    /// <summary>Proves raw S7 batch bridges use the driver's batch read and write behavior.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RawBatchBridge_PublishesAndWritesThroughAdvancedBatchApisAsync()
    {
        using var plc = CreateBulkS7();
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);

        var result = await clients
            .PublishS7PlcTags(
                RawBatchPublishTopic,
                plc,
                0,
                FormatBatchValues,
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(RawBatchPublishTopic);
        await Assert.That(client.PublishedMessages[0].ConvertPayloadToString()).Contains("50");

        using var subscription = clients.SubscribeS7PlcTags(
            RawBatchSubscribeTopic,
            plc,
            static _ => CreateBatchValues(RawFirstWriteValue, RawSecondWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        await client.SimulateMessageReceivedAsync(RawBatchSubscribeTopic, IgnoredPayload);
        await plc.WaitForWriteCountAsync(BatchWriteCount);

        await Assert.That(plc.GetValue<int>(BulkFirstVariable)).IsEqualTo(RawFirstWriteValue);
        await Assert.That(plc.GetValue<int>(BulkSecondVariable)).IsEqualTo(RawSecondWriteValue);
    }

    /// <summary>Proves asynchronous S7 batch bridge wrappers forward to the same batch behavior.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task AsyncBatchBridge_PublishesAndWritesThroughAdvancedBatchApisAsync()
    {
        using var plc = CreateBulkS7();
        using var client = new MockMqttClient();
        var clients = SignalAsync.Return<IMqttClient>(client);

        var result = await clients
            .PublishS7PlcTags(
                AsyncBatchPublishTopic,
                plc,
                0,
                FormatBatchValues,
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(AsyncBatchPublishTopic);
        await Assert.That(client.PublishedMessages[0].ConvertPayloadToString()).Contains("50");

        using var subscription = clients.SubscribeS7PlcTags(
            AsyncBatchSubscribeTopic,
            plc,
            static _ => CreateBatchValues(AsyncFirstWriteValue, AsyncSecondWriteValue),
            CancellationToken.None);
        await client.SimulateMessageReceivedAsync(AsyncBatchSubscribeTopic, IgnoredPayload);
        await plc.WaitForWriteCountAsync(BatchWriteCount);

        await Assert.That(plc.GetValue<int>(BulkFirstVariable)).IsEqualTo(AsyncFirstWriteValue);
        await Assert.That(plc.GetValue<int>(BulkSecondVariable)).IsEqualTo(AsyncSecondWriteValue);
    }

    /// <summary>Proves resilient S7 batch bridges publish and subscribe through a real broker.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ResilientBatchBridge_PublishesAndWritesThroughLiveBrokerAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var plc = CreateBulkS7();
        await using var resilient = await LiveResilientSource.StartAsync(broker);
        await using var probe = await broker.SubscribeProbeAsync(ResilientBatchPublishTopic);

        var resultTask = resilient.Source
            .PublishS7PlcTags(
                ResilientBatchPublishTopic,
                plc,
                0,
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains(BulkVariablePrefix);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains("50");

        using var readinessRegistration = resilient.RegisterSubscriptionReadiness(ResilientBatchSubscribeTopic, out var readiness);
        using var subscription = resilient.Source.SubscribeS7PlcTags(
            ResilientBatchSubscribeTopic,
            plc,
            static _ => CreateBatchValues(ResilientFirstWriteValue, ResilientSecondWriteValue),
            CancellationToken.None);
        await readiness.WaitAsync(Timeout);
        _ = await PublishS7BulkProbeAsync(broker, ResilientBatchSubscribeTopic, IgnoredPayload);
        await plc.WaitForWriteCountAsync(BatchWriteCount);

        await Assert.That(plc.GetValue<int>(BulkFirstVariable)).IsEqualTo(ResilientFirstWriteValue);
        await Assert.That(plc.GetValue<int>(BulkSecondVariable)).IsEqualTo(ResilientSecondWriteValue);
    }

    /// <summary>Proves S7 logical bulk bridges publish and subscribe for raw, resilient, and async clients.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task LogicalBulkBridges_PublishAndWriteMultipleTagsAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var plc = CreateBulkS7();
        using var logicalTags = CreateLogicalTags(plc);
        await using var resilient = await LiveResilientSource.StartAsync(broker);

        await AssertLogicalRawPublishAsync(broker, logicalTags);
        await AssertLogicalResilientPublishAsync(broker, resilient, logicalTags);
        await AssertLogicalResilientFormattedPublishAsync(broker, resilient, logicalTags);
        await AssertLogicalAsyncPublishAsync(broker, logicalTags);

        using var rawSubscription = broker.Bridge.SubscribeS7LogicalTags(
            LogicalRawSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalValues(LogicalRawWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        await EnsureRawSubscriptionAsync(broker.BridgeClient, LogicalRawSubscribeTopic);
        _ = await PublishS7BulkProbeAsync(broker, LogicalRawSubscribeTopic, IgnoredPayload);
        await plc.WaitForValueAsync(BulkFirstVariable, LogicalRawWriteValue);

        using var readinessRegistration = resilient.RegisterSubscriptionReadiness(LogicalResilientSubscribeTopic, out var readiness);
        using var resilientSubscription = resilient.Source.SubscribeS7LogicalTags(
            LogicalResilientSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalValues(LogicalResilientWriteValue),
            CancellationToken.None);
        await readiness.WaitAsync(Timeout);
        _ = await PublishS7BulkProbeAsync(broker, LogicalResilientSubscribeTopic, IgnoredPayload);
        await plc.WaitForValueAsync(BulkFirstVariable, LogicalResilientWriteValue);

        using var asyncSubscription = SignalAsync.Return(broker.BridgeClient).SubscribeS7LogicalTags(
            LogicalAsyncSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalValues(LogicalAsyncWriteValue),
            CancellationToken.None);
        await EnsureRawSubscriptionAsync(broker.BridgeClient, LogicalAsyncSubscribeTopic);
        _ = await PublishS7BulkProbeAsync(broker, LogicalAsyncSubscribeTopic, IgnoredPayload);
        await plc.WaitForValueAsync(BulkFirstVariable, LogicalAsyncWriteValue);
    }

    /// <summary>Exercises validation, parser-failure, cancellation, and disposal branches for S7 bulk bridges.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBridge_ValidationErrorsCancellationAndDisposalAreHandledAsync()
    {
        using var plc = CreateBulkS7();
        using var client = new MockMqttClient();
        using var logicalTags = CreateLogicalTags(plc);
        var raw = Signal.Emit<IMqttClient>(client);
        var resilient = Signal.Emit<IResilientMqttClient>(new MockResilientMqttClient());
        var errors = new List<Exception>();
        var errorArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await Assert.That(() => raw.PublishS7PlcTags(BatchParserFailureTopic, plc, 0, static values => string.Empty))
            .Throws<ArgumentException>();
        await Assert.That(() => raw.PublishS7LogicalTags(LogicalRawPublishTopic, logicalTags))
            .Throws<ArgumentException>();
        await Assert.That(() => resilient.PublishS7PlcTags(BatchParserFailureTopic, null!, 0, BulkFirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishS7LogicalTags(LogicalResilientPublishTopic, null!, BulkFirstVariable))
            .Throws<ArgumentNullException>();

        using var parserSubscription = raw.SubscribeS7PlcTags<int>(
            BatchParserFailureTopic,
            plc,
            static _ => throw new FormatException("bad batch"),
            error =>
            {
                errors.Add(error);
                _ = errorArrived.TrySetResult();
            },
            CancellationToken.None);
        await client.SimulateMessageReceivedAsync(BatchParserFailureTopic, "bad");
        await errorArrived.Task.WaitAsync(Timeout);
        await Assert.That(errors[0]).IsTypeOf<FormatException>();

        using var cancellation = new CancellationTokenSource();
        using var cancelled = raw.SubscribeS7PlcTags(
            BatchCancellationTopic,
            plc,
            static _ => CreateBatchValues(RawFirstWriteValue, RawSecondWriteValue),
            OnUnexpectedError,
            cancellation.Token);
        await cancellation.CancelAsync();
        await client.SimulateMessageReceivedAsync(BatchCancellationTopic, IgnoredPayload);
        await DrainS7BulkContinuationsAsync();
        await Assert.That(plc.GetValue<int>(BulkFirstVariable)).IsEqualTo(BulkInitialFirstValue);

        var disposed = raw.SubscribeS7LogicalTags(
            LogicalRawSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalValues(LogicalRawWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        disposed.Dispose();
        disposed.Dispose();
        await client.SimulateMessageReceivedAsync(LogicalRawSubscribeTopic, IgnoredPayload);
        await DrainS7BulkContinuationsAsync();
        await Assert.That(plc.GetValue<int>(BulkFirstVariable)).IsEqualTo(BulkInitialFirstValue);
    }

    /// <summary>Exercises default wrapper overloads that delegate through public S7 bulk APIs.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBridge_DefaultWrappersAndObserverBranchesAreReachableAsync()
    {
        using var plc = CreateBulkS7();
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        using var logicalTags = CreateLogicalTags(plc);
        var raw = Signal.Emit<IMqttClient>(rawClient);
        var resilient = Signal.Emit<IResilientMqttClient>(resilientClient);
        var asyncRaw = SignalAsync.Return<IMqttClient>(rawClient);
        var asyncResilient = SignalAsync.Return<IResilientMqttClient>(resilientClient);

        await AssertRawDefaultWrapperAsync(raw, rawClient, plc);
        AssertSynchronousResilientSubscribeWrappers(resilient, plc, logicalTags);
        AssertAsynchronousRawWrappers(asyncRaw, plc, logicalTags);
        AssertAsynchronousResilientWrappers(asyncResilient, plc, logicalTags);

        await Assert.That(rawClient.PublishedMessages).IsNotEmpty();
    }

    /// <summary>Exercises internal ordered observer lifecycle branches through its public interfaces.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBridge_InternalObserverLifecycleBranchesAreReachableAsync()
    {
        using var plc = CreateBulkS7();

        var completed = CreateReflectedBatchObserver(plc, OnUnexpectedError);
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)completed).OnCompleted();

        var errored = CreateReflectedBatchObserver(plc, OnUnexpectedError);
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)errored).OnError(
            new InvalidOperationException("observer failure"));
        await Assert.That(() =>
                ((IObserver<MqttApplicationMessageReceivedEventArgs>)errored).OnError(null!))
            .Throws<ArgumentNullException>();

        var disposed = CreateReflectedBatchObserver(plc, OnUnexpectedError);
        ((IDisposable)disposed).Dispose();
        var disposable = new CountingDisposable();
        InvokeReflectedAttach(disposed, disposable);
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)disposed).OnNext(CreateS7BulkReceivedMessage("ignored"));
        await Assert.That(disposable.DisposeCount).IsEqualTo(1);

        var callbackFailure = CreateReflectedBatchObserver(
            plc,
            static _ => throw new InvalidOperationException("callback failed"),
            static _ => throw new FormatException("bad callback"));
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)callbackFailure).OnNext(CreateS7BulkReceivedMessage("bad"));
        await DrainS7BulkContinuationsAsync();

        var protectedDispose = CreateReflectedBatchObserver(plc, null);
        InvokeReflectedDispose(protectedDispose, false);
        ((IDisposable)protectedDispose).Dispose();
    }

    /// <summary>Exercises null logical payload formatting and null observer error callback branches.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBridge_NullFormattingAndNullErrorCallbackBranchesAreReachableAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var plc = CreateBulkS7();
        using var logicalTags = CreateLogicalTags(plc);
        await using var resilient = await LiveResilientSource.StartAsync(broker);
        plc.Value<object?>(BulkFirstVariable, null);

        await using var probe = await broker.SubscribeProbeAsync("s7/logical/null-format");
        var resultTask = resilient.Source
            .PublishS7LogicalTags("s7/logical/null-format", logicalTags, BulkFirstVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;
        await Assert.That(result.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEmpty();
        await Assert.That(() =>
                resilient.Source.PublishS7LogicalTags("s7/logical/null-tags", logicalTags, (string[])null!))
            .Throws<ArgumentNullException>();

        var nullErrorCallback = CreateReflectedBatchObserver(
            plc,
            null,
            static _ => throw new FormatException("no callback"));
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)nullErrorCallback).OnNext(
            CreateS7BulkReceivedMessage("bad"));
        await DrainS7BulkContinuationsAsync();
    }

    /// <summary>Exercises single-tag S7 publish formatting for null values and null string conversions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SingleTagPublishers_FormatNullValuesAsEmptyPayloadsAsync()
    {
        using var nullPlc = CreateBulkS7();
        var nullTag = new LogicalTagKey<string>("s7.single.null");
        await AssertSingleTagRawPublishPayloadAsync(nullPlc, nullTag, "s7/single/raw/null", string.Empty);
        await AssertSingleTagResilientPublishAsync(nullPlc, nullTag, "s7/single/resilient/null");
    }

    /// <summary>Exercises the default raw-client batch publish wrapper.</summary>
    /// <param name="raw">The raw MQTT client source.</param>
    /// <param name="rawClient">The mock MQTT client.</param>
    /// <param name="plc">The S7 seam.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertRawDefaultWrapperAsync(
        IObservable<IMqttClient> raw,
        MockMqttClient rawClient,
        BulkRecordingS7 plc)
    {
        _ = await raw.PublishS7PlcTags("s7/bulk/default/raw", plc, 0, BulkFirstVariable).FirstAsync(Timeout);
        await Assert.That(rawClient.PublishedMessages[0].ConvertPayloadToString()).Contains("50");
    }

    /// <summary>Exercises synchronous resilient subscribe wrappers with explicit error callbacks.</summary>
    /// <param name="resilient">The resilient MQTT client source.</param>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    private static void AssertSynchronousResilientSubscribeWrappers(
        IObservable<IResilientMqttClient> resilient,
        BulkRecordingS7 plc,
        S7LogicalTagClient logicalTags)
    {
        using var resilientBatch = resilient.SubscribeS7PlcTags(
            "s7/bulk/default/resilient/subscribe",
            plc,
            static _ => CreateBatchValues(RawFirstWriteValue, RawSecondWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        using var resilientLogical = resilient.SubscribeS7LogicalTags(
            "s7/logical/default/resilient/subscribe",
            logicalTags,
            static _ => CreateLogicalValues(LogicalRawWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
    }

    /// <summary>Exercises asynchronous raw-client wrapper overloads.</summary>
    /// <param name="asyncRaw">The asynchronous raw MQTT client source.</param>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    private static void AssertAsynchronousRawWrappers(
        IObservableAsync<IMqttClient> asyncRaw,
        BulkRecordingS7 plc,
        S7LogicalTagClient logicalTags)
    {
        _ = asyncRaw.PublishS7PlcTags("s7/bulk/default/async/raw", plc, 0, BulkFirstVariable);
        using var asyncRawBatch = asyncRaw.SubscribeS7PlcTags(
            "s7/bulk/default/async/raw/subscribe",
            plc,
            static _ => CreateBatchValues(AsyncFirstWriteValue, AsyncSecondWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        _ = asyncRaw.PublishS7LogicalTags("s7/logical/default/async/raw", logicalTags, BulkFirstVariable);
        using var asyncRawLogical = asyncRaw.SubscribeS7LogicalTags(
            "s7/logical/default/async/raw/subscribe",
            logicalTags,
            static _ => CreateLogicalValues(LogicalAsyncWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
    }

    /// <summary>Exercises asynchronous resilient-client wrapper overloads.</summary>
    /// <param name="asyncResilient">The asynchronous resilient MQTT client source.</param>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    private static void AssertAsynchronousResilientWrappers(
        IObservableAsync<IResilientMqttClient> asyncResilient,
        BulkRecordingS7 plc,
        S7LogicalTagClient logicalTags)
    {
        _ = asyncResilient.PublishS7PlcTags("s7/bulk/default/async/resilient", plc, 0, BulkFirstVariable);
        _ = asyncResilient.PublishS7PlcTags(
            "s7/bulk/formatted/async/resilient",
            plc,
            0,
            FormatBatchValues,
            BulkFirstVariable);
        using var asyncResilientBatch = asyncResilient.SubscribeS7PlcTags(
            "s7/bulk/default/async/resilient/subscribe",
            plc,
            static _ => CreateBatchValues(ResilientFirstWriteValue, ResilientSecondWriteValue),
            CancellationToken.None);
        using var asyncResilientBatchWithError = asyncResilient.SubscribeS7PlcTags(
            "s7/bulk/default/async/resilient/subscribe/error",
            plc,
            static _ => CreateBatchValues(ResilientFirstWriteValue, ResilientSecondWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
        _ = asyncResilient.PublishS7LogicalTags("s7/logical/default/async/resilient", logicalTags, BulkFirstVariable);
        _ = asyncResilient.PublishS7LogicalTags(
            "s7/logical/formatted/async/resilient",
            logicalTags,
            static value => Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty,
            BulkFirstVariable);
        using var asyncResilientLogical = asyncResilient.SubscribeS7LogicalTags(
            "s7/logical/default/async/resilient/subscribe",
            logicalTags,
            static _ => CreateLogicalValues(LogicalResilientWriteValue),
            CancellationToken.None);
        using var asyncResilientLogicalWithError = asyncResilient.SubscribeS7LogicalTags(
            "s7/logical/default/async/resilient/subscribe/error",
            logicalTags,
            static _ => CreateLogicalValues(LogicalResilientWriteValue),
            OnUnexpectedError,
            CancellationToken.None);
    }

    /// <summary>Creates the internal S7 batch observer for lifecycle branch coverage.</summary>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="parser">The optional parser override.</param>
    /// <returns>The observer instance.</returns>
    private static object CreateReflectedBatchObserver(
        BulkRecordingS7 plc,
        Action<Exception>? onError,
        Func<string, IReadOnlyDictionary<string, int>>? parser = null)
    {
        var observerType = typeof(S7PlcBulkMqttExtensions)
            .GetNestedType("S7BatchWriteObserver`1", System.Reflection.BindingFlags.NonPublic)?
            .MakeGenericType(typeof(int))
            ?? throw new InvalidOperationException("The S7 batch observer type was not found.");
        return Activator.CreateInstance(
            observerType,
            plc,
            parser ?? (static _ => CreateBatchValues(RawFirstWriteValue, RawSecondWriteValue)),
            onError,
            CancellationToken.None)
            ?? throw new InvalidOperationException("The S7 batch observer could not be created.");
    }

    /// <summary>Invokes the internal observer attach method.</summary>
    /// <param name="observer">The observer instance.</param>
    /// <param name="disposable">The disposable to attach.</param>
    private static void InvokeReflectedAttach(object observer, IDisposable disposable)
    {
        var attach = observer.GetType().GetMethod(
            "Attach",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        _ = (attach ?? throw new InvalidOperationException("The S7 observer attach method was not found."))
            .Invoke(observer, [disposable]);
    }

    /// <summary>Invokes the protected dispose overload on the internal observer base.</summary>
    /// <param name="observer">The observer instance.</param>
    /// <param name="disposing">Whether managed resources are being disposed.</param>
    private static void InvokeReflectedDispose(object observer, bool disposing)
    {
        var dispose = observer.GetType().BaseType?.GetMethod(
            "Dispose",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        _ = (dispose ?? throw new InvalidOperationException("The S7 observer dispose method was not found."))
            .Invoke(observer, [disposing]);
    }

    /// <summary>Publishes one probe payload through the real broker.</summary>
    /// <param name="broker">The live broker fixture.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The UTF-8 payload.</param>
    /// <returns>The real MQTT publish result.</returns>
    private static Task<MqttClientPublishResult> PublishS7BulkProbeAsync(
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

    /// <summary>Creates MQTT received-message arguments for direct observer branch tests.</summary>
    /// <param name="payload">The UTF-8 payload.</param>
    /// <returns>The MQTT received-message arguments.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateS7BulkReceivedMessage(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var sequence = new ReadOnlySequence<byte>(bytes);
        var message = new MqttApplicationMessage
        {
            Topic = "s7/bulk/direct-observer",
            Payload = sequence,
        };
        var packet = new MqttPublishPacket
        {
            Topic = message.Topic,
            Payload = sequence,
        };
        return new("task8-s7", message, packet, null);
    }

    /// <summary>Allows queued asynchronous observer callbacks to complete without sleeping.</summary>
    /// <returns>A task representing continuation draining.</returns>
    private static async Task DrainS7BulkContinuationsAsync()
    {
        const int continuationCount = 32;
        for (var index = 0; index < continuationCount; index++)
        {
            await Task.Yield();
        }
    }

    /// <summary>Publishes logical tags through the raw-client bridge.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertLogicalRawPublishAsync(LiveMqttBroker broker, S7LogicalTagClient logicalTags)
    {
        await using var probe = await broker.SubscribeProbeAsync(LogicalRawPublishTopic);
        var resultTask = broker.Bridge
            .PublishS7LogicalTags(
                LogicalRawPublishTopic,
                logicalTags,
                static value => $"{value.TagName}:{Convert.ToString(value.Value, CultureInfo.InvariantCulture)}",
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains(BulkVariablePrefix);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains("50");
    }

    /// <summary>Publishes logical tags through the resilient-client bridge.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="resilient">The resilient client source.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertLogicalResilientPublishAsync(
        LiveMqttBroker broker,
        LiveResilientSource resilient,
        S7LogicalTagClient logicalTags)
    {
        await using var probe = await broker.SubscribeProbeAsync(LogicalResilientPublishTopic);
        var resultTask = resilient.Source
            .PublishS7LogicalTags(LogicalResilientPublishTopic, logicalTags, BulkFirstVariable, BulkSecondVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains("50");
    }

    /// <summary>Publishes logical tags through the resilient-client bridge with a custom formatter.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="resilient">The resilient client source.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertLogicalResilientFormattedPublishAsync(
        LiveMqttBroker broker,
        LiveResilientSource resilient,
        S7LogicalTagClient logicalTags)
    {
        const string topic = "s7/logical/resilient/formatted/publish";
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = resilient.Source
            .PublishS7LogicalTags(
                topic,
                logicalTags,
                static value => $"{value.TagName}:{Convert.ToString(value.Value, CultureInfo.InvariantCulture)}",
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains(BulkVariablePrefix);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains("50");
    }

    /// <summary>Publishes logical tags through the asynchronous raw-client bridge.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertLogicalAsyncPublishAsync(LiveMqttBroker broker, S7LogicalTagClient logicalTags)
    {
        await using var probe = await broker.SubscribeProbeAsync(LogicalAsyncPublishTopic);
        var resultTask = SignalAsync.Return(broker.BridgeClient)
            .PublishS7LogicalTags(
                LogicalAsyncPublishTopic,
                logicalTags,
                static value => Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty,
                BulkFirstVariable,
                BulkSecondVariable)
            .FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).Contains("50");
    }

    /// <summary>Publishes a single S7 tag through the raw-client bridge.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="tag">The logical tag key.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="expectedPayload">The expected UTF-8 payload.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertSingleTagRawPublishPayloadAsync<T>(
        IRxS7 plc,
        LogicalTagKey<T> tag,
        string topic,
        string expectedPayload)
    {
        using var client = new MockMqttClient();

        _ = await Signal.Emit<IMqttClient>(client)
            .PublishS7PlcTag(topic, tag, plc)
            .FirstAsync(Timeout);

        await Assert.That(client.PublishedMessages.Count).IsEqualTo(1);
        await Assert.That(Encoding.UTF8.GetString(client.PublishedMessages[0].Payload)).IsEqualTo(expectedPayload);
    }

    /// <summary>Publishes a single S7 tag through the resilient-client bridge.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="plc">The S7 seam.</param>
    /// <param name="tag">The logical tag key.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertSingleTagResilientPublishAsync<T>(
        IRxS7 plc,
        LogicalTagKey<T> tag,
        string topic)
    {
        using var client = new MockResilientMqttClient();

        var resultTask = Signal.Emit<IResilientMqttClient>(client)
            .PublishS7PlcTag(topic, tag, plc)
            .FirstAsync(Timeout);
        await Task.Yield();
        await client.SimulateApplicationMessageProcessedAsync();

        var result = await resultTask;
        await Assert.That(result.Exception).IsNull();
    }

    /// <summary>Creates a deterministic in-memory S7 seam for bulk tests.</summary>
    /// <returns>The configured S7 seam.</returns>
    private static BulkRecordingS7 CreateBulkS7()
    {
        var plc = new BulkRecordingS7();
        plc.Value(BulkFirstVariable, BulkInitialFirstValue);
        plc.Value(BulkSecondVariable, BulkInitialSecondValue);
        plc.ResetWriteTracking();
        return plc;
    }

    /// <summary>Creates an S7 logical tag client over the in-memory S7 seam.</summary>
    /// <param name="plc">The S7 seam.</param>
    /// <returns>The configured logical tag client.</returns>
    private static S7LogicalTagClient CreateLogicalTags(BulkRecordingS7 plc)
    {
        var catalog = new LogicalTagCatalog();
        var logicalTags = new S7LogicalTagClient(plc, catalog, TimeProvider.System);
        logicalTags.RegisterTag(new(BulkFirstVariable, BulkFirstAddress, BulkDataType));
        logicalTags.RegisterTag(new(BulkSecondVariable, BulkSecondAddress, BulkDataType));
        return logicalTags;
    }

    /// <summary>Formats an S7 batch update without assuming all observed variables have emitted yet.</summary>
    /// <param name="values">The current batch snapshot.</param>
    /// <returns>The formatted payload.</returns>
    private static string FormatBatchValues(IReadOnlyDictionary<string, int> values)
    {
        var payload = new StringBuilder();
        foreach (var variable in BulkVariables)
        {
            if (!values.TryGetValue(variable, out var value))
            {
                continue;
            }

            if (payload.Length > 0)
            {
                _ = payload.Append(',');
            }

            _ = payload.Append(variable);
            _ = payload.Append('=');
            _ = payload.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        return payload.ToString();
    }

    /// <summary>Creates batch values for the two standard variables.</summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <returns>The batch values.</returns>
    private static Dictionary<string, int> CreateBatchValues(int first, int second) =>
        new Dictionary<string, int>
        {
            [BulkFirstVariable] = first,
            [BulkSecondVariable] = second,
        };

    /// <summary>Creates logical values for the two standard variables.</summary>
    /// <param name="first">The first logical value.</param>
    /// <returns>The logical values.</returns>
    private static IReadOnlyCollection<LogicalTagValue> CreateLogicalValues(int first) =>
        [
            new(BulkFirstVariable, first, DateTimeOffset.UnixEpoch),
            new(BulkSecondVariable, first + 1, DateTimeOffset.UnixEpoch),
        ];

    /// <summary>Fails a test when an unexpected asynchronous bridge error occurs.</summary>
    /// <param name="error">The unexpected error.</param>
    private static void OnUnexpectedError(Exception error) =>
        throw new InvalidOperationException("Unexpected S7 bulk bridge error.", error);

    /// <summary>Provides deterministic S7 reads, writes, and observations for bulk bridge tests.</summary>
    private sealed class BulkRecordingS7 : IRxS7
    {
        /// <summary>Stores values by variable name.</summary>
        private readonly Dictionary<string, object?> _values = [];

        /// <summary>Stores writes by variable name.</summary>
        private readonly List<string?> _writes = [];

        /// <summary>Signals the next write.</summary>
        private TaskCompletionSource _writeArrived = NewWriteSignal();

        /// <inheritdoc/>
        public string IP => IPAddress.Loopback.ToString();

        /// <inheritdoc/>
        public IObservable<bool> IsConnected => Signal.Emit(true);

        /// <inheritdoc/>
        public bool IsConnectedValue => true;

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public IObservable<string> LastError => Signal.None<string>();

        /// <inheritdoc/>
        public IObservable<ErrorCode> LastErrorCode => Signal.None<ErrorCode>();

        /// <inheritdoc/>
        public IObservable<Tag?> ObserveAll => new SnapshotObservable<Tag?>(SnapshotTags());

        /// <inheritdoc/>
        public CpuType PLCType => CpuType.S71500;

        /// <inheritdoc/>
        public short Rack => 0;

        /// <inheritdoc/>
        public short Slot => 1;

        /// <inheritdoc/>
        public IObservable<bool> IsPaused => Signal.Emit(false);

        /// <inheritdoc/>
        public IObservable<string> Status => Signal.None<string>();

        /// <inheritdoc/>
        public Tags TagList { get; } = [];

        /// <inheritdoc/>
        public bool ShowWatchDogWriting { get; set; }

        /// <inheritdoc/>
        public string? WatchDogAddress => null;

        /// <inheritdoc/>
        public ushort WatchDogValueToWrite { get; set; }

        /// <inheritdoc/>
        public int WatchDogWritingTime => 0;

        /// <inheritdoc/>
        public IObservable<long> ReadTime => Signal.None<long>();

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;

        /// <inheritdoc/>
        public IObservable<T?> Observe<T>(LogicalTagKey<T> tag) =>
            Signal.Emit((T?)GetValue<T>(tag.Name));

        /// <inheritdoc/>
        public Task<T?> ReadAsync<T>(LogicalTagKey<T> tag) =>
            Task.FromResult((T?)GetValue<T>(tag.Name));

        /// <inheritdoc/>
        public Task<T?> ReadAsync<T>(LogicalTagKey<T> tag, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ReadAsync(tag);
        }

        /// <inheritdoc/>
        public void Value<T>(string? variable, T? value)
        {
            TaskCompletionSource writeArrived;
            lock (_values)
            {
                if (variable is not null)
                {
                    _values[variable] = value;
                    if (TagList[variable] is Tag tag)
                    {
                        tag.Value = value;
                    }
                    else
                    {
                        TagList.Add(new(variable, variable, value!, typeof(T)));
                    }
                }

                _writes.Add(variable);
                writeArrived = _writeArrived;
                _writeArrived = NewWriteSignal();
            }

            _ = writeArrived.TrySetResult();
        }

        /// <inheritdoc/>
        public IObservable<string[]> GetCpuInfo()
        {
            string[] values = [];
            return Signal.Emit(values);
        }

        /// <summary>Gets a stored value.</summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="name">The variable name.</param>
        /// <returns>The stored value.</returns>
        internal T? GetValue<T>(string name)
        {
            lock (_values)
            {
                return _values.TryGetValue(name, out var value) && value is T typed ? typed : default;
            }
        }

        /// <summary>Resets write tracking after initial values are configured.</summary>
        internal void ResetWriteTracking()
        {
            lock (_values)
            {
                _writes.Clear();
                _writeArrived = NewWriteSignal();
            }
        }

        /// <summary>Waits until the requested number of writes has arrived.</summary>
        /// <param name="count">The expected write count.</param>
        /// <returns>A task that completes after the writes arrive.</returns>
        internal async Task WaitForWriteCountAsync(int count)
        {
            while (true)
            {
                Task waitTask;
                lock (_values)
                {
                    if (_writes.Count >= count)
                    {
                        return;
                    }

                    waitTask = _writeArrived.Task;
                }

                await waitTask.WaitAsync(Timeout).ConfigureAwait(false);
            }
        }

        /// <summary>Waits until a variable has the expected value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="name">The variable name.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns>A task that completes when the value matches.</returns>
        internal async Task WaitForValueAsync<T>(string name, T expected)
        {
            using var cancellation = new CancellationTokenSource(Timeout);
            while (!EqualityComparer<T?>.Default.Equals(GetValue<T>(name), expected))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        /// <summary>Creates a write signal.</summary>
        /// <returns>A new task completion source.</returns>
        private static TaskCompletionSource NewWriteSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Snapshots the currently registered S7 tags.</summary>
        /// <returns>The current S7 tags.</returns>
        private List<Tag?> SnapshotTags()
        {
            lock (_values)
            {
                var tags = new List<Tag?>(TagList.Count);
                foreach (var item in TagList.Values)
                {
                    if (item is Tag tag)
                    {
                        tags.Add(tag);
                    }
                }

                return tags;
            }
        }
    }

    /// <summary>Synchronously emits a deterministic snapshot of observable values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values to emit.</param>
    private sealed class SnapshotObservable<T>(IReadOnlyList<T> values) : IObservable<T>
    {
        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            foreach (var value in values)
            {
                observer.OnNext(value);
            }

            observer.OnCompleted();
            return EmptyDisposable.Instance;
        }
    }

    /// <summary>Provides a no-op subscription lifetime.</summary>
    private sealed class EmptyDisposable : IDisposable
    {
        /// <summary>The shared no-op disposable.</summary>
        internal static readonly EmptyDisposable Instance = new();

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }

    /// <summary>Counts dispose calls from observer attach/dispose tests.</summary>
    private sealed class CountingDisposable : IDisposable
    {
        /// <summary>Gets the number of dispose calls.</summary>
        internal int DisposeCount { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => DisposeCount++;
    }
}
