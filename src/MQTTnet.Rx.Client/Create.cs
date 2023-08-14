// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

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
    /// Created a mqtt Client.
    /// </summary>
    /// <returns>An IMqttClient.</returns>
    public static IObservable<IMqttClient> MqttClient() =>
        Observable.Create<IMqttClient>(observer =>
            {
                var mqttClient = MqttFactory.CreateMqttClient();
                observer.OnNext(mqttClient);
                return Disposable.Create(() => mqttClient.Dispose());
            }).Retry();

    /// <summary>
    /// Manageds the MQTT client.
    /// </summary>
    /// <returns>A Managed Mqtt Client.</returns>
    public static IObservable<IManagedMqttClient> ManagedMqttClient() =>
        Observable.Create<IManagedMqttClient>(observer =>
            {
                var mqttClient = MqttFactory.CreateManagedMqttClient();
                observer.OnNext(mqttClient);
                return Disposable.Create(() => mqttClient.Dispose());
            }).Retry();

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
            disposable.Add(client.Subscribe(c => disposable.Add(Observable.StartAsync(async token => await c.ConnectAsync(mqttClientOptions.Build(), token)).Subscribe(_ => observer.OnNext(c)))));
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
            disposable.Add(client.Subscribe(c => disposable.Add(Observable.StartAsync(async () => await c.StartAsync(mqttClientOptions.Build())).Subscribe(_ => observer.OnNext(c)))));
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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (clientBuilder == null)
        {
            throw new ArgumentNullException(nameof(clientBuilder));
        }

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
