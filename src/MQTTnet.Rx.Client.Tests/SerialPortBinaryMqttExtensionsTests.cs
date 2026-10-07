// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
#if REACTIVE_SHIM
using IoT.Driver.Serial.Reactive;
using MQTTnet.Rx.SerialPort.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.Serial;
using MQTTnet.Rx.SerialPort;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Advanced;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests binary serial writes and receive-count publications.</summary>
public sealed class SerialPortBinaryMqttExtensionsTests
{
    /// <summary>The topic used by guard tests.</summary>
    private const string ValidationTopic = "topic";

    /// <summary>The split point for a segmented payload.</summary>
    private const int SplitIndex = 2;

    /// <summary>The receive byte count.</summary>
    private const int ReceiveCount = 42;

    /// <summary>The bounded test operation wait.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies arbitrary binary payloads survive both contiguous and segmented MQTT messages.</summary>
    /// <param name="asynchronous">Whether to use the asynchronous client sequence.</param>
    /// <param name="segmented">Whether the MQTT payload spans two buffers.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task WriteBytes_PreservesBinaryPayloadAsync(bool asynchronous, bool segmented)
    {
        const string topic = "serial/binary";
        byte[] expected = [0, 255, 128, 195, 40];
        var first = new PayloadSegment(expected.AsMemory(0, SplitIndex));
        var last = first.Append(expected.AsMemory(SplitIndex));
        var payload = segmented ? new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length) : new ReadOnlySequence<byte>(expected);
        var serial = Substitute.For<ISerialPortRx>();
        byte[]? written = null;
        serial.When(static port => port.Write(Arg.Any<ReadOnlyMemory<byte>>()))
            .Do(call => written = call.Arg<ReadOnlyMemory<byte>>().ToArray());
        var subscribed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var mqtt = new ScriptedMqttClient
        {
            SubscribeHandler = (_, _) =>
            {
                _ = subscribed.TrySetResult();
                return Task.FromResult(new MqttClientSubscribeResult(0, [], string.Empty, []));
            },
        };
        var clients = Signal.Emit<IMqttClient>(mqtt);
        using var lifetime = asynchronous
            ? clients.ToSignal().SubscribeSerialPortWriteBytes(topic, serial)
            : clients.SubscribeSerialPortWriteBytes(topic, serial);
        await subscribed.Task.WaitAsync(Timeout);
        var message = new MqttApplicationMessage { Topic = topic, Payload = payload };
        var received = new MqttApplicationMessageReceivedEventArgs("serial-test", message, new MqttPublishPacket(), null);
        await mqtt.RaiseApplicationMessageReceivedAsync(received);
        await Assert.That(written).IsEquivalentTo(expected);
        lifetime.Dispose();
        written = null;
        await mqtt.RaiseApplicationMessageReceivedAsync(received);
        await Assert.That(written).IsNull();
    }

    /// <summary>Publishes receive counts through both client kinds and observable forms.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="resilient">Whether to enqueue through a resilient client.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task ReceiveCount_PublishesInvariantCountAsync(bool asynchronous, bool resilient)
    {
        const string topic = "serial/count";
        var serial = Substitute.For<IPortRx>();
        _ = serial.BytesReceived.Returns(Signal.Emit(ReceiveCount));
        using var raw = new MockMqttClient();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        var managed = Substitute.For<IResilientMqttClient>();
        _ = managed.ApplicationMessageProcessed.Returns(processed);
        var enqueued = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = managed.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            _ = enqueued.TrySetResult(call.Arg<MqttApplicationMessage>());
            return Task.CompletedTask;
        });
        if (resilient)
        {
            var clients = Signal.Emit(managed);
            using var lifetime = asynchronous
                ? clients.ToSignal().PublishSerialPortReceiveCount(topic, serial).ToObservable().Subscribe(Witness.Create<ApplicationMessageProcessedEventArgs>(static _ => { }))
                : clients.PublishSerialPortReceiveCount(topic, serial).Subscribe(Witness.Create<ApplicationMessageProcessedEventArgs>(static _ => { }));
            var message = await enqueued.Task.WaitAsync(Timeout);
            await Assert.That(message.ConvertPayloadToString()).IsEqualTo("42");
        }
        else
        {
            var clients = Signal.Emit<IMqttClient>(raw);
            var results = asynchronous
                ? clients.ToSignal().PublishSerialPortReceiveCount(topic, serial).ToObservable()
                : clients.PublishSerialPortReceiveCount(topic, serial);
            _ = await results.FirstAsync(Timeout);
            await Assert.That(raw.PublishedMessages[0].ConvertPayloadToString()).IsEqualTo("42");
        }
    }

    /// <summary>Verifies resilient binary writers attach to and dispose their received-message stream.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ResilientWriteBytes_PreservesPayloadAndDisposesAsync(bool asynchronous)
    {
        const string topic = "serial/resilient-binary";
        byte[] expected = [0, byte.MaxValue];
        using var received = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        using var managed = Substitute.For<IResilientMqttClient>();
        _ = managed.ApplicationMessageReceived.Returns(received);
        _ = managed.SubscribeAsync(Arg.Any<IEnumerable<MqttTopicFilter>>()).Returns(Task.CompletedTask);
        var serial = Substitute.For<ISerialPortRx>();
        byte[]? written = null;
        serial.When(static port => port.Write(Arg.Any<ReadOnlyMemory<byte>>()))
            .Do(call => written = call.Arg<ReadOnlyMemory<byte>>().ToArray());
        var clients = Signal.Emit(managed);
        using var lifetime = asynchronous
            ? clients.ToSignal().SubscribeSerialPortWriteBytes(topic, serial)
            : clients.SubscribeSerialPortWriteBytes(topic, serial);
        var message = new MqttApplicationMessage { Topic = topic, Payload = new(expected) };
        var eventArgs = new MqttApplicationMessageReceivedEventArgs("serial-test", message, new MqttPublishPacket(), null);
        received.OnNext(eventArgs);
        await Assert.That(written).IsEquivalentTo(expected);
        lifetime.Dispose();
        written = null;
        received.OnNext(eventArgs);
        await Assert.That(written).IsNull();
    }

    /// <summary>Verifies every binary bridge checks its client, topic, and transport.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task BinaryBridges_RejectInvalidArgumentsAsync()
    {
        var serial = Substitute.For<ISerialPortRx>();
        var raw = Signal.Empty<IMqttClient>();
        var managed = Signal.Empty<IResilientMqttClient>();
        IObservable<IMqttClient> missingRaw = null!;
        IObservable<IResilientMqttClient> missingManaged = null!;
        IObservableAsync<IMqttClient> missingAsyncRaw = null!;
        IObservableAsync<IResilientMqttClient> missingAsyncManaged = null!;
        Action[] missingArguments =
        [
            () => missingRaw.PublishSerialPortReceiveCount(ValidationTopic, serial),
            () => missingManaged.PublishSerialPortReceiveCount(ValidationTopic, serial),
            () => missingAsyncRaw.PublishSerialPortReceiveCount(ValidationTopic, serial),
            () => missingAsyncManaged.PublishSerialPortReceiveCount(ValidationTopic, serial),
            () => missingRaw.SubscribeSerialPortWriteBytes(ValidationTopic, serial),
            () => missingManaged.SubscribeSerialPortWriteBytes(ValidationTopic, serial),
            () => missingAsyncRaw.SubscribeSerialPortWriteBytes(ValidationTopic, serial),
            () => missingAsyncManaged.SubscribeSerialPortWriteBytes(ValidationTopic, serial),
            () => raw.PublishSerialPortReceiveCount(ValidationTopic, null!),
            () => managed.PublishSerialPortReceiveCount(ValidationTopic, null!),
            () => raw.ToSignal().PublishSerialPortReceiveCount(ValidationTopic, null!),
            () => managed.ToSignal().PublishSerialPortReceiveCount(ValidationTopic, null!),
            () => raw.SubscribeSerialPortWriteBytes(ValidationTopic, null!),
            () => managed.SubscribeSerialPortWriteBytes(ValidationTopic, null!),
            () => raw.ToSignal().SubscribeSerialPortWriteBytes(ValidationTopic, null!),
            () => managed.ToSignal().SubscribeSerialPortWriteBytes(ValidationTopic, null!),
        ];
        foreach (var action in missingArguments)
        {
            await Assert.That(action).Throws<ArgumentNullException>();
        }

        Action[] invalidTopics =
        [
            () => raw.PublishSerialPortReceiveCount(" ", serial),
            () => managed.PublishSerialPortReceiveCount(" ", serial),
            () => raw.ToSignal().PublishSerialPortReceiveCount(" ", serial),
            () => managed.ToSignal().PublishSerialPortReceiveCount(" ", serial),
            () => raw.SubscribeSerialPortWriteBytes(" ", serial),
            () => managed.SubscribeSerialPortWriteBytes(" ", serial),
            () => raw.ToSignal().SubscribeSerialPortWriteBytes(" ", serial),
            () => managed.ToSignal().SubscribeSerialPortWriteBytes(" ", serial),
        ];
        foreach (var action in invalidTopics)
        {
            await Assert.That(action).Throws<ArgumentException>();
        }
    }

    /// <summary>Represents a segment in an MQTT binary payload.</summary>
    private sealed class PayloadSegment : ReadOnlySequenceSegment<byte>
    {
        /// <summary>Initializes a new instance of the <see cref="PayloadSegment"/> class.</summary>
        /// <param name="memory">The segment's bytes.</param>
        internal PayloadSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        /// <summary>Appends a segment to this payload.</summary>
        /// <param name="memory">The following bytes.</param>
        /// <returns>The appended segment.</returns>
        internal PayloadSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new PayloadSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }
}
