// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides unambiguous asynchronous MQTT signal conversions.</summary>
internal static class PrimitivesExtensions
{
    /// <summary>Converts an observable sequence to an asynchronous MQTT signal.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="source">The observable sequence to convert.</param>
    extension<T>(IObservable<T> source)
    {
        /// <summary>Converts the current observable sequence to an asynchronous MQTT signal.</summary>
        /// <returns>The converted asynchronous observable sequence.</returns>
        internal IObservableAsync<T> ToMqttAsyncSignal()
        {
            ArgumentNullException.ThrowIfNull(source);
#if REACTIVE_SHIM
            return MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions.ToSignal(source);
#else
            return MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions.ToSignal(source);
#endif
        }
    }
}
