// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using IoT.Driver.ModbusRx.Reactive.Device;
using IoT.Driver.ModbusRx.Reactive.IO;
using MQTTnet.Rx.Modbus.Reactive;
#else
using IoT.Driver.ModbusRx.Device;
using IoT.Driver.ModbusRx.IO;
using MQTTnet.Rx.Modbus;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ModbusCreate = MQTTnet.Rx.Modbus.Reactive.Create;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using ModbusCreate = MQTTnet.Rx.Modbus.Create;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies native serial-master access and MQTT message preservation.</summary>
public sealed class ModbusMessageSurfaceTests
{
    /// <summary>The maximum wait for a test operation.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Preserves the complete application message created for a native driver reading.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task PublishesCompleteMessageAsync(bool asynchronous)
    {
        using var client = new MockMqttClient();
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("modbus/native")
            .WithPayload("42")
            .WithContentType("application/json")
            .WithResponseTopic("modbus/reply")
            .Build();
        var clients = Signal.Emit<IMqttClient>(client);
        var readings = Signal.Emit(1);
        var operation = asynchronous
            ? clients.ToSignal().PublishModbusMessages(readings.ToSignal(), _ => message).ToObservable()
            : clients.PublishModbusMessages(readings, _ => message);
        _ = await operation.FirstAsync(Timeout);
        await Assert.That(client.PublishedMessages.Count).IsEqualTo(1);
        await Assert.That(client.PublishedMessages[0]).IsSameReferenceAs(message);
    }

    /// <summary>Preserves complete messages through resilient publishing.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ResilientPublishesCompleteMessageAsync(bool asynchronous)
    {
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        using var client = Substitute.For<IResilientMqttClient>();
        var message = new MqttApplicationMessageBuilder().WithTopic("modbus/native").Build();
        _ = client.ApplicationMessageProcessed.Returns(processed);
        _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            processed.OnNext(new(new() { ApplicationMessage = call.Arg<MqttApplicationMessage>() }, null));
            return Task.CompletedTask;
        });
        var clients = Signal.Emit(client);
        var readings = Signal.Emit(1);
        var operation = asynchronous
            ? clients.ToSignal().PublishModbusMessages(readings.ToSignal(), _ => message).ToObservable()
            : clients.PublishModbusMessages(readings, _ => message);
        var result = await operation.FirstAsync(Timeout);
        await Assert.That(result.ApplicationMessage.ApplicationMessage).IsSameReferenceAs(message);
    }

    /// <summary>Keeps borrowed serial masters alive after the subscription completes.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SerialMasterRemainsCallerOwnedAsync(bool asynchronous)
    {
        using var resource = new TestStreamResource();
        using var master = ModbusSerialMaster.CreateRtu(resource);
        var source = asynchronous
            ? ObservableAsyncCreateExtensions.FromSerialMaster(master).ToObservable()
            : ModbusCreate.FromSerialMaster(master);
        var state = await source.FirstAsync(Timeout);
        await Assert.That(state.Master).IsSameReferenceAs(master);
        await Assert.That(state.Connected).IsTrue();
        await Assert.That(master.IsDisposed).IsFalse();
    }

    /// <summary>Disposes factory-created serial masters after the subscription ends.</summary>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SerialFactoryDisposesMasterAsync(bool asynchronous)
    {
        using var resource = new TestStreamResource();
        var master = ModbusSerialMaster.CreateAscii(resource);
        var source = asynchronous
            ? ObservableAsyncCreateExtensions.FromSerialFactory(() => master).ToObservable()
            : ModbusCreate.FromSerialFactory(() => master);
        var state = await source.FirstAsync(Timeout);
        await Assert.That(state.Master).IsSameReferenceAs(master);
        await Assert.That(master.IsDisposed).IsTrue();
    }

    /// <summary>Rejects invalid serial factories and propagates factory failures.</summary>
    /// <returns>The asynchronous test operation.</returns>
    [Test]
    public async Task SerialFactoriesRejectInvalidArgumentsAsync()
    {
        await Assert.That(static () => ModbusCreate.FromSerialMaster(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ModbusCreate.FromSerialFactory(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ObservableAsyncCreateExtensions.FromSerialMaster(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ObservableAsyncCreateExtensions.FromSerialFactory(null!)).Throws<ArgumentNullException>();
        var failure = new InvalidOperationException("serial factory failure");
        await Assert.That(() => ModbusCreate.FromSerialFactory(() => throw failure).FirstAsync(Timeout))
            .Throws<InvalidOperationException>();
        await Assert.That(() => ObservableAsyncCreateExtensions.FromSerialFactory(() => throw failure).ToObservable().FirstAsync(Timeout))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Provides a transport resource for lifecycle-only tests.</summary>
    private sealed class TestStreamResource : IStreamResource
    {
        /// <summary>Gets the infinite timeout value.</summary>
        public int InfiniteTimeout => System.Threading.Timeout.Infinite;

        /// <summary>Gets or sets the read timeout.</summary>
        public int ReadTimeout { get; set; }

        /// <summary>Gets or sets the write timeout.</summary>
        public int WriteTimeout { get; set; }

        /// <summary>Discards pending input.</summary>
        public void DiscardInBuffer()
        {
        }

        /// <summary>Reads from the empty test resource.</summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns>The empty read result.</returns>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count) => Task.FromResult(0);

        /// <summary>Writes to the test resource.</summary>
        /// <param name="buffer">The source buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
        }

        /// <summary>Disposes the test resource.</summary>
        public void Dispose()
        {
        }
    }
}
