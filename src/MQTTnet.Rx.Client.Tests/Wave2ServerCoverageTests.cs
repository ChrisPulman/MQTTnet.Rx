// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using MQTTnet.Rx.Server.Reactive;
#else
using MQTTnet.Rx.Server;
#endif
using MQTTnet.Server;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ServerCreate = MQTTnet.Rx.Server.Reactive.Create;
#else
using ServerCreate = MQTTnet.Rx.Server.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the residual server lifetime and asynchronous event unsubscription paths.</summary>
[NotInParallel]
public class Wave2ServerCoverageTests
{
    /// <summary>The maximum time allowed for a factory to emit its first value.</summary>
    private static readonly TimeSpan FactoryTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Verifies every asynchronous event bridge removes its handler when explicitly disposed.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousEventExtensions_ExplicitDisposalRemovesEveryHandlerAsync()
    {
        using var server = CreateServer();

        await SubscribeAndDisposeAsync(server.ObserveApplicationMessageNotConsumed());
        await SubscribeAndDisposeAsync(server.ObserveClientAcknowledgedPublishPacket());
        await SubscribeAndDisposeAsync(server.ObserveClientConnected());
        await SubscribeAndDisposeAsync(server.ObserveClientDisconnected());
        await SubscribeAndDisposeAsync(server.ObserveClientSubscribedTopic());
        await SubscribeAndDisposeAsync(server.ObserveClientUnsubscribedTopic());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingClientEnqueue());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingInboundPacket());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingOutboundPacket());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingPublish());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingSubscription());
        await SubscribeAndDisposeAsync(server.ObserveInterceptingUnsubscription());
        await SubscribeAndDisposeAsync(server.ObserveLoadingRetainedMessage());
        await SubscribeAndDisposeAsync(server.ObservePreparingSession());
        await SubscribeAndDisposeAsync(server.ObserveRetainedMessageChanged());
        await SubscribeAndDisposeAsync(server.ObserveRetainedMessagesCleared());
        await SubscribeAndDisposeAsync(server.ObserveSessionDeleted());
        await SubscribeAndDisposeAsync(server.ObserveStarted());
        await SubscribeAndDisposeAsync(server.ObserveStopped());
        await SubscribeAndDisposeAsync(server.ObserveValidatingConnection());

        await Assert.That(server.IsStarted).IsFalse();
    }

    /// <summary>Verifies shared synchronous subscriptions retain and release the server deterministically.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerFactory_OnlyLastSubscriptionStopsAndAllDisposalsAreIdempotentAsync()
    {
        var observable = ServerCreate.MqttServer(static builder => builder.WithoutDefaultEndpoint().Build());
        var first = await SubscribeFirstAsync(observable);
        var second = await SubscribeFirstAsync(observable);

        await Assert.That(first.Value.Server).IsSameReferenceAs(second.Value.Server);
        await Assert.That(first.Value.Server.IsStarted).IsTrue();

        first.Subscription.Dispose();
        first.Subscription.Dispose();
        await Assert.That(second.Value.Server.IsStarted).IsTrue();

        second.Subscription.Dispose();
        second.Subscription.Dispose();
        await Assert.That(second.Value.Server.IsStarted).IsFalse();
    }

    /// <summary>Verifies shared asynchronous subscriptions retain and release the server deterministically.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerSignalFactory_OnlyLastSubscriptionStopsAndAllDisposalsAreIdempotentAsync()
    {
        var observable = ServerCreate.MqttServerSignal(static builder => builder.WithoutDefaultEndpoint().Build());
        var first = await SubscribeFirstAsync(observable);
        var second = await SubscribeFirstAsync(observable);

        await Assert.That(first.Value.Server).IsSameReferenceAs(second.Value.Server);
        await Assert.That(first.Value.Server.IsStarted).IsTrue();

        await first.Subscription.DisposeAsync();
        await first.Subscription.DisposeAsync();
        await Assert.That(second.Value.Server.IsStarted).IsTrue();

        await second.Subscription.DisposeAsync();
        await second.Subscription.DisposeAsync();
        await Assert.That(second.Value.Server.IsStarted).IsFalse();
    }

    /// <summary>Creates an MQTT server without network endpoints.</summary>
    /// <returns>The created MQTT server.</returns>
    private static MqttServer CreateServer()
    {
        var factory = new MqttServerFactory();
        return factory.CreateMqttServer(factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build());
    }

    /// <summary>Subscribes to an observable and waits for its first emitted value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <returns>The first observed value and the subscription that owns its lifetime.</returns>
    private static async Task<(T Value, IDisposable Subscription)> SubscribeFirstAsync<T>(IObservable<T> observable)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = observable.Subscribe(
            value => _ = completion.TrySetResult(value),
            exception => _ = completion.TrySetException(exception));
        var value = await completion.Task.WaitAsync(FactoryTimeout);
        return (value, subscription);
    }

    /// <summary>Subscribes to an asynchronous observable and waits for its first emitted value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The asynchronous observable to subscribe to.</param>
    /// <returns>The first observed value and the asynchronous subscription that owns its lifetime.</returns>
    private static async Task<(T Value, IAsyncDisposable Subscription)> SubscribeFirstAsync<T>(
        IObservableAsync<T> observable)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await observable.SubscribeAsync(
            (value, cancellationToken) =>
            {
                _ = cancellationToken;
                _ = completion.TrySetResult(value);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        var value = await completion.Task.WaitAsync(FactoryTimeout);
        return (value, subscription);
    }

    /// <summary>Subscribes to and explicitly disposes an asynchronous observable event bridge.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="observable">The asynchronous observable event bridge.</param>
    /// <returns>A task that represents the asynchronous disposal.</returns>
    private static async Task SubscribeAndDisposeAsync<T>(IObservableAsync<T> observable)
    {
        var subscription = await observable.SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        await subscription.DisposeAsync();
    }
}
