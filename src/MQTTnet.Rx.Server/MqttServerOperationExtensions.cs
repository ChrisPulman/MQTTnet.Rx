// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides cold reactive wrappers for every public MQTT server operation.</summary>
public static class MqttServerOperationExtensions
{
    /// <summary>Provides reactive operations for an MQTT server.</summary>
    /// <param name="server">The MQTT server.</param>
    extension(MqttServer server)
    {
        /// <summary>Deletes every retained message when subscribed.</summary>
        /// <returns>A cold delete operation.</returns>
        public IObservable<RxVoid> DeleteRetainedMessages() =>
            CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.DeleteRetainedMessagesAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Deletes every retained message through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous delete operation.</returns>
        public IObservableAsync<RxVoid> ObserveDeleteRetainedMessages() =>
            CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.DeleteRetainedMessagesAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Disconnects a client when subscribed.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxVoid> DisconnectClient(string clientId, MqttServerClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.DisconnectClientAsync(clientId, options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Disconnects a client with fluent disconnect-option configuration.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold disconnect operation.</returns>
        public IObservable<RxVoid> DisconnectClient(
            string clientId,
            Action<MqttServerClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerClientDisconnectOptionsBuilder();
            configure(builder);
            return server.DisconnectClient(clientId, builder.Build());
        }

        /// <summary>Disconnects a client through an asynchronous observable.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="options">The disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxVoid> ObserveDisconnectClient(
            string clientId,
            MqttServerClientDisconnectOptions options)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.DisconnectClientAsync(clientId, options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Disconnects a client asynchronously with fluent disconnect-option configuration.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>A cold asynchronous disconnect operation.</returns>
        public IObservableAsync<RxVoid> ObserveDisconnectClient(
            string clientId,
            Action<MqttServerClientDisconnectOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerClientDisconnectOptionsBuilder();
            configure(builder);
            return server.ObserveDisconnectClient(clientId, builder.Build());
        }

        /// <summary>Gets a point-in-time client-status collection when subscribed.</summary>
        /// <returns>A cold client query.</returns>
        public IObservable<IList<MqttClientStatus>> GetClients() =>
            CreateObservable.FromTask<IList<MqttClientStatus>>(cancellationToken =>
            {
                var operation = server.GetClientsAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Gets a point-in-time client-status collection asynchronously.</summary>
        /// <returns>A cold asynchronous client query.</returns>
        public IObservableAsync<IList<MqttClientStatus>> ObserveClients() =>
            CreateObservable.FromTaskSignal<IList<MqttClientStatus>>(cancellationToken =>
            {
                var operation = server.GetClientsAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Gets one retained message when subscribed.</summary>
        /// <param name="topic">The retained-message topic.</param>
        /// <returns>A cold retained-message query.</returns>
        public IObservable<MqttApplicationMessage> GetRetainedMessage(string topic)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return CreateObservable.FromTask<MqttApplicationMessage>(cancellationToken =>
            {
                var operation = server.GetRetainedMessageAsync(topic);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Gets one retained message asynchronously.</summary>
        /// <param name="topic">The retained-message topic.</param>
        /// <returns>A cold asynchronous retained-message query.</returns>
        public IObservableAsync<MqttApplicationMessage> ObserveRetainedMessage(string topic)
        {
            ArgumentNullException.ThrowIfNull(topic);
            return CreateObservable.FromTaskSignal<MqttApplicationMessage>(cancellationToken =>
            {
                var operation = server.GetRetainedMessageAsync(topic);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Gets every retained message when subscribed.</summary>
        /// <returns>A cold retained-message query.</returns>
        public IObservable<IList<MqttApplicationMessage>> GetRetainedMessages() =>
            CreateObservable.FromTask<IList<MqttApplicationMessage>>(cancellationToken =>
            {
                var operation = server.GetRetainedMessagesAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Gets every retained message asynchronously.</summary>
        /// <returns>A cold asynchronous retained-message query.</returns>
        public IObservableAsync<IList<MqttApplicationMessage>> ObserveRetainedMessages() =>
            CreateObservable.FromTaskSignal<IList<MqttApplicationMessage>>(cancellationToken =>
            {
                var operation = server.GetRetainedMessagesAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Gets a session status when subscribed.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>A cold session query.</returns>
        public IObservable<MqttSessionStatus> GetSession(string clientId)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            return CreateObservable.FromTask<MqttSessionStatus>(cancellationToken =>
            {
                var operation = server.GetSessionAsync(clientId);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Gets a session status asynchronously.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>A cold asynchronous session query.</returns>
        public IObservableAsync<MqttSessionStatus> ObserveSession(string clientId)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            return CreateObservable.FromTaskSignal<MqttSessionStatus>(cancellationToken =>
            {
                var operation = server.GetSessionAsync(clientId);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Gets every session status when subscribed.</summary>
        /// <returns>A cold session query.</returns>
        public IObservable<IList<MqttSessionStatus>> GetSessions() =>
            CreateObservable.FromTask<IList<MqttSessionStatus>>(cancellationToken =>
            {
                var operation = server.GetSessionsAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Gets every session status asynchronously.</summary>
        /// <returns>A cold asynchronous session query.</returns>
        public IObservableAsync<IList<MqttSessionStatus>> ObserveSessions() =>
            CreateObservable.FromTaskSignal<IList<MqttSessionStatus>>(cancellationToken =>
            {
                var operation = server.GetSessionsAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Injects an application message when subscribed.</summary>
        /// <param name="message">The injected application message.</param>
        /// <returns>A cold injection operation.</returns>
        public IObservable<RxVoid> InjectApplicationMessageOperation(InjectedMqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.InjectApplicationMessage(message, cancellationToken);
                return operation;
            });
        }

        /// <summary>Injects an application message through an asynchronous observable.</summary>
        /// <param name="message">The injected application message.</param>
        /// <returns>A cold asynchronous injection operation.</returns>
        public IObservableAsync<RxVoid> ObserveInjectApplicationMessage(InjectedMqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.InjectApplicationMessage(message, cancellationToken);
                return operation;
            });
        }

        /// <summary>Starts the server when subscribed.</summary>
        /// <returns>A cold start operation.</returns>
        public IObservable<RxVoid> Start() => CreateObservable.FromTask(cancellationToken =>
        {
            var operation = server.StartAsync();
            return operation.WaitAsync(cancellationToken);
        });

        /// <summary>Starts the server through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous start operation.</returns>
        public IObservableAsync<RxVoid> ObserveStart() => CreateObservable.FromTaskSignal(cancellationToken =>
        {
            var operation = server.StartAsync();
            return operation.WaitAsync(cancellationToken);
        });

        /// <summary>Stops the server using default options when subscribed.</summary>
        /// <returns>A cold stop operation.</returns>
        public IObservable<RxVoid> Stop() => server.Stop(new MqttServerStopOptions());

        /// <summary>Stops the server when subscribed.</summary>
        /// <param name="options">The stop options.</param>
        /// <returns>A cold stop operation.</returns>
        public IObservable<RxVoid> Stop(MqttServerStopOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.StopAsync(options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Stops the server with fluent stop-option configuration.</summary>
        /// <param name="configure">Configures the stop options.</param>
        /// <returns>A cold stop operation.</returns>
        public IObservable<RxVoid> Stop(Action<MqttServerStopOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerStopOptionsBuilder();
            configure(builder);
            return server.Stop(builder.Build());
        }

        /// <summary>Stops the server asynchronously using default options.</summary>
        /// <returns>A cold asynchronous stop operation.</returns>
        public IObservableAsync<RxVoid> ObserveStop() => server.ObserveStop(new MqttServerStopOptions());

        /// <summary>Stops the server through an asynchronous observable.</summary>
        /// <param name="options">The stop options.</param>
        /// <returns>A cold asynchronous stop operation.</returns>
        public IObservableAsync<RxVoid> ObserveStop(MqttServerStopOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.StopAsync(options);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Stops the server asynchronously with fluent stop-option configuration.</summary>
        /// <param name="configure">Configures the stop options.</param>
        /// <returns>A cold asynchronous stop operation.</returns>
        public IObservableAsync<RxVoid> ObserveStop(Action<MqttServerStopOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttServerStopOptionsBuilder();
            configure(builder);
            return server.ObserveStop(builder.Build());
        }

        /// <summary>Subscribes a client to topic filters when subscribed.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="topicFilters">The topic filters.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<RxVoid> SubscribeClient(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(topicFilters);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.SubscribeAsync(clientId, topicFilters);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Subscribes a client using one fluently configured topic filter.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="configure">Configures the topic filter.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<RxVoid> SubscribeClient(string clientId, Action<MqttTopicFilterBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttTopicFilterBuilder();
            configure(builder);
            return server.SubscribeClient(clientId, [builder.Build()]);
        }

        /// <summary>Subscribes a client to topic filters asynchronously.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="topicFilters">The topic filters.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<RxVoid> ObserveSubscribeClient(
            string clientId,
            ICollection<MqttTopicFilter> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(topicFilters);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.SubscribeAsync(clientId, topicFilters);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Subscribes a client asynchronously using one fluently configured topic filter.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="configure">Configures the topic filter.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<RxVoid> ObserveSubscribeClient(
            string clientId,
            Action<MqttTopicFilterBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttTopicFilterBuilder();
            configure(builder);
            return server.ObserveSubscribeClient(clientId, [builder.Build()]);
        }

        /// <summary>Unsubscribes a client from topic filters when subscribed.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="topicFilters">The topic names.</param>
        /// <returns>A cold unsubscribe operation.</returns>
        public IObservable<RxVoid> UnsubscribeClient(string clientId, ICollection<string> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(topicFilters);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.UnsubscribeAsync(clientId, topicFilters);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Unsubscribes a client from topic filters asynchronously.</summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="topicFilters">The topic names.</param>
        /// <returns>A cold asynchronous unsubscribe operation.</returns>
        public IObservableAsync<RxVoid> ObserveUnsubscribeClient(
            string clientId,
            ICollection<string> topicFilters)
        {
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(topicFilters);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.UnsubscribeAsync(clientId, topicFilters);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Updates one retained message when subscribed.</summary>
        /// <param name="message">The retained application message.</param>
        /// <returns>A cold retained-message update.</returns>
        public IObservable<RxVoid> UpdateRetainedMessage(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTask(cancellationToken =>
            {
                var operation = server.UpdateRetainedMessageAsync(message);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Updates one retained message through an asynchronous observable.</summary>
        /// <param name="message">The retained application message.</param>
        /// <returns>A cold asynchronous retained-message update.</returns>
        public IObservableAsync<RxVoid> ObserveUpdateRetainedMessage(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = server.UpdateRetainedMessageAsync(message);
                return operation.WaitAsync(cancellationToken);
            });
        }
    }
}
