// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides compatibility static methods for reactive MQTT client operations.</summary>
public static class ReactiveClientOperations
{
    /// <summary>Sends a broker ping through an observable client sequence.</summary>
    /// <param name="client">The client sequence used to send the ping.</param>
    /// <returns>An observable that completes after the ping succeeds.</returns>
    public static IObservable<RxUnit> Ping(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Ping(client);

    /// <summary>Sends a broker ping through an asynchronous observable client sequence.</summary>
    /// <param name="client">The asynchronous client sequence used to send the ping.</param>
    /// <returns>An asynchronous observable that completes after the ping succeeds.</returns>
    public static IObservableAsync<RxUnit> Ping(IObservableAsync<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Ping(client);

    /// <summary>Sends periodic broker pings using an observable client sequence.</summary>
    /// <param name="client">The client sequence used to send periodic pings.</param>
    /// <returns>An observable that emits after each successful ping.</returns>
    public static IObservable<RxUnit> PingPeriodically(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.PingPeriodically(client);

    /// <summary>Sends periodic broker pings at the supplied interval.</summary>
    /// <param name="client">The client sequence used to send periodic pings.</param>
    /// <param name="interval">The interval between ping requests.</param>
    /// <returns>An observable that emits after each successful ping.</returns>
    public static IObservable<RxUnit> PingPeriodically(
        IObservable<IMqttClient> client,
        TimeSpan? interval) => ReactiveClientOperationsExtensions.PingPeriodically(client, interval);

    /// <summary>Sends periodic broker pings through an asynchronous client sequence.</summary>
    /// <param name="client">The asynchronous client sequence used to send periodic pings.</param>
    /// <returns>An asynchronous observable that emits after each successful ping.</returns>
    public static IObservableAsync<RxUnit> PingPeriodically(IObservableAsync<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.PingPeriodically(client);

    /// <summary>Sends periodic asynchronous broker pings at the supplied interval.</summary>
    /// <param name="client">The asynchronous client sequence used to send periodic pings.</param>
    /// <param name="interval">The interval between ping requests.</param>
    /// <returns>An asynchronous observable that emits after each successful ping.</returns>
    public static IObservableAsync<RxUnit> PingPeriodically(
        IObservableAsync<IMqttClient> client,
        TimeSpan? interval) => ReactiveClientOperationsExtensions.PingPeriodically(client, interval);

    /// <summary>Subscribes an observable client to the supplied topic names.</summary>
    /// <param name="client">The client sequence that creates the subscription.</param>
    /// <param name="topics">The topic names to subscribe to.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        IObservable<IMqttClient> client,
        string[] topics) => ReactiveClientOperationsExtensions.Subscribe(client, topics);

    /// <summary>Subscribes an observable client using one quality-of-service level.</summary>
    /// <param name="client">The client sequence that creates the subscription.</param>
    /// <param name="topics">The topic names to subscribe to.</param>
    /// <param name="qualityOfServiceLevel">The delivery guarantee requested for every topic.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        IObservable<IMqttClient> client,
        string[] topics,
        MqttQualityOfServiceLevel qualityOfServiceLevel) =>
        ReactiveClientOperationsExtensions.Subscribe(client, topics, qualityOfServiceLevel);

    /// <summary>Subscribes an observable client with a configured topic filter.</summary>
    /// <param name="client">The client sequence that creates the subscription.</param>
    /// <param name="topicFilterBuilder">The action that configures the topic filter.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        IObservable<IMqttClient> client,
        Action<MqttTopicFilterBuilder> topicFilterBuilder) =>
        ReactiveClientOperationsExtensions.Subscribe(client, topicFilterBuilder);

    /// <summary>Subscribes an observable client with the supplied topic filters.</summary>
    /// <param name="client">The client sequence that creates the subscription.</param>
    /// <param name="topicFilters">The topic filters to subscribe to.</param>
    /// <returns>An observable that emits the subscription result.</returns>
    public static IObservable<MqttClientSubscribeResult> Subscribe(
        IObservable<IMqttClient> client,
        params MqttTopicFilter[] topicFilters) => ReactiveClientOperationsExtensions.Subscribe(client, topicFilters);

    /// <summary>Subscribes an asynchronous client to the supplied topic names.</summary>
    /// <param name="client">The asynchronous client sequence that creates the subscription.</param>
    /// <param name="topics">The topic names to subscribe to.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        IObservableAsync<IMqttClient> client,
        string[] topics) => ReactiveClientOperationsExtensions.Subscribe(client, topics);

    /// <summary>Subscribes an asynchronous client using one quality-of-service level.</summary>
    /// <param name="client">The asynchronous client sequence that creates the subscription.</param>
    /// <param name="topics">The topic names to subscribe to.</param>
    /// <param name="qualityOfServiceLevel">The delivery guarantee requested for every topic.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        IObservableAsync<IMqttClient> client,
        string[] topics,
        MqttQualityOfServiceLevel qualityOfServiceLevel) =>
        ReactiveClientOperationsExtensions.Subscribe(client, topics, qualityOfServiceLevel);

    /// <summary>Subscribes an asynchronous client with a configured topic filter.</summary>
    /// <param name="client">The asynchronous client sequence that creates the subscription.</param>
    /// <param name="topicFilterBuilder">The action that configures the topic filter.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        IObservableAsync<IMqttClient> client,
        Action<MqttTopicFilterBuilder> topicFilterBuilder) =>
        ReactiveClientOperationsExtensions.Subscribe(client, topicFilterBuilder);

    /// <summary>Subscribes an asynchronous client with the supplied topic filters.</summary>
    /// <param name="client">The asynchronous client sequence that creates the subscription.</param>
    /// <param name="topicFilters">The topic filters to subscribe to.</param>
    /// <returns>An asynchronous observable that emits the subscription result.</returns>
    public static IObservableAsync<MqttClientSubscribeResult> Subscribe(
        IObservableAsync<IMqttClient> client,
        params MqttTopicFilter[] topicFilters) => ReactiveClientOperationsExtensions.Subscribe(client, topicFilters);

    /// <summary>Removes the supplied subscriptions from an observable client.</summary>
    /// <param name="client">The client sequence that removes the subscriptions.</param>
    /// <param name="topics">The topic names to unsubscribe from.</param>
    /// <returns>An observable that emits the unsubscription result.</returns>
    public static IObservable<MqttClientUnsubscribeResult> Unsubscribe(
        IObservable<IMqttClient> client,
        params string[] topics) => ReactiveClientOperationsExtensions.Unsubscribe(client, topics);

    /// <summary>Removes the supplied subscriptions from an asynchronous client.</summary>
    /// <param name="client">The asynchronous client sequence that removes subscriptions.</param>
    /// <param name="topics">The topic names to unsubscribe from.</param>
    /// <returns>An asynchronous observable that emits the unsubscription result.</returns>
    public static IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(
        IObservableAsync<IMqttClient> client,
        params string[] topics) => ReactiveClientOperationsExtensions.Unsubscribe(client, topics);

    /// <summary>Disconnects an observable client with the default disconnect reason.</summary>
    /// <param name="client">The client sequence to disconnect.</param>
    /// <returns>An observable that completes when disconnection finishes.</returns>
    public static IObservable<RxUnit> Disconnect(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Disconnect(client);

    /// <summary>Disconnects an observable client with the supplied reason.</summary>
    /// <param name="client">The client sequence to disconnect.</param>
    /// <param name="reason">The reason sent with the disconnect request.</param>
    /// <returns>An observable that completes when disconnection finishes.</returns>
    public static IObservable<RxUnit> Disconnect(
        IObservable<IMqttClient> client,
        MqttClientDisconnectOptionsReason reason) => ReactiveClientOperationsExtensions.Disconnect(client, reason);

    /// <summary>Disconnects an asynchronous client with the default disconnect reason.</summary>
    /// <param name="client">The asynchronous client sequence to disconnect.</param>
    /// <returns>An asynchronous observable that completes when disconnection finishes.</returns>
    public static IObservableAsync<RxUnit> Disconnect(IObservableAsync<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Disconnect(client);

    /// <summary>Disconnects an asynchronous client with the supplied reason.</summary>
    /// <param name="client">The asynchronous client sequence to disconnect.</param>
    /// <param name="reason">The reason sent with the disconnect request.</param>
    /// <returns>An asynchronous observable that completes when disconnection finishes.</returns>
    public static IObservableAsync<RxUnit> Disconnect(
        IObservableAsync<IMqttClient> client,
        MqttClientDisconnectOptionsReason reason) => ReactiveClientOperationsExtensions.Disconnect(client, reason);

    /// <summary>Reconnects each client produced by an observable sequence.</summary>
    /// <param name="client">The client sequence whose connections are restored.</param>
    /// <returns>An observable that completes after reconnection succeeds.</returns>
    public static IObservable<RxUnit> Reconnect(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Reconnect(client);

    /// <summary>Reconnects each client produced by an asynchronous observable sequence.</summary>
    /// <param name="client">The asynchronous client sequence whose connections are restored.</param>
    /// <returns>An asynchronous observable that completes after reconnection succeeds.</returns>
    public static IObservableAsync<RxUnit> Reconnect(IObservableAsync<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.Reconnect(client);

    /// <summary>Observes connection state changes for clients in an observable sequence.</summary>
    /// <param name="client">The client sequence whose connection state is observed.</param>
    /// <returns>An observable that emits the current connection state.</returns>
    public static IObservable<bool> ConnectionStatus(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.ConnectionStatus(client);

    /// <summary>Observes connection state changes for asynchronous client sequences.</summary>
    /// <param name="client">The asynchronous client sequence whose connection state is observed.</param>
    /// <returns>An asynchronous observable that emits the current connection state.</returns>
    public static IObservableAsync<bool> ConnectionStatus(IObservableAsync<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.ConnectionStatus(client);

    /// <summary>Waits until a client from an observable sequence connects.</summary>
    /// <param name="client">The client sequence to await connection from.</param>
    /// <returns>An observable that emits the connected client.</returns>
    public static IObservable<IMqttClient> WaitForConnection(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.WaitForConnection(client);

    /// <summary>Waits for connection until the supplied timeout expires.</summary>
    /// <param name="client">The client sequence to await connection from.</param>
    /// <param name="timeout">The maximum time allowed for connection.</param>
    /// <returns>An observable that emits the connected client.</returns>
    public static IObservable<IMqttClient> WaitForConnection(
        IObservable<IMqttClient> client,
        TimeSpan? timeout) => ReactiveClientOperationsExtensions.WaitForConnection(client, timeout);

    /// <summary>Waits until an asynchronous client sequence produces a connected client.</summary>
    /// <param name="client">The asynchronous client sequence to await connection from.</param>
    /// <returns>An asynchronous observable that emits the connected client.</returns>
    public static IObservableAsync<IMqttClient> WaitForConnection(
        IObservableAsync<IMqttClient> client) => ReactiveClientOperationsExtensions.WaitForConnection(client);

    /// <summary>Waits asynchronously for connection until the supplied timeout expires.</summary>
    /// <param name="client">The asynchronous client sequence to await connection from.</param>
    /// <param name="timeout">The maximum time allowed for connection.</param>
    /// <returns>An asynchronous observable that emits the connected client.</returns>
    public static IObservableAsync<IMqttClient> WaitForConnection(
        IObservableAsync<IMqttClient> client,
        TimeSpan? timeout) => ReactiveClientOperationsExtensions.WaitForConnection(client, timeout);

    /// <summary>Publishes text through a client from an observable sequence.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        string payload) => ReactiveClientOperationsExtensions.Publish(client, topic, payload);

    /// <summary>Publishes text with a specified delivery guarantee.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos);

    /// <summary>Publishes retained text with a specified delivery guarantee.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <param name="retain">Whether the broker retains the message.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos,
        bool retain) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos, retain);

    /// <summary>Publishes binary data through a client from an observable sequence.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        byte[] payload) => ReactiveClientOperationsExtensions.Publish(client, topic, payload);

    /// <summary>Publishes binary data with a specified delivery guarantee.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos);

    /// <summary>Publishes retained binary data with a delivery guarantee.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <param name="retain">Whether the broker retains the message.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos,
        bool retain) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos, retain);

    /// <summary>Publishes a message configured by the supplied builder action.</summary>
    /// <param name="client">The client sequence that publishes the message.</param>
    /// <param name="messageBuilder">The action that configures the MQTT message.</param>
    /// <returns>An observable that emits the publishing result.</returns>
    public static IObservable<MqttClientPublishResult> Publish(
        IObservable<IMqttClient> client,
        Action<MqttApplicationMessageBuilder> messageBuilder) =>
        ReactiveClientOperationsExtensions.Publish(client, messageBuilder);

    /// <summary>Publishes text through an asynchronous client sequence.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        string payload) => ReactiveClientOperationsExtensions.Publish(client, topic, payload);

    /// <summary>Publishes asynchronous text with a specified delivery guarantee.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos);

    /// <summary>Publishes retained asynchronous text with a delivery guarantee.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The text message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <param name="retain">Whether the broker retains the message.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        string payload,
        MqttQualityOfServiceLevel qos,
        bool retain) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos, retain);

    /// <summary>Publishes binary data through an asynchronous client sequence.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        byte[] payload) => ReactiveClientOperationsExtensions.Publish(client, topic, payload);

    /// <summary>Publishes asynchronous binary data with a delivery guarantee.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos);

    /// <summary>Publishes retained asynchronous binary data with a delivery guarantee.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The binary message body.</param>
    /// <param name="qos">The requested quality-of-service level.</param>
    /// <param name="retain">Whether the broker retains the message.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        string topic,
        byte[] payload,
        MqttQualityOfServiceLevel qos,
        bool retain) => ReactiveClientOperationsExtensions.Publish(client, topic, payload, qos, retain);

    /// <summary>Publishes an asynchronously configured message through the client sequence.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the message.</param>
    /// <param name="messageBuilder">The action that configures the MQTT message.</param>
    /// <returns>An asynchronous observable that emits the publishing result.</returns>
    public static IObservableAsync<MqttClientPublishResult> Publish(
        IObservableAsync<IMqttClient> client,
        Action<MqttApplicationMessageBuilder> messageBuilder) =>
        ReactiveClientOperationsExtensions.Publish(client, messageBuilder);

    /// <summary>Publishes each message from an observable sequence.</summary>
    /// <param name="client">The client sequence that publishes the messages.</param>
    /// <param name="messages">The messages to publish.</param>
    /// <returns>An observable that emits one result for each published message.</returns>
    public static IObservable<MqttClientPublishResult> PublishMany(
        IObservable<IMqttClient> client,
        IObservable<MqttApplicationMessage> messages) =>
        ReactiveClientOperationsExtensions.PublishMany(client, messages);

    /// <summary>Publishes each message from an asynchronous observable sequence.</summary>
    /// <param name="client">The asynchronous client sequence that publishes the messages.</param>
    /// <param name="messages">The asynchronous messages to publish.</param>
    /// <returns>An asynchronous observable that emits one result for each published message.</returns>
    public static IObservableAsync<MqttClientPublishResult> PublishMany(
        IObservableAsync<IMqttClient> client,
        IObservableAsync<MqttApplicationMessage> messages) =>
        ReactiveClientOperationsExtensions.PublishMany(client, messages);

    /// <summary>Gets options from each client produced by an observable sequence.</summary>
    /// <param name="client">The client sequence whose options are requested.</param>
    /// <returns>An observable that emits the client options.</returns>
    public static IObservable<MqttClientOptions?> GetOptions(IObservable<IMqttClient> client) =>
        ReactiveClientOperationsExtensions.GetOptions(client);

    /// <summary>Gets options from each client produced by an asynchronous sequence.</summary>
    /// <param name="client">The asynchronous client sequence whose options are requested.</param>
    /// <returns>An asynchronous observable that emits the client options.</returns>
    public static IObservableAsync<MqttClientOptions?> GetOptions(
        IObservableAsync<IMqttClient> client) => ReactiveClientOperationsExtensions.GetOptions(client);
}
