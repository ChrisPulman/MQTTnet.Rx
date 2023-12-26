// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;

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
    public static MqttFactory MqttFactory { get; private set; } = new();

    /// <summary>
    /// Creates the MQTT factory.
    /// </summary>
    /// <param name="mqttFactory">The MQTT factory.</param>
    public static void NewMqttFactory(MqttFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>
    /// Creates a MQTTs server.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>An MqttServer.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IObservable<(MqttServer Server, CompositeDisposable Disposable)> MqttServer(Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
    {
        builder.ThrowArgumentNullExceptionIfNull(nameof(builder));

        var mqttServer = MqttFactory.CreateMqttServer(builder(MqttFactory.CreateServerOptionsBuilder()));
        var serverCount = 0;
        return Observable.Create<(MqttServer Server, CompositeDisposable Disposable)>(async observer =>
        {
            var disposable = new CompositeDisposable();
            observer.OnNext((mqttServer, disposable));
            Interlocked.Increment(ref serverCount);
            if (serverCount == 1)
            {
                await mqttServer.StartAsync();
            }

            return Disposable.Create(async () =>
            {
                Interlocked.Decrement(ref serverCount);
                if (serverCount == 0)
                {
                    await mqttServer.StopAsync();
                    mqttServer.Dispose();
                }

                disposable.Dispose();
            });
        }).Retry();
    }

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
    /// Manageds the MQTT client.
    /// </summary>
    /// <returns>A Managed Mqtt Client.</returns>
    public static IObservable<IManagedMqttClient> ManagedMqttClient()
    {
        var mqttClient = MqttFactory.CreateManagedMqttClient();
        var clientCount = 0;
        return Observable.Create<IManagedMqttClient>(observer =>
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
    /// Withes the managed client options.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <returns>A Managed Mqtt Client.</returns>
    public static IObservable<IManagedMqttClient> WithManagedClientOptions(this IObservable<IManagedMqttClient> client, Action<ManagedMqttClientOptionsBuilder> optionsBuilder) =>
        Observable.Create<IManagedMqttClient>(observer =>
        {
            var mqttClientOptions = MqttFactory.CreateManagedClientOptionsBuilder();
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
    /// <exception cref="System.ArgumentNullException">
    /// builder
    /// or
    /// clientBuilder.
    /// </exception>
    public static ManagedMqttClientOptionsBuilder WithClientOptions(this ManagedMqttClientOptionsBuilder builder, Action<MqttClientOptionsBuilder> clientBuilder)
    {
        builder.ThrowArgumentNullExceptionIfNull(nameof(builder));
        clientBuilder.ThrowArgumentNullExceptionIfNull(nameof(clientBuilder));

        var optionsBuilder = MqttFactory.CreateClientOptionsBuilder();
        clientBuilder(optionsBuilder);
        builder.WithClientOptions(optionsBuilder);
        return builder;
    }

    /// <summary>
    /// Creates the client options builder.
    /// </summary>
    /// <param name="factory">The MqttFactory.</param>
    /// <returns>A Managed Mqtt Client Options Builder.</returns>
#pragma warning disable RCS1175 // Unused 'this' parameter.
    public static ManagedMqttClientOptionsBuilder CreateManagedClientOptionsBuilder(this MqttFactory factory) => new();
#pragma warning restore RCS1175 // Unused 'this' parameter.
}
