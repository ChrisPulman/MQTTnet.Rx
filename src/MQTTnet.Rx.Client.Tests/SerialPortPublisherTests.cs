// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.Serial.Reactive;
#else
using IoT.Driver.Serial;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using MQTTnet.Rx.SerialPort.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using MQTTnet.Rx.SerialPort;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises serial-port data-source MQTT publisher surfaces.</summary>
public sealed class SerialPortPublisherTests
{
    /// <summary>The line publish topic.</summary>
    private const string LineTopic = "serial/publish/line";

    /// <summary>The byte publish topic.</summary>
    private const string ByteTopic = "serial/publish/byte";

    /// <summary>The formatted byte publish topic.</summary>
    private const string ByteTextTopic = "serial/publish/byte-text";

    /// <summary>The error publish topic.</summary>
    private const string ErrorTopic = "serial/publish/error";

    /// <summary>The formatted error publish topic.</summary>
    private const string ErrorTextTopic = "serial/publish/error-text";

    /// <summary>The open-state publish topic.</summary>
    private const string OpenTopic = "serial/publish/open";

    /// <summary>The received line payload.</summary>
    private const string LinePayload = "ready";

    /// <summary>The formatted byte payload.</summary>
    private const string ByteTextPayload = "0x42";

    /// <summary>The serial error message.</summary>
    private const string ErrorMessage = "serial fault";

    /// <summary>The bounded wait used for observable assertions.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>Publishes every serial data-source observable through raw MQTT clients.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RawPublishers_ForwardLinesBytesErrorsAndOpenStateAsync()
    {
        using var sources = new SerialSources();
        var serial = CreateSerialPort(sources);
        using var mqtt = new MockMqttClient();

        var line = Signal.Emit<IMqttClient>(mqtt).PublishSerialPortLines(LineTopic, serial).FirstAsync(Timeout);
        var bytes = Signal.Emit<IMqttClient>(mqtt).PublishSerialPortBytes(ByteTopic, serial).FirstAsync(Timeout);
        var byteText = Signal.Emit<IMqttClient>(mqtt)
            .PublishSerialPortBytes(ByteTextTopic, serial, static value => $"0x{value:X2}")
            .FirstAsync(Timeout);
        var error = Signal.Emit<IMqttClient>(mqtt).PublishSerialPortErrors(ErrorTopic, serial).FirstAsync(Timeout);
        var errorText = Signal.Emit<IMqttClient>(mqtt)
            .PublishSerialPortErrors(ErrorTextTopic, serial, static value => value.Message)
            .FirstAsync(Timeout);
        var open = Signal.Emit<IMqttClient>(mqtt).PublishSerialPortOpenState(OpenTopic, serial).FirstAsync(Timeout);

        await EmitSerialSourcesAsync(sources);

        await AssertRawPublishAsync(line, bytes, byteText, error, errorText, open);
        await Assert.That(FindPayload(mqtt, LineTopic)).IsEqualTo(LinePayload);
        await Assert.That(FindPayload(mqtt, ByteTextTopic)).IsEqualTo(ByteTextPayload);
        await Assert.That(FindPayload(mqtt, ErrorTextTopic)).IsEqualTo(ErrorMessage);
        await Assert.That(FindPayload(mqtt, OpenTopic)).IsEqualTo(bool.TrueString);
    }

    /// <summary>Publishes every serial data-source observable through resilient MQTT clients.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResilientPublishers_ForwardLinesBytesErrorsAndOpenStateAsync()
    {
        using var sources = new SerialSources();
        var serial = CreateSerialPort(sources);
        using var mqtt = new MockResilientMqttClient();

        var line = Signal.Emit<IResilientMqttClient>(mqtt).PublishSerialPortLines(LineTopic, serial).FirstAsync(Timeout);
        var bytes = Signal.Emit<IResilientMqttClient>(mqtt).PublishSerialPortBytes(ByteTopic, serial).FirstAsync(Timeout);
        var byteText = Signal.Emit<IResilientMqttClient>(mqtt)
            .PublishSerialPortBytes(ByteTextTopic, serial, static value => value.ToString())
            .FirstAsync(Timeout);
        var error = Signal.Emit<IResilientMqttClient>(mqtt).PublishSerialPortErrors(ErrorTopic, serial).FirstAsync(Timeout);
        var errorText = Signal.Emit<IResilientMqttClient>(mqtt)
            .PublishSerialPortErrors(ErrorTextTopic, serial, static value => value.Message)
            .FirstAsync(Timeout);
        var open = Signal.Emit<IResilientMqttClient>(mqtt).PublishSerialPortOpenState(OpenTopic, serial).FirstAsync(Timeout);

        await EmitSerialSourcesAsync(sources);
        await SimulateProcessedMessagesAsync(mqtt);

        await AssertResilientPublishAsync(line, bytes, byteText, error, errorText, open);
    }

    /// <summary>Verifies asynchronous publisher wrappers forward to the synchronous publisher surfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AsyncPublishers_ForwardEveryPublisherSurfaceAsync()
    {
        using var sources = new SerialSources();
        var serial = CreateSerialPort(sources);
        using var raw = new MockMqttClient();
        using var resilient = new MockResilientMqttClient();

        await AssertAsyncRawPublishersAsync(serial, raw);
        await AssertAsyncResilientPublishersAsync(serial, resilient);
    }

    /// <summary>Validates publisher guards for missing serial dependencies.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublisherGuards_RejectMissingObservableArgumentsAsync()
    {
        using var sources = new SerialSources();
        var serial = CreateSerialPort(sources);
        IObservable<IMqttClient> raw = Signal.None<IMqttClient>();
        IObservable<IResilientMqttClient> resilient = Signal.None<IResilientMqttClient>();

        await Assert.That(() => raw.PublishSerialPortLines(" ", serial)).Throws<ArgumentException>();
        await Assert.That(() => raw.PublishSerialPortLines(LineTopic, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishSerialPortBytes(ByteTextTopic, serial, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishSerialPortLines(" ", serial)).Throws<ArgumentException>();
        await Assert.That(() => resilient.PublishSerialPortLines(LineTopic, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishSerialPortErrors(ErrorTextTopic, serial, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Creates a serial-port substitute backed by deterministic observable sources.</summary>
    /// <param name="sources">The deterministic serial sources.</param>
    /// <returns>The substitute serial port.</returns>
    private static ISerialPortRx CreateSerialPort(SerialSources sources)
    {
        var serial = Substitute.For<ISerialPortRx>();
        _ = serial.Lines.Returns(sources.Lines);
        _ = serial.DataReceivedBytes.Returns(sources.Bytes);
        _ = serial.ErrorReceived.Returns(sources.Errors);
        _ = serial.IsOpenObservable.Returns(sources.OpenStates);
        return serial;
    }

    /// <summary>Emits one value from every serial source after publisher subscriptions attach.</summary>
    /// <param name="sources">The serial sources.</param>
    /// <returns>A task representing the asynchronous yield.</returns>
    private static async Task EmitSerialSourcesAsync(SerialSources sources)
    {
        await Task.Yield();
        sources.Lines.OnNext(LinePayload);
        sources.Bytes.OnNext((byte)'B');
        sources.Errors.OnNext(new InvalidOperationException(ErrorMessage));
        sources.OpenStates.OnNext(true);
    }

    /// <summary>Simulates one processed event for each resilient publish.</summary>
    /// <param name="mqtt">The resilient MQTT client.</param>
    /// <returns>A task representing the simulated acknowledgements.</returns>
    private static async Task SimulateProcessedMessagesAsync(MockResilientMqttClient mqtt)
    {
        await mqtt.SimulateApplicationMessageProcessedAsync();
        await mqtt.SimulateApplicationMessageProcessedAsync();
        await mqtt.SimulateApplicationMessageProcessedAsync();
        await mqtt.SimulateApplicationMessageProcessedAsync();
        await mqtt.SimulateApplicationMessageProcessedAsync();
        await mqtt.SimulateApplicationMessageProcessedAsync();
    }

    /// <summary>Asserts raw publisher tasks completed successfully.</summary>
    /// <param name="line">The line publish task.</param>
    /// <param name="bytes">The byte publish task.</param>
    /// <param name="byteText">The formatted byte publish task.</param>
    /// <param name="error">The error publish task.</param>
    /// <param name="errorText">The formatted error publish task.</param>
    /// <param name="open">The open-state publish task.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertRawPublishAsync(
        Task<MqttClientPublishResult> line,
        Task<MqttClientPublishResult> bytes,
        Task<MqttClientPublishResult> byteText,
        Task<MqttClientPublishResult> error,
        Task<MqttClientPublishResult> errorText,
        Task<MqttClientPublishResult> open)
    {
        await Assert.That((await line).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That((await bytes).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That((await byteText).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That((await error).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That((await errorText).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That((await open).ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
    }

    /// <summary>Asserts resilient publisher tasks completed successfully.</summary>
    /// <param name="line">The line publish task.</param>
    /// <param name="bytes">The byte publish task.</param>
    /// <param name="byteText">The formatted byte publish task.</param>
    /// <param name="error">The error publish task.</param>
    /// <param name="errorText">The formatted error publish task.</param>
    /// <param name="open">The open-state publish task.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertResilientPublishAsync(
        Task<ApplicationMessageProcessedEventArgs> line,
        Task<ApplicationMessageProcessedEventArgs> bytes,
        Task<ApplicationMessageProcessedEventArgs> byteText,
        Task<ApplicationMessageProcessedEventArgs> error,
        Task<ApplicationMessageProcessedEventArgs> errorText,
        Task<ApplicationMessageProcessedEventArgs> open)
    {
        await Assert.That((await line).Exception).IsNull();
        await Assert.That((await bytes).Exception).IsNull();
        await Assert.That((await byteText).Exception).IsNull();
        await Assert.That((await error).Exception).IsNull();
        await Assert.That((await errorText).Exception).IsNull();
        await Assert.That((await open).Exception).IsNull();
    }

    /// <summary>Asserts raw asynchronous wrappers are created.</summary>
    /// <param name="serial">The serial port.</param>
    /// <param name="mqtt">The MQTT client.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertAsyncRawPublishersAsync(ISerialPortRx serial, IMqttClient mqtt)
    {
        var clients = SignalAsync.Return(mqtt);
        await Assert.That(clients.PublishSerialPortLines(LineTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortBytes(ByteTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortBytes(ByteTextTopic, serial, static value => value.ToString())).IsNotNull();
        await Assert.That(clients.PublishSerialPortErrors(ErrorTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortErrors(ErrorTextTopic, serial, static value => value.Message)).IsNotNull();
        await Assert.That(clients.PublishSerialPortOpenState(OpenTopic, serial)).IsNotNull();
    }

    /// <summary>Asserts resilient asynchronous wrappers are created.</summary>
    /// <param name="serial">The serial port.</param>
    /// <param name="mqtt">The resilient MQTT client.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertAsyncResilientPublishersAsync(ISerialPortRx serial, IResilientMqttClient mqtt)
    {
        var clients = SignalAsync.Return(mqtt);
        await Assert.That(clients.PublishSerialPortLines(LineTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortBytes(ByteTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortBytes(ByteTextTopic, serial, static value => value.ToString())).IsNotNull();
        await Assert.That(clients.PublishSerialPortErrors(ErrorTopic, serial)).IsNotNull();
        await Assert.That(clients.PublishSerialPortErrors(ErrorTextTopic, serial, static value => value.Message)).IsNotNull();
        await Assert.That(clients.PublishSerialPortOpenState(OpenTopic, serial)).IsNotNull();
    }

    /// <summary>Finds a raw published payload by topic.</summary>
    /// <param name="client">The raw MQTT client.</param>
    /// <param name="topic">The topic to find.</param>
    /// <returns>The published payload.</returns>
    private static string FindPayload(MockMqttClient client, string topic)
    {
        foreach (var message in client.PublishedMessages)
        {
            if (string.Equals(message.Topic, topic, StringComparison.Ordinal))
            {
                return message.ConvertPayloadToString();
            }
        }

        throw new InvalidOperationException($"Topic '{topic}' was not published.");
    }

    /// <summary>Stores deterministic serial observable sources.</summary>
    private sealed class SerialSources : IDisposable
    {
        /// <summary>Gets serial lines.</summary>
        public TestSignal<string> Lines { get; } = new();

        /// <summary>Gets serial bytes.</summary>
        public TestSignal<byte> Bytes { get; } = new();

        /// <summary>Gets serial errors.</summary>
        public TestSignal<Exception> Errors { get; } = new();

        /// <summary>Gets serial open states.</summary>
        public TestSignal<bool> OpenStates { get; } = new();

        /// <inheritdoc/>
        public void Dispose()
        {
            Lines.Dispose();
            Bytes.Dispose();
            Errors.Dispose();
            OpenStates.Dispose();
        }
    }
}
