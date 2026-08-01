// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using IoT.Driver.Serial;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.SerialPort;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using SerialAsyncCreate = MQTTnet.Rx.SerialPort.ObservableAsyncCreateExtensions;
using SerialCreate = MQTTnet.Rx.SerialPort.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the serial-port MQTT bridge over real MQTT and deterministic paired serial transports.</summary>
[NotInParallel]
public sealed partial class SerialPortLiveBridgeTests
{
    /// <summary>The short frame timeout used by construction-only and completion tests.</summary>
    private const int FrameTimeoutMilliseconds = 100;

    /// <summary>The live framed-payload timeout in milliseconds.</summary>
    private const int LiveFrameTimeoutMilliseconds = 500;

    /// <summary>The valid topic used by argument-validation tests.</summary>
    private const string ValidationTopic = "topic";

    /// <summary>The private writer-core method exercised for its defensive final guard.</summary>
    private const string WriterCoreMethodName = "SubscribeSerialPortWriteCore";

    /// <summary>The maximum duration of one network or observable operation.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Proves static raw publishing reaches a real broker probe.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task StaticRawPublish_PairedFrameReachesRealBrokerProbeAsync()
    {
        const string topic = "tests/serial/raw/static/publish";
        const string framedPayload = "<raw-static>";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var probeSubscription = await broker.SubscribeProbeAsync(topic);
        using var pair = new InMemoryPortRxPair("RAW-STATIC-TX", "RAW-STATIC-BRIDGE");
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();

        var publishResult = SerialCreate.PublishSerialPort(
                broker.Bridge,
                topic,
                pair.Second,
                Signal.Emit('<'),
                Signal.Emit('>'),
                LiveFrameTimeoutMilliseconds)
            .FirstAsync(OperationTimeout);

        pair.First.Write($"ignored{framedPayload}trailing");

        var result = await publishResult;
        var received = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(received.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo(framedPayload);
    }

    /// <summary>Proves async raw publishing with alternate delimiters reaches a real broker probe.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncRawExtensionPublish_PairedFrameReachesRealBrokerProbeAsync()
    {
        const string topic = "tests/serial/raw/async/publish";
        const string framedPayload = "[raw-async]";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var probeSubscription = await broker.SubscribeProbeAsync(topic);
        using var pair = new InMemoryPortRxPair("RAW-ASYNC-TX", "RAW-ASYNC-BRIDGE");
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();

        var publishResult = broker.Bridge
            .ToSignal()
            .PublishSerialPort(
                topic,
                pair.Second,
                Signal.Emit('[').ToSignal(),
                Signal.Emit(']').ToSignal(),
                LiveFrameTimeoutMilliseconds)
            .FirstAsync(OperationTimeout);

        pair.First.Write($"noise{framedPayload}");

        var result = await publishResult;
        var received = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo(framedPayload);
    }

    /// <summary>Proves static resilient publishing delivers a serial frame through a real broker.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task StaticResilientPublish_PairedFrameReachesRealBrokerProbeAsync()
    {
        const string topic = "tests/serial/resilient/static/publish";
        const string framedPayload = "{resilient-static}";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        await using var probeSubscription = await broker.SubscribeProbeAsync(topic);
        await using var resilient = await LiveResilientLease.StartAsync(broker.Port);
        using var pair = new InMemoryPortRxPair("RESILIENT-TX", "RESILIENT-BRIDGE");
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();

        var processedResult = SerialCreate.PublishSerialPort(
                resilient.Source,
                topic,
                pair.Second,
                Signal.Emit('{'),
                Signal.Emit('}'),
                LiveFrameTimeoutMilliseconds)
            .FirstAsync(OperationTimeout);

        pair.First.Write(framedPayload);

        var processed = await processedResult;
        var received = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

        await Assert.That(processed.Exception).IsNull();
        await Assert.That(received.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo(framedPayload);
    }

    /// <summary>Proves raw synchronous writers traverse a real broker and paired port.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawSyncWriters_RealBrokerPayloadsReachPairedSerialEndpointAsync()
    {
        const string lineTopic = "tests/serial/raw/write/line";
        const string textTopic = "tests/serial/raw/write/text";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var pair = new InMemoryPortRxPair("RAW-WRITER", "RAW-RECEIVER");
        await OpenPairAsync(pair, "\r\n");
        var lines = new List<string>();
        var batches = new List<byte[]>();
        var lineReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var textReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lineCapture = pair.Second.Lines.Subscribe(line =>
        {
            lock (lines)
            {
                lines.Add(line);
            }

            _ = line == "line:one" && lineReceived.TrySetResult(true);
        });
        using var batchCapture = pair.Second.DataReceivedBatches.Subscribe(batch =>
        {
            lock (batches)
            {
                batches.Add(batch);
            }

            var text = Encoding.ASCII.GetString(batch);
            _ = text == "text:two" && textReceived.TrySetResult(true);
        });
        using var lineBridge = SerialCreate.SubscribeSerialPortWriteLine(
            broker.Bridge,
            lineTopic,
            pair.First,
            static payload => $"line:{payload}");
        using var textBridge = broker.Bridge.SubscribeSerialPortWrite(
            textTopic,
            pair.First,
            static payload => $"text:{payload}");

        await EnsureRawSubscriptionAsync(broker.BridgeClient, lineTopic);
        await EnsureRawSubscriptionAsync(broker.BridgeClient, textTopic);
        await PublishFromProbeAsync(broker.ProbeClient, lineTopic, "one");
        _ = await lineReceived.Task.WaitAsync(OperationTimeout);
        await PublishFromProbeAsync(broker.ProbeClient, textTopic, "two");
        _ = await textReceived.Task.WaitAsync(OperationTimeout);

        await Assert.That(lines).Contains("line:one");
        await Assert.That(ContainsBatch(batches, "text:two")).IsTrue();
    }

    /// <summary>Proves raw async writers traverse a real broker and paired port.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawAsyncWriters_RealBrokerPayloadsReachPairedSerialEndpointAsync()
    {
        const string bytesTopic = "tests/serial/raw/write/bytes";
        const string asyncLineTopic = "tests/serial/raw/write/async-line";
        await using var broker = await LiveMqttBroker.StartAsync();
        _ = await broker.ConnectClientsAsync();
        using var bytePair = new InMemoryPortRxPair("RAW-ASYNC-BYTE-WRITER", "RAW-ASYNC-BYTE-RECEIVER");
        using var linePair = new InMemoryPortRxPair("RAW-ASYNC-LINE-WRITER", "RAW-ASYNC-LINE-RECEIVER");
        await OpenPairAsync(bytePair, "\n");
        await OpenPairAsync(linePair, "\n");
        var lines = new List<string>();
        var batches = new List<byte[]>();
        var bytesReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lineReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lineCapture = linePair.Second.Lines.Subscribe(line =>
        {
            lines.Add(line);
            _ = line == "async:four" && lineReceived.TrySetResult(true);
        });
        using var batchCapture = bytePair.Second.DataReceivedBatches.Subscribe(batch =>
        {
            batches.Add(batch);
            _ = Encoding.ASCII.GetString(batch) == "bytes:three" && bytesReceived.TrySetResult(true);
        });
        using var byteBridge = SerialAsyncCreate.SubscribeSerialPortWrite(
            broker.Bridge.ToSignal(),
            bytesTopic,
            bytePair.First,
            static payload => Encoding.ASCII.GetBytes($"bytes:{payload}"));
        using var lineBridge = broker.Bridge
            .ToSignal()
            .SubscribeSerialPortWriteLine(
                asyncLineTopic,
                linePair.First,
                static payload => $"async:{payload}");

        await EnsureRawSubscriptionAsync(broker.BridgeClient, bytesTopic);
        await EnsureRawSubscriptionAsync(broker.BridgeClient, asyncLineTopic);
        await PublishFromProbeAsync(broker.ProbeClient, bytesTopic, "three");
        _ = await bytesReceived.Task.WaitAsync(OperationTimeout);
        await PublishFromProbeAsync(broker.ProbeClient, asyncLineTopic, "four");
        _ = await lineReceived.Task.WaitAsync(OperationTimeout);

        await Assert.That(lines).Contains("async:four");
        await Assert.That(ContainsBatch(batches, "bytes:three")).IsTrue();
    }

    /// <summary>Exercises resilient writers with deterministic messages and a paired port.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientSyncWriters_ReachPairedPortAsync()
    {
        const string staticLineTopic = "tests/serial/resilient/write/static-line";
        const string staticTextTopic = "tests/serial/resilient/write/static-text";
        const string staticBytesTopic = "tests/serial/resilient/write/static-bytes";
        using var client = new MockResilientMqttClient();
        var clients = Signal.Emit<IResilientMqttClient>(client);
        using var pair = new InMemoryPortRxPair("RESILIENT-WRITER", "RESILIENT-RECEIVER");
        pair.First.NewLine = "\n";
        pair.Second.NewLine = "\n";
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();
        var lines = new List<string>();
        var batches = new List<byte[]>();
        using var lineCapture = pair.Second.Lines.Subscribe(lines.Add);
        using var batchCapture = pair.Second.DataReceivedBatches.Subscribe(batches.Add);

        using var staticLine = SerialCreate.SubscribeSerialPortWriteLine(
            clients,
            staticLineTopic,
            pair.First,
            static payload => $"sl:{payload}");
        using var staticText = SerialCreate.SubscribeSerialPortWrite(
            clients,
            staticTextTopic,
            pair.First,
            static payload => $"st:{payload}");
        using var staticBytes = clients.SubscribeSerialPortWrite(
            staticBytesTopic,
            pair.First,
            static payload => Encoding.ASCII.GetBytes($"sb:{payload}"));

        await client.SimulateMessageReceivedAsync(staticLineTopic, "one");
        await client.SimulateMessageReceivedAsync(staticTextTopic, "two");
        await client.SimulateMessageReceivedAsync(staticBytesTopic, "three");

        await Assert.That(lines).Contains("sl:one");
        await Assert.That(ContainsBatch(batches, "st:two")).IsTrue();
        await Assert.That(ContainsBatch(batches, "sb:three")).IsTrue();
    }

    /// <summary>Exercises async-observable resilient writers with deterministic messages and a paired port.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientAsyncWriters_ReachPairedPortAsync()
    {
        const string asyncLineTopic = "tests/serial/resilient/write/async-line";
        const string asyncTextTopic = "tests/serial/resilient/write/async-text";
        const string asyncBytesTopic = "tests/serial/resilient/write/async-bytes";
        using var client = new MockResilientMqttClient();
        var asyncClients = Signal.Emit<IResilientMqttClient>(client).ToSignal();
        using var pair = new InMemoryPortRxPair("RESILIENT-ASYNC-WRITER", "RESILIENT-ASYNC-RECEIVER");
        pair.First.NewLine = "\n";
        pair.Second.NewLine = "\n";
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();
        var lines = new List<string>();
        var batches = new List<byte[]>();
        using var lineCapture = pair.Second.Lines.Subscribe(lines.Add);
        using var batchCapture = pair.Second.DataReceivedBatches.Subscribe(batches.Add);
        using var asyncLine = SerialAsyncCreate.SubscribeSerialPortWriteLine(
            asyncClients,
            asyncLineTopic,
            pair.First,
            static payload => $"al:{payload}");
        using var asyncText = asyncClients.SubscribeSerialPortWrite(
            asyncTextTopic,
            pair.First,
            static payload => $"at:{payload}");
        using var asyncBytes = SerialAsyncCreate.SubscribeSerialPortWrite(
            asyncClients,
            asyncBytesTopic,
            pair.First,
            static payload => Encoding.ASCII.GetBytes($"ab:{payload}"));

        await client.SimulateMessageReceivedAsync(asyncLineTopic, "four");
        await client.SimulateMessageReceivedAsync(asyncTextTopic, "five");
        await client.SimulateMessageReceivedAsync(asyncBytesTopic, "six");

        await Assert.That(lines).Contains("al:four");
        await Assert.That(ContainsBatch(batches, "at:five")).IsTrue();
        await Assert.That(ContainsBatch(batches, "ab:six")).IsTrue();
    }
}
