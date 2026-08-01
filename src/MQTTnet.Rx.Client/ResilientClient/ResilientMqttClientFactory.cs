// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Rx.Client.ResilientClient.Internal;

namespace MQTTnet.Rx.Client;

/// <summary>Creates resilient MQTT clients around caller-owned MQTTnet clients.</summary>
/// <remarks>
/// This factory is useful when an application needs to retain control of the underlying MQTTnet client, for example
/// to provide a custom transport, diagnostics implementation, or deterministic in-memory client.
/// </remarks>
public static class ResilientMqttClientFactory
{
    /// <summary>Creates a resilient wrapper for an existing MQTTnet client.</summary>
    /// <param name="mqttClient">The MQTTnet client to manage. Cannot be null.</param>
    /// <param name="logger">The logger used by the resilient wrapper. Cannot be null.</param>
    /// <returns>A resilient MQTT client that owns and disposes <paramref name="mqttClient"/>.</returns>
    public static IResilientMqttClient Create(IMqttClient mqttClient, IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(mqttClient);
        ArgumentNullException.ThrowIfNull(logger);

        return new ResilientMqttClient(mqttClient, logger);
    }
}
