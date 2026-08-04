// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.AspNetCore.Reactive;
#else
namespace MQTTnet.Rx.AspNetCore;
#endif

/// <summary>Provides return-preserving MQTTnet ASP.NET Core hosting extensions.</summary>
public static class MqttAspNetCoreHostingExtensions
{
    /// <summary>Provides hosted MQTT server configuration.</summary>
    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>Configures the hosted MQTT server resolved from application services.</summary>
        /// <param name="configure">The server configuration callback.</param>
        /// <returns>The same application builder.</returns>
        public IApplicationBuilder ConfigureMqttServer(Action<MqttServer> configure)
        {
            _ = MQTTnet.AspNetCore.ApplicationBuilderExtensions.UseMqttServer(app, configure);
            return app;
        }
    }

    /// <summary>Provides MQTT connection middleware configuration.</summary>
    /// <param name="builder">The connection builder.</param>
    extension(IConnectionBuilder builder)
    {
        /// <summary>Adds the MQTT connection handler to a connection pipeline.</summary>
        /// <returns>The same connection builder.</returns>
        public IConnectionBuilder UseMqttConnectionHandler()
        {
            _ = MQTTnet.AspNetCore.ConnectionBuilderExtensions.UseMqtt(builder);
            return builder;
        }
    }

    /// <summary>Provides MQTT endpoint mapping.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps an MQTT WebSocket endpoint.</summary>
        /// <param name="pattern">The endpoint route pattern.</param>
        /// <returns>The same endpoint route builder.</returns>
        public IEndpointRouteBuilder MapMqttEndpoint(string pattern)
        {
            MQTTnet.AspNetCore.EndpointRouterExtensions.MapMqtt(endpoints, pattern);
            return endpoints;
        }
    }
}
