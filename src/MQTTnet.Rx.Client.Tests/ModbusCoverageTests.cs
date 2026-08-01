// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using ModbusCreate = MQTTnet.Rx.Modbus.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the synchronous Modbus-to-MQTT integration surface.</summary>
public class ModbusCoverageTests
{
    /// <summary>The topic used for scalar register writes.</summary>
    private const string RegisterTopic = "modbus/register";

    /// <summary>The topic used for multiple-register writes.</summary>
    private const string RegistersTopic = "modbus/registers";

    /// <summary>The topic used for scalar coil writes.</summary>
    private const string CoilTopic = "modbus/coil";

    /// <summary>The topic used for multiple-coil writes.</summary>
    private const string CoilsTopic = "modbus/coils";

    /// <summary>The MQTT topic used by publishing tests.</summary>
    private const string PublishTopic = "modbus/publish";

    /// <summary>The number of messages created by the publishing test.</summary>
    private const int ExpectedPublishedMessageCount = 2;

    /// <summary>The first value used by serialization tests.</summary>
    private const int FirstSerializedValue = 1;

    /// <summary>The second value used by serialization tests.</summary>
    private const int SecondSerializedValue = 2;

    /// <summary>The third value used by serialization tests.</summary>
    private const int ThirdSerializedValue = 3;

    /// <summary>The binary payload source value.</summary>
    private const int BinaryPayloadValue = 456;

    /// <summary>The scalar payload source value.</summary>
    private const int ScalarPayloadValue = 123;

    /// <summary>The single-register Modbus address.</summary>
    private const ushort SingleRegisterAddress = 7;

    /// <summary>The multiple-register Modbus address.</summary>
    private const ushort MultipleRegisterAddress = 8;

    /// <summary>The single-coil Modbus address.</summary>
    private const ushort SingleCoilAddress = 9;

    /// <summary>The multiple-coil Modbus address.</summary>
    private const ushort MultipleCoilAddress = 10;

    /// <summary>The raw single-register payload value.</summary>
    private const ushort RawSingleRegisterValue = 12;

    /// <summary>The first raw multiple-register payload value.</summary>
    private const ushort RawFirstMultipleRegisterValue = 13;

    /// <summary>The second raw multiple-register payload value.</summary>
    private const ushort RawSecondMultipleRegisterValue = 14;

    /// <summary>The resilient single-register payload value.</summary>
    private const ushort ResilientSingleRegisterValue = 15;

    /// <summary>The first resilient multiple-register payload value.</summary>
    private const ushort ResilientFirstMultipleRegisterValue = 16;

    /// <summary>The second resilient multiple-register payload value.</summary>
    private const ushort ResilientSecondMultipleRegisterValue = 17;

    /// <summary>The expected serialized values.</summary>
    private static readonly int[] SerializedValues =
        [FirstSerializedValue, SecondSerializedValue, ThirdSerializedValue,];

    /// <summary>The expected raw multiple-register values.</summary>
    private static readonly ushort[] RawMultipleRegisterValues =
        [RawFirstMultipleRegisterValue, RawSecondMultipleRegisterValue,];

    /// <summary>The expected resilient multiple-register values.</summary>
    private static readonly ushort[] ResilientMultipleRegisterValues =
        [ResilientFirstMultipleRegisterValue, ResilientSecondMultipleRegisterValue,];

    /// <summary>The expected raw multiple-coil values.</summary>
    private static readonly bool[] RawMultipleCoilValues = [true, false];

    /// <summary>The expected resilient multiple-coil values.</summary>
    private static readonly bool[] ResilientMultipleCoilValues = [false, true];

    /// <summary>Verifies the master factory and JSON helpers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task FactoriesAndSerialization_ExposeExpectedValuesAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();

        var fromMaster = await ModbusCreate.FromMaster(master).FirstAsync();
        await Assert.That(fromMaster.Connected).IsTrue();
        await Assert.That(fromMaster.Error).IsNull();
        await Assert.That(fromMaster.Master).IsSameReferenceAs(master);

        var fromFactory = await ModbusCreate.FromFactory(simulator.CreateMaster).FirstAsync();
        await Assert.That(fromFactory.Connected).IsTrue();
        await Assert.That(fromFactory.Error).IsNull();
        await Assert.That(fromFactory.Master).IsNotNull();

        const string expectedJson = "[1,2,3]";
        var serialized = SerializedValues.Serialize();
        await Assert.That(serialized).IsEqualTo(expectedJson);
        await Assert.That(serialized.DeSerialize<int[]>()).IsEquivalentTo(SerializedValues);
        await Assert.That(((object?)null).Serialize()).IsEqualTo("null");
    }

    /// <summary>Verifies that PublishModbus supports both documented payload forms.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PublishModbus_PublishesStringAndBinaryPayloadsAsync()
    {
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var stringReader = Signal.Emit<(bool Connected, Exception? Error, object? Data)>(
            (true, null, ScalarPayloadValue));
        var binaryReader = Signal.Emit<(bool Connected, Exception? Error, object? Data)>(
            (true, null, BinaryPayloadValue));

        await clients.PublishModbus(
            stringReader,
            PublishTopic,
            static value => value.ToString() ?? string.Empty).FirstAsync();
        await clients.PublishModbus(
            binaryReader,
            PublishTopic,
            static value => BitConverter.GetBytes((int)value)).FirstAsync();

        await Assert.That(mqttClient.PublishedMessages.Count).IsEqualTo(ExpectedPublishedMessageCount);
        await Assert.That(mqttClient.PublishedMessages[0].Topic).IsEqualTo(PublishTopic);
        await Assert.That(mqttClient.PublishedMessages[0].ConvertPayloadToString()).IsEqualTo("123");
        await Assert.That(mqttClient.PublishedMessages[1].Payload.ToArray()).IsEquivalentTo(
            BitConverter.GetBytes(BinaryPayloadValue));
    }

    /// <summary>Verifies the null-data filter and the unsupported payload error path.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PublishModbus_FiltersNullDataAndRejectsUnsupportedPayloadsAsync()
    {
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var reader = Signal.Emit<(bool Connected, Exception? Error, object? Data)>((true, null, null));

        await Assert.That(async () => await clients.PublishModbus(
            reader,
            PublishTopic,
            static _ => "ignored").FirstAsync())
            .Throws<InvalidOperationException>();
        await Assert.That(() => clients.PublishModbus(reader, PublishTopic, static _ => 1))
            .Throws<NotSupportedException>();
        await Assert.That(mqttClient.PublishedMessages.Count).IsEqualTo(0);
    }

    /// <summary>Verifies raw-client convenience subscriptions parse every supported primitive form.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawClientConvenienceSubscriptions_ParseIncomingPayloadsAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockMqttClient();
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master));
        ushort singleRegister = 0;
        ushort[]? multipleRegisters = null;
        var singleCoil = false;
        bool[]? multipleCoils = null;
        ushort singleRegisterAddress = 0;
        ushort multipleRegistersAddress = 0;
        ushort singleCoilAddress = 0;
        ushort multipleCoilsAddress = 0;

        using var singleRegisterSubscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWriteSingleRegister(
            modbus,
            RegisterTopic,
            SingleRegisterAddress,
            (_, address, value) => (singleRegisterAddress, singleRegister) = (address, value));
        using var multipleRegistersSubscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWriteMultipleRegisters(
            modbus,
            RegistersTopic,
            MultipleRegisterAddress,
            (_, address, values) => (multipleRegistersAddress, multipleRegisters) = (address, values));
        using var singleCoilSubscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWriteSingleCoil(
            modbus,
            CoilTopic,
            SingleCoilAddress,
            (_, address, value) => (singleCoilAddress, singleCoil) = (address, value));
        using var multipleCoilsSubscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWriteMultipleCoils(
            modbus,
            CoilsTopic,
            MultipleCoilAddress,
            (_, address, values) => (multipleCoilsAddress, multipleCoils) = (address, values));

        await mqttClient.SimulateMessageReceivedAsync(RegisterTopic, RawSingleRegisterValue.ToString());
        await mqttClient.SimulateMessageReceivedAsync(RegistersTopic, string.Join(", ", RawMultipleRegisterValues));
        await mqttClient.SimulateMessageReceivedAsync(CoilTopic, "true");
        await mqttClient.SimulateMessageReceivedAsync(CoilsTopic, "true, false");

        await Assert.That(singleRegister).IsEqualTo(RawSingleRegisterValue);
        await Assert.That(multipleRegisters).IsEquivalentTo(RawMultipleRegisterValues);
        await Assert.That(singleCoil).IsTrue();
        await Assert.That(multipleCoils).IsEquivalentTo(RawMultipleCoilValues);
        await Assert.That(singleRegisterAddress).IsEqualTo(SingleRegisterAddress);
        await Assert.That(multipleRegistersAddress).IsEqualTo(MultipleRegisterAddress);
        await Assert.That(singleCoilAddress).IsEqualTo(SingleCoilAddress);
        await Assert.That(multipleCoilsAddress).IsEqualTo(MultipleCoilAddress);
    }

    /// <summary>Verifies resilient-client convenience subscriptions parse every supported primitive form.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientConvenienceSubscriptions_ParseIncomingPayloadsAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockResilientMqttClient();
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master));
        ushort singleRegister = 0;
        ushort[]? multipleRegisters = null;
        var singleCoil = false;
        bool[]? multipleCoils = null;
        ushort singleRegisterAddress = 0;
        ushort multipleRegistersAddress = 0;
        ushort singleCoilAddress = 0;
        ushort multipleCoilsAddress = 0;

        using var singleRegisterSubscription = Signal.Emit<IResilientMqttClient>(mqttClient)
            .SubscribeWriteSingleRegister(
            modbus,
            RegisterTopic,
            SingleRegisterAddress,
            (_, address, value) => (singleRegisterAddress, singleRegister) = (address, value));
        using var multipleRegistersSubscription = Signal.Emit<IResilientMqttClient>(mqttClient)
            .SubscribeWriteMultipleRegisters(
            modbus,
            RegistersTopic,
            MultipleRegisterAddress,
            (_, address, values) => (multipleRegistersAddress, multipleRegisters) = (address, values));
        using var singleCoilSubscription = Signal.Emit<IResilientMqttClient>(mqttClient).SubscribeWriteSingleCoil(
            modbus,
            CoilTopic,
            SingleCoilAddress,
            (_, address, value) => (singleCoilAddress, singleCoil) = (address, value));
        using var multipleCoilsSubscription = Signal.Emit<IResilientMqttClient>(mqttClient).SubscribeWriteMultipleCoils(
            modbus,
            CoilsTopic,
            MultipleCoilAddress,
            (_, address, values) => (multipleCoilsAddress, multipleCoils) = (address, values));

        await mqttClient.SimulateMessageReceivedAsync(RegisterTopic, ResilientSingleRegisterValue.ToString());
        await mqttClient.SimulateMessageReceivedAsync(
            RegistersTopic,
            string.Join(", ", ResilientMultipleRegisterValues));
        await mqttClient.SimulateMessageReceivedAsync(CoilTopic, "false");
        await mqttClient.SimulateMessageReceivedAsync(CoilsTopic, "false, true");

        await Assert.That(singleRegister).IsEqualTo(ResilientSingleRegisterValue);
        await Assert.That(multipleRegisters).IsEquivalentTo(ResilientMultipleRegisterValues);
        await Assert.That(singleCoil).IsFalse();
        await Assert.That(multipleCoils).IsEquivalentTo(ResilientMultipleCoilValues);
        await Assert.That(singleRegisterAddress).IsEqualTo(SingleRegisterAddress);
        await Assert.That(multipleRegistersAddress).IsEqualTo(MultipleRegisterAddress);
        await Assert.That(singleCoilAddress).IsEqualTo(SingleCoilAddress);
        await Assert.That(multipleCoilsAddress).IsEqualTo(MultipleCoilAddress);
    }
}
