// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Buffers;
using System.Globalization;
using System.Text;
using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.TwinCAT;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using TwinCatAsync = MQTTnet.Rx.TwinCAT.ObservableAsyncCreateExtensions;
using TwinCatCreate = MQTTnet.Rx.TwinCAT.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains the end-to-end test portion of the TwinCAT live-broker bridge fixture.</summary>
public sealed partial class TwinCatLiveBrokerBridgeTests
{
    /// <summary>The simulated ADS symbol bridged by these tests.</summary>
    private const string AdsVariable = ".Main.Counter";

    /// <summary>The in-memory hash-table key bridged by these tests.</summary>
    private const string HashVariable = "Counter";

    /// <summary>The TwinCAT 3 runtime port used only as simulator metadata.</summary>
    private const int TwinCat3Port = 851;

    /// <summary>The number of ADS operations expected from the subscriber families.</summary>
    private const int ExpectedSubscriberOperations = 3;

    /// <summary>The raw static ADS value.</summary>
    private const int RawStaticAdsValue = 101;

    /// <summary>The raw static hash value.</summary>
    private const int RawStaticHashValue = 102;

    /// <summary>The raw extension ADS value.</summary>
    private const int RawExtensionAdsValue = 103;

    /// <summary>The raw extension hash value.</summary>
    private const int RawExtensionHashValue = 106;

    /// <summary>The raw async ADS value.</summary>
    private const int RawAsyncAdsValue = 104;

    /// <summary>The raw async hash value.</summary>
    private const int RawAsyncHashValue = 105;

    /// <summary>The resilient static ADS value.</summary>
    private const int ResilientStaticAdsValue = 201;

    /// <summary>The resilient static hash value.</summary>
    private const int ResilientStaticHashValue = 202;

    /// <summary>The resilient extension ADS value.</summary>
    private const int ResilientExtensionAdsValue = 203;

    /// <summary>The resilient extension hash value.</summary>
    private const int ResilientExtensionHashValue = 206;

    /// <summary>The resilient async ADS value.</summary>
    private const int ResilientAsyncAdsValue = 204;

    /// <summary>The resilient async hash value.</summary>
    private const int ResilientAsyncHashValue = 205;

    /// <summary>The raw static write value.</summary>
    private const int RawStaticWriteValue = 301;

    /// <summary>The raw extension write value.</summary>
    private const int RawExtensionWriteValue = 302;

    /// <summary>The raw async write value.</summary>
    private const int RawAsyncWriteValue = 303;

    /// <summary>The resilient static write value.</summary>
    private const int ResilientStaticWriteValue = 401;

    /// <summary>The resilient extension write value.</summary>
    private const int ResilientExtensionWriteValue = 402;

    /// <summary>The resilient async write value.</summary>
    private const int ResilientAsyncWriteValue = 403;

    /// <summary>The bounded duration for live broker evidence.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Proves static, extension, and async raw-client publishers carry ADS/hash values through the real broker.
    /// </summary>
    /// <returns>A task representing the live integration test.</returns>
    [Test]
    public async Task RawPublishers_CarryAdsAndHashValuesThroughRealBrokerAsync()
    {
        await using var broker = await StartConnectedBrokerAsync();
        using var ads = CreateAdsClient();
        using var hash = CreateHashTable();
        IRxTcAdsClient adsContract = ads;
        IHashTableRx hashContract = hash;
        var asyncClients = SignalAsync.Return(broker.BridgeClient);

        await AssertRawPublishAsync(
            broker,
            "twincat/raw/static/ads",
            RawStaticAdsValue,
            TwinCatCreate.PublishTcPlcTag(broker.Bridge, "twincat/raw/static/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertRawHashPublishAsync(
            broker,
            "twincat/raw/static/hash",
            0,
            RawStaticHashValue,
            TwinCatCreate.PublishTcPlcTag(broker.Bridge, "twincat/raw/static/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);
        await AssertRawPublishAsync(
            broker,
            "twincat/raw/extension/ads",
            RawExtensionAdsValue,
            broker.Bridge.PublishTcPlcTag("twincat/raw/extension/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertRawHashPublishAsync(
            broker,
            "twincat/raw/extension/hash",
            RawStaticHashValue,
            RawExtensionHashValue,
            broker.Bridge.PublishTcPlcTag("twincat/raw/extension/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);
        await AssertRawAsyncPublishAsync(
            broker,
            "twincat/raw/async/ads",
            RawAsyncAdsValue,
            TwinCatAsync.PublishTcPlcTag(asyncClients, "twincat/raw/async/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertRawAsyncHashPublishAsync(
            broker,
            "twincat/raw/async/hash",
            RawExtensionHashValue,
            RawAsyncHashValue,
            TwinCatAsync.PublishTcPlcTag(asyncClients, "twincat/raw/async/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);

        await Assert.That(ads.OperationMetrics.NotificationPublications)
            .IsGreaterThanOrEqualTo(ExpectedSubscriberOperations);
        await Assert.That(hash[HashVariable]).IsEqualTo(RawAsyncHashValue);
    }

    /// <summary>
    /// Proves static, extension, and async resilient publishers carry ADS/hash values through the real broker.
    /// </summary>
    /// <returns>A task representing the live integration test.</returns>
    [Test]
    public async Task ResilientPublishers_CarryAdsAndHashValuesThroughRealBrokerAsync()
    {
        await using var broker = await StartConnectedBrokerAsync();
        using var ads = CreateAdsClient();
        using var hash = CreateHashTable();
        using var processed = new ReactiveUI.Primitives.Signals.Signal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateLiveResilientClient(broker.BridgeClient, processed);
        IRxTcAdsClient adsContract = ads;
        IHashTableRx hashContract = hash;
        var clients = Signal.Emit(resilient);
        var asyncClients = SignalAsync.Return(resilient);

        await AssertResilientPublishAsync(
            broker,
            "twincat/resilient/static/ads",
            ResilientStaticAdsValue,
            TwinCatCreate.PublishTcPlcTag(clients, "twincat/resilient/static/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertResilientHashPublishAsync(
            broker,
            "twincat/resilient/static/hash",
            0,
            ResilientStaticHashValue,
            TwinCatCreate.PublishTcPlcTag(clients, "twincat/resilient/static/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);
        await AssertResilientPublishAsync(
            broker,
            "twincat/resilient/extension/ads",
            ResilientExtensionAdsValue,
            clients.PublishTcPlcTag("twincat/resilient/extension/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertResilientHashPublishAsync(
            broker,
            "twincat/resilient/extension/hash",
            ResilientStaticHashValue,
            ResilientExtensionHashValue,
            clients.PublishTcPlcTag("twincat/resilient/extension/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);
        await AssertResilientAsyncPublishAsync(
            broker,
            "twincat/resilient/async/ads",
            ResilientAsyncAdsValue,
            TwinCatAsync.PublishTcPlcTag(asyncClients, "twincat/resilient/async/ads", AdsVariable, adsContract, -1),
            value => ads.SetValue(AdsVariable, value));
        await AssertResilientAsyncHashPublishAsync(
            broker,
            "twincat/resilient/async/hash",
            ResilientExtensionHashValue,
            ResilientAsyncHashValue,
            TwinCatAsync.PublishTcPlcTag(asyncClients, "twincat/resilient/async/hash", HashVariable, hashContract, -1),
            value => hash[HashVariable] = value);

        await Assert.That(ads.OperationMetrics.NotificationPublications)
            .IsGreaterThanOrEqualTo(ExpectedSubscriberOperations);
        await Assert.That(hash[HashVariable]).IsEqualTo(ResilientAsyncHashValue);
        resilient.Dispose();
    }

    /// <summary>
    /// Proves raw static, extension, and async subscribers write broker payloads into the in-memory ADS client.
    /// </summary>
    /// <returns>A task representing the live integration test.</returns>
    [Test]
    public async Task RawSubscribers_WriteProbePayloadIntoAdsAndDisposeCleanlyAsync()
    {
        await using var broker = await StartConnectedBrokerAsync();
        using var ads = CreateAdsClient();
        IRxTcAdsClient adsContract = ads;

        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/raw/static/write",
            RawStaticWriteValue,
            () =>
            {
                TwinCatCreate.SubscribeTcTag(
                    broker.Bridge,
                    "twincat/raw/static/write",
                    AdsVariable,
                    adsContract,
                    ParsePayload);
                return null;
            },
            proveDisposal: false);
        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/raw/extension/write",
            RawExtensionWriteValue,
            () => broker.Bridge.SubscribeTcTag("twincat/raw/extension/write", AdsVariable, adsContract, ParsePayload),
            proveDisposal: true);
        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/raw/async/write",
            RawAsyncWriteValue,
            () => TwinCatAsync.SubscribeTcTag(
                SignalAsync.Return(broker.BridgeClient),
                "twincat/raw/async/write",
                AdsVariable,
                adsContract,
                ParsePayload),
            proveDisposal: true);

        await Assert.That(ads.OperationMetrics.WriteOperations).IsEqualTo(ExpectedSubscriberOperations);
    }

    /// <summary>
    /// Proves resilient static, extension, and async subscribers write broker payloads into the in-memory ADS client.
    /// </summary>
    /// <returns>A task representing the live integration test.</returns>
    [Test]
    public async Task ResilientSubscribers_WriteProbePayloadIntoAdsAndDisposeCleanlyAsync()
    {
        await using var broker = await StartConnectedBrokerAsync();
        using var ads = CreateAdsClient();
        using var processed = new ReactiveUI.Primitives.Signals.Signal<ApplicationMessageProcessedEventArgs>();
        var resilient = CreateLiveResilientClient(broker.BridgeClient, processed);
        IRxTcAdsClient adsContract = ads;
        var clients = Signal.Emit(resilient);
        var asyncClients = SignalAsync.Return(resilient);

        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/resilient/static/write",
            ResilientStaticWriteValue,
            () =>
            {
                TwinCatCreate.SubscribeTcTag(
                    clients,
                    "twincat/resilient/static/write",
                    AdsVariable,
                    adsContract,
                    ParsePayload);
                return null;
            },
            proveDisposal: false);
        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/resilient/extension/write",
            ResilientExtensionWriteValue,
            () => clients.SubscribeTcTag("twincat/resilient/extension/write", AdsVariable, adsContract, ParsePayload),
            proveDisposal: true);
        await AssertAdsWriteAsync(
            broker,
            ads,
            "twincat/resilient/async/write",
            ResilientAsyncWriteValue,
            () => TwinCatAsync.SubscribeTcTag(
                asyncClients,
                "twincat/resilient/async/write",
                AdsVariable,
                adsContract,
                ParsePayload),
            proveDisposal: true);

        await Assert.That(ads.OperationMetrics.WriteOperations).IsEqualTo(ExpectedSubscriberOperations);
        resilient.Dispose();
    }

    /// <summary>Closes every null-validation branch on synchronous and asynchronous TwinCAT extensions.</summary>
    /// <returns>A task representing the validation test.</returns>
    [Test]
    public async Task AllBridgeFamilies_ValidateClientAndDriverDependenciesAsync()
    {
        using var ads = CreateAdsClient();
        using var hash = CreateHashTable();
        IRxTcAdsClient adsContract = ads;
        IHashTableRx hashContract = hash;
        await ValidateRawDependenciesAsync(adsContract, hashContract);
        await ValidateResilientDependenciesAsync(adsContract, hashContract);
        await ValidateAsyncRawDependenciesAsync(adsContract, hashContract);
        await ValidateAsyncResilientDependenciesAsync(adsContract, hashContract);
    }

    /// <summary>
    /// Captures the Behavior-style current hash value and the following mutation as separate broker messages.
    /// </summary>
    /// <typeparam name="TResult">The MQTT publication result type.</typeparam>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The unique MQTT topic.</param>
    /// <param name="value">The value supplied to the mutation.</param>
    /// <param name="publishResults">The TwinCAT bridge result stream.</param>
    /// <param name="trigger">The hash mutation that starts the second flow.</param>
    /// <returns>The two publication results and their corresponding broker messages.</returns>
    private static async Task<(
        TResult InitialResult,
        TResult MutationResult,
        LiveMqttMessage InitialMessage,
        LiveMqttMessage MutationMessage)>
        CaptureHashPublicationsAsync<TResult>(
            LiveMqttBroker broker,
            string topic,
            int value,
            IObservable<TResult> publishResults,
            Action<int> trigger)
    {
        await using var probe = await broker.SubscribeProbeAsync(topic);
        var initialResult = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mutationResult = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var publicationIndex = 0;
        using var bridge = publishResults.Subscribe(
            result =>
            {
                if (Interlocked.Increment(ref publicationIndex) == 1)
                {
                    _ = initialResult.TrySetResult(result);
                }
                else
                {
                    _ = mutationResult.TrySetResult(result);
                }
            },
            error =>
            {
                _ = initialResult.TrySetException(error);
                _ = mutationResult.TrySetException(error);
            });

        var currentResult = await initialResult.Task.WaitAsync(OperationTimeout);
        var currentMessage = await probe.MessageReceived.WaitAsync(OperationTimeout);
        var mutationMessage = new TaskCompletionSource<LiveMqttMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Func<MqttApplicationMessageReceivedEventArgs, Task> mutationHandler = eventArgs =>
            RecordMutationMessageAsync(eventArgs, topic, mutationMessage);

        broker.ProbeClient.ApplicationMessageReceivedAsync += mutationHandler;
        try
        {
            trigger(value);
            var changedResult = await mutationResult.Task.WaitAsync(OperationTimeout);
            var changedMessage = await mutationMessage.Task.WaitAsync(OperationTimeout);
            return (currentResult, changedResult, currentMessage, changedMessage);
        }
        finally
        {
            broker.ProbeClient.ApplicationMessageReceivedAsync -= mutationHandler;
        }
    }

    /// <summary>Verifies one exact topic and invariant integer payload observed by the live probe.</summary>
    /// <param name="message">The captured live broker message.</param>
    /// <param name="topic">The expected exact topic.</param>
    /// <param name="value">The expected integer payload.</param>
    /// <returns>A task representing the TUnit assertions.</returns>
    private static async Task AssertPublishedMessageAsync(LiveMqttMessage message, string topic, int value)
    {
        await Assert.That(message.Topic).IsEqualTo(topic);
        await Assert.That(Encoding.UTF8.GetString(message.Payload))
            .IsEqualTo(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Records a probe message that belongs to the requested topic.</summary>
    /// <param name="eventArgs">The received MQTT message event arguments.</param>
    /// <param name="topic">The expected MQTT topic.</param>
    /// <param name="mutationMessage">The completion source receiving the captured message.</param>
    /// <returns>A completed task after recording the matching message.</returns>
    private static Task RecordMutationMessageAsync(
        MqttApplicationMessageReceivedEventArgs eventArgs,
        string topic,
        TaskCompletionSource<LiveMqttMessage> mutationMessage)
    {
        if (string.Equals(eventArgs.ApplicationMessage.Topic, topic, StringComparison.Ordinal))
        {
            _ = mutationMessage.TrySetResult(
                new(
                    eventArgs.ApplicationMessage.Topic,
                    eventArgs.ApplicationMessage.Payload.ToArray()));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes from the probe, waits for ADS write evidence, and optionally proves subscription disposal.
    /// </summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="ads">The in-memory ADS client that records the write.</param>
    /// <param name="topic">The unique MQTT write topic.</param>
    /// <param name="value">The value to publish and verify.</param>
    /// <param name="subscribe">The TwinCAT bridge subscription factory.</param>
    /// <param name="proveDisposal">Whether to prove a disposed subscription ignores a second delivered payload.</param>
    /// <returns>A task representing the broker-to-ADS evidence flow.</returns>
    private static async Task AssertAdsWriteAsync(
        LiveMqttBroker broker,
        InMemoryAdsClient ads,
        string topic,
        int value,
        Func<IDisposable?> subscribe,
        bool proveDisposal)
    {
        var directSubscription = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic).Build();
        _ = await broker.BridgeClient.SubscribeAsync(directSubscription, CancellationToken.None);
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var writeEvidence = ads.OnWrite.Subscribe(_ => written.TrySetResult());
        var subscription = subscribe();
        await Task.Yield();

        await PublishFromProbeAsync(broker, topic, value);
        await written.Task.WaitAsync(OperationTimeout);
        await Assert.That(ads.TryGetValue<int>(AdsVariable, out var actual)).IsTrue();
        await Assert.That(actual).IsEqualTo(value);

        if (!proveDisposal)
        {
            return;
        }

        subscription!.Dispose();
        subscription.Dispose();
        var barrierTopic = $"{topic}/disposed-barrier";
        var barrierReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task BarrierEvidenceAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            if (string.Equals(args.ApplicationMessage.Topic, barrierTopic, StringComparison.Ordinal))
            {
                _ = barrierReached.TrySetResult();
            }

            return Task.CompletedTask;
        }

        broker.BridgeClient.ApplicationMessageReceivedAsync += BarrierEvidenceAsync;
        try
        {
            var barrierSubscription = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(barrierTopic).Build();
            _ = await broker.BridgeClient.SubscribeAsync(barrierSubscription, CancellationToken.None);
            await PublishFromProbeAsync(broker, topic, value + 1);
            await PublishFromProbeAsync(broker, barrierTopic, value);
            await barrierReached.Task.WaitAsync(OperationTimeout);
        }
        finally
        {
            broker.BridgeClient.ApplicationMessageReceivedAsync -= BarrierEvidenceAsync;
        }

        await Assert.That(ads.TryGetValue<int>(AdsVariable, out var afterDisposal)).IsTrue();
        await Assert.That(afterDisposal).IsEqualTo(value);
    }
}
#endif
