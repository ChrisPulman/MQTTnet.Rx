// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides cold reactive projections for every resilient MQTT client property.</summary>
public static class ResilientMqttClientPropertyExtensions
{
    /// <summary>Provides resilient-client property projections.</summary>
    /// <param name="client">The resilient MQTT client.</param>
    extension(IResilientMqttClient client)
    {
        /// <summary>Captures every resilient-client property immediately.</summary>
        /// <returns>The current resilient-client property snapshot.</returns>
        public ResilientMqttClientProperties Properties() => new(
            client.InternalClient,
            client.IsConnected,
            client.IsStarted,
            client.Options,
            client.PendingApplicationMessagesCount);

        /// <summary>Reads an arbitrary resilient-client property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<IResilientMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return Signal.FromAsync(_ => Task.FromResult(selector(client)));
        }

        /// <summary>Reads an arbitrary resilient-client property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<IResilientMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return SignalAsync.FromAsync(_ => new ValueTask<T>(selector(client)));
        }

        /// <summary>Captures every resilient-client property once per subscription.</summary>
        /// <returns>A cold resilient-client property snapshot.</returns>
        public IObservable<ResilientMqttClientProperties> PropertySnapshots() =>
            client.Property(static value => value.Properties());

        /// <summary>Captures every resilient-client property once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous resilient-client property snapshot.</returns>
        public IObservableAsync<ResilientMqttClientProperties> ObservePropertySnapshots() =>
            client.ObserveProperty(static value => value.Properties());

        /// <summary>Observes subscription changes synchronously.</summary>
        /// <returns>A synchronous observable sequence of subscription changes.</returns>
        public IObservable<SubscriptionsChangedEventArgs> SubscriptionsChanged() =>
            Signal.Create<SubscriptionsChangedEventArgs>(observer =>
                client.RegisterSubscriptionsChangedHandler((eventArgs, _) =>
                {
                    observer.OnNext(eventArgs);
                    return ValueTask.CompletedTask;
                }));
    }
}
