// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
#if REACTIVE_SHIM
using CP.Collections.Reactive;
using MQTTnet.Rx.TwinCAT.Reactive;
using Bridge = MQTTnet.Rx.TwinCAT.Reactive.ApplicationMessageBridgeExtensions;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using CP.Collections;
using MQTTnet.Rx.TwinCAT;
using Bridge = MQTTnet.Rx.TwinCAT.ApplicationMessageBridgeExtensions;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies complete-message publishing from TwinCAT hash-table tags.</summary>
public sealed class TwinCatApplicationMessageBridgeExtensionsTests
{
    /// <summary>The hash-table variable observed by the bridge.</summary>
    private const string TagName = "ApplicationMessage.Value";

    /// <summary>The test message topic.</summary>
    private const string Topic = "tests/twincat/application-message";

    /// <summary>The value stored in the hash table.</summary>
    private const int ObservedValue = 321;

    /// <summary>The packet identifier for the scripted publish result.</summary>
    private const int PacketIdentifier = 0;

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
    public async Task PublishTcPlcTag_PreservesMessageIdentityAsync(bool resilient, bool asynchronous)
    {
        using var table = CreateTable();
        var expected = CreateMessage();
        IHashTableRx plc = table;

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
                ? SignalAsync.Emit(client).PublishTcPlcTag<int>(TagName, plc, _ => expected).ToObservable()
                : Signal.Emit(client).PublishTcPlcTag<int>(TagName, plc, _ => expected);
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
                ? SignalAsync.Emit<IMqttClient>(client).PublishTcPlcTag<int>(TagName, plc, _ => expected).ToObservable()
                : Signal.Emit<IMqttClient>(client).PublishTcPlcTag<int>(TagName, plc, _ => expected);
            _ = await operation.FirstAsync(OperationTimeout);
            await Assert.That(ReferenceEquals(published, expected)).IsTrue();
        }
    }

    /// <summary>Verifies all four overloads validate their source, tag, table, and factory.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task PublishTcPlcTag_ValidatesDependenciesAsync()
    {
        using var table = CreateTable();
        IHashTableRx plc = table;
        Func<int, MqttApplicationMessage> factory = static _ => new() { Topic = Topic };
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();
        var asyncRaw = SignalAsync.None<IMqttClient>();
        var asyncResilient = SignalAsync.None<IResilientMqttClient>();

        await Assert.That(() => Bridge.PublishTcPlcTag((IObservable<IMqttClient>)null!, TagName, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(raw, null!, plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(raw, " ", plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(raw, TagName, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(raw, TagName, plc, (Func<int, MqttApplicationMessage>)null!)).Throws<ArgumentNullException>();

        await Assert.That(() => Bridge.PublishTcPlcTag((IObservable<IResilientMqttClient>)null!, TagName, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(resilient, null!, plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(resilient, " ", plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(resilient, TagName, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(resilient, TagName, plc, (Func<int, MqttApplicationMessage>)null!)).Throws<ArgumentNullException>();

        await Assert.That(() => Bridge.PublishTcPlcTag((IObservableAsync<IMqttClient>)null!, TagName, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncRaw, null!, plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncRaw, " ", plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncRaw, TagName, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncRaw, TagName, plc, (Func<int, MqttApplicationMessage>)null!)).Throws<ArgumentNullException>();

        await Assert.That(() => Bridge.PublishTcPlcTag((IObservableAsync<IResilientMqttClient>)null!, TagName, plc, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncResilient, null!, plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncResilient, " ", plc, factory)).Throws<ArgumentException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncResilient, TagName, null!, factory)).Throws<ArgumentNullException>();
        await Assert.That(() => Bridge.PublishTcPlcTag(asyncResilient, TagName, plc, (Func<int, MqttApplicationMessage>)null!)).Throws<ArgumentNullException>();
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
    public async Task PublishTcPlcTag_PropagatesFactoryFailureAsync(bool resilient, bool asynchronous)
    {
        using var table = CreateTable();
        IHashTableRx plc = table;
        var failure = new InvalidOperationException("message factory failed");
        Func<int, MqttApplicationMessage> factory = _ => throw failure;

        if (resilient)
        {
            using var client = Substitute.For<IResilientMqttClient>();
            _ = client.ApplicationMessageProcessed.Returns(Signal.Empty<ApplicationMessageProcessedEventArgs>());
            _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(Task.CompletedTask);
            var operation = asynchronous
                ? SignalAsync.Emit(client).PublishTcPlcTag(TagName, plc, factory).ToObservable()
                : Signal.Emit(client).PublishTcPlcTag(TagName, plc, factory);
            await Assert.That(async () => await operation.FirstAsync(OperationTimeout)).Throws<InvalidOperationException>();
        }
        else
        {
            using var client = new ScriptedMqttClient();
            var operation = asynchronous
                ? SignalAsync.Emit<IMqttClient>(client).PublishTcPlcTag(TagName, plc, factory).ToObservable()
                : Signal.Emit<IMqttClient>(client).PublishTcPlcTag(TagName, plc, factory);
            await Assert.That(async () => await operation.FirstAsync(OperationTimeout)).Throws<InvalidOperationException>();
        }
    }

    /// <summary>Creates a case-preserving hash table with one integer value.</summary>
    /// <returns>The populated table.</returns>
    private static HashTableRx CreateTable()
    {
        var table = new HashTableRx(useUpperCase: false);
        table.Add(TagName, ObservedValue);
        return table;
    }

    /// <summary>Creates the complete MQTT message forwarded by the publisher.</summary>
    /// <returns>The expected application message.</returns>
    private static MqttApplicationMessage CreateMessage() => new MqttApplicationMessageBuilder()
        .WithTopic(Topic)
        .WithPayload(Payload)
        .Build();
}
#endif
