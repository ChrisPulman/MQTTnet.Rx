// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client;

/// <summary>Provides extension methods for working with resilient MQTT client observables.</summary>
public static class ConnectionExtensions
{
    /// <summary>Provides readiness extensions for synchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The observable sequence of resilient MQTT clients.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Emits each client when it is ready for use.</summary>
        /// <returns>A shared sequence that emits a client immediately when connected and after each
        /// reconnection.</returns>
        public IObservable<IResilientMqttClient> WhenReady() =>
            CreateObservable
                .RetryForever(
                    Signal.Create<IResilientMqttClient>(observer =>
                    {
                        var subscription = new AssignmentSlot();
                        var disposables = new MultipleDisposable(subscription);
                        subscription.Create(
                            client.Subscribe(connectedClient =>
                            {
                                if (connectedClient.IsConnected)
                                {
                                    observer.OnNext(connectedClient);
                                }

                                disposables.Add(
                                    new MultipleDisposable
                                    {
                                        connectedClient.Connected.Subscribe(_ =>
                                            observer.OnNext(connectedClient)),
                                        connectedClient.Disconnected.Subscribe(static _ => { }),
                                    });
                            }));
                        return disposables;
                    }))
                .Publish()
                .RefCount();
    }

    /// <summary>Provides readiness extensions for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous observable sequence of resilient MQTT clients.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Emits each client when it is ready for use.</summary>
        /// <returns>An asynchronous sequence that emits a client immediately when connected and after each
        /// reconnection.</returns>
        public IObservableAsync<IResilientMqttClient> WhenReady()
        {
            ArgumentNullException.ThrowIfNull(client);

            return SignalAsync
                .Create<IResilientMqttClient>(
                    async (observer, cancellationToken) =>
                    {
                        var disposables = new MultipleDisposableAsync();

                        await disposables
                            .AddAsync(
                                await client
                                    .SubscribeAsync(
                                        async (connectedClient, token) =>
                                        {
                                            if (connectedClient.IsConnected)
                                            {
                                                await observer
                                                    .OnNextAsync(connectedClient, token)
                                                    .ConfigureAwait(false);
                                            }

                                            await disposables
                                                .AddAsync(
                                                    await connectedClient
                                                        .ObserveConnected()
                                                        .SubscribeAsync(
                                                            (_, handlerToken) =>
                                                                observer.OnNextAsync(
                                                                    connectedClient,
                                                                    handlerToken),
                                                            token)
                                                        .ConfigureAwait(false))
                                                .ConfigureAwait(false);
                                        },
                                        cancellationToken)
                                    .ConfigureAwait(false))
                            .ConfigureAwait(false);

                        return disposables;
                    })
                .Retry();
        }
    }

    /// <summary>Provides asynchronous event-observation extensions for resilient MQTT clients.</summary>
    /// <param name="client">The resilient MQTT client to observe.</param>
    extension(IResilientMqttClient client)
    {
        /// <summary>Observes processed application messages.</summary>
        /// <returns>An asynchronous observable sequence of processed message events.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> ObserveApplicationMessageProcessed() =>
            client.ApplicationMessageProcessedAsyncObservable;

        /// <summary>Observes received application messages.</summary>
        /// <returns>An asynchronous observable sequence of received application message events.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveApplicationMessageReceived() =>
            client.ApplicationMessageReceivedAsyncObservable;

        /// <summary>Observes skipped application messages.</summary>
        /// <returns>An asynchronous observable sequence of skipped message events.</returns>
        public IObservableAsync<ApplicationMessageSkippedEventArgs> ObserveApplicationMessageSkipped() =>
            client.ApplicationMessageSkippedAsyncObservable;

        /// <summary>Observes successful resilient-client connections.</summary>
        /// <returns>An asynchronous observable sequence of connection events.</returns>
        public IObservableAsync<MqttClientConnectedEventArgs> ObserveConnected() =>
            client.ConnectedAsyncObservable;

        /// <summary>Observes failed resilient-client connection attempts.</summary>
        /// <returns>An asynchronous observable sequence of failed connection attempts.</returns>
        public IObservableAsync<ConnectingFailedEventArgs> ObserveConnectingFailed() =>
            client.ConnectingFailedAsyncObservable;

        /// <summary>Observes resilient-client connection-state changes.</summary>
        /// <returns>An asynchronous observable sequence of connection-state change events.</returns>
        public IObservableAsync<EventArgs> ObserveConnectionStateChanged() =>
            client.ConnectionStateChangedAsyncObservable;

        /// <summary>Observes resilient-client disconnections.</summary>
        /// <returns>An asynchronous observable sequence of disconnection events.</returns>
        public IObservableAsync<MqttClientDisconnectedEventArgs> ObserveDisconnected() =>
            client.DisconnectedAsyncObservable;

        /// <summary>Observes subscription-synchronization failures.</summary>
        /// <returns>An asynchronous observable sequence of subscription synchronization failures.</returns>
        public IObservableAsync<ResilientProcessFailedEventArgs> ObserveSynchronizingSubscriptionsFailed() =>
            client.SynchronizingSubscriptionsFailedAsyncObservable;

        /// <summary>Observes subscription changes.</summary>
        /// <returns>An asynchronous observable sequence of subscription change events.</returns>
        public IObservableAsync<SubscriptionsChangedEventArgs> ObserveSubscriptionsChanged() =>
            CreateObservable.FromHandlerRegistration<SubscriptionsChangedEventArgs>(
                client.RegisterSubscriptionsChangedHandler);
    }
}
