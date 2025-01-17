// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Server;

namespace MQTTnet.Rx.Server;

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
    public static MqttServerFactory MqttFactory { get; private set; } = new();

    /// <summary>
    /// Creates the MQTT factory.
    /// </summary>
    /// <param name="mqttFactory">The MQTT factory.</param>
    public static void NewMqttFactory(MqttServerFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>
    /// Creates a MQTTs server.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>An MqttServer.</returns>
    /// <exception cref="System.ArgumentNullException">builder.</exception>
    public static IObservable<(MqttServer Server, CompositeDisposable Disposable)> MqttServer(Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var mqttServer = MqttFactory.CreateMqttServer(builder(MqttFactory.CreateServerOptionsBuilder()));
        var serverCount = 0;
        return Observable.Create<(MqttServer Server, CompositeDisposable Disposable)>(async observer =>
        {
            var disposable = new CompositeDisposable();
            Interlocked.Increment(ref serverCount);
            if (serverCount == 1)
            {
                await mqttServer.StartAsync();
            }

            observer.OnNext((mqttServer, disposable));
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
}
