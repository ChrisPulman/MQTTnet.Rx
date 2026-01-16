// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Server;

namespace MQTTnet.Rx.Server;

/// <summary>
/// Provides factory methods and properties for creating and managing MQTT server instances.
/// </summary>
/// <remarks>The Create class offers static members to configure and instantiate MQTT servers using a customizable
/// factory. It is intended for scenarios where MQTT server creation and lifecycle management need to be centralized or
/// shared across an application.</remarks>
public static class Create
{
    /// <summary>
    /// Gets the default factory instance for creating MQTT server components.
    /// </summary>
    /// <remarks>Use this property to obtain a shared instance of the MQTT server factory. The returned
    /// factory can be used to create and configure MQTT server instances throughout the application.</remarks>
    public static MqttServerFactory MqttFactory { get; private set; } = new();

    /// <summary>
    /// Sets the MQTT server factory to use for creating MQTT server instances.
    /// </summary>
    /// <param name="mqttFactory">The factory instance that will be used to create MQTT servers. Cannot be null.</param>
    public static void NewMqttFactory(MqttServerFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>
    /// Creates and starts an MQTT server as an observable sequence, allowing subscribers to manage the server's
    /// lifetime and resources.
    /// </summary>
    /// <remarks>The server instance is shared among all subscribers. The server is started automatically when
    /// the first observer subscribes and is stopped and disposed when the last observer unsubscribes. The returned
    /// disposable should be disposed to release resources and stop the server when no longer needed. This method
    /// retries the observable sequence on error, ensuring resilience to transient failures.</remarks>
    /// <param name="builder">A delegate that configures and returns the options for the MQTT server. Cannot be null.</param>
    /// <returns>An observable sequence that emits a tuple containing the started MQTT server instance and a disposable for
    /// managing its lifetime. The server is started when the first subscription is made and stopped when the last
    /// subscription is disposed.</returns>
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
