// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
#if !REACTIVE_SHIM
using CP.Collections;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Reactive;
#else
using IoT.Driver.TwinCATRx;
#endif
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
using TwinCatAsyncCreate = MQTTnet.Rx.TwinCAT.Reactive.ObservableAsyncCreateExtensions;
#else
using TwinCatAsyncCreate = MQTTnet.Rx.TwinCAT.ObservableAsyncCreateExtensions;
#endif
#if REACTIVE_SHIM
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Reactive.Create;
#else
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests argument validation in the Windows-only TwinCAT integration.</summary>
public sealed class TwinCatPackageCoverageTests
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

    /// <summary>Tests synchronous TwinCAT helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => TwinCatCreate.PublishTcPlcTag<int>(
            (IObservable<IMqttClient>)null!,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.PublishTcPlcTag<int>(
            RawClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.SubscribeTcTag(
            (IObservable<IMqttClient>)null!,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.SubscribeTcTag(
            RawClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.PublishTcPlcTag<int>(
            RawClient,
            Topic,
            Variable,
            (IHashTableRx)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.PublishTcPlcTag<int>(
            ResilientClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.SubscribeTcTag(
            ResilientClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatCreate.PublishTcPlcTag<int>(
            ResilientClient,
            Topic,
            Variable,
            (IHashTableRx)null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Tests asynchronous TwinCAT helper validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncHelpers_RejectMissingDependenciesAsync()
    {
        await Assert.That(static () => TwinCatAsyncCreate.PublishTcPlcTag<int>(
            (IObservableAsync<IMqttClient>)null!,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.PublishTcPlcTag<int>(
            AsyncRawClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.SubscribeTcTag(
            (IObservableAsync<IMqttClient>)null!,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.SubscribeTcTag(
            AsyncRawClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.PublishTcPlcTag<int>(
            AsyncRawClient,
            Topic,
            Variable,
            (IHashTableRx)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.PublishTcPlcTag<int>(
            AsyncResilientClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.SubscribeTcTag(
            AsyncResilientClient,
            Topic,
            Variable,
            (IRxTcAdsClient)null!,
            static _ => 0)).Throws<ArgumentNullException>();
        await Assert.That(static () => TwinCatAsyncCreate.PublishTcPlcTag<int>(
            AsyncResilientClient,
            Topic,
            Variable,
            (IHashTableRx)null!)).Throws<ArgumentNullException>();
    }
}
#endif
