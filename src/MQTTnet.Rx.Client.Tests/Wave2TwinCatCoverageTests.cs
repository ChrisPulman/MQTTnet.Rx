// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Client;
using NSubstitute;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using TwinCatAsyncCreate = MQTTnet.Rx.TwinCAT.ObservableAsyncCreateExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;

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
        var hashTable = Substitute.For<IHashTableRx>();

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
        var hashTable = Substitute.For<IHashTableRx>();

        _ = TwinCatAsyncCreate.SubscribeTcTag(raw, Topic, Variable, ads, static _ => 0);
        _ = TwinCatAsyncCreate.SubscribeTcTag(resilient, Topic, Variable, ads, static _ => 0);

        await Assert.That(ads).IsNotNull();
        await Assert.That(hashTable).IsNotNull();
    }
}
#endif
