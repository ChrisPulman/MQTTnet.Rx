// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.ABPlcRx;
using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using AbAsyncCreate = MQTTnet.Rx.ABPlc.ObservableAsyncCreateExtensions;
using AbCreate = MQTTnet.Rx.ABPlc.Create;
using S7Extensions = MQTTnet.Rx.S7Plc.S7PlcExtensions;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes the null-value and async-forwarding coverage paths for the AB and S7 MQTT bridges.</summary>
public sealed class FinalAbS7CoverageClosureTests
{
    /// <summary>The bounded duration used to await the deterministic in-memory bridge results.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <summary>Verifies both AB publishers serialize a null PLC value as an empty MQTT payload.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AbPublishers_SerializeNullObservedValuesAsEmptyPayloadsAsync()
    {
        const string topic = "coverage/ab/null";
        const string variable = "coverage.ab.null";
        var plc = Substitute.For<IABPlcRx>();
        _ = plc.Observe(variable, default(string), -1).Returns(Signal.Emit<string?>(null));
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();

        _ = await AbCreate.PublishABPlcTag<string>(
            Signal.Emit<IMqttClient>(rawClient),
            topic,
            variable,
            plc).FirstAsync(Timeout);
        var resilientResult = AbCreate.PublishABPlcTag<string>(
            Signal.Emit<IResilientMqttClient>(resilientClient),
            topic,
            variable,
            plc).FirstAsync(Timeout);
        await Task.Yield();
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await resilientResult;

        await Assert.That(rawClient.PublishedMessages.Count).IsEqualTo(1);
        await Assert.That(rawClient.PublishedMessages[0].Payload.IsEmpty).IsTrue();
    }

    /// <summary>Forwards valid raw and resilient sequences through the AB asynchronous publishers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AbAsyncPublishers_ForwardValidClientSequencesAsync()
    {
        const string topic = "coverage/ab/async";
        const string variable = "coverage.ab.async";
        var plc = Substitute.For<IABPlcRx>();
        _ = plc.Observe(variable, default(int), -1).Returns(Signal.None<int>());

        var raw = AbAsyncCreate.PublishABPlcTag<int>(
            SignalAsync.None<IMqttClient>(),
            topic,
            variable,
            plc,
            []);
        var resilient = AbAsyncCreate.PublishABPlcTag<int>(
            SignalAsync.None<IResilientMqttClient>(),
            topic,
            variable,
            plc,
            []);

        await Assert.That(raw).IsNotNull();
        await Assert.That(resilient).IsNotNull();
    }

    /// <summary>Verifies both S7 publishers serialize a null observed tag value as an empty MQTT payload.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task S7Publishers_SerializeNullObservedValuesAsEmptyPayloadsAsync()
    {
        var tag = new LogicalTagKey<string>("coverage.s7.null");
        var plc = Substitute.For<IRxS7>();
        _ = plc.Observe(tag).Returns(Signal.Emit<string?>(null));
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();

        var raw = S7Extensions.PublishS7PlcTag(
            Signal.Emit<IMqttClient>(rawClient),
            "coverage/s7/raw/null",
            tag,
            plc);
        var resilient = S7Extensions.PublishS7PlcTag(
            Signal.Emit<IResilientMqttClient>(resilientClient),
            "coverage/s7/resilient/null",
            tag,
            plc);

        _ = await raw.FirstAsync(Timeout);
        var resilientResult = resilient.FirstAsync(Timeout);
        await Task.Yield();
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        _ = await resilientResult;

        await Assert.That(rawClient.PublishedMessages.Count).IsEqualTo(1);
        await Assert.That(rawClient.PublishedMessages[0].Payload.IsEmpty).IsTrue();
    }
}
