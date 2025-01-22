// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Rx.Client.ResilientClient.Internal;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Create.
/// </summary>
public static class Create
{
    /// <summary>
    /// Gets the MQTT factory.
    /// </summary>
    /// <value>
    /// The MQTT factory.
    /// </value>
    public static MqttClientFactory MqttFactory { get; private set; } = new();

    /// <summary>
    /// Creates the MQTT factory.
    /// </summary>
    /// <param name="mqttFactory">The MQTT factory.</param>
    public static void NewMqttFactory(MqttClientFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>
    /// Created a mqtt Client.
    /// </summary>
    /// <returns>An IMqttClient.</returns>
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
                    Interlocked.Decrement(ref clientCount);
                    if (clientCount == 0)
                    {
                        mqttClient.Dispose();
                    }
                });
            }).Retry();
    }

    /// <summary>
    /// Resilient the MQTT client.
    /// </summary>
    /// <returns>A Resilient Mqtt Client.</returns>
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
                    Interlocked.Decrement(ref clientCount);
                    if (clientCount == 0)
                    {
                        mqttClient.Dispose();
                    }
                });
            }).Retry();
    }

    /// <summary>
    /// Withes the client options.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>A Mqtt Client and client Options.</returns>
    public static IObservable<IMqttClient> WithClientOptions(this IObservable<IMqttClient> client, Action<MqttClientOptionsBuilder> optionsBuilder) =>
        Observable.Create<IMqttClient>(observer =>
        {
            var mqttClientOptions = MqttFactory.CreateClientOptionsBuilder();
            optionsBuilder(mqttClientOptions);
            var disposable = new CompositeDisposable();
            disposable.Add(client.Subscribe(c =>
            {
                if (c.IsConnected)
                {
                    observer.OnNext(c);
                }
                else
                {
                    disposable.Add(Observable.StartAsync(async token => await c.ConnectAsync(mqttClientOptions.Build(), token)).Subscribe(_ => observer.OnNext(c)));
                }
            }));
            return disposable;
        });

    /// <summary>
    /// Withes the Resilient client options.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>A Resilient Mqtt Client.</returns>
    public static IObservable<IResilientMqttClient> WithResilientClientOptions(this IObservable<IResilientMqttClient> client, Action<ResilientMqttClientOptionsBuilder> optionsBuilder) =>
        Observable.Create<IResilientMqttClient>(observer =>
        {
            var mqttClientOptions = MqttFactory.CreateResilientClientOptionsBuilder();
            optionsBuilder(mqttClientOptions);
            var disposable = new CompositeDisposable();
            disposable.Add(client.Subscribe(c =>
            {
                if (c.IsStarted)
                {
                    observer.OnNext(c);
                }
                else
                {
                    disposable.Add(Observable.StartAsync(async () => await c.StartAsync(mqttClientOptions.Build())).Subscribe(_ => observer.OnNext(c)));
                }
            }));
            return disposable;
        });

    /// <summary>
    /// Withes the client options.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="clientBuilder">The client builder.</param>
    /// <returns>A ManagedMqttClientOptionsBuilder.</returns>
    /// <exception cref="ArgumentNullException">
    /// builder
    /// or
    /// clientBuilder.
    /// </exception>
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
    /// Creates the client options builder.
    /// </summary>
    /// <param name="factory">The MqttFactory.</param>
    /// <returns>A Resilient Mqtt Client Options Builder.</returns>
#pragma warning disable RCS1175 // Unused 'this' parameter.
    public static ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder(this MqttClientFactory factory) => new();
#pragma warning restore RCS1175 // Unused 'this' parameter.

    /// <summary>
    /// Creates the Resilient MQTT client.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="mqttClient">The MQTT client.</param>
    /// <returns>IResilientMqttClient.</returns>
    /// <exception cref="ArgumentNullException">factory.</exception>
    private static ResilientMqttClient CreateResilientMqttClient(this MqttClientFactory factory, IMqttClient? mqttClient = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (mqttClient == null)
        {
            return new ResilientMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);
        }

        return new ResilientMqttClient(mqttClient, factory.DefaultLogger);
    }
}
