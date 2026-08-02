// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive;
#else
using MQTTnet.Rx.Client;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using AbAsyncCreate = MQTTnet.Rx.ABPlc.Reactive.ObservableAsyncCreateExtensions;
#else
using AbAsyncCreate = MQTTnet.Rx.ABPlc.ObservableAsyncCreateExtensions;
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

/// <summary>Tests argument validation in the optional PLC and serial-port integrations.</summary>
public sealed class IntegrationPackageCoverageTests
{
    /// <summary>Gets the MQTT topic used by the validation tests.</summary>
    private const string Topic = "coverage/topic";

    /// <summary>Gets the PLC variable used by the validation tests.</summary>
    private const string Variable = "coverage.variable";

    /// <summary>Gets the empty raw-client stream used by the validation tests.</summary>
    private static readonly IObservable<IMqttClient> RawClient = Signal.None<IMqttClient>();

    /// <summary>Gets the empty resilient-client stream used by the validation tests.</summary>
    private static readonly IObservable<IResilientMqttClient> ResilientClient = Signal.None<IResilientMqttClient>();

    /// <summary>Gets the empty asynchronous raw-client stream used by the validation tests.</summary>
    private static readonly IObservableAsync<IMqttClient> AsyncRawClient = SignalAsync.None<IMqttClient>();

    /// <summary>Gets the empty asynchronous resilient-client stream used by the validation tests.</summary>
    private static readonly IObservableAsync<IResilientMqttClient> AsyncResilientClient =
        SignalAsync.None<IResilientMqttClient>();

    /// <summary>Tests Allen-Bradley synchronous helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AbPlcSynchronousHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => AbCreate.PublishABPlcTag<int>(
                (IObservable<IMqttClient>)null!,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbCreate.PublishABPlcTag<int>(
                RawClient,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbCreate.SubscribeABPlcTag(
                (IObservable<IMqttClient>)null!,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbCreate.SubscribeABPlcTag(
                RawClient,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbCreate.PublishABPlcTag<int>(
                ResilientClient,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbCreate.SubscribeABPlcTag(
                ResilientClient,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests Allen-Bradley asynchronous helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AbPlcAsyncHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => AbAsyncCreate.PublishABPlcTag<int>(
                (IObservableAsync<IMqttClient>)null!,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbAsyncCreate.PublishABPlcTag<int>(
                AsyncRawClient,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbAsyncCreate.SubscribeABPlcTag(
                (IObservableAsync<IMqttClient>)null!,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbAsyncCreate.SubscribeABPlcTag(
                AsyncRawClient,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbAsyncCreate.PublishABPlcTag<int>(
                AsyncResilientClient,
                Topic,
                Variable,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => AbAsyncCreate.SubscribeABPlcTag(
                AsyncResilientClient,
                Topic,
                Variable,
                null!,
                static _ => 0))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Tests serial-port synchronous helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SerialPortSynchronousHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => SerialCreate.PublishSerialPort(
                (IObservable<IMqttClient>)null!,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.PublishSerialPort(
                RawClient,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWriteLine(
                (IObservable<IMqttClient>)null!,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWriteLine(
                RawClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWrite(
                RawClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWrite(
                RawClient,
                Topic,
                null!,
                static value => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
        await AssertSerialPortResilientClientValidationAsync();
    }

    /// <summary>Tests serial-port asynchronous helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SerialPortAsyncHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => SerialAsyncCreate.PublishSerialPort(
                (IObservableAsync<IMqttClient>)null!,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.PublishSerialPort(
                AsyncRawClient,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWriteLine(
                (IObservableAsync<IMqttClient>)null!,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWriteLine(
                AsyncRawClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWrite(
                AsyncRawClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWrite(
                AsyncRawClient,
                Topic,
                null!,
                static value => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
        await AssertSerialPortAsyncResilientClientValidationAsync();
    }

    /// <summary>Verifies async serial-port helpers reject missing resilient-client dependencies.</summary>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task AssertSerialPortAsyncResilientClientValidationAsync()
    {
        await Assert.That(static () => SerialAsyncCreate.PublishSerialPort(
                AsyncResilientClient,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWriteLine(
                AsyncResilientClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWrite(
                AsyncResilientClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialAsyncCreate.SubscribeSerialPortWrite(
                AsyncResilientClient,
                Topic,
                null!,
                static value => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Verifies synchronous serial-port helpers reject missing resilient-client dependencies.</summary>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task AssertSerialPortResilientClientValidationAsync()
    {
        await Assert.That(static () => SerialCreate.PublishSerialPort(
                ResilientClient,
                Topic,
                null!,
                null!,
                null!,
                0))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWriteLine(
                ResilientClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWrite(
                ResilientClient,
                Topic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => SerialCreate.SubscribeSerialPortWrite(
                ResilientClient,
                Topic,
                null!,
                static value => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
    }
}
