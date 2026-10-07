// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using IoT.Driver.OmronPlcRx.Reactive;
using IoT.Driver.OmronPlcRx.Reactive.Tags;
using Bridge = MQTTnet.Rx.OmronPlc.Reactive.ApplicationMessageBridgeExtensions;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using IoT.Driver.OmronPlcRx;
using IoT.Driver.OmronPlcRx.Tags;
using Bridge = MQTTnet.Rx.OmronPlc.ApplicationMessageBridgeExtensions;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies complete application-message publishing from native Omron observations.</summary>
public sealed class OmronApplicationMessageBridgeExtensionsTests
{
    /// <summary>The value supplied by the native simulator.</summary>
    private const int ObservedValue = 42;

    /// <summary>The application-message expiry in seconds.</summary>
    private const uint MessageExpiry = 17;

    /// <summary>The bounded wait for a published value.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The binary payload to preserve.</summary>
    private static readonly byte[] Payload = [0, byte.MaxValue, ObservedValue];

    /// <summary>The binary correlation data to preserve.</summary>
    private static readonly byte[] CorrelationData = [0, 1];

    /// <summary>Preserves binary payloads and MQTT delivery and metadata settings.</summary>
    /// <param name="asynchronous">Whether to use the asynchronous-observable bridge.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task PublishOmronPlcTag_PreservesCompleteMessageAsync(bool asynchronous)
    {
        using var plc = new OmronPlcSimulator();
        var tag = new LogicalTagKey<int>("Binary");
        plc.Seed(new(tag.Name, "D100"), ObservedValue);
        using var mqtt = new ScriptedMqttClient();
        var expected = new MqttApplicationMessageBuilder()
            .WithTopic("plc/binary")
            .WithPayload(Payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(false)
            .WithContentType("application/octet-stream")
            .WithResponseTopic("plc/replies")
            .WithCorrelationData(CorrelationData)
            .WithMessageExpiryInterval(MessageExpiry)
            .WithUserProperty("driver", "omron"u8.ToArray().AsMemory())
            .Build();
        MqttApplicationMessage? actual = null;
        mqtt.PublishHandler = (message, _) =>
        {
            actual = message;
            return Task.FromResult(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
        };

        if (asynchronous)
        {
            _ = await Bridge.PublishOmronPlcTag(
                    SignalAsync.Emit<IMqttClient>(mqtt),
                    tag,
                    plc,
                    _ => expected)
                .ToObservable().FirstAsync(OperationTimeout);
        }
        else
        {
            _ = await Bridge.PublishOmronPlcTag(
                    Signal.Emit<IMqttClient>(mqtt),
                    tag,
                    plc,
                    _ => expected)
                .FirstAsync(OperationTimeout);
        }

        await Assert.That(ReferenceEquals(actual, expected)).IsTrue();
    }

    /// <summary>Forwards message-construction failures to the returned sequence.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task PublishOmronPlcTag_FactoryFailurePropagatesAsync()
    {
        using var plc = new OmronPlcSimulator();
        var tag = new LogicalTagKey<int>("Failure");
        plc.Seed(new(tag.Name, "D100"), ObservedValue);
        using var mqtt = new ScriptedMqttClient();
        var failure = new InvalidOperationException("message factory failed");
        await Assert.That(async () => await Bridge.PublishOmronPlcTag(
                Signal.Emit<IMqttClient>(mqtt),
                tag,
                plc,
                _ => throw failure)
            .FirstAsync(OperationTimeout))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Preserves complete messages through resilient enqueue and processing.</summary>
    /// <param name="asynchronous">Whether to use the asynchronous-observable bridge.</param>
    /// <returns>The asynchronous test.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task PublishOmronPlcTag_ResilientMessageIdentityPreservedAsync(bool asynchronous)
    {
        using var plc = new OmronPlcSimulator();
        var tag = new LogicalTagKey<int>("Resilient");
        plc.Seed(new(tag.Name, "D100"), ObservedValue);
        using var processed = new TestSignal<ApplicationMessageProcessedEventArgs>();
        using var mqtt = Substitute.For<IResilientMqttClient>();
        var expected = new MqttApplicationMessageBuilder().WithTopic("plc/resilient").WithPayload(Payload).Build();
        MqttApplicationMessage? enqueued = null;
        _ = mqtt.ApplicationMessageProcessed.Returns(processed);
        _ = mqtt.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
        {
            enqueued = call.Arg<MqttApplicationMessage>();
            processed.OnNext(new(new() { ApplicationMessage = enqueued }, null));
            return Task.CompletedTask;
        });
        var operation = asynchronous
            ? Bridge.PublishOmronPlcTag(SignalAsync.Emit(mqtt), tag, plc, _ => expected).ToObservable()
            : Bridge.PublishOmronPlcTag(Signal.Emit(mqtt), tag, plc, _ => expected);
        var result = await operation.FirstAsync(OperationTimeout);
        await Assert.That(ReferenceEquals(enqueued, expected)).IsTrue();
        await Assert.That(ReferenceEquals(result.ApplicationMessage.ApplicationMessage, expected)).IsTrue();
    }

    /// <summary>Validates all dependencies before creating the observable.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task PublishOmronPlcTag_ValidatesDependenciesAsync()
    {
        using var plc = new OmronPlcSimulator();
        var tag = new LogicalTagKey<int>("Validation");
        Func<int, MqttApplicationMessage> factory = static _ => new() { Topic = "plc/validation" };
        var raw = Signal.None<IMqttClient>();
        await Assert.That(() => Bridge.PublishOmronPlcTag((IObservable<IMqttClient>)null!, tag, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(raw, null!, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(raw, tag, null!, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(raw, tag, plc, (Func<int, MqttApplicationMessage>)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag((IObservableAsync<IMqttClient>)null!, tag, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag((IObservable<IResilientMqttClient>)null!, tag, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag((IObservableAsync<IResilientMqttClient>)null!, tag, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IMqttClient>(), null!, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IMqttClient>(), tag, null!, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IMqttClient>(), tag, plc, (Func<int, MqttApplicationMessage>)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(Signal.None<IResilientMqttClient>(), null!, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(Signal.None<IResilientMqttClient>(), tag, null!, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(Signal.None<IResilientMqttClient>(), tag, plc, (Func<int, MqttApplicationMessage>)null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IResilientMqttClient>(), null!, plc, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IResilientMqttClient>(), tag, null!, factory))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishOmronPlcTag(SignalAsync.None<IResilientMqttClient>(), tag, plc, (Func<int, MqttApplicationMessage>)null!))
            .Throws<ArgumentNullException>();
    }
}
