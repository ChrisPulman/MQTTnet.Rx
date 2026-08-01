// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using MQTTnet.Rx.Server;
using ReactiveUI.Primitives.Async;
using ServerCreate = MQTTnet.Rx.Server.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes behavioral coverage for MQTT server lifetime and event-handler ownership.</summary>
[NotInParallel]
public class ServerCoverageClosureTests
{
    /// <summary>The maximum time allowed for asynchronous server operations.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>Verifies direct session disposal is idempotent and owns subscriber resources.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerSession_DirectDisposalIsIdempotentAndReleasesResourcesAsync()
    {
        var value = await SubscribeFirstAsync(
            ServerCreate.MqttServer(static builder => builder.WithoutDefaultEndpoint().Build()));
        using var resource = new RecordingDisposable();
        value.Value.Disposable.Add(resource);

        await Assert.That(value.Value.Disposable.IsDisposed).IsFalse();
        await value.Value.Disposable.DisposeAsync();
        await value.Value.Disposable.DisposeAsync();
        value.Subscription.Dispose();

        await Assert.That(value.Value.Disposable.IsDisposed).IsTrue();
        await Assert.That(resource.IsDisposed).IsTrue();
        await Assert.That(value.Value.Server.IsStarted).IsFalse();
    }

    /// <summary>Verifies retained factories exercise both default and explicit store-directory branches.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RetainedFactories_DefaultAndExplicitDirectoriesStartAndStopAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mqttnet-rx-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        try
        {
            var defaultStore = await SubscribeFirstAsync(
                ServerCreate.MqttServerWithRetainedMessages(
                    static builder => builder.WithoutDefaultEndpoint().Build()));
            defaultStore.Subscription.Dispose();

            var explicitStore = await SubscribeFirstAsync(
                ServerCreate.MqttServerWithRetainedMessages(
                    static builder => builder.WithoutDefaultEndpoint().Build(),
                    directory));
            explicitStore.Subscription.Dispose();

            var defaultAsyncStore = await SubscribeFirstAsync(
                ServerCreate.MqttServerWithRetainedMessagesSignal(
                    static builder => builder.WithoutDefaultEndpoint().Build()));
            await defaultAsyncStore.Subscription.DisposeAsync();

            var explicitAsyncStore = await SubscribeFirstAsync(
                ServerCreate.MqttServerWithRetainedMessagesSignal(
                    static builder => builder.WithoutDefaultEndpoint().Build(),
                    directory));
            await explicitAsyncStore.Subscription.DisposeAsync();

            await Assert.That(defaultStore.Value.Server.IsStarted).IsFalse();
            await Assert.That(explicitStore.Value.Server.IsStarted).IsFalse();
            await Assert.That(defaultAsyncStore.Value.Server.IsStarted).IsFalse();
            await Assert.That(explicitAsyncStore.Value.Server.IsStarted).IsFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>Verifies a factory can create a fresh server after its previous subscription wave stops.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerFactory_LaterSubscriptionWaveUsesFreshServerAsync()
    {
        var source = ServerCreate.MqttServer(static builder => builder.WithoutDefaultEndpoint().Build());
        var first = await SubscribeFirstAsync(source);
        await first.Value.Disposable.DisposeAsync();
        first.Subscription.Dispose();

        var second = await SubscribeFirstAsync(source);
        await Assert.That(ReferenceEquals(first.Value.Server, second.Value.Server)).IsFalse();
        await Assert.That(second.Value.Server.IsStarted).IsTrue();

        await second.Value.Disposable.DisposeAsync();
        second.Subscription.Dispose();
        await Assert.That(second.Value.Server.IsStarted).IsFalse();
    }

    /// <summary>Verifies synchronous server start failures stop after the configured retry bound.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerFactory_OccupiedEndpointTerminatesAfterBoundedRetriesAsync()
    {
        using var listener = new TcpListener(IPAddress.Any, 0);
        listener.Server.ExclusiveAddressUse = true;
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var emitted = false;
        var source = ServerCreate.MqttServerWithRetainedMessages(
            builder => builder.WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());

        using var subscription = source.Subscribe(
            value =>
            {
                emitted = true;
                value.Disposable.Dispose();
            },
            exception => _ = error.TrySetResult(exception));
        var terminalError = await error.Task.WaitAsync(Timeout);

        await Assert.That(emitted).IsFalse();
        await Assert.That(terminalError).IsNotNull();
    }

    /// <summary>Verifies a failed endpoint start detaches retained handlers and observes cancellation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task MqttServerSignal_OccupiedEndpointHonorsCancellationAsync()
    {
        using var listener = new TcpListener(IPAddress.Any, 0);
        listener.Server.ExclusiveAddressUse = true;
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cancellation = new CancellationTokenSource(Timeout);
        var source = ServerCreate.MqttServerWithRetainedMessagesSignal(
            builder => builder.WithDefaultEndpoint().WithDefaultEndpointPort(port).Build());

        var observer = new RecordingAsyncObserver<(MQTTnet.Server.MqttServer Server, MqttServerSession Disposable)>();
        await using var subscription = await source.SubscribeAsync(observer, cancellation.Token);
        var terminalError = await observer.Error.WaitAsync(Timeout);

        await Assert.That(terminalError).IsNotNull();
    }

    /// <summary>Verifies observer failures release both asynchronous factory session variants.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousFactories_ObserverFailureReleasesSessionAsync()
    {
        await ExerciseThrowingObserverAsync(
            ServerCreate.MqttServerSignal(static builder => builder.WithoutDefaultEndpoint().Build()));
        await ExerciseThrowingObserverAsync(
            ServerCreate.MqttServerWithRetainedMessagesSignal(
                static builder => builder.WithoutDefaultEndpoint().Build()));
    }

    /// <summary>Verifies observer failures release both synchronous factory session variants and terminate.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousFactories_ObserverFailureReleasesSessionAndTerminatesAsync()
    {
        await ExerciseThrowingObserverAsync(
            ServerCreate.MqttServer(static builder => builder.WithoutDefaultEndpoint().Build()));
        await ExerciseThrowingObserverAsync(
            ServerCreate.MqttServerWithRetainedMessages(static builder => builder.WithoutDefaultEndpoint().Build()));
    }

    /// <summary>Exercises a synchronous factory with an observer that rejects every value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The factory observable to exercise.</param>
    /// <returns>A task that completes after the bounded retries terminate.</returns>
    private static async Task ExerciseThrowingObserverAsync<T>(IObservable<T> observable)
    {
        var attempts = 0;
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = observable.Subscribe(
            value =>
            {
                GC.KeepAlive(value);
                _ = Interlocked.Increment(ref attempts);
                throw new InvalidOperationException("Expected observer failure.");
            },
            exception => _ = error.TrySetResult(exception));
        var terminalError = await error.Task.WaitAsync(Timeout);

        await Assert.That(attempts).IsGreaterThan(0);
        await Assert.That(terminalError).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Exercises an asynchronous factory with an observer that rejects every value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The factory observable to exercise.</param>
    /// <returns>A task that completes after the observer failure is raised.</returns>
    private static async Task ExerciseThrowingObserverAsync<T>(IObservableAsync<T> observable)
    {
        var observer = new RecordingAsyncObserver<T>(throwOnNext: true);
        await using var subscription = await observable.SubscribeAsync(observer, CancellationToken.None);
        var terminalError = await observer.Error.WaitAsync(Timeout);

        await Assert.That(observer.Attempts).IsGreaterThan(0);
        await Assert.That(terminalError).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Subscribes to a synchronous observable and waits for its first value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <returns>The first value and its owning subscription.</returns>
    private static async Task<(T Value, IDisposable Subscription)> SubscribeFirstAsync<T>(IObservable<T> observable)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = observable.Subscribe(
            value => _ = completion.TrySetResult(value),
            exception => _ = completion.TrySetException(exception));
        return (await completion.Task.WaitAsync(Timeout), subscription);
    }

    /// <summary>Subscribes to an asynchronous observable and waits for its first value.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The observable to subscribe to.</param>
    /// <returns>The first value and its owning subscription.</returns>
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
        return (await completion.Task.WaitAsync(Timeout), subscription);
    }

    /// <summary>Records deterministic synchronous disposal.</summary>
    private sealed class RecordingDisposable : IDisposable
    {
        /// <summary>Gets whether this instance has been disposed.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;
    }

    /// <summary>Records asynchronous terminal errors.</summary>
    /// <typeparam name="T">The observed element type.</typeparam>
    /// <param name="throwOnNext">Whether each value notification should fail.</param>
    private sealed class RecordingAsyncObserver<T>(bool throwOnNext = false) : IObserverAsync<T>
    {
        /// <summary>The source completed without producing the expected error.</summary>
        private static readonly InvalidOperationException UnexpectedCompletion = new("Expected a terminal error.");

        /// <summary>Signals the terminal error.</summary>
        private readonly TaskCompletionSource<Exception> _error = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets the terminal error task.</summary>
        public Task<Exception> Error => _error.Task;

        /// <summary>Gets the number of values delivered to this observer.</summary>
        public int Attempts { get; private set; }

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <inheritdoc/>
        public ValueTask OnCompletedAsync(ReactiveUI.Primitives.Result result)
        {
            _ = _error.TrySetResult(result.Exception ?? UnexpectedCompletion);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnErrorResumeAsync(Exception error, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _ = _error.TrySetResult(error);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnNextAsync(T value, CancellationToken cancellationToken)
        {
            GC.KeepAlive(value);
            _ = cancellationToken;
            Attempts++;
            return throwOnNext
                ? ValueTask.FromException(new InvalidOperationException("Expected observer failure."))
                : ValueTask.CompletedTask;
        }
    }
}
