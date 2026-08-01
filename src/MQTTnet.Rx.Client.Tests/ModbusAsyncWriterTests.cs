// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests the composed asynchronous Modbus writer pipelines.</summary>
public class ModbusAsyncWriterTests
{
    /// <summary>The topic used by the writer tests.</summary>
    private const string WriterTopic = "modbus/write";

    /// <summary>The number of seconds allowed for asynchronous writer completion.</summary>
    private const int WriterTimeoutSeconds = 5;

    /// <summary>The raw-client value written by the test.</summary>
    private const int RawClientValue = 42;

    /// <summary>The resilient-client value written by the test.</summary>
    private const int ResilientClientValue = 84;

    /// <summary>Verifies that a raw client awaits a successful asynchronous writer.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawClientAsyncWriter_ProcessesMessageAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockMqttClient();
        var written = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWrite(
            Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master)),
            WriterTopic,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture),
            (masterToWrite, value) =>
            {
                ArgumentNullException.ThrowIfNull(masterToWrite);
                _ = written.TrySetResult(value);
                return Task.CompletedTask;
            });

        await mqttClient.SimulateMessageReceivedAsync(
            WriterTopic,
            RawClientValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(
            await written.Task.WaitAsync(TimeSpan.FromSeconds(WriterTimeoutSeconds))).IsEqualTo(RawClientValue);
    }

    /// <summary>Verifies that a raw client propagates an asynchronous writer failure.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawClientAsyncWriter_PropagatesFailureAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockMqttClient();

        using var subscription = Signal.Emit<IMqttClient>(mqttClient).SubscribeWrite(
            Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master)),
            WriterTopic,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture),
            static (_, _) => Task.FromException(new InvalidOperationException("writer failed")));

        await Assert.That(() => mqttClient.SimulateMessageReceivedAsync(
            WriterTopic,
            RawClientValue.ToString(CultureInfo.InvariantCulture)))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Verifies that a resilient client awaits a successful asynchronous writer.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientAsyncWriter_ProcessesMessageAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockResilientMqttClient();
        var written = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = Signal.Emit<IResilientMqttClient>(mqttClient).SubscribeWrite(
            Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master)),
            WriterTopic,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture),
            (masterToWrite, value) =>
            {
                ArgumentNullException.ThrowIfNull(masterToWrite);
                _ = written.TrySetResult(value);
                return Task.CompletedTask;
            });

        await mqttClient.SimulateMessageReceivedAsync(
            WriterTopic,
            ResilientClientValue.ToString(CultureInfo.InvariantCulture));

        await Assert.That(
            await written.Task.WaitAsync(TimeSpan.FromSeconds(WriterTimeoutSeconds))).IsEqualTo(ResilientClientValue);
    }

    /// <summary>Verifies that a resilient client propagates an asynchronous writer failure.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientAsyncWriter_PropagatesFailureAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var mqttClient = new MockResilientMqttClient();

        using var subscription = Signal.Emit<IResilientMqttClient>(mqttClient).SubscribeWrite(
            Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master)),
            WriterTopic,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture),
            static (_, _) => Task.FromException(new InvalidOperationException("writer failed")));

        await Assert.That(() => mqttClient.SimulateMessageReceivedAsync(
            WriterTopic,
            ResilientClientValue.ToString(CultureInfo.InvariantCulture)))
            .Throws<InvalidOperationException>();
    }
}
