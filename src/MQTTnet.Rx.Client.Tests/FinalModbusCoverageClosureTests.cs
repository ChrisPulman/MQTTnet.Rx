// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.ModbusRx.Reactive.Device;
#else
using IoT.Driver.ModbusRx.Device;
#endif
using MQTTnet.Protocol;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive;
#else
using MQTTnet.Rx.Client;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using MQTTnet.Rx.Modbus.Reactive;
#else
using MQTTnet.Rx.Modbus;
#endif
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes the synchronous Modbus publishing convenience-overload coverage paths.</summary>
public sealed class FinalModbusCoverageClosureTests
{
    /// <summary>The topic used by the simulator-backed publishing pipelines.</summary>
    private const string Topic = "coverage/modbus/final";

    /// <summary>The first address supplied to each Modbus read adapter.</summary>
    private const ushort Address = 0;

    /// <summary>The number of values requested from each Modbus read adapter.</summary>
    private const ushort NumberOfPoints = 1;

    /// <summary>The explicit polling interval used by the forwarding overloads.</summary>
    private const double IntervalMilliseconds = 1.0;

    /// <summary>The bounded duration allowed for each simulator-backed publication.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>The interval used to pump fake resilient processed-message events.</summary>
    private static readonly TimeSpan ProcessEventInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>Verifies every uncovered raw-client convenience overload publishes a simulator value.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawClientConveniencePublishers_ForwardSimulatorValuesAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(mqttClient);
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>(
            (true, null, master));
        var pipelines = new[]
        {
            clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints),
            clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
            clients.PublishInputRegisters(
                modbus,
                Topic,
                Address,
                NumberOfPoints,
                IntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce),
            clients.PublishInputs(modbus, Topic, Address, NumberOfPoints),
            clients.PublishCoils(modbus, Topic, Address, NumberOfPoints),
            clients.PublishCoils(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
        };

        foreach (var pipeline in pipelines)
        {
            _ = await pipeline.FirstAsync(Timeout);
        }

        await Assert.That(mqttClient.PublishedMessages.Count).IsGreaterThanOrEqualTo(pipelines.Length);
    }

    /// <summary>Verifies every uncovered resilient-client convenience overload publishes a simulator value.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientConveniencePublishers_ForwardSimulatorValuesAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockResilientMqttClient();
        var clients = Signal.Emit<IResilientMqttClient>(mqttClient);
        var modbus = Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>(
            (true, null, master));
        var pipelines = new[]
        {
            clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints),
            clients.PublishInputRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
            clients.PublishInputRegisters(
                modbus,
                Topic,
                Address,
                NumberOfPoints,
                IntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce),
            clients.PublishHoldingRegisters(modbus, Topic, Address, NumberOfPoints),
            clients.PublishHoldingRegisters(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
            clients.PublishHoldingRegisters(
                modbus,
                Topic,
                Address,
                NumberOfPoints,
                IntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce),
            clients.PublishInputs(modbus, Topic, Address, NumberOfPoints),
            clients.PublishInputs(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
            clients.PublishInputs(
                modbus,
                Topic,
                Address,
                NumberOfPoints,
                IntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce),
            clients.PublishCoils(modbus, Topic, Address, NumberOfPoints),
            clients.PublishCoils(modbus, Topic, Address, NumberOfPoints, IntervalMilliseconds),
            clients.PublishCoils(
                modbus,
                Topic,
                Address,
                NumberOfPoints,
                IntervalMilliseconds,
                MqttQualityOfServiceLevel.ExactlyOnce),
        };

        foreach (var pipeline in pipelines)
        {
            var processed = await AwaitProcessedMessageAsync(mqttClient, pipeline);
            await Assert.That(processed.Exception).IsNull();
        }
    }

    /// <summary>Awaits one resilient publish while periodically raising the fake processed event.</summary>
    /// <param name="mqttClient">The fake resilient MQTT client.</param>
    /// <param name="pipeline">The publishing pipeline to observe.</param>
    /// <returns>The first processed-message event produced by the pipeline.</returns>
    private static async Task<ApplicationMessageProcessedEventArgs> AwaitProcessedMessageAsync(
        MockResilientMqttClient mqttClient,
        IObservable<ApplicationMessageProcessedEventArgs> pipeline)
    {
        var result = pipeline.FirstAsync(Timeout);
        using var cancellation = new CancellationTokenSource(Timeout);
        using var timer = new PeriodicTimer(ProcessEventInterval);
        while (!result.IsCompleted)
        {
            await mqttClient.SimulateApplicationMessageProcessedAsync();
            if (!result.IsCompleted)
            {
                _ = await timer.WaitForNextTickAsync(cancellation.Token);
            }
        }

        return await result;
    }
}
