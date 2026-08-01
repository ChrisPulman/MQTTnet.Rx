// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.TwinCAT;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using TwinCatAsync = MQTTnet.Rx.TwinCAT.ObservableAsyncCreateExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains validation coverage for the TwinCAT live-broker bridge fixture.</summary>
public sealed partial class TwinCatLiveBrokerBridgeTests
{
    /// <summary>Validates raw synchronous dependencies.</summary>
    /// <param name="ads">The non-null ADS contract.</param>
    /// <param name="hash">The non-null hash-table contract.</param>
    /// <returns>A task representing the TUnit assertions.</returns>
    private static async Task ValidateRawDependenciesAsync(IRxTcAdsClient ads, IHashTableRx hash)
    {
        var clients = Signal.None<IMqttClient>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                (IObservable<IMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                (IObservable<IMqttClient>)null!,
                "v",
                HashVariable,
                hash,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                clients,
                "v",
                HashVariable,
                (IHashTableRx)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.SubscribeTcTag(
                (IObservable<IMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                ParsePayload)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.SubscribeTcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                ParsePayload)).Throws<ArgumentNullException>();
    }

    /// <summary>Validates resilient synchronous dependencies.</summary>
    /// <param name="ads">The non-null ADS contract.</param>
    /// <param name="hash">The non-null hash-table contract.</param>
    /// <returns>A task representing the TUnit assertions.</returns>
    private static async Task ValidateResilientDependenciesAsync(IRxTcAdsClient ads, IHashTableRx hash)
    {
        var clients = Signal.None<IResilientMqttClient>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                (IObservable<IResilientMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                (IObservable<IResilientMqttClient>)null!,
                "v",
                HashVariable,
                hash,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.PublishTcPlcTag(
                clients,
                "v",
                HashVariable,
                (IHashTableRx)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.SubscribeTcTag(
                (IObservable<IResilientMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                ParsePayload)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatCreate.SubscribeTcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                ParsePayload)).Throws<ArgumentNullException>();
    }

    /// <summary>Validates raw asynchronous dependencies.</summary>
    /// <param name="ads">The non-null ADS contract.</param>
    /// <param name="hash">The non-null hash-table contract.</param>
    /// <returns>A task representing the TUnit assertions.</returns>
    private static async Task ValidateAsyncRawDependenciesAsync(IRxTcAdsClient ads, IHashTableRx hash)
    {
        var clients = SignalAsync.None<IMqttClient>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                (IObservableAsync<IMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                (IObservableAsync<IMqttClient>)null!,
                "v",
                HashVariable,
                hash,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                clients,
                "v",
                HashVariable,
                (IHashTableRx)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.SubscribeTcTag(
                (IObservableAsync<IMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                ParsePayload)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.SubscribeTcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                ParsePayload)).Throws<ArgumentNullException>();
    }

    /// <summary>Validates resilient asynchronous dependencies.</summary>
    /// <param name="ads">The non-null ADS contract.</param>
    /// <param name="hash">The non-null hash-table contract.</param>
    /// <returns>A task representing the TUnit assertions.</returns>
    private static async Task ValidateAsyncResilientDependenciesAsync(IRxTcAdsClient ads, IHashTableRx hash)
    {
        var clients = SignalAsync.None<IResilientMqttClient>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                (IObservableAsync<IResilientMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                (IObservableAsync<IResilientMqttClient>)null!,
                "v",
                HashVariable,
                hash,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.PublishTcPlcTag(
                clients,
                "v",
                HashVariable,
                (IHashTableRx)null!,
                1)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.SubscribeTcTag(
                (IObservableAsync<IResilientMqttClient>)null!,
                "v",
                AdsVariable,
                ads,
                ParsePayload)).Throws<ArgumentNullException>();
        await Assert.That(
            () => TwinCatAsync.SubscribeTcTag(
                clients,
                "v",
                AdsVariable,
                (IRxTcAdsClient)null!,
                ParsePayload)).Throws<ArgumentNullException>();
    }
}
#endif
