// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.ModbusRx.Reactive.Device;
#else
using IoT.Driver.ModbusRx.Device;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using MQTTnet.Rx.Modbus.Reactive;
#else
using MQTTnet.Rx.Modbus;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the asynchronous Modbus publishing adapters through the loopback simulator.</summary>
public sealed class Wave2ModbusCoverageTests
{
    /// <summary>The topic used by the asynchronous publishing tests.</summary>
    private const string Topic = "coverage/modbus";

    /// <summary>The address used by Modbus reads.</summary>
    private const ushort Address = 0;

    /// <summary>The number of Modbus values to read.</summary>
    private const ushort NumberOfPoints = 1;

    /// <summary>The interval in milliseconds used by Modbus polling.</summary>
    private const double IntervalMilliseconds = 1.0;

    /// <summary>The number of completion signals sent while awaiting a resilient publish.</summary>
    private const int MaximumProcessSignals = 20;

    /// <summary>The minimum number of raw-client messages expected.</summary>
    private const int MinimumPublishedMessageCount = 5;

    /// <summary>The payload value used by custom Modbus publishing.</summary>
    private const string PayloadText = "payload";

    /// <summary>The maximum duration allowed for a loopback publish.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>The delay between resilient completion signals.</summary>
    private static readonly TimeSpan ProcessSignalDelay = TimeSpan.FromMilliseconds(10);

    /// <summary>The binary payload used to exercise the resilient byte-array publishing path.</summary>
    private static readonly byte[] BinaryPayload = [1];

    /// <summary>Verifies the async Modbus master factories produce a usable connection tuple.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncFactories_ExposeMasterAndFactoryResultsAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();

        var fromMaster = await ObservableAsyncCreateExtensions.FromMasterAsync(master).FirstAsync(Timeout);
        var fromFactory = await ObservableAsyncCreateExtensions
            .FromFactoryAsync(simulator.CreateMaster)
            .FirstAsync(Timeout);

        await Assert.That(fromMaster.Connected).IsTrue();
        await Assert.That(fromMaster.Master).IsSameReferenceAs(master);
        await Assert.That(fromFactory.Connected).IsTrue();
        await Assert.That(fromFactory.Master).IsNotNull();
        fromFactory.Master!.Dispose();
    }

    /// <summary>Verifies all raw-client asynchronous read publishing overloads.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawClientAsyncPublishOverloads_PublishAllReadKindsAndPayloadAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient).ToSignal();
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>(
            (true, null, master)).ToSignal();

        _ = await clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds)
            .FirstAsync(Timeout);
        _ = await clients.PublishHoldingRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds)
            .FirstAsync(Timeout);
        _ = await clients.PublishInputs(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds)
            .FirstAsync(Timeout);
        _ = await clients.PublishCoils(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds)
            .FirstAsync(Timeout);
        _ = await clients.PublishModbus(
            Signal.Emit<(bool Connected, Exception? Error, object? Data)>((true, null, PayloadText)).ToSignal(),
            Topic,
            static value => value.ToString() ?? string.Empty).FirstAsync(Timeout);

        await Assert.That(mqttClient.PublishedMessages.Count).IsGreaterThanOrEqualTo(MinimumPublishedMessageCount);
    }

    /// <summary>Verifies all resilient-client asynchronous read publishing overloads.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientAsyncPublishOverloads_PublishAllReadKindsAndPayloadAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockResilientMqttClient();
        var clients = Signal.Emit<IResilientMqttClient>(mqttClient).ToSignal();
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>(
            (true, null, master)).ToSignal();

        await PublishResilientAsync(
            mqttClient,
            clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds));
        await PublishResilientAsync(
            mqttClient,
            clients.PublishHoldingRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds));
        await PublishResilientAsync(
            mqttClient,
            clients.PublishInputs(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds));
        await PublishResilientAsync(
            mqttClient,
            clients.PublishCoils(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds));
        await PublishResilientAsync(mqttClient, clients.PublishModbus(
            Signal.Emit<(bool Connected, Exception? Error, object? Data)>((true, null, PayloadText)).ToSignal(),
            Topic,
            static value => value.ToString() ?? string.Empty));
        await PublishResilientAsync(mqttClient, clients.PublishModbus(
            Signal.Emit<(bool Connected, Exception? Error, object? Data)>((true, null, BinaryPayload)).ToSignal(),
            Topic,
            static value => (byte[])value));

        await Assert.That(() => clients.PublishModbus(
            Signal.Emit<(bool Connected, Exception? Error, object? Data)>((true, null, PayloadText)).ToSignal(),
            Topic,
            static _ => 1)).Throws<NotSupportedException>();

        await Assert.That(mqttClient.PendingApplicationMessagesCount).IsEqualTo(0);
    }

    /// <summary>Publishes through a resilient stream and completes it with the mock's processed event.</summary>
    /// <typeparam name="T">The result produced by the asynchronous observable.</typeparam>
    /// <param name="client">The mock client that completes the resilient publish sequence.</param>
    /// <param name="observable">The resilient publishing sequence.</param>
    /// <returns>A task that represents completion of the publishing sequence.</returns>
    private static async Task PublishResilientAsync<T>(MockResilientMqttClient client, IObservableAsync<T> observable)
        where T : notnull
    {
        var result = observable.FirstAsync(Timeout);
        for (var i = 0; i < MaximumProcessSignals && !result.IsCompleted; i++)
        {
            await Task.Delay(ProcessSignalDelay);
            await client.SimulateApplicationMessageProcessedAsync();
        }

        _ = await result;
    }
}
