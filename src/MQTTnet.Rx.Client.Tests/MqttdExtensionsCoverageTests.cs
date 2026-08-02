// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides focused branch coverage for MQTT daemon publish and subscribe extensions.</summary>
public sealed class MqttdExtensionsCoverageTests
{
    /// <summary>Defines the short delay that permits asynchronous MQTT subscriptions to start.</summary>
    private const int SubscriptionStartDelayMilliseconds = 100;

    /// <summary>Defines the expected number of raw publish operations exercised by the overload test.</summary>
    private const int RawPublishOperationCount = 10;

    /// <summary>Defines the expected number of resilient publish notifications.</summary>
    private const int ResilientPublishNotificationCount = 6;

    /// <summary>Defines the payload text shared by publish and receive operations.</summary>
    private const string PayloadText = "payload";

    /// <summary>Defines the expected observed dictionary value.</summary>
    private const int ExpectedObservedValue = 7;

    /// <summary>Defines the number of dictionary conversion results.</summary>
    private const int DictionaryResultCount = 4;

    /// <summary>Defines the integer represented by the nested JSON payload.</summary>
    private const long ExpectedNestedInteger = 2L;

    /// <summary>Defines the decimal represented by the nested JSON payload.</summary>
    private const double ExpectedNestedDecimal = 2.5D;

    /// <summary>Defines the expected number of matching messages.</summary>
    private const int ExpectedMatchingMessageCount = 2;

    /// <summary>Defines the discovery lookback window in hours.</summary>
    private const int DiscoveryWindowHours = 2;

    /// <summary>Defines a matching MQTT topic.</summary>
    private const string MatchingTopic = "coverage/devices/one";

    /// <summary>Defines a non-matching MQTT topic.</summary>
    private const string NonMatchingTopic = "coverage/other/one";

    /// <summary>Defines the topic filter applied by filter tests.</summary>
    private const string DeviceTopicFilter = "coverage/devices/+";

    /// <summary>Defines the expected final byte payload.</summary>
    private static readonly byte[] ExpectedFinalBytePayload = [4];

    /// <summary>Defines the first byte payload.</summary>
    private static readonly byte[] FirstBytePayload = [1];

    /// <summary>Defines the second byte payload.</summary>
    private static readonly byte[] SecondBytePayload = [2];

    /// <summary>Defines the third byte payload.</summary>
    private static readonly byte[] ThirdBytePayload = [3];

    /// <summary>Tests every raw-client publish overload and both payload builders.</summary>
    /// <returns>A task that completes after publish operations are observed.</returns>
    [Test]
    public async Task PublishMessage_RawClientOverloads_CreateConfiguredMessagesAsync()
    {
        // Arrange
        const string jsonContentType = "application/json";
        var client = new MockMqttClient();
        var rawClients = Signal.Emit<IMqttClient>(client);
        using var subscriptions = new MultipleDisposable
        {
            rawClients.PublishMessage(Signal.Emit(("coverage/raw/default-string", PayloadText))).Subscribe(),
        };

        // Act
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/qos-string", PayloadText)),
            MqttQualityOfServiceLevel.AtLeastOnce).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/full-string", PayloadText)),
            MqttQualityOfServiceLevel.AtMostOnce,
            retain: false).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/builder-default-string", PayloadText)),
            static builder => _ = builder.WithContentType(jsonContentType)).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/builder-qos-string", PayloadText)),
            static builder => _ = builder.WithContentType(jsonContentType),
            MqttQualityOfServiceLevel.AtLeastOnce).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/builder-full-string", PayloadText)),
            static builder => _ = builder.WithContentType(jsonContentType),
            MqttQualityOfServiceLevel.AtMostOnce,
            retain: false).Subscribe());
        subscriptions.Add(rawClients
            .PublishMessage(Signal.Emit(("coverage/raw/default-bytes", FirstBytePayload)))
            .Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/qos-bytes", SecondBytePayload)),
            MqttQualityOfServiceLevel.AtLeastOnce).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/builder-default-bytes", ThirdBytePayload)),
            static builder => _ = builder.WithContentType(jsonContentType)).Subscribe());
        subscriptions.Add(rawClients.PublishMessage(
            Signal.Emit(("coverage/raw/builder-full-bytes", ExpectedFinalBytePayload)),
            static builder => _ = builder.WithContentType(jsonContentType),
            MqttQualityOfServiceLevel.AtMostOnce,
            retain: false).Subscribe());
        await Task.Delay(SubscriptionStartDelayMilliseconds);

        // Assert
        await Assert.That(client.PublishedMessages).Count().IsEqualTo(RawPublishOperationCount);
        await Assert.That(client.PublishedMessages[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
        await Assert.That(client.PublishedMessages[2].Retain).IsFalse();
        await Assert.That(client.PublishedMessages[3].ContentType).IsEqualTo(jsonContentType);
        await Assert.That(client.PublishedMessages[9].Payload.ToArray()).IsEquivalentTo(ExpectedFinalBytePayload);
    }

    /// <summary>Tests every resilient-client publish overload and processed-message forwarding.</summary>
    /// <returns>A task that completes after processed-message notifications are observed.</returns>
    [Test]
    public async Task PublishMessage_ResilientClientOverloads_ForwardProcessedEventsAsync()
    {
        // Arrange
        var client = new MockResilientMqttClient();
        var resilientClients = Signal.Emit<IResilientMqttClient>(client);
        var processed = new List<ApplicationMessageProcessedEventArgs>();
        using var subscriptions = new MultipleDisposable
        {
            resilientClients
                .PublishMessage(Signal.Emit(("coverage/resilient/default-string", PayloadText)))
                .Subscribe(processed.Add),
        };

        // Act
        subscriptions.Add(resilientClients.PublishMessage(
            Signal.Emit(("coverage/resilient/qos-string", PayloadText)),
            MqttQualityOfServiceLevel.AtLeastOnce).Subscribe(processed.Add));
        subscriptions.Add(resilientClients.PublishMessage(
            Signal.Emit(("coverage/resilient/full-string", PayloadText)),
            MqttQualityOfServiceLevel.AtMostOnce,
            retain: false).Subscribe(processed.Add));
        subscriptions.Add(resilientClients.PublishMessage(
            Signal.Emit(("coverage/resilient/default-bytes", FirstBytePayload))).Subscribe(processed.Add));
        subscriptions.Add(resilientClients.PublishMessage(
            Signal.Emit(("coverage/resilient/qos-bytes", SecondBytePayload)),
            MqttQualityOfServiceLevel.AtLeastOnce).Subscribe(processed.Add));
        subscriptions.Add(resilientClients.PublishMessage(
            Signal.Emit(("coverage/resilient/full-bytes", ThirdBytePayload)),
            MqttQualityOfServiceLevel.AtMostOnce,
            retain: false).Subscribe(processed.Add));
        await Task.Delay(SubscriptionStartDelayMilliseconds);
        await client.SimulateApplicationMessageProcessedAsync();

        // Assert
        await Assert.That(processed).Count().IsEqualTo(ResilientPublishNotificationCount);
    }

    /// <summary>Tests JSON conversions, failure paths, topic filtering, and cached dictionary observation.</summary>
    /// <returns>A task that completes after all synchronous observable values are collected.</returns>
    [Test]
    public async Task SubscribePayloadExtensions_ConvertFilterAndObserveAllDataShapesAsync()
    {
        // Arrange
        const string observedKey = "coverage-observed-value";
        const string nestedJson =
            "{\"text\":\"value\",\"integer\":2,\"decimal\":2.5,\"enabled\":true,\"disabled\":false,"
            + "\"empty\":null,\"array\":[1],\"object\":{\"child\":\"value\"}}";
        using var dictionaries = new TestSignal<Dictionary<string, object>>();
        var observedValues = new List<object?>();
        var dictionariesResult = new List<Dictionary<string, object?>?>();
        var typedResult = new List<CoveragePayload?>();
        var filtered = new List<MqttApplicationMessageReceivedEventArgs>();
        using var subscriptions = new MultipleDisposable
        {
            dictionaries.Observe(observedKey).Subscribe(observedValues.Add),
        };

        // Act
        dictionaries.OnNext(new());
        dictionaries.OnNext(new() { [observedKey] = ExpectedObservedValue });
        subscriptions.Add(new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, string.Empty),
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, "not-json"),
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, "[]"),
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, nestedJson),
        }.ToObservable().ToDictionary().Subscribe(dictionariesResult.Add));
        subscriptions.Add(Signal
            .Emit(TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, "not-json"))
            .ToObject(static json => JsonSerializer.Deserialize<CoveragePayload>(json))
            .Subscribe(typedResult.Add));
        subscriptions.Add(new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, PayloadText),
            TestDataHelpers.CreateMessageReceivedArgs(NonMatchingTopic, PayloadText),
            TestDataHelpers.CreateMessageReceivedArgs(MatchingTopic, PayloadText),
        }.ToObservable().WhereTopicIsMatch(DeviceTopicFilter).Subscribe(filtered.Add));

        // Assert
        await Assert.That(observedValues).Count().IsEqualTo(1);
        await Assert.That(observedValues[0]).IsEqualTo(ExpectedObservedValue);
        await Assert.That(dictionariesResult).Count().IsEqualTo(DictionaryResultCount);
        await Assert.That(dictionariesResult[0]).IsNull();
        await Assert.That(dictionariesResult[1]).IsNull();
        await Assert.That(dictionariesResult[2]).IsNull();
        await Assert.That(dictionariesResult[3]!["integer"]).IsEqualTo(ExpectedNestedInteger);
        await Assert.That(dictionariesResult[3]!["decimal"]).IsEqualTo(ExpectedNestedDecimal);
        await Assert.That(dictionariesResult[3]!["enabled"] is true).IsTrue();
        await Assert.That(dictionariesResult[3]!["disabled"] is false).IsTrue();
        await Assert.That(dictionariesResult[3]!["empty"]).IsNull();
        await Assert.That(dictionariesResult[3]!["array"]).IsTypeOf<List<object?>>();
        await Assert.That(dictionariesResult[3]!["object"]).IsTypeOf<Dictionary<string, object?>>();
        await Assert.That(typedResult[0]).IsNull();
        await Assert.That(filtered).Count().IsEqualTo(ExpectedMatchingMessageCount);
    }

    /// <summary>Tests raw and resilient subscriptions, topic discovery overloads, and invalid arguments.</summary>
    /// <returns>A task that completes after MQTT message events are delivered.</returns>
    [Test]
    public async Task SubscribeToTopicAndDiscoverTopics_RawAndResilient_ForwardMatchingMessagesAsync()
    {
        // Arrange
        var rawClient = new MockMqttClient();
        var resilientClient = new MockResilientMqttClient();
        var rawClients = Signal.Emit<IMqttClient>(rawClient);
        var resilientClients = Signal.Emit<IResilientMqttClient>(resilientClient);
        var rawMessages = new List<MqttApplicationMessageReceivedEventArgs>();
        var resilientMessages = new List<MqttApplicationMessageReceivedEventArgs>();
        var discoveredTopics = new List<IEnumerable<(string Topic, DateTime LastSeen)>>();
        using var subscriptions = new MultipleDisposable
        {
            rawClients.SubscribeToTopics(MatchingTopic, DeviceTopicFilter).Subscribe(rawMessages.Add),
        };

        // Act
        subscriptions.Add(resilientClients
            .SubscribeToTopics(MatchingTopic, DeviceTopicFilter)
            .Subscribe(resilientMessages.Add));
        subscriptions.Add(rawClients.DiscoverTopics().Subscribe(discoveredTopics.Add));
        subscriptions.Add(rawClients.DiscoverTopics(TimeSpan.FromHours(DiscoveryWindowHours)).Subscribe());
        subscriptions.Add(rawClients.DiscoverTopics(TimeSpan.FromSeconds(1), TimeProvider.System).Subscribe());
        subscriptions.Add(resilientClients.DiscoverTopics().Subscribe());
        subscriptions.Add(resilientClients.DiscoverTopics(TimeSpan.FromHours(DiscoveryWindowHours)).Subscribe());
        subscriptions.Add(resilientClients.DiscoverTopics(TimeSpan.FromSeconds(1), TimeProvider.System).Subscribe());
        await Task.Delay(SubscriptionStartDelayMilliseconds);
        await rawClient.SimulateMessageReceivedAsync(MatchingTopic, PayloadText);
        await resilientClient.SimulateMessageReceivedAsync(MatchingTopic, PayloadText);

        // Assert
        await Assert.That(rawMessages).Count().IsEqualTo(ExpectedMatchingMessageCount);
        await Assert.That(resilientMessages).Count().IsEqualTo(ExpectedMatchingMessageCount);
        await Assert.That(discoveredTopics).Count().IsGreaterThan(0);
        using var discoveredTopicEnumerator = discoveredTopics[^1].GetEnumerator();
        await Assert.That(discoveredTopicEnumerator.MoveNext()).IsTrue();
        await Assert.That(discoveredTopicEnumerator.Current.Topic).IsEqualTo(MatchingTopic);
        await Assert.That(() => rawClients.DiscoverTopics(TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => resilientClients.DiscoverTopics(TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => rawClients.DiscoverTopics(TimeSpan.FromSeconds(1), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => resilientClients.DiscoverTopics(TimeSpan.FromSeconds(1), null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Represents the typed payload used to exercise unsuccessful object deserialization.</summary>
    public sealed class CoveragePayload
    {
        /// <summary>Gets or sets the payload name.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
