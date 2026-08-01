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
using NSubstitute;
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

/// <summary>Tests the reachable configured paths of the Windows-only TwinCAT integration.</summary>
public sealed class Wave2TwinCatCoverageTests
{
    /// <summary>Gets the MQTT topic used by these tests.</summary>
    private const string Topic = "wave2/twincat";

    /// <summary>Gets the PLC variable used by these tests.</summary>
    private const string Variable = "wave2.variable";

    /// <summary>Tests synchronous helpers accept explicitly supplied driver instances.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousHelpers_AcceptExplicitDriverInstancesAsync()
    {
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();
        var ads = Substitute.For<IRxTcAdsClient>();
#if REACTIVE_SHIM
        using var hashTable = new IHashTableRx(useUpperCase: false);
#else
        var hashTable = Substitute.For<IHashTableRx>();
#endif

        TwinCatCreate.SubscribeTcTag(raw, Topic, Variable, ads, static _ => 0);
        TwinCatCreate.SubscribeTcTag(resilient, Topic, Variable, ads, static _ => 0);

        await Assert.That(ads).IsNotNull();
        await Assert.That(hashTable).IsNotNull();
    }

    /// <summary>Tests asynchronous helpers accept explicitly supplied driver instances.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncHelpers_AcceptExplicitDriverInstancesAsync()
    {
        var raw = SignalAsync.None<IMqttClient>();
        var resilient = SignalAsync.None<IResilientMqttClient>();
        var ads = Substitute.For<IRxTcAdsClient>();
#if REACTIVE_SHIM
        using var hashTable = new IHashTableRx(useUpperCase: false);
#else
        var hashTable = Substitute.For<IHashTableRx>();
#endif

        _ = TwinCatAsyncCreate.SubscribeTcTag(raw, Topic, Variable, ads, static _ => 0);
        _ = TwinCatAsyncCreate.SubscribeTcTag(resilient, Topic, Variable, ads, static _ => 0);

        await Assert.That(ads).IsNotNull();
        await Assert.That(hashTable).IsNotNull();
    }
}
#endif
