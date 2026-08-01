// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using IoT.Driver.Core;
using IoT.Driver.MitsubishiRx;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.Mitsubishi;
using ReactiveUI.Primitives.Async;
using MitsubishiClient = IoT.Driver.MitsubishiRx.MitsubishiRx;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Completes guard and lifecycle branch coverage for the Mitsubishi MQTT bridge.</summary>
public class MitsubishiBridgeBranchTests
{
    /// <summary>The value used when attachment occurs after disposal.</summary>
    private const ushort LateAttachmentValue = 2;

    /// <summary>The successful queued write value.</summary>
    private const ushort SuccessfulWriteValue = 91;

    /// <summary>The failed queued write value.</summary>
    private const ushort FailedWriteValue = 92;

    /// <summary>The cancelled queued write value.</summary>
    private const ushort CancelledWriteValue = 93;

    /// <summary>The simulator word address used by the logical tag.</summary>
    private const string Address = "D200";

    /// <summary>The logical tag name used by the branch tests.</summary>
    private const string TagName = "Branch.Value";

    /// <summary>The valid MQTT topic used by guard tests.</summary>
    private const string Topic = "tests/mitsubishi/branches";

    /// <summary>Gets the closed internal observer type from the production assembly.</summary>
    private static readonly Type InternalObserverType =
        (typeof(MitsubishiMqttExtensions).Assembly
            .GetType("MQTTnet.Rx.Mitsubishi.MitsubishiTagWriteObserver`1", throwOnError: true)
            ?? throw new TypeLoadException("The Mitsubishi MQTT write observer type is unavailable."))
            .MakeGenericType(typeof(ushort));

    /// <summary>Verifies every synchronous publish argument guard.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublicSyncPublish_RejectsInvalidArgumentsAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        await using var fixture = CreateFixture(broker.Port, LogicalTagAccessMode.ReadWrite);
        IObservable<IMqttClient> syncClient = broker.Bridge;
        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            static value => value.ToString())).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.PublishMitsubishiTag(
            " ",
            fixture.Tag,
            fixture.LogicalTags,
            static value => value.ToString())).Throws<ArgumentException>();
        await Assert.That(() => syncClient.PublishMitsubishiTag<ushort>(
            Topic,
            null!,
            fixture.LogicalTags,
            static value => value.ToString())).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.PublishMitsubishiTag(
            Topic,
            fixture.Tag,
            null!,
            static value => value.ToString())).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.PublishMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies every synchronous subscribe argument guard.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublicSyncSubscribe_RejectsInvalidArgumentsAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        await using var fixture = CreateFixture(broker.Port, LogicalTagAccessMode.ReadWrite);
        IObservable<IMqttClient> syncClient = broker.Bridge;

        await Assert.That(() => ((IObservable<IMqttClient>)null!).SubscribeMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            static _ => (ushort)1,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.SubscribeMitsubishiTag(
            " ",
            fixture.Tag,
            fixture.LogicalTags,
            static _ => (ushort)1,
            null,
            CancellationToken.None)).Throws<ArgumentException>();
        await Assert.That(() => syncClient.SubscribeMitsubishiTag(
            Topic,
            null!,
            fixture.LogicalTags,
            static _ => (ushort)1,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.SubscribeMitsubishiTag(
            Topic,
            fixture.Tag,
            null!,
            static _ => (ushort)1,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => syncClient.SubscribeMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            null!,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies every asynchronous public argument guard.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublicAsyncBridge_RejectsInvalidArgumentsAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        await using var fixture = CreateFixture(broker.Port, LogicalTagAccessMode.ReadWrite);
        IObservableAsync<IMqttClient> asyncClient = broker.Bridge.ToSignal();

        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).PublishMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            static value => value.ToString())).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).SubscribeMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            static _ => (ushort)1,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncClient.PublishMitsubishiTag<ushort>(
            Topic,
            null!,
            fixture.LogicalTags,
            static value => value.ToString())).Throws<ArgumentNullException>();
        await Assert.That(() => asyncClient.SubscribeMitsubishiTag(
            Topic,
            fixture.Tag,
            fixture.LogicalTags,
            null!,
            null,
            CancellationToken.None)).Throws<ArgumentNullException>();
    }

    /// <summary>Exercises observer completion, source errors, attachment, repeated disposal, and null guards.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InternalObserver_LifecycleAndSourceErrorBranchesAreDeterministicAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        await using var fixture = CreateFixture(broker.Port, LogicalTagAccessMode.ReadWrite);
        var observedError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = CreateObserver(
            fixture,
            static _ => (ushort)1,
            error => _ = observedError.TrySetResult(error),
            CancellationToken.None);
        var attached = new TrackingDisposable();

        Attach(observer, attached);
        observer.Observer.OnError(new InvalidOperationException("source fault"));
        var sourceError = await observedError.Task;
        observer.Disposable.Dispose();
        observer.Observer.OnCompleted();

        await Assert.That(sourceError.Message).IsEqualTo("source fault");
        await Assert.That(attached.IsDisposed).IsTrue();
        await Assert.That(() => observer.Observer.OnError(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => observer.Observer.OnNext(null!)).Throws<ArgumentNullException>();

        var disposedBeforeAttach = CreateObserver(
            fixture,
            static _ => LateAttachmentValue,
            null,
            CancellationToken.None);
        disposedBeforeAttach.Disposable.Dispose();
        var lateAttachment = new TrackingDisposable();
        Attach(disposedBeforeAttach, lateAttachment);
        disposedBeforeAttach.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(Topic, "2"));
        disposedBeforeAttach.Observer.OnError(new InvalidOperationException("ignored callback"));

        await Assert.That(lateAttachment.IsDisposed).IsTrue();
        var attachMethod = GetAttachMethod();
        var attachException = Assert.Throws<TargetInvocationException>(
            () => attachMethod.Invoke(disposedBeforeAttach.Instance, [null]));
        await Assert.That(attachException.InnerException).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>Exercises queued writes without an error callback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InternalObserver_QueuedWriteBranchesCompleteWithoutCallbackAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        await using var successFixture = CreateFixture(broker.Port, LogicalTagAccessMode.ReadWrite);
        var success = CreateObserver(
            successFixture,
            static _ => SuccessfulWriteValue,
            null,
            CancellationToken.None);
        success.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(Topic, "91"));
        await PendingWriteAsync(success);

        await Assert.That(successFixture.Memory.ReadWord(Address)).IsEqualTo(SuccessfulWriteValue);
        success.Disposable.Dispose();

        await using var failureFixture = CreateFixture(broker.Port, LogicalTagAccessMode.Read);
        var failedWrite = CreateObserver(
            failureFixture,
            static _ => FailedWriteValue,
            null,
            CancellationToken.None);
        failedWrite.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(Topic, "92"));
        await PendingWriteAsync(failedWrite);

        await Assert.That(failureFixture.Memory.ReadWord(Address)).IsEqualTo((ushort)0);
        failedWrite.Disposable.Dispose();

        var parserFault = CreateObserver(
            successFixture,
            static _ => throw new FormatException("parser fault"),
            null,
            CancellationToken.None);
        parserFault.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(Topic, "bad"));
        await PendingWriteAsync(parserFault);
        parserFault.Disposable.Dispose();

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelled = CreateObserver(
            successFixture,
            static _ => CancelledWriteValue,
            null,
            cancellation.Token);
        cancelled.Observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(Topic, "93"));
        await PendingWriteAsync(cancelled);
        cancelled.Disposable.Dispose();

        await Assert.That(successFixture.Memory.ReadWord(Address)).IsEqualTo(SuccessfulWriteValue);
    }

    /// <summary>Creates an internal production observer through its non-public constructor.</summary>
    /// <param name="fixture">The simulator-backed logical tag fixture.</param>
    /// <param name="parser">The MQTT payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>The reflected observer surface.</returns>
    private static ReflectedObserver CreateObserver(
        MitsubishiFixture fixture,
        Func<string, ushort> parser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
    {
        var instance = Activator.CreateInstance(
            InternalObserverType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [fixture.Tag, fixture.LogicalTags, parser, onError, cancellationToken],
            culture: null)
            ?? throw new InvalidOperationException("The Mitsubishi write observer could not be created.");
        return new(
            instance,
            (IObserver<MqttApplicationMessageReceivedEventArgs>)instance,
            (IDisposable)instance);
    }

    /// <summary>Invokes the observer's internal attachment surface.</summary>
    /// <param name="observer">The reflected observer.</param>
    /// <param name="subscription">The subscription to attach.</param>
    private static void Attach(ReflectedObserver observer, IDisposable subscription) =>
        _ = GetAttachMethod().Invoke(observer.Instance, [subscription]);

    /// <summary>Awaits the internal serialized write tail.</summary>
    /// <param name="observer">The reflected observer.</param>
    /// <returns>The pending write task.</returns>
    private static Task PendingWriteAsync(ReflectedObserver observer)
    {
        var pendingWriteField = InternalObserverType.GetField(
            "_pendingWrite",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(InternalObserverType.FullName, "_pendingWrite");
        return pendingWriteField.GetValue(observer.Instance) as Task
            ?? throw new InvalidOperationException("The Mitsubishi write observer has no pending write task.");
    }

    /// <summary>Gets the internal observer attachment method.</summary>
    /// <returns>The reflected attachment method.</returns>
    private static MethodInfo GetAttachMethod() =>
        InternalObserverType.GetMethod(nameof(Attach), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(InternalObserverType.FullName, nameof(Attach));

    /// <summary>Creates a simulator-backed logical tag fixture.</summary>
    /// <param name="port">An ephemeral port value used only for simulator options.</param>
    /// <param name="accessMode">The logical tag access mode.</param>
    /// <returns>The configured fixture.</returns>
    private static MitsubishiFixture CreateFixture(int port, LogicalTagAccessMode accessMode)
    {
        var memory = new MitsubishiSimulatorMemory();
        var transport = new MitsubishiSimulatorTransport(memory);
        var options = new MitsubishiClientOptions(
            "127.0.0.1",
            port,
            MitsubishiFrameType.ThreeE,
            CommunicationDataCode.Binary,
            MitsubishiTransportKind.Tcp);
        var owner = new MitsubishiClient(options, transport, scheduler: null);
        var logicalTags = owner.CreateLogicalTagClient(null, TimeSpan.FromHours(1), null);
        var tag = new LogicalTagKey<ushort>(TagName);
        logicalTags.RegisterTag(new(
            TagName,
            Address,
            "UInt16",
            new LogicalTagOptions
            {
                AccessMode = accessMode,
                ScanInterval = TimeSpan.FromHours(1),
            }));
        return new(memory, owner, logicalTags, tag);
    }

    /// <summary>Tracks deterministic subscription disposal.</summary>
    private sealed class TrackingDisposable : IDisposable
    {
        /// <summary>Gets a value indicating whether disposal occurred.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;
    }

    /// <summary>Stores the reflected observer interfaces.</summary>
    /// <param name="Instance">The internal production observer instance.</param>
    /// <param name="Observer">The public observer interface.</param>
    /// <param name="Disposable">The public disposable interface.</param>
    private sealed record ReflectedObserver(
        object Instance,
        IObserver<MqttApplicationMessageReceivedEventArgs> Observer,
        IDisposable Disposable);

    /// <summary>Owns the simulator-backed Mitsubishi logical-tag resources.</summary>
    /// <param name="Memory">The simulator memory.</param>
    /// <param name="Owner">The Mitsubishi client.</param>
    /// <param name="LogicalTags">The logical tag client.</param>
    /// <param name="Tag">The typed logical tag.</param>
    private sealed record MitsubishiFixture(
        MitsubishiSimulatorMemory Memory,
        MitsubishiClient Owner,
        MitsubishiLogicalTagClient LogicalTags,
        LogicalTagKey<ushort> Tag) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            LogicalTags.Dispose();
            await Owner.DisposeAsync();
        }
    }
}
