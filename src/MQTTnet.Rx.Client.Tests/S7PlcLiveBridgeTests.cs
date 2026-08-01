// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using IoT.Driver.Core;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.S7Plc;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using S7Create = MQTTnet.Rx.S7Plc.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises every S7 MQTT bridge over a live in-process MQTT broker and an in-memory PLC boundary.</summary>
public sealed partial class S7PlcLiveBridgeTests
{
    /// <summary>The validation-only topic.</summary>
    private const string ValidationTopic = "s7/validation";

    /// <summary>The third zero-based write index.</summary>
    private const int ThirdWriteIndex = 2;

    /// <summary>Raw typed publish value.</summary>
    private const int RawTypedPublishValue = 101;

    /// <summary>Raw string publish value.</summary>
    private const int RawStringPublishValue = 102;

    /// <summary>Raw asynchronous publish value.</summary>
    private const int RawAsyncPublishValue = 103;

    /// <summary>Raw typed write value.</summary>
    private const int RawTypedWriteValue = 201;

    /// <summary>Raw string write value.</summary>
    private const int RawStringWriteValue = 202;

    /// <summary>Raw asynchronous write value.</summary>
    private const int RawAsyncWriteValue = 203;

    /// <summary>Resilient typed publish value.</summary>
    private const int ResilientTypedPublishValue = 301;

    /// <summary>Resilient string publish value.</summary>
    private const int ResilientStringPublishValue = 302;

    /// <summary>Resilient asynchronous publish value.</summary>
    private const int ResilientAsyncPublishValue = 303;

    /// <summary>Resilient typed write value.</summary>
    private const int ResilientTypedWriteValue = 401;

    /// <summary>Resilient string write value.</summary>
    private const int ResilientStringWriteValue = 402;

    /// <summary>Resilient asynchronous write value.</summary>
    private const int ResilientAsyncWriteValue = 403;

    /// <summary>The typed PLC tag used by bridge tests.</summary>
    private static readonly LogicalTagKey<int> Tag = new("S7.Live.Value");

    /// <summary>The maximum time allowed for a live broker operation.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Proves all raw-client S7 bridges move values in both directions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task RawClientBridges_MoveTypedAndStringValuesInBothDirectionsAsync()
    {
        const string typedWriteTopic = "s7/raw/typed/write";
        const string stringWriteTopic = "s7/raw/string/write";
        const string asyncWriteTopic = "s7/raw/async/write";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var plc = new RecordingS7();

        plc.SetObserved(Tag, RawTypedPublishValue);
        await AssertRawPublishAsync(
            broker,
            "s7/raw/typed/publish",
            broker.Bridge.PublishS7PlcTag("s7/raw/typed/publish", Tag, plc),
            RawTypedPublishValue);

        plc.SetObserved(Tag, RawStringPublishValue);
        await AssertRawPublishAsync(
            broker,
            "s7/raw/string/publish",
            S7Create.PublishS7PlcTag<int>(broker.Bridge, "s7/raw/string/publish", Tag.Name, plc),
            RawStringPublishValue);

        plc.SetObserved(Tag, RawAsyncPublishValue);
        await AssertRawPublishAsync(
            broker,
            "s7/raw/async/publish",
            SignalAsync.Return(broker.BridgeClient)
                .PublishS7PlcTag("s7/raw/async/publish", Tag, plc)
                .ToObservable(),
            RawAsyncPublishValue);

        using (var subscription = broker.Bridge.SubscribeS7PlcTag(typedWriteTopic, Tag, plc, int.Parse))
        {
            await EnsureRawSubscriptionAsync(broker.BridgeClient, typedWriteTopic);
            await AssertWriteAsync(broker, plc, 0, typedWriteTopic, RawTypedWriteValue);
        }

        S7Create.SubscribeS7PlcTag(broker.Bridge, stringWriteTopic, Tag.Name, plc, int.Parse);
        await EnsureRawSubscriptionAsync(broker.BridgeClient, stringWriteTopic);
        await AssertWriteAsync(broker, plc, 1, stringWriteTopic, RawStringWriteValue);

        using (var subscription = SignalAsync.Return(broker.BridgeClient)
            .SubscribeS7PlcTag(asyncWriteTopic, Tag, plc, int.Parse))
        {
            await EnsureRawSubscriptionAsync(broker.BridgeClient, asyncWriteTopic);
            await AssertWriteAsync(broker, plc, ThirdWriteIndex, asyncWriteTopic, RawAsyncWriteValue);
        }

        await Assert.That(plc.IsDisposed).IsFalse();
    }

    /// <summary>Proves all resilient-client S7 bridges move values in both directions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ResilientClientBridges_MoveTypedAndStringValuesInBothDirectionsAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var plc = new RecordingS7();
        await using var resilient = await LiveResilientSource.StartAsync(broker);

        plc.SetObserved(Tag, ResilientTypedPublishValue);
        await AssertResilientPublishAsync(
            broker,
            "s7/resilient/typed/publish",
            resilient.Source.PublishS7PlcTag("s7/resilient/typed/publish", Tag, plc),
            ResilientTypedPublishValue);

        plc.SetObserved(Tag, ResilientStringPublishValue);
        await AssertResilientPublishAsync(
            broker,
            "s7/resilient/string/publish",
            S7Create.PublishS7PlcTag<int>(resilient.Source, "s7/resilient/string/publish", Tag.Name, plc),
            ResilientStringPublishValue);

        plc.SetObserved(Tag, ResilientAsyncPublishValue);
        await AssertResilientPublishAsync(
            broker,
            "s7/resilient/async/publish",
            SignalAsync.Return(resilient.Client)
                .PublishS7PlcTag("s7/resilient/async/publish", Tag, plc)
                .ToObservable(),
            ResilientAsyncPublishValue);

        await AssertResilientTypedWriteAsync(broker, resilient, plc);
        await AssertResilientStringWriteAsync(broker, resilient, plc);
        await AssertResilientAsyncWriteAsync(broker, resilient, plc);
    }

    /// <summary>Exercises every null-validation branch on synchronous raw and resilient S7 extensions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task SynchronousExtensions_RejectEveryMissingDependencyAsync()
    {
        using var plc = new RecordingS7();
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();

        await AssertRawSynchronousValidationAsync(plc, raw);
        await AssertResilientSynchronousValidationAsync(plc, resilient);
    }

    /// <summary>Exercises every null-validation branch on asynchronous raw and resilient S7 extensions.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task AsynchronousExtensions_RejectEveryMissingDependencyAsync()
    {
        using var plc = new RecordingS7();
        var raw = SignalAsync.None<IMqttClient>();
        var resilient = SignalAsync.None<IResilientMqttClient>();

        await AssertRawAsynchronousValidationAsync(plc, raw);
        await AssertResilientAsynchronousValidationAsync(plc, resilient);
    }

    /// <summary>Publishes one fake PLC value through a raw bridge and verifies it at the live probe.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="topic">The exact topic to probe.</param>
    /// <param name="results">The raw-client publish results.</param>
    /// <param name="expectedValue">The integer expected in the MQTT payload.</param>
    /// <returns>A task representing the publish assertion.</returns>
    private static async Task AssertRawPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservable<MqttClientPublishResult> results,
        int expectedValue)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = results.FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(expectedValue.ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>Publishes one fake PLC value through a resilient bridge and verifies it at the live probe.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="topic">The exact topic to probe.</param>
    /// <param name="results">The resilient-client processed-message results.</param>
    /// <param name="expectedValue">The integer expected in the MQTT payload.</param>
    /// <returns>A task representing the publish assertion.</returns>
    private static async Task AssertResilientPublishAsync(
        LiveMqttBroker broker,
        string topic,
        IObservable<ApplicationMessageProcessedEventArgs> results,
        int expectedValue)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = results.FirstAsync(Timeout);
        var messageTask = probe.MessageReceived.WaitAsync(Timeout);

        var result = await resultTask;
        var message = await messageTask;

        await Assert.That(result.Exception).IsNull();
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(expectedValue.ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>Publishes a retained probe value and verifies the S7 bridge records the converted write.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="plc">The recording PLC seam.</param>
    /// <param name="writeIndex">The zero-based write to await.</param>
    /// <param name="topic">The MQTT topic consumed by the bridge.</param>
    /// <param name="expectedValue">The value expected at the PLC seam.</param>
    /// <returns>A task representing the write assertion.</returns>
    private static async Task AssertWriteAsync(
        LiveMqttBroker broker,
        RecordingS7 plc,
        int writeIndex,
        string topic,
        int expectedValue)
    {
        var writeTask = plc.WaitForWriteAsync(writeIndex);
        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(expectedValue.ToString(CultureInfo.CurrentCulture))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        var publishResult = await broker.ProbeClient.PublishAsync(applicationMessage, CancellationToken.None);
        var write = await writeTask.WaitAsync(Timeout);

        await Assert.That(publishResult.ReasonCode is
            MqttClientPublishReasonCode.Success or
            MqttClientPublishReasonCode.NoMatchingSubscribers).IsTrue();
        await Assert.That(write.Variable).IsEqualTo(Tag.Name);
        await Assert.That(write.Value).IsEqualTo(expectedValue);
    }

    /// <summary>Awaits an exact-topic SUBACK on the raw bridge client before publishing a write probe.</summary>
    /// <param name="client">The connected raw bridge client.</param>
    /// <param name="topic">The exact bridge topic.</param>
    /// <returns>A task representing the broker subscription handshake.</returns>
    private static async Task EnsureRawSubscriptionAsync(IMqttClient client, string topic)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        var result = await client.SubscribeAsync(options, CancellationToken.None);
        foreach (var item in result.Items)
        {
            if (item.ResultCode is not MqttClientSubscribeResultCode.GrantedQoS0
                and not MqttClientSubscribeResultCode.GrantedQoS1
                and not MqttClientSubscribeResultCode.GrantedQoS2)
            {
                throw new InvalidOperationException(
                    $"The live broker rejected the S7 bridge subscription to '{topic}'.");
            }
        }
    }

    /// <summary>Verifies the typed resilient bridge after its exact-topic SUBACK.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="resilient">The connected resilient MQTT source.</param>
    /// <param name="plc">The recording PLC seam.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertResilientTypedWriteAsync(
        LiveMqttBroker broker,
        LiveResilientSource resilient,
        RecordingS7 plc)
    {
        const string topic = "s7/resilient/typed/write";
        using var readinessRegistration = resilient.RegisterSubscriptionReadiness(topic, out var readiness);
        using var subscription = resilient.Source.SubscribeS7PlcTag(topic, Tag, plc, int.Parse);
        await readiness.WaitAsync(Timeout);
        await AssertWriteAsync(broker, plc, 0, topic, ResilientTypedWriteValue);
    }

    /// <summary>Verifies the string compatibility resilient bridge after its exact-topic SUBACK.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="resilient">The connected resilient MQTT source.</param>
    /// <param name="plc">The recording PLC seam.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertResilientStringWriteAsync(
        LiveMqttBroker broker,
        LiveResilientSource resilient,
        RecordingS7 plc)
    {
        const string topic = "s7/resilient/string/write";
        using var readinessRegistration = resilient.RegisterSubscriptionReadiness(topic, out var readiness);
        S7Create.SubscribeS7PlcTag(resilient.Source, topic, Tag.Name, plc, int.Parse);
        await readiness.WaitAsync(Timeout);
        await AssertWriteAsync(broker, plc, 1, topic, ResilientStringWriteValue);
    }

    /// <summary>Verifies the asynchronous resilient bridge after its exact-topic SUBACK.</summary>
    /// <param name="broker">The live MQTT broker fixture.</param>
    /// <param name="resilient">The connected resilient MQTT source.</param>
    /// <param name="plc">The recording PLC seam.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertResilientAsyncWriteAsync(
        LiveMqttBroker broker,
        LiveResilientSource resilient,
        RecordingS7 plc)
    {
        const string topic = "s7/resilient/async/write";
        using var readinessRegistration = resilient.RegisterSubscriptionReadiness(topic, out var readiness);
        using var subscription = SignalAsync.Return(resilient.Client)
            .SubscribeS7PlcTag(topic, Tag, plc, int.Parse);
        await readiness.WaitAsync(Timeout);
        await AssertWriteAsync(broker, plc, ThirdWriteIndex, topic, ResilientAsyncWriteValue);
    }
}
