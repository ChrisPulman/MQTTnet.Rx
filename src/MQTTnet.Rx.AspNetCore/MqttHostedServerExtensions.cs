// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using ServerPropertyExtensions = MQTTnet.Rx.Server.Reactive.MqttServerPropertyExtensions;
namespace MQTTnet.Rx.AspNetCore.Reactive;
#else
using ServerPropertyExtensions = MQTTnet.Rx.Server.MqttServerPropertyExtensions;
namespace MQTTnet.Rx.AspNetCore;
#endif

/// <summary>Provides fluent configuration and reactive state for an ASP.NET Core hosted MQTT server.</summary>
public static class MqttHostedServerExtensions
{
    /// <summary>Provides hosted MQTT server configuration and state.</summary>
    /// <param name="server">The hosted MQTT server.</param>
    extension(MqttHostedServer server)
    {
        /// <summary>Configures whether the server accepts new connections.</summary>
        /// <param name="acceptNewConnections">Whether new clients may connect.</param>
        /// <returns>The same hosted server.</returns>
        public MqttHostedServer WithAcceptNewConnections(bool acceptNewConnections)
        {
            server.AcceptNewConnections = acceptNewConnections;
            return server;
        }

        /// <summary>Adds or replaces an item in the hosted server session dictionary.</summary>
        /// <param name="key">The session-item key.</param>
        /// <param name="value">The session-item value.</param>
        /// <returns>The same hosted server.</returns>
        public MqttHostedServer WithServerSessionItem(object key, object value)
        {
            server.ServerSessionItems[key] = value;
            return server;
        }

        /// <summary>Applies arbitrary fluent configuration to the hosted server.</summary>
        /// <param name="configure">The configuration callback.</param>
        /// <returns>The same hosted server.</returns>
        public MqttHostedServer ConfigureHostedServer(Action<MqttHostedServer> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(server);
            return server;
        }

        /// <summary>Observes the current and subsequent hosted-server started state.</summary>
        /// <returns>An observable state sequence.</returns>
        public IObservable<bool> IsStartedChanges() => ServerPropertyExtensions.IsStartedChanges(server);

        /// <summary>Observes the current and subsequent hosted-server started state asynchronously.</summary>
        /// <returns>An asynchronous observable state sequence.</returns>
        public IObservableAsync<bool> ObserveIsStarted() =>
            ServerPropertyExtensions.ObserveIsStartedChanges(server);
    }
}
