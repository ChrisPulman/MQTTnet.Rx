// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.AspNetCore.Reactive;
#else
namespace MQTTnet.Rx.AspNetCore;
#endif

/// <summary>Provides fluent MQTTnet ASP.NET Core service-registration extensions.</summary>
public static class MqttAspNetCoreServiceCollectionExtensions
{
    /// <summary>Provides fluent MQTT service registration.</summary>
    /// <param name="services">The service collection being configured.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds hosted MQTT server services after server options have been registered.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithHostedMqttServer()
        {
            MQTTnet.AspNetCore.ServiceCollectionExtensions.AddHostedMqttServer(services);
            return services;
        }

        /// <summary>Adds a hosted MQTT server using prebuilt options.</summary>
        /// <param name="options">The server options.</param>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithHostedMqttServer(MqttServerOptions options)
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddHostedMqttServer(services, options);
            return services;
        }

        /// <summary>Adds a hosted MQTT server configured by a server-options builder.</summary>
        /// <param name="configure">The optional options-builder callback.</param>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithHostedMqttServer(Action<MqttServerOptionsBuilder>? configure)
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddHostedMqttServer(services, configure);
            return services;
        }

        /// <summary>Adds a hosted MQTT server whose options callback can resolve application services.</summary>
        /// <param name="configure">The service-aware options-builder callback.</param>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithHostedMqttServerServices(Action<AspNetMqttServerOptionsBuilder> configure)
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddHostedMqttServerWithServices(services, configure);
            return services;
        }

        /// <summary>Adds the MQTT connection handler adapter.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttConnectionHandler()
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddMqttConnectionHandler(services);
            return services;
        }

        /// <summary>Adds the ASP.NET Core connections services required by endpoint routing.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttConnections()
        {
            _ = services.AddConnections();
            return services;
        }

        /// <summary>Adds an MQTTnet logger.</summary>
        /// <param name="logger">The logger instance.</param>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttLogger(IMqttNetLogger logger)
        {
            MQTTnet.AspNetCore.ServiceCollectionExtensions.AddMqttLogger(services, logger);
            return services;
        }

        /// <summary>Adds a complete hosted MQTT server with its connection handler.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttServer() => services.WithMqttServer(null);

        /// <summary>Adds a complete hosted MQTT server with its connection handler.</summary>
        /// <param name="configure">The optional server-options callback.</param>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttServer(Action<MqttServerOptionsBuilder>? configure)
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddMqttServer(services, configure);
            return services;
        }

        /// <summary>Adds the MQTT TCP server adapter.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttTcpServerAdapter()
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddMqttTcpServerAdapter(services);
            return services;
        }

        /// <summary>Adds the MQTT WebSocket server adapter.</summary>
        /// <returns>The same service collection.</returns>
        public IServiceCollection WithMqttWebSocketServerAdapter()
        {
            _ = MQTTnet.AspNetCore.ServiceCollectionExtensions.AddMqttWebSocketServerAdapter(services);
            return services;
        }
    }
}
