// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Covers the final client event-factory and retry lifecycle paths.</summary>
public sealed class FinalClientCreateCoverageClosureTests
{
    /// <summary>Specifies the second value emitted after retrying the source.</summary>
    private const int ExpectedSecondValue = 2;

    /// <summary>Specifies a late value that must be ignored after disposal.</summary>
    private const int IgnoredValue = 3;

    /// <summary>Specifies the expected number of source subscriptions.</summary>
    private const int ExpectedSourceSubscriptionCount = 2;

    /// <summary>Specifies the expected number of active source lifetime disposals.</summary>
    private const int ExpectedSourceDisposalCount = 1;

    /// <summary>Specifies the time allowed for an asynchronous observer to receive an event.</summary>
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Verifies synchronous readiness observes connection changes and owns both event subscriptions.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WhenReady_SynchronousProjectionEmitsOnConnectionAsync()
    {
        // Arrange
        using var client = new MockResilientMqttClient();
        var readyClients = new List<IResilientMqttClient>();

        // Act
        using var subscription = Signal.Emit<IResilientMqttClient>(client)
            .WhenReady()
            .Subscribe(readyClients.Add);
        await client.SimulateConnectedAsync();
        await client.SimulateDisconnectedAsync();

        // Assert
        await Assert.That(readyClients).Count().IsEqualTo(1);
        await Assert.That(readyClients[0]).IsSameReferenceAs(client);
    }

    /// <summary>Verifies every asynchronous raw-client projection releases its MQTT event handler.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task AsyncRawEventProjections_EmitAndReleaseEveryHandlerAsync()
    {
        // Arrange
        using var client = new MockMqttClient();
        var receivedCount = 0;
        var connectedCount = 0;
        var disconnectedCount = 0;
        using var cancellation = new CancellationTokenSource();

        // Act
        var received = await SubscribeDirectAsync(
            client.ObserveApplicationMessageReceived(),
            _ => receivedCount++,
            cancellation.Token);
        var connected = await SubscribeDirectAsync(
            client.ObserveConnected(),
            _ => connectedCount++,
            cancellation.Token);
        var connecting = await SubscribeDirectAsync(
            client.ObserveConnecting(),
            cancellationToken: cancellation.Token);
        var disconnected = await SubscribeDirectAsync(
            client.ObserveDisconnected(),
            _ => disconnectedCount++,
            cancellation.Token);
        var inspected = await SubscribeDirectAsync(
            client.ObserveInspectPackage(),
            cancellationToken: cancellation.Token);

        await client.SimulateMessageReceivedAsync("coverage/create", "payload");
        await client.SimulateConnectedAsync();
        await client.SimulateDisconnectedAsync();

        await cancellation.CancelAsync();
        await received.DisposeAsync();
        await connected.DisposeAsync();
        await connecting.DisposeAsync();
        await disconnected.DisposeAsync();
        await inspected.DisposeAsync();

        await client.SimulateMessageReceivedAsync("coverage/create", "ignored");
        await client.SimulateConnectedAsync();
        await client.SimulateDisconnectedAsync();

        // Assert
        await Assert.That(receivedCount).IsEqualTo(1);
        await Assert.That(connectedCount).IsEqualTo(1);
        await Assert.That(disconnectedCount).IsEqualTo(1);
    }

    /// <summary>Verifies awaited resilient handler registrations emit values and are disposed.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HandlerRegistration_EmitsAndReleasesRegistrationAsync()
    {
        // Arrange
        var client = Substitute.For<IResilientMqttClient>();
        Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask>? registeredHandler = null;
        var registration = new RecordingDisposable();
        using var cancellation = new CancellationTokenSource();
        _ = client.RegisterSubscriptionsChangedHandler(
                Arg.Any<Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask>>())
            .Returns(callInfo =>
            {
                registeredHandler = callInfo.Arg<Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask>>();
                return registration;
            });
        var notification = new SubscriptionsChangedEventArgs([], []);
        var observed = new TaskCompletionSource<SubscriptionsChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        var subscription = await SubscribeDirectAsync(
            client.ObserveSubscriptionsChanged(),
            value => _ = observed.TrySetResult(value),
            cancellation.Token);
        await (registeredHandler ?? throw new InvalidOperationException("The handler was not registered."))(
            notification,
            CancellationToken.None);
        var result = await observed.Task.WaitAsync(EventTimeout);
        await cancellation.CancelAsync();
        await subscription.DisposeAsync();
        await registration.Disposed.WaitAsync(EventTimeout);

        // Assert
        await Assert.That(result).IsSameReferenceAs(notification);
        await Assert.That(registration.IsDisposed).IsTrue();
    }

    /// <summary>Verifies retry resubscription, completion, disposal, and late-notification guards.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task RetryForever_ResubscribesAndIgnoresNotificationsAfterDisposalAsync()
    {
        // Arrange
        IObserver<int>? firstSourceObserver = null;
        IObserver<int>? secondSourceObserver = null;
        var sourceDisposalCount = 0;
        var source = new ScriptedObservable<int>((attempt, observer) =>
        {
            if (attempt == 1)
            {
                firstSourceObserver = observer;
            }
            else
            {
                secondSourceObserver = observer;
            }

            return new RecordingDisposable(() => sourceDisposalCount++);
        });
        var downstream = new RecordingObserver<int>();
        var retrying = InvokeRetryForever(source);

        // Act
        var subscription = retrying.Subscribe(downstream);
        (firstSourceObserver ?? throw new InvalidOperationException("The first source was not subscribed."))
            .OnNext(1);
        firstSourceObserver.OnError(new InvalidOperationException("retry"));
        (secondSourceObserver ?? throw new InvalidOperationException("The retry source was not subscribed."))
            .OnNext(ExpectedSecondValue);
        secondSourceObserver.OnCompleted();
        subscription.Dispose();
        subscription.Dispose();
        secondSourceObserver.OnNext(IgnoredValue);
        secondSourceObserver.OnError(new InvalidOperationException("ignored"));
        secondSourceObserver.OnCompleted();

        // Assert
        await Assert.That(source.SubscribeCount).IsEqualTo(ExpectedSourceSubscriptionCount);
        await Assert.That(sourceDisposalCount).IsEqualTo(ExpectedSourceDisposalCount);
        await Assert.That(downstream.Values).IsEquivalentTo([1, ExpectedSecondValue]);
        await Assert.That(downstream.CompletionCount).IsEqualTo(1);
        await Assert.That(downstream.Errors).IsEmpty();
    }

    /// <summary>Invokes the internal retry factory so its subscription lifecycle can be tested directly.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="source">The source whose failures are retried.</param>
    /// <returns>The retrying observable.</returns>
    private static IObservable<T> InvokeRetryForever<T>(IObservable<T> source)
    {
        var factoryType = typeof(Create).Assembly.GetType(
            TestTypeNames.CreateObservable,
            throwOnError: true) ?? throw new InvalidOperationException("The observable factory type was not found.");
        var factoryMethod = factoryType.GetMethod(
                "RetryForever",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The retry factory method was not found.");
        return factoryMethod.MakeGenericMethod(typeof(T)).Invoke(null, [source]) as IObservable<T>
            ?? throw new InvalidOperationException("The retry factory returned an unexpected value.");
    }

    /// <summary>Subscribes through the async contract and returns its source-owned lifetime.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="source">The asynchronous observable to subscribe to.</param>
    /// <param name="onNext">The optional action invoked for each value.</param>
    /// <param name="cancellationToken">The token used to cancel the subscription.</param>
    /// <returns>The subscription lifetime returned by the source.</returns>
    private static ValueTask<IAsyncDisposable> SubscribeDirectAsync<T>(
        IObservableAsync<T> source,
        Action<T>? onNext = null,
        CancellationToken cancellationToken = default) =>
        source.SubscribeAsync(new RecordingAsyncObserver<T>(onNext), cancellationToken);

    /// <summary>Forwards asynchronous values to an optional synchronous recording callback.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The optional action invoked for each value.</param>
    private sealed class RecordingAsyncObserver<T>(Action<T>? onNext) : IObserverAsync<T>
    {
        /// <inheritdoc/>
        ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

        /// <inheritdoc/>
        ValueTask IObserverAsync<T>.OnCompletedAsync(ReactiveUI.Primitives.Result result)
        {
            _ = result;
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        ValueTask IObserverAsync<T>.OnErrorResumeAsync(Exception error, CancellationToken cancellationToken)
        {
            _ = error;
            _ = cancellationToken;
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        ValueTask IObserverAsync<T>.OnNextAsync(T value, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            onNext?.Invoke(value);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Records observer notifications.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    private sealed class RecordingObserver<T> : IObserver<T>
    {
        /// <summary>Gets the observed values.</summary>
        internal List<T> Values { get; } = [];

        /// <summary>Gets the observed errors.</summary>
        internal List<Exception> Errors { get; } = [];

        /// <summary>Gets the number of completion notifications.</summary>
        internal int CompletionCount { get; private set; }

        /// <inheritdoc/>
        public void OnCompleted() => CompletionCount++;

        /// <inheritdoc/>
        public void OnError(Exception error) => Errors.Add(error);

        /// <inheritdoc/>
        public void OnNext(T value) => Values.Add(value);
    }

    /// <summary>Records disposal and optionally invokes a callback once.</summary>
    /// <param name="onDispose">The optional callback to invoke on first disposal.</param>
    private sealed class RecordingDisposable(Action? onDispose = null) : IDisposable
    {
        /// <summary>Signals when this lifetime has been disposed.</summary>
        private readonly TaskCompletionSource<bool> _disposed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Indicates whether this lifetime has been disposed.</summary>
        private bool _isDisposed;

        /// <summary>Gets a value indicating whether this lifetime has been disposed.</summary>
        internal bool IsDisposed => _isDisposed;

        /// <summary>Gets the task that completes when this lifetime is disposed.</summary>
        internal Task Disposed => _disposed.Task;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            onDispose?.Invoke();
            _ = _disposed.TrySetResult(true);
        }
    }
}
