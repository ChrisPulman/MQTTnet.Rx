// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Connection helpers for resilient client to expose a ready (started+connected) stream.
/// </summary>
public static class ConnectionExtensions
{
    /// <summary>
    /// Emits the resilient client only when it is started and connected.
    /// Re-emits after reconnects. Useful to gate publish/subscribe pipelines.
    /// </summary>
    /// <param name="client">The resilient client observable.</param>
    /// <returns>Observable of connected resilient clients.</returns>
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
