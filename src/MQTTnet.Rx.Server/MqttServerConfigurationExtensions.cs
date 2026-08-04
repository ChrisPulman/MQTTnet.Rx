// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides receiver-preserving fluent MQTT server configuration.</summary>
public static class MqttServerConfigurationExtensions
{
    /// <summary>Provides direct server configuration.</summary>
    /// <param name="server">The MQTT server.</param>
    extension(MqttServer server)
    {
        /// <summary>Sets whether the server accepts new connections.</summary>
        /// <param name="value">Whether new connections are accepted.</param>
        /// <returns>The original server.</returns>
        public MqttServer WithAcceptNewConnections(bool value)
        {
            server.AcceptNewConnections = value;
            return server;
        }

        /// <summary>Adds or replaces one server session item.</summary>
        /// <param name="key">The session-item key.</param>
        /// <param name="value">The session-item value.</param>
        /// <returns>The original server.</returns>
        public MqttServer WithServerSessionItem(object key, object value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            server.ServerSessionItems[key] = value;
            return server;
        }

        /// <summary>Removes one server session item.</summary>
        /// <param name="key">The session-item key.</param>
        /// <returns>The original server.</returns>
        public MqttServer WithoutServerSessionItem(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            server.ServerSessionItems.Remove(key);
            return server;
        }

        /// <summary>Clears every server session item.</summary>
        /// <returns>The original server.</returns>
        public MqttServer ClearServerSessionItems()
        {
            server.ServerSessionItems.Clear();
            return server;
        }

        /// <summary>Runs arbitrary direct configuration while preserving the server receiver.</summary>
        /// <param name="configure">Configures the server.</param>
        /// <returns>The original server.</returns>
        public MqttServer ConfigureServer(Action<MqttServer> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(server);
            return server;
        }
    }
}
