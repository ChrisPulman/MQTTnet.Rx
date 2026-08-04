// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides complete receiver-preserving access to MQTT server option objects.</summary>
public static class MqttServerOptionsConfigurationExtensions
{
    /// <summary>Provides complete direct access to disconnect options.</summary>
    /// <param name="builder">The disconnect-options builder.</param>
    extension(MqttServerClientDisconnectOptionsBuilder builder)
    {
        /// <summary>Configures the underlying disconnect options while preserving the builder receiver.</summary>
        /// <param name="configure">Configures the disconnect options.</param>
        /// <returns>The original builder.</returns>
        public MqttServerClientDisconnectOptionsBuilder ConfigureOptions(
            Action<MqttServerClientDisconnectOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(builder.Build());
            return builder;
        }
    }

    /// <summary>Provides complete direct access to server options not covered by MQTTnet's named builder methods.</summary>
    /// <param name="builder">The server-options builder.</param>
    extension(MqttServerOptionsBuilder builder)
    {
        /// <summary>Configures the underlying server options while preserving the builder receiver.</summary>
        /// <param name="configure">Configures the server options.</param>
        /// <returns>The original builder.</returns>
        public MqttServerOptionsBuilder ConfigureOptions(Action<MqttServerOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(builder.Build());
            return builder;
        }
    }

    /// <summary>Provides complete direct access to stop options.</summary>
    /// <param name="builder">The stop-options builder.</param>
    extension(MqttServerStopOptionsBuilder builder)
    {
        /// <summary>Configures the underlying stop options while preserving the builder receiver.</summary>
        /// <param name="configure">Configures the stop options.</param>
        /// <returns>The original builder.</returns>
        public MqttServerStopOptionsBuilder ConfigureOptions(Action<MqttServerStopOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            configure(builder.Build());
            return builder;
        }
    }
}
