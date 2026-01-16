// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for working with resilient MQTT client observables.
/// </summary>
public static class ConnectionExtensions
{
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
}
