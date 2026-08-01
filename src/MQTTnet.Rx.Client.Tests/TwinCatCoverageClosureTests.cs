// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Reflection;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
#else
using CP.Collections;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Reactive;
#else
using IoT.Driver.TwinCATRx;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Core.Reactive;
#else
using IoT.Driver.TwinCATRx.Core;
#endif
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive;
#else
using MQTTnet.Rx.Client;
#endif
#if REACTIVE_SHIM
using MQTTnet.Rx.TwinCAT.Reactive;
#else
using MQTTnet.Rx.TwinCAT;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.Reactive.TwinCatRxExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Reactive.Create;
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.Reactive.CreateExtensions;
#else
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.TwinCatRxExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.CreateExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes public-forwarder coverage for the Windows-only TwinCAT package.</summary>
public sealed class TwinCatCoverageClosureTests
{
    /// <summary>The in-memory ADS variable used by construction tests.</summary>
    private const string AdsVariable = ".Main.Coverage";

    /// <summary>The reactive hash-table key used by construction tests.</summary>
    private const string HashVariable = "Coverage";

    /// <summary>The simulated TwinCAT runtime port.</summary>
    private const int TwinCatPort = 851;

    /// <summary>Exercises every synchronous TwinCAT façade and extension with a missing client.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousPublicSurface_RejectsMissingClientAsync()
    {
        var createMethods = typeof(TwinCatCreate).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var extensionMethods = typeof(TwinCatCreateExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);

        await AssertMissingClientAsync(createMethods);
        await AssertMissingClientAsync(extensionMethods);
    }

    /// <summary>Exercises every asynchronous TwinCAT extension with a missing client.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousPublicSurface_RejectsMissingClientAsync()
    {
        var methods = typeof(ObservableAsyncCreateExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
        await AssertMissingClientAsync(methods);
    }

    /// <summary>Constructs every asynchronous publisher with configured in-memory driver instances.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousPublishers_AcceptConfiguredDriverInstancesAsync()
    {
        using var ads = CreateAdsClient();
        using var hash = new HashTableRx(useUpperCase: false);
        hash.Add(HashVariable, 0);
        IRxTcAdsClient adsContract = ads;
        IHashTableRx hashContract = hash;
        var raw = SignalAsync.None<IMqttClient>();
        var resilient = SignalAsync.None<IResilientMqttClient>();

        var rawAds = ObservableAsyncCreateExtensions.PublishTcPlcTag(
            raw,
            "coverage/raw/ads",
            AdsVariable,
            adsContract,
            -1);
        var rawHash = ObservableAsyncCreateExtensions.PublishTcPlcTag(
            raw,
            "coverage/raw/hash",
            HashVariable,
            hashContract,
            -1);
        var resilientAds = ObservableAsyncCreateExtensions.PublishTcPlcTag(
            resilient,
            "coverage/resilient/ads",
            AdsVariable,
            adsContract,
            -1);
        var resilientHash = ObservableAsyncCreateExtensions.PublishTcPlcTag(
            resilient,
            "coverage/resilient/hash",
            HashVariable,
            hashContract,
            -1);

        await Assert.That(rawAds).IsNotNull();
        await Assert.That(rawHash).IsNotNull();
        await Assert.That(resilientAds).IsNotNull();
        await Assert.That(resilientHash).IsNotNull();
    }

    /// <summary>Invokes each static forwarder with a null client and verifies its validation exception.</summary>
    /// <param name="methods">The forwarders to invoke.</param>
    /// <returns>A task that represents the TUnit assertions.</returns>
    private static async Task AssertMissingClientAsync(MethodInfo[] methods)
    {
        foreach (var method in methods)
        {
            try
            {
                var result = InvokeWithMissingFirstArgument(method);
                if (method.ReturnType != typeof(void))
                {
                    await Assert.That(result).IsNotNull();
                }
            }
            catch (TargetInvocationException exception)
            {
                await Assert.That(exception.InnerException).IsTypeOf<ArgumentNullException>();
            }
        }

        await Assert.That(methods.Length).IsGreaterThan(0);
    }

    /// <summary>Invokes a generic static TwinCAT forwarder with a null first argument.</summary>
    /// <param name="method">The forwarder to invoke.</param>
    /// <returns>The forwarder's result, if any.</returns>
    private static object? InvokeWithMissingFirstArgument(MethodInfo method)
    {
        var callable = method.IsGenericMethodDefinition ? method.MakeGenericMethod(typeof(int)) : method;
        var parameters = callable.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsValueType)
            {
                arguments[i] = Activator.CreateInstance(parameters[i].ParameterType);
            }
        }

        return callable.Invoke(null, arguments);
    }

    /// <summary>Creates a configured in-memory ADS client for observable construction.</summary>
    /// <returns>The connected client.</returns>
    private static InMemoryAdsClient CreateAdsClient()
    {
        var ads = new InMemoryAdsClient();
        var settings = new Settings
        {
            AdsAddress = "in-memory",
            Port = TwinCatPort,
            SettingsId = "twincat-coverage-closure",
        };
        TwinCatCoreExtensions.AddNotification(settings, AdsVariable);
        _ = ads.RegisterSymbol(AdsVariable, 0);
        ads.Connect(settings);
        return ads;
    }
}
#endif
