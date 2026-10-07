// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.OmronPlcRx.Reactive;
using IoT.Driver.OmronPlcRx.Reactive.Tags;
using MQTTnet.Rx.OmronPlc.Reactive;
using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
using OmronOrdered = MQTTnet.Rx.OmronPlc.Reactive.OmronOrderedWriteExtensions;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.OmronPlcRx;
using IoT.Driver.OmronPlcRx.Tags;
using MQTTnet.Rx.OmronPlc;
using ObservableSignalConversion = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
using OmronOrdered = MQTTnet.Rx.OmronPlc.OmronOrderedWriteExtensions;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests ordered Omron MQTT writes, cancellation, and terminal error handling.</summary>
public sealed class OmronOrderedWriteExtensionsTests
{
    /// <summary>The topic used by the ordered writer tests.</summary>
    private const string Topic = "omron/ordered/write";

    /// <summary>The registered tag name used by the ordered writer tests.</summary>
    private const string TagName = "OrderedTag";

    /// <summary>The number of seconds allowed for a controlled operation.</summary>
    private const int OperationTimeoutSeconds = 5;

    /// <summary>The first sequential write number.</summary>
    private const int FirstWriteNumber = 1;

    /// <summary>The second sequential write number.</summary>
    private const int SecondWriteNumber = 2;

    /// <summary>The first received payload.</summary>
    private const string FirstPayload = "1";

    /// <summary>The second received payload.</summary>
    private const string SecondPayload = "2";

    /// <summary>The third received payload.</summary>
    private const string ThirdPayload = "3";

    /// <summary>The fourth expected write count.</summary>
    private const int ExpectedWriteCount = 4;

    /// <summary>The payload written by the raw client.</summary>
    private const int RawPayloadValue = 17;

    /// <summary>The payload written by the resilient client.</summary>
    private const int ResilientPayloadValue = 23;

    /// <summary>The value argument index on the native write method.</summary>
    private const int NativeValueArgumentIndex = 1;

    /// <summary>The cancellation token argument index on the native write method.</summary>
    private const int NativeCancellationArgumentIndex = 2;

    /// <summary>The common tag key used by the raw client tests.</summary>
    private static readonly LogicalTagKey<int> RawTag = new(TagName);

    /// <summary>The common tag key used by the resilient client tests.</summary>
    private static readonly LogicalTagKey<int> ResilientTag = new(nameof(ResilientTag));

    /// <summary>Verifies that MQTT receives remain nonblocking and native writes preserve message order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RawClient_QueuesWritesInOrderWithoutBlockingMessageDeliveryAsync()
    {
        using var client = new MockMqttClient();
        var plc = Substitute.For<IOmronPlcRx>();
        var firstStarted = NewSignal<int>();
        var secondStarted = NewSignal<int>();
        var firstWrite = NewSignal<bool>();
        var secondWrite = NewSignal<bool>();
        var calls = 0;
        _ = plc.WriteValueAsync(Arg.Any<LogicalTagKey<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var value = call.ArgAt<int>(NativeValueArgumentIndex);
                return Interlocked.Increment(ref calls) switch
                {
                    FirstWriteNumber => StartWrite(value, firstStarted, firstWrite.Task),
                    SecondWriteNumber => StartWrite(value, secondStarted, secondWrite.Task),
                    _ => Task.FromException(new InvalidOperationException("Unexpected write.")),
                };
            });

        using var subscription = Signal.Emit<IMqttClient>(client).SubscribeOmronPlcTagOrdered(
            Topic,
            RawTag,
            plc,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture),
            static _ => { },
            CancellationToken.None);

        await client.SimulateMessageReceivedAsync(Topic, FirstPayload);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        await client.SimulateMessageReceivedAsync(Topic, SecondPayload);
        await Assert.That(Volatile.Read(ref calls)).IsEqualTo(FirstWriteNumber);

        _ = firstWrite.TrySetResult(true);
        await Assert.That(await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds))).IsEqualTo(SecondWriteNumber);
        _ = secondWrite.TrySetResult(true);
    }

    /// <summary>Verifies successful writes through all four client-sequence overloads.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClientSequenceOverloads_WriteTypedValuesAsync()
    {
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var plc = Substitute.For<IOmronPlcRx>();
        var completedWrites = NewSignal<bool>();
        var calls = 0;
        _ = plc.WriteValueAsync(Arg.Any<LogicalTagKey<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (Interlocked.Increment(ref calls) == ExpectedWriteCount)
                {
                    _ = completedWrites.TrySetResult(true);
                }

                return Task.CompletedTask;
            });
        var rawKey = RawTag;
        var resilientKey = ResilientTag;
        using var rawSubscription = Signal.Emit<IMqttClient>(rawClient).SubscribeOmronPlcTagOrdered(Topic, rawKey, plc, ParseInteger, static _ => { }, CancellationToken.None);
        using var resilientSubscription = Signal.Emit<IResilientMqttClient>(resilientClient).SubscribeOmronPlcTagOrdered(
            Topic,
            resilientKey,
            plc,
            ParseInteger,
            static _ => { },
            CancellationToken.None);
        using var asyncRawSubscription = SignalAsync.Emit<IMqttClient>(rawClient).SubscribeOmronPlcTagOrdered(
            Topic,
            rawKey,
            plc,
            ParseInteger,
            static _ => { },
            CancellationToken.None);
        using var asyncResilientSubscription = SignalAsync.Emit<IResilientMqttClient>(resilientClient)
            .SubscribeOmronPlcTagOrdered(
                Topic,
                resilientKey,
                plc,
                ParseInteger,
                static _ => { },
                CancellationToken.None);

        await rawClient.SimulateMessageReceivedAsync(Topic, RawPayloadValue.ToString(CultureInfo.InvariantCulture));
        await resilientClient.SimulateMessageReceivedAsync(Topic, ResilientPayloadValue.ToString(CultureInfo.InvariantCulture));
        await completedWrites.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        await Assert.That(calls).IsEqualTo(ExpectedWriteCount);
    }

    /// <summary>Verifies that disposing the subscription cancels an active write and prevents queued writes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Dispose_CancelsActiveWriteAndDiscardsQueuedMessagesAsync()
    {
        using var client = new MockMqttClient();
        var plc = Substitute.For<IOmronPlcRx>();
        var started = NewSignal<CancellationToken>();
        var calls = 0;
        _ = plc.WriteValueAsync(Arg.Any<LogicalTagKey<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _ = Interlocked.Increment(ref calls);
                var token = call.ArgAt<CancellationToken>(NativeCancellationArgumentIndex);
                _ = started.TrySetResult(token);
                return WaitForCancellationAsync(token);
            });
        var subscription = Signal.Emit<IMqttClient>(client).SubscribeOmronPlcTagOrdered(
            Topic,
            RawTag,
            plc,
            ParseInteger,
            static _ => { },
            CancellationToken.None);

        await client.SimulateMessageReceivedAsync(Topic, FirstPayload);
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        await client.SimulateMessageReceivedAsync(Topic, SecondPayload);
        subscription.Dispose();

        await Assert.That(token.IsCancellationRequested).IsTrue();
        await Assert.That(calls).IsEqualTo(FirstWriteNumber);
    }

    /// <summary>Verifies that external cancellation cancels the active write and removes the message subscription.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExternalCancellation_CancelsWriteAndStopsReceivingAsync()
    {
        using var client = new MockMqttClient();
        using var cancellation = new CancellationTokenSource();
        var plc = Substitute.For<IOmronPlcRx>();
        var started = NewSignal<CancellationToken>();
        var calls = 0;
        _ = plc.WriteValueAsync(Arg.Any<LogicalTagKey<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _ = Interlocked.Increment(ref calls);
                var token = call.ArgAt<CancellationToken>(NativeCancellationArgumentIndex);
                _ = started.TrySetResult(token);
                return WaitForCancellationAsync(token);
            });
        using var subscription = Signal.Emit<IMqttClient>(client).SubscribeOmronPlcTagOrdered(
            Topic,
            RawTag,
            plc,
            ParseInteger,
            static _ => { },
            cancellation.Token);

        await client.SimulateMessageReceivedAsync(Topic, FirstPayload);
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        await client.SimulateMessageReceivedAsync(Topic, SecondPayload);
        await cancellation.CancelAsync();
        await client.SimulateMessageReceivedAsync(Topic, ThirdPayload);

        await Assert.That(token.IsCancellationRequested).IsTrue();
        await Assert.That(calls).IsEqualTo(FirstWriteNumber);
    }

    /// <summary>Verifies that parser and native write failures reach the mandatory error callback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParserAndNativeFailures_InvokeErrorCallbackAsync()
    {
        using var client = new MockMqttClient();
        var plc = Substitute.For<IOmronPlcRx>();
        var parseError = new FormatException("Invalid payload.");
        var parserCallback = NewSignal<Exception>();
        using var parserSubscription = Signal.Emit<IMqttClient>(client).SubscribeOmronPlcTagOrdered(
            Topic,
            RawTag,
            plc,
            _ => throw parseError,
            error => _ = parserCallback.TrySetResult(error),
            CancellationToken.None);

        await client.SimulateMessageReceivedAsync(Topic, "bad");
        await Assert.That(await parserCallback.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(parseError);

        using var secondClient = new MockMqttClient();
        var nativeError = new InvalidOperationException("Native write failed.");
        var nativeCallback = NewSignal<Exception>();
        var failingPlc = Substitute.For<IOmronPlcRx>();
        _ = failingPlc.WriteValueAsync(Arg.Any<LogicalTagKey<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(nativeError));
        using var nativeSubscription = Signal.Emit<IMqttClient>(secondClient).SubscribeOmronPlcTagOrdered(
            Topic,
            RawTag,
            failingPlc,
            ParseInteger,
            error => _ = nativeCallback.TrySetResult(error),
            CancellationToken.None);

        await secondClient.SimulateMessageReceivedAsync(Topic, "7");
        await Assert.That(await nativeCallback.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(nativeError);
    }

    /// <summary>Verifies that source failures reach the callback for all four client sequence types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AllOverloads_SourceFailure_InvokesErrorCallbackAsync()
    {
        var expectedError = new InvalidOperationException("Client source failed.");
        var rawSource = new TestSignal<IMqttClient>();
        var resilientSource = new TestSignal<IResilientMqttClient>();
        var asyncRawSource = new TestSignal<IMqttClient>();
        var asyncResilientSource = new TestSignal<IResilientMqttClient>();
        var rawError = NewSignal<Exception>();
        var resilientError = NewSignal<Exception>();
        var asyncRawError = NewSignal<Exception>();
        var asyncResilientError = NewSignal<Exception>();
        using var simulator = CreateSimulator();
        var tag = new LogicalTagKey<int>(TagName);
        using var rawSubscription = rawSource.SubscribeOmronPlcTagOrdered(Topic, tag, simulator, ParseInteger, error => _ = rawError.TrySetResult(error), CancellationToken.None);
        using var resilientSubscription = resilientSource.SubscribeOmronPlcTagOrdered(Topic, tag, simulator, ParseInteger, error => _ = resilientError.TrySetResult(error), CancellationToken.None);
        using var asyncRawSubscription = ObservableSignalConversion.ToSignal(asyncRawSource).SubscribeOmronPlcTagOrdered(
            Topic,
            tag,
            simulator,
            ParseInteger,
            error => _ = asyncRawError.TrySetResult(error),
            CancellationToken.None);
        using var asyncResilientSubscription = ObservableSignalConversion.ToSignal(asyncResilientSource).SubscribeOmronPlcTagOrdered(
            Topic,
            tag,
            simulator,
            ParseInteger,
            error => _ = asyncResilientError.TrySetResult(error),
            CancellationToken.None);

        rawSource.OnError(expectedError);
        resilientSource.OnError(expectedError);
        asyncRawSource.OnError(expectedError);
        asyncResilientSource.OnError(expectedError);

        await Assert.That(await rawError.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(expectedError);
        await Assert.That(await resilientError.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(expectedError);
        await Assert.That(await asyncRawError.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(expectedError);
        await Assert.That(await asyncResilientError.Task.WaitAsync(TimeSpan.FromSeconds(OperationTimeoutSeconds)))
            .IsSameReferenceAs(expectedError);
    }

    /// <summary>Verifies all overloads reject absent dependencies and the required error callback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AllOverloads_ValidateRequiredArgumentsAsync()
    {
        var rawClients = Signal.None<IMqttClient>();
        var resilientClients = Signal.None<IResilientMqttClient>();
        var asyncRawClients = SignalAsync.None<IMqttClient>();
        var asyncResilientClients = SignalAsync.None<IResilientMqttClient>();
        using var simulator = CreateSimulator();
        var tag = new LogicalTagKey<int>(TagName);
        IObservable<IMqttClient> missingRaw = null!;
        IObservable<IResilientMqttClient> missingResilient = null!;
        IObservableAsync<IMqttClient> missingAsyncRaw = null!;
        IObservableAsync<IResilientMqttClient> missingAsyncResilient = null!;

        await Assert.That(() => OmronOrdered.SubscribeOmronPlcTagOrdered(missingRaw, Topic, tag, simulator, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.SubscribeOmronPlcTagOrdered(" ", tag, simulator, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentException>();
        await Assert.That(() => rawClients.SubscribeOmronPlcTagOrdered(Topic, tag, simulator, ParseInteger, null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilientClients.SubscribeOmronPlcTagOrdered(Topic, tag, simulator, null!, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncRawClients.SubscribeOmronPlcTagOrdered(Topic, tag, null!, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilientClients.SubscribeOmronPlcTagOrdered(Topic, tag, simulator, ParseInteger, null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronOrdered.SubscribeOmronPlcTagOrdered(missingAsyncRaw, Topic, tag, simulator, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronOrdered.SubscribeOmronPlcTagOrdered(missingResilient, Topic, tag, simulator, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronOrdered.SubscribeOmronPlcTagOrdered(missingAsyncResilient, Topic, tag, simulator, ParseInteger, static _ => { }, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Creates a task completion source configured for asynchronous continuations.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>The new task completion source.</returns>
    private static TaskCompletionSource<T> NewSignal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Waits indefinitely until cancellation ends the native write.</summary>
    /// <param name="cancellationToken">The token supplied to the native write.</param>
    /// <returns>A task that completes only when canceled.</returns>
    private static Task WaitForCancellationAsync(CancellationToken cancellationToken) =>
        Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

    /// <summary>Completes the write-start signal and returns the pending result task.</summary>
    /// <param name="value">The value being written.</param>
    /// <param name="started">The start signal.</param>
    /// <param name="writeTask">The pending native write.</param>
    /// <returns>The native write task.</returns>
    private static Task StartWrite(
        int value,
        TaskCompletionSource<int> started,
        Task writeTask)
    {
        _ = started.TrySetResult(value);
        return writeTask;
    }

    /// <summary>Parses an integer payload using invariant culture.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed value.</returns>
    private static int ParseInteger(string payload) => int.Parse(payload, CultureInfo.InvariantCulture);

    /// <summary>Creates a simulator with the test tag registered.</summary>
    /// <returns>The configured simulator.</returns>
    private static OmronPlcSimulator CreateSimulator()
    {
        var simulator = new OmronPlcSimulator();
        simulator.Seed(new(TagName, "D100"), 0);
        return simulator;
    }
}
