// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides fluent configuration for reactive MQTT server sequences.</summary>
public static class MqttServerSequenceConfigurationExtensions
{
    /// <summary>Provides fluent configuration for synchronous MQTT server sequences.</summary>
    /// <param name="servers">The server sequence.</param>
    extension(IObservable<MqttServer> servers)
    {
        /// <summary>Configures every emitted server and preserves the sequence.</summary>
        /// <param name="configure">Configures each server.</param>
        /// <returns>The configured server sequence.</returns>
        public IObservable<MqttServer> ConfigureServer(Action<MqttServer> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            return servers.Select(server => server.ConfigureServer(configure));
        }
    }

    /// <summary>Provides fluent configuration for asynchronous MQTT server sequences.</summary>
    /// <param name="servers">The asynchronous server sequence.</param>
    extension(IObservableAsync<MqttServer> servers)
    {
        /// <summary>Configures every emitted server and preserves the asynchronous sequence.</summary>
        /// <param name="configure">Configures each server.</param>
        /// <returns>The configured asynchronous server sequence.</returns>
        public IObservableAsync<MqttServer> ConfigureServer(Action<MqttServer> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            return servers.Select(server => server.ConfigureServer(configure));
        }
    }
}
