// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using IoT.Driver.ABPlcRx;
using IoT.Driver.Core;
using MQTTnet.Protocol;
using MQTTnet.Rx.ABPlc;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Concurrency;
using ReactiveUI.Primitives.Reactive.Signals;
using AbAsyncCreate = MQTTnet.Rx.ABPlc.ObservableAsyncCreateExtensions;
using AbCreate = MQTTnet.Rx.ABPlc.Create;
using MqttCreate = MQTTnet.Rx.Client.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises Allen-Bradley MQTT bridges over real loopback network transports.</summary>
public sealed class ABPlcLiveBridgeTests
{
    /// <summary>The registered PLC group used by every simulator fixture.</summary>
    private const string Group = "LiveBridge";

    /// <summary>The logical PLC variable published to MQTT.</summary>
    private const string OutboundVariable = "OutboundValue";

    /// <summary>The physical simulator tag published to MQTT.</summary>
    private const string OutboundPhysicalTag = "Program:Live.Outbound";

    /// <summary>The logical PLC variable written from MQTT.</summary>
    private const string InboundVariable = "InboundValue";

    /// <summary>The physical simulator tag written from MQTT.</summary>
    private const string InboundPhysicalTag = "Program:Live.Inbound";

    /// <summary>The maximum duration allowed for an event-driven live operation.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Proves synchronous raw static and extension AB bridges in both network directions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawSynchronousSurfaces_RoundTripThroughLiveBrokerAsync()
    {
        const string outboundTopic = "tests/ab/raw/sync/outbound";
        const string inboundTopic = "tests/ab/raw/sync/inbound";
        const int outboundValue = 137;
        const int inboundValue = 241;

        var broker = await LiveMqttBroker.StartAsync();
        var simulator = CreateSimulator(outboundValue);
        var plc = new WriteObservedPlc(simulator);
        try
        {
            _ = await broker.ConnectClientsAsync();
            await using var probeSubscription = await broker.SubscribeProbeAsync(outboundTopic);

            var publishResultTask = AbCreate
                .PublishABPlcTag<int>(broker.Bridge, outboundTopic, OutboundVariable, plc)
                .FirstAsync(OperationTimeout);
            var publishResult = await publishResultTask;
            var outboundMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

            await AssertOutboundAsync(simulator, outboundTopic, outboundValue, outboundMessage);
            await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);

            var inboundPublish = await PublishRetainedFromProbeAsync(broker, inboundTopic, inboundValue);
            using var bridgeSubscription = broker.Bridge.SubscribeABPlcTag(
                inboundTopic,
                InboundVariable,
                plc,
                int.Parse);

            await AssertInboundAsync(
                simulator,
                inboundPublish,
                plc.Written,
                inboundValue);
            bridgeSubscription.Dispose();
        }
        finally
        {
            simulator.Dispose();
            await broker.DisposeAsync();
        }

        await AssertDisposedResourcesAsync(simulator, broker);
    }

    /// <summary>Proves asynchronous raw extension and static AB bridges in both network directions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RawAsynchronousSurfaces_RoundTripThroughLiveBrokerAsync()
    {
        const string outboundTopic = "tests/ab/raw/async/outbound";
        const string inboundTopic = "tests/ab/raw/async/inbound";
        const int outboundValue = 353;
        const int inboundValue = 467;

        var broker = await LiveMqttBroker.StartAsync();
        var simulator = CreateSimulator(outboundValue);
        var plc = new WriteObservedPlc(simulator);
        try
        {
            _ = await broker.ConnectClientsAsync();
            await using var probeSubscription = await broker.SubscribeProbeAsync(outboundTopic);
            var asyncBridge = SignalAsync.Return(broker.BridgeClient);

            var publishResult = await asyncBridge
                .PublishABPlcTag<int>(outboundTopic, OutboundVariable, plc)
                .FirstAsync(OperationTimeout);
            var outboundMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

            await AssertOutboundAsync(simulator, outboundTopic, outboundValue, outboundMessage);
            await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);

            var inboundPublish = await PublishRetainedFromProbeAsync(broker, inboundTopic, inboundValue);
            using var bridgeSubscription = AbAsyncCreate.SubscribeABPlcTag(
                asyncBridge,
                inboundTopic,
                InboundVariable,
                plc,
                int.Parse);

            await AssertInboundAsync(
                simulator,
                inboundPublish,
                plc.Written,
                inboundValue);
        }
        finally
        {
            simulator.Dispose();
            await broker.DisposeAsync();
        }

        await AssertDisposedResourcesAsync(simulator, broker);
    }

    /// <summary>Proves synchronous resilient static and extension AB bridges in both network directions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientSynchronousSurfaces_RoundTripThroughLiveBrokerAsync()
    {
        const string outboundTopic = "tests/ab/resilient/sync/outbound";
        const string inboundTopic = "tests/ab/resilient/sync/inbound";
        const int outboundValue = 571;
        const int inboundValue = 683;

        var broker = await LiveMqttBroker.StartAsync();
        var simulator = CreateSimulator(outboundValue);
        var plc = new WriteObservedPlc(simulator);
        OwnedResilientClient? resilientOwner = null;
        try
        {
            _ = await broker.ConnectClientsAsync();
            resilientOwner = await OwnedResilientClient.ConnectAsync(broker.Port);
            await using var probeSubscription = await broker.SubscribeProbeAsync(outboundTopic);
            var resilientBridge = Signal.Emit(resilientOwner.Client);

            var processed = await AbCreate
                .PublishABPlcTag<int>(resilientBridge, outboundTopic, OutboundVariable, plc)
                .FirstAsync(OperationTimeout);
            var outboundMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

            await AssertOutboundAsync(simulator, outboundTopic, outboundValue, outboundMessage);
            await Assert.That(processed.Exception).IsNull();

            var inboundPublish = await PublishRetainedFromProbeAsync(broker, inboundTopic, inboundValue);
            using var bridgeSubscription = resilientBridge.SubscribeABPlcTag(
                inboundTopic,
                InboundVariable,
                plc,
                int.Parse);

            await AssertInboundAsync(
                simulator,
                inboundPublish,
                plc.Written,
                inboundValue);
        }
        finally
        {
            await DisposeResilientScenarioAsync(resilientOwner, simulator, broker);
        }

        await AssertDisposedResourcesAsync(simulator, broker, resilientOwner);
    }

    /// <summary>Proves asynchronous resilient extension and static AB bridges in both network directions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientAsynchronousSurfaces_RoundTripThroughLiveBrokerAsync()
    {
        const string outboundTopic = "tests/ab/resilient/async/outbound";
        const string inboundTopic = "tests/ab/resilient/async/inbound";
        const int outboundValue = 797;
        const int inboundValue = 809;

        var broker = await LiveMqttBroker.StartAsync();
        var simulator = CreateSimulator(outboundValue);
        var plc = new WriteObservedPlc(simulator);
        OwnedResilientClient? resilientOwner = null;
        try
        {
            _ = await broker.ConnectClientsAsync();
            resilientOwner = await OwnedResilientClient.ConnectAsync(broker.Port);
            await using var probeSubscription = await broker.SubscribeProbeAsync(outboundTopic);
            var asyncBridge = SignalAsync.Return(resilientOwner.Client);

            var processed = await asyncBridge
                .PublishABPlcTag<int>(outboundTopic, OutboundVariable, plc)
                .FirstAsync(OperationTimeout);
            var outboundMessage = await probeSubscription.MessageReceived.WaitAsync(OperationTimeout);

            await AssertOutboundAsync(simulator, outboundTopic, outboundValue, outboundMessage);
            await Assert.That(processed.Exception).IsNull();

            var inboundPublish = await PublishRetainedFromProbeAsync(broker, inboundTopic, inboundValue);
            using var bridgeSubscription = AbAsyncCreate.SubscribeABPlcTag(
                asyncBridge,
                inboundTopic,
                InboundVariable,
                plc,
                int.Parse);

            await AssertInboundAsync(
                simulator,
                inboundPublish,
                plc.Written,
                inboundValue);
        }
        finally
        {
            await DisposeResilientScenarioAsync(resilientOwner, simulator, broker);
        }

        await AssertDisposedResourcesAsync(simulator, broker, resilientOwner);
    }

    /// <summary>Exercises the resilient null-client guards that precede every AB bridge operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientSurfaces_RejectNullClientSequencesAsync()
    {
        using var simulator = CreateSimulator(1);
        IObservable<IResilientMqttClient> nullClient = null!;
        IObservableAsync<IResilientMqttClient> nullAsyncClient = null!;

        await Assert.That(() => AbCreate.PublishABPlcTag<int>(
            nullClient,
            "tests/ab/null/sync/publish",
            OutboundVariable,
            simulator)).Throws<ArgumentNullException>();
        await Assert.That(() => AbCreate.SubscribeABPlcTag(
            nullClient,
            "tests/ab/null/sync/subscribe",
            InboundVariable,
            simulator,
            int.Parse)).Throws<ArgumentNullException>();
        await Assert.That(() => AbAsyncCreate.PublishABPlcTag<int>(
            nullAsyncClient,
            "tests/ab/null/async/publish",
            OutboundVariable,
            simulator)).Throws<ArgumentNullException>();
        await Assert.That(() => AbAsyncCreate.SubscribeABPlcTag(
            nullAsyncClient,
            "tests/ab/null/async/subscribe",
            InboundVariable,
            simulator,
            int.Parse)).Throws<ArgumentNullException>();
    }

    /// <summary>Creates a simulator with registered tags and a committed outbound value.</summary>
    /// <param name="outboundValue">The value to publish when the outbound bridge subscribes.</param>
    /// <returns>A configured deterministic PLC simulator.</returns>
    private static ABPlcSimulator CreateSimulator(int outboundValue)
    {
        var simulator = new ABPlcSimulator(PlcType.LGX);
        simulator.ScanEnabled = false;
        simulator.AddUpdateTagItem(OutboundVariable, OutboundPhysicalTag, Group, 0);
        simulator.AddUpdateTagItem(InboundVariable, InboundPhysicalTag, Group, 0);
        simulator.SetTagValue(OutboundPhysicalTag, outboundValue);
        simulator.SetTagValue(InboundPhysicalTag, 0);
        _ = simulator.Read(OutboundVariable);
        _ = simulator.Read(InboundVariable);
        return simulator;
    }

    /// <summary>Publishes a retained integer payload from the real probe client.</summary>
    /// <param name="broker">The live broker fixture.</param>
    /// <param name="topic">The target topic.</param>
    /// <param name="value">The integer payload.</param>
    /// <returns>The broker's PUBACK result.</returns>
    private static Task<MqttClientPublishResult> PublishRetainedFromProbeAsync(
        LiveMqttBroker broker,
        string topic,
        int value)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(value.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();
        return broker.ProbeClient.PublishAsync(message, CancellationToken.None);
    }

    /// <summary>Asserts the PLC-to-MQTT path and simulator read evidence.</summary>
    /// <param name="simulator">The simulator that emitted the tag value.</param>
    /// <param name="topic">The exact MQTT topic.</param>
    /// <param name="expectedValue">The expected PLC and MQTT value.</param>
    /// <param name="message">The message captured by the real probe client.</param>
    /// <returns>A task that represents the asynchronous assertions.</returns>
    private static async Task AssertOutboundAsync(
        ABPlcSimulator simulator,
        string topic,
        int expectedValue,
        LiveMqttMessage message)
    {
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(expectedValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Assert.That(simulator.GetValue(OutboundVariable, 0, -1)).IsEqualTo(expectedValue);
        await Assert.That(simulator.OperationMetrics.ReadOperations).IsGreaterThan(0L);
    }

    /// <summary>Asserts the MQTT-to-PLC path and exact simulator write evidence.</summary>
    /// <param name="simulator">The simulator written by the bridge.</param>
    /// <param name="publishResult">The probe client's PUBACK.</param>
    /// <param name="observedValue">The event-driven PLC observation.</param>
    /// <param name="expectedValue">The expected physical PLC value.</param>
    /// <returns>A task that represents the asynchronous assertions.</returns>
    private static async Task AssertInboundAsync(
        ABPlcSimulator simulator,
        MqttClientPublishResult publishResult,
        Task<int> observedValue,
        int expectedValue)
    {
        await Assert.That(publishResult.ReasonCode is
            MqttClientPublishReasonCode.Success or
            MqttClientPublishReasonCode.NoMatchingSubscribers).IsTrue();
        await Assert.That(await observedValue.WaitAsync(OperationTimeout)).IsEqualTo(expectedValue);
        await Assert.That(simulator.GetTagValue<int>(InboundPhysicalTag, default)).IsEqualTo(expectedValue);
        await Assert.That(simulator.OperationMetrics.WriteOperations).IsGreaterThan(0L);
        var successfulWriteRecorded = false;
        foreach (var entry in simulator.OperationLog)
        {
            if (entry.Operation == ABPlcSimulatorOperation.Write
                && string.Equals(entry.TagName, InboundPhysicalTag, StringComparison.Ordinal)
                && entry.StatusCode == PlcTagStatus.StatusOK)
            {
                successfulWriteRecorded = true;
                break;
            }
        }

        await Assert.That(successfulWriteRecorded).IsTrue();
    }

    /// <summary>Asserts that all resources involved in a live bridge scenario have been disposed.</summary>
    /// <param name="simulator">The PLC simulator used by the scenario.</param>
    /// <param name="broker">The live MQTT broker used by the scenario.</param>
    /// <param name="resilientOwner">The optional owner of the resilient MQTT client.</param>
    /// <returns>A task that represents the asynchronous assertions.</returns>
    private static async Task AssertDisposedResourcesAsync(
        ABPlcSimulator simulator,
        LiveMqttBroker broker,
        OwnedResilientClient? resilientOwner = null)
    {
        if (resilientOwner is not null)
        {
            await Assert.That(resilientOwner.IsDisposed).IsTrue();
        }

        await Assert.That(simulator.IsDisposed).IsTrue();
        await Assert.That(broker.IsDisposed).IsTrue();
        await Assert.That(broker.TeardownException).IsNull();
    }

    /// <summary>Disposes the optional resilient client and the resources shared by a live bridge scenario.</summary>
    /// <param name="resilientOwner">The optional owner of the resilient MQTT client.</param>
    /// <param name="simulator">The PLC simulator used by the scenario.</param>
    /// <param name="broker">The live MQTT broker used by the scenario.</param>
    /// <returns>A task that represents the asynchronous teardown.</returns>
    private static async Task DisposeResilientScenarioAsync(
        OwnedResilientClient? resilientOwner,
        ABPlcSimulator simulator,
        LiveMqttBroker broker)
    {
        if (resilientOwner is not null)
        {
            await resilientOwner.DisposeAsync();
        }

        simulator.Dispose();
        await broker.DisposeAsync();
    }

    /// <summary>Composes a PLC connection and exposes completion after its inbound write returns.</summary>
    /// <param name="inner">The live simulator connection to decorate.</param>
    private sealed class WriteObservedPlc(IABPlcRx inner) : IABPlcRx
    {
        /// <summary>Completes only after the decorated PLC has committed the inbound value.</summary>
        private readonly TaskCompletionSource<int> _written = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public bool IsDisposed => inner.IsDisposed;

        /// <inheritdoc/>
        public IObservable<IPlcTag?> ObserveAll => inner.ObserveAll;

        /// <inheritdoc/>
        public IObservableAsync<IPlcTag?> ObserveAllAsyncObservable => inner.ObserveAllAsyncObservable;

        /// <inheritdoc/>
        public bool ScanEnabled
        {
            get => inner.ScanEnabled;
            set => inner.ScanEnabled = value;
        }

        /// <inheritdoc/>
        public bool AutoWriteValue
        {
            get => inner.AutoWriteValue;
            set => inner.AutoWriteValue = value;
        }

        /// <summary>Gets a task that completes after the simulator write returns.</summary>
        public Task<int> Written => _written.Task;

        /// <inheritdoc/>
        public void AddUpdateTagItem<T>(string tagName, T? typeWitness) =>
            inner.AddUpdateTagItem(tagName, typeWitness);

        /// <inheritdoc/>
        public void AddUpdateTagItem<T>(string variable, string tagName, T? typeWitness) =>
            inner.AddUpdateTagItem(variable, tagName, typeWitness);

        /// <inheritdoc/>
        public void AddUpdateTagItem<T>(string variable, string tagName, string tagGroup, T? typeWitness) =>
            inner.AddUpdateTagItem(variable, tagName, tagGroup, typeWitness);

        /// <inheritdoc/>
        public bool RemoveTagItem(string variable) => inner.RemoveTagItem(variable);

        /// <inheritdoc/>
        public IObservable<T?> Observe<T>(string? variable, T? typeWitness, int bit) =>
            inner.Observe(variable, typeWitness, bit);

        /// <inheritdoc/>
        public IObservableAsync<T?> ObserveAsyncObservable<T>(string? variable, T? typeWitness, int bit) =>
            inner.ObserveAsyncObservable(variable, typeWitness, bit);

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<string, object?>> ObserveMany(params string[] variables) =>
            inner.ObserveMany(variables);

        /// <inheritdoc/>
        public IObservableAsync<IReadOnlyDictionary<string, object?>> ObserveManyAsyncObservable(
            params string[] variables) =>
            inner.ObserveManyAsyncObservable(variables);

        /// <inheritdoc/>
        public IObservable<IPlcTag> ObserveGroup(string groupName) => inner.ObserveGroup(groupName);

        /// <inheritdoc/>
        public IObservableAsync<IPlcTag> ObserveGroupAsyncObservable(string groupName) =>
            inner.ObserveGroupAsyncObservable(groupName);

        /// <inheritdoc/>
        public IObserver<T> CreateWriter<T>(string variable, T? typeWitness, int bit) =>
            inner.CreateWriter(variable, typeWitness, bit);

        /// <inheritdoc/>
        public IObservable<T?> ObserveSampled<T>(
            string variable,
            TimeSpan sampleInterval,
            T? typeWitness,
            int bit,
            ISequencer? scheduler) =>
            inner.ObserveSampled(variable, sampleInterval, typeWitness, bit, scheduler);

        /// <inheritdoc/>
        public IObservableAsync<T?> ObserveSampledAsyncObservable<T>(
            string variable,
            TimeSpan sampleInterval,
            T? typeWitness,
            int bit,
            ISequencer? scheduler) =>
            inner.ObserveSampledAsyncObservable(variable, sampleInterval, typeWitness, bit, scheduler);

        /// <inheritdoc/>
        public IObservable<PlcTagResult> ObserveErrors() => inner.ObserveErrors();

        /// <inheritdoc/>
        public IObservableAsync<PlcTagResult> ObserveErrorsAsyncObservable() =>
            inner.ObserveErrorsAsyncObservable();

        /// <inheritdoc/>
        public T? GetValue<T>(string? variable, T? typeWitness, int bit) =>
            inner.GetValue(variable, typeWitness, bit);

        /// <inheritdoc/>
        public void Value<T>(string? variable, T? value, int bit)
        {
            inner.Value(variable, value, bit);
            if (!string.Equals(variable, InboundVariable, StringComparison.Ordinal) || value is not int writtenValue)
            {
                return;
            }

            _ = _written.TrySetResult(writtenValue);
        }

        /// <inheritdoc/>
        public IEnumerable<PlcTagResult> Write() => inner.Write();

        /// <inheritdoc/>
        public PlcTagResult? Write(string? variable) => inner.Write(variable);

        /// <inheritdoc/>
        public IEnumerable<PlcTagResult> Read() => inner.Read();

        /// <inheritdoc/>
        public PlcTagResult? Read(string? variable) => inner.Read(variable);

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlcTagResult>> ReadManyAsync(
            IReadOnlyCollection<string> variables,
            CancellationToken cancellationToken) =>
            inner.ReadManyAsync(variables, cancellationToken);

        /// <inheritdoc/>
        public Task<IReadOnlyList<PlcTagResult>> WriteManyAsync(
            IReadOnlyDictionary<string, object?> values,
            CancellationToken cancellationToken) =>
            inner.WriteManyAsync(values, cancellationToken);

        /// <inheritdoc/>
        public Task<TagOperationResult<T>> ReadValueAsync<T>(
            string variable,
            T? typeWitness,
            int bit,
            CancellationToken cancellationToken) =>
            inner.ReadValueAsync(variable, typeWitness, bit, cancellationToken);

        /// <inheritdoc/>
        public Task<TagOperationResult<T>> WriteValueAsync<T>(
            string variable,
            T value,
            int bit,
            CancellationToken cancellationToken) =>
            inner.WriteValueAsync(variable, value, bit, cancellationToken);

        /// <inheritdoc/>
        public bool Ping(bool echo) => inner.Ping(echo);

        /// <inheritdoc/>
        public Task<bool> PingAsync(bool echo, CancellationToken cancellationToken) =>
            inner.PingAsync(echo, cancellationToken);

        /// <inheritdoc/>
        public IObservable<bool> ObservePing(TimeSpan interval, bool echo, ISequencer? scheduler) =>
            inner.ObservePing(interval, echo, scheduler);

        /// <inheritdoc/>
        public IObservableAsync<bool> ObservePingAsyncObservable(
            TimeSpan interval,
            bool echo,
            ISequencer? scheduler) =>
            inner.ObservePingAsyncObservable(interval, echo, scheduler);

        /// <inheritdoc/>
        public void Dispose() => inner.Dispose();
    }

    /// <summary>Owns a real resilient MQTT client and its source subscription.</summary>
    private sealed class OwnedResilientClient : IAsyncDisposable
    {
        /// <summary>The resilient client's reconnect delay in milliseconds.</summary>
        private const int ReconnectDelayMilliseconds = 25;

        /// <summary>The source subscription that owns the resilient client lifetime.</summary>
        private readonly IDisposable _owner;

        /// <summary>Initializes a new instance of the <see cref="OwnedResilientClient"/> class.</summary>
        /// <param name="client">The connected resilient client.</param>
        /// <param name="owner">The source subscription that owns the client.</param>
        private OwnedResilientClient(IResilientMqttClient client, IDisposable owner)
        {
            Client = client;
            _owner = owner;
        }

        /// <summary>Gets the connected resilient client.</summary>
        public IResilientMqttClient Client { get; }

        /// <summary>Gets a value indicating whether teardown completed.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>Creates and connects a resilient client to the live broker.</summary>
        /// <param name="port">The broker's OS-assigned loopback port.</param>
        /// <returns>An owner that keeps the resilient client alive.</returns>
        public static async Task<OwnedResilientClient> ConnectAsync(int port)
        {
            var clientSource = MqttCreate.ResilientMqttClient();
            var emitted = new TaskCompletionSource<IResilientMqttClient>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var owner = clientSource.Subscribe(new CaptureObserver<IResilientMqttClient>(emitted));
            var client = await emitted.Task.WaitAsync(OperationTimeout);
            var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<MqttClientConnectedEventArgs> connectedHandler = (_, _) =>
                _ = connected.TrySetResult();
            client.ConnectedEvent += connectedHandler;

            try
            {
                var reconnectDelay = TimeSpan.FromMilliseconds(ReconnectDelayMilliseconds);
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId($"ab-resilient-{Guid.NewGuid():N}")
                    .WithTcpServer(System.Net.IPAddress.Loopback.ToString(), port)
                    .Build();
                var options = new ResilientMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(reconnectDelay)
                    .WithClientOptions(clientOptions)
                    .Build();

                await client.StartAsync(options);
                if (client.IsConnected)
                {
                    _ = connected.TrySetResult();
                }

                await connected.Task.WaitAsync(OperationTimeout);
                return new(client, owner);
            }
            catch
            {
                owner.Dispose();
                throw;
            }
            finally
            {
                client.ConnectedEvent -= connectedHandler;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            try
            {
                if (Client.IsStarted)
                {
                    await Client.StopAsync();
                }
            }
            finally
            {
                _owner.Dispose();
            }
        }
    }

    /// <summary>Completes a task from a single observable value.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="completion">The task completion source to complete.</param>
    private sealed class CaptureObserver<T>(TaskCompletionSource<T> completion) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted() =>
            _ = completion.TrySetException(new InvalidOperationException("The source completed without a value."));

        /// <inheritdoc/>
        public void OnError(Exception error) => _ = completion.TrySetException(error);

        /// <inheritdoc/>
        public void OnNext(T value) => _ = completion.TrySetResult(value);
    }
}
