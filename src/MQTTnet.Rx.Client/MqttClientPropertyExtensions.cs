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

/// <summary>Provides cold reactive projections for every public MQTT client property.</summary>
public static class MqttClientPropertyExtensions
{
    /// <summary>Provides MQTT client property projections.</summary>
    /// <param name="client">The MQTT client.</param>
    extension(IMqttClient client)
    {
        /// <summary>Captures every public client property immediately.</summary>
        /// <returns>The current client-property snapshot.</returns>
        public MqttClientProperties Properties() => new(client.IsConnected, client.Options);

        /// <summary>Reads an arbitrary client property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<IMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return Signal.FromAsync(_ => Task.FromResult(selector(client)));
        }

        /// <summary>Reads an arbitrary client property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<IMqttClient, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return SignalAsync.FromAsync(_ => new ValueTask<T>(selector(client)));
        }

        /// <summary>Captures every public client property once per subscription.</summary>
        /// <returns>A cold client-property snapshot.</returns>
        public IObservable<MqttClientProperties> PropertySnapshots() =>
            client.Property(static value => value.Properties());

        /// <summary>Captures every public client property once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous client-property snapshot.</returns>
        public IObservableAsync<MqttClientProperties> ObservePropertySnapshots() =>
            client.ObserveProperty(static value => value.Properties());

        /// <summary>Reads the connected state once per subscription.</summary>
        /// <returns>A cold connected-state snapshot.</returns>
        public IObservable<bool> IsConnectedValue() =>
            client.Property(static value => value.IsConnected);

        /// <summary>Reads the connected state once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous connected-state snapshot.</returns>
        public IObservableAsync<bool> ObserveIsConnected() =>
            client.ObserveProperty(static value => value.IsConnected);

        /// <summary>Reads the current options once per subscription.</summary>
        /// <returns>A cold options snapshot.</returns>
        public IObservable<MqttClientOptions?> OptionsSnapshot() =>
            client.Property(static value => (MqttClientOptions?)value.Options);

        /// <summary>Reads the current options once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous options snapshot.</returns>
        public IObservableAsync<MqttClientOptions?> ObserveOptionsSnapshot() =>
            client.ObserveProperty(static value => (MqttClientOptions?)value.Options);
    }
}
