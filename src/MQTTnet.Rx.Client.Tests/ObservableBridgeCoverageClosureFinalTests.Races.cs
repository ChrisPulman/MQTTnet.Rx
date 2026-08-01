// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
using MQTTnet.Rx.Client.MemoryEfficient;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Disposables;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains retry and task-pool race coverage for observable compatibility bridges.</summary>
public sealed partial class ObservableBridgeCoverageClosureFinalTests
{
    /// <summary>Exercises task-pool drain coordination and post-disposal notifications.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PrimitivesCompatibilityBridge_SerializesTaskPoolRacesAsync()
    {
        var message = TestDataHelpers.CreateMessageReceivedArgs("task-pool/race", Payload);
        var deliveryCount = await VerifyTaskPoolDeliveryRaceAsync(message);
        await VerifySelfDisposingTaskPoolSubscriptionAsync(message);

        await Assert.That(deliveryCount).IsEqualTo(ExpectedRetryAttempts);
    }

    /// <summary>Verifies that queued task-pool deliveries are serialized and ignored after disposal.</summary>
    /// <param name="message">The message used for each delivery.</param>
    /// <returns>The number of deliveries completed before disposal.</returns>
    private static async Task<int> VerifyTaskPoolDeliveryRaceAsync(MqttApplicationMessageReceivedEventArgs message)
    {
        IObserver<MqttApplicationMessageReceivedEventArgs>? observer = null;
        var source = new ScriptedObservable<MqttApplicationMessageReceivedEventArgs>((_, value) =>
        {
            observer = value;
            return EmptyDisposable.Instance;
        });
        using var release = new ManualResetEventSlim();
        var firstDelivery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondDelivery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryCount = 0;
        using var subscription = TestObservableExtensions.Subscribe(
            LowAllocExtensions.ObserveOnThreadPool(source),
            value => ProcessTaskPoolDelivery(value, ref deliveryCount, firstDelivery, secondDelivery, release));
        var assignedObserver = observer ?? throw new InvalidOperationException("Task-pool observer was not assigned.");
        assignedObserver.OnNext(message);
        await firstDelivery.Task.WaitAsync(Timeout);
        assignedObserver.OnNext(message);
        release.Set();
        await secondDelivery.Task.WaitAsync(Timeout);
        subscription.Dispose();
        assignedObserver.OnNext(message);
        return deliveryCount;
    }

    /// <summary>Processes a task-pool delivery while blocking only the first notification.</summary>
    /// <param name="value">The delivered value.</param>
    /// <param name="deliveryCount">The number of deliveries observed so far.</param>
    /// <param name="firstDelivery">The signal for the first delivery.</param>
    /// <param name="secondDelivery">The signal for the second delivery.</param>
    /// <param name="release">The gate that releases the first delivery.</param>
    private static void ProcessTaskPoolDelivery(
        MqttApplicationMessageReceivedEventArgs value,
        ref int deliveryCount,
        TaskCompletionSource<bool> firstDelivery,
        TaskCompletionSource<bool> secondDelivery,
        ManualResetEventSlim release)
    {
        GC.KeepAlive(value);
        if (Interlocked.Increment(ref deliveryCount) == 1)
        {
            _ = firstDelivery.TrySetResult(true);
            _ = release.Wait(Timeout);
            return;
        }

        _ = secondDelivery.TrySetResult(true);
    }

    /// <summary>Verifies a subscription can dispose itself while handling a task-pool notification.</summary>
    /// <param name="message">The message used for the notification.</param>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifySelfDisposingTaskPoolSubscriptionAsync(
        MqttApplicationMessageReceivedEventArgs message)
    {
        IObserver<MqttApplicationMessageReceivedEventArgs>? observer = null;
        var source = new ScriptedObservable<MqttApplicationMessageReceivedEventArgs>((_, value) =>
        {
            observer = value;
            return EmptyDisposable.Instance;
        });
        var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable? subscription = null;
        subscription = TestObservableExtensions.Subscribe(
            LowAllocExtensions.ObserveOnThreadPool(source),
            value => DisposeTaskPoolSubscription(value, subscription, disposed));
        var assignedObserver = observer
            ?? throw new InvalidOperationException("Self-disposing observer was not assigned.");
        assignedObserver.OnNext(message);
        await disposed.Task.WaitAsync(Timeout);
        await Assert.That(disposed.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Disposes a task-pool subscription after receiving its first notification.</summary>
    /// <param name="value">The delivered value.</param>
    /// <param name="subscription">The subscription to dispose.</param>
    /// <param name="disposed">The signal completed after disposal.</param>
    private static void DisposeTaskPoolSubscription(
        MqttApplicationMessageReceivedEventArgs value,
        IDisposable? subscription,
        TaskCompletionSource<bool> disposed)
    {
        GC.KeepAlive(value);
        subscription?.Dispose();
        _ = disposed.TrySetResult(true);
    }
}
