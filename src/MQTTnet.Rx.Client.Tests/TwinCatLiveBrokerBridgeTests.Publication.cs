// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Globalization;
using System.Text;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains the publication assertion helpers for the TwinCAT live-broker bridge fixture.</summary>
public sealed partial class TwinCatLiveBrokerBridgeTests
{
    /// <summary>Publishes one raw synchronous bridge result and verifies the probe's exact message.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="value">The value expected at the probe.</param>
    /// <param name="publishResults">The TwinCAT bridge result stream.</param>
    /// <param name="trigger">The ADS/hash mutation that starts the flow.</param>
    /// <returns>A task representing the end-to-end publish evidence.</returns>
    private static async Task AssertRawPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int value,
        IObservable<MqttClientPublishResult> publishResults,
        Action<int> trigger)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = publishResults.FirstAsync(OperationTimeout);
        await Task.Yield();
        trigger(value);

        var result = await resultTask;
        var message = await probe.MessageReceived.WaitAsync(OperationTimeout);
        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Acknowledges a hash publisher before verifying raw-client mutation output.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="initialValue">The hash table's current value when the bridge subscribes.</param>
    /// <param name="value">The mutated value expected at the probe.</param>
    /// <param name="publishResults">The TwinCAT bridge result stream.</param>
    /// <param name="trigger">The hash mutation that starts the second flow.</param>
    /// <returns>A task representing both current-state and mutation evidence.</returns>
    private static async Task AssertRawHashPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int initialValue,
        int value,
        IObservable<MqttClientPublishResult> publishResults,
        Action<int> trigger)
    {
        var evidence = await CaptureHashPublicationsAsync(broker, topic, value, publishResults, trigger);
        await Assert.That(evidence.InitialResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(evidence.MutationResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await AssertPublishedMessageAsync(evidence.InitialMessage, topic, initialValue);
        await AssertPublishedMessageAsync(evidence.MutationMessage, topic, value);
    }

    /// <summary>Publishes one raw asynchronous bridge result and verifies the probe's exact message.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="value">The value expected at the probe.</param>
    /// <param name="publishResults">The asynchronous TwinCAT bridge result stream.</param>
    /// <param name="trigger">The ADS/hash mutation that starts the flow.</param>
    /// <returns>A task representing the end-to-end publish evidence.</returns>
    private static async Task AssertRawAsyncPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int value,
        IObservableAsync<MqttClientPublishResult> publishResults,
        Action<int> trigger)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = publishResults.FirstAsync(OperationTimeout);
        await Task.Yield();
        trigger(value);

        var result = await resultTask;
        var message = await probe.MessageReceived.WaitAsync(OperationTimeout);
        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Acknowledges a hash publisher's current state before verifying its asynchronous raw-client mutation.
    /// </summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="initialValue">The hash table's current value when the bridge subscribes.</param>
    /// <param name="value">The mutated value expected at the probe.</param>
    /// <param name="publishResults">The asynchronous TwinCAT bridge result stream.</param>
    /// <param name="trigger">The hash mutation that starts the second flow.</param>
    /// <returns>A task representing both current-state and mutation evidence.</returns>
    private static Task AssertRawAsyncHashPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int initialValue,
        int value,
        IObservableAsync<MqttClientPublishResult> publishResults,
        Action<int> trigger) =>
        AssertRawHashPublishAsync(broker, topic, initialValue, value, publishResults.ToObservable(), trigger);

    /// <summary>Publishes one resilient synchronous bridge result and verifies the probe's exact message.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="value">The value expected at the probe.</param>
    /// <param name="publishResults">The resilient TwinCAT bridge result stream.</param>
    /// <param name="trigger">The ADS/hash mutation that starts the flow.</param>
    /// <returns>A task representing the end-to-end publish evidence.</returns>
    private static async Task AssertResilientPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int value,
        IObservable<ApplicationMessageProcessedEventArgs> publishResults,
        Action<int> trigger)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = publishResults.FirstAsync(OperationTimeout);
        await Task.Yield();
        trigger(value);

        var result = await resultTask;
        var message = await probe.MessageReceived.WaitAsync(OperationTimeout);
        await Assert.That(result.Exception).IsNull();
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Acknowledges a hash publisher's current state before verifying its resilient-client mutation publication.
    /// </summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="initialValue">The hash table's current value when the bridge subscribes.</param>
    /// <param name="value">The mutated value expected at the probe.</param>
    /// <param name="publishResults">The resilient TwinCAT bridge result stream.</param>
    /// <param name="trigger">The hash mutation that starts the second flow.</param>
    /// <returns>A task representing both current-state and mutation evidence.</returns>
    private static async Task AssertResilientHashPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int initialValue,
        int value,
        IObservable<ApplicationMessageProcessedEventArgs> publishResults,
        Action<int> trigger)
    {
        var evidence = await CaptureHashPublicationsAsync(broker, topic, value, publishResults, trigger);
        await Assert.That(evidence.InitialResult.Exception).IsNull();
        await Assert.That(evidence.MutationResult.Exception).IsNull();
        await AssertPublishedMessageAsync(evidence.InitialMessage, topic, initialValue);
        await AssertPublishedMessageAsync(evidence.MutationMessage, topic, value);
    }

    /// <summary>Publishes one resilient asynchronous bridge result and verifies the probe's exact message.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="value">The value expected at the probe.</param>
    /// <param name="publishResults">The asynchronous resilient TwinCAT bridge result stream.</param>
    /// <param name="trigger">The ADS/hash mutation that starts the flow.</param>
    /// <returns>A task representing the end-to-end publish evidence.</returns>
    private static async Task AssertResilientAsyncPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int value,
        IObservableAsync<ApplicationMessageProcessedEventArgs> publishResults,
        Action<int> trigger)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var resultTask = publishResults.FirstAsync(OperationTimeout);
        await Task.Yield();
        trigger(value);

        var result = await resultTask;
        var message = await probe.MessageReceived.WaitAsync(OperationTimeout);
        await Assert.That(result.Exception).IsNull();
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Acknowledges a hash publisher's current state before verifying its asynchronous resilient-client mutation.
    /// </summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="initialValue">The hash table's current value when the bridge subscribes.</param>
    /// <param name="value">The mutated value expected at the probe.</param>
    /// <param name="publishResults">The asynchronous resilient TwinCAT bridge result stream.</param>
    /// <param name="trigger">The hash mutation that starts the second flow.</param>
    /// <returns>A task representing both current-state and mutation evidence.</returns>
    private static Task AssertResilientAsyncHashPublishAsync(
        LiveMqttBroker broker,
        string topic,
        int initialValue,
        int value,
        IObservableAsync<ApplicationMessageProcessedEventArgs> publishResults,
        Action<int> trigger) =>
        AssertResilientHashPublishAsync(broker, topic, initialValue, value, publishResults.ToObservable(), trigger);
}
#endif
