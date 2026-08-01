// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.ABPlcRx.Reactive;
#else
using IoT.Driver.ABPlcRx;
#endif
#if REACTIVE_SHIM
using IoT.Driver.Serial.Reactive;
#else
using IoT.Driver.Serial;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using AbCreate = MQTTnet.Rx.ABPlc.Reactive.Create;
#else
using AbCreate = MQTTnet.Rx.ABPlc.Create;
#endif
#if REACTIVE_SHIM
using SerialAsyncCreate = MQTTnet.Rx.SerialPort.Reactive.ObservableAsyncCreateExtensions;
#else
using SerialAsyncCreate = MQTTnet.Rx.SerialPort.ObservableAsyncCreateExtensions;
#endif
#if REACTIVE_SHIM
using SerialCreate = MQTTnet.Rx.SerialPort.Reactive.Create;
#else
using SerialCreate = MQTTnet.Rx.SerialPort.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises reachable configuration and bridge paths in the AB PLC and serial-port integrations.</summary>
public sealed class Wave2AbSerialCoverageTests
{
    /// <summary>The MQTT topic used by the tests.</summary>
    private const string Topic = "wave2/integration";

    /// <summary>The PLC variable used by the tests.</summary>
    private const string Variable = "wave2.variable";

    /// <summary>The serial frame start delimiter.</summary>
    private const char FrameStart = '<';

    /// <summary>The serial frame end delimiter.</summary>
    private const char FrameEnd = '>';

    /// <summary>The serial frame payload character.</summary>
    private const char FramePayload = 'A';

    /// <summary>The framing timeout in milliseconds.</summary>
    private const int TimeoutMilliseconds = 1000;

    /// <summary>Verifies all AB helpers use the explicitly supplied PLC instance.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AbPlcHelpers_UseExplicitPlcInstanceAsync()
    {
        var plc = Substitute.For<IABPlcRx>();
        _ = plc.Observe(Variable, default(int), -1).Returns(Signal.None<int>());
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();

        var rawPublish = AbCreate.PublishABPlcTag<int>(raw, Topic, Variable, plc);
        _ = AbCreate.SubscribeABPlcTag(raw, Topic, Variable, plc, static _ => 0);
        var resilientPublish = AbCreate.PublishABPlcTag<int>(resilient, Topic, Variable, plc);
        _ = AbCreate.SubscribeABPlcTag(resilient, Topic, Variable, plc, static _ => 0);

        await Assert.That(rawPublish).IsNotNull();
        await Assert.That(resilientPublish).IsNotNull();
    }

    /// <summary>Verifies the serial writer helpers use the explicitly supplied serial-port instance.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SerialWriterHelpers_UseExplicitSerialPortInstanceAsync()
    {
        var serial = Substitute.For<ISerialPortRx>();
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();

        _ = SerialCreate.SubscribeSerialPortWriteLine(raw, Topic, serial, static value => value);
        _ = SerialCreate.SubscribeSerialPortWrite(raw, Topic, serial, static value => value);
        _ = SerialCreate.SubscribeSerialPortWrite(raw, Topic, serial, static _ => Array.Empty<byte>());
        _ = SerialCreate.SubscribeSerialPortWriteLine(resilient, Topic, serial, static value => value);
        _ = SerialCreate.SubscribeSerialPortWrite(resilient, Topic, serial, static value => value);
        _ = SerialCreate.SubscribeSerialPortWrite(resilient, Topic, serial, static _ => Array.Empty<byte>());

        await Assert.That(serial).IsNotNull();
    }

    /// <summary>Verifies synchronous publishers frame received characters and dispatch MQTT messages.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SerialPublishers_FrameAndPublishRawAndResilientStreamsAsync()
    {
        using var rawData = new TestSignal<char>();
        var rawSerial = CreateSerialPort(rawData);
        using var rawClient = new MockMqttClient();
        var rawResultTask = SerialCreate.PublishSerialPort(
            Signal.Emit<IMqttClient>(rawClient),
            Topic,
            rawSerial,
            Signal.Emit(FrameStart),
            Signal.Emit(FrameEnd),
            TimeoutMilliseconds)
            .FirstAsync(TimeSpan.FromSeconds(1));
        await Task.Yield();
        EmitFrame(rawData);
        var rawResult = await rawResultTask;
        await Assert.That(rawResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(rawClient.PublishedMessages.Count).IsEqualTo(1);

        using var resilientData = new TestSignal<char>();
        var resilientSerial = CreateSerialPort(resilientData);
        var resilientClient = new MockResilientMqttClient();
        var resilientResultTask = SerialCreate.PublishSerialPort(
            Signal.Emit<IResilientMqttClient>(resilientClient),
            Topic,
            resilientSerial,
            Signal.Emit(FrameStart),
            Signal.Emit(FrameEnd),
            TimeoutMilliseconds)
            .FirstAsync(TimeSpan.FromSeconds(1));
        await Task.Yield();
        EmitFrame(resilientData);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        var resilientResult = await resilientResultTask;
        await Assert.That(resilientResult.Exception).IsNull();
    }

    /// <summary>Verifies async publisher bridges validate sources before synchronous forwarding.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncSerialPublishers_ValidateBridgeArgumentsAsync()
    {
        using var dataReceived = new TestSignal<char>();
        var serial = CreateSerialPort(dataReceived);
        var starts = SignalAsync.Emit(FrameStart);
        var ends = SignalAsync.Emit(FrameEnd);

        await Assert.That(static () => SerialAsyncCreate.PublishSerialPort(
            (IObservableAsync<IMqttClient>)null!,
            Topic,
            null!,
            null!,
            null!,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
            SignalAsync.None<IMqttClient>(),
            Topic,
            serial,
            null!,
            ends,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
            SignalAsync.None<IMqttClient>(),
            Topic,
            serial,
            starts,
            null!,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.PublishSerialPort(
            (IObservableAsync<IResilientMqttClient>)null!,
            Topic,
            null!,
            null!,
            null!,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
            SignalAsync.None<IResilientMqttClient>(),
            Topic,
            serial,
            null!,
            ends,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
            SignalAsync.None<IResilientMqttClient>(),
            Topic,
            serial,
            starts,
            null!,
            TimeoutMilliseconds)).Throws<ArgumentNullException>();
    }

    /// <summary>Creates a serial facade with a deterministic framed input stream.</summary>
    /// <param name="dataReceived">The deterministic receive stream.</param>
    /// <returns>A serial facade for a single frame.</returns>
    private static ISerialPortRx CreateSerialPort(IObservable<char> dataReceived)
    {
        var serial = Substitute.For<ISerialPortRx>();
        _ = serial.DataReceived.Returns(dataReceived);
        return serial;
    }

    /// <summary>Emits one framed payload after the serial pipeline has subscribed.</summary>
    /// <param name="dataReceived">The serial receive stream.</param>
    private static void EmitFrame(TestSignal<char> dataReceived)
    {
        dataReceived.OnNext(FrameStart);
        dataReceived.OnNext(FramePayload);
        dataReceived.OnNext(FrameEnd);
    }
}
