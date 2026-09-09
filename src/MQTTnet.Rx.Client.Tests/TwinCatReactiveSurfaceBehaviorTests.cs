// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Globalization;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
#else
using CP.Collections;
#endif
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Reactive;
#else
using IoT.Driver.TwinCATRx;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Core.Reactive;
#else
using IoT.Driver.TwinCATRx.Core;
#endif
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive;
using MQTTnet.Rx.TwinCAT.Reactive;
#else
using MQTTnet.Rx.Client;
using MQTTnet.Rx.TwinCAT;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.Reactive.TwinCatRxExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Reactive.Create;
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.Reactive.CreateExtensions;
using TwinCatStructureExtensions = IoT.Driver.TwinCATRx.Reactive.TwinCatRxExtensions;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.TwinCatRxExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.CreateExtensions;
using TwinCatStructureExtensions = IoT.Driver.TwinCATRx.TwinCatRxExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises TwinCAT reactive bridge behavior against in-memory clients.</summary>
public sealed class TwinCatReactiveSurfaceBehaviorTests
{
    /// <summary>The ADS scalar symbol used by behavioral tests.</summary>
    private const string ScalarVariable = ".Main.Behavior";

    /// <summary>The structure member used by behavioral tests.</summary>
    private const string MemberName = "Value";

    /// <summary>The structure array member used by behavioral tests.</summary>
    private const string ArrayMemberName = "Values";

    /// <summary>The structure object member used by behavioral tests.</summary>
    private const string ObjectMemberName = "Object";

    /// <summary>The decimal structure member used by formatter branch tests.</summary>
    private const string DecimalMemberName = "Decimal";

    /// <summary>The logical tag backed by <see cref="ScalarVariable"/>.</summary>
    private const string LogicalTagName = "Behavior.Value";

    /// <summary>The missing logical tag used by error publication tests.</summary>
    private const string MissingLogicalTagName = "Behavior.Missing";

    /// <summary>The correlation id used by read publisher tests.</summary>
    private const string ReadCorrelationId = "read-id";

    /// <summary>The private read helper method name.</summary>
    private const string ReadOnceMethodName = "ReadOnce";

    /// <summary>The simulated TwinCAT runtime port.</summary>
    private const int TwinCatPort = 851;

    /// <summary>The ADS array symbol used by async wrapper tests.</summary>
    private const string ArrayVariable = ".Main.Array";

    /// <summary>The ADS structured symbol used by payload formatter tests.</summary>
    private const string StructVariable = ".Main.Struct";

    /// <summary>The ADS structured symbol used by write-through tests.</summary>
    private const string WritableStructVariable = ".Main.WritableStruct";

    /// <summary>The ADS string symbol used by payload formatter tests.</summary>
    private const string StringVariable = ".Main.String";

    /// <summary>The ADS Boolean symbol used by scalar compatibility tests.</summary>
    private const string BoolVariable = ".Main.Bool";

    /// <summary>The ADS character symbol used by scalar compatibility tests.</summary>
    private const string CharVariable = ".Main.Char";

    /// <summary>The configured scalar value.</summary>
    private const int InitialScalarValue = 42;

    /// <summary>The minimum structure publications emitted by current-state and mutation notifications.</summary>
    private const int MinimumStructurePublications = 2;

    /// <summary>The number of raw structure write subscriptions expected before simulating inbound payloads.</summary>
    private const int RawStructureSubscriptionCount = 2;

    /// <summary>The number of resilient write subscriptions expected before simulating inbound payloads.</summary>
    private const int ResilientWriteSubscriptionCount = 5;

    /// <summary>The value published from structure observation.</summary>
    private const int StructureObservedValue = 77;

    /// <summary>The expected JSON payload for an array structure member.</summary>
    private const string StructureArrayPayload = "[4,5,6]";

    /// <summary>The expected JSON payload for the registered ADS array.</summary>
    private const string InitialArrayPayload = "[1,2,3]";

    /// <summary>The expected JSON payload for the registered ADS public-field structure.</summary>
    private const string InitialStructPayload = "{\"Item1\":7,\"Item2\":[1,2,3],\"Item3\":{\"Item1\":11,\"Item2\":12}}";

    /// <summary>The expected payload for the registered scalar value.</summary>
    private const string InitialScalarPayload = "42";

    /// <summary>The expected payload for the registered string value.</summary>
    private const string InitialStringPayload = "ready";

    /// <summary>The expected payload for the registered Boolean value.</summary>
    private const string InitialBoolPayload = "True";

    /// <summary>The expected payload for the registered character value.</summary>
    private const string InitialCharPayload = "A";

    /// <summary>The payload emitted when a logical tag read fails.</summary>
    private const string LogicalReadErrorPayload = "error";

    /// <summary>The resilient logical read topic used for delegate-cache coverage.</summary>
    private const string ResilientLogicalReadWarmTopic = "twincat/resilient/logical/read-warm";

    /// <summary>The expected JSON payload for an object structure member.</summary>
    private const string StructureObjectPayload = "{\"alpha\":2,\"beta\":3}";

    /// <summary>The expected payload for a decimal structure member.</summary>
    private const string StructureDecimalPayload = "1.5";

    /// <summary>The value written directly into a structure member.</summary>
    private const int StructureMemberWriteValue = 88;

    /// <summary>The value written through structure clone semantics.</summary>
    private const int StructureCloneWriteValue = 99;

    /// <summary>The value written to a single logical tag.</summary>
    private const int LogicalSingleWriteValue = 123;

    /// <summary>The value written by logical tag bulk operations.</summary>
    private const int LogicalBulkWriteValue = 124;

    /// <summary>The value written through the correlated raw subscription.</summary>
    private const int CorrelatedWriteValue = 125;

    /// <summary>The value written through the resilient correlated subscription.</summary>
    private const int ResilientCorrelatedWriteValue = 126;

    /// <summary>The value used by the static Create hash forwarder.</summary>
    private const int StaticHashValue = 201;

    /// <summary>The value used by the static Create write forwarder.</summary>
    private const int StaticWriteValue = 202;

    /// <summary>The failed logical tag write value.</summary>
    private const int FailedLogicalWriteValue = 5;

    /// <summary>The first array value published for structured payload verification.</summary>
    private const int StructureArrayFirstValue = 4;

    /// <summary>The second array value published for structured payload verification.</summary>
    private const int StructureArraySecondValue = 5;

    /// <summary>The third array value published for structured payload verification.</summary>
    private const int StructureArrayThirdValue = 6;

    /// <summary>The first object value published for structured payload verification.</summary>
    private const int StructureObjectAlphaValue = 2;

    /// <summary>The second object value published for structured payload verification.</summary>
    private const int StructureObjectBetaValue = 3;

    /// <summary>The polling delay used while waiting for in-memory publication callbacks.</summary>
    private const int PublicationPollDelayMilliseconds = 10;

    /// <summary>The value written through the resilient single logical tag subscription.</summary>
    private const int ResilientSingleLogicalWriteValue = 127;

    /// <summary>The decimal value published by formatter branch tests.</summary>
    private const decimal StructureDecimalValue = 1.5M;

    /// <summary>The second array value used by wrapper tests.</summary>
    private const int InitialArraySecondValue = 2;

    /// <summary>The ADS array length requested by wrapper tests.</summary>
    private const int ArrayLength = 3;

    /// <summary>The structured status value read from a simulated ADS public-field struct.</summary>
    private const int StructStatusValue = 7;

    /// <summary>The nested structured code read from a simulated ADS public-field struct.</summary>
    private const int StructNestedCodeValue = 11;

    /// <summary>The nested structured extra value read from a simulated ADS public-field struct.</summary>
    private const int StructNestedExtraValue = 12;

    /// <summary>The registered ADS Boolean value.</summary>
    private const bool InitialBoolValue = true;

    /// <summary>The registered ADS character value.</summary>
    private const char InitialCharValue = 'A';

    /// <summary>The argument index containing a correlated read id.</summary>
    private const int ReadIdArgumentIndex = 2;

    /// <summary>The bounded wait used for in-memory observable emissions.</summary>
    private static readonly TimeSpan ObservableTimeout = TimeSpan.FromSeconds(2);

    /// <summary>The registered ADS array value.</summary>
    private static readonly int[] InitialArrayValue =
    [
        1,
        InitialArraySecondValue,
        ArrayLength,
    ];

    /// <summary>Gets the registered ADS public-field structure value.</summary>
    private static (int Status, int[] Values, (int Code, int Extra) Nested) InitialStructValue =>
        (StructStatusValue, InitialArrayValue, (StructNestedCodeValue, StructNestedExtraValue));

    /// <summary>Publishes structure observations and writes MQTT payloads into structure members.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawStructurePublishAndSubscribe_UpdateHashTableMembersAsync()
    {
        using var mqtt = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqtt);
        var structure = CreateStructureTable();

        using var structurePublish = clients.PublishTcStructMember<int>("twincat/struct/out", MemberName, structure)
            .Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        using var arrayPublish = clients.PublishTcStructMember<int[]>("twincat/struct/array", ArrayMemberName, structure)
            .Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        using var objectPublish = clients.PublishTcStructMember<IReadOnlyDictionary<string, int>>(
            "twincat/struct/object",
            ObjectMemberName,
            structure).Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        using var decimalPublish = clients.PublishTcStructMember<decimal>(
            "twincat/struct/decimal",
            DecimalMemberName,
            structure).Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        structure[MemberName] = StructureObservedValue;
        structure[ArrayMemberName] = new[]
        {
            StructureArrayFirstValue,
            StructureArraySecondValue,
            StructureArrayThirdValue,
        };
        structure[ObjectMemberName] = new Dictionary<string, int>
        {
            ["alpha"] = StructureObjectAlphaValue,
            ["beta"] = StructureObjectBetaValue,
        };
        structure[DecimalMemberName] = StructureDecimalValue;
        using var memberWrite = clients.SubscribeTcStructMember(
            "twincat/struct/member/in",
            MemberName,
            structure,
            ParsePayload);
        var cloneFactoryCalls = 0;
        using var cloneWrite = clients.SubscribeTcStructWrite(
            "twincat/struct/write/in",
            MemberName,
            structure,
            CreateCountingPayloadFactory(() => cloneFactoryCalls++));

        await WaitForSubscriptionsAsync(mqtt, RawStructureSubscriptionCount);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/struct/member/in",
            StructureMemberWriteValue.ToString(CultureInfo.InvariantCulture));
        await Assert.That(structure[MemberName]).IsEqualTo(StructureMemberWriteValue);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/struct/write/in",
            StructureCloneWriteValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(structure[MemberName]).IsEqualTo(StructureCloneWriteValue);
        await Assert.That(cloneFactoryCalls).IsEqualTo(1);
        await Assert.That(mqtt.PublishedMessages.Count).IsGreaterThanOrEqualTo(MinimumStructurePublications);
        await AssertPublishedPayloadAsync(mqtt, "twincat/struct/array", StructureArrayPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/struct/object", StructureObjectPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/struct/decimal", StructureDecimalPayload);
    }

    /// <summary>Publishes logical tag reads and writes single and bulk logical tag payloads.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawLogicalTagReadAndWrite_UseTwinCatBulkOperationsAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqtt);
        IRxTcAdsClient adsContract = ads;
        using var tags = CreateLogicalTags(ads);

        await CaptureOneAsync(clients.PublishTcLogicalTagReads(
            "twincat/logical/read",
            [LogicalTagName],
            tags,
            static result => result.Value?.Value?.ToString() ?? result.Error ?? string.Empty));
        using var singleWrite = clients.SubscribeTcLogicalTag(
            "twincat/logical/single",
            LogicalTagName,
            tags,
            ParsePayload);
        using var bulkWrite = clients.SubscribeTcLogicalTags(
            "twincat/logical/bulk",
            tags,
            CreateLogicalTagValues);
        using var correlatedWrite = clients.SubscribeTcTag(
            "twincat/logical/correlated",
            ScalarVariable,
            adsContract,
            "correlated-write",
            ParsePayload);

        await mqtt.SimulateMessageReceivedAsync(
            "twincat/logical/single",
            LogicalSingleWriteValue.ToString(CultureInfo.InvariantCulture));
        await Assert.That(ads.TryGetValue<int>(ScalarVariable, out var singleValue)).IsTrue();
        await Assert.That(singleValue).IsEqualTo(LogicalSingleWriteValue);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/logical/bulk",
            LogicalBulkWriteValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(ads.TryGetValue<int>(ScalarVariable, out var bulkValue)).IsTrue();
        await Assert.That(bulkValue).IsEqualTo(LogicalBulkWriteValue);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/logical/correlated",
            CorrelatedWriteValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(ads.TryGetValue<int>(ScalarVariable, out var correlatedValue)).IsTrue();
        await Assert.That(correlatedValue).IsEqualTo(CorrelatedWriteValue);
        await Assert.That(mqtt.PublishedMessages.Count).IsEqualTo(1);
        await AssertPayloadAsync(mqtt.PublishedMessages[0], "twincat/logical/read", "42");
    }

    /// <summary>Writes MQTT payloads through the underlying TwinCAT attached-structure transaction.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawStructWrite_WithAttachedTwinCatStructure_WritesClonedStructureToAdsAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqtt);
        var structure = TwinCatStructureExtensions.CreateStruct(ads, WritableStructVariable)
            ?? throw new InvalidOperationException("The TwinCAT structure was not created.");
        using var subscription = clients.SubscribeTcStructWrite(
            "twincat/struct/attached/write",
            MemberName,
            structure,
            ParsePayload);

        ads.Read(WritableStructVariable);
        await WaitForSubscriptionsAsync(mqtt, 1);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/struct/attached/write",
            StructureCloneWriteValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(ads.TryGetValue<WritableStructureValue>(WritableStructVariable, out var written)).IsTrue();
        await Assert.That(written!.Value).IsEqualTo(StructureCloneWriteValue);
        structure.Dispose();
    }

    /// <summary>Exercises resilient TwinCAT publish overloads against in-memory clients.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ResilientPublishOverloads_PublishAgainstInMemoryClientAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateResilientClient(mqtt, processed);
        var clients = Signal.Emit(resilient);
        var structure = CreateStructureTable();
        IRxTcAdsClient adsContract = ads;
        using var tags = CreateLogicalTags(ads);
        using var adsPublish = clients.PublishTcPlcTag<int>("twincat/resilient/ads", ScalarVariable, adsContract)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());
        using var hashPublish = clients.PublishTcPlcTag<int>("twincat/resilient/hash", MemberName, structure)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());
        using var structPublish = clients.PublishTcStructMember<IReadOnlyDictionary<string, int>>(
            "twincat/resilient/struct",
            ObjectMemberName,
            structure).Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());
        using var logicalPublish = clients.PublishTcLogicalTags(
            "twincat/resilient/logical",
            [LogicalTagName],
            tags,
            static value => value.Value?.ToString() ?? value.Quality)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());
        using var logicalRead = clients.PublishTcLogicalTagReads(
            "twincat/resilient/logical/read",
            [LogicalTagName],
            tags,
            static result => result.Value?.Value?.ToString() ?? result.Error ?? string.Empty)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());
        using var warmLogicalRead = clients.PublishTcLogicalTagReads(
            ResilientLogicalReadWarmTopic,
            [LogicalTagName],
            tags,
            static result => result.Value?.Value?.ToString() ?? result.Error ?? string.Empty)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());

        ads.SetValue(ScalarVariable, ResilientCorrelatedWriteValue);
        structure[MemberName] = ResilientCorrelatedWriteValue;
        structure[ObjectMemberName] = new Dictionary<string, int>
        {
            ["alpha"] = StructureObjectAlphaValue,
            ["beta"] = StructureObjectBetaValue,
        };

        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/struct", StructureObjectPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/logical/read", InitialScalarPayload);
        await AssertPublishedPayloadAsync(mqtt, ResilientLogicalReadWarmTopic, InitialScalarPayload);
        resilient.Dispose();
    }

    /// <summary>Exercises resilient TwinCAT subscribe overloads against in-memory clients.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ResilientSubscribeOverloads_WriteAgainstInMemoryClientAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateResilientClient(mqtt, processed);
        var clients = Signal.Emit(resilient);
        var structure = CreateStructureTable();
        IRxTcAdsClient adsContract = ads;
        using var tags = CreateLogicalTags(ads);
        using var correlatedWrite = clients.SubscribeTcTag(
            "twincat/resilient/correlated",
            ScalarVariable,
            adsContract,
            "resilient-correlated",
            ParsePayload);
        using var structMemberWrite = clients.SubscribeTcStructMember(
            "twincat/resilient/struct/member",
            MemberName,
            structure,
            ParsePayload);
        var cloneFactoryCalls = 0;
        using var structCloneWrite = clients.SubscribeTcStructWrite(
            "twincat/resilient/struct/write",
            MemberName,
            structure,
            CreateCountingPayloadFactory(() => cloneFactoryCalls++));
        using var logicalBulkWrite = clients.SubscribeTcLogicalTags(
            "twincat/resilient/logical/bulk",
            tags,
            CreateLogicalTagValues);
        using var logicalSingleWrite = clients.SubscribeTcLogicalTag(
            "twincat/resilient/logical/single",
            LogicalTagName,
            tags,
            ParsePayload);

        await WaitForSubscriptionsAsync(mqtt, ResilientWriteSubscriptionCount);
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/resilient/correlated",
            ResilientCorrelatedWriteValue.ToString(CultureInfo.InvariantCulture));
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/resilient/struct/member",
            StructureMemberWriteValue.ToString(CultureInfo.InvariantCulture));
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/resilient/struct/write",
            StructureCloneWriteValue.ToString(CultureInfo.InvariantCulture));
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/resilient/logical/bulk",
            LogicalBulkWriteValue.ToString(CultureInfo.InvariantCulture));
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/resilient/logical/single",
            ResilientSingleLogicalWriteValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(ads.TryGetValue<int>(ScalarVariable, out var written)).IsTrue();
        await Assert.That(written).IsEqualTo(ResilientSingleLogicalWriteValue);
        await Assert.That(structure[MemberName]).IsEqualTo(StructureCloneWriteValue);
        await Assert.That(cloneFactoryCalls).IsEqualTo(1);
        resilient.Dispose();
    }

    /// <summary>Publishes raw ADS read responses for scalar, correlated, and array variables.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawReadPublishers_ReadAdsValuesAndPublishPayloadsAsync()
    {
        using var mqtt = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqtt);
        var ads = CreateReadableAdsClient();

        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int>("twincat/read/scalar", ScalarVariable, ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int>(
            "twincat/read/correlated",
            ScalarVariable,
            ReadCorrelationId,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int[]>(
            "twincat/read/array",
            ArrayVariable,
            ArrayLength,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int[]>(
            "twincat/read/array-id",
            ArrayVariable,
            ArrayLength,
            ReadCorrelationId,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<(int Status, int[] Values, (int Code, int Extra) Nested)>(
            "twincat/read/struct",
            StructVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<string>(
            "twincat/read/string",
            StringVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<bool>(
            "twincat/read/bool",
            BoolVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<char>(
            "twincat/read/char",
            CharVariable,
            ads));

        await AssertPublishedPayloadAsync(mqtt, "twincat/read/scalar", InitialScalarPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/read/array-id", InitialArrayPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/read/struct", InitialStructPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/read/string", InitialStringPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/read/bool", InitialBoolPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/read/char", InitialCharPayload);
    }

    /// <summary>Publishes resilient ADS read responses for scalar, correlated, and array variables.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ResilientReadPublishers_ReadAdsValuesAndPublishPayloadsAsync()
    {
        using var mqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateResilientClient(mqtt, processed);
        var clients = Signal.Emit(resilient);
        var ads = CreateReadableAdsClient();

        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int>("twincat/resilient/read/scalar", ScalarVariable, ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int>(
            "twincat/resilient/read/correlated",
            ScalarVariable,
            ReadCorrelationId,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int[]>(
            "twincat/resilient/read/array",
            ArrayVariable,
            ArrayLength,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<int[]>(
            "twincat/resilient/read/array-id",
            ArrayVariable,
            ArrayLength,
            ReadCorrelationId,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<(int Status, int[] Values, (int Code, int Extra) Nested)>(
            "twincat/resilient/read/struct",
            StructVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<string>(
            "twincat/resilient/read/string",
            StringVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<bool>(
            "twincat/resilient/read/bool",
            BoolVariable,
            ads));
        _ = await CaptureOneAsync(clients.PublishTcPlcRead<char>(
            "twincat/resilient/read/char",
            CharVariable,
            ads));

        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/scalar", InitialScalarPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/array-id", InitialArrayPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/struct", InitialStructPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/string", InitialStringPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/bool", InitialBoolPayload);
        await AssertPublishedPayloadAsync(mqtt, "twincat/resilient/read/char", InitialCharPayload);
        resilient.Dispose();
    }

    /// <summary>Publishes logical-tag read errors through the resilient read publisher.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ResilientLogicalTagReadPublisher_PublishesMissingTagErrorsAsync()
    {
        using var mqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateResilientClient(mqtt, processed);
        var clients = Signal.Emit(resilient);
        using var ads = CreateAdsClient();
        using var tags = CreateLogicalTags(ads);
        IObservable<IResilientMqttClient> nullClients = null!;

        await Assert.That(() => nullClients.PublishTcLogicalTagReads(
            "twincat/resilient/logical/null",
            [MissingLogicalTagName],
            tags,
            static result => result.Succeeded ? InitialScalarPayload : LogicalReadErrorPayload))
            .Throws<ArgumentNullException>();

        _ = await CaptureOneAsync(clients.PublishTcLogicalTagReads(
            "twincat/resilient/logical/read-error",
            [MissingLogicalTagName],
            tags,
            static result => result.Succeeded ? InitialScalarPayload : LogicalReadErrorPayload));

        await AssertPublishedPayloadAsync(
            mqtt,
            "twincat/resilient/logical/read-error",
            LogicalReadErrorPayload);
        resilient.Dispose();
    }

    /// <summary>Publishes logical-tag observations from controllable ADS data signals.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task LogicalTagPublishers_ObserveReadableAdsDataAsync()
    {
        using var rawMqtt = new MockMqttClient();
        using var resilientMqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var ads = CreateReadableAdsClient();
        using var tags = CreateLogicalTags(ads);
        var rawClients = Signal.Emit<IMqttClient>(rawMqtt);
        var resilient = CreateResilientClient(resilientMqtt, processed);
        var resilientClients = Signal.Emit(resilient);
        using var rawPublish = rawClients.PublishTcLogicalTags(
            "twincat/logical/observe",
            [LogicalTagName],
            tags,
            static value => value.Value?.ToString() ?? value.Quality).Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        using var resilientPublish = resilientClients.PublishTcLogicalTags(
            "twincat/resilient/logical/observe",
            [LogicalTagName],
            tags,
            static value => value.Value?.ToString() ?? value.Quality)
            .Subscribe(new IgnoringObserver<ApplicationMessageProcessedEventArgs>());

        ads.Read(ScalarVariable);

        await AssertPublishedPayloadAsync(rawMqtt, "twincat/logical/observe", InitialScalarPayload);
        await AssertPublishedPayloadAsync(resilientMqtt, "twincat/resilient/logical/observe", InitialScalarPayload);
        resilient.Dispose();
    }

    /// <summary>Verifies scalar and array read helpers dispose their observation subscriptions when reads throw.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ReadHelpers_DisposeSubscriptionsWhenReadThrowsAsync()
    {
        var scalarFailure = CaptureSubscribeFailure(InvokeScalarReadOnce<int>(
            CreateThrowingReadAdsClient(),
            ScalarVariable,
            null));
        var arrayFailure = CaptureSubscribeFailure(InvokeArrayReadOnce<int[]>(
            CreateThrowingReadAdsClient(),
            ArrayVariable,
            ArrayLength,
            null));

        await Assert.That(scalarFailure).IsTypeOf<InvalidOperationException>();
        await Assert.That(arrayFailure).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Exercises raw asynchronous TwinCAT wrapper overloads.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawAsyncWrappers_DelegateToObservableImplementationsAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        var clients = SignalAsync.Return<IMqttClient>(mqtt);
        var structure = CreateStructureTable();
        IRxTcAdsClient adsContract = ads;
        using var tags = CreateLogicalTags(ads);

        var read = clients.PublishTcPlcRead<int[]>("twincat/async/read", ArrayVariable, ArrayLength, "raw-id", adsContract);
        using var member = clients.SubscribeTcStructMember("twincat/async/member", MemberName, structure, ParsePayload);
        using var logical = clients.SubscribeTcLogicalTags("twincat/async/logical", tags, CreateLogicalTagValues);

        await Assert.That(read).IsNotNull();
        await mqtt.SimulateMessageReceivedAsync(
            "twincat/async/member",
            StructureMemberWriteValue.ToString(CultureInfo.InvariantCulture));
        await Assert.That(structure[MemberName]).IsEqualTo(StructureMemberWriteValue);
    }

    /// <summary>Exercises resilient asynchronous TwinCAT wrapper overloads.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task ResilientAsyncWrappers_DelegateToObservableImplementationsAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateResilientClient(mqtt, processed);
        var clients = SignalAsync.Return(resilient);
        var structure = CreateStructureTable();
        IRxTcAdsClient adsContract = ads;
        using var tags = CreateLogicalTags(ads);

        var scalarRead = clients.PublishTcPlcRead<int>("twincat/async/resilient/read", ScalarVariable, adsContract);
        var idRead = clients.PublishTcPlcRead<int>("twincat/async/resilient/id", ScalarVariable, "id", adsContract);
        var arrayRead = clients.PublishTcPlcRead<int[]>("twincat/async/resilient/array", ArrayVariable, ArrayLength, adsContract);
        var arrayIdRead = clients.PublishTcPlcRead<int[]>(
            "twincat/async/resilient/array-id",
            ArrayVariable,
            ArrayLength,
            "id",
            adsContract);
        var logicalReads = clients.PublishTcLogicalTagReads(
            "twincat/async/resilient/logical-read",
            [LogicalTagName],
            tags,
            static result => result.Value?.Value?.ToString() ?? result.Error ?? string.Empty);
        var logicalTags = clients.PublishTcLogicalTags(
            "twincat/async/resilient/logical-tags",
            [LogicalTagName],
            tags,
            static value => value.Value?.ToString() ?? value.Quality);
        using var write = clients.SubscribeTcTag("twincat/async/resilient/write", ScalarVariable, adsContract, "id", ParsePayload);
        using var member = clients.SubscribeTcStructMember("twincat/async/resilient/member", MemberName, structure, ParsePayload);
        using var clone = clients.SubscribeTcStructWrite("twincat/async/resilient/clone", MemberName, structure, ParsePayload);
        using var logical = clients.SubscribeTcLogicalTag("twincat/async/resilient/logical", LogicalTagName, tags, ParsePayload);

        await Assert.That(scalarRead).IsNotNull();
        await Assert.That(idRead).IsNotNull();
        await Assert.That(arrayRead).IsNotNull();
        await Assert.That(arrayIdRead).IsNotNull();
        await Assert.That(logicalReads).IsNotNull();
        await Assert.That(logicalTags).IsNotNull();
        resilient.Dispose();
    }

    /// <summary>Verifies logical tag batch write failures surface as observable errors.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task LogicalTagBulkWriteFailure_IsPropagatedToSubscriptionAsync()
    {
        using var ads = CreateAdsClient();
        using var tags = new TwinCatLogicalTagClient(ads);
        Exception? failure = null;
        try
        {
            _ = await InvokeWriteLogicalTagsAsync(
                tags,
                [new("Missing.Tag", FailedLogicalWriteValue, TimeProvider.System.GetUtcNow(), "Bad")],
                CancellationToken.None);
        }
        catch (InvalidOperationException exception)
        {
            failure = exception;
        }

        await Assert.That(failure).IsNotNull();
    }

    /// <summary>Exercises the compatibility static Create forwarders.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task StaticCreateForwarders_DelegateToExtensionImplementationsAsync()
    {
        using var ads = CreateAdsClient();
        using var mqtt = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqtt);
        IRxTcAdsClient adsContract = ads;
#if !REACTIVE_SHIM
        var hash = CreateStructureTable();
        IHashTableRx hashContract = hash;
#endif

        await CaptureOneAfterTriggerAsync(
            TwinCatCreate.PublishTcPlcTag<int>(
            clients,
            "twincat/static/ads",
            ScalarVariable,
            adsContract),
            () => ads.SetValue(ScalarVariable, StaticWriteValue));
#if !REACTIVE_SHIM
        using var hashSubscription = clients.PublishTcPlcTag<int>(
            "twincat/static/hash",
            MemberName,
            hashContract).Subscribe(new IgnoringObserver<MqttClientPublishResult>());
        hash[MemberName] = StaticHashValue;
#endif
        using var writer = clients.SubscribeTcTag("twincat/static/write", ScalarVariable, adsContract, ParsePayload);

        await mqtt.SimulateMessageReceivedAsync(
            "twincat/static/write",
            StaticWriteValue.ToString(CultureInfo.InvariantCulture));

#if REACTIVE_SHIM
        await Assert.That(mqtt.PublishedMessages.Count).IsEqualTo(1);
#else
        await Assert.That(mqtt.PublishedMessages.Count).IsGreaterThanOrEqualTo(MinimumStructurePublications);
#endif
        await Assert.That(ads.TryGetValue<int>(ScalarVariable, out var written)).IsTrue();
        await Assert.That(written).IsEqualTo(StaticWriteValue);
    }

    /// <summary>Creates a payload factory that records each invocation before parsing the payload.</summary>
    /// <param name="onPayload">The callback invoked before parsing each payload.</param>
    /// <returns>The counting payload factory.</returns>
    private static Func<string, int> CreateCountingPayloadFactory(Action onPayload)
    {
        ArgumentNullException.ThrowIfNull(onPayload);
        return payload =>
        {
            onPayload();
            return ParsePayload(payload);
        };
    }

    /// <summary>Waits until the mock MQTT client has registered the expected subscription count.</summary>
    /// <param name="client">The mock client to inspect.</param>
    /// <param name="expectedCount">The minimum expected subscription count.</param>
    /// <returns>A task that completes when the subscriptions are visible.</returns>
    private static async Task WaitForSubscriptionsAsync(MockMqttClient client, int expectedCount)
    {
        var deadline = TimeProvider.System.GetUtcNow() + ObservableTimeout;
        while (client.Subscriptions.Count < expectedCount && TimeProvider.System.GetUtcNow() < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(PublicationPollDelayMilliseconds)).ConfigureAwait(false);
        }

        await Assert.That(client.Subscriptions.Count).IsGreaterThanOrEqualTo(expectedCount);
    }

    /// <summary>Captures the first value from an observable publication sequence.</summary>
    /// <typeparam name="T">The publication result type.</typeparam>
    /// <param name="source">The publication result source.</param>
    /// <returns>The first publication result.</returns>
    private static async Task<T> CaptureOneAsync<T>(IObservable<T> source)
    {
        var observer = new RecordingObserver<T>();
        using var subscription = source.Subscribe(observer);
        await observer.FirstValue.Task.WaitAsync(ObservableTimeout);
        await Assert.That(observer.Values).Count().IsEqualTo(1);
        await Assert.That(observer.Error).IsNull();
        return observer.Values[0];
    }

    /// <summary>Captures the first value after triggering an observable source.</summary>
    /// <typeparam name="T">The publication result type.</typeparam>
    /// <param name="source">The publication result source.</param>
    /// <param name="trigger">The trigger that causes the source to emit.</param>
    /// <returns>The first publication result.</returns>
    private static async Task<T> CaptureOneAfterTriggerAsync<T>(IObservable<T> source, Action trigger)
    {
        var observer = new RecordingObserver<T>();
        using var subscription = source.Subscribe(observer);
        trigger();
        await observer.FirstValue.Task.WaitAsync(ObservableTimeout);
        await Assert.That(observer.Values).Count().IsEqualTo(1);
        await Assert.That(observer.Error).IsNull();
        return observer.Values[0];
    }

    /// <summary>Invokes the private logical tag write helper through reflection.</summary>
    /// <param name="tags">The logical tag client.</param>
    /// <param name="values">The values to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reflected write task.</returns>
    private static Task<IReadOnlyList<TagOperationResult<LogicalTagValue>>> InvokeWriteLogicalTagsAsync(
        TwinCatLogicalTagClient tags,
        IReadOnlyCollection<LogicalTagValue> values,
        CancellationToken cancellationToken)
    {
        var method = typeof(TwinCatCreateExtensions).GetMethod(
            "WriteLogicalTagsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(TwinCatCreateExtensions).FullName, "WriteLogicalTagsAsync");
        return (Task<IReadOnlyList<TagOperationResult<LogicalTagValue>>>)method.Invoke(
            null,
            [tags, values, cancellationToken])!;
    }

    /// <summary>Creates a connected in-memory ADS client with a scalar symbol.</summary>
    /// <returns>The connected deterministic ADS client.</returns>
    private static InMemoryAdsClient CreateAdsClient()
    {
        var ads = new InMemoryAdsClient();
        var settings = new Settings
        {
            AdsAddress = "in-memory",
            Port = TwinCatPort,
            SettingsId = "twincat-task10",
        };
        TwinCatCoreExtensions.AddNotification(settings, ScalarVariable);
        TwinCatCoreExtensions.AddNotification(settings, ArrayVariable);
        TwinCatCoreExtensions.AddNotification(settings, WritableStructVariable);
        TwinCatCoreExtensions.AddWriteVariable(settings, ScalarVariable);
        TwinCatCoreExtensions.AddWriteVariable(settings, WritableStructVariable);
        _ = ads.RegisterSymbol(ScalarVariable, InitialScalarValue);
        _ = ads.RegisterSymbol(ArrayVariable, InitialArrayValue);
        _ = ads.RegisterSymbol(WritableStructVariable, new WritableStructureValue());
        ads.Connect(settings);
        return ads;
    }

    /// <summary>Creates a controllable ADS substitute that emits values from read requests.</summary>
    /// <returns>The readable ADS substitute.</returns>
    private static IRxTcAdsClient CreateReadableAdsClient()
    {
        var dataReceived = new TestSignal<(string Variable, object? Data, string? Id)>();
        var ads = Substitute.For<IRxTcAdsClient>();
        _ = ads.DataReceived.Returns(dataReceived);
        ads.When(static client => client.Read(Arg.Any<string>()))
            .Do(call => dataReceived.OnNext((call.Arg<string>(), ReadValue(call.Arg<string>()), string.Empty)));
        ads.When(static client => client.Read(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => dataReceived.OnNext((call.ArgAt<string>(0), ReadValue(call.ArgAt<string>(0)), call.ArgAt<string>(1))));
        ads.When(static client => client.Read(Arg.Any<string>(), Arg.Any<int?>()))
            .Do(call => dataReceived.OnNext((call.ArgAt<string>(0), ReadValue(call.ArgAt<string>(0)), string.Empty)));
        ads.When(static client => client.Read(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>()))
            .Do(call => dataReceived.OnNext((
                call.ArgAt<string>(0),
                ReadValue(call.ArgAt<string>(0)),
                call.ArgAt<string>(ReadIdArgumentIndex))));
        return ads;
    }

    /// <summary>Gets the deterministic read value for a simulated ADS variable.</summary>
    /// <param name="variable">The ADS variable name.</param>
    /// <returns>The simulated value.</returns>
    private static object ReadValue(string variable) =>
        variable switch
        {
            ArrayVariable => InitialArrayValue,
            StructVariable => InitialStructValue,
            StringVariable => InitialStringPayload,
            BoolVariable => InitialBoolValue,
            CharVariable => InitialCharValue,
            _ => InitialScalarValue,
        };

    /// <summary>Creates an ADS substitute that throws from read calls.</summary>
    /// <returns>The throwing ADS substitute.</returns>
    private static IRxTcAdsClient CreateThrowingReadAdsClient()
    {
        var dataReceived = new TestSignal<(string Variable, object? Data, string? Id)>();
        var ads = Substitute.For<IRxTcAdsClient>();
        _ = ads.DataReceived.Returns(dataReceived);
        ads.When(static client => client.Read(Arg.Any<string>())).Do(static _ => throw new InvalidOperationException());
        ads.When(static client => client.Read(Arg.Any<string>(), Arg.Any<int?>()))
            .Do(static _ => throw new InvalidOperationException());
        return ads;
    }

    /// <summary>Invokes the private scalar read helper.</summary>
    /// <typeparam name="T">The read value type.</typeparam>
    /// <param name="plc">The ADS client.</param>
    /// <param name="plcVariable">The PLC variable.</param>
    /// <param name="id">The optional correlation id.</param>
    /// <returns>The reflected observable.</returns>
    private static IObservable<T> InvokeScalarReadOnce<T>(IRxTcAdsClient plc, string plcVariable, string? id)
    {
        var method = typeof(TwinCatCreateExtensions).GetMethod(
            ReadOnceMethodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            [typeof(IRxTcAdsClient), typeof(string), typeof(string)])
            ?? throw new MissingMethodException(typeof(TwinCatCreateExtensions).FullName, ReadOnceMethodName);
        return (IObservable<T>)method.MakeGenericMethod(typeof(T)).Invoke(null, [plc, plcVariable, id])!;
    }

    /// <summary>Invokes the private array read helper.</summary>
    /// <typeparam name="T">The read value type.</typeparam>
    /// <param name="plc">The ADS client.</param>
    /// <param name="plcVariable">The PLC variable.</param>
    /// <param name="arrayLength">The requested array length.</param>
    /// <param name="id">The optional correlation id.</param>
    /// <returns>The reflected observable.</returns>
    private static IObservable<T> InvokeArrayReadOnce<T>(
        IRxTcAdsClient plc,
        string plcVariable,
        int arrayLength,
        string? id)
    {
        var method = typeof(TwinCatCreateExtensions).GetMethod(
            ReadOnceMethodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            [typeof(IRxTcAdsClient), typeof(string), typeof(int), typeof(string)])
            ?? throw new MissingMethodException(typeof(TwinCatCreateExtensions).FullName, ReadOnceMethodName);
        return (IObservable<T>)method.MakeGenericMethod(typeof(T)).Invoke(null, [plc, plcVariable, arrayLength, id])!;
    }

    /// <summary>Captures a subscription-time failure.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="source">The source expected to fail on subscribe.</param>
    /// <returns>The captured exception.</returns>
    private static Exception CaptureSubscribeFailure<T>(IObservable<T> source)
    {
        try
        {
            using var subscription = source.Subscribe(new IgnoringObserver<T>());
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The source was expected to fail on subscribe.");
    }

    /// <summary>Creates a writable structure-like hash table.</summary>
    /// <returns>The populated structure table.</returns>
    private static HashTableRx CreateStructureTable()
    {
        var table = new HashTableRx(useUpperCase: false);
        table.Add(MemberName, 0);
        table.Add(ArrayMemberName, Array.Empty<int>());
        table.Add(ObjectMemberName, new Dictionary<string, int>());
        table.Add(DecimalMemberName, 0M);
        return table;
    }

    /// <summary>Creates a logical-tag facade over the in-memory ADS client.</summary>
    /// <param name="ads">The ADS client to wrap.</param>
    /// <returns>The logical-tag client.</returns>
    private static TwinCatLogicalTagClient CreateLogicalTags(IRxTcAdsClient ads)
    {
        var tags = new TwinCatLogicalTagClient(ads);
        tags.RegisterTag(tags.CreateTag(LogicalTagName, ScalarVariable, "Int32"));
        return tags;
    }

    /// <summary>Creates an in-memory resilient facade over the recording MQTT client.</summary>
    /// <param name="internalClient">The internal MQTT client.</param>
    /// <param name="processed">The processed-message signal.</param>
    /// <returns>The resilient MQTT facade.</returns>
    private static IResilientMqttClient CreateResilientClient(
        MockMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed)
    {
        var receivedAsync = internalClient.ObserveApplicationMessageReceived();
        var client = Substitute.For<IResilientMqttClient>();
        _ = client.InternalClient.Returns(internalClient);
        _ = client.IsConnected.Returns(true);
        _ = client.IsStarted.Returns(true);
        _ = client.ApplicationMessageProcessed.Returns(processed);
        _ = client.ApplicationMessageProcessedAsyncObservable.Returns(processed.ToSignal());
        _ = client.ApplicationMessageReceived.Returns(receivedAsync.ToObservable());
        _ = client.ApplicationMessageReceivedAsyncObservable.Returns(receivedAsync);
        _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
            PublishResilientMessageAsync(
                internalClient,
                processed,
                call.Arg<MqttApplicationMessage>() ?? throw new InvalidOperationException(
                    "The resilient facade requires an application message.")));
        _ = client.EnqueueAsync(Arg.Any<ResilientMqttApplicationMessage>()).Returns(call =>
            PublishResilientMessageAsync(
                internalClient,
                processed,
                call.Arg<ResilientMqttApplicationMessage>() ?? throw new InvalidOperationException(
                    "The resilient facade requires a managed application message.")));
        _ = client.SubscribeAsync(Arg.Any<IEnumerable<MqttTopicFilter>>()).Returns(async call =>
        {
            var builder = new MqttClientSubscribeOptionsBuilder();
            var filters = call.Arg<IEnumerable<MqttTopicFilter>>() ?? throw new InvalidOperationException(
                "The resilient facade requires topic filters.");
            foreach (var filter in filters)
            {
                _ = builder.WithTopicFilter(filter);
            }

            _ = await internalClient.SubscribeAsync(builder.Build(), CancellationToken.None).ConfigureAwait(false);
        });
        return client;
    }

    /// <summary>Publishes a resilient MQTT message through the in-memory internal client.</summary>
    /// <param name="internalClient">The internal MQTT client.</param>
    /// <param name="processed">The processed-message signal.</param>
    /// <param name="message">The MQTT application message.</param>
    /// <returns>A task that represents the publication.</returns>
    private static async Task PublishResilientMessageAsync(
        MockMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed,
        MqttApplicationMessage message)
    {
        var managed = new ResilientMqttApplicationMessage { ApplicationMessage = message };
        await PublishResilientMessageAsync(internalClient, processed, managed).ConfigureAwait(false);
    }

    /// <summary>Publishes a resilient MQTT managed message through the in-memory internal client.</summary>
    /// <param name="internalClient">The internal MQTT client.</param>
    /// <param name="processed">The processed-message signal.</param>
    /// <param name="managed">The managed MQTT message.</param>
    /// <returns>A task that represents the publication.</returns>
    private static async Task PublishResilientMessageAsync(
        MockMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed,
        ResilientMqttApplicationMessage managed)
    {
        Exception? failure = null;
        try
        {
            var message = managed.ApplicationMessage ?? throw new InvalidOperationException(
                "The resilient facade requires an application message.");
            _ = await internalClient.PublishAsync(message, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        processed.OnNext(new(managed, failure));
    }

    /// <summary>Parses an invariant integer payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed integer.</returns>
    private static int ParsePayload(string payload) => int.Parse(payload, CultureInfo.InvariantCulture);

    /// <summary>Creates logical tag values from an invariant payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The value collection.</returns>
    private static IReadOnlyCollection<LogicalTagValue> CreateLogicalTagValues(string payload) =>
        [new(LogicalTagName, ParsePayload(payload), TimeProvider.System.GetUtcNow(), "Good")];

    /// <summary>Verifies one MQTT message payload.</summary>
    /// <param name="message">The published message.</param>
    /// <param name="topic">The expected topic.</param>
    /// <param name="payload">The expected payload.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertPayloadAsync(MqttApplicationMessage message, string topic, string payload)
    {
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(message.ConvertPayloadToString()).IsEqualTo(payload);
    }

    /// <summary>Verifies a topic eventually receives an expected payload.</summary>
    /// <param name="mqtt">The recording MQTT client.</param>
    /// <param name="topic">The expected topic.</param>
    /// <param name="payload">The expected payload.</param>
    /// <returns>A task that represents the assertion.</returns>
    private static async Task AssertPublishedPayloadAsync(MockMqttClient mqtt, string topic, string payload)
    {
        var deadline = TimeProvider.System.GetUtcNow() + ObservableTimeout;
        MqttApplicationMessage? message;
        do
        {
            message = null;
            foreach (var candidate in mqtt.PublishedMessages)
            {
                if (candidate.Topic == topic)
                {
                    message = candidate;
                }
            }

            if (message?.ConvertPayloadToString() == payload)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(PublicationPollDelayMilliseconds)).ConfigureAwait(false);
        }
        while (TimeProvider.System.GetUtcNow() < deadline);

        await Assert.That(message).IsNotNull();
        await Assert.That(message!.ConvertPayloadToString()).IsEqualTo(payload);
    }

    /// <summary>Represents a writable structured ADS value with property-backed members.</summary>
    private sealed class WritableStructureValue
    {
        /// <summary>Gets or sets the structure member used by write-through tests.</summary>
        public int Value { get; set; }
    }

    /// <summary>Records observable values and terminal errors.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class RecordingObserver<T> : IObserver<T>
    {
        /// <summary>Gets the observed values.</summary>
        public List<T> Values { get; } = [];

        /// <summary>Gets the observed error.</summary>
        public Exception? Error { get; private set; }

        /// <summary>Gets a task that completes when the first value is observed.</summary>
        public TaskCompletionSource FirstValue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            Error = error;
            _ = FirstValue.TrySetException(error);
        }

        /// <inheritdoc/>
        public void OnNext(T value)
        {
            Values.Add(value);
            _ = FirstValue.TrySetResult();
        }
    }

    /// <summary>Ignores observable values while keeping subscriptions active.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class IgnoringObserver<T> : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
            throw error;
        }

        /// <inheritdoc/>
        public void OnNext(T value) => GC.KeepAlive(value);
    }
}
#endif
