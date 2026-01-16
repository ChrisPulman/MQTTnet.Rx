// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Server;

namespace MQTTnet.Rx.Server;

/// <summary>
/// Provides extension methods for the MqttServer type to expose server events as observable sequences.
/// </summary>
/// <remarks>These extension methods enable reactive programming patterns by allowing consumers to subscribe to
/// various MQTT server events using IObservable{T}. Each method corresponds to a specific server event and returns an
/// observable sequence that emits event arguments when the event occurs. This approach simplifies event handling and
/// integration with reactive workflows.</remarks>
public static class MqttServerExtensions
{
    /// <summary>
    /// Returns an observable sequence that signals when an application message published to the server is not consumed
    /// by any client.
    /// </summary>
    /// <remarks>This method enables reactive handling of unconsumed application messages using the observer
    /// pattern. Subscribers can use the returned observable to respond to messages that are not delivered to any
    /// client. The observable completes when the server is disposed.</remarks>
    /// <param name="server">The MQTT server instance to monitor for unconsumed application messages. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="ApplicationMessageNotConsumedEventArgs"/> that notifies subscribers each
    /// time an application message is not consumed by any client.</returns>
    public static IObservable<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumed(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ApplicationMessageNotConsumedEventArgs>(
            handler => server.ApplicationMessageNotConsumedAsync += handler,
            handler => server.ApplicationMessageNotConsumedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client acknowledges receipt of a published MQTT message on
    /// the specified server.
    /// </summary>
    /// <remarks>This method enables reactive handling of client acknowledgments for published messages using
    /// the observer pattern. The observable completes when the server is disposed. Thread safety and event ordering are
    /// determined by the underlying server implementation.</remarks>
    /// <param name="server">The MQTT server instance to monitor for client publish acknowledgments. Cannot be null.</param>
    /// <returns>An observable sequence of events containing information about each client acknowledgment of a published message.
    /// Subscribers receive a notification each time a client acknowledges a publish packet.</returns>
    public static IObservable<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientAcknowledgedPublishPacketEventArgs>(
            handler => server.ClientAcknowledgedPublishPacketAsync += handler,
            handler => server.ClientAcknowledgedPublishPacketAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client connects to the specified MQTT server.
    /// </summary>
    /// <remarks>The returned observable emits a value for each client connection event and completes when the
    /// server is disposed. Subscribers are notified on the thread that raises the underlying event.</remarks>
    /// <param name="server">The MQTT server instance to monitor for client connection events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time a client connects to the server. Each value contains
    /// event data describing the connected client.</returns>
    public static IObservable<ClientConnectedEventArgs> ClientConnected(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientConnectedEventArgs>(
            handler => server.ClientConnectedAsync += handler,
            handler => server.ClientConnectedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client disconnects from the MQTT server.
    /// </summary>
    /// <remarks>Subscribers to the returned observable will receive notifications for each client
    /// disconnection event as long as the subscription is active.</remarks>
    /// <param name="server">The MQTT server instance to monitor for client disconnection events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time a client disconnects from the server. Each value contains
    /// event data describing the disconnected client.</returns>
    public static IObservable<ClientDisconnectedEventArgs> ClientDisconnected(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientDisconnectedEventArgs>(
            handler => server.ClientDisconnectedAsync += handler,
            handler => server.ClientDisconnectedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client subscribes to a topic on the specified MQTT server.
    /// </summary>
    /// <param name="server">The MQTT server instance to monitor for client topic subscription events. Cannot be null.</param>
    /// <returns>An observable sequence of event arguments containing information about each client topic subscription.
    /// Subscribers receive a notification each time a client subscribes to a topic.</returns>
    public static IObservable<ClientSubscribedTopicEventArgs> ClientSubscribedTopic(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientSubscribedTopicEventArgs>(
            handler => server.ClientSubscribedTopicAsync += handler,
            handler => server.ClientSubscribedTopicAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client unsubscribes from a topic on the specified MQTT
    /// server.
    /// </summary>
    /// <remarks>The returned observable emits a value each time a client unsubscribes from a topic.
    /// Subscribers can use this to react to unsubscription events in real time. The observable completes when the
    /// server is disposed. This method provides a reactive alternative to handling the ClientUnsubscribedTopicAsync
    /// event directly.</remarks>
    /// <param name="server">The MQTT server instance to monitor for client topic unsubscription events. Cannot be null.</param>
    /// <returns>An observable sequence of event arguments containing information about each client unsubscription event.</returns>
    public static IObservable<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ClientUnsubscribedTopicEventArgs>(
            handler => server.ClientUnsubscribedTopicAsync += handler,
            handler => server.ClientUnsubscribedTopicAsync -= handler);

    /// <summary>
    /// Creates an observable sequence that emits events when a client attempts to enqueue an application message on the
    /// specified MQTT server.
    /// </summary>
    /// <remarks>Subscribers to the returned observable receive notifications corresponding to the <see
    /// cref="MqttServer.InterceptingClientEnqueueAsync"/> event. The observable completes when the server is disposed.
    /// This method enables reactive handling of client message enqueue interception in MQTT server
    /// applications.</remarks>
    /// <param name="server">The MQTT server instance to monitor for client message enqueue events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingClientApplicationMessageEnqueueEventArgs"/> that provides data
    /// for each client message enqueue attempt.</returns>
    public static IObservable<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueue(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs>(
            handler => server.InterceptingClientEnqueueAsync += handler,
            handler => server.InterceptingClientEnqueueAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when an inbound MQTT packet is intercepted by the server.
    /// </summary>
    /// <remarks>Subscribers to the returned observable will receive a notification each time the server
    /// intercepts an inbound packet. Unsubscribing from the observable detaches the event handler from the server. This
    /// method enables reactive handling of inbound packet interception events.</remarks>
    /// <param name="server">The MQTT server instance to monitor for inbound packet interception events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingPacketEventArgs"/> that provides details about each intercepted
    /// inbound packet.</returns>
    public static IObservable<InterceptingPacketEventArgs> InterceptingInboundPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
            handler => server.InterceptingInboundPacketAsync += handler,
            handler => server.InterceptingInboundPacketAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the server is about to send an outbound MQTT packet, allowing
    /// inspection or modification before transmission.
    /// </summary>
    /// <remarks>Subscribers can use this observable to monitor or modify outbound MQTT packets before they
    /// are sent to clients. The sequence completes when the server is disposed. This method enables reactive handling
    /// of outbound packet interception without directly subscribing to the event.</remarks>
    /// <param name="server">The MQTT server instance to observe for outbound packet interception events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingPacketEventArgs"/> that provides event data for each outbound
    /// packet intercepted by the server.</returns>
    public static IObservable<InterceptingPacketEventArgs> InterceptingOutboundPacket(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
            handler => server.InterceptingOutboundPacketAsync += handler,
            handler => server.InterceptingOutboundPacketAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client attempts to publish a message, allowing interception
    /// and inspection of the publish event on the specified MQTT server.
    /// </summary>
    /// <remarks>Use this method to monitor or control message publishing on the server, such as for
    /// validation, logging, or custom authorization. The observable completes when the server is disposed. This method
    /// is thread-safe and can be subscribed to from multiple threads.</remarks>
    /// <param name="server">The MQTT server instance to observe for intercepting publish events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingPublishEventArgs"/> that provides information about each
    /// intercepted publish attempt. Subscribers can inspect or modify the publish operation before it is processed by
    /// the server.</returns>
    public static IObservable<InterceptingPublishEventArgs> InterceptingPublish(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingPublishEventArgs>(
            handler => server.InterceptingPublishAsync += handler,
            handler => server.InterceptingPublishAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals whenever a client attempts to subscribe to one or more MQTT topics
    /// on the server.
    /// </summary>
    /// <remarks>Use this method to reactively handle or inspect client subscription requests, such as for
    /// authorization or logging purposes. The observable completes when the server is disposed. This method does not
    /// initiate or alter the server's subscription logic; it provides a way to observe and optionally influence
    /// subscription events.</remarks>
    /// <param name="server">The MQTT server instance to monitor for subscription interception events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingSubscriptionEventArgs"/> that provides details about each
    /// intercepted subscription attempt. Subscribers receive an event each time a client initiates a subscription.</returns>
    public static IObservable<InterceptingSubscriptionEventArgs> InterceptingSubscription(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingSubscriptionEventArgs>(
            handler => server.InterceptingSubscriptionAsync += handler,
            handler => server.InterceptingSubscriptionAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client attempts to unsubscribe from one or more MQTT topics
    /// on the server.
    /// </summary>
    /// <remarks>Subscribers can use the event arguments to inspect or modify the unsubscription process, or
    /// to prevent it by setting appropriate properties. The observable completes when the server is disposed.</remarks>
    /// <param name="server">The MQTT server instance to monitor for unsubscription events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="InterceptingUnsubscriptionEventArgs"/> that provides details about each
    /// unsubscription attempt.</returns>
    public static IObservable<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscription(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<InterceptingUnsubscriptionEventArgs>(
            handler => server.InterceptingUnsubscriptionAsync += handler,
            handler => server.InterceptingUnsubscriptionAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT server is loading retained messages.
    /// </summary>
    /// <remarks>This method enables reactive handling of retained message loading events using the Reactive
    /// Extensions (Rx) pattern. Subscribers will receive a notification each time the server triggers the retained
    /// message loading event. The observable completes when the server is disposed.</remarks>
    /// <param name="server">The MQTT server instance to observe for retained message loading events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="LoadingRetainedMessagesEventArgs"/> that notifies subscribers each time the
    /// server loads retained messages.</returns>
    public static IObservable<LoadingRetainedMessagesEventArgs> LoadingRetainedMessage(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<LoadingRetainedMessagesEventArgs>(
            handler => server.LoadingRetainedMessageAsync += handler,
            handler => server.LoadingRetainedMessageAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT server is preparing a new client session.
    /// </summary>
    /// <remarks>Subscribers receive notifications corresponding to the server's PreparingSessionAsync event.
    /// The observable completes when the server is disposed. This method enables reactive handling of session
    /// preparation events in MQTT server workflows.</remarks>
    /// <param name="server">The MQTT server instance to observe for session preparation events. Cannot be null.</param>
    /// <returns>An observable sequence that produces an event each time the server begins preparing a client session.</returns>
    public static IObservable<EventArgs> PreparingSession(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.PreparingSessionAsync += handler,
            handler => server.PreparingSessionAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a retained message is added, updated, or removed on the MQTT
    /// server.
    /// </summary>
    /// <remarks>The observable emits a value each time the <see
    /// cref="MqttServer.RetainedMessageChangedAsync"/> event is raised. Subscribers receive notifications for all
    /// retained message changes, including additions, updates, and removals. The returned observable completes when the
    /// server is disposed.</remarks>
    /// <param name="server">The MQTT server instance to monitor for retained message changes. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="RetainedMessageChangedEventArgs"/> that notifies subscribers whenever a
    /// retained message is changed on the server.</returns>
    public static IObservable<RetainedMessageChangedEventArgs> RetainedMessageChanged(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<RetainedMessageChangedEventArgs>(
            handler => server.RetainedMessageChangedAsync += handler,
            handler => server.RetainedMessageChangedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when all retained MQTT messages have been cleared on the server.
    /// </summary>
    /// <remarks>Subscribers to the returned observable will be notified whenever the server's retained
    /// messages are cleared, allowing reactive handling of this event. The observable completes only when
    /// unsubscribed.</remarks>
    /// <param name="server">The MQTT server instance to monitor for retained message clearance events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a notification each time the server clears all retained messages. Each
    /// notification contains event data associated with the clearance operation.</returns>
    public static IObservable<EventArgs> RetainedMessagesCleared(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.RetainedMessagesClearedAsync += handler,
            handler => server.RetainedMessagesClearedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client session is deleted from the MQTT server.
    /// </summary>
    /// <remarks>Subscribers receive notifications asynchronously when client sessions are removed from the
    /// server. The observable completes only when the server is disposed or the event source is no longer
    /// available.</remarks>
    /// <param name="server">The MQTT server instance to monitor for session deletion events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time a session is deleted. Each notification contains event
    /// data describing the deleted session.</returns>
    public static IObservable<SessionDeletedEventArgs> SessionDeleted(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<SessionDeletedEventArgs>(
            handler => server.SessionDeletedAsync += handler,
            handler => server.SessionDeletedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT server has started.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time the server raises its StartedAsync event. The
    /// observable completes only when the underlying event source is disposed or unsubscribed.</remarks>
    /// <param name="server">The MQTT server instance to observe for start events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time the server's start event occurs. Each notification
    /// contains the event data associated with the server start.</returns>
    public static IObservable<EventArgs> Started(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.StartedAsync += handler,
            handler => server.StartedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when the MQTT server has stopped.
    /// </summary>
    /// <remarks>Subscribers receive a notification each time the server's StoppedAsync event is raised. This
    /// method enables reactive handling of server stop events using the Reactive Extensions (Rx) pattern.</remarks>
    /// <param name="server">The MQTT server instance to observe for stop events. Cannot be null.</param>
    /// <returns>An observable sequence that produces a value each time the server stops. The sequence completes when the server
    /// is disposed.</returns>
    public static IObservable<EventArgs> Stopped(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<EventArgs>(
            handler => server.StoppedAsync += handler,
            handler => server.StoppedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that signals when a client connection to the MQTT server is being validated.
    /// </summary>
    /// <remarks>Subscribers receive an event each time a client attempts to connect and the server performs
    /// validation. This can be used to implement custom authentication or authorization logic. The observable completes
    /// when the server is disposed.</remarks>
    /// <param name="server">The MQTT server instance for which to observe validating connection events. Cannot be null.</param>
    /// <returns>An observable sequence of <see cref="ValidatingConnectionEventArgs"/> that provides information about each
    /// client connection validation event.</returns>
    public static IObservable<ValidatingConnectionEventArgs> ValidatingConnection(this MqttServer server) =>
        CreateObservable.FromAsyncEvent<ValidatingConnectionEventArgs>(
            handler => server.ValidatingConnectionAsync += handler,
            handler => server.ValidatingConnectionAsync -= handler);
}
