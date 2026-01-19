// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
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

    /// <summary>
    /// Creates an observable sequence that provides an MQTT server instance with support for persisting retained
    /// messages to disk.
    /// </summary>
    /// <remarks>The observable ensures that retained messages are loaded from and saved to disk using a JSON
    /// file named 'RetainedMessages.json' in the specified directory or temp directory if no path is given. The MQTT server is started when the first
    /// subscription is made and stopped when all subscriptions are disposed. This method is intended for scenarios
    /// where retained message persistence is required across server restarts.</remarks>
    /// <param name="builder">A delegate that configures and returns the options for the MQTT server. Cannot be null.</param>
    /// <param name="retainedMessageDirectory">The directory path where retained messages are stored as a JSON file. If null, the system's temporary directory
    /// is used.</param>
    /// <returns>An observable sequence that emits a tuple containing the created MQTT server and a disposable resource for
    /// managing the server's lifetime. The server persists retained messages to the specified directory.</returns>
    public static IObservable<(MqttServer Server, CompositeDisposable Disposable)> MqttServerWithRetainedMessages(Func<MqttServerOptionsBuilder, MqttServerOptions> builder, string? retainedMessageDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var mqttServer = MqttFactory.CreateMqttServer(builder(MqttFactory.CreateServerOptionsBuilder()));
        IDisposable retainedDisposable;
        var serverCount = 0;
        return Observable.Create<(MqttServer Server, CompositeDisposable Disposable)>(async observer =>
        {
            var disposable = new CompositeDisposable();
            Interlocked.Increment(ref serverCount);
            if (serverCount == 1)
            {
                var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), "RetainedMessages.json");
                retainedDisposable = mqttServer.LoadingRetainedMessage().Subscribe(async e =>
                {
                    try
                    {
                        var models = await JsonSerializer.DeserializeAsync<List<MqttRetainedMessageModel>>(File.OpenRead(storePath)) ?? [];
                        e.LoadedRetainedMessages = models.ConvertAll(m => m.ToApplicationMessage());
                        Console.WriteLine("Retained messages loaded.");
                    }
                    catch (FileNotFoundException)
                    {
                        // Ignore because nothing is stored yet.
                        Console.WriteLine("No retained messages stored yet.");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                });
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
