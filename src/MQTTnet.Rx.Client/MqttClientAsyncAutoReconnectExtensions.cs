// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides automatic reconnection for asynchronous MQTT client sequences.</summary>
public static class MqttClientAsyncAutoReconnectExtensions
{
    /// <summary>The default delay before reconnecting.</summary>
    private static readonly TimeSpan DefaultReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>Provides connection recovery for asynchronous MQTT client sequences.</summary>
    /// <param name="clients">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> clients)
    {
        /// <summary>Monitors disconnections and reconnects without an attempt limit.</summary>
        /// <returns>The auto-reconnecting client sequence.</returns>
        public IObservableAsync<IMqttClient> WithAutoReconnect() => clients.WithAutoReconnect(null, 0);

        /// <summary>Monitors disconnections and reconnects without an attempt limit.</summary>
        /// <param name="reconnectDelay">The delay before each reconnect attempt.</param>
        /// <returns>The auto-reconnecting client sequence.</returns>
        public IObservableAsync<IMqttClient> WithAutoReconnect(TimeSpan? reconnectDelay) =>
            clients.WithAutoReconnect(reconnectDelay, 0);

        /// <summary>Monitors disconnections and reconnects using the supplied retry policy.</summary>
        /// <param name="reconnectDelay">The delay before each reconnect attempt.</param>
        /// <param name="maxReconnectAttempts">The maximum attempts; zero means unlimited.</param>
        /// <returns>The auto-reconnecting client sequence.</returns>
        public IObservableAsync<IMqttClient> WithAutoReconnect(
            TimeSpan? reconnectDelay,
            int maxReconnectAttempts)
        {
            var delay = reconnectDelay ?? DefaultReconnectDelay;
            return SignalAsync.Create<IMqttClient>(async (observer, cancellationToken) =>
            {
                var disposables = new MultipleDisposableAsync();
                await disposables.AddAsync(
                    await clients.SubscribeAsync(
                        async (client, token) =>
                        {
                            if (client.IsConnected)
                            {
                                await observer.OnNextAsync(client, token).ConfigureAwait(false);
                            }

                            var reconnecting = 0;
                            await disposables.AddAsync(
                                await client.ObserveDisconnected().SubscribeAsync(
                                    async (eventArgs, handlerToken) =>
                                    {
                                        GC.KeepAlive(eventArgs);
                                        GC.KeepAlive(handlerToken);
                                        if (Interlocked.Exchange(ref reconnecting, 1) != 0)
                                        {
                                            return;
                                        }

                                        try
                                        {
                                            await ReconnectAsync(
                                                client,
                                                delay,
                                                maxReconnectAttempts,
                                                observer,
                                                cancellationToken).ConfigureAwait(false);
                                        }
                                        finally
                                        {
                                            _ = Interlocked.Exchange(ref reconnecting, 0);
                                        }
                                    },
                                    token).ConfigureAwait(false)).ConfigureAwait(false);
                        },
                        cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
                return disposables;
            });
        }
    }

    /// <summary>Reconnects until successful, cancelled, or the attempt limit is reached.</summary>
    /// <param name="client">The disconnected client.</param>
    /// <param name="delay">The retry delay.</param>
    /// <param name="maxReconnectAttempts">The maximum attempts; zero means unlimited.</param>
    /// <param name="observer">The downstream observer.</param>
    /// <param name="cancellationToken">Cancels reconnection.</param>
    /// <returns>A task that represents reconnection.</returns>
    private static async Task ReconnectAsync(
        IMqttClient client,
        TimeSpan delay,
        int maxReconnectAttempts,
        IObserverAsync<IMqttClient> observer,
        CancellationToken cancellationToken)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                attempts++;
                await client.ReconnectAsync(cancellationToken).ConfigureAwait(false);
                await observer.OnNextAsync(client, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception) when (maxReconnectAttempts == 0 || attempts < maxReconnectAttempts)
            {
            }
            catch (Exception exception)
            {
                await observer.OnErrorResumeAsync(exception, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }
}
