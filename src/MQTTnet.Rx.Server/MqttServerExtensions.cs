// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Server;

namespace MQTTnet.Rx.Server;

/// <summary>
/// MqttServerExtensions.
/// </summary>
public static class MqttServerExtensions
{
    /// <summary>
    /// Applications the message not consumed.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ApplicationMessageNotConsumedEventArgs.</returns>
    public static IObservable<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumed(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ApplicationMessageNotConsumedEventArgs>(
            handler => server.ApplicationMessageNotConsumedAsync += handler,
            handler => server.ApplicationMessageNotConsumedAsync -= handler);

    /// <summary>
    /// Clients the acknowledged publish packet.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ClientAcknowledgedPublishPacketEventArgs.</returns>
    public static IObservable<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientAcknowledgedPublishPacketEventArgs>(
            handler => server.ClientAcknowledgedPublishPacketAsync += handler,
            handler => server.ClientAcknowledgedPublishPacketAsync -= handler);

    /// <summary>
    /// Client connected.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ClientConnectedEventArgs.</returns>
    public static IObservable<ClientConnectedEventArgs> ClientConnected(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientConnectedEventArgs>(
            handler => server.ClientConnectedAsync += handler,
            handler => server.ClientConnectedAsync -= handler);

    /// <summary>
    /// Clients the disconnected.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ClientDisconnectedEventArgs.</returns>
    public static IObservable<ClientDisconnectedEventArgs> ClientDisconnected(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientDisconnectedEventArgs>(
            handler => server.ClientDisconnectedAsync += handler,
            handler => server.ClientDisconnectedAsync -= handler);

    /// <summary>
    /// Clients the subscribed topic.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ClientSubscribedTopicEventArgs.</returns>
    public static IObservable<ClientSubscribedTopicEventArgs> ClientSubscribedTopic(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientSubscribedTopicEventArgs>(
            handler => server.ClientSubscribedTopicAsync += handler,
            handler => server.ClientSubscribedTopicAsync -= handler);

    /// <summary>
    /// Clients the unsubscribed topic.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ClientUnsubscribedTopicEventArgs.</returns>
    public static IObservable<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientUnsubscribedTopicEventArgs>(
            handler => server.ClientUnsubscribedTopicAsync += handler,
            handler => server.ClientUnsubscribedTopicAsync -= handler);

    /// <summary>
    /// Interceptings the client enqueue.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingClientApplicationMessageEnqueueEventArgs.</returns>
    public static IObservable<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueue(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs>(
            handler => server.InterceptingClientEnqueueAsync += handler,
            handler => server.InterceptingClientEnqueueAsync -= handler);

    /// <summary>
    /// Interceptings the inbound packet.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingPacketEventArgs.</returns>
    public static IObservable<InterceptingPacketEventArgs> InterceptingInboundPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
            handler => server.InterceptingInboundPacketAsync += handler,
            handler => server.InterceptingInboundPacketAsync -= handler);

    /// <summary>
    /// Interceptings the outbound packet.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingPacketEventArgs.</returns>
    public static IObservable<InterceptingPacketEventArgs> InterceptingOutboundPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
            handler => server.InterceptingOutboundPacketAsync += handler,
            handler => server.InterceptingOutboundPacketAsync -= handler);

    /// <summary>
    /// Interceptings the publish.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingPublishEventArgs.</returns>
    public static IObservable<InterceptingPublishEventArgs> InterceptingPublish(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPublishEventArgs>(
            handler => server.InterceptingPublishAsync += handler,
            handler => server.InterceptingPublishAsync -= handler);

    /// <summary>
    /// Interceptings the subscription.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingSubscriptionEventArgs.</returns>
    public static IObservable<InterceptingSubscriptionEventArgs> InterceptingSubscription(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingSubscriptionEventArgs>(
            handler => server.InterceptingSubscriptionAsync += handler,
            handler => server.InterceptingSubscriptionAsync -= handler);

    /// <summary>
    /// Interceptings the unsubscription.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of InterceptingUnsubscriptionEventArgs.</returns>
    public static IObservable<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscription(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingUnsubscriptionEventArgs>(
            handler => server.InterceptingUnsubscriptionAsync += handler,
            handler => server.InterceptingUnsubscriptionAsync -= handler);

    /// <summary>
    /// Loadings the retained message.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of LoadingRetainedMessagesEventArgs.</returns>
    public static IObservable<LoadingRetainedMessagesEventArgs> LoadingRetainedMessage(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<LoadingRetainedMessagesEventArgs>(
            handler => server.LoadingRetainedMessageAsync += handler,
            handler => server.LoadingRetainedMessageAsync -= handler);

    /// <summary>
    /// Preparings the session.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of EventArgs.</returns>
    public static IObservable<EventArgs> PreparingSession(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.PreparingSessionAsync += handler,
            handler => server.PreparingSessionAsync -= handler);

    /// <summary>
    /// Retaineds the message changed.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of RetainedMessageChangedEventArgs.</returns>
    public static IObservable<RetainedMessageChangedEventArgs> RetainedMessageChanged(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<RetainedMessageChangedEventArgs>(
            handler => server.RetainedMessageChangedAsync += handler,
            handler => server.RetainedMessageChangedAsync -= handler);

    /// <summary>
    /// Retaineds the messages cleared.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of EventArgs.</returns>
    public static IObservable<EventArgs> RetainedMessagesCleared(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.RetainedMessagesClearedAsync += handler,
            handler => server.RetainedMessagesClearedAsync -= handler);

    /// <summary>
    /// Sessions the deleted.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of SessionDeletedEventArgs.</returns>
    public static IObservable<SessionDeletedEventArgs> SessionDeleted(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<SessionDeletedEventArgs>(
            handler => server.SessionDeletedAsync += handler,
            handler => server.SessionDeletedAsync -= handler);

    /// <summary>
    /// Starteds the specified server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of EventArgs.</returns>
    public static IObservable<EventArgs> Started(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.StartedAsync += handler,
            handler => server.StartedAsync -= handler);

    /// <summary>
    /// Stoppeds the specified server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of EventArgs.</returns>
    public static IObservable<EventArgs> Stopped(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.StoppedAsync += handler,
            handler => server.StoppedAsync -= handler);

    /// <summary>
    /// Validatings the connection.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>An Observable of ValidatingConnectionEventArgs.</returns>
    public static IObservable<ValidatingConnectionEventArgs> ValidatingConnection(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ValidatingConnectionEventArgs>(
            handler => server.ValidatingConnectionAsync += handler,
            handler => server.ValidatingConnectionAsync -= handler);
}
