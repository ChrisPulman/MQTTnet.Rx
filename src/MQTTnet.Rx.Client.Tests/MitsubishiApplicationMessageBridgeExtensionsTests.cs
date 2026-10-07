// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.MitsubishiRx.Reactive;
using MQTTnet.Rx.Mitsubishi.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.MitsubishiRx;
using MQTTnet.Rx.Mitsubishi;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies complete-message publishing from Mitsubishi logical tags.</summary>
public sealed class MitsubishiApplicationMessageBridgeExtensionsTests
{
    /// <summary>The simulator word used for the logical tag.</summary>
    private const string Address = "D200";

    /// <summary>The logical tag registered with the simulator.</summary>
    private const string TagName = "ApplicationMessage.Value";

    /// <summary>The test message topic.</summary>
    private const string Topic = "tests/mitsubishi/application-message";

    /// <summary>The value seeded in simulator memory.</summary>
    private const ushort ObservedValue = 321;

    /// <summary>The packet identifier for the scripted publish result.</summary>
    private const int PacketIdentifier = 0;

    /// <summary>The simulator metadata port.</summary>
    private const int SimulatorPort = 851;

    /// <summary>The maximum time to wait for a publication.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The binary payload attached to the expected message.</summary>
    private static readonly byte[] Payload = "plc"u8.ToArray();

    /// <summary>Verifies all raw and resilient publishing overloads forward the factory message.</summary>
    /// <param name="resilient">Whether to enqueue through the resilient client.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PublishMitsubishiTag_PreservesMessageIdentityAsync(bool resilient, bool asynchronous)
    {
        await using var fixture = CreateFixture();
        fixture.Memory.WriteWord(Address, ObservedValue);
        var expected = CreateMessage();

        if (resilient)
        {
            using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
            using var client = Substitute.For<IResilientMqttClient>();
            MqttApplicationMessage? enqueued = null;
            _ = client.ApplicationMessageProcessed.Returns(processed);
            _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
            {
                enqueued = call.Arg<MqttApplicationMessage>();
                processed.OnNext(new(
                    new ResilientMqttApplicationMessage { ApplicationMessage = enqueued },
                    null));
                return Task.CompletedTask;
            });

            var operation = asynchronous
                ? SignalAsync.Emit(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, _ => expected).ToObservable()
                : Signal.Emit(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, _ => expected);
            _ = await operation.FirstAsync(OperationTimeout);
            await Assert.That(ReferenceEquals(enqueued, expected)).IsTrue();
        }
        else
        {
            using var client = new ScriptedMqttClient();
            MqttApplicationMessage? published = null;
            client.PublishHandler = (message, _) =>
            {
                published = message;
                return Task.FromResult(new MqttClientPublishResult(
                    PacketIdentifier,
                    MqttClientPublishReasonCode.Success,
                    string.Empty,
                    []));
            };
            var operation = asynchronous
                ? SignalAsync.Emit<IMqttClient>(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, _ => expected).ToObservable()
                : Signal.Emit<IMqttClient>(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, _ => expected);
            _ = await operation.FirstAsync(OperationTimeout);
            await Assert.That(ReferenceEquals(published, expected)).IsTrue();
        }
    }

    /// <summary>Verifies all four overloads validate their source and dependencies.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task PublishMitsubishiTag_ValidatesDependenciesAsync()
    {
        await using var fixture = CreateFixture();
        Func<ushort, MqttApplicationMessage> factory = static _ => new() { Topic = Topic };
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();
        var asyncRaw = SignalAsync.None<IMqttClient>();
        var asyncResilient = SignalAsync.None<IResilientMqttClient>();

        await Assert.That(() => ((IObservable<IMqttClient>)null!).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishMitsubishiTag(null!, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishMitsubishiTag(fixture.Tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => raw.PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, null!)).Throws<ArgumentNullException>();

        await Assert.That(() => ((IObservable<IResilientMqttClient>)null!).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishMitsubishiTag(null!, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishMitsubishiTag(fixture.Tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, null!)).Throws<ArgumentNullException>();

        await Assert.That(() => ((IObservableAsync<IMqttClient>)null!).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.PublishMitsubishiTag(null!, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.PublishMitsubishiTag(fixture.Tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, null!)).Throws<ArgumentNullException>();

        await Assert.That(() => ((IObservableAsync<IResilientMqttClient>)null!).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.PublishMitsubishiTag(null!, fixture.LogicalTags, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.PublishMitsubishiTag(fixture.Tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies a factory exception propagates through each overload.</summary>
    /// <param name="resilient">Whether to enqueue through the resilient client.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PublishMitsubishiTag_PropagatesFactoryFailureAsync(bool resilient, bool asynchronous)
    {
        await using var fixture = CreateFixture();
        fixture.Memory.WriteWord(Address, ObservedValue);
        var failure = new InvalidOperationException("message factory failed");
        Func<ushort, MqttApplicationMessage> factory = _ => throw failure;

        if (resilient)
        {
            using var client = Substitute.For<IResilientMqttClient>();
            _ = client.ApplicationMessageProcessed.Returns(Signal.Empty<ApplicationMessageProcessedEventArgs>());
            _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(Task.CompletedTask);
            var operation = asynchronous
                ? SignalAsync.Emit(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory).ToObservable()
                : Signal.Emit(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory);
            await Assert.That(async () => await operation.FirstAsync(OperationTimeout)).Throws<InvalidOperationException>();
        }
        else
        {
            using var client = new ScriptedMqttClient();
            var operation = asynchronous
                ? SignalAsync.Emit<IMqttClient>(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory).ToObservable()
                : Signal.Emit<IMqttClient>(client).PublishMitsubishiTag(fixture.Tag, fixture.LogicalTags, factory);
            await Assert.That(async () => await operation.FirstAsync(OperationTimeout)).Throws<InvalidOperationException>();
        }
    }

    /// <summary>Creates simulator memory, transport, owner, and a registered unsigned word tag.</summary>
    /// <returns>The owned simulator fixture.</returns>
    private static MitsubishiFixture CreateFixture()
    {
        var memory = new MitsubishiSimulatorMemory();
        var transport = new MitsubishiSimulatorTransport(memory);
        var options = new MitsubishiClientOptions(
            "127.0.0.1",
            SimulatorPort,
            MitsubishiFrameType.ThreeE,
            CommunicationDataCode.Binary,
            MitsubishiTransportKind.Tcp);
        var owner = new MitsubishiRx(options, transport, scheduler: null);
        var logicalTags = owner.CreateLogicalTagClient(null, TimeSpan.FromHours(1), null);
        var tag = new LogicalTagKey<ushort>(TagName);
        logicalTags.RegisterTag(new(
            TagName,
            Address,
            "UInt16",
            new LogicalTagOptions { AccessMode = LogicalTagAccessMode.ReadWrite, ScanInterval = TimeSpan.FromHours(1) }));
        return new(memory, owner, logicalTags, tag);
    }

    /// <summary>Creates the complete MQTT message forwarded by the publisher.</summary>
    /// <returns>The expected application message.</returns>
    private static MqttApplicationMessage CreateMessage() => new MqttApplicationMessageBuilder()
        .WithTopic(Topic)
        .WithPayload(Payload)
        .Build();

    /// <summary>Owns the simulator resources used by one test.</summary>
    /// <param name="Memory">The simulator memory.</param>
    /// <param name="Owner">The Mitsubishi driver.</param>
    /// <param name="LogicalTags">The registered logical-tag client.</param>
    /// <param name="Tag">The typed logical tag.</param>
    private sealed record MitsubishiFixture(
        MitsubishiSimulatorMemory Memory,
        MitsubishiRx Owner,
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
