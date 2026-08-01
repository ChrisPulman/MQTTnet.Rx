// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MQTTnet.Protocol;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
using MQTTnet.Rx.Client.MemoryEfficient;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes behavioral coverage for the observable compatibility bridges.</summary>
public sealed partial class ObservableBridgeCoverageClosureFinalTests
{
    /// <summary>The JSON payload shared by bridge tests.</summary>
    private const string Payload = "payload";

    /// <summary>The JSON object used to cover nested null value conversion.</summary>
    private const string JsonObjectPayload = "{\"none\":null}";

    /// <summary>The JSON integer used by bridge tests.</summary>
    private const int ExpectedInteger = 42;

    /// <summary>The floating-point JSON value used to exercise numeric fallback conversion.</summary>
    private const double ExpectedFloatingPoint = 1.5D;

    /// <summary>The expected count for the two input messages.</summary>
    private const int ExpectedMessageCount = 2;

    /// <summary>The unavailable topic-level index.</summary>
    private const int UnavailableTopicLevel = 9;

    /// <summary>The expected retry subscription count.</summary>
    private const int ExpectedRetryAttempts = 2;

    /// <summary>The UTF-8 payload shared by byte-publish tests.</summary>
    private static readonly byte[] PayloadBytes = "payload"u8.ToArray();

    /// <summary>The bounded wait used for asynchronous bridge handoffs.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>The bounded delay used to let fire-and-forget bridge drains settle.</summary>
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromMilliseconds(50);

    /// <summary>The cached metadata used to deserialize integer payloads.</summary>
    private static readonly JsonTypeInfo<int> IntegerTypeInfo = JsonTypeInfo.CreateJsonTypeInfo<int>(
        new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() });

    /// <summary>Exercises topic matching, payload deserialization, and conversion failure paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncPayloadBridge_HandlesMatcherAndDeserializerEdgeCasesAsync()
    {
        var matching = TestDataHelpers.CreateMessageReceivedArgs(
            "root/alpha/value",
            ExpectedInteger.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var malformed = TestDataHelpers.CreateMessageReceivedArgs("root/beta/value", "not-json");
        var empty = TestDataHelpers.CreateMessageReceivedArgs("root/gamma/value", string.Empty);
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source =
            TestObservableBridge.ToSignal(Signal.FromEnumerable([matching, malformed]));
        await VerifyTopicFilteringAsync(source);
        await VerifyPayloadConversionsAsync(source, empty);
    }

    /// <summary>Exercises invalid placeholder characters and literal mismatches during topic extraction.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncPayloadBridge_RejectsInvalidPlaceholdersAndLiteralMismatchesAsync()
    {
        var message = TestDataHelpers.CreateMessageReceivedArgs("root/alpha/value", Payload);
        var source = SignalAsync.Return(message);

        await Assert.That(async () => await source
            .ExtractTopicValues("root/{1invalid}/value")
            .ToObservable()
            .CollectAsync(Timeout))
            .Throws<ArgumentException>();
        await Assert.That(async () => await source
            .ExtractTopicValues("root/{name\u0301}/value")
            .ToObservable()
            .CollectAsync(Timeout))
            .Throws<ArgumentException>();

        var literalMismatch = await source
            .ExtractTopicValues("root/x{name}/value")
            .ToObservable()
            .CollectAsync(Timeout);
        await Assert.That(literalMismatch).IsEmpty();
    }

    /// <summary>Exercises raw and resilient async facade methods that defer to synchronous subscriptions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncClientBridge_ExposesRawAndResilientSubscriptionAndPublishFamiliesAsync()
    {
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var raw = SignalAsync.Return<IMqttClient>(rawClient);
        var resilient = SignalAsync.Return<IResilientMqttClient>(resilientClient);
        var text = SignalAsync.Return(("closure/text", Payload));
        var bytes = SignalAsync.Return(("closure/bytes", PayloadBytes));

        _ = raw.SubscribeToTopics("closure/raw/one");
        _ = raw.SubscribeToTopic("closure/raw/two");
        _ = raw.DiscoverTopics();
        _ = raw.DiscoverTopics(TimeSpan.FromSeconds(1));
        _ = raw.DiscoverTopics(TimeSpan.FromSeconds(1), TimeProvider.System);
        _ = resilient.SubscribeToTopics("closure/resilient/one");
        _ = resilient.SubscribeToTopic("closure/resilient/two");
        _ = resilient.DiscoverTopics();
        _ = resilient.DiscoverTopics(TimeSpan.FromSeconds(1));
        _ = resilient.DiscoverTopics(TimeSpan.FromSeconds(1), TimeProvider.System);

        var textPublish = resilient
            .PublishMessage(text, MqttQualityOfServiceLevel.AtLeastOnce, false)
            .FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await textPublish;
        var bytePublish = resilient
            .PublishMessage(bytes, MqttQualityOfServiceLevel.AtMostOnce, true)
            .FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await bytePublish;
        var defaultTextPublish = resilient.PublishMessage(text).FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await defaultTextPublish;
        var qosTextPublish = resilient
            .PublishMessage(text, MqttQualityOfServiceLevel.ExactlyOnce)
            .FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await qosTextPublish;
        var defaultBytePublish = resilient.PublishMessage(bytes).FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await defaultBytePublish;
        var qosBytePublish = resilient
            .PublishMessage(bytes, MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await qosBytePublish;

        await Assert.That(rawClient.PublishedMessages).IsEmpty();
    }

    /// <summary>Exercises retry, grouped-terminal delivery, and task-pool terminal notifications.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PrimitivesCompatibilityBridge_RetriesAndForwardsTerminalNotificationsAsync()
    {
        var attempt = 0;
        var matching = TestDataHelpers.CreateMessageReceivedArgs("retry/value", Payload);
        var retryingSource = Signal.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
        {
            attempt++;
            if (attempt == 1)
            {
                observer.OnError(new InvalidOperationException("retry"));
            }
            else
            {
                observer.OnNext(matching);
                observer.OnCompleted();
            }

            return EmptyDisposable.Instance;
        });

        var retried = await TestObservableBridge
            .ToSignal(retryingSource)
            .WhereTopicMatchesAny("retry/#")
            .ToObservable()
            .CollectAsync(Timeout);
        var grouped = await LowAllocExtensions.GroupByTopic(Signal.FromEnumerable([matching]))
            .Select(static group => group.Key)
            .CollectAsync(Timeout);
        var observed = await LowAllocExtensions
            .ObserveOnThreadPool(Signal.FromEnumerable([matching]))
            .CollectAsync(Timeout);
        var observedError = new InvalidOperationException("task-pool");

        await Assert.That(async () => await LowAllocExtensions
                .ObserveOnThreadPool(Signal.Fail<MqttApplicationMessageReceivedEventArgs>(observedError))
                .CollectAsync(Timeout))
            .Throws<InvalidOperationException>();
        await Assert.That(attempt).IsEqualTo(ExpectedRetryAttempts);
        await Assert.That(retried).Count().IsEqualTo(1);
        await Assert.That(grouped).Count().IsEqualTo(1);
        await Assert.That(observed).Count().IsEqualTo(1);
    }

    /// <summary>Exercises grouped error forwarding and a nonterminal task-pool notification drain.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PrimitivesCompatibilityBridge_ForwardsGroupedErrorsAndDrainsNonterminalValuesAsync()
    {
        using var source = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        var message = TestDataHelpers.CreateMessageReceivedArgs("group/value", Payload);
        var groupError = new InvalidOperationException("group error");
        var observedError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var groups = TestObservableExtensions.Subscribe(
            LowAllocExtensions.GroupByTopic(source),
            group => _ = TestObservableExtensions.Subscribe(
                group,
                static _ => { },
                error => _ = observedError.TrySetResult(error)),
            static _ => { });
        source.OnNext(message);
        source.OnError(groupError);
        var forwarded = await observedError.Task.WaitAsync(Timeout);

        using var scheduled = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        var observedValue = new TaskCompletionSource<MqttApplicationMessageReceivedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var taskPool = TestObservableExtensions.Subscribe(
            LowAllocExtensions.ObserveOnThreadPool(scheduled),
            value => _ = observedValue.TrySetResult(value));
        scheduled.OnNext(message);
        var received = await observedValue.Task.WaitAsync(Timeout);

        await Assert.That(forwarded).IsSameReferenceAs(groupError);
        await Assert.That(received).IsSameReferenceAs(message);
    }

    /// <summary>Exercises bridge subscription failures and cancellation handling.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_ForwardsAsyncSubscriptionFailuresAsync()
    {
        var failure = new InvalidOperationException("subscription failure");
        var observedFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var throwing = SignalAsync.Create<int>((_, _) => ValueTask.FromException<IAsyncDisposable>(failure));
        using var failedSubscription = TestObservableExtensions.Subscribe(
            throwing.ToObservable(),
            static _ => { },
            exception => _ = observedFailure.TrySetResult(exception));
        var forwarded = await observedFailure.Task.WaitAsync(Timeout);

        await Assert.That(forwarded).IsSameReferenceAs(failure);
    }

    /// <summary>Exercises asynchronous bridge cancellation, delivery failures, and error-resume forwarding.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_HandlesDeliveryCancellationFailuresAndResumeAsync()
    {
        using var cancellation = new CancellationTokenSource();
        using var cancellationSource = new TestSignal<int>();
        var cancellationEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var cancellationSubscription = await TestObservableBridge.ToSignal(cancellationSource).SubscribeAsync(
            async (value, token) =>
            {
                _ = cancellationEntered.TrySetResult(true);
                await cancellation.CancelAsync();
                await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, token);
            },
            static (_, _) => ValueTask.CompletedTask,
            static _ => ValueTask.CompletedTask,
            cancellation.Token);
        cancellationSource.OnNext(ExpectedInteger);
        await cancellationEntered.Task.WaitAsync(Timeout);
        await Task.Delay(DrainTimeout);

        using var failureSource = new TestSignal<int>();
        var failureDelivered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryFailure = new InvalidOperationException("delivery failure");
        await using var failureSubscription = await TestObservableBridge.ToSignal(failureSource).SubscribeAsync(
            (value, token) =>
            {
                _ = failureDelivered.TrySetResult(true);
                return ValueTask.FromException(deliveryFailure);
            },
            static (_, _) => ValueTask.CompletedTask,
            static _ => ValueTask.CompletedTask,
            CancellationToken.None);
        failureSource.OnNext(ExpectedInteger);
        await failureDelivered.Task.WaitAsync(Timeout);
        await Task.Delay(DrainTimeout);

        var resumeFailure = new InvalidOperationException("resume failure");
        var resumed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resuming = SignalAsync.Create<int>(async (observer, token) =>
        {
            await observer.OnErrorResumeAsync(resumeFailure, token);
            return ReactiveUI.Primitives.Async.Disposables.DisposableAsync.Empty;
        });
        using var resumeSubscription = TestObservableExtensions.Subscribe(
            resuming.ToObservable(),
            static _ => { },
            error => _ = resumed.TrySetResult(error));
        var observed = await resumed.Task.WaitAsync(Timeout);

        await Assert.That(observed).IsSameReferenceAs(resumeFailure);
    }

    /// <summary>Verifies disposing an asynchronous observable bridge more than once is harmless.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_DisposeIsIdempotentAsync()
    {
        var subscribed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = SignalAsync.Create<int>((observer, token) =>
        {
            _ = subscribed.TrySetResult(true);
            return new ValueTask<IAsyncDisposable>(new RecordingAsyncDisposable(disposed));
        });
        using var subscription = TestObservableExtensions.Subscribe(source.ToObservable());
        await subscribed.Task.WaitAsync(Timeout);
        subscription.Dispose();
        subscription.Dispose();
        await disposed.Task.WaitAsync(Timeout);

        await Assert.That(disposed.Task.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Exercises deterministic notification queueing, cancellation, and observer failure paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_SerializesAndTerminatesFailedDeliveriesAsync()
    {
        using var preCanceled = new CancellationTokenSource();
        await preCanceled.CancelAsync();
        using var preCanceledSource = new TestSignal<int>();
        var preCanceledObserver = new ControlledAsyncObserver<int>(new InvalidOperationException("must not run"));
        await using var preCanceledSubscription = await TestObservableBridge
            .ToSignal(preCanceledSource)
            .SubscribeAsync(preCanceledObserver, preCanceled.Token);
        preCanceledSource.OnNext(ExpectedInteger);

        using var failedSource = new TestSignal<int>();
        var releaseFailure = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failedObserver = new ControlledAsyncObserver<int>(
            new InvalidOperationException("delivery"),
            null,
            releaseFailure.Task);
        await using var failedSubscription = await TestObservableBridge
            .ToSignal(failedSource)
            .SubscribeAsync(failedObserver, CancellationToken.None);
        failedSource.OnNext(ExpectedInteger);
        await failedObserver.Entered.WaitAsync(Timeout);
        failedSource.OnNext(ExpectedInteger + 1);
        _ = releaseFailure.TrySetResult(true);
        await failedObserver.Finished.WaitAsync(Timeout);

        using var canceledSource = new TestSignal<int>();
        using var deliveryCancellation = new CancellationTokenSource();
        var canceledObserver = new ControlledAsyncObserver<int>(
            new OperationCanceledException(deliveryCancellation.Token),
            deliveryCancellation);
        await using var canceledSubscription = await TestObservableBridge
            .ToSignal(canceledSource)
            .SubscribeAsync(canceledObserver, deliveryCancellation.Token);
        canceledSource.OnNext(ExpectedInteger);
        await canceledObserver.Finished.WaitAsync(Timeout);
        await Task.Delay(DrainTimeout);

        await Assert.That(preCanceledObserver.Entered.IsCompleted).IsFalse();
        await Assert.That(failedObserver.Entered.IsCompletedSuccessfully).IsTrue();
        await Assert.That(canceledObserver.Entered.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Exercises asynchronous subscription cancellation, terminal failure, and disposal failures.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ObservableCompatibilityBridge_HandlesCanceledSubscriptionsAndDisposalFailuresAsync()
    {
        var canceledError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledSubscriptionSource = SignalAsync.Create<int>(
            static (_, _) => ValueTask.FromException<IAsyncDisposable>(new OperationCanceledException("subscribe")));
        using var canceledSubscription = TestObservableExtensions.Subscribe(
            canceledSubscriptionSource.ToObservable(),
            static _ => { },
            error => _ = canceledError.TrySetResult(error));

        var terminalFailure = new InvalidOperationException("terminal");
        var observedTerminal = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var terminalSubscription = TestObservableExtensions.Subscribe(
            SignalAsync.Fail<int>(terminalFailure).ToObservable(),
            static _ => { },
            error => _ = observedTerminal.TrySetResult(error));
        var terminal = await observedTerminal.Task.WaitAsync(Timeout);

        var canceledDisposal = new ThrowingAsyncDisposable(new OperationCanceledException("dispose"));
        var canceledDisposeSource = CreateAsyncDisposableSource(canceledDisposal);
        using var canceledDisposeSubscription = TestObservableExtensions.Subscribe(
            canceledDisposeSource.ToObservable());
        await canceledDisposal.Subscribed.WaitAsync(Timeout);
        canceledDisposeSubscription.Dispose();
        await canceledDisposal.Attempted.WaitAsync(Timeout);

        var failedDisposal = new ThrowingAsyncDisposable(new InvalidOperationException("dispose"));
        var failedDisposeSource = CreateAsyncDisposableSource(failedDisposal);
        using var failedDisposeSubscription = TestObservableExtensions.Subscribe(failedDisposeSource.ToObservable());
        await failedDisposal.Subscribed.WaitAsync(Timeout);
        failedDisposeSubscription.Dispose();
        await failedDisposal.Attempted.WaitAsync(Timeout);
        await Task.Delay(DrainTimeout);

        await Assert.That(canceledError.Task.IsCompleted).IsFalse();
        await Assert.That(terminal).IsSameReferenceAs(terminalFailure);
        await Assert.That(canceledDisposal.Attempted.IsCompletedSuccessfully).IsTrue();
        await Assert.That(failedDisposal.Attempted.IsCompletedSuccessfully).IsTrue();
    }

    /// <summary>Exercises retry reentrancy, retry disposal, task creation, and task-pool coordination.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PrimitivesCompatibilityBridge_HandlesRetryAndTaskPoolRacesAsync()
    {
        var message = TestDataHelpers.CreateMessageReceivedArgs("retry/race", Payload);
        IObserver<MqttApplicationMessageReceivedEventArgs>? retryObserver = null;
        var retrySource = new ScriptedObservable<MqttApplicationMessageReceivedEventArgs>((attempt, observer) =>
        {
            retryObserver = observer;
            if (attempt == 1)
            {
                observer.OnError(new InvalidOperationException("retry"));
            }

            return EmptyDisposable.Instance;
        });
        var retrySubscription = TestObservableExtensions.Subscribe(
            retrySource.ToDictionary(),
            static _ => { });
        var assignedRetryObserver = retryObserver
            ?? throw new InvalidOperationException("Retry observer was not assigned.");
        assignedRetryObserver.OnNext(message);
        assignedRetryObserver.OnCompleted();
        retrySubscription.Dispose();
        retrySubscription.Dispose();
        assignedRetryObserver.OnNext(message);
        assignedRetryObserver.OnCompleted();
        assignedRetryObserver.OnError(new InvalidOperationException("after disposal"));

        var throwingSource = new ScriptedObservable<MqttApplicationMessageReceivedEventArgs>(static (attempt, _) =>
            attempt == 1
                ? throw new InvalidOperationException("subscribe")
                : EmptyDisposable.Instance);
        using var throwingSubscription = TestObservableExtensions.Subscribe(
            throwingSource.ToDictionary(),
            static _ => { });

        using var client = new MockMqttClient();
        _ = await ReactiveClientOperations
            .Disconnect(Signal.Return<IMqttClient>(client))
            .FirstAsync(Timeout);

        await Assert.That(retrySource.SubscribeCount).IsEqualTo(ExpectedRetryAttempts);
        await Assert.That(throwingSource.SubscribeCount).IsEqualTo(ExpectedRetryAttempts);
    }

    /// <summary>Creates an asynchronous source whose returned lifetime fails when disposed.</summary>
    /// <param name="disposable">The failing lifetime.</param>
    /// <returns>The asynchronous source.</returns>
    private static IObservableAsync<int> CreateAsyncDisposableSource(ThrowingAsyncDisposable disposable) =>
        SignalAsync.Create<int>((_, _) =>
        {
            _ = disposable.SignalSubscribed();
            return new ValueTask<IAsyncDisposable>(disposable);
        });

    /// <summary>Verifies the topic filter paths exposed through the asynchronous bridge.</summary>
    /// <param name="source">The source to filter.</param>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyTopicFilteringAsync(
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        var oneFilter = await source.WhereTopicMatchesAny("root/+/value").ToObservable().CollectAsync(Timeout);
        var severalFilters = await source
            .WhereTopicMatchesAny("missing/#", "root/+/value")
            .ToObservable()
            .CollectAsync(Timeout);
        var noFilters = await source.WhereTopicMatchesAny().ToObservable().CollectAsync(Timeout);
        var unmatchedFilter = await source
            .WhereTopicMatchesAny("other/#", "missing/#")
            .ToObservable()
            .CollectAsync(Timeout);
        var impossibleLevel = await source
            .SelectTopicLevel(UnavailableTopicLevel)
            .ToObservable()
            .CollectAsync(Timeout);

        await Assert.That(oneFilter).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(severalFilters).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(noFilters).IsEmpty();
        await Assert.That(unmatchedFilter).IsEmpty();
        await Assert.That(impossibleLevel).IsEmpty();
    }

    /// <summary>Verifies JSON conversion and invalid-payload paths exposed through the asynchronous bridge.</summary>
    /// <param name="source">The source to convert.</param>
    /// <param name="empty">The empty-payload message.</param>
    /// <returns>A task that represents the asynchronous verification.</returns>
    private static async Task VerifyPayloadConversionsAsync(
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> source,
        MqttApplicationMessageReceivedEventArgs empty)
    {
        var invalidDictionaries = await source.ToDictionary().ToObservable().CollectAsync(Timeout);
        var metadataValues = await source.ToObject(IntegerTypeInfo).ToObservable().CollectAsync(Timeout);
        var converterValues = await source
            .ToObject(static payload => JsonSerializer.Deserialize<int>(payload))
            .ToObservable()
            .CollectAsync(Timeout);
        var repeatedCaptureFailure = await source
            .ExtractTopicValues("root/{name}{name}x/value")
            .ToObservable()
            .CollectAsync(Timeout);
        var dictionaryWithNull = await SignalAsync
            .Return(TestDataHelpers.CreateMessageReceivedArgs("root/object/value", JsonObjectPayload))
            .ToDictionary()
            .ToObservable()
            .FirstAsync(Timeout);
        var emptyDictionary = await SignalAsync.Return(empty).ToDictionary().ToObservable().FirstAsync(Timeout);
        var hugeNumber = await SignalAsync
            .Return(TestDataHelpers.CreateMessageReceivedArgs("root/number/value", "{\"number\":1e400}"))
            .ToDictionary()
            .ToObservable()
            .CollectAsync(Timeout);
        var floatingNumber = await SignalAsync
            .Return(TestDataHelpers.CreateMessageReceivedArgs("root/number/value", "{\"number\":1.5}"))
            .ToDictionary()
            .ToObservable()
            .CollectAsync(Timeout);
        var nullDictionary = dictionaryWithNull ?? throw new InvalidOperationException("Expected a JSON dictionary.");
        var hugeNumberDictionary = hugeNumber[0]
            ?? throw new InvalidOperationException("Expected a numeric dictionary.");
        var floatingNumberDictionary = floatingNumber[0]
            ?? throw new InvalidOperationException("Expected a numeric dictionary.");

        await Assert.That(invalidDictionaries[0]).IsNull();
        await Assert.That(metadataValues[0]).IsEqualTo(ExpectedInteger);
        await Assert.That(metadataValues[1]).IsEqualTo(0);
        await Assert.That(converterValues[1]).IsEqualTo(0);
        await Assert.That(repeatedCaptureFailure).IsEmpty();
        await Assert.That(nullDictionary["none"]).IsNull();
        await Assert.That(emptyDictionary).IsNull();
        await Assert.That(hugeNumberDictionary["number"]).IsEqualTo(double.PositiveInfinity);
        await Assert.That(floatingNumberDictionary["number"]).IsEqualTo(ExpectedFloatingPoint);
    }
}
