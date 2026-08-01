// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides JSON serialization extensions used by the Modbus bridge.</summary>
public static class SerializationExtensions
{
    /// <summary>Extends values with JSON serialization.</summary>
    /// <param name="value">The value to serialize.</param>
    extension(object? value)
    {
        /// <summary>Serializes the value to JSON.</summary>
        /// <returns>The JSON representation.</returns>
        public string Serialize() => System.Text.Json.JsonSerializer.Serialize(value);
    }

    /// <summary>Extends JSON strings with typed deserialization.</summary>
    /// <param name="value">The JSON string.</param>
    extension(string value)
    {
        /// <summary>Deserializes the JSON string.</summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        public T? DeSerialize<T>(params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(value);
            return System.Text.Json.JsonSerializer.Deserialize<T>(value);
        }
    }
}
