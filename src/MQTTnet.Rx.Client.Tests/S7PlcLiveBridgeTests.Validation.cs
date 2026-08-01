// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
using ReactiveUI.Primitives.Async;
using S7AsyncExtensions = MQTTnet.Rx.S7Plc.ObservableAsyncCreateExtensions;
using S7Extensions = MQTTnet.Rx.S7Plc.S7PlcExtensions;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains null-validation assertions for S7 bridge overloads.</summary>
public sealed partial class S7PlcLiveBridgeTests
{
    /// <summary>Asserts synchronous raw-client null validation.</summary>
    /// <param name="plc">The recording PLC seam.</param>
    /// <param name="raw">The raw MQTT client source.</param>
    /// <returns>A task representing the validation assertions.</returns>
    private static async Task AssertRawSynchronousValidationAsync(
        RecordingS7 plc,
        IObservable<IMqttClient> raw)
    {
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                (IObservable<IMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                raw,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                (IObservable<IMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                null!,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                plc,
                null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts synchronous resilient-client null validation.</summary>
    /// <param name="plc">The recording PLC seam.</param>
    /// <param name="resilient">The resilient MQTT client source.</param>
    /// <returns>A task representing the validation assertions.</returns>
    private static async Task AssertResilientSynchronousValidationAsync(
        RecordingS7 plc,
        IObservable<IResilientMqttClient> resilient)
    {
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                (IObservable<IResilientMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                resilient,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.PublishS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                (IObservable<IResilientMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                null!,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7Extensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                plc,
                null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts asynchronous raw-client null validation.</summary>
    /// <param name="plc">The recording PLC seam.</param>
    /// <param name="raw">The asynchronous raw MQTT client source.</param>
    /// <returns>A task representing the validation assertions.</returns>
    private static async Task AssertRawAsynchronousValidationAsync(
        RecordingS7 plc,
        IObservableAsync<IMqttClient> raw)
    {
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                raw,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                null!,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                raw,
                ValidationTopic,
                Tag,
                plc,
                null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts asynchronous resilient-client null validation.</summary>
    /// <param name="plc">The recording PLC seam.</param>
    /// <param name="resilient">The asynchronous resilient MQTT client source.</param>
    /// <returns>A task representing the validation assertions.</returns>
    private static async Task AssertResilientAsynchronousValidationAsync(
        RecordingS7 plc,
        IObservableAsync<IResilientMqttClient> resilient)
    {
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                resilient,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.PublishS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                Tag,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                (LogicalTagKey<int>)null!,
                plc,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                null!,
                int.Parse))
            .Throws<ArgumentNullException>();
        await Assert.That(() => S7AsyncExtensions.SubscribeS7PlcTag(
                resilient,
                ValidationTopic,
                Tag,
                plc,
                null!))
            .Throws<ArgumentNullException>();
    }
}
