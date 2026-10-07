// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.Serial.Reactive;
using MQTTnet.Rx.SerialPort.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.Serial;
using MQTTnet.Rx.SerialPort;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests complete MQTT messages produced by serial streams and frames.</summary>
public sealed class SerialPortApplicationMessageExtensionsTests
{
    /// <summary>The frame timeout in milliseconds.</summary>
    private const int FrameTimeout = 500;

    /// <summary>The MQTT message expiration interval.</summary>
    private const uint Expiry = 60;

    /// <summary>The bounded wait for publication.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies scalar and framed streams retain the original MQTT metadata.</summary>
    /// <param name="asynchronous">Whether to use asynchronous client sequences.</param>
    /// <param name="resilient">Whether to enqueue through a resilient client.</param>
    /// <param name="framed">Whether to assemble a serial frame.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false, false)]
    [Arguments(false, false, true)]
    [Arguments(false, true, false)]
    [Arguments(false, true, true)]
    [Arguments(true, false, false)]
    [Arguments(true, false, true)]
    [Arguments(true, true, false)]
    [Arguments(true, true, true)]
    public async Task PublishSerialMessages_PreservesCompleteMessageAsync(bool asynchronous, bool resilient, bool framed)
    {
        using var characters = new TestSignal<char>();
        using var lines = new TestSignal<string>();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var serial = Substitute.For<ISerialPortRx>();
        _ = serial.DataReceived.Returns(characters);
        _ = serial.Lines.Returns(lines);
        var message = CreateMessage();
        using var raw = new MockMqttClient();
        using var managed = Substitute.For<IResilientMqttClient>();
        MqttApplicationMessage? enqueued = null;
        _ = managed.ApplicationMessageProcessed.Returns(processed);
        _ = managed.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            enqueued = call.Arg<MqttApplicationMessage>();
            processed.OnNext(new(new() { ApplicationMessage = enqueued }, null));
            return Task.CompletedTask;
        });
        Func<string, MqttApplicationMessage> factory = _ => message;
        Task operation = resilient
            ? CreateManagedPublisher(managed, serial, asynchronous, framed, factory).FirstAsync(Timeout)
            : CreateRawPublisher(raw, serial, asynchronous, framed, factory).FirstAsync(Timeout);

        lines.OnNext("value");
        characters.OnNext('<');
        characters.OnNext('x');
        characters.OnNext('>');
        await operation;
        await Assert.That(ReferenceEquals(resilient ? enqueued : raw.PublishedMessages[0], message)).IsTrue();
    }

    /// <summary>Verifies framed stream errors reach publication observers.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="resilient">Whether to use a resilient client.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task FramedPublishing_ForwardsSerialErrorAsync(bool asynchronous, bool resilient)
    {
        using var characters = new TestSignal<char>();
        var serial = Substitute.For<ISerialPortRx>();
        _ = serial.DataReceived.Returns(characters);
        using var raw = new MockMqttClient();
        using var managed = Substitute.For<IResilientMqttClient>();
        var starts = Signal.Emit('<');
        var ends = Signal.Emit('>');
        Func<string, MqttApplicationMessage> factory = static value => new MqttApplicationMessageBuilder().WithTopic("serial/error").WithPayload(value).Build();
        Task operation;
        if (resilient)
        {
            var clients = Signal.Emit(managed);
            operation = asynchronous
                ? clients.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory).ToObservable().FirstAsync(Timeout)
                : clients.PublishSerialPort(serial, starts, ends, FrameTimeout, factory).FirstAsync(Timeout);
        }
        else
        {
            var clients = Signal.Emit<IMqttClient>(raw);
            operation = asynchronous
                ? clients.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory).ToObservable().FirstAsync(Timeout)
                : clients.PublishSerialPort(serial, starts, ends, FrameTimeout, factory).FirstAsync(Timeout);
        }

        characters.OnError(new InvalidOperationException("serial failed"));
        await Assert.That(async () => await operation).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies start and end marker failures reach framed publication observers.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="startMarker">Whether the start marker stream fails.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task FramedPublishing_ForwardsMarkerErrorAsync(bool asynchronous, bool startMarker)
    {
        using var characters = new TestSignal<char>();
        using var starts = new TestSignal<char>();
        using var ends = new TestSignal<char>();
        var serial = Substitute.For<ISerialPortRx>();
        _ = serial.DataReceived.Returns(characters);
        using var raw = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(raw);
        Func<string, MqttApplicationMessage> factory = static value => new MqttApplicationMessageBuilder().WithTopic("serial/error").WithPayload(value).Build();
        var publisher = asynchronous
            ? clients.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory).ToObservable()
            : clients.PublishSerialPort(serial, starts, ends, FrameTimeout, factory);
        var operation = publisher.FirstAsync(Timeout);
        var failedSource = startMarker ? starts : ends;
        failedSource.OnError(new InvalidOperationException("marker failed"));
        await Assert.That(async () => await operation).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies complete-message bridges reject missing dependencies.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task MessageBridges_RejectMissingArgumentsAsync()
    {
        var serial = Substitute.For<ISerialPortRx>();
        var raw = Signal.Empty<IMqttClient>();
        var managed = Signal.Empty<IResilientMqttClient>();
        var starts = Signal.Emit('<');
        var ends = Signal.Emit('>');
        Func<string, MqttApplicationMessage> factory = static _ => new();
        Func<ISerialPortRx, IObservable<string>> source = static port => port.Lines;
        IObservable<IMqttClient> missingRaw = null!;
        IObservable<IResilientMqttClient> missingManaged = null!;
        IObservableAsync<IMqttClient> missingAsyncRaw = null!;
        IObservableAsync<IResilientMqttClient> missingAsyncManaged = null!;
        Action[] invalid =
        [
            () => raw.PublishSerialPort(null!, starts, ends, FrameTimeout, factory),
            () => raw.PublishSerialPort(serial, null!, ends, FrameTimeout, factory),
            () => raw.PublishSerialPort(serial, starts, null!, FrameTimeout, factory),
            () => raw.PublishSerialPort(serial, starts, ends, FrameTimeout, null!),
            () => raw.PublishSerialPortMessages(null!, source, factory),
            () => raw.PublishSerialPortMessages(serial, null!, factory),
            () => raw.PublishSerialPortMessages(serial, source, null!),
            () => managed.PublishSerialPort(null!, starts, ends, FrameTimeout, factory),
            () => managed.PublishSerialPort(serial, null!, ends, FrameTimeout, factory),
            () => managed.PublishSerialPort(serial, starts, null!, FrameTimeout, factory),
            () => managed.PublishSerialPort(serial, starts, ends, FrameTimeout, null!),
            () => managed.PublishSerialPortMessages(null!, source, factory),
            () => managed.PublishSerialPortMessages(serial, null!, factory),
            () => managed.PublishSerialPortMessages(serial, source, null!),
            () => raw.ToSignal().PublishSerialPort(null!, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory),
            () => raw.ToSignal().PublishSerialPort(serial, null!, ends.ToSignal(), FrameTimeout, factory),
            () => raw.ToSignal().PublishSerialPort(serial, starts.ToSignal(), null!, FrameTimeout, factory),
            () => raw.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, null!),
            () => raw.ToSignal().PublishSerialPortMessages(null!, source, factory),
            () => raw.ToSignal().PublishSerialPortMessages(serial, null!, factory),
            () => raw.ToSignal().PublishSerialPortMessages(serial, source, null!),
            () => managed.ToSignal().PublishSerialPort(null!, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory),
            () => managed.ToSignal().PublishSerialPort(serial, null!, ends.ToSignal(), FrameTimeout, factory),
            () => managed.ToSignal().PublishSerialPort(serial, starts.ToSignal(), null!, FrameTimeout, factory),
            () => managed.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, null!),
            () => managed.ToSignal().PublishSerialPortMessages(null!, source, factory),
            () => managed.ToSignal().PublishSerialPortMessages(serial, null!, factory),
            () => managed.ToSignal().PublishSerialPortMessages(serial, source, null!),
            () => missingRaw.PublishSerialPort(serial, starts, ends, FrameTimeout, factory),
            () => missingRaw.PublishSerialPortMessages(serial, source, factory),
            () => missingManaged.PublishSerialPort(serial, starts, ends, FrameTimeout, factory),
            () => missingManaged.PublishSerialPortMessages(serial, source, factory),
            () => missingAsyncRaw.PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory),
            () => missingAsyncRaw.PublishSerialPortMessages(serial, source, factory),
            () => missingAsyncManaged.PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory),
            () => missingAsyncManaged.PublishSerialPortMessages(serial, source, factory),
        ];
        foreach (var action in invalid)
        {
            await Assert.That(action).Throws<ArgumentNullException>();
        }
    }

    /// <summary>Creates a message containing MQTT delivery and metadata properties.</summary>
    /// <returns>The complete message.</returns>
    private static MqttApplicationMessage CreateMessage() => new MqttApplicationMessageBuilder()
        .WithTopic("serial/full")
        .WithPayload("serial-value")
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
        .WithRetainFlag()
        .WithContentType("application/serial")
        .WithResponseTopic("serial/response")
        .WithCorrelationData("correlation"u8.ToArray())
        .WithMessageExpiryInterval(Expiry)
        .WithUserProperty("source", "serial"u8.ToArray())
        .Build();

    /// <summary>Selects a raw serial publisher.</summary>
    /// <param name="client">The MQTT client.</param>
    /// <param name="serial">The serial transport.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="framed">Whether to assemble serial frames.</param>
    /// <param name="factory">Creates each complete message.</param>
    /// <returns>The publisher.</returns>
    private static IObservable<MqttClientPublishResult> CreateRawPublisher(
        IMqttClient client,
        ISerialPortRx serial,
        bool asynchronous,
        bool framed,
        Func<string, MqttApplicationMessage> factory)
    {
        var clients = Signal.Emit(client);
        var starts = Signal.Emit('<');
        var ends = Signal.Emit('>');
        if (asynchronous)
        {
            var publisher = framed
                ? clients.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory)
                : clients.ToSignal().PublishSerialPortMessages(serial, static port => port.Lines, factory);
            return publisher.ToObservable();
        }

        return framed
            ? clients.PublishSerialPort(serial, starts, ends, FrameTimeout, factory)
            : clients.PublishSerialPortMessages(serial, static port => port.Lines, factory);
    }

    /// <summary>Selects a managed serial publisher.</summary>
    /// <param name="client">The MQTT client.</param>
    /// <param name="serial">The serial transport.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="framed">Whether to assemble serial frames.</param>
    /// <param name="factory">Creates each complete message.</param>
    /// <returns>The publisher.</returns>
    private static IObservable<ApplicationMessageProcessedEventArgs> CreateManagedPublisher(
        IResilientMqttClient client,
        ISerialPortRx serial,
        bool asynchronous,
        bool framed,
        Func<string, MqttApplicationMessage> factory)
    {
        var clients = Signal.Emit(client);
        var starts = Signal.Emit('<');
        var ends = Signal.Emit('>');
        if (asynchronous)
        {
            var publisher = framed
                ? clients.ToSignal().PublishSerialPort(serial, starts.ToSignal(), ends.ToSignal(), FrameTimeout, factory)
                : clients.ToSignal().PublishSerialPortMessages(serial, static port => port.Lines, factory);
            return publisher.ToObservable();
        }

        return framed
            ? clients.PublishSerialPort(serial, starts, ends, FrameTimeout, factory)
            : clients.PublishSerialPortMessages(serial, static port => port.Lines, factory);
    }
}
