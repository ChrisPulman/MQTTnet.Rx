// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Disposables;
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

/// <summary>Provides extensions for configuring MQTT clients and resilient MQTT client options.</summary>
public static class CreateExtensions
{
    /// <summary>Provides configuration extensions for synchronous MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence to configure.</param>
    extension(IObservable<IMqttClient> client)
    {

        /// <summary>Configures and connects each MQTT client in the sequence.</summary>
        /// <remarks>If a client in the sequence is already connected, it is emitted immediately. Otherwise, the
        /// method attempts to connect the client using the configured options before emitting it. This method is
        /// typically
        /// used to apply custom connection settings to each client in a reactive workflow.</remarks>
        /// <param name="optionsBuilder">A delegate that configures the MQTT client options using the provided options
        /// builder. This delegate is invoked
        /// for each client before connection.</param>
        /// <returns>An observable sequence of MQTT clients that have been configured and are connected using the
        /// specified options.</returns>
        public IObservable<IMqttClient> WithClientOptions(
            Action<MqttClientOptionsBuilder> optionsBuilder) =>
            Signal.Create<IMqttClient>(observer =>
            {
                var options = Create.MqttFactory.CreateClientOptionsBuilder();
                optionsBuilder(options);
                var subscription = new AssignmentSlot();
                var disposable = new MultipleDisposable(subscription);
                subscription.Create(
                    client.Subscribe(c =>
                    {
                        if (c.IsConnected)
                        {
                            observer.OnNext(c);
                        }
                        else
                        {
                            disposable.Add(
                                Signal
                                    .FromAsync(async token =>
                                    {
                                        await c.ConnectAsync(options.Build(), token)
                                            .ConfigureAwait(false);
                                        return c;
                                    })
                                    .Subscribe(observer.OnNext));
                        }
                    }));
                return disposable;
            });
    }

    /// <summary>Provides configuration extensions for synchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence to configure.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Configures and starts each resilient MQTT client in the sequence.</summary>
        /// <remarks>If a client in the sequence is not started, this method starts it with the configured options
        /// before emitting it to observers. Clients that are already started are emitted immediately without
        /// reconfiguration.</remarks>
        /// <param name="optionsBuilder">A delegate that configures the options for each resilient MQTT client using a
        /// ResilientMqttClientOptionsBuilder.</param>
        /// <returns>An observable sequence of resilient MQTT clients that have been configured with the specified
        /// options and
        /// started if necessary.</returns>
        public IObservable<IResilientMqttClient> WithResilientClientOptions(
            Action<ResilientMqttClientOptionsBuilder> optionsBuilder) =>
            Signal.Create<IResilientMqttClient>(observer =>
            {
                var options = Create.MqttFactory.CreateResilientClientOptionsBuilder();
                optionsBuilder(options);
                var subscription = new AssignmentSlot();
                var disposable = new MultipleDisposable(subscription);
                subscription.Create(
                    client.Subscribe(c =>
                    {
                        if (c.IsStarted)
                        {
                            observer.OnNext(c);
                        }
                        else
                        {
                            disposable.Add(
                                Signal
                                    .FromAsync(async () =>
                                    {
                                        await c.StartAsync(options.Build()).ConfigureAwait(false);
                                        return c;
                                    })
                                    .Subscribe(observer.OnNext));
                        }
                    }));
                return disposable;
            });
    }

    /// <summary>Provides configuration extensions for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence to configure.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Configures and connects each MQTT client in the asynchronous sequence.</summary>
        /// <param name="optionsBuilder">A delegate that configures the MQTT client options using the provided options
        /// builder.</param>
        /// <returns>An asynchronous observable sequence of configured and connected MQTT clients.</returns>
        public IObservableAsync<IMqttClient> WithClientOptions(
            Action<MqttClientOptionsBuilder> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            var options = Create.MqttFactory.CreateClientOptionsBuilder();
            optionsBuilder(options);

            return client.Select(
                async (c, cancellationToken) =>
                {
                    if (!c.IsConnected)
                    {
                        await c.ConnectAsync(options.Build(), cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return c;
                });
        }
    }

    /// <summary>Provides configuration extensions for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence to configure.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Configures and starts each resilient MQTT client in the asynchronous sequence.</summary>
        /// <param name="optionsBuilder">A delegate that configures each resilient client instance.</param>
        /// <returns>An asynchronous observable sequence of configured resilient MQTT clients.</returns>
        public IObservableAsync<IResilientMqttClient> WithResilientClientOptions(
            Action<ResilientMqttClientOptionsBuilder> optionsBuilder)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(optionsBuilder);

            var options = Create.MqttFactory.CreateResilientClientOptionsBuilder();
            optionsBuilder(options);

            return client.Select(
                async (c, _) =>
                {
                    if (!c.IsStarted)
                    {
                        await c.StartAsync(options.Build()).ConfigureAwait(false);
                    }

                    return c;
                });
        }
    }

    /// <summary>Provides factory extensions for MQTT client creation.</summary>
    /// <param name="factory">The MQTT client factory that creates the options builder.</param>
    extension(MqttClientFactory factory)
    {
        /// <summary>Creates an options builder for a resilient MQTT client.</summary>
        /// <remarks>This extension method provides a convenient way to obtain a ResilientMqttClientOptionsBuilder
        /// from an MqttClientFactory. The returned builder can be used to configure advanced options for resilient MQTT
        /// client connections.</remarks>
        /// <returns>A new ResilientMqttClientOptionsBuilder instance for configuring resilient MQTT client
        /// options.</returns>
        public ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder()
        {
            ArgumentNullException.ThrowIfNull(factory);
            return new();
        }
    }

    /// <summary>Provides configuration extensions for resilient MQTT client options builders.</summary>
    /// <param name="builder">The resilient MQTT client options builder to configure.</param>
    extension(ResilientMqttClientOptionsBuilder builder)
    {
        /// <summary>Configures the underlying MQTT client options using the specified builder action.</summary>
        /// <remarks>Use this method to customize MQTT client connection settings, such as credentials, endpoints,
        /// or protocol options, before building the resilient client.</remarks>
        /// <param name="clientBuilder">An action that configures the MQTT client options using the provided <see
        /// cref="MqttClientOptionsBuilder"/>.
        /// Cannot be null.</param>
        /// <returns>The same <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
        public ResilientMqttClientOptionsBuilder WithClientOptions(
            Action<MqttClientOptionsBuilder> clientBuilder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(clientBuilder);

            var optionsBuilder = Create.MqttFactory.CreateClientOptionsBuilder();
            clientBuilder(optionsBuilder);
            _ = builder.WithClientOptions(optionsBuilder);
            return builder;
        }
    }
}
