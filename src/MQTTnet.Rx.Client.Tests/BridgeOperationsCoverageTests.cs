// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises asynchronous bridge and client-operation compatibility paths.</summary>
public sealed class BridgeOperationsCoverageTests
{
    /// <summary>The first topic level index.</summary>
    private const int FirstTopicLevel = 0;

    /// <summary>The expected number of matching temperature messages.</summary>
    private const int ExpectedTemperatureMessages = 2;

    /// <summary>The expected number of published messages.</summary>
    private const int ExpectedPublishedMessages = 6;

    /// <summary>The number of messages supplied to the topic bridge tests.</summary>
    private const int ExpectedMessageCount = 3;

    /// <summary>The number of subscriptions created by the facade test.</summary>
    private const int ExpectedSubscriptionCount = 2;

    /// <summary>The expected number of initial connection attempts.</summary>
    private const int ExpectedConnectCount = 1;

    /// <summary>The index of the final published bridge message.</summary>
    private const int FinalPublishedMessageIndex = 5;

    /// <summary>The unavailable topic level index.</summary>
    private const int UnavailableTopicLevel = 5;

    /// <summary>The numeric JSON value represented by the first message.</summary>
    private const long ExpectedJsonValue = 21L;

    /// <summary>The converted scalar value.</summary>
    private const int ExpectedScalarValue = 42;

    /// <summary>The primary kitchen temperature topic.</summary>
    private const string KitchenTemperatureTopic = "home/kitchen/temperature";

    /// <summary>The byte payload used by the binary facade test.</summary>
    private static readonly byte[] FacadePayload = [9];

    /// <summary>The first byte payload used by publish bridge tests.</summary>
    private static readonly byte[] FirstBridgePayload = [1];

    /// <summary>The second byte payload used by publish bridge tests.</summary>
    private static readonly byte[] SecondBridgePayload = [2];

    /// <summary>The third byte payload used by publish bridge tests.</summary>
    private static readonly byte[] ThirdBridgePayload = [3];

    /// <summary>Verifies async topic filters, extraction, JSON conversion, and scalar conversion.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncBridge_MessageProjectionAndTopicFiltersProduceExpectedValuesAsync()
    {
        var messages = CreateProjectionMessages();

        var noMatches = await messages.WhereTopicMatchesAny().ToObservable().CollectAsync();
        var oneMatch = await messages.WhereTopicMatchesAny("home/+/temperature")
            .ToObservable()
            .CollectAsync();
        var manyMatches = await messages.WhereTopicMatchesAny("home/kitchen/#", "home/+/temperature")
            .ToObservable()
            .CollectAsync();
        var excluded = await messages.WhereTopicIsNotMatch("home/kitchen/#").ToObservable().CollectAsync();
        var levels = await messages.WhereTopicLevelCount(ExpectedMessageCount)
            .SelectTopicLevel(FirstTopicLevel)
            .ToObservable()
            .CollectAsync();
        var extracted = await messages.ExtractTopicValues("home/{room}/{measurement}")
            .ToObservable()
            .CollectAsync();
        var payload = await messages.WhereTopicIsMatch(KitchenTemperatureTopic)
            .ToUtf8String()
            .FirstAsync(TimeSpan.FromSeconds(1));
        var dictionary = await messages.WhereTopicIsMatch(KitchenTemperatureTopic)
            .ToDictionary()
            .FirstAsync(TimeSpan.FromSeconds(1));
        var objectValue = await SignalAsync.Return<object>("42")
            .Select(static value => (object?)value)
            .ToInt32()
            .FirstAsync(TimeSpan.FromSeconds(1));

        await Assert.That(noMatches).IsEmpty();
        await Assert.That(oneMatch).Count().IsEqualTo(ExpectedTemperatureMessages);
        await Assert.That(manyMatches).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(excluded).Count().IsEqualTo(1);
        await Assert.That(levels).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(levels[0]).IsEqualTo("home");
        await Assert.That(extracted).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(extracted[0].Values["room"]).IsEqualTo("kitchen");
        await Assert.That(extracted[0].Values["measurement"]).IsEqualTo("temperature");
        await Assert.That(payload).IsEqualTo("{\"value\":21,\"enabled\":true}");
        await Assert.That(dictionary).IsNotNull();
        await Assert.That(dictionary!["value"]).IsEqualTo(ExpectedJsonValue);
        await Assert.That(objectValue).IsEqualTo(ExpectedScalarValue);
    }

    /// <summary>Verifies invalid topic placeholders and JSON use deterministic fallbacks.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncBridge_InvalidInputsFollowDocumentedFallbackPathsAsync()
    {
        var message = SignalAsync.Return(
            TestDataHelpers.CreateMessageReceivedArgs("devices/alpha", "not-json"));

        await Assert.That(() => message.ExtractTopicValues("devices/{1invalid}")).Throws<ArgumentException>();
        var dictionary = await message.ToDictionary().FirstAsync(TimeSpan.FromSeconds(1));
        var value = await message.ToObject(static json => JsonSerializer.Deserialize<int>(json))
            .FirstAsync(TimeSpan.FromSeconds(1));
        var unavailableLevel = await message.SelectTopicLevel(UnavailableTopicLevel)
            .ToObservable()
            .CollectAsync();

        await Assert.That(dictionary).IsNull();
        await Assert.That(value).IsEqualTo(0);
        await Assert.That(unavailableLevel).IsEmpty();
    }

    /// <summary>Verifies raw-client async publish bridge overloads configure and publish messages.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncBridge_RawClientPublishMessageOverloadsPublishConfiguredMessagesAsync()
    {
        using var mqttClient = new MockMqttClient();
        var client = SignalAsync.Return<IMqttClient>(mqttClient);
        _ = await client.PublishMessage(SignalAsync.Return(("bridge/text/default", "one")))
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await client.PublishMessage(
                SignalAsync.Return(("bridge/text/qos", "two")),
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await client.PublishMessage(
            SignalAsync.Return(("bridge/text/configured", "three")),
            static builder => _ = builder.WithRetainFlag(false),
            MqttQualityOfServiceLevel.AtMostOnce,
            false).FirstAsync(TimeSpan.FromSeconds(1));
        _ = await client.PublishMessage(SignalAsync.Return(("bridge/bytes/default", FirstBridgePayload)))
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await client.PublishMessage(
                SignalAsync.Return(("bridge/bytes/qos", SecondBridgePayload)),
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await client.PublishMessage(
            SignalAsync.Return(("bridge/bytes/configured", ThirdBridgePayload)),
            static builder => _ = builder.WithRetainFlag(false),
            MqttQualityOfServiceLevel.AtMostOnce,
            false).FirstAsync(TimeSpan.FromSeconds(1));

        await Assert.That(mqttClient.PublishedMessages).Count().IsEqualTo(ExpectedPublishedMessages);
        await Assert.That(mqttClient.PublishedMessages[0].Topic).IsEqualTo("bridge/text/default");
        await Assert.That(mqttClient.PublishedMessages[FinalPublishedMessageIndex].Topic)
            .IsEqualTo("bridge/bytes/configured");
    }

    /// <summary>Verifies the async facade forwards MQTT operations and preserves configured arguments.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ReactiveClientOperations_AsyncFacadeForwardsOperationFamiliesAsync()
    {
        using var mqttClient = new MockMqttClient();
        var client = SignalAsync.Return<IMqttClient>(mqttClient);
        var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();
        var (status, connected, options) = await ExerciseReactiveFacadeAsync(
            mqttClient,
            client,
            clientOptions);

        await Assert.That(mqttClient.Subscriptions).Count().IsEqualTo(ExpectedSubscriptionCount);
        await Assert.That(mqttClient.Unsubscriptions).Count().IsEqualTo(ExpectedSubscriptionCount);
        await Assert.That(mqttClient.PublishedMessages).Count().IsEqualTo(ExpectedMessageCount);
        await Assert.That(mqttClient.DisconnectCount).IsEqualTo(1);
        await Assert.That(mqttClient.ConnectCount).IsEqualTo(ExpectedConnectCount);
        await Assert.That(status).IsTrue();
        await Assert.That(connected).IsSameReferenceAs(mqttClient);
        await Assert.That(options).IsSameReferenceAs(clientOptions);
    }

    /// <summary>Exercises every asynchronous facade operation against a connected MQTT client.</summary>
    /// <param name="mqttClient">The fake MQTT client used by the facade.</param>
    /// <param name="client">The asynchronous source that emits the fake client.</param>
    /// <param name="clientOptions">The options assigned to the fake client.</param>
    /// <returns>The final connection status, client, and options exposed by the facade.</returns>
    private static async Task<(bool Status, IMqttClient Connected, MqttClientOptions Options)>
        ExerciseReactiveFacadeAsync(
            MockMqttClient mqttClient,
            IObservableAsync<IMqttClient> client,
            MqttClientOptions clientOptions)
    {
        await mqttClient.ConnectAsync(clientOptions);
        await ExerciseFacadePublishOperationsAsync(client);

        _ = ReactiveClientOperations.Reconnect(SignalAsync.Return<IMqttClient>(mqttClient));
        await mqttClient.SimulateConnectedAsync();
        var connected = await ReactiveClientOperations
            .WaitForConnection(SignalAsync.Return<IMqttClient>(mqttClient), TimeSpan.FromSeconds(1))
            .FirstAsync(TimeSpan.FromSeconds(1));
        var status = await ReactiveClientOperations
            .ConnectionStatus(SignalAsync.Return<IMqttClient>(mqttClient))
            .FirstAsync(TimeSpan.FromSeconds(1));
        var options = await ReactiveClientOperations
            .GetOptions(SignalAsync.Return<IMqttClient>(mqttClient))
            .FirstAsync(TimeSpan.FromSeconds(1));

        ArgumentNullException.ThrowIfNull(options);
        return (status, connected, options);
    }

    /// <summary>Exercises all subscription, publication, and disconnection facade operations.</summary>
    /// <param name="client">The asynchronous source that emits the fake client.</param>
    /// <returns>A task that represents the asynchronous facade operations.</returns>
    private static async Task ExerciseFacadePublishOperationsAsync(IObservableAsync<IMqttClient> client)
    {
        _ = await ReactiveClientOperations.Subscribe(
                client,
                ["facade/one"],
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Subscribe(
                client,
                static filter => _ = filter.WithTopic("facade/two"))
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Unsubscribe(client, "facade/one", "facade/two")
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Publish(
                client,
                "facade/text",
                "payload",
                MqttQualityOfServiceLevel.ExactlyOnce,
                true)
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Publish(
                client,
                "facade/bytes",
                FacadePayload,
                MqttQualityOfServiceLevel.AtLeastOnce,
                false)
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Publish(
                client,
                static builder => _ = builder.WithTopic("facade/builder"))
            .FirstAsync(TimeSpan.FromSeconds(1));
        _ = await ReactiveClientOperations.Disconnect(
                client,
                MqttClientDisconnectOptionsReason.NormalDisconnection)
            .FirstAsync(TimeSpan.FromSeconds(1));
    }

    /// <summary>Creates the finite message source used by the asynchronous topic projection test.</summary>
    /// <returns>The source that emits the projection test messages.</returns>
    private static IObservableAsync<MqttApplicationMessageReceivedEventArgs> CreateProjectionMessages()
    {
        MqttApplicationMessageReceivedEventArgs[] messageItems =
        [
            TestDataHelpers.CreateMessageReceivedArgs(
                KitchenTemperatureTopic,
                "{\"value\":21,\"enabled\":true}"),
            TestDataHelpers.CreateMessageReceivedArgs("home/living/temperature", "{\"value\":22}"),
            TestDataHelpers.CreateMessageReceivedArgs("home/kitchen/humidity", "invalid-json")
        ];
        IObservable<MqttApplicationMessageReceivedEventArgs> messageObservable =
            Signal.FromEnumerable(messageItems);
        return messageObservable.ToSignal();
    }
}
