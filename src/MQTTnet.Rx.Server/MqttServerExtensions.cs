// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides MQTT server event observable extensions.</summary>
public static class MqttServerExtensions
{
    /// <summary>Provides event projections for an MQTT server.</summary>
    /// <param name="server">The MQTT server whose events are observed.</param>
    extension(MqttServer server)
    {
        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumed() =>
            CreateObservable.FromAsyncEvent<ApplicationMessageNotConsumedEventArgs>(
                handler => server.ApplicationMessageNotConsumedAsync += handler,
                handler => server.ApplicationMessageNotConsumedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ApplicationMessageNotConsumedEventArgs> ObserveApplicationMessageNotConsumed() =>
            CreateObservable.FromAsyncEventSignal<ApplicationMessageNotConsumedEventArgs>(
                handler => server.ApplicationMessageNotConsumedAsync += handler,
                handler => server.ApplicationMessageNotConsumedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacket() =>
            CreateObservable.FromAsyncEvent<ClientAcknowledgedPublishPacketEventArgs>(
                handler => server.ClientAcknowledgedPublishPacketAsync += handler,
                handler => server.ClientAcknowledgedPublishPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ClientAcknowledgedPublishPacketEventArgs> ObserveClientAcknowledgedPublishPacket() =>
            CreateObservable.FromAsyncEventSignal<ClientAcknowledgedPublishPacketEventArgs>(
                handler => server.ClientAcknowledgedPublishPacketAsync += handler,
                handler => server.ClientAcknowledgedPublishPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ClientConnectedEventArgs> ClientConnected() =>
            CreateObservable.FromAsyncEvent<ClientConnectedEventArgs>(
                handler => server.ClientConnectedAsync += handler,
                handler => server.ClientConnectedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ClientConnectedEventArgs> ObserveClientConnected() =>
            CreateObservable.FromAsyncEventSignal<ClientConnectedEventArgs>(
                handler => server.ClientConnectedAsync += handler,
                handler => server.ClientConnectedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ClientDisconnectedEventArgs> ClientDisconnected() =>
            CreateObservable.FromAsyncEvent<ClientDisconnectedEventArgs>(
                handler => server.ClientDisconnectedAsync += handler,
                handler => server.ClientDisconnectedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ClientDisconnectedEventArgs> ObserveClientDisconnected() =>
            CreateObservable.FromAsyncEventSignal<ClientDisconnectedEventArgs>(
                handler => server.ClientDisconnectedAsync += handler,
                handler => server.ClientDisconnectedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ClientSubscribedTopicEventArgs> ClientSubscribedTopic() =>
            CreateObservable.FromAsyncEvent<ClientSubscribedTopicEventArgs>(
                handler => server.ClientSubscribedTopicAsync += handler,
                handler => server.ClientSubscribedTopicAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ClientSubscribedTopicEventArgs> ObserveClientSubscribedTopic() =>
            CreateObservable.FromAsyncEventSignal<ClientSubscribedTopicEventArgs>(
                handler => server.ClientSubscribedTopicAsync += handler,
                handler => server.ClientSubscribedTopicAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic() =>
            CreateObservable.FromAsyncEvent<ClientUnsubscribedTopicEventArgs>(
                handler => server.ClientUnsubscribedTopicAsync += handler,
                handler => server.ClientUnsubscribedTopicAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ClientUnsubscribedTopicEventArgs> ObserveClientUnsubscribedTopic() =>
            CreateObservable.FromAsyncEventSignal<ClientUnsubscribedTopicEventArgs>(
                handler => server.ClientUnsubscribedTopicAsync += handler,
                handler => server.ClientUnsubscribedTopicAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueue() =>
            CreateObservable.FromAsyncEvent<InterceptingClientApplicationMessageEnqueueEventArgs>(
                handler => server.InterceptingClientEnqueueAsync += handler,
                handler => server.InterceptingClientEnqueueAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingClientApplicationMessageEnqueueEventArgs>
            ObserveInterceptingClientEnqueue() =>
            CreateObservable.FromAsyncEventSignal<InterceptingClientApplicationMessageEnqueueEventArgs>(
                handler => server.InterceptingClientEnqueueAsync += handler,
                handler => server.InterceptingClientEnqueueAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingPacketEventArgs> InterceptingInboundPacket() =>
            CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
                handler => server.InterceptingInboundPacketAsync += handler,
                handler => server.InterceptingInboundPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingPacketEventArgs> ObserveInterceptingInboundPacket() =>
            CreateObservable.FromAsyncEventSignal<InterceptingPacketEventArgs>(
                handler => server.InterceptingInboundPacketAsync += handler,
                handler => server.InterceptingInboundPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingPacketEventArgs> InterceptingOutboundPacket() =>
            CreateObservable.FromAsyncEvent<InterceptingPacketEventArgs>(
                handler => server.InterceptingOutboundPacketAsync += handler,
                handler => server.InterceptingOutboundPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingPacketEventArgs> ObserveInterceptingOutboundPacket() =>
            CreateObservable.FromAsyncEventSignal<InterceptingPacketEventArgs>(
                handler => server.InterceptingOutboundPacketAsync += handler,
                handler => server.InterceptingOutboundPacketAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingPublishEventArgs> InterceptingPublish() =>
            CreateObservable.FromAsyncEvent<InterceptingPublishEventArgs>(
                handler => server.InterceptingPublishAsync += handler,
                handler => server.InterceptingPublishAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingPublishEventArgs> ObserveInterceptingPublish() =>
            CreateObservable.FromAsyncEventSignal<InterceptingPublishEventArgs>(
                handler => server.InterceptingPublishAsync += handler,
                handler => server.InterceptingPublishAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingSubscriptionEventArgs> InterceptingSubscription() =>
            CreateObservable.FromAsyncEvent<InterceptingSubscriptionEventArgs>(
                handler => server.InterceptingSubscriptionAsync += handler,
                handler => server.InterceptingSubscriptionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingSubscriptionEventArgs> ObserveInterceptingSubscription() =>
            CreateObservable.FromAsyncEventSignal<InterceptingSubscriptionEventArgs>(
                handler => server.InterceptingSubscriptionAsync += handler,
                handler => server.InterceptingSubscriptionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscription() =>
            CreateObservable.FromAsyncEvent<InterceptingUnsubscriptionEventArgs>(
                handler => server.InterceptingUnsubscriptionAsync += handler,
                handler => server.InterceptingUnsubscriptionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<InterceptingUnsubscriptionEventArgs> ObserveInterceptingUnsubscription() =>
            CreateObservable.FromAsyncEventSignal<InterceptingUnsubscriptionEventArgs>(
                handler => server.InterceptingUnsubscriptionAsync += handler,
                handler => server.InterceptingUnsubscriptionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<LoadingRetainedMessagesEventArgs> LoadingRetainedMessage() =>
            CreateObservable.FromAsyncEvent<LoadingRetainedMessagesEventArgs>(
                handler => server.LoadingRetainedMessageAsync += handler,
                handler => server.LoadingRetainedMessageAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<LoadingRetainedMessagesEventArgs> ObserveLoadingRetainedMessage() =>
            CreateObservable.FromAsyncEventSignal<LoadingRetainedMessagesEventArgs>(
                handler => server.LoadingRetainedMessageAsync += handler,
                handler => server.LoadingRetainedMessageAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<EventArgs> PreparingSession() =>
            CreateObservable.FromAsyncEvent<EventArgs>(
                handler => server.PreparingSessionAsync += handler,
                handler => server.PreparingSessionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<EventArgs> ObservePreparingSession() =>
            CreateObservable.FromAsyncEventSignal<EventArgs>(
                handler => server.PreparingSessionAsync += handler,
                handler => server.PreparingSessionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<RetainedMessageChangedEventArgs> RetainedMessageChanged() =>
            CreateObservable.FromAsyncEvent<RetainedMessageChangedEventArgs>(
                handler => server.RetainedMessageChangedAsync += handler,
                handler => server.RetainedMessageChangedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<RetainedMessageChangedEventArgs> ObserveRetainedMessageChanged() =>
            CreateObservable.FromAsyncEventSignal<RetainedMessageChangedEventArgs>(
                handler => server.RetainedMessageChangedAsync += handler,
                handler => server.RetainedMessageChangedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<EventArgs> RetainedMessagesCleared() =>
            CreateObservable.FromAsyncEvent<EventArgs>(
                handler => server.RetainedMessagesClearedAsync += handler,
                handler => server.RetainedMessagesClearedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<EventArgs> ObserveRetainedMessagesCleared() =>
            CreateObservable.FromAsyncEventSignal<EventArgs>(
                handler => server.RetainedMessagesClearedAsync += handler,
                handler => server.RetainedMessagesClearedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<SessionDeletedEventArgs> SessionDeleted() =>
            CreateObservable.FromAsyncEvent<SessionDeletedEventArgs>(
                handler => server.SessionDeletedAsync += handler,
                handler => server.SessionDeletedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<SessionDeletedEventArgs> ObserveSessionDeleted() =>
            CreateObservable.FromAsyncEventSignal<SessionDeletedEventArgs>(
                handler => server.SessionDeletedAsync += handler,
                handler => server.SessionDeletedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<EventArgs> Started() =>
            CreateObservable.FromAsyncEvent<EventArgs>(
                handler => server.StartedAsync += handler,
                handler => server.StartedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<EventArgs> ObserveStarted() =>
            CreateObservable.FromAsyncEventSignal<EventArgs>(
                handler => server.StartedAsync += handler,
                handler => server.StartedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<EventArgs> Stopped() =>
            CreateObservable.FromAsyncEvent<EventArgs>(
                handler => server.StoppedAsync += handler,
                handler => server.StoppedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<EventArgs> ObserveStopped() =>
            CreateObservable.FromAsyncEventSignal<EventArgs>(
                handler => server.StoppedAsync += handler,
                handler => server.StoppedAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservable<ValidatingConnectionEventArgs> ValidatingConnection() =>
            CreateObservable.FromAsyncEvent<ValidatingConnectionEventArgs>(
                handler => server.ValidatingConnectionAsync += handler,
                handler => server.ValidatingConnectionAsync -= handler);

        /// <summary>Observes the associated MQTT server event.</summary>
        /// <returns>An observable event sequence.</returns>
        public IObservableAsync<ValidatingConnectionEventArgs> ObserveValidatingConnection() =>
            CreateObservable.FromAsyncEventSignal<ValidatingConnectionEventArgs>(
                handler => server.ValidatingConnectionAsync += handler,
                handler => server.ValidatingConnectionAsync -= handler);
    }
}
