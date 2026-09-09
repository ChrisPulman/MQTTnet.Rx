// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
#else
using CP.Collections;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Core.Reactive;
using IoT.Driver.TwinCATRx.Reactive;
#else
using IoT.Driver.TwinCATRx;
using IoT.Driver.TwinCATRx.Core;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive;
using MQTTnet.Rx.TwinCAT.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using MQTTnet.Rx.Client;
using MQTTnet.Rx.TwinCAT;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises automatic TwinCAT structure-to-MQTT publication.</summary>
public sealed class TwinCatStructureBridgeTests
{
    /// <summary>The simulated PLC structure symbol.</summary>
    private const string PlcVariable = "GVL.Rig";

    /// <summary>The default MQTT topic prefix.</summary>
    private const string TopicPrefix = "factory/rig/";

    /// <summary>The pressure structure member name.</summary>
    private const string PressureMemberName = nameof(RigValues.Pressure);

    /// <summary>The fully qualified pressure member key.</summary>
    private const string PressureKey = $"{PlcVariable}.{PressureMemberName}";

    /// <summary>The initial pressure value.</summary>
    private const int InitialPressure = 42;

    /// <summary>The updated pressure value.</summary>
    private const int UpdatedPressure = 43;

    /// <summary>The nested temperature value.</summary>
    private const double NestedTemperature = 18.5D;

    /// <summary>The status code value.</summary>
    private const int StatusCode = 7;

    /// <summary>The decimal amount value.</summary>
    private const decimal AmountValue = 12.5M;

    /// <summary>The initial pressure MQTT payload.</summary>
    private const string InitialPressurePayload = "42";

    /// <summary>The updated pressure MQTT payload.</summary>
    private const string UpdatedPressurePayload = "43";

    /// <summary>The generated MQTT topic for the pressure member.</summary>
    private const string FactoryPressureTopic = "factory/rig/Pressure";

    /// <summary>The generated MQTT topic for the nested pressure member.</summary>
    private const string FactorySensorPressureTopic = "factory/rig/Sensor/Pressure";

    /// <summary>The generated MQTT topic for the nested temperature member.</summary>
    private const string FactorySensorTemperatureTopic = "factory/rig/Sensor/Temperature";

    /// <summary>The generated MQTT topic for the status member.</summary>
    private const string FactoryStatusTopic = "factory/rig/Status";

    /// <summary>The MQTT payload collection drain duration.</summary>
    private const int CollectionDrainMilliseconds = 100;

    /// <summary>The snapshot republish interval.</summary>
    private const int RepublishIntervalMilliseconds = 20;

    /// <summary>The delay used to verify timer disposal.</summary>
    private const int DisposeDrainMilliseconds = 75;

    /// <summary>The expected minimum republished snapshot count.</summary>
    private const int ExpectedRepublishCount = 2;

    /// <summary>The expected number of initial member publications without an explicit republish timer.</summary>
    private const int ExpectedInitialPublicationCount = 1;

    /// <summary>The default ADS runtime port.</summary>
    private const int DefaultAdsPort = 851;

    /// <summary>The polling delay used while waiting for asynchronous bridge conditions.</summary>
    private const int PollDelayMilliseconds = 10;

    /// <summary>The test timeout for asynchronous bridge work.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    /// <summary>The sample integer array structure value.</summary>
    private static readonly int[] StructureValues = [1, 2, 3];

    /// <summary>The sample status flags structure value.</summary>
    private static readonly int[] StatusFlags = [4, 5];

    /// <summary>Represents a sample structure mode.</summary>
    private enum BridgeMode
    {
        /// <summary>Manual mode.</summary>
        Manual,

        /// <summary>Automatic mode.</summary>
        Automatic,
    }

    /// <summary>Verifies low-level structure observation publishes leaf snapshots and changes.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [RequiresUnreferencedCode("HashTableRx.SetStructure reflects over test data.")]
    public async Task ObserveTcStructureMessages_PublishesSnapshotChangesAndFormattedLeavesAsync()
    {
        using var structure = CreateStructureTable();
        using var nestedTable = new HashTableRx(useUpperCase: false);
        nestedTable.Add("Ignored", StatusCode);
        structure.Add($"{PlcVariable}.NestedTable", nestedTable);
        var options = CreateOptions();
        var messages = new List<(string Topic, string Payload)>();

        using var subscription = TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, options)
            .Subscribe(messages.Add);
        structure.SetStructure(CreateSetStructureSnapshot(UpdatedPressure, ready: false));
        await WaitUntilAsync(() => messages.Exists(
            static message => message.Topic == FactorySensorPressureTopic && message.Payload == UpdatedPressurePayload));

        await Assert.That(messages).Contains((FactoryPressureTopic, InitialPressurePayload));
        await Assert.That(messages).Contains((FactorySensorPressureTopic, InitialPressurePayload));
        await Assert.That(messages).Contains((FactorySensorTemperatureTopic, "18.5"));
        await Assert.That(messages).Contains(("factory/rig/Values", "[1,2,3]"));
        await Assert.That(messages).Contains((FactoryStatusTopic, "{\"Code\":7,\"Flags\":[4,5]}"));
        await Assert.That(messages).Contains(("factory/rig/Name", "ready"));
        await Assert.That(messages).Contains(("factory/rig/Enabled", "True"));
        await Assert.That(messages).Contains(("factory/rig/Grade", "A"));
        await Assert.That(messages).Contains(("factory/rig/Mode", nameof(BridgeMode.Automatic)));
        await Assert.That(messages).Contains(("factory/rig/Amount", "12.5"));
        await Assert.That(messages).Contains(("factory/rig/Nullable", "null"));
        await Assert.That(messages).Contains((FactorySensorPressureTopic, UpdatedPressurePayload));
        await Assert.That(CountMessages(messages, FactoryPressureTopic, InitialPressurePayload))
            .IsEqualTo(ExpectedInitialPublicationCount);
        await Assert.That(messages.Exists(static message => message.Topic.EndsWith("NestedTable", StringComparison.Ordinal)))
            .IsFalse();
    }

    /// <summary>Verifies custom filtering, topics, payloads, and member matching.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [RequiresUnreferencedCode("HashTableRx.SetStructure reflects over test data.")]
    public async Task ObserveTcStructureMessages_AppliesCustomFilteringTopicsAndPayloadsAsync()
    {
        using var structure = CreateStructureTable();
        var formatted = new List<TwinCatStructureValue>();
        var options = CreateOptions();
        options.MemberFilter = static member => string.Equals(member, PressureMemberName, StringComparison.OrdinalIgnoreCase);
        options.TopicFactory = static (prefix, member) => string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix.TrimEnd('/').ToUpperInvariant()}::{member.ToLowerInvariant()}");
        options.PayloadFormatter = value =>
        {
            formatted.Add(value);
            return string.Create(CultureInfo.InvariantCulture, $"{value.MemberName}|{value.Topic}|{value.Value}");
        };

        var messages = await TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, options)
            .CollectAsync(TimeSpan.FromMilliseconds(CollectionDrainMilliseconds));

        await Assert.That(messages).Count().IsEqualTo(1);
        await Assert.That(messages[0].Topic).IsEqualTo("FACTORY/RIG::pressure");
        await Assert.That(messages[0].Payload)
            .IsEqualTo("Pressure|FACTORY/RIG::pressure|42");
        await Assert.That(formatted).Count().IsEqualTo(1);
        await Assert.That(formatted[0].Value).IsEqualTo(InitialPressure);
        await Assert.That(TwinCatStructureBridgeExtensions.IsTcStructureMember(PressureKey, "pressure"))
            .IsTrue();
        await Assert.That(TwinCatStructureBridgeExtensions.IsTcStructureMember(PressureMemberName, "PRESSURE")).IsTrue();
        await Assert.That(TwinCatStructureBridgeExtensions.IsTcStructureMember("GVL.Rig.PressureX", PressureMemberName))
            .IsFalse();
    }

    /// <summary>Verifies TwinCAT 2-style uppercase structure tables publish uppercase member paths.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [RequiresUnreferencedCode("HashTableRx.SetStructure reflects over test data.")]
    public async Task ObserveTcStructureMessages_PreservesUppercaseTwinCat2MemberPathsAsync()
    {
        using var structure = new HashTableRx(useUpperCase: true);
        structure.SetStructure(CreateSetStructureSnapshot(InitialPressure, ready: true));
        var options = CreateOptions(topicPrefix: "tc2/rig", plcVariable: string.Empty);

        var messages = await TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, options)
            .CollectAsync(TimeSpan.FromMilliseconds(CollectionDrainMilliseconds));

        await Assert.That(messages).Contains(("tc2/rig/PRESSURE", InitialPressurePayload));
        await Assert.That(messages).Contains(("tc2/rig/SENSOR/PRESSURE", InitialPressurePayload));
        await Assert.That(messages).Contains(("tc2/rig/SENSOR/TEMPERATURE", "18.5"));
    }

    /// <summary>Verifies validation and observer error routing.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ObserveTcStructureMessages_ValidatesOptionsAndRoutesPublicationErrorsAsync()
    {
        using var structure = CreateStructureTable();
        var options = CreateOptions();
        options.TopicFactory = static (_, _) => throw new InvalidOperationException("topic failed");
        var observedError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, options)
            .Subscribe(static _ => { }, error => _ = observedError.TrySetResult(error));
        var error = await observedError.Task.WaitAsync(Timeout);

        await Assert.That(error).IsTypeOf<InvalidOperationException>();
        await Assert.That(static () => TwinCatStructureBridgeExtensions.ObserveTcStructureMessages(null!, CreateOptions()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => TwinCatStructureBridgeExtensions.ObserveTcStructureMessages(structure, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => TwinCatStructureBridgeExtensions.ObserveTcStructureMessages(
                structure,
                CreateOptions(topicPrefix: " ")))
            .Throws<ArgumentException>();
        await Assert.That(() => TwinCatStructureBridgeExtensions.ObserveTcStructureMessages(
                structure,
                CreateOptions(republishInterval: TimeSpan.Zero)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(static () => TwinCatStructureBridgeExtensions.IsTcStructureMember(null!, PressureMemberName))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatStructureBridgeExtensions.IsTcStructureMember(PressureKey, null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Verifies republish timers and idempotent disposal.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ObserveTcStructureMessages_RepublishesSnapshotAndStopsAfterDisposeAsync()
    {
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add(PressureKey, InitialPressure);
        var options = CreateOptions(republishInterval: TimeSpan.FromMilliseconds(RepublishIntervalMilliseconds));
        var messages = new List<(string Topic, string Payload)>();

        using var subscription = TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, options)
            .Subscribe(messages.Add);
        await WaitUntilAsync(() => messages.Count >= ExpectedRepublishCount);
        subscription.Dispose();
        subscription.Dispose();
        var countAfterDispose = messages.Count;
        await Task.Delay(DisposeDrainMilliseconds);

        await Assert.That(countAfterDispose).IsGreaterThanOrEqualTo(ExpectedRepublishCount);
        await Assert.That(messages.Count).IsEqualTo(countAfterDispose);
    }

    /// <summary>Verifies recursive table snapshots do not re-enter already visited tables.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ObserveTcStructureMessages_IgnoresRecursiveSnapshotTablesAsync()
    {
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add(PressureKey, InitialPressure);
        structure.Add($"{PlcVariable}.Loop", structure);

        var messages = await TwinCatStructureBridgeExtensions
            .ObserveTcStructureMessages(structure, CreateOptions())
            .CollectAsync(TimeSpan.FromMilliseconds(CollectionDrainMilliseconds));

        await Assert.That(messages).Contains((FactoryPressureTopic, InitialPressurePayload));
        await Assert.That(messages.Exists(static message => message.Topic.EndsWith("Loop", StringComparison.Ordinal)))
            .IsFalse();
    }

    /// <summary>Verifies raw and async raw MQTT client structure publish overloads preserve delivery settings.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_RawClientOverloadsPublishConfiguredMessagesAsync()
    {
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add(PressureKey, InitialPressure);
        using MockMqttClient rawClient = new();
        using MockMqttClient asyncRawClient = new();
        var options = CreateOptions(topicPrefix: "raw/rig", qos: MqttQualityOfServiceLevel.ExactlyOnce, retain: false);

        _ = await Signal.Emit<IMqttClient>(rawClient)
            .PublishTcStructure(structure, options)
            .FirstAsync(Timeout);
        _ = await SignalAsync.Return<IMqttClient>(asyncRawClient)
            .PublishTcStructure(structure, options)
            .ToObservable()
            .FirstAsync(Timeout);

        await AssertPublishedMessageAsync(rawClient.PublishedMessages[0], "raw/rig/Pressure", InitialPressurePayload, options);
        await AssertPublishedMessageAsync(
            asyncRawClient.PublishedMessages[0],
            "raw/rig/Pressure",
            InitialPressurePayload,
            options);
    }

    /// <summary>Verifies resilient and async resilient MQTT client structure publish overloads preserve delivery settings.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_ResilientClientOverloadsPublishConfiguredMessagesAsync()
    {
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add(PressureKey, InitialPressure);
        var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var asyncProcessed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var resilientClient = CreateResilientClient(processed);
        var asyncResilientClient = CreateResilientClient(asyncProcessed);
        var options = CreateOptions(topicPrefix: "resilient/rig", qos: MqttQualityOfServiceLevel.AtMostOnce);

        var capturedTask = ConfigureResilientPublication(resilientClient, processed);
        var result = Signal.Emit(resilientClient)
            .PublishTcStructure(structure, options)
            .FirstAsync(Timeout);
        var captured = await capturedTask.Task.WaitAsync(Timeout);
        _ = await result;
        var asyncCapturedTask = ConfigureResilientPublication(asyncResilientClient, asyncProcessed);
        var asyncResult = SignalAsync.Return(asyncResilientClient)
            .PublishTcStructure(structure, options)
            .ToObservable()
            .FirstAsync(Timeout);
        var asyncCaptured = await asyncCapturedTask.Task.WaitAsync(Timeout);
        _ = await asyncResult;

        await AssertPublishedMessageAsync(captured, "resilient/rig/Pressure", InitialPressurePayload, options);
        await AssertPublishedMessageAsync(asyncCaptured, "resilient/rig/Pressure", InitialPressurePayload, options);
    }

    /// <summary>Verifies public overloads reject missing dependencies.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_PublicOverloadsRejectMissingDependenciesAsync()
    {
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add(PressureKey, InitialPressure);
        var raw = Signal.Emit<IMqttClient>(new MockMqttClient());
        var resilient = Signal.Emit(CreateResilientClient(new()));
        var asyncRaw = SignalAsync.Return<IMqttClient>(new MockMqttClient());
        var asyncResilient = SignalAsync.Return(
            CreateResilientClient(new()));

        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishTcStructure(structure, CreateOptions()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishTcStructure(null!, CreateOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishTcStructure(structure, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishTcStructure(CreateOptions(), null!)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishTcStructure(CreateOptions(topicPrefix: " "), static () => new InMemoryAdsClient()))
            .Throws<ArgumentException>();
        await Assert.That(() => raw.PublishTcStructure(
                CreateOptions(amsNetId: " "),
                static () => new InMemoryAdsClient()))
            .Throws<ArgumentException>();
        await Assert.That(() => raw.PublishTcStructure(
                CreateOptions(adsPort: 0),
                static () => new InMemoryAdsClient()))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => raw.PublishTcStructure(
                CreateOptions(plcVariable: " "),
                static () => new InMemoryAdsClient()))
            .Throws<ArgumentException>();
        await Assert.That(() => resilient.PublishTcStructure(null!, CreateOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishTcStructure(structure, null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ((IObservable<IResilientMqttClient>)null!).PublishTcStructure(
                CreateOptions(),
                static () => new InMemoryAdsClient()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishTcStructure(null!, static () => new InMemoryAdsClient()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishTcStructure(CreateOptions(), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishTcStructure(
                CreateOptions(topicPrefix: " "),
                static () => new InMemoryAdsClient()))
            .Throws<ArgumentException>();
        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).PublishTcStructure(structure, CreateOptions()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.PublishTcStructure(null!, CreateOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.PublishTcStructure(structure, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IResilientMqttClient>)null!).PublishTcStructure(
                structure,
                CreateOptions()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.PublishTcStructure(null!, CreateOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.PublishTcStructure(structure, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies an owned ADS bridge links a structure, publishes, and releases resources.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedRawBridgeConnectsPublishesAndDisposesAdsAsync()
    {
        var linked = new TaskCompletionSource<HashTableRx>(TaskCreationOptions.RunContinuationsAsynchronously);
        using MockMqttClient mqtt = new();
        var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(
            PlcVariable,
            new NestedRig { Sensor = new() { Pressure = InitialPressure }, Ready = true });
        var options = CreateOptions(topicPrefix: "owned/rig");
        options.PlcVariable = PlcVariable;
        options.StructureLinked = structure => _ = linked.TrySetResult(structure);

        var bridge = Signal.Emit<IMqttClient>(mqtt).PublishTcStructure(options, () => ads);
        _ = await linked.Task.WaitAsync(Timeout);
        await WaitUntilAsync(() => mqtt.PublishedMessages.Count >= ExpectedRepublishCount);
        bridge.Dispose();
        bridge.Dispose();

        await Assert.That(CountMessages(mqtt.PublishedMessages, "owned/rig/Sensor/Pressure", InitialPressurePayload))
            .IsEqualTo(ExpectedInitialPublicationCount);
        await Assert.That(CountMessages(mqtt.PublishedMessages, "owned/rig/Ready", "True"))
            .IsEqualTo(ExpectedInitialPublicationCount);
        await Assert.That(ads.Connected).IsFalse();
    }

    /// <summary>Verifies an owned resilient ADS bridge routes setup failures through the configured handler.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedResilientBridgeRoutesSetupErrorsAsync()
    {
        var setupError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = CreateOptions(topicPrefix: "owned/resilient");
        options.ErrorHandler = error => _ = setupError.TrySetResult(error);

        var bridge = Signal.Emit(CreateResilientClient(new())).PublishTcStructure(
            options,
            static () => throw new InvalidOperationException("resilient factory failed"));
        var error = await setupError.Task.WaitAsync(Timeout);
        bridge.Dispose();

        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies the owned resilient publication callback handles a completing client stream.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedResilientBridgeHandlesCompletedClientStreamAsync()
    {
        var linked = new TaskCompletionSource<HashTableRx>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(PlcVariable, new RigValues { Pressure = InitialPressure });
        var options = CreateOptions(topicPrefix: "owned/resilient/completed");
        options.StructureLinked = structure => _ = linked.TrySetResult(structure);

        var bridge = Signal.Empty<IResilientMqttClient>().PublishTcStructure(options, () => ads);
        _ = await linked.Task.WaitAsync(Timeout);
        await Task.Delay(CollectionDrainMilliseconds);
        bridge.Dispose();

        await Assert.That(ads.Connected).IsFalse();
    }

    /// <summary>Verifies owned raw publication errors are safe when no error handler is configured.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedBridgeIgnoresPublicationErrorsWithoutHandlerAsync()
    {
        var linked = new TaskCompletionSource<HashTableRx>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(PlcVariable, new RigValues { Pressure = InitialPressure });
        var options = CreateOptions();
        options.StructureLinked = structure => _ = linked.TrySetResult(structure);
        options.TopicFactory = static (_, _) => throw new InvalidOperationException("ignored publish failed");

        var bridge = Signal.Emit<IMqttClient>(new MockMqttClient()).PublishTcStructure(options, () => ads);
        _ = await linked.Task.WaitAsync(Timeout);
        await Task.Delay(CollectionDrainMilliseconds);
        bridge.Dispose();

        await Assert.That(ads.Connected).IsFalse();
    }

    /// <summary>Verifies ADS client errors are forwarded through the owned bridge handler.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedBridgeRoutesAdsClientErrorsAsync()
    {
        var adsErrors = new TestSignal<Exception>();
        var observedError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ads = Substitute.For<IRxTcAdsClient>();
        _ = ads.ErrorReceived.Returns(adsErrors);
        ads.When(static client => client.Connect(Arg.Any<ISettings>())).Do(_ =>
            adsErrors.OnNext(new InvalidOperationException("ads callback failed")));
        var options = CreateOptions();
        options.ErrorHandler = error => _ = observedError.TrySetResult(error);

        var bridge = Signal.Emit<IMqttClient>(new MockMqttClient()).PublishTcStructure(options, () => ads);
        var error = await observedError.Task.WaitAsync(Timeout);
        bridge.Dispose();

        await Assert.That(error).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies owned bridge setup and publish errors are routed through the configured handler.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishTcStructure_OwnedBridgeRoutesSetupAndPublicationErrorsAsync()
    {
        var setupError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var setupOptions = CreateOptions();
        setupOptions.ErrorHandler = error => _ = setupError.TrySetResult(error);

        using var setupBridge = Signal.Emit<IMqttClient>(new MockMqttClient())
            .PublishTcStructure(setupOptions, static () => throw new InvalidOperationException("factory failed"));
        var setupException = await setupError.Task.WaitAsync(Timeout);

        var publishError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(PlcVariable, new RigValues { Pressure = InitialPressure });
        var publishOptions = CreateOptions();
        publishOptions.TopicFactory = static (_, _) => throw new InvalidOperationException("publish failed");
        publishOptions.ErrorHandler = error => _ = publishError.TrySetResult(error);

        using var publishBridge = Signal.Emit<IMqttClient>(new MockMqttClient()).PublishTcStructure(
            publishOptions,
            () => ads);
        var publishException = await publishError.Task.WaitAsync(Timeout);
        publishBridge.Dispose();

        await Assert.That(setupException).IsTypeOf<InvalidOperationException>();
        await Assert.That(publishException).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Creates a representative structure table with scalar, nested, array, object, and null leaves.</summary>
    /// <returns>The populated structure table.</returns>
    [RequiresUnreferencedCode("HashTableRx.SetStructure reflects over test data.")]
    private static HashTableRx CreateStructureTable()
    {
        var table = new HashTableRx(useUpperCase: false);
        table.SetStructure(CreateSetStructureSnapshot(InitialPressure, ready: true));
        table.Add("Name", "ready");
        table.Add("Enabled", true);
        table.Add("Grade", 'A');
        table.Add("Mode", BridgeMode.Automatic);
        table.Add("Amount", AmountValue);
        table.Add("Status", new PublicFieldStatus(StatusCode, StatusFlags));
        table.Add("Nullable", null!);
        return table;
    }

    /// <summary>Creates a reflected nested structure snapshot.</summary>
    /// <param name="pressure">The pressure value.</param>
    /// <param name="ready">The ready flag.</param>
    /// <returns>The nested structure snapshot.</returns>
    private static SetStructureSnapshot CreateSetStructureSnapshot(int pressure, bool ready) =>
        new()
        {
            Pressure = InitialPressure,
            Sensor = new() { Pressure = pressure, Temperature = NestedTemperature },
            Ready = ready,
        };

    /// <summary>Creates reusable TwinCAT structure bridge options.</summary>
    /// <param name="topicPrefix">The MQTT topic prefix.</param>
    /// <param name="amsNetId">The AMS Net ID.</param>
    /// <param name="adsPort">The ADS runtime port.</param>
    /// <param name="plcVariable">The PLC structure variable.</param>
    /// <param name="republishInterval">The optional republish interval.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether messages are retained.</param>
    /// <returns>The configured options.</returns>
    private static TwinCatStructureOptions CreateOptions(
        string topicPrefix = TopicPrefix,
        string amsNetId = "127.0.0.1.1.1",
        int adsPort = DefaultAdsPort,
        string plcVariable = PlcVariable,
        TimeSpan? republishInterval = null,
        MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce,
        bool retain = true) =>
        new()
        {
            AmsNetId = amsNetId,
            AdsPort = adsPort,
            PlcVariable = plcVariable,
            TopicPrefix = topicPrefix,
            RepublishInterval = republishInterval,
            QualityOfService = qos,
            Retain = retain,
        };

    /// <summary>Waits until a test condition becomes true.</summary>
    /// <param name="condition">The condition to poll.</param>
    /// <returns>The asynchronous wait.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var until = TimeProvider.System.GetTimestamp() + (long)(Timeout.TotalSeconds * TimeProvider.System.TimestampFrequency);
        while (!condition())
        {
            if (TimeProvider.System.GetTimestamp() >= until)
            {
                throw new TimeoutException("The expected TwinCAT bridge condition was not reached.");
            }

            await Task.Delay(PollDelayMilliseconds).ConfigureAwait(false);
        }
    }

    /// <summary>Asserts the exact MQTT application message created by the structure bridge.</summary>
    /// <param name="message">The published message.</param>
    /// <param name="topic">The expected topic.</param>
    /// <param name="payload">The expected payload.</param>
    /// <param name="options">The expected delivery options.</param>
    /// <returns>The asynchronous assertions.</returns>
    private static async Task AssertPublishedMessageAsync(
        MqttApplicationMessage message,
        string topic,
        string payload,
        TwinCatStructureOptions options)
    {
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEqualTo(payload);
        await Assert.That(message.QualityOfServiceLevel).IsEqualTo(options.QualityOfService);
        await Assert.That(message.Retain).IsEqualTo(options.Retain);
    }

    /// <summary>Counts published MQTT test messages that match an exact topic and payload.</summary>
    /// <param name="messages">The published MQTT messages.</param>
    /// <param name="topic">The expected topic.</param>
    /// <param name="payload">The expected payload.</param>
    /// <returns>The matching message count.</returns>
    private static int CountMessages(
        IEnumerable<MqttApplicationMessage> messages,
        string topic,
        string payload)
    {
        var count = 0;
        foreach (var message in messages)
        {
            if (string.Equals(message.Topic, topic, StringComparison.Ordinal) &&
                string.Equals(Encoding.UTF8.GetString(message.Payload), payload, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Counts low-level structure messages that match an exact topic and payload.</summary>
    /// <param name="messages">The observed structure messages.</param>
    /// <param name="topic">The expected topic.</param>
    /// <param name="payload">The expected payload.</param>
    /// <returns>The matching message count.</returns>
    private static int CountMessages(
        IEnumerable<(string Topic, string Payload)> messages,
        string topic,
        string payload)
    {
        var count = 0;
        foreach (var message in messages)
        {
            if (string.Equals(message.Topic, topic, StringComparison.Ordinal) &&
                string.Equals(message.Payload, payload, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Creates a resilient MQTT facade that exposes the processed-message stream used by publishers.</summary>
    /// <param name="processed">The processed-message stream.</param>
    /// <returns>The substitute resilient client.</returns>
    private static IResilientMqttClient CreateResilientClient(TestSignal<ApplicationMessageProcessedEventArgs> processed)
    {
        var resilient = Substitute.For<IResilientMqttClient>();
        _ = resilient.ApplicationMessageProcessed.Returns(processed);
        return resilient;
    }

    /// <summary>Configures a resilient client substitute to capture the next enqueued message.</summary>
    /// <param name="resilient">The resilient client substitute.</param>
    /// <param name="processed">The processed-message stream to notify.</param>
    /// <returns>The captured MQTT application message completion.</returns>
    private static TaskCompletionSource<MqttApplicationMessage> ConfigureResilientPublication(
        IResilientMqttClient resilient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed)
    {
        var completion = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = resilient.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            var message = call.Arg<MqttApplicationMessage>();
            processed.OnNext(new(new() { ApplicationMessage = message }, null));
            _ = completion.TrySetResult(message);
            return Task.CompletedTask;
        });

        return completion;
    }

    /// <summary>Represents a sample public-property status object.</summary>
    /// <param name="Code">The status code.</param>
    /// <param name="Flags">The status flags.</param>
    public readonly record struct PublicFieldStatus(int Code, int[] Flags);

    /// <summary>Represents a dynamically discovered PLC structure in the ADS simulator.</summary>
    public sealed class RigValues
    {
        /// <summary>Gets or sets the structure pressure member.</summary>
        public int Pressure { get; set; }
    }

    /// <summary>Represents a full reflected structure snapshot.</summary>
    public sealed class SetStructureSnapshot
    {
        /// <summary>Gets or sets the root pressure member.</summary>
        public int Pressure { get; set; }

        /// <summary>Gets or sets the nested sensor structure.</summary>
        public SensorValues Sensor { get; set; } = new();

        /// <summary>Gets or sets the array member.</summary>
        public int[] Values { get; } = StructureValues;

        /// <summary>Gets or sets a value indicating whether the rig is ready.</summary>
        public bool Ready { get; set; }
    }

    /// <summary>Represents nested sensor values in a reflected structure.</summary>
    public sealed class SensorValues
    {
        /// <summary>Gets or sets the nested pressure member.</summary>
        public int Pressure { get; set; }

        /// <summary>Gets or sets the nested temperature member.</summary>
        public double Temperature { get; set; }
    }

    /// <summary>Represents nested PLC data discovered dynamically by ADS.</summary>
    public sealed class NestedRig
    {
        /// <summary>Gets or sets the sensor structure.</summary>
        public RigValues Sensor { get; set; } = new();

        /// <summary>Gets or sets a value indicating whether the rig is ready.</summary>
        public bool Ready { get; set; }

        /// <summary>Gets or sets the array member.</summary>
        public int[] Values { get; } = StructureValues;
    }
}
#endif
