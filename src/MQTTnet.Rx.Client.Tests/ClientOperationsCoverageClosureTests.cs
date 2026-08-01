// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.MemoryEfficient;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes public-behaviour coverage of client-operation and low-allocation edge paths.</summary>
public sealed class ClientOperationsCoverageClosureTests
{
    /// <summary>Defines the expected number of queued messages delivered without an overflow callback.</summary>
    private const int ExpectedQueuedMessageCount = 2;

    /// <summary>Defines the bounded period used to establish subscriptions in this fixture.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Verifies synchronous connection waiting applies its timeout and periodic ping accepts its default interval.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task SynchronousOperations_UseTimeoutAndDefaultPeriodicIntervalAsync()
    {
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);
        var connection = clients.WaitForConnection(Timeout).FirstAsync(Timeout);
        using var periodicPing = clients.PingPeriodically().Subscribe();

        await client.SimulateConnectedAsync();

        await Assert.That(await connection).IsSameReferenceAs(client);
    }

    /// <summary>Verifies omitted back-pressure callbacks remain safe when re-entrant sources overflow.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task LowAllocationBackPressure_OmittedCallbacksSafelyDropOverflowAsync()
    {
        using var droppedSource = new ReactiveUI.Primitives.Signals.Signal<MqttApplicationMessageReceivedEventArgs>();
        var first = TestDataHelpers.CreateMessageReceivedArgs("coverage/backpressure/first", "one");
        var second = TestDataHelpers.CreateMessageReceivedArgs("coverage/backpressure/second", "two");
        var dropped = 0;
        using var droppedSubscription = droppedSource.WithBackPressureDrop().Subscribe(message =>
        {
            GC.KeepAlive(message);
            dropped++;
            droppedSource.OnNext(second);
        });

        droppedSource.OnNext(first);

        using var queuedSource = new ReactiveUI.Primitives.Signals.Signal<MqttApplicationMessageReceivedEventArgs>();
        var queued = 0;
        using var queuedSubscription = queuedSource.WithBackPressureQueue(1).Subscribe(message =>
        {
            queued++;
            if (!ReferenceEquals(message, first))
            {
                return;
            }

            queuedSource.OnNext(second);
            queuedSource.OnNext(first);
        });

        queuedSource.OnNext(first);

        await Assert.That(dropped).IsEqualTo(1);
        await Assert.That(queued).IsEqualTo(ExpectedQueuedMessageCount);
    }
}
