// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Reflection;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
using IoT.Driver.TwinCATRx.Reactive;
using MQTTnet.Rx.TwinCAT.Reactive;
using ISettings = IoT.Driver.TwinCATRx.Core.Reactive.ISettings;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.TwinCAT;
using ISettings = IoT.Driver.TwinCATRx.Core.ISettings;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies deterministic cancellation and late-callback resource cleanup.</summary>
public sealed class TwinCatStructureLifetimeTests
{
    /// <summary>Stores the maximum wait for a simulated connection operation.</summary>
    private const int TimeoutSeconds = 5;

    /// <summary>Stores the transition that starts a ready structure's publisher.</summary>
    private const string PublishLinkedTransition = "PublishLinkedStructure";

    /// <summary>Verifies cancellation during a synchronous ADS connect defers disposal until it returns.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DisposeDuringConnectDefersAdsDisposalUntilConnectReturnsAsync()
    {
        using var connectRelease = new ManualResetEventSlim();
        using var errors = new TestSignal<Exception>();
        using var mqtt = new MockMqttClient();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ads = Substitute.For<IRxTcAdsClient>();
        _ = ads.ErrorReceived.Returns(errors);
        ads.When(static client => client.Connect(Arg.Any<ISettings>())).Do(call =>
        {
            _ = entered.TrySetResult();
            if (!connectRelease.Wait(TimeSpan.FromSeconds(TimeoutSeconds)))
            {
                throw new TimeoutException("The test did not release the simulated ADS connection.");
            }
        });
        ads.When(static client => client.Dispose()).Do(call => { _ = disposed.TrySetResult(); });
        using var bridge = Signal.Emit<IMqttClient>(mqtt).PublishTcStructure(CreateOptions(), () => ads);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
            errors.OnNext(new InvalidOperationException("Simulated ADS diagnostic without a callback."));
            bridge.Dispose();
            await Assert.That(disposed.Task.IsCompleted).IsFalse();
        }
        finally
        {
            connectRelease.Set();
        }

        await disposed.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        await Assert.That(disposed.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Verifies late resources and callbacks cannot restart an already disposed bridge.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DisposedBridgeRejectsLateResourcesAndReadinessCallbacksAsync()
    {
        using var errors = new TestSignal<Exception>();
        var adsDisposed = false;
        var published = false;
        var ads = Substitute.For<IRxTcAdsClient>();
        ads.When(static client => client.Dispose()).Do(_ => adsDisposed = true);
        using var errorLease = new TrackingDisposable();
        using var readyLease = new TrackingDisposable();
        using var table = new HashTableRx(useUpperCase: false);
        using var bridge = CreateUnstartedBridge(CreateOptions(), () => ads, _ =>
        {
            published = true;
            return new TrackingDisposable();
        });
        bridge.Dispose();

        var error = await Assert.That(() => Invoke(bridge, "StoreAdsClient", ads, errorLease))
            .Throws<TargetInvocationException>();
        await Assert.That(error?.InnerException).IsTypeOf<OperationCanceledException>();
        await Assert.That(errorLease.Disposed).IsTrue();
        await Assert.That(adsDisposed).IsTrue();
        await Assert.That(Invoke(bridge, "StoreStructure", table, readyLease) is false).IsTrue();
        await Assert.That(readyLease.Disposed).IsTrue();
        _ = Invoke(bridge, PublishLinkedTransition, table);
        _ = Invoke(bridge, "ConnectAndPublish");
        await Assert.That(published).IsFalse();
    }

    /// <summary>Verifies a publication acquired concurrently with disposal is itself disposed.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublicationReturnedAfterDisposalIsReleasedAsync()
    {
        using var ads = new InMemoryAdsClient();
        using var table = new HashTableRx(useUpperCase: false);
        var publication = new TrackingDisposable();
        IDisposable? bridge = null;
        bridge = CreateUnstartedBridge(CreateOptions(), () => ads, _ =>
        {
            bridge?.Dispose();
            return publication;
        });
        using (bridge)
        {
            _ = Invoke(bridge, PublishLinkedTransition, table);
            await Assert.That(publication.Disposed).IsTrue();
        }
    }

    /// <summary>Verifies disposal while obtaining the ADS stream prevents the explicit initial read.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DisposalDuringStructureLinkPreventsInitialReadAsync()
    {
        var ads = Substitute.For<IRxTcAdsClient>();
        var readStarted = false;
        ads.When(static client => client.Read(Arg.Any<string>())).Do(_ => readStarted = true);
        using var bridge = CreateUnstartedBridge(CreateOptions(), () => ads, static _ => new TrackingDisposable());
        _ = ads.DataReceived.Returns(call =>
        {
            bridge.Dispose();
            return Signal.Empty<(string Variable, object? Data, string? Id)>();
        });

        _ = Invoke(bridge, "LinkStructure", ads);
        await Assert.That(readStarted).IsFalse();
    }

    /// <summary>Verifies failed structure creation has a descriptive error before any read starts.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task MissingAdsClientCannotProduceALinkedStructureAsync()
    {
        using var ads = new InMemoryAdsClient();
        using var bridge = CreateUnstartedBridge(CreateOptions(), () => ads, static _ => new TrackingDisposable());
        var error = await Assert.That(() => Invoke(bridge, "LinkStructure", [null]))
            .Throws<TargetInvocationException>();
        await Assert.That(error?.InnerException).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Verifies optional setup-error handling does not make connection failures escape the worker.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task SetupFailureWithoutErrorHandlerTerminatesNormallyAsync()
    {
        using var bridge = CreateUnstartedBridge(
            CreateOptions(),
            static () => throw new InvalidOperationException("Simulated ADS factory failure."),
            static _ => new TrackingDisposable());
        await Assert.That(Invoke(bridge, "ConnectAndPublish")).IsNull();
    }

    /// <summary>Verifies cleanup can wait for a readiness callback without retaining the callback's bridge lock.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DisposeAllowsPendingReadinessCallbackToFinishAsync()
    {
        using var release = new ManualResetEventSlim();
        using var callbackFinished = new ManualResetEventSlim();
        using var ads = new InMemoryAdsClient();
        using var table = new HashTableRx(useUpperCase: false);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var publication = new TrackingDisposable();
        var readiness = Substitute.For<IDisposable>();
        readiness.When(static lease => lease.Dispose()).Do(_ =>
        {
            release.Set();
            if (!callbackFinished.Wait(TimeSpan.FromSeconds(TimeoutSeconds)))
            {
                throw new TimeoutException("The readiness callback could not finish during disposal.");
            }
        });
        var options = CreateOptions();
        options.StructureLinked = tableValue =>
        {
            _ = entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(TimeoutSeconds)))
            {
                throw new TimeoutException("The readiness callback was not released.");
            }
        };
        using var bridge = CreateUnstartedBridge(options, () => ads, _ => publication);
        _ = Invoke(bridge, "StoreStructure", table, readiness);
        var callback = Task.Run(() =>
        {
            try
            {
                _ = Invoke(bridge, PublishLinkedTransition, table);
            }
            finally
            {
                callbackFinished.Set();
            }
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        await Task.Run(bridge.Dispose).WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        await callback.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        await Assert.That(publication.Disposed).IsTrue();
    }

    /// <summary>Verifies the underlying publication lifetime independently guards repeated terminal transitions.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublicationStateErrorsOnceAndDisposesChangeLeaseOnceAsync()
    {
        var errors = 0;
        var disposals = 0;
        var observer = Substitute.For<IObserver<(string Topic, string Payload)>>();
        observer.When(static target => target.OnError(Arg.Any<Exception>())).Do(_ => errors++);
        var changes = Substitute.For<IDisposable>();
        changes.When(static target => target.Dispose()).Do(_ => disposals++);
        var stateType = GetNestedType("TwinCatStructurePublicationState");
        var state = Activator.CreateInstance(stateType, observer)
            ?? throw new InvalidOperationException("The publication state could not be created.");
        _ = Invoke(state, "TryError", new InvalidOperationException("first"));
        _ = Invoke(state, "TryError", new InvalidOperationException("second"));
        var leaseType = GetNestedType("TwinCatStructureSubscription");
        using var lease = Activator.CreateInstance(leaseType, state, changes, null) as IDisposable
            ?? throw new InvalidOperationException("The publication lease could not be created.");
        lease.Dispose();
        lease.Dispose();
        await Assert.That(errors).IsEqualTo(1);
        await Assert.That(disposals).IsEqualTo(1);
    }

    /// <summary>Creates the test connection settings.</summary>
    /// <returns>The bridge settings.</returns>
    private static TwinCatStructureOptions CreateOptions() => new()
    {
        AmsNetId = "127.0.0.1.1.1",
        PlcVariable = "GVL.Rig",
        TopicPrefix = "lifetime/rig",
    };

    /// <summary>Creates a worker without scheduling startup so late-callback states can be exercised deterministically.</summary>
    /// <param name="options">The connection settings.</param>
    /// <param name="adsFactory">The ADS factory.</param>
    /// <param name="publishFactory">The publication factory.</param>
    /// <returns>The owned worker.</returns>
    private static IDisposable CreateUnstartedBridge(
        TwinCatStructureOptions options,
        Func<IRxTcAdsClient> adsFactory,
        Func<HashTableRx, IDisposable> publishFactory)
    {
        var type = GetNestedType("TwinCatStructureOwnedBridge");
        return Activator.CreateInstance(type, options, adsFactory, publishFactory) is IDisposable bridge
            ? bridge
            : throw new InvalidOperationException("The owned bridge could not be created.");
    }

    /// <summary>Invokes a worker transition without nondeterministic scheduler timing.</summary>
    /// <param name="bridge">The owned worker.</param>
    /// <param name="name">The transition method.</param>
    /// <param name="arguments">The transition arguments.</param>
    /// <returns>The transition result.</returns>
    private static object? Invoke(object bridge, string name, params object?[] arguments)
    {
        var method = bridge.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException($"The transition {name} was not found.");
        return method.Invoke(bridge, arguments);
    }

    /// <summary>Finds a lifetime implementation type for deterministic transition checks.</summary>
    /// <param name="name">The nested implementation type name.</param>
    /// <returns>The implementation type.</returns>
    private static Type GetNestedType(string name) =>
        typeof(TwinCatStructureBridgeExtensions).GetNestedType(name, BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The implementation type {name} was not found.");

    /// <summary>Records whether a late subscription lease is released.</summary>
    private sealed class TrackingDisposable : IDisposable
    {
        /// <summary>Gets whether the lease was released.</summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => Disposed = true;
    }
}
#endif
