// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes the final behavior and lifetime coverage paths for reactive client operations.</summary>
public sealed class FinalClientOperationsCoverageClosureTests
{
    /// <summary>The expected initial and event-driven connection status count.</summary>
    private const int ExpectedStatusCount = 3;

    /// <summary>The expected processed-notification count for the two resilient publish paths.</summary>
    private const int ExpectedProcessedNotificationCount = 2;

    /// <summary>The interval used to exercise the explicit periodic-ping branch.</summary>
    private static readonly TimeSpan PingInterval = TimeSpan.FromMilliseconds(1);

    /// <summary>The maximum time allowed for the periodic ping callback to execute.</summary>
    private static readonly TimeSpan PingCompletionTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Verifies connection-state subscriptions report transitions and remove every event handler.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ConnectionStatus_ReportsTransitionsAndDetachesEventHandlersAsync()
    {
        using var client = new MockMqttClient();
        var clients = Signal.Emit(client);
        var statuses = new List<bool>();
        using (var subscription = clients.ConnectionStatus().Subscribe(statuses.Add))
        {
            await client.SimulateConnectedAsync();
            await client.SimulateDisconnectedAsync();

            await Assert.That(statuses).IsEquivalentTo([false, true, false]);
            await Assert.That(client.DisconnectedHandlerCount).IsEqualTo(1);
        }

        await Assert.That(client.DisconnectedHandlerCount).IsZero();
    }

    /// <summary>Verifies default and explicit periodic-ping intervals attach to the supplied client stream.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task PingPeriodically_AcceptsDefaultAndExplicitIntervalsAsync()
    {
        var client = Substitute.For<IMqttClient>();
        var clients = Signal.Emit(client);
        var pinged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = client
            .PingAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                GC.KeepAlive(callInfo);
                _ = pinged.TrySetResult(true);
                return Task.CompletedTask;
            });
        using var subscriptions = new MultipleDisposable
        {
            clients.PingPeriodically().Subscribe(),
            clients.PingPeriodically(PingInterval).Subscribe(),
        };

        var pingWasObserved = await pinged.Task.WaitAsync(PingCompletionTimeout);

        await Assert.That(pingWasObserved).IsTrue();
    }

    /// <summary>Verifies asynchronous status transitions, handler cleanup, and the default ping interval.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AsyncOperations_ReportStatusTransitionsAndUseDefaultPingIntervalAsync()
    {
        using var client = new MockMqttClient();
        var clients = SignalAsync.Return<IMqttClient>(client);
        var statuses = new List<bool>();
        var statusesObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var subscription = await clients
            .ConnectionStatus()
            .SubscribeAsync(
                (status, cancellationToken) =>
                {
                    GC.KeepAlive(cancellationToken);
                    statuses.Add(status);
                    if (statuses.Count == ExpectedStatusCount)
                    {
                        _ = statusesObserved.TrySetResult(true);
                    }

                    return default;
                },
                cancellationToken: cancellation.Token);
        await WaitUntilAsync(() =>
            client.ConnectedHandlerCount == 1 && client.DisconnectedHandlerCount == 1);
        await client.SimulateConnectedAsync();
        await client.SimulateDisconnectedAsync();
        await client.SimulateDisconnectedAsync();
        _ = await statusesObserved.Task.WaitAsync(PingCompletionTimeout);
        await cancellation.CancelAsync();
        await subscription.DisposeAsync();
        await WaitUntilAsync(() =>
            client.ConnectedHandlerCount == 0 && client.DisconnectedHandlerCount == 0);
        _ = clients.PingPeriodically();

        await Assert.That(statuses).IsEquivalentTo([false, true, false]);
        await Assert.That(client.ConnectedHandlerCount).IsZero();
        await Assert.That(client.DisconnectedHandlerCount).IsZero();
    }

    /// <summary>Verifies resilient publish overloads enqueue payloads and forward processed notifications.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ResilientPublish_EnqueuesTextAndBytesAndForwardsProcessedNotificationsAsync()
    {
        using var client = new MockResilientMqttClient();
        var clients = Signal.Emit<IResilientMqttClient>(client);
        var processed = new List<ApplicationMessageProcessedEventArgs>();
        using var subscriptions = new MultipleDisposable
        {
            clients.PublishMessage(Signal.Emit(("coverage/final/text", "payload"))).Subscribe(processed.Add),
            clients.PublishMessage(Signal.Emit(("coverage/final/bytes", new byte[] { 1 }))).Subscribe(processed.Add),
        };

        await client.SimulateApplicationMessageProcessedAsync();

        await Assert.That(processed).Count().IsEqualTo(ExpectedProcessedNotificationCount);
    }

    /// <summary>Verifies topic-level grouping retains both existing and unavailable topic-level keys.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task GroupByTopicLevel_UsesLevelAndEmptyFallbackKeysAsync()
    {
        var keys = new List<string>();
        using var subscription = new[]
            {
                TestDataHelpers.CreateMessageReceivedArgs("coverage/final/value", "one"),
                TestDataHelpers.CreateMessageReceivedArgs("coverage", "two"),
            }
            .ToObservable()
            .GroupByTopicLevel(1)
            .Select(static group => group.Key)
            .Subscribe(keys.Add);

        await Assert.That(keys).IsEquivalentTo(["final", string.Empty]);
    }

    /// <summary>Waits for an asynchronous subscription lifecycle condition.</summary>
    /// <param name="condition">The condition to observe.</param>
    /// <returns>A task that completes when the condition becomes true.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(PingCompletionTimeout);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));
        while (!condition())
        {
            _ = await timer.WaitForNextTickAsync(cancellation.Token);
        }
    }
}
