// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using IoT.Driver.ModbusRx.Data;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using ModbusCreate = MQTTnet.Rx.Modbus.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the Modbus bridge against simulator memory and a real loopback MQTT broker.</summary>
public sealed partial class ModbusLiveSimulatorBridgeTests
{
    /// <summary>The simulated Modbus unit identifier.</summary>
    private const byte UnitId = 0;

    /// <summary>The number of points used by live multi-value operations.</summary>
    private const ushort PointCount = 2;

    /// <summary>The first nonzero Modbus address used by read tests.</summary>
    private const ushort ReadAddress = 3;

    /// <summary>The first nonzero holding-register address used by write tests.</summary>
    private const ushort RegisterWriteAddress = 7;

    /// <summary>The first nonzero coil address used by write tests.</summary>
    private const ushort CoilWriteAddress = 11;

    /// <summary>The interval used by explicit polling overloads.</summary>
    private const double PollingIntervalMilliseconds = 1.0;

    /// <summary>The number of entries allocated in each simulated data area.</summary>
    private const ushort DataAreaSize = 32;

    /// <summary>The first seeded input-register value.</summary>
    private const ushort InputRegisterValue = 1301;

    /// <summary>The first seeded holding-register value.</summary>
    private const ushort HoldingRegisterValue = 2301;

    /// <summary>The raw MQTT register-write value.</summary>
    private const ushort RawRegisterWriteValue = 3301;

    /// <summary>The resilient MQTT register-write value.</summary>
    private const ushort ResilientRegisterWriteValue = 4301;

    /// <summary>The second raw MQTT register-write value.</summary>
    private const ushort RawRegisterWriteValueTwo = 3302;

    /// <summary>The expected serialized input-register values.</summary>
    private const string ExpectedInputRegistersJson = "[1301,1302]";

    /// <summary>The expected serialized holding-register values.</summary>
    private const string ExpectedHoldingRegistersJson = "[2301,2302]";

    /// <summary>The expected serialized discrete-input values.</summary>
    private const string ExpectedInputsJson = "[true,false]";

    /// <summary>The expected serialized coil values.</summary>
    private const string ExpectedCoilsJson = "[false,true]";

    /// <summary>The JSON supplied to the type-witness deserializer.</summary>
    private const string WitnessJson = "[5,8]";

    /// <summary>The topic prefix shared by this fixture.</summary>
    private const string TopicPrefix = "modbus/live";

    /// <summary>The maximum duration allowed for a live operation.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>The expected raw multiple-register write.</summary>
    private static readonly ushort[] RawRegisterWriteValues =
        [RawRegisterWriteValue, RawRegisterWriteValueTwo,];

    /// <summary>The expected raw multiple-coil write.</summary>
    private static readonly bool[] RawCoilWriteValues = [true, false];

    /// <summary>The expected type-witness deserialization result.</summary>
    private static readonly int[] WitnessValues = [5, 8];

    /// <summary>Proves raw simulator values flow through MQTT in both directions.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RawBridge_MovesRegistersAndCoilsBothWaysOverLiveMqttAsync()
    {
        using var dataStore = CreateSeededDataStore();
        using var simulator = new ModbusSimulator(UnitId, dataStore);
        using var master = simulator.CreateMaster();
        var modbus = ModbusCreate.FromMaster(master);
        await using var broker = await LiveMqttBroker.StartAsync();
        await ConnectBrokerAsync(broker);

        await AssertRawPublishAsync(
            broker,
            $"{TopicPrefix}/raw/input-registers",
            broker.Bridge.PublishInputRegisters(
                modbus,
                $"{TopicPrefix}/raw/input-registers",
                ReadAddress,
                PointCount,
                PollingIntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true),
            ExpectedInputRegistersJson);
        await AssertRawPublishAsync(
            broker,
            $"{TopicPrefix}/raw/holding-registers",
            ModbusCreate.PublishHoldingRegisters(
                broker.Bridge,
                modbus,
                $"{TopicPrefix}/raw/holding-registers",
                ReadAddress,
                PointCount),
            ExpectedHoldingRegistersJson);
        await AssertRawPublishAsync(
            broker,
            $"{TopicPrefix}/raw/inputs",
            broker.Bridge.PublishInputs(
                modbus,
                $"{TopicPrefix}/raw/inputs",
                ReadAddress,
                PointCount,
                PollingIntervalMilliseconds),
            ExpectedInputsJson);
        await AssertRawPublishAsync(
            broker,
            $"{TopicPrefix}/raw/coils",
            broker.Bridge.PublishCoils(
                modbus,
                $"{TopicPrefix}/raw/coils",
                ReadAddress,
                PointCount,
                PollingIntervalMilliseconds,
                MqttQualityOfServiceLevel.AtLeastOnce),
            ExpectedCoilsJson);

        await AssertRawWritesReachDataStoreAsync(broker, dataStore, modbus);
        await broker.DisposeAsync();
        await broker.DisposeAsync();
        await Assert.That(broker.IsDisposed).IsTrue();
        await Assert.That(broker.TeardownException).IsNull();
    }

    /// <summary>Proves resilient and asynchronous bridges move simulator values through the real broker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResilientAndAsyncBridges_MoveValuesBothWaysOverLiveMqttAsync()
    {
        using var dataStore = CreateSeededDataStore();
        using var simulator = new ModbusSimulator(UnitId, dataStore);
        using var master = simulator.CreateMaster();
        var modbus = ModbusCreate.FromMaster(master);
        await using var broker = await LiveMqttBroker.StartAsync();
        await ConnectBrokerAsync(broker);
        await using var resilient = await LiveResilientSource.StartAsync(broker);

        await AssertResilientPublishAsync(
            broker,
            $"{TopicPrefix}/resilient/holding-registers",
            resilient.Source.PublishHoldingRegisters(
                modbus,
                $"{TopicPrefix}/resilient/holding-registers",
                ReadAddress,
                PointCount,
                PollingIntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true),
            ExpectedHoldingRegistersJson);

        var asyncMaster = SignalAsync.Return(
            (Connected: true, Error: (Exception?)null, Master: (ModbusIpMaster?)master));
        await AssertRawAsyncPublishAsync(
            broker,
            $"{TopicPrefix}/async/raw/coils",
            SignalAsync.Return(broker.BridgeClient).PublishCoils(
                asyncMaster,
                $"{TopicPrefix}/async/raw/coils",
                ReadAddress,
                PointCount,
                PollingIntervalMilliseconds,
                MqttQualityOfServiceLevel.AtLeastOnce,
                false),
            ExpectedCoilsJson);
        await AssertResilientAsyncPublishAsync(
            broker,
            $"{TopicPrefix}/async/resilient/input-registers",
            SignalAsync.Return(resilient.Client).PublishInputRegisters(
                asyncMaster,
                $"{TopicPrefix}/async/resilient/input-registers",
                ReadAddress,
                PointCount),
            ExpectedInputRegistersJson);

        await AssertResilientWriteReachesDataStoreAsync(
            broker,
            dataStore,
            modbus,
            resilient.Source);
    }

    /// <summary>Verifies factory ownership, type-witness serialization, and guarded extension entry points.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FactoriesSerializationAndValidation_ExposeCompleteContractsAsync()
    {
        using var simulator = new ModbusSimulator(UnitId);
        using var master = simulator.CreateMaster();
        var fromMaster = await ModbusCreate.FromMaster(master).FirstAsync(Timeout);
        var fromFactory = await ModbusCreate.FromFactory(simulator.CreateMaster).FirstAsync(Timeout);
        var asyncFromMaster = await ObservableAsyncCreateExtensions.FromMasterAsync(master).FirstAsync(Timeout);
        var asyncFromFactory = await ObservableAsyncCreateExtensions.FromFactoryAsync(simulator.CreateMaster)
            .FirstAsync(Timeout);

        await Assert.That(fromMaster.Master).IsSameReferenceAs(master);
        await Assert.That(fromFactory.Master).IsNotNull();
        await Assert.That(asyncFromMaster.Master).IsSameReferenceAs(master);
        await Assert.That(asyncFromFactory.Master).IsNotNull();
        fromFactory.Master!.Dispose();
        asyncFromFactory.Master!.Dispose();

        int[][] witness = [];
        var deserialized = WitnessJson.DeSerialize(witness);
        var staticDeserialized = ModbusCreate.DeSerialize(WitnessJson, witness);
        await Assert.That(deserialized).IsEquivalentTo(WitnessValues);
        await Assert.That(staticDeserialized).IsEquivalentTo(WitnessValues);
        await Assert.That(ModbusCreate.Serialize((object?)null)).IsEqualTo("null");

        await Assert.That(static () => ModbusCreate.FromMaster(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ModbusCreate.FromFactory(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => "not-json".DeSerialize(witness)).Throws<System.Text.Json.JsonException>();
        await AssertSynchronousValidationAsync(master);
        await AssertAsynchronousValidationAsync(master);
    }

    /// <summary>Connects both MQTT clients and verifies their broker acknowledgements.</summary>
    /// <param name="broker">The live broker.</param>
    /// <returns>A task representing the connection readiness assertion.</returns>
    private static async Task ConnectBrokerAsync(LiveMqttBroker broker)
    {
        var results = await broker.ConnectClientsAsync();

        await Assert.That(results.Bridge.ResultCode).IsEqualTo(MqttClientConnectResultCode.Success);
        await Assert.That(results.Probe.ResultCode).IsEqualTo(MqttClientConnectResultCode.Success);
        await Assert.That(broker.BridgeClient.IsConnected).IsTrue();
        await Assert.That(broker.ProbeClient.IsConnected).IsTrue();
    }

    /// <summary>Creates a probe after both the SUBACK and broker-side subscription event.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="topic">The exact topic to observe.</param>
    /// <returns>The acknowledged live subscription.</returns>
    private static async Task<LiveMqttSubscription> SubscribeReadyProbeAsync(
        LiveMqttBroker broker,
        string topic)
    {
        var probe = await broker.SubscribeProbeAsync(topic);
        try
        {
            await probe.SubscriptionReady.WaitAsync(Timeout);
            await Assert.That(probe.SubscribeResult).IsNotNull();
            await Assert.That(probe.SubscribeResultCode)
                .IsEqualTo(MqttClientSubscribeResultCode.GrantedQoS1);
            return probe;
        }
        catch
        {
            await probe.DisposeAsync();
            throw;
        }
    }

    /// <summary>Publishes a simulator value with a raw bridge and verifies the broker payload.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="topic">The exact MQTT topic.</param>
    /// <param name="results">The bridge publish result stream.</param>
    /// <param name="expectedPayload">The expected UTF-8 payload.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertRawPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservable<MqttClientPublishResult> results,
        string expectedPayload)
    {
        await using var probe = await SubscribeReadyProbeAsync(broker, topic);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);
        var result = await results.FirstAsync(Timeout);
        var message = await messageTask;

        await Assert.That(result.ReasonCode is
            MqttClientPublishReasonCode.Success or
            MqttClientPublishReasonCode.NoMatchingSubscribers).IsTrue();
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEqualTo(expectedPayload);
    }

    /// <summary>Publishes a simulator value with a resilient bridge and verifies the broker payload.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="topic">The exact MQTT topic.</param>
    /// <param name="results">The bridge processed-message stream.</param>
    /// <param name="expectedPayload">The expected UTF-8 payload.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertResilientPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservable<ApplicationMessageProcessedEventArgs> results,
        string expectedPayload)
    {
        await using var probe = await SubscribeReadyProbeAsync(broker, topic);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);
        var processed = await results.FirstAsync(Timeout);
        var message = await messageTask;

        await Assert.That(processed.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEqualTo(expectedPayload);
    }

    /// <summary>Publishes a simulator value with an asynchronous raw bridge.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="topic">The exact MQTT topic.</param>
    /// <param name="results">The asynchronous publish result stream.</param>
    /// <param name="expectedPayload">The expected UTF-8 payload.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertRawAsyncPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservableAsync<MqttClientPublishResult> results,
        string expectedPayload)
    {
        await using var probe = await SubscribeReadyProbeAsync(broker, topic);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);
        var result = await results.FirstAsync(Timeout);
        var message = await messageTask;

        await Assert.That(result.ReasonCode is
            MqttClientPublishReasonCode.Success or
            MqttClientPublishReasonCode.NoMatchingSubscribers).IsTrue();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEqualTo(expectedPayload);
    }

    /// <summary>Publishes a simulator value with an asynchronous resilient bridge.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="topic">The exact MQTT topic.</param>
    /// <param name="results">The asynchronous processed-message stream.</param>
    /// <param name="expectedPayload">The expected UTF-8 payload.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertResilientAsyncPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservableAsync<ApplicationMessageProcessedEventArgs> results,
        string expectedPayload)
    {
        await using var probe = await SubscribeReadyProbeAsync(broker, topic);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);
        var processed = await results.FirstAsync(Timeout);
        var message = await messageTask;

        await Assert.That(processed.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload)).IsEqualTo(expectedPayload);
    }

    /// <summary>Publishes retained raw MQTT commands and verifies nonzero forwarded Modbus addresses.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="dataStore">The simulator data store.</param>
    /// <param name="modbus">The connected master stream.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertRawWritesReachDataStoreAsync(
        LiveMqttBroker broker,
        DataStore dataStore,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus)
    {
        const string registerTopic = $"{TopicPrefix}/raw/write/registers";
        const string coilTopic = $"{TopicPrefix}/raw/write/coils";
        await PublishRetainedAsync(broker.ProbeClient, registerTopic, "3301,3302");
        await PublishRetainedAsync(broker.ProbeClient, coilTopic, "true,false");
        using var registerWritten = new DataStoreWriteSignal(
            dataStore,
            ModbusDataType.HoldingRegister,
            RegisterWriteAddress);
        using var coilWritten = new DataStoreWriteSignal(dataStore, ModbusDataType.Coil, CoilWriteAddress);
        var writer = new ModbusWriteRecorder();

        using var registerSubscription = ModbusCreate.SubscribeWriteMultipleRegisters(
            broker.Bridge,
            modbus,
            registerTopic,
            RegisterWriteAddress,
            writer.WriteRegisters);
        using var coilSubscription = broker.Bridge.SubscribeWriteMultipleCoils(
            modbus,
            coilTopic,
            CoilWriteAddress,
            writer.WriteCoils);

        await (await writer.RegisterWrite.WaitAsync(Timeout)).WaitAsync(Timeout);
        await (await writer.CoilWrite.WaitAsync(Timeout)).WaitAsync(Timeout);
        await registerWritten.Completion.WaitAsync(Timeout);
        await coilWritten.Completion.WaitAsync(Timeout);
        await AssertDataStoreWritesAsync(dataStore, RawRegisterWriteValues, RawCoilWriteValues);
    }

    /// <summary>Publishes a retained resilient MQTT command and verifies simulator memory.</summary>
    /// <param name="broker">The live broker.</param>
    /// <param name="dataStore">The simulator data store.</param>
    /// <param name="modbus">The connected master stream.</param>
    /// <param name="resilient">The connected resilient client stream.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertResilientWriteReachesDataStoreAsync(
        LiveMqttBroker broker,
        DataStore dataStore,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        IObservable<IResilientMqttClient> resilient)
    {
        const string topic = $"{TopicPrefix}/resilient/write/register";
        await PublishRetainedAsync(
            broker.ProbeClient,
            topic,
            ResilientRegisterWriteValue.ToString(CultureInfo.InvariantCulture));
        using var written = new DataStoreWriteSignal(
            dataStore,
            ModbusDataType.HoldingRegister,
            RegisterWriteAddress);
        var writer = new ModbusWriteRecorder();
        using var subscription = ModbusCreate.SubscribeWriteSingleRegister(
            resilient,
            modbus,
            topic,
            RegisterWriteAddress,
            writer.WriteRegister);

        await (await writer.RegisterWrite.WaitAsync(Timeout)).WaitAsync(Timeout);
        await written.Completion.WaitAsync(Timeout);
        ushort actual;
        dataStore.Lock.EnterReadLock();
        try
        {
            actual = dataStore.HoldingRegisters[RegisterWriteAddress + 1];
        }
        finally
        {
            dataStore.Lock.ExitReadLock();
        }

        await Assert.That(actual).IsEqualTo(ResilientRegisterWriteValue);
    }

    /// <summary>Verifies simulator memory after raw MQTT register and coil commands.</summary>
    /// <param name="dataStore">The simulator data store.</param>
    /// <param name="expectedRegisters">The expected register values.</param>
    /// <param name="expectedCoils">The expected coil values.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertDataStoreWritesAsync(
        DataStore dataStore,
        ushort[] expectedRegisters,
        bool[] expectedCoils)
    {
        ushort[] registers;
        bool[] coils;
        dataStore.Lock.EnterReadLock();
        try
        {
            registers =
            [
                dataStore.HoldingRegisters[RegisterWriteAddress + 1],
                dataStore.HoldingRegisters[RegisterWriteAddress + PointCount],
            ];
            coils =
            [
                dataStore.CoilDiscretes[CoilWriteAddress + 1],
                dataStore.CoilDiscretes[CoilWriteAddress + PointCount],
            ];
        }
        finally
        {
            dataStore.Lock.ExitReadLock();
        }

        await Assert.That(registers).IsEquivalentTo(expectedRegisters);
        await Assert.That(coils).IsEquivalentTo(expectedCoils);
    }

    /// <summary>Publishes a retained MQTT command used to remove subscription-readiness races.</summary>
    /// <param name="client">The connected probe client.</param>
    /// <param name="topic">The command topic.</param>
    /// <param name="payload">The command payload.</param>
    /// <returns>A task representing the publish.</returns>
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

    /// <summary>Creates deterministic Modbus simulator memory.</summary>
    /// <returns>The seeded data store.</returns>
    private static DataStore CreateSeededDataStore()
    {
        var dataStore = DataStoreFactory.CreateDefaultDataStore(
            DataAreaSize,
            DataAreaSize,
            DataAreaSize,
            DataAreaSize);
        dataStore.WriteDataOptimized(
            [InputRegisterValue, (ushort)(InputRegisterValue + 1)],
            dataStore.InputRegisters,
            ReadAddress);
        dataStore.WriteDataOptimized(
            [HoldingRegisterValue, (ushort)(HoldingRegisterValue + 1)],
            dataStore.HoldingRegisters,
            ReadAddress);
        dataStore.WriteDataOptimized([true, false], dataStore.InputDiscretes, ReadAddress);
        dataStore.WriteDataOptimized([false, true], dataStore.CoilDiscretes, ReadAddress);

        return dataStore;
    }

    /// <summary>Exercises synchronous null guards and unsupported payload branches.</summary>
    /// <param name="master">A live simulator master used only for type-correct streams.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertSynchronousValidationAsync(ModbusIpMaster master)
    {
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();
        var modbus = Signal.Emit((Connected: true, Error: (Exception?)null, Master: (ModbusIpMaster?)master));
        var reader = Signal.Emit((Connected: true, Error: (Exception?)null, Data: (object?)1));

        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishCoils(
            modbus,
            TopicPrefix,
            ReadAddress,
            PointCount,
            PollingIntervalMilliseconds,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false))
            .Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishCoils(
            null!,
            TopicPrefix,
            ReadAddress,
            PointCount,
            PollingIntervalMilliseconds,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishModbus(reader, TopicPrefix, static _ => 1))
            .Throws<NotSupportedException>();
        await Assert.That(() => resilient.PublishModbus(reader, TopicPrefix, static _ => 1))
            .Throws<NotSupportedException>();
        await AssertSubscribeValidationAsync(raw, resilient, modbus);
    }

    /// <summary>Exercises synchronous subscription null guards.</summary>
    /// <param name="raw">The raw-client stream.</param>
    /// <param name="resilient">The resilient-client stream.</param>
    /// <param name="modbus">The simulator master stream.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertSubscribeValidationAsync(
        IObservable<IMqttClient> raw,
        IObservable<IResilientMqttClient> resilient,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus)
    {
        await Assert.That(() => ((IObservable<IMqttClient>)null!).SubscribeWrite(
            modbus,
            TopicPrefix,
            int.Parse,
            static (_, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => raw.SubscribeWrite(
            null!,
            TopicPrefix,
            int.Parse,
            static (_, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => raw.SubscribeWrite(
            modbus,
            TopicPrefix,
            (Func<string, int>)null!,
            static (_, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => raw.SubscribeWrite(
            modbus,
            TopicPrefix,
            int.Parse,
            (Action<ModbusIpMaster, int>)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.SubscribeWrite(
            modbus,
            TopicPrefix,
            int.Parse,
            (Func<ModbusIpMaster, int, Task>)null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Exercises asynchronous null guards and unsupported payload branches.</summary>
    /// <param name="master">A live simulator master used only for type-correct streams.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertAsynchronousValidationAsync(ModbusIpMaster master)
    {
        var raw = SignalAsync.None<IMqttClient>();
        var resilient = SignalAsync.None<IResilientMqttClient>();
        var modbus = SignalAsync.Return(
            (Connected: true, Error: (Exception?)null, Master: (ModbusIpMaster?)master));
        var reader = SignalAsync.Return(
            (Connected: true, Error: (Exception?)null, Data: (object?)1));

        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).PublishCoils(
            modbus,
            TopicPrefix,
            ReadAddress,
            PointCount,
            PollingIntervalMilliseconds,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false))
            .Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishCoils(
            null!,
            TopicPrefix,
            ReadAddress,
            PointCount,
            PollingIntervalMilliseconds,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishModbus(reader, TopicPrefix, static _ => 1))
            .Throws<NotSupportedException>();
        await Assert.That(() => resilient.PublishModbus(reader, TopicPrefix, static _ => 1))
            .Throws<NotSupportedException>();
    }
}
