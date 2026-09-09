// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.MitsubishiRx.Reactive;
using IoT.Driver.OmronPlcRx.Reactive;
using IoT.Driver.OmronPlcRx.Reactive.Tags;
using MQTTnet.Rx.Mitsubishi.Reactive;
using MQTTnet.Rx.OmronPlc.Reactive;
#else
using IoT.Driver.MitsubishiRx;
using IoT.Driver.OmronPlcRx;
using IoT.Driver.OmronPlcRx.Tags;
using MQTTnet.Rx.Mitsubishi;
using MQTTnet.Rx.OmronPlc;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using MitsubishiBulkExtensions = MQTTnet.Rx.Mitsubishi.Reactive.MitsubishiBulkMqttExtensions;
using MitsubishiClient = IoT.Driver.MitsubishiRx.Reactive.MitsubishiRx;
using OmronBulkExtensions = MQTTnet.Rx.OmronPlc.Reactive.OmronPlcBulkMqttExtensions;
#else
using MitsubishiBulkExtensions = MQTTnet.Rx.Mitsubishi.MitsubishiBulkMqttExtensions;
using MitsubishiClient = IoT.Driver.MitsubishiRx.MitsubishiRx;
using OmronBulkExtensions = MQTTnet.Rx.OmronPlc.OmronPlcBulkMqttExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises Omron and Mitsubishi logical-tag bulk MQTT bridges.</summary>
public sealed class OmronMitsubishiBulkBridgeTests
{
    /// <summary>The Omron bulk MQTT topic.</summary>
    private const string OmronTopic = "tests/omron/bulk";

    /// <summary>The first Omron logical tag name.</summary>
    private const string OmronFirstTag = "Omron.Bulk.First";

    /// <summary>The second Omron logical tag name.</summary>
    private const string OmronSecondTag = "Omron.Bulk.Second";

    /// <summary>The first Omron physical address.</summary>
    private const string OmronFirstAddress = "D300";

    /// <summary>The second Omron physical address.</summary>
    private const string OmronSecondAddress = "D301";

    /// <summary>The Mitsubishi bulk MQTT topic.</summary>
    private const string MitsubishiTopic = "tests/mitsubishi/bulk";

    /// <summary>The first Mitsubishi logical tag name.</summary>
    private const string MitsubishiFirstTag = "Mitsubishi.Bulk.First";

    /// <summary>The second Mitsubishi logical tag name.</summary>
    private const string MitsubishiSecondTag = "Mitsubishi.Bulk.Second";

    /// <summary>The first Mitsubishi device address.</summary>
    private const string MitsubishiFirstAddress = "D300";

    /// <summary>The second Mitsubishi device address.</summary>
    private const string MitsubishiSecondAddress = "D301";

    /// <summary>The shared async payload marker.</summary>
    private const string AsyncPayload = "async";

    /// <summary>The simulator port value used by the Mitsubishi fixture.</summary>
    private const int MitsubishiFixturePort = 49_999;

    /// <summary>The first Omron publish value.</summary>
    private const int OmronPublishedFirst = 17;

    /// <summary>The second Omron publish value.</summary>
    private const int OmronPublishedSecond = 29;

    /// <summary>The first Omron written value.</summary>
    private const int OmronWrittenFirst = 41;

    /// <summary>The second Omron written value.</summary>
    private const int OmronWrittenSecond = 43;

    /// <summary>The Omron formatted value.</summary>
    private const int OmronFormattedValue = 51;

    /// <summary>The Omron resilient observed value.</summary>
    private const int OmronResilientValue = 53;

    /// <summary>The first Omron observer value.</summary>
    private const int OmronObserverFirst = 61;

    /// <summary>The second Omron observer value.</summary>
    private const int OmronObserverSecond = 63;

    /// <summary>The Omron cancelled value.</summary>
    private const int OmronCancelledValue = 99;

    /// <summary>The first Mitsubishi publish value.</summary>
    private const ushort MitsubishiPublishedFirst = 71;

    /// <summary>The second Mitsubishi publish value.</summary>
    private const ushort MitsubishiPublishedSecond = 73;

    /// <summary>The first Mitsubishi written value.</summary>
    private const ushort MitsubishiWrittenFirst = 81;

    /// <summary>The second Mitsubishi written value.</summary>
    private const ushort MitsubishiWrittenSecond = 83;

    /// <summary>The first Mitsubishi formatted value.</summary>
    private const ushort MitsubishiFormattedFirst = 91;

    /// <summary>The second Mitsubishi formatted value.</summary>
    private const ushort MitsubishiFormattedSecond = 93;

    /// <summary>The first Mitsubishi observer value.</summary>
    private const ushort MitsubishiObserverFirst = 101;

    /// <summary>The second Mitsubishi observer value.</summary>
    private const ushort MitsubishiObserverSecond = 103;

    /// <summary>The Mitsubishi read-only write value.</summary>
    private const ushort MitsubishiReadOnlyValue = 111;

    /// <summary>The Mitsubishi cancelled value.</summary>
    private const ushort MitsubishiCancelledValue = 115;

    /// <summary>The first Omron default-subscribe closure value.</summary>
    private const int OmronClosureFirst = 71;

    /// <summary>The second Omron default-subscribe closure value.</summary>
    private const int OmronClosureSecond = 73;

    /// <summary>The Omron default resilient publish closure value.</summary>
    private const int OmronClosurePublished = 79;

    /// <summary>The expected Omron error count.</summary>
    private const int ExpectedOmronErrorCount = 3;

    /// <summary>The Mitsubishi error count before write-result failures.</summary>
    private const int ExpectedMitsubishiInitialErrorCount = 3;

    /// <summary>The expected minimum Mitsubishi error count.</summary>
    private const int ExpectedMitsubishiMinimumErrorCount = 4;

    /// <summary>The maximum duration allowed for asynchronous assertions.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Publishes and writes multiple Omron logical tags through the raw MQTT bridge.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task OmronRawBulkBridge_UsesObserveManyAndWriteManyAsync()
    {
        using var simulator = CreateOmronSimulator();
        using var logicalTags = CreateOmronLogicalTags(simulator);
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);
        using var publisher = clients.PublishOmronLogicalTags(
            OmronTopic,
            logicalTags,
            static value => $"{value.TagName}:{value.Value}",
            OmronFirstTag,
            OmronSecondTag).Subscribe();

        await simulator.WriteValueAsync(new(OmronFirstTag), OmronPublishedFirst, CancellationToken.None);
        await simulator.WriteValueAsync(new(OmronSecondTag), OmronPublishedSecond, CancellationToken.None);
        await WaitUntilAsync(() => client.PublishedMessages.Count >= 2);

        using var subscriber = clients.SubscribeOmronLogicalTags(
            $"{OmronTopic}/write",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            static errors => throw new InvalidOperationException("Unexpected Omron bulk error.", errors),
            CancellationToken.None);
        await client.SimulateMessageReceivedAsync($"{OmronTopic}/write", $"{OmronWrittenFirst},{OmronWrittenSecond}");
        await WaitUntilAsync(() => HasOmronWrite(simulator, OmronWrittenFirst)
            && HasOmronWrite(simulator, OmronWrittenSecond));

        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(OmronTopic);
        await Assert.That(HasPayload(client, $"{OmronFirstTag}:{OmronPublishedFirst}")).IsTrue();
        await Assert.That(HasPayload(client, $"{OmronSecondTag}:{OmronPublishedSecond}")).IsTrue();
        await Assert.That(HasOmronWrite(simulator, OmronWrittenFirst)).IsTrue();
        await Assert.That(HasOmronWrite(simulator, OmronWrittenSecond)).IsTrue();
    }

    /// <summary>Covers Omron default formatting, resilient wrappers, async wrappers, and validation guards.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task OmronBulkWrappers_FormatValuesAndValidateArgumentsAsync()
    {
        using var simulator = CreateOmronSimulator();
        using var logicalTags = CreateOmronLogicalTags(simulator);
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var rawClients = Signal.Emit<IMqttClient>(rawClient);
        var resilientClients = Signal.Emit<IResilientMqttClient>(resilientClient);
        using var rawPublish = rawClients.PublishOmronLogicalTags(OmronTopic, logicalTags, OmronFirstTag)
            .Subscribe(static _ => { });
        await simulator.WriteValueAsync(new(OmronFirstTag), OmronFormattedValue, CancellationToken.None);
        await WaitUntilAsync(() => HasPayload(rawClient, $"{OmronFormattedValue}"));
        var resilientFormatterObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resilientPublish = resilientClients.PublishOmronLogicalTags(
            OmronTopic,
            logicalTags,
            value =>
            {
                if (value.TagName == OmronSecondTag)
                {
                    _ = resilientFormatterObserved.TrySetResult();
                }

                return Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            },
            OmronSecondTag).Subscribe(static _ => { });
        await simulator.WriteValueAsync(new(OmronSecondTag), OmronResilientValue, CancellationToken.None);
        await resilientFormatterObserved.Task.WaitAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        using var rawAsyncSubscribe = SignalAsync.Return<IMqttClient>(rawClient).SubscribeOmronLogicalTags(
            $"{OmronTopic}/async/raw",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            null,
            CancellationToken.None);
        using var resilientSubscribe = resilientClients.SubscribeOmronLogicalTags(
            $"{OmronTopic}/resilient",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            CancellationToken.None);
        using var resilientAsyncSubscribe = SignalAsync.Return<IResilientMqttClient>(resilientClient).SubscribeOmronLogicalTags(
            $"{OmronTopic}/async/resilient",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            null,
            CancellationToken.None);
        _ = SignalAsync.Return<IMqttClient>(rawClient).PublishOmronLogicalTags(OmronTopic, logicalTags, OmronFirstTag).ToObservable();
        _ = SignalAsync.Return<IMqttClient>(rawClient).PublishOmronLogicalTags(OmronTopic, logicalTags, static _ => AsyncPayload, OmronFirstTag).ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient).PublishOmronLogicalTags(OmronTopic, logicalTags, OmronFirstTag).ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient).PublishOmronLogicalTags(OmronTopic, logicalTags, static _ => AsyncPayload, OmronFirstTag).ToObservable();

        await Assert.That(HasPayload(rawClient, $"{OmronFormattedValue}")).IsTrue();
        await AssertOmronGuardsAsync(rawClients, logicalTags);
    }

    /// <summary>Covers Omron bulk observer parser, write, cancellation, and lifecycle branches.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task OmronBulkObserver_HandlesErrorsCancellationAndDisposalAsync()
    {
        using var simulator = CreateOmronSimulator();
        using var logicalTags = CreateOmronLogicalTags(simulator);
        var errors = new List<Exception>();
        var observer = CreateOmronObserver(
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            errors.Add,
            CancellationToken.None);
        var attached = new TrackingDisposable();
        Attach(observer, attached);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(
            OmronTopic,
            $"{OmronObserverFirst},{OmronObserverSecond}"));
        await PendingWriteAsync(observer);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, "bad"));
        await PendingWriteAsync(observer);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, "null"));
        await PendingWriteAsync(observer);
        observer.Observer.OnError(new InvalidOperationException("source fault"));
        observer.Observer.OnCompleted();
        observer.Disposable.Dispose();

        await Assert.That(await OmronReadAsync(simulator, OmronFirstTag)).IsEqualTo(OmronObserverFirst);
        await Assert.That(await OmronReadAsync(simulator, OmronSecondTag)).IsEqualTo(OmronObserverSecond);
        await Assert.That(errors.Count).IsEqualTo(ExpectedOmronErrorCount);
        await Assert.That(attached.IsDisposed).IsTrue();
        await Assert.That(() => observer.Observer.OnError(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => observer.Observer.OnNext(null!)).Throws<ArgumentNullException>();
        var disposedBeforeAttach = CreateOmronObserver(logicalTags, static _ => [], null, CancellationToken.None);
        disposedBeforeAttach.Disposable.Dispose();
        var lateAttachment = new TrackingDisposable();
        Attach(disposedBeforeAttach, lateAttachment);
        disposedBeforeAttach.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, "ignored"));
        InvokeDispose(disposedBeforeAttach, false);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelled = CreateOmronObserver(
            logicalTags,
            static _ => [new LogicalTagValue(OmronFirstTag, OmronCancelledValue, DateTimeOffset.UnixEpoch)],
            errors.Add,
            cancellation.Token);
        cancelled.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, $"{OmronCancelledValue}"));
        await PendingWriteAsync(cancelled);
        cancelled.Disposable.Dispose();

        await Assert.That(lateAttachment.IsDisposed).IsTrue();
        await Assert.That(await OmronReadAsync(simulator, OmronFirstTag)).IsEqualTo(OmronObserverFirst);
        var attachException = Assert.Throws<TargetInvocationException>(() =>
            GetAttachMethod(disposedBeforeAttach.Instance.GetType()).Invoke(disposedBeforeAttach.Instance, [null]));
        await Assert.That(attachException.InnerException).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>Publishes and writes multiple Mitsubishi logical tags through the raw MQTT bridge.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task MitsubishiRawBulkBridge_UsesObserveManyAndWriteManyAsync()
    {
        await using var fixture = CreateMitsubishiFixture(LogicalTagAccessMode.ReadWrite);
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);
        fixture.Memory.WriteWord(MitsubishiFirstAddress, MitsubishiPublishedFirst);
        fixture.Memory.WriteWord(MitsubishiSecondAddress, MitsubishiPublishedSecond);
        using var publisher = clients.PublishMitsubishiTags(
            MitsubishiTopic,
            fixture.LogicalTags,
            static value => $"{value.TagName}:{value.Value}",
            MitsubishiFirstTag,
            MitsubishiSecondTag).Subscribe(static _ => { });
        await WaitUntilAsync(() => client.PublishedMessages.Count > 0);
        using var subscriber = clients.SubscribeMitsubishiTags(
            $"{MitsubishiTopic}/write",
            fixture.LogicalTags,
            ParseMitsubishiBulkPayload,
            static errors => throw new InvalidOperationException("Unexpected Mitsubishi bulk error.", errors),
            CancellationToken.None);
        await client.SimulateMessageReceivedAsync($"{MitsubishiTopic}/write", $"{MitsubishiWrittenFirst},{MitsubishiWrittenSecond}");
        await WaitUntilAsync(() => fixture.Memory.ReadWord(MitsubishiFirstAddress) == MitsubishiWrittenFirst
            && fixture.Memory.ReadWord(MitsubishiSecondAddress) == MitsubishiWrittenSecond);

        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(MitsubishiTopic);
        await Assert.That(client.PublishedMessages[0].ConvertPayloadToString())
            .IsEqualTo($"{MitsubishiFirstTag}:{MitsubishiPublishedFirst}");
        await Assert.That(fixture.Memory.ReadWord(MitsubishiFirstAddress)).IsEqualTo(MitsubishiWrittenFirst);
        await Assert.That(fixture.Memory.ReadWord(MitsubishiSecondAddress)).IsEqualTo(MitsubishiWrittenSecond);
    }

    /// <summary>Covers Mitsubishi default formatting, resilient wrappers, async wrappers, and validation guards.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task MitsubishiBulkWrappers_FormatValuesAndValidateArgumentsAsync()
    {
        await using var fixture = CreateMitsubishiFixture(LogicalTagAccessMode.ReadWrite);
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var rawClients = Signal.Emit<IMqttClient>(rawClient);
        var resilientClients = Signal.Emit<IResilientMqttClient>(resilientClient);
        fixture.Memory.WriteWord(MitsubishiFirstAddress, MitsubishiFormattedFirst);
        fixture.Memory.WriteWord(MitsubishiSecondAddress, MitsubishiFormattedSecond);
        using var rawPublish = rawClients.PublishMitsubishiTags(MitsubishiTopic, fixture.LogicalTags, MitsubishiFirstTag)
            .Subscribe(static _ => { });
        await WaitUntilAsync(() => rawClient.PublishedMessages.Count > 0);
        var formatted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var resilientPublish = resilientClients.PublishMitsubishiTags(
            MitsubishiTopic,
            fixture.LogicalTags,
            value =>
            {
                if (value.TagName == MitsubishiSecondTag)
                {
                    _ = formatted.TrySetResult();
                }

                return Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            },
            MitsubishiSecondTag).Subscribe(static _ => { });
        await formatted.Task.WaitAsync(Timeout);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        using var resilientSubscribe = resilientClients.SubscribeMitsubishiTags(
            $"{MitsubishiTopic}/resilient",
            fixture.LogicalTags,
            ParseMitsubishiBulkPayload,
            null,
            CancellationToken.None);
        using var rawAsyncSubscribe = SignalAsync.Return<IMqttClient>(rawClient).SubscribeMitsubishiTags(
            $"{MitsubishiTopic}/async/raw",
            fixture.LogicalTags,
            ParseMitsubishiBulkPayload,
            null,
            CancellationToken.None);
        using var resilientAsyncSubscribe = SignalAsync.Return<IResilientMqttClient>(resilientClient).SubscribeMitsubishiTags(
            $"{MitsubishiTopic}/async/resilient",
            fixture.LogicalTags,
            ParseMitsubishiBulkPayload,
            null,
            CancellationToken.None);
        _ = SignalAsync.Return<IMqttClient>(rawClient).PublishMitsubishiTags(MitsubishiTopic, fixture.LogicalTags, MitsubishiFirstTag).ToObservable();
        _ = SignalAsync.Return<IMqttClient>(rawClient).PublishMitsubishiTags(MitsubishiTopic, fixture.LogicalTags, static _ => AsyncPayload, MitsubishiFirstTag).ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient).PublishMitsubishiTags(MitsubishiTopic, fixture.LogicalTags, MitsubishiFirstTag).ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient).PublishMitsubishiTags(MitsubishiTopic, fixture.LogicalTags, static _ => AsyncPayload, MitsubishiFirstTag).ToObservable();

        await Assert.That(rawClient.PublishedMessages[0].ConvertPayloadToString()).IsEqualTo($"{MitsubishiFormattedFirst}");
        await AssertMitsubishiGuardsAsync(rawClients, fixture.LogicalTags);
    }

    /// <summary>Covers Mitsubishi bulk observer parser, write failure, cancellation, and lifecycle branches.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task MitsubishiBulkObserver_HandlesErrorsCancellationAndDisposalAsync()
    {
        await using var successFixture = CreateMitsubishiFixture(LogicalTagAccessMode.ReadWrite);
        var errors = new List<Exception>();
        var observer = CreateMitsubishiObserver(
            successFixture.LogicalTags,
            ParseMitsubishiBulkPayload,
            errors.Add,
            CancellationToken.None);
        var attached = new TrackingDisposable();
        Attach(observer, attached);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(
            MitsubishiTopic,
            $"{MitsubishiObserverFirst},{MitsubishiObserverSecond}"));
        await PendingWriteAsync(observer);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, "bad"));
        await PendingWriteAsync(observer);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, "null"));
        await PendingWriteAsync(observer);
        observer.Observer.OnError(new InvalidOperationException("source fault"));
        observer.Observer.OnCompleted();
        observer.Disposable.Dispose();

        await Assert.That(successFixture.Memory.ReadWord(MitsubishiFirstAddress)).IsEqualTo(MitsubishiObserverFirst);
        await Assert.That(successFixture.Memory.ReadWord(MitsubishiSecondAddress)).IsEqualTo(MitsubishiObserverSecond);
        await Assert.That(errors.Count).IsEqualTo(ExpectedMitsubishiInitialErrorCount);
        await Assert.That(attached.IsDisposed).IsTrue();
        await Assert.That(() => observer.Observer.OnError(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => observer.Observer.OnNext(null!)).Throws<ArgumentNullException>();
        await using var readOnlyFixture = CreateMitsubishiFixture(LogicalTagAccessMode.Read);
        var writeFailed = CreateMitsubishiObserver(readOnlyFixture.LogicalTags, ParseMitsubishiReadOnly, errors.Add, CancellationToken.None);
        writeFailed.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, $"{MitsubishiReadOnlyValue}"));
        await PendingWriteAsync(writeFailed);
        writeFailed.Disposable.Dispose();
        var disposedBeforeAttach = CreateMitsubishiObserver(successFixture.LogicalTags, static _ => [], null, CancellationToken.None);
        disposedBeforeAttach.Disposable.Dispose();
        var lateAttachment = new TrackingDisposable();
        Attach(disposedBeforeAttach, lateAttachment);
        disposedBeforeAttach.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, "ignored"));
        InvokeDispose(disposedBeforeAttach, false);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelled = CreateMitsubishiObserver(successFixture.LogicalTags, ParseMitsubishiCancelled, errors.Add, cancellation.Token);
        cancelled.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, $"{MitsubishiCancelledValue}"));
        await PendingWriteAsync(cancelled);
        cancelled.Disposable.Dispose();

        await Assert.That(errors.Count).IsGreaterThanOrEqualTo(ExpectedMitsubishiMinimumErrorCount);
        await Assert.That(lateAttachment.IsDisposed).IsTrue();
        await Assert.That(successFixture.Memory.ReadWord(MitsubishiFirstAddress)).IsEqualTo(MitsubishiObserverFirst);
        var attachException = Assert.Throws<TargetInvocationException>(() =>
            GetAttachMethod(disposedBeforeAttach.Instance.GetType()).Invoke(disposedBeforeAttach.Instance, [null]));
        await Assert.That(attachException.InnerException).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>Covers default overload and error-callback closure branches for both bulk bridges.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BulkBridgeClosureBranches_CoverDefaultSubscribeAndCallbackFailuresAsync()
    {
        await CoverOmronClosureBranchesAsync();
        await CoverMitsubishiClosureBranchesAsync();
        await Assert.That(InvokeFormatter(typeof(OmronBulkExtensions), NullStringConvertible.Instance))
            .IsEqualTo(string.Empty);
        await Assert.That(InvokeFormatter(typeof(OmronBulkExtensions), OmronClosureFirst)).IsEqualTo($"{OmronClosureFirst}");
        await Assert.That(InvokeFormatter(typeof(MitsubishiBulkExtensions), NullStringConvertible.Instance))
            .IsEqualTo(string.Empty);
        await Assert.That(InvokeFormatter(typeof(MitsubishiBulkExtensions), MitsubishiPublishedFirst))
            .IsEqualTo($"{MitsubishiPublishedFirst}");
    }

    /// <summary>Covers Omron closure branches that are not reached by core behavior tests.</summary>
    /// <returns>A task representing the coverage work.</returns>
    private static async Task CoverOmronClosureBranchesAsync()
    {
        using var simulator = CreateOmronSimulator();
        using var logicalTags = CreateOmronLogicalTags(simulator);
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        using var rawSubscribe = Signal.Emit<IMqttClient>(rawClient).SubscribeOmronLogicalTags(
            $"{OmronTopic}/default/raw",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            CancellationToken.None);
        using var asyncRawSubscribe = SignalAsync.Return<IMqttClient>(rawClient).SubscribeOmronLogicalTags(
            $"{OmronTopic}/default/async/raw",
            logicalTags,
            static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
            CancellationToken.None);
        using var asyncResilientSubscribe = SignalAsync.Return<IResilientMqttClient>(resilientClient)
            .SubscribeOmronLogicalTags(
                $"{OmronTopic}/default/async/resilient",
                logicalTags,
                static payload => ParseBulkPayload(payload, OmronFirstTag, OmronSecondTag),
                CancellationToken.None);
        using var resilientPublish = Signal.Emit<IResilientMqttClient>(resilientClient).PublishOmronLogicalTags(
            OmronTopic,
            logicalTags,
            OmronSecondTag).Subscribe(static _ => { });
        await rawClient.SimulateMessageReceivedAsync(
            $"{OmronTopic}/default/raw",
            $"{OmronClosureFirst},{OmronClosureSecond}");
        await WaitUntilAsync(() => HasOmronWrite(simulator, OmronClosureFirst)
            && HasOmronWrite(simulator, OmronClosureSecond));
        await simulator.WriteValueAsync(new(OmronSecondTag), OmronClosurePublished, CancellationToken.None);
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        var observer = CreateOmronObserver(
            logicalTags,
            static _ => throw new FormatException("omron callback failure"),
            static _ => throw new InvalidOperationException("omron callback observer failure"),
            CancellationToken.None);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, "bad"));
        await PendingWriteAsync(observer);
        observer.Disposable.Dispose();
        var noCallback = CreateOmronObserver(
            logicalTags,
            static _ => throw new FormatException("omron no callback failure"),
            null,
            CancellationToken.None);
        noCallback.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(OmronTopic, "bad"));
        await PendingWriteAsync(noCallback);
        noCallback.Disposable.Dispose();
        InvokeDefaultResilientPublishTwice(
            typeof(OmronBulkExtensions),
            nameof(OmronBulkExtensions.PublishOmronLogicalTags),
            resilientClient,
            logicalTags,
            OmronSecondTag);
        await AssertOmronResilientDefaultGuardsAsync(resilientClient, logicalTags);
    }

    /// <summary>Covers Mitsubishi closure branches that are not reached by core behavior tests.</summary>
    /// <returns>A task representing the coverage work.</returns>
    private static async Task CoverMitsubishiClosureBranchesAsync()
    {
        await using var fixture = CreateMitsubishiFixture(LogicalTagAccessMode.ReadWrite);
        using var resilientClient = new MockResilientMqttClient();
        using var resilientPublish = Signal.Emit<IResilientMqttClient>(resilientClient).PublishMitsubishiTags(
            MitsubishiTopic,
            fixture.LogicalTags,
            MitsubishiSecondTag).Subscribe(static _ => { });
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        var noCallback = CreateMitsubishiObserver(fixture.LogicalTags, ParseMitsubishiBulkPayload, null, CancellationToken.None);
        noCallback.Observer.OnError(new InvalidOperationException("source without callback"));
        InvokeDefaultResilientPublishTwice(
            typeof(MitsubishiBulkExtensions),
            nameof(MitsubishiBulkExtensions.PublishMitsubishiTags),
            resilientClient,
            fixture.LogicalTags,
            MitsubishiSecondTag);
        await AssertMitsubishiResilientDefaultGuardsAsync(resilientClient, fixture.LogicalTags);
        var observer = CreateMitsubishiObserver(
            fixture.LogicalTags,
            static _ => throw new FormatException("mitsubishi callback failure"),
            static _ => throw new InvalidOperationException("mitsubishi callback observer failure"),
            CancellationToken.None);
        observer.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(MitsubishiTopic, "bad"));
        await PendingWriteAsync(observer);
        observer.Disposable.Dispose();
    }

    /// <summary>Creates a deterministic Omron simulator with both logical tags seeded.</summary>
    /// <returns>The configured simulator.</returns>
    private static OmronPlcSimulator CreateOmronSimulator()
    {
        var simulator = new OmronPlcSimulator();
        simulator.Seed(new(OmronFirstTag, OmronFirstAddress), 0);
        simulator.Seed(new(OmronSecondTag, OmronSecondAddress), 0);
        return simulator;
    }

    /// <summary>Creates an Omron logical-tag client over the simulator.</summary>
    /// <param name="simulator">The simulator facade.</param>
    /// <returns>The registered logical-tag client.</returns>
    private static OmronLogicalTagClient CreateOmronLogicalTags(OmronPlcSimulator simulator)
    {
        var logicalTags = new OmronLogicalTagClient(simulator);
        _ = logicalTags.CreateTag<int>(new(OmronFirstTag, OmronFirstAddress));
        _ = logicalTags.CreateTag<int>(new(OmronSecondTag, OmronSecondAddress));
        return logicalTags;
    }

    /// <summary>Creates a simulator-backed Mitsubishi logical-tag fixture.</summary>
    /// <param name="accessMode">The logical tag access mode.</param>
    /// <returns>The configured fixture.</returns>
    private static MitsubishiFixture CreateMitsubishiFixture(LogicalTagAccessMode accessMode)
    {
        var memory = new MitsubishiSimulatorMemory();
        var transport = new MitsubishiSimulatorTransport(memory);
        var options = new MitsubishiClientOptions(
            "127.0.0.1",
            MitsubishiFixturePort,
            MitsubishiFrameType.ThreeE,
            CommunicationDataCode.Binary,
            MitsubishiTransportKind.Tcp);
        var owner = new MitsubishiClient(options, transport, scheduler: null);
        var logicalTags = owner.CreateLogicalTagClient(null, TimeSpan.FromHours(1), null);
        logicalTags.RegisterTag(new(
            MitsubishiFirstTag,
            MitsubishiFirstAddress,
            "UInt16",
            new LogicalTagOptions { AccessMode = accessMode, ScanInterval = TimeSpan.FromHours(1) }));
        logicalTags.RegisterTag(new(
            MitsubishiSecondTag,
            MitsubishiSecondAddress,
            "UInt16",
            new LogicalTagOptions { AccessMode = accessMode, ScanInterval = TimeSpan.FromHours(1) }));
        return new(memory, owner, logicalTags);
    }

    /// <summary>Verifies Omron resilient default-publish validation paths.</summary>
    /// <param name="client">The resilient MQTT client.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <returns>A task representing the assertions.</returns>
    private static async Task AssertOmronResilientDefaultGuardsAsync(
        IResilientMqttClient client,
        OmronLogicalTagClient logicalTags)
    {
        await Assert.That(() => Signal.Emit(client).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                (string[])null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<IResilientMqttClient>)null!).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                OmronSecondTag)).Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit(client).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags)).Throws<ArgumentException>();
        await Assert.That(() => Signal.Emit(client).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                [null!])).Throws<ArgumentException>();
    }

    /// <summary>Verifies Mitsubishi resilient default-publish validation paths.</summary>
    /// <param name="client">The resilient MQTT client.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <returns>A task representing the assertions.</returns>
    private static async Task AssertMitsubishiResilientDefaultGuardsAsync(
        IResilientMqttClient client,
        MitsubishiLogicalTagClient logicalTags)
    {
        await Assert.That(() => Signal.Emit(client).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                (string[])null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<IResilientMqttClient>)null!).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                MitsubishiSecondTag)).Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit(client).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags)).Throws<ArgumentException>();
        await Assert.That(() => Signal.Emit(client).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                [null!])).Throws<ArgumentException>();
    }

    /// <summary>Verifies Omron public argument validation paths.</summary>
    /// <param name="rawClients">The raw MQTT clients.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <returns>A task representing the assertions.</returns>
    private static async Task AssertOmronGuardsAsync(
        IObservable<IMqttClient> rawClients,
        OmronLogicalTagClient logicalTags)
    {
        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                OmronFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishOmronLogicalTags(" ", logicalTags, OmronFirstTag))
            .Throws<ArgumentException>();
        await Assert.That(() => rawClients.PublishOmronLogicalTags(OmronTopic, null!, OmronFirstTag))
            .Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                (Func<LogicalTagValue, string>)null!,
                OmronFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                static _ => string.Empty)).Throws<ArgumentException>();
        await Assert.That(() => rawClients.PublishOmronLogicalTags(OmronTopic, logicalTags, (string[])null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.SubscribeOmronLogicalTags(
                OmronTopic,
                logicalTags,
                null!,
                CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<IResilientMqttClient>)null!).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                OmronFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).SubscribeOmronLogicalTags(
                OmronTopic,
                logicalTags,
                static _ => [],
                null,
                CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IResilientMqttClient>)null!).PublishOmronLogicalTags(
                OmronTopic,
                logicalTags,
                OmronFirstTag)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies Mitsubishi public argument validation paths.</summary>
    /// <param name="rawClients">The raw MQTT clients.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <returns>A task representing the assertions.</returns>
    private static async Task AssertMitsubishiGuardsAsync(
        IObservable<IMqttClient> rawClients,
        MitsubishiLogicalTagClient logicalTags)
    {
        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                MitsubishiFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishMitsubishiTags(" ", logicalTags, MitsubishiFirstTag))
            .Throws<ArgumentException>();
        await Assert.That(() => rawClients.PublishMitsubishiTags(MitsubishiTopic, null!, MitsubishiFirstTag))
            .Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                (Func<LogicalTagValue, string>)null!,
                MitsubishiFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                static _ => string.Empty)).Throws<ArgumentException>();
        await Assert.That(() => rawClients.PublishMitsubishiTags(MitsubishiTopic, logicalTags, (string[])null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => rawClients.SubscribeMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                null!,
                null,
                CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<IResilientMqttClient>)null!).SubscribeMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                static _ => [],
                null,
                CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).PublishMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                MitsubishiFirstTag)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IResilientMqttClient>)null!).SubscribeMitsubishiTags(
                MitsubishiTopic,
                logicalTags,
                static _ => [],
                null,
                CancellationToken.None)).Throws<ArgumentNullException>();
    }

    /// <summary>Parses a comma-separated two-value payload into logical tag values.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <param name="firstName">The first logical tag name.</param>
    /// <param name="secondName">The second logical tag name.</param>
    /// <returns>The parsed logical tag values.</returns>
    private static IReadOnlyCollection<LogicalTagValue> ParseBulkPayload(string payload, string firstName, string secondName)
    {
        if (payload == "null")
        {
            return null!;
        }

        var parts = payload.Split(',', StringSplitOptions.TrimEntries);
        return
        [
            new(firstName, int.Parse(parts[0], CultureInfo.InvariantCulture), DateTimeOffset.UnixEpoch),
            new(secondName, int.Parse(parts[1], CultureInfo.InvariantCulture), DateTimeOffset.UnixEpoch),
        ];
    }

    /// <summary>Parses a Mitsubishi UInt16 comma-separated payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed logical tag values.</returns>
    private static IReadOnlyCollection<LogicalTagValue> ParseMitsubishiBulkPayload(string payload)
    {
        if (payload == "null")
        {
            return null!;
        }

        var parts = payload.Split(',', StringSplitOptions.TrimEntries);
        return
        [
            new(MitsubishiFirstTag, ushort.Parse(parts[0], CultureInfo.InvariantCulture), DateTimeOffset.UnixEpoch),
            new(MitsubishiSecondTag, ushort.Parse(parts[1], CultureInfo.InvariantCulture), DateTimeOffset.UnixEpoch),
        ];
    }

    /// <summary>Determines whether a mock client published a payload.</summary>
    /// <param name="client">The mock MQTT client.</param>
    /// <param name="expected">The expected payload.</param>
    /// <returns><see langword="true"/> when the payload exists.</returns>
    private static bool HasPayload(MockMqttClient client, string expected)
    {
        foreach (var message in client.PublishedMessages)
        {
            if (string.Equals(message.ConvertPayloadToString(), expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds a Mitsubishi read-only write request.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed logical tag value.</returns>
    private static IReadOnlyCollection<LogicalTagValue> ParseMitsubishiReadOnly(string payload)
    {
        GC.KeepAlive(payload);
        return [new(MitsubishiFirstTag, MitsubishiReadOnlyValue, DateTimeOffset.UnixEpoch)];
    }

    /// <summary>Builds a Mitsubishi cancelled write request.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed logical tag value.</returns>
    private static IReadOnlyCollection<LogicalTagValue> ParseMitsubishiCancelled(string payload)
    {
        GC.KeepAlive(payload);
        return [new(MitsubishiFirstTag, MitsubishiCancelledValue, DateTimeOffset.UnixEpoch)];
    }

    /// <summary>Reads one Omron logical tag value asynchronously.</summary>
    /// <param name="simulator">The simulator.</param>
    /// <param name="tagName">The logical tag name.</param>
    /// <returns>The current tag value.</returns>
    private static Task<int> OmronReadAsync(OmronPlcSimulator simulator, string tagName) =>
        simulator.ReadValueAsync(new LogicalTagKey<int>(tagName), CancellationToken.None);

    /// <summary>Determines whether the Omron simulator recorded a successful write.</summary>
    /// <param name="simulator">The simulator.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when a matching successful write exists.</returns>
    private static bool HasOmronWrite(OmronPlcSimulator simulator, int expected)
    {
        foreach (var operation in simulator.Operations)
        {
            if (operation.Operation == OmronSimulatorOperation.Write
                && operation.Succeeded
                && Equals(operation.Value, expected))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Invokes a default resilient publish overload twice through reflection.</summary>
    /// <param name="extensionType">The extension type that owns the overload.</param>
    /// <param name="methodName">The publish method name.</param>
    /// <param name="client">The resilient client.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="tagName">The logical tag name.</param>
    private static void InvokeDefaultResilientPublishTwice(
        Type extensionType,
        string methodName,
        IResilientMqttClient client,
        object logicalTags,
        string tagName)
    {
        var method = GetDefaultResilientPublishMethod(extensionType, methodName);
        var clients = Signal.Emit(client);
        object[] arguments = [clients, OmronTopic, logicalTags, CreateTagArray(tagName)];
        GC.KeepAlive(method.Invoke(null, arguments));
        GC.KeepAlive(method.Invoke(null, arguments));
    }

    /// <summary>Creates a tag array for reflected params arguments.</summary>
    /// <param name="tagName">The logical tag name.</param>
    /// <returns>A one-item tag array.</returns>
    private static string[] CreateTagArray(string tagName) => [tagName];

    /// <summary>Gets a reflected default resilient publish overload.</summary>
    /// <param name="extensionType">The extension type that owns the overload.</param>
    /// <param name="methodName">The publish method name.</param>
    /// <returns>The default resilient publish method.</returns>
    private static MethodInfo GetDefaultResilientPublishMethod(Type extensionType, string methodName)
    {
        foreach (var method in extensionType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            var parameters = method.GetParameters();
            if (method.Name == methodName
                && parameters.Length == 4
                && parameters[0].ParameterType == typeof(IObservable<IResilientMqttClient>)
                && parameters[1].ParameterType == typeof(string)
                && parameters[3].ParameterType == typeof(string[]))
            {
                return method;
            }
        }

        throw new MissingMethodException(extensionType.FullName, methodName);
    }

    /// <summary>Invokes a private logical-tag formatter with a null logical value.</summary>
    /// <param name="extensionType">The extension type that owns the formatter.</param>
    /// <param name="value">The logical value to format.</param>
    /// <returns>The formatter result.</returns>
    private static string InvokeFormatter(Type extensionType, object? value)
    {
        var method = extensionType.GetMethod("FormatLogicalTagValue", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(extensionType.FullName, "FormatLogicalTagValue");
        return (string)(method.Invoke(null, [new LogicalTagValue("Formatter.Value", value, DateTimeOffset.UnixEpoch)])
            ?? throw new InvalidOperationException("The formatter returned a null string reference."));
    }

    /// <summary>Creates the private Omron bulk observer through reflection.</summary>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <param name="parser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>The reflected observer surface.</returns>
    private static ReflectedObserver CreateOmronObserver(
        OmronLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> parser,
        Action<Exception>? onError,
        CancellationToken cancellationToken) =>
        CreateBulkObserver(
            typeof(OmronBulkExtensions),
            "OmronLogicalTagBulkWriteObserver",
            logicalTags,
            parser,
            onError,
            cancellationToken);

    /// <summary>Creates the private Mitsubishi bulk observer through reflection.</summary>
    /// <param name="logicalTags">The logical tag client.</param>
    /// <param name="parser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>The reflected observer surface.</returns>
    private static ReflectedObserver CreateMitsubishiObserver(
        MitsubishiLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> parser,
        Action<Exception>? onError,
        CancellationToken cancellationToken) =>
        CreateBulkObserver(
            typeof(MitsubishiBulkExtensions),
            "MitsubishiBulkWriteObserver",
            logicalTags,
            parser,
            onError,
            cancellationToken);

    /// <summary>Creates one reflected private bulk observer.</summary>
    /// <param name="extensionType">The extension type that owns the nested observer.</param>
    /// <param name="nestedName">The nested observer name.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="parser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>The reflected observer surface.</returns>
    private static ReflectedObserver CreateBulkObserver(
        Type extensionType,
        string nestedName,
        object logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> parser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
    {
        var observerType = extensionType.GetNestedType(nestedName, BindingFlags.NonPublic)
            ?? throw new MissingMemberException(extensionType.FullName, nestedName);
        var instance = Activator.CreateInstance(
            observerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [logicalTags, parser, onError, cancellationToken],
            culture: null)
            ?? throw new InvalidOperationException("The bulk write observer could not be created.");
        return new(instance, (IObserver<MqttApplicationMessageReceivedEventArgs>)instance, (IDisposable)instance);
    }

    /// <summary>Invokes the private Attach method on a reflected observer.</summary>
    /// <param name="observer">The reflected observer.</param>
    /// <param name="subscription">The subscription to attach.</param>
    private static void Attach(ReflectedObserver observer, IDisposable subscription) =>
        _ = GetAttachMethod(observer.Instance.GetType()).Invoke(observer.Instance, [subscription]);

    /// <summary>Gets the reflected Attach method.</summary>
    /// <param name="observerType">The observer type.</param>
    /// <returns>The Attach method.</returns>
    private static MethodInfo GetAttachMethod(Type observerType) =>
        observerType.GetMethod(nameof(Attach), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(observerType.FullName, nameof(Attach));

    /// <summary>Invokes the private Dispose overload.</summary>
    /// <param name="observer">The reflected observer.</param>
    /// <param name="disposing">Whether managed resources are being disposed.</param>
    private static void InvokeDispose(ReflectedObserver observer, bool disposing)
    {
        var method = observer.Instance.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            return;
        }

        _ = method.Invoke(observer.Instance, [disposing]);
    }

    /// <summary>Awaits the internal serialized write tail.</summary>
    /// <param name="observer">The reflected observer.</param>
    /// <returns>The pending write task.</returns>
    private static Task PendingWriteAsync(ReflectedObserver observer)
    {
        var field = observer.Instance.GetType().GetField("_pendingWrite", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(observer.Instance.GetType().FullName, "_pendingWrite");
        return field.GetValue(observer.Instance) as Task
            ?? throw new InvalidOperationException("The observer has no pending write task.");
    }

    /// <summary>Waits until a condition becomes true.</summary>
    /// <param name="condition">The awaited condition.</param>
    /// <returns>A task representing the wait.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        using var cancellation = new CancellationTokenSource(Timeout);
        while (!condition())
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    /// <summary>Provides a convertible value whose string representation is null.</summary>
    private sealed class NullStringConvertible : IConvertible
    {
        /// <summary>The shared convertible instance.</summary>
        internal static readonly NullStringConvertible Instance = new();

        /// <inheritdoc/>
        public TypeCode GetTypeCode() => TypeCode.Object;

        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public byte ToByte(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public char ToChar(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public double ToDouble(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public short ToInt16(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public int ToInt32(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public long ToInt64(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public float ToSingle(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public string ToString(IFormatProvider? provider) => null!;

        /// <inheritdoc/>
        public object ToType(Type conversionType, IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();

        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
    }

    /// <summary>Records deterministic subscription disposal.</summary>
    private sealed class TrackingDisposable : IDisposable
    {
        /// <summary>Gets a value indicating whether disposal occurred.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;
    }

    /// <summary>Stores a reflected observer surface.</summary>
    /// <param name="Instance">The private observer instance.</param>
    /// <param name="Observer">The public observer interface.</param>
    /// <param name="Disposable">The disposable interface.</param>
    private sealed record ReflectedObserver(
        object Instance,
        IObserver<MqttApplicationMessageReceivedEventArgs> Observer,
        IDisposable Disposable);

    /// <summary>Owns Mitsubishi simulator logical-tag resources.</summary>
    /// <param name="Memory">The simulator memory.</param>
    /// <param name="Owner">The Mitsubishi owner client.</param>
    /// <param name="LogicalTags">The logical-tag client.</param>
    private sealed record MitsubishiFixture(
        MitsubishiSimulatorMemory Memory,
        MitsubishiClient Owner,
        MitsubishiLogicalTagClient LogicalTags) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            LogicalTags.Dispose();
            await Owner.DisposeAsync();
        }
    }
}
