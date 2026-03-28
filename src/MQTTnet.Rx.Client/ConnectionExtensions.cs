// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI.Extensions.Async;
using ReactiveUI.Extensions.Async.Disposables;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for working with resilient MQTT client observables.
/// </summary>
public static class ConnectionExtensions
{
    /// <summary>
    /// Returns an asynchronous observable sequence that signals when an application message has been processed.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of processed message events.</returns>
    public static IObservableAsync<ApplicationMessageProcessedEventArgs> ObserveApplicationMessageProcessedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<ApplicationMessageProcessedEventArgs>(
            handler => client.ApplicationMessageProcessedAsync += handler,
            handler => client.ApplicationMessageProcessedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when an application message is received.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of received application message events.</returns>
    public static IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveApplicationMessageReceivedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<MqttApplicationMessageReceivedEventArgs>(
            handler => client.ApplicationMessageReceivedAsync += handler,
            handler => client.ApplicationMessageReceivedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when an application message is skipped.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of skipped message events.</returns>
    public static IObservableAsync<ApplicationMessageSkippedEventArgs> ObserveApplicationMessageSkippedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<ApplicationMessageSkippedEventArgs>(
            handler => client.ApplicationMessageSkippedAsync += handler,
            handler => client.ApplicationMessageSkippedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when the resilient client connects.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of connection events.</returns>
    public static IObservableAsync<MqttClientConnectedEventArgs> ObserveConnectedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<MqttClientConnectedEventArgs>(
            handler => client.ConnectedAsync += handler,
            handler => client.ConnectedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when a connection attempt fails.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of failed connection attempts.</returns>
    public static IObservableAsync<ConnectingFailedEventArgs> ObserveConnectingFailedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<ConnectingFailedEventArgs>(
            handler => client.ConnectingFailedAsync += handler,
            handler => client.ConnectingFailedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when the resilient client connection state changes.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of connection state change events.</returns>
    public static IObservableAsync<EventArgs> ObserveConnectionStateChangedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<EventArgs>(
            handler => client.ConnectionStateChangedAsync += handler,
            handler => client.ConnectionStateChangedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when the resilient client disconnects.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of disconnection events.</returns>
    public static IObservableAsync<MqttClientDisconnectedEventArgs> ObserveDisconnectedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<MqttClientDisconnectedEventArgs>(
            handler => client.DisconnectedAsync += handler,
            handler => client.DisconnectedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when subscription synchronization fails.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of subscription synchronization failures.</returns>
    public static IObservableAsync<ResilientProcessFailedEventArgs> ObserveSynchronizingSubscriptionsFailedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<ResilientProcessFailedEventArgs>(
            handler => client.SynchronizingSubscriptionsFailedAsync += handler,
            handler => client.SynchronizingSubscriptionsFailedAsync -= handler);

    /// <summary>
    /// Returns an asynchronous observable sequence that signals when subscriptions change.
    /// </summary>
    /// <param name="client">The resilient MQTT client instance to observe.</param>
    /// <returns>An asynchronous observable sequence of subscription change events.</returns>
    public static IObservableAsync<SubscriptionsChangedEventArgs> ObserveSubscriptionsChangedAsync(this IResilientMqttClient client) =>
        CreateObservable.FromAsyncEventAsync<SubscriptionsChangedEventArgs>(
            handler => client.SubscriptionsChangedAsync += handler,
            handler => client.SubscriptionsChangedAsync -= handler);

    /// <summary>
    /// Returns an observable sequence that emits the resilient MQTT client instance each time it is connected and ready
    /// for use.
    /// </summary>
    /// <remarks>The returned observable is hot and shared among all subscribers. Subscribers will receive the
    /// client instance each time it transitions to a connected state. This is useful for triggering actions that
    /// require an active connection.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT client instances to monitor for readiness.</param>
    /// <returns>An observable sequence that produces the client instance whenever it becomes connected. The sequence emits
    /// immediately if the client is already connected, and on each subsequent connection event.</returns>
    public static IObservable<IResilientMqttClient> WhenReady(this IObservable<IResilientMqttClient> client) =>
        Observable.Create<IResilientMqttClient>(observer =>
        {
            var cd = new CompositeDisposable();
            cd.Add(client.Subscribe(c =>
            {
                // Emit immediately if already connected
                if (c.IsConnected)
                {
                    observer.OnNext(c);
                }

                // Track connection changes
                var d = new CompositeDisposable
                {
                    c.Connected.Subscribe(_ => observer.OnNext(c)),
                    c.Disconnected.Subscribe(_ => { })
                };
                cd.Add(d);
            }));
            return cd;
        }).Retry().Publish().RefCount();

    /// <summary>
    /// Returns an asynchronous observable sequence that emits the resilient MQTT client instance whenever it is connected.
    /// </summary>
    /// <param name="client">An asynchronous observable sequence of resilient MQTT client instances to monitor.</param>
    /// <returns>An asynchronous observable sequence that produces the client whenever it becomes connected.</returns>
    public static IObservableAsync<IResilientMqttClient> WhenReady(this IObservableAsync<IResilientMqttClient> client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return ObservableAsync.Create<IResilientMqttClient>(async (observer, cancellationToken) =>
            {
                var disposables = new CompositeDisposableAsync();

                await disposables.AddAsync(
                    await client.SubscribeAsync(
                        async (c, ct) =>
                        {
                            if (c.IsConnected)
                            {
                                await observer.OnNextAsync(c, ct).ConfigureAwait(false);
                            }

                            await disposables.AddAsync(
                                await c.ObserveConnectedAsync()
                                    .SubscribeAsync((_, token) => observer.OnNextAsync(c, token), ct)
                                    .ConfigureAwait(false))
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false))
                    .ConfigureAwait(false);

                return disposables;
            })
            .Retry()
            .Publish()
            .RefCount();
    }
}
