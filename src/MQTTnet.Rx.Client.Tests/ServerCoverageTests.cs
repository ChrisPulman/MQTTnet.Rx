// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json;
using MQTTnet.Packets;
using MQTTnet.Protocol;
#if REACTIVE_SHIM
using MQTTnet.Rx.Server.Reactive;
#else
using MQTTnet.Rx.Server;
#endif
using MQTTnet.Server;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using ServerCreate = MQTTnet.Rx.Server.Reactive.Create;
#else
using ServerCreate = MQTTnet.Rx.Server.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the MQTT server package runtime surface.</summary>
[NotInParallel]
public class ServerCoverageTests
{
    /// <summary>The maximum time allowed for a server factory to emit.</summary>
    private static readonly TimeSpan FactoryTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The correlation data used by retained-message tests.</summary>
    private static readonly byte[] TestCorrelationData = [4, 5, 6];

    /// <summary>The payload used by retained-message tests.</summary>
    private static readonly byte[] TestPayload = [1, 2, 3];

    /// <summary>Verifies retained messages round-trip all persisted properties.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RetainedMessageModel_RoundTripsAllPropertiesAsync()
    {
        var userProperties = new List<MqttUserProperty>
        {
            new("key", new ReadOnlyMemory<byte>("value"u8.ToArray())),
        };
        var message = new MqttApplicationMessage
        {
            Topic = "coverage/topic",
            PayloadSegment = new(TestPayload),
            UserProperties = userProperties,
            ResponseTopic = "coverage/response",
            CorrelationData = TestCorrelationData,
            ContentType = "application/octet-stream",
            PayloadFormatIndicator = MqttPayloadFormatIndicator.Unspecified,
            QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
        };

        var model = MqttRetainedMessageModel.Create(message);
        var result = model.ToApplicationMessage();

        await Assert.That(model.Topic).IsEqualTo(message.Topic);
        await Assert.That(model.ResponseTopic).IsEqualTo(message.ResponseTopic);
        await Assert.That(model.ContentType).IsEqualTo(message.ContentType);
        await Assert.That(model.PayloadFormatIndicator).IsEqualTo(message.PayloadFormatIndicator);
        await Assert.That(model.QualityOfServiceLevel).IsEqualTo(message.QualityOfServiceLevel);
        await Assert.That(model.UserProperties).IsSameReferenceAs(userProperties);
        await Assert.That(model.CorrelationData).IsSameReferenceAs(TestCorrelationData);
        await Assert.That(result.Retain).IsTrue();
        await Assert.That(result.Dup).IsFalse();
        await Assert.That(result.Topic).IsEqualTo(message.Topic);
        var resultPayload = result.Payload.ToArray();
        await Assert.That(resultPayload.Length).IsEqualTo(TestPayload.Length);
        for (var index = 0; index < TestPayload.Length; index++)
        {
            await Assert.That(resultPayload[index]).IsEqualTo(TestPayload[index]);
        }
    }

    /// <summary>Verifies retained-message null and empty-payload branches.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RetainedMessageModel_HandlesNullAndEmptyValuesAsync()
    {
        await Assert.That(static () => MqttRetainedMessageModel.Create(null!)).Throws<ArgumentNullException>();

        var result = new MqttRetainedMessageModel().ToApplicationMessage();

        await Assert.That(result.Payload.IsEmpty).IsTrue();
        await Assert.That(result.Retain).IsTrue();
        await Assert.That(result.Topic).IsNull();
    }

    /// <summary>Verifies all synchronous server event bridges attach and detach their handlers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousEventExtensions_SubscribeAndDisposeAsync()
    {
        using var server = CreateServer();

        SubscribeAndDispose(server.ApplicationMessageNotConsumed());
        SubscribeAndDispose(server.ClientAcknowledgedPublishPacket());
        SubscribeAndDispose(server.ClientConnected());
        SubscribeAndDispose(server.ClientDisconnected());
        SubscribeAndDispose(server.ClientSubscribedTopic());
        SubscribeAndDispose(server.ClientUnsubscribedTopic());
        SubscribeAndDispose(server.InterceptingClientEnqueue());
        SubscribeAndDispose(server.InterceptingInboundPacket());
        SubscribeAndDispose(server.InterceptingOutboundPacket());
        SubscribeAndDispose(server.InterceptingPublish());
        SubscribeAndDispose(server.InterceptingSubscription());
        SubscribeAndDispose(server.InterceptingUnsubscription());
        SubscribeAndDispose(server.LoadingRetainedMessage());
        SubscribeAndDispose(server.PreparingSession());
        SubscribeAndDispose(server.RetainedMessageChanged());
        SubscribeAndDispose(server.RetainedMessagesCleared());
        SubscribeAndDispose(server.SessionDeleted());
        SubscribeAndDispose(server.Started());
        SubscribeAndDispose(server.Stopped());
        SubscribeAndDispose(server.ValidatingConnection());

        await Assert.That(server.IsStarted).IsFalse();
    }

    /// <summary>Verifies all asynchronous server event bridges attach and detach their handlers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousEventExtensions_SubscribeAndDisposeAsync()
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

    /// <summary>Verifies synchronous and asynchronous lifecycle observables emit real server events.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LifecycleEventExtensions_EmitStartedAndStoppedEventsAsync()
    {
        using var server = CreateServer();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedAsync = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stoppedAsync = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var startedSubscription = server.Started().Subscribe(eventArgs =>
        {
            _ = eventArgs;
            _ = started.TrySetResult(true);
        });
        using var stoppedSubscription = server.Stopped().Subscribe(eventArgs =>
        {
            _ = eventArgs;
            _ = stopped.TrySetResult(true);
        });
        await using var startedAsyncSubscription = await server.ObserveStarted().SubscribeAsync(
            (eventArgs, cancellationToken) =>
            {
                _ = eventArgs;
                _ = cancellationToken;
                _ = startedAsync.TrySetResult(true);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        await using var stoppedAsyncSubscription = await server.ObserveStopped().SubscribeAsync(
            (eventArgs, cancellationToken) =>
            {
                _ = eventArgs;
                _ = cancellationToken;
                _ = stoppedAsync.TrySetResult(true);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        await server.StartAsync();
        await started.Task.WaitAsync(FactoryTimeout);
        await startedAsync.Task.WaitAsync(FactoryTimeout);
        await server.StopAsync();
        await stopped.Task.WaitAsync(FactoryTimeout);
        await stoppedAsync.Task.WaitAsync(FactoryTimeout);

        await Assert.That(started.Task.IsCompletedSuccessfully).IsTrue();
        await Assert.That(startedAsync.Task.IsCompletedSuccessfully).IsTrue();
        await Assert.That(stopped.Task.IsCompletedSuccessfully).IsTrue();
        await Assert.That(stoppedAsync.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Verifies the synchronous factory shares its server and releases subscriber resources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerFactory_SharesAndDisposesServerLifetimeAsync()
    {
        var originalFactory = ServerCreate.MqttFactory;
        var replacementFactory = new MqttServerFactory();
        ServerCreate.NewMqttFactory(replacementFactory);
        try
        {
            await Assert.That(ServerCreate.MqttFactory).IsSameReferenceAs(replacementFactory);
            await Assert.That(static () => ServerCreate.MqttServer(null!)).Throws<ArgumentNullException>();

            var observable = ServerCreate.MqttServer(static builder => builder.WithoutDefaultEndpoint().Build());
            var first = await SubscribeFirstAsync(observable);
            var second = await SubscribeFirstAsync(observable);
            var firstResourceDisposed = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            first.Value.Disposable.Add(Scope.Create(
                firstResourceDisposed,
                static completion => _ = completion.TrySetResult(true)));

            await Assert.That(first.Value.Server).IsSameReferenceAs(second.Value.Server);
            await Assert.That(first.Value.Server.IsStarted).IsTrue();

            first.Subscription.Dispose();
            await Assert.That(firstResourceDisposed.Task.IsCompleted).IsTrue();
            await Assert.That(second.Value.Server.IsStarted).IsTrue();
            first.Subscription.Dispose();
            second.Subscription.Dispose();
            second.Subscription.Dispose();
        }
        finally
        {
            ServerCreate.NewMqttFactory(originalFactory);
        }
    }

    /// <summary>Verifies the asynchronous factory shares its server and releases subscriber resources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerSignalFactory_SharesAndDisposesServerLifetimeAsync()
    {
        await Assert.That(static () => ServerCreate.MqttServerSignal(null!)).Throws<ArgumentNullException>();

        var observable = ServerCreate.MqttServerSignal(static builder => builder.WithoutDefaultEndpoint().Build());
        var first = await SubscribeFirstAsync(observable);
        var second = await SubscribeFirstAsync(observable);
        var firstResourceDisposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Value.Disposable.Add(Scope.Create(
            firstResourceDisposed,
            static completion => _ = completion.TrySetResult(true)));

        await Assert.That(first.Value.Server).IsSameReferenceAs(second.Value.Server);
        await Assert.That(first.Value.Server.IsStarted).IsTrue();

        await first.Subscription.DisposeAsync();
        await Assert.That(firstResourceDisposed.Task.IsCompleted).IsTrue();
        await Assert.That(second.Value.Server.IsStarted).IsTrue();
        await first.Subscription.DisposeAsync();
        await second.Subscription.DisposeAsync();
        await second.Subscription.DisposeAsync();
    }

    /// <summary>Verifies retained-message factories exercise existing and missing persistence stores.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RetainedMessageFactories_LoadExistingAndMissingStoresAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mqttnet-rx-server-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        var storePath = Path.Combine(directory, "RetainedMessages.json");
        try
        {
            var model = new MqttRetainedMessageModel { Topic = "retained/topic", Payload = TestPayload };
            MqttRetainedMessageModel[] models = [model];
            await File.WriteAllTextAsync(storePath, JsonSerializer.Serialize(models));

            var persisted = ServerCreate.MqttServerWithRetainedMessages(
                static builder => builder.WithoutDefaultEndpoint().Build(),
                directory);
            var persistedSubscription = await SubscribeFirstAsync(persisted);
            await Assert.That(persistedSubscription.Value.Server.IsStarted).IsTrue();
            persistedSubscription.Subscription.Dispose();

            await File.WriteAllTextAsync(storePath, "null");
            var empty = ServerCreate.MqttServerWithRetainedMessages(
                static builder => builder.WithoutDefaultEndpoint().Build(),
                directory);
            var emptySubscription = await SubscribeFirstAsync(empty);
            await Assert.That(emptySubscription.Value.Server.IsStarted).IsTrue();
            emptySubscription.Subscription.Dispose();

            File.Delete(storePath);
            var missing = ServerCreate.MqttServerWithRetainedMessages(
                static builder => builder.WithoutDefaultEndpoint().Build(),
                directory);
            var missingSubscription = await SubscribeFirstAsync(missing);
            await Assert.That(missingSubscription.Value.Server.IsStarted).IsTrue();
            missingSubscription.Subscription.Dispose();

            await Assert.That(static () => ServerCreate.MqttServerWithRetainedMessages(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            File.Delete(storePath);
            Directory.Delete(directory);
        }
    }

    /// <summary>Verifies asynchronous retained-message factories load existing and missing stores.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RetainedMessageAsyncFactories_LoadExistingAndMissingStoresAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mqttnet-rx-server-async-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        var storePath = Path.Combine(directory, "RetainedMessages.json");
        try
        {
            var model = new MqttRetainedMessageModel { Topic = "retained/async", Payload = TestPayload };
            MqttRetainedMessageModel[] models = [model];
            await File.WriteAllTextAsync(storePath, JsonSerializer.Serialize(models));

            var persisted = ServerCreate.MqttServerWithRetainedMessagesSignal(
                static builder => builder.WithoutDefaultEndpoint().Build(),
                directory);
            var persistedSubscription = await SubscribeFirstAsync(persisted);
            await Assert.That(persistedSubscription.Value.Server.IsStarted).IsTrue();
            await persistedSubscription.Subscription.DisposeAsync();

            File.Delete(storePath);
            var missing = ServerCreate.MqttServerWithRetainedMessagesSignal(
                static builder => builder.WithoutDefaultEndpoint().Build(),
                directory);
            var missingSubscription = await SubscribeFirstAsync(missing);
            await Assert.That(missingSubscription.Value.Server.IsStarted).IsTrue();
            await missingSubscription.Subscription.DisposeAsync();

            await Assert.That(static () => ServerCreate.MqttServerWithRetainedMessagesSignal(null!))
                .Throws<ArgumentNullException>();
        }
        finally
        {
            File.Delete(storePath);
            Directory.Delete(directory);
        }
    }

    /// <summary>Creates an MQTT server without network endpoints.</summary>
    /// <returns>The created MQTT server.</returns>
    private static MqttServer CreateServer()
    {
        var factory = new MqttServerFactory();
        return factory.CreateMqttServer(factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build());
    }

    /// <summary>Subscribes to a synchronous factory and waits for its first value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The factory observable.</param>
    /// <returns>The first value and the subscription that owns its lifetime.</returns>
    private static async Task<(T Value, IDisposable Subscription)> SubscribeFirstAsync<T>(IObservable<T> observable)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = observable.Subscribe(
            value => _ = completion.TrySetResult(value),
            exception => _ = completion.TrySetException(exception));
        var value = await completion.Task.WaitAsync(FactoryTimeout);
        return (value, subscription);
    }

    /// <summary>Subscribes to an asynchronous factory and waits for its first value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The asynchronous factory observable.</param>
    /// <returns>The first value and the asynchronous subscription that owns its lifetime.</returns>
    private static async Task<(T Value, IAsyncDisposable Subscription)> SubscribeFirstAsync<T>(
        IObservableAsync<T> observable)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = await observable.SubscribeAsync(
            (value, cancellationToken) =>
            {
                _ = completion.TrySetResult(value);
                _ = cancellationToken;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        var value = await completion.Task.WaitAsync(FactoryTimeout);
        return (value, subscription);
    }

    /// <summary>Attaches and detaches a synchronous observable subscription.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="observable">The observable to exercise.</param>
    private static void SubscribeAndDispose<T>(IObservable<T> observable)
    {
        using var subscription = observable.Subscribe();
    }

    /// <summary>Attaches and detaches an asynchronous observable subscription.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="observable">The asynchronous observable to exercise.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task SubscribeAndDisposeAsync<T>(IObservableAsync<T> observable)
    {
        await using var subscription = await observable.SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
    }
}
