// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.S7PlcRx.Reactive;
using IoT.Driver.S7PlcRx.Reactive.LogicalTags;
using ReactiveUI.Primitives.Reactive;
using Bridge = MQTTnet.Rx.S7Plc.Reactive.ApplicationMessageBridgeExtensions;
using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.S7PlcRx;
using IoT.Driver.S7PlcRx.LogicalTags;
using ReactiveUI.Primitives;
using Bridge = MQTTnet.Rx.S7Plc.ApplicationMessageBridgeExtensions;
using ObservableSignalConversion = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies complete-message publishing from native S7 PLC observations.</summary>
public sealed class S7ApplicationMessageBridgeExtensionsTests
{
    /// <summary>The PLC value emitted by the native observation.</summary>
    private const int ObservedValue = 42;

    /// <summary>The native observation failure message.</summary>
    private const string SourceFailureMessage = "S7 source failed";

    /// <summary>The logical S7 tag used by the tests.</summary>
    private const string TagName = "Bridge.Value";

    /// <summary>The timeout for an observable result.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The binary payload preserved in each complete MQTT message.</summary>
    private static readonly byte[] Payload = [0, byte.MaxValue, ObservedValue];

    /// <summary>The correlation data preserved in each complete MQTT message.</summary>
    private static readonly byte[] CorrelationData = [0, 1];

    /// <summary>Preserves the factory-created message for raw and resilient clients in both stream forms.</summary>
    /// <param name="resilient">Whether the client uses resilient publishing.</param>
    /// <param name="asynchronous">Whether the client stream is asynchronous.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PublishS7PlcTag_PreservesCompleteMessageAsync(bool resilient, bool asynchronous)
    {
        var tag = new LogicalTagKey<int>(TagName);
        var plc = Substitute.For<IRxS7>();
        _ = plc.Observe(tag).Returns(Signal.Emit(ObservedValue));
        var expected = CreateMessage();
        MqttApplicationMessage? enqueued = null;
        MqttApplicationMessage? published = null;
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        using var rawClient = new ScriptedMqttClient
        {
            PublishHandler = (message, _) =>
            {
                published = message;
                return Task.FromResult(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
            },
        };
        using var resilientClient = Substitute.For<IResilientMqttClient>();
        _ = resilientClient.ApplicationMessageProcessed.Returns(processed);
        _ = resilientClient.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            enqueued = call.Arg<MqttApplicationMessage>();
            processed.OnNext(new(new() { ApplicationMessage = enqueued }, null));
            return Task.CompletedTask;
        });
        var rawClients = Signal.Emit<IMqttClient>(rawClient);
        var resilientClients = Signal.Emit(resilientClient);
        int? producedValue = null;
        Func<int, MqttApplicationMessage> factory = new(value =>
        {
            producedValue = value;
            return expected;
        });
        var operation = CreateOperation(rawClients, resilientClients, resilient, asynchronous, tag, plc, factory);

        var result = await operation.FirstAsync(OperationTimeout);

        if (resilient)
        {
            await Assert.That(ReferenceEquals(enqueued, expected)).IsTrue();
            var processedResult = (ApplicationMessageProcessedEventArgs)result;
            await Assert.That(ReferenceEquals(processedResult.ApplicationMessage.ApplicationMessage, expected)).IsTrue();
        }
        else
        {
            await Assert.That(ReferenceEquals(published, expected)).IsTrue();
        }

        await Assert.That(producedValue).IsEqualTo(ObservedValue);
    }

    /// <summary>Rejects a missing dependency through each of the four overloads.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublishS7PlcTag_ValidatesEveryOverloadAsync()
    {
        var tag = new LogicalTagKey<int>(TagName);
        var plc = Substitute.For<IRxS7>();
        var raw = Signal.Empty<IMqttClient>();
        var resilient = Signal.Empty<IResilientMqttClient>();
        var asyncRaw = ObservableSignalConversion.ToSignal(raw);
        var asyncResilient = ObservableSignalConversion.ToSignal(resilient);
        Func<int, MqttApplicationMessage> factory = static _ => CreateMessage();
        IObservable<IMqttClient> nullRaw = null!;
        IObservable<IResilientMqttClient> nullResilient = null!;
        IObservableAsync<IMqttClient> nullAsyncRaw = null!;
        IObservableAsync<IResilientMqttClient> nullAsyncResilient = null!;
        LogicalTagKey<int> nullTag = null!;

        await Assert.That(() => Bridge.PublishS7PlcTag(nullRaw, tag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(raw, nullTag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(raw, tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(raw, tag, plc, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(nullResilient, tag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(resilient, nullTag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(resilient, tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(resilient, tag, plc, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(nullAsyncRaw, tag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncRaw, nullTag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncRaw, tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncRaw, tag, plc, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(nullAsyncResilient, tag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncResilient, nullTag, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncResilient, tag, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishS7PlcTag(asyncResilient, tag, plc, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Forwards factory and native observation errors from each overload.</summary>
    /// <param name="resilient">Whether the client uses resilient publishing.</param>
    /// <param name="asynchronous">Whether the client stream is asynchronous.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task PublishS7PlcTag_PropagatesFactoryAndSourceFailuresAsync(bool resilient, bool asynchronous)
    {
        var tag = new LogicalTagKey<int>(TagName);
        var failure = new InvalidOperationException(SourceFailureMessage);
        var failedSource = new TestSignal<int>();
        var plc = Substitute.For<IRxS7>();
        _ = plc.Observe(tag).Returns(failedSource);
        using var rawClient = new ScriptedMqttClient();
        using var resilientClient = Substitute.For<IResilientMqttClient>();
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        _ = resilientClient.ApplicationMessageProcessed.Returns(processed);
        _ = resilientClient.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(Task.CompletedTask);
        var rawClients = Signal.Emit<IMqttClient>(rawClient);
        var resilientClients = Signal.Emit(resilientClient);

        var sourceOperation = CreateOperation(rawClients, resilientClients, resilient, asynchronous, tag, plc, static _ => CreateMessage());
        var sourceResult = sourceOperation.FirstAsync(OperationTimeout);
        failedSource.OnError(failure);
        await Assert.That(async () => await sourceResult).Throws<InvalidOperationException>();

        _ = plc.Observe(tag).Returns(Signal.Emit(ObservedValue));
        var factoryOperation = CreateOperation(rawClients, resilientClients, resilient, asynchronous, tag, plc, _ => throw failure);
        await Assert.That(async () => await factoryOperation.FirstAsync(OperationTimeout)).Throws<InvalidOperationException>();
    }

    /// <summary>Creates the requested bridge overload and converts its values for shared assertions.</summary>
    /// <param name="rawClients">The raw MQTT client source.</param>
    /// <param name="resilientClients">The resilient MQTT client source.</param>
    /// <param name="resilient">Whether to publish through a resilient client.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <param name="tag">The logical S7 tag.</param>
    /// <param name="plc">The native S7 driver.</param>
    /// <param name="factory">The complete-message factory.</param>
    /// <returns>The bridge results as an ordinary observable.</returns>
    private static IObservable<object> CreateOperation(
        IObservable<IMqttClient> rawClients,
        IObservable<IResilientMqttClient> resilientClients,
        bool resilient,
        bool asynchronous,
        LogicalTagKey<int> tag,
        IRxS7 plc,
        Func<int, MqttApplicationMessage> factory)
    {
        if (resilient && asynchronous)
        {
            return Bridge.PublishS7PlcTag(ObservableSignalConversion.ToSignal(resilientClients), tag, plc, factory)
                .ToObservable().Select(static result => (object)result);
        }

        if (resilient)
        {
            return Bridge.PublishS7PlcTag(resilientClients, tag, plc, factory)
                .Select(static result => (object)result);
        }

        return asynchronous
            ? Bridge.PublishS7PlcTag(ObservableSignalConversion.ToSignal(rawClients), tag, plc, factory)
                .ToObservable().Select(static result => (object)result)
            : Bridge.PublishS7PlcTag(rawClients, tag, plc, factory)
                .Select(static result => (object)result);
    }

    /// <summary>Creates an MQTT message with payload and MQTT 5 metadata.</summary>
    /// <returns>The complete application message.</returns>
    private static MqttApplicationMessage CreateMessage() => new MqttApplicationMessageBuilder()
        .WithTopic("plc/s7/complete")
        .WithPayload(Payload)
        .WithContentType("application/octet-stream")
        .WithResponseTopic("plc/s7/reply")
        .WithCorrelationData(CorrelationData)
        .Build();
}
