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
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.Reactive.CreateExtensions;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.TwinCatRxExtensions;
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.CreateExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the task-9 TwinCAT read, structure, and logical-tag bridge surface.</summary>
public sealed class TwinCatBridgeSurfaceTests
{
    /// <summary>The ADS symbol used by construction-only bridge tests.</summary>
    private const string AdsVariable = ".Main.Task9";

    /// <summary>The structure member used by construction-only bridge tests.</summary>
    private const string MemberName = "Value";

    /// <summary>The logical tag used by construction-only bridge tests.</summary>
    private const string LogicalTagName = "Task9.Value";

    /// <summary>The MQTT topic used by construction-only bridge tests.</summary>
    private const string Topic = "twincat/task9";

    /// <summary>The in-memory TwinCAT port used only as simulator metadata.</summary>
    private const int TwinCatPort = 851;

    /// <summary>Constructs the new raw-client TwinCAT bridge families with configured in-memory dependencies.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task RawTask9Surface_AcceptsConfiguredDependenciesAsync()
    {
        using var ads = CreateAdsClient();
        using var structure = CreateStructureTable();
        using var tags = CreateLogicalTags(ads);
        var client = Signal.None<IMqttClient>();
        var resilientClient = Signal.None<IResilientMqttClient>();
        IRxTcAdsClient adsContract = ads;
        HashTableRx tableContract = structure;

        var scalarRead = client.PublishTcPlcRead<int>(Topic, AdsVariable, adsContract);
        var correlatedRead = client.PublishTcPlcRead<int>(Topic, AdsVariable, "task9-read", adsContract);
        var arrayRead = client.PublishTcPlcRead<int[]>(Topic, AdsVariable, 1, adsContract);
        var correlatedArrayRead = client.PublishTcPlcRead<int[]>(Topic, AdsVariable, 1, "task9-array", adsContract);
        var structurePublisher = client.PublishTcStructMember<int>(Topic, MemberName, tableContract);
        var logicalPublisher = client.PublishTcLogicalTags(Topic, [LogicalTagName], tags, FormatLogicalTagValue);
        var logicalReads = client.PublishTcLogicalTagReads(Topic, [LogicalTagName], tags, FormatLogicalTagResult);
        using var tagWriter = client.SubscribeTcTag(Topic, AdsVariable, adsContract, "task9-write", ParsePayload);
        using var tableWriter = client.SubscribeTcStructMember(Topic, MemberName, tableContract, ParsePayload);
        using var structureWriter = client.SubscribeTcStructWrite(Topic, MemberName, structure, ParsePayload);
        using var logicalWriter = client.SubscribeTcLogicalTags(Topic, tags, CreateLogicalTagValues);
        var resilientArrayRead = resilientClient.PublishTcPlcRead<int[]>(Topic, AdsVariable, 1, adsContract);
        var resilientStructurePublisher = resilientClient.PublishTcStructMember<int>(Topic, MemberName, tableContract);
        var resilientLogicalReads = resilientClient.PublishTcLogicalTagReads(Topic, [LogicalTagName], tags, FormatLogicalTagResult);
        using var resilientStructureWriter =
            resilientClient.SubscribeTcStructWrite(Topic, MemberName, structure, ParsePayload);
        using var resilientLogicalWriter =
            resilientClient.SubscribeTcLogicalTag(Topic, LogicalTagName, tags, ParsePayload);

        await Assert.That(scalarRead).IsNotNull();
        await Assert.That(correlatedRead).IsNotNull();
        await Assert.That(arrayRead).IsNotNull();
        await Assert.That(correlatedArrayRead).IsNotNull();
        await Assert.That(structurePublisher).IsNotNull();
        await Assert.That(logicalPublisher).IsNotNull();
        await Assert.That(logicalReads).IsNotNull();
        await Assert.That(resilientArrayRead).IsNotNull();
        await Assert.That(resilientStructurePublisher).IsNotNull();
        await Assert.That(resilientLogicalReads).IsNotNull();
    }

    /// <summary>Constructs the new async-client TwinCAT read and logical bridge families.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task AsyncTask9Surface_AcceptsConfiguredDependenciesAsync()
    {
        using var ads = CreateAdsClient();
        using var structure = CreateStructureTable();
        using var tags = CreateLogicalTags(ads);
        var client = SignalAsync.None<IMqttClient>();
        IRxTcAdsClient adsContract = ads;
        HashTableRx tableContract = structure;

        var scalarRead = client.PublishTcPlcRead<int>(Topic, AdsVariable, adsContract);
        var correlatedRead = client.PublishTcPlcRead<int>(Topic, AdsVariable, "task9-read", adsContract);
        var arrayRead = client.PublishTcPlcRead<int[]>(Topic, AdsVariable, 1, adsContract);
        var structurePublisher = client.PublishTcStructMember<int>(Topic, MemberName, tableContract);
        var logicalPublisher = client.PublishTcLogicalTags(Topic, [LogicalTagName], tags, FormatLogicalTagValue);
        var logicalReads = client.PublishTcLogicalTagReads(Topic, [LogicalTagName], tags, FormatLogicalTagResult);
        using var correlatedWriter = client.SubscribeTcTag(Topic, AdsVariable, adsContract, "task9-write", ParsePayload);
        using var structureWriter = client.SubscribeTcStructWrite(Topic, MemberName, structure, ParsePayload);
        using var logicalWriter = client.SubscribeTcLogicalTag(Topic, LogicalTagName, tags, ParsePayload);

        await Assert.That(scalarRead).IsNotNull();
        await Assert.That(correlatedRead).IsNotNull();
        await Assert.That(arrayRead).IsNotNull();
        await Assert.That(structurePublisher).IsNotNull();
        await Assert.That(logicalPublisher).IsNotNull();
        await Assert.That(logicalReads).IsNotNull();
    }

    /// <summary>Constructs the new async resilient-client TwinCAT bridge families.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task AsyncResilientTask9Surface_AcceptsConfiguredDependenciesAsync()
    {
        using var ads = CreateAdsClient();
        using var structure = CreateStructureTable();
        using var tags = CreateLogicalTags(ads);
        var resilientClient = SignalAsync.None<IResilientMqttClient>();
        IRxTcAdsClient adsContract = ads;
        HashTableRx tableContract = structure;

        var resilientArrayRead = resilientClient.PublishTcPlcRead<int[]>(Topic, AdsVariable, 1, adsContract);
        var resilientStructurePublisher = resilientClient.PublishTcStructMember<int>(Topic, MemberName, tableContract);
        var resilientLogicalReads =
            resilientClient.PublishTcLogicalTagReads(Topic, [LogicalTagName], tags, FormatLogicalTagResult);
        using var resilientLogicalWriter = resilientClient.SubscribeTcLogicalTags(Topic, tags, CreateLogicalTagValues);

        await Assert.That(resilientArrayRead).IsNotNull();
        await Assert.That(resilientStructurePublisher).IsNotNull();
        await Assert.That(resilientLogicalReads).IsNotNull();
    }

    /// <summary>Verifies the new TwinCAT bridge families reject missing dependencies.</summary>
    /// <returns>A task that represents the asynchronous TUnit assertions.</returns>
    [Test]
    public async Task Task9Surface_RejectsMissingDependenciesAsync()
    {
        using var ads = CreateAdsClient();
        using var structure = CreateStructureTable();
        using var tags = CreateLogicalTags(ads);
        var client = Signal.None<IMqttClient>();
        IRxTcAdsClient adsContract = ads;
        HashTableRx tableContract = structure;

        await Assert.That(() => client.PublishTcPlcRead<int>(Topic, AdsVariable, (IRxTcAdsClient)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.PublishTcPlcRead<int>(Topic, AdsVariable, (string)null!, adsContract))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.PublishTcStructMember<int>(Topic, MemberName, (HashTableRx)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.SubscribeTcTag(Topic, AdsVariable, adsContract, (string)null!, ParsePayload))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.SubscribeTcStructMember(Topic, MemberName, tableContract, (Func<string, int>)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.PublishTcLogicalTags(Topic, [LogicalTagName], tags, (Func<LogicalTagValue, string>)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.SubscribeTcLogicalTags(Topic, tags, null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Creates a connected in-memory ADS client with one readable and writable symbol.</summary>
    /// <returns>The connected deterministic ADS client.</returns>
    private static InMemoryAdsClient CreateAdsClient()
    {
        var ads = new InMemoryAdsClient();
        var settings = new Settings
        {
            AdsAddress = "in-memory",
            Port = TwinCatPort,
            SettingsId = "twincat-task9",
        };
        TwinCatCoreExtensions.AddNotification(settings, AdsVariable);
        TwinCatCoreExtensions.AddWriteVariable(settings, AdsVariable);
        _ = ads.RegisterSymbol(AdsVariable, 0);
        ads.Connect(settings);
        return ads;
    }

    /// <summary>Creates a writable structure-like hash table for bridge construction tests.</summary>
    /// <returns>The populated structure table.</returns>
    private static HashTableRx CreateStructureTable()
    {
        var table = new HashTableRx(useUpperCase: false);
        table.Add(MemberName, 0);
        return table;
    }

    /// <summary>Creates a logical-tag facade over the in-memory ADS client.</summary>
    /// <param name="ads">The ADS client to wrap.</param>
    /// <returns>The logical-tag client.</returns>
    private static TwinCatLogicalTagClient CreateLogicalTags(IRxTcAdsClient ads)
    {
        var tags = new TwinCatLogicalTagClient(ads);
        tags.RegisterTag(tags.CreateTag(LogicalTagName, AdsVariable, "Int32"));
        return tags;
    }

    /// <summary>Parses an invariant integer payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed integer.</returns>
    private static int ParsePayload(string payload) => int.Parse(payload, CultureInfo.InvariantCulture);

    /// <summary>Formats a logical tag value for MQTT publication.</summary>
    /// <param name="value">The logical tag value.</param>
    /// <returns>The MQTT payload.</returns>
    private static string FormatLogicalTagValue(LogicalTagValue value) =>
        Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Formats a logical tag operation result for MQTT publication.</summary>
    /// <param name="result">The logical tag operation result.</param>
    /// <returns>The MQTT payload.</returns>
    private static string FormatLogicalTagResult(TagOperationResult<LogicalTagValue> result) =>
        result.Succeeded && result.Value is { } value ? FormatLogicalTagValue(value) : result.Error ?? string.Empty;

    /// <summary>Creates a logical tag value collection from an MQTT payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The logical tag values to write.</returns>
    private static IReadOnlyCollection<LogicalTagValue> CreateLogicalTagValues(string payload) =>
        [new(LogicalTagName, ParsePayload(payload), TimeProvider.System.GetUtcNow(), "Good")];
}
#endif
