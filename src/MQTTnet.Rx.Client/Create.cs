// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Rx.Client.ResilientClient.Internal;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides factory methods and extension methods for creating and configuring MQTT clients and related options.
/// </summary>
/// <remarks>The Create class offers static members to facilitate the creation and configuration of MQTT clients,
/// resilient clients, and their associated options builders. It is intended to simplify the setup and management of
/// MQTT client instances in applications that use reactive programming patterns. All members are thread-safe and
/// designed for use in multi-threaded environments.</remarks>
public static class Create
{
    /// <summary>
    /// Gets the default factory instance for creating MQTT clients.
    /// </summary>
    /// <remarks>Use this property to obtain a shared instance of the MQTT client factory when creating new
    /// MQTT client connections. The returned factory is thread-safe and intended for reuse throughout the
    /// application.</remarks>
    public static MqttClientFactory MqttFactory { get; private set; } = new();

    /// <summary>
    /// Sets the global MQTT client factory instance to use for creating MQTT clients.
    /// </summary>
    /// <remarks>Use this method to replace the default MQTT client factory with a custom implementation. This
    /// affects all future MQTT client creation operations that rely on the global factory instance.</remarks>
    /// <param name="mqttFactory">The MQTT client factory to be used for subsequent client creation. Cannot be null.</param>
    public static void NewMqttFactory(MqttClientFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>
    /// Creates an observable sequence that provides a shared instance of an MQTT client.
    /// </summary>
    /// <remarks>The returned observable shares a single underlying MQTT client instance among all
    /// subscribers. The client is disposed automatically when the last subscription is disposed. Subscribers should not
    /// dispose the client directly. The observable sequence will retry on errors, resubscribing as needed.</remarks>
    /// <returns>An observable sequence that emits a single shared <see cref="IMqttClient"/> instance to each subscriber. The
    /// client is disposed when all subscriptions are disposed.</returns>
    public static IObservable<IMqttClient> MqttClient()
    {
        var mqttClient = MqttFactory.CreateMqttClient();
        var clientCount = 0;
        return Observable.Create<IMqttClient>(observer =>
            {
                observer.OnNext(mqttClient);
                Interlocked.Increment(ref clientCount);
                return Disposable.Create(() =>
                {
                    if (Interlocked.Decrement(ref clientCount) == 0)
                    {
                        mqttClient.Dispose();
                    }
                });
            }).Retry();
    }

    /// <summary>
    /// Creates an observable sequence that provides a resilient MQTT client instance, automatically handling
    /// reconnections and resource management.
    /// </summary>
    /// <remarks>The returned observable ensures that the underlying MQTT client is shared among all
    /// subscribers and is disposed only when the last subscription is disposed. If a subscriber unsubscribes and then
    /// resubscribes, a new subscription will reuse the same client instance as long as at least one subscription
    /// remains active. The observable automatically retries on errors, providing resilience against transient
    /// failures.</remarks>
    /// <returns>An observable sequence that emits a single instance of an <see cref="IResilientMqttClient"/>. The client is
    /// disposed when all subscriptions are disposed.</returns>
    public static IObservable<IResilientMqttClient> ResilientMqttClient()
    {
        var mqttClient = MqttFactory.CreateResilientMqttClient();
        var clientCount = 0;
        return Observable.Create<IResilientMqttClient>(observer =>
            {
                observer.OnNext(mqttClient);
                Interlocked.Increment(ref clientCount);
                return Disposable.Create(() =>
                {
                    if (Interlocked.Decrement(ref clientCount) == 0)
                    {
                        mqttClient.Dispose();
                    }
                });
            }).Retry();
    }

    /// <summary>
    /// Configures each MQTT client in the observable sequence with the specified client options before connecting.
    /// </summary>
    /// <remarks>If a client in the sequence is already connected, it is emitted immediately. Otherwise, the
    /// method attempts to connect the client using the configured options before emitting it. This method is typically
    /// used to apply custom connection settings to each client in a reactive workflow.</remarks>
    /// <param name="client">An observable sequence of MQTT clients to be configured and connected.</param>
    /// <param name="optionsBuilder">A delegate that configures the MQTT client options using the provided options builder. This delegate is invoked
    /// for each client before connection.</param>
    /// <returns>An observable sequence of MQTT clients that have been configured and are connected using the specified options.</returns>
    public static IObservable<IMqttClient> WithClientOptions(this IObservable<IMqttClient> client, Action<MqttClientOptionsBuilder> optionsBuilder) =>
        Observable.Create<IMqttClient>(observer =>
        {
            var options = MqttFactory.CreateClientOptionsBuilder();
            optionsBuilder(options);
            var disposable = new CompositeDisposable();
            disposable.Add(client.Subscribe(c =>
            {
                if (c.IsConnected)
                {
                    observer.OnNext(c);
                }
                else
                {
                    disposable.Add(Observable.StartAsync(async token => await c.ConnectAsync(options.Build(), token)).Subscribe(_ => observer.OnNext(c)));
                }
            }));
            return disposable;
        });

    /// <summary>
    /// Configures each resilient MQTT client in the observable sequence using the specified options builder before
    /// starting the client if it is not already started.
    /// </summary>
    /// <remarks>If a client in the sequence is not started, this method starts it with the configured options
    /// before emitting it to observers. Clients that are already started are emitted immediately without
    /// reconfiguration.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients to be configured.</param>
    /// <param name="optionsBuilder">A delegate that configures the options for each resilient MQTT client using a ResilientMqttClientOptionsBuilder.</param>
    /// <returns>An observable sequence of resilient MQTT clients that have been configured with the specified options and
    /// started if necessary.</returns>
    public static IObservable<IResilientMqttClient> WithResilientClientOptions(this IObservable<IResilientMqttClient> client, Action<ResilientMqttClientOptionsBuilder> optionsBuilder) =>
        Observable.Create<IResilientMqttClient>(observer =>
        {
            var options = MqttFactory.CreateResilientClientOptionsBuilder();
            optionsBuilder(options);
            var disposable = new CompositeDisposable();
            disposable.Add(client.Subscribe(c =>
            {
                if (c.IsStarted)
                {
                    observer.OnNext(c);
                }
                else
                {
                    disposable.Add(Observable.StartAsync(async () => await c.StartAsync(options.Build())).Subscribe(_ => observer.OnNext(c)));
                }
            }));
            return disposable;
        });

    /// <summary>
    /// Configures the underlying MQTT client options using the specified builder action.
    /// </summary>
    /// <remarks>Use this method to customize MQTT client connection settings, such as credentials, endpoints,
    /// or protocol options, before building the resilient client.</remarks>
    /// <param name="builder">The builder instance to configure. Cannot be null.</param>
    /// <param name="clientBuilder">An action that configures the MQTT client options using the provided <see cref="MqttClientOptionsBuilder"/>.
    /// Cannot be null.</param>
    /// <returns>The same <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public static ResilientMqttClientOptionsBuilder WithClientOptions(this ResilientMqttClientOptionsBuilder builder, Action<MqttClientOptionsBuilder> clientBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(clientBuilder);

        var optionsBuilder = MqttFactory.CreateClientOptionsBuilder();
        clientBuilder(optionsBuilder);
        builder.WithClientOptions(optionsBuilder);
        return builder;
    }

    /// <summary>
    /// Creates a new instance of the ResilientMqttClientOptionsBuilder for configuring resilient MQTT client options.
    /// </summary>
    /// <remarks>This extension method provides a convenient way to obtain a ResilientMqttClientOptionsBuilder
    /// from an MqttClientFactory. The returned builder can be used to configure advanced options for resilient MQTT
    /// client connections.</remarks>
    /// <param name="factory">The MqttClientFactory instance used to extend with resilient client options builder functionality. This
    /// parameter is not used within the method.</param>
    /// <returns>A new ResilientMqttClientOptionsBuilder instance for configuring resilient MQTT client options.</returns>
#pragma warning disable RCS1175 // Unused 'this' parameter.
    public static ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder(this MqttClientFactory factory) => new();
#pragma warning restore RCS1175 // Unused 'this' parameter.

    /// <summary>
    /// Creates a new instance of the ResilientMqttClient using the specified MQTT client or a newly created one.
    /// </summary>
    /// <param name="factory">The factory used to create a new MQTT client if one is not provided. Cannot be null.</param>
    /// <param name="mqttClient">An optional existing MQTT client to use. If null, a new client is created using the factory.</param>
    /// <returns>A ResilientMqttClient instance that wraps the provided or newly created MQTT client.</returns>
    private static ResilientMqttClient CreateResilientMqttClient(this MqttClientFactory factory, IMqttClient? mqttClient = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return mqttClient == null
            ? new ResilientMqttClient(factory.CreateMqttClient(), factory.DefaultLogger)
            : new ResilientMqttClient(mqttClient, factory.DefaultLogger);
    }
}
