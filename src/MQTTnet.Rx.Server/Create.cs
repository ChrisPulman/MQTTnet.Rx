// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Threading.Channels;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Creates MQTT server observable sequences.</summary>
public static class Create
{
    /// <summary>Defines the maximum number of attempts used when starting a server sequence.</summary>
    private const int MaximumServerRetries = 3;

    /// <summary>Defines the retained-message persistence file name.</summary>
    private const string RetainedMessagesFileName = "RetainedMessages.json";

    /// <summary>Gets the MQTT server factory.</summary>
    public static MqttServerFactory MqttFactory { get; private set; } = new();

    /// <summary>Sets the MQTT server factory.</summary>
    /// <param name="mqttFactory">The MQTT server factory.</param>
    public static void NewMqttFactory(MqttServerFactory mqttFactory)
    {
        ArgumentNullException.ThrowIfNull(mqttFactory);
        MqttFactory = mqttFactory;
    }

    /// <summary>Creates an MQTT server observable sequence.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerObservable(() => factory.CreateMqttServer(options));
    }

    /// <summary>Creates an MQTT server observable sequence using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(logger);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerObservable(() => factory.CreateMqttServer(options, logger));
    }

    /// <summary>Creates an MQTT server observable sequence using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerObservable(() => factory.CreateMqttServer(options, serverAdapters));
    }

    /// <summary>Creates an MQTT server observable sequence using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);
        ArgumentNullException.ThrowIfNull(logger);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerObservable(() => factory.CreateMqttServer(options, serverAdapters, logger));
    }

    /// <summary>Creates an asynchronous MQTT server sequence.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerSignal(() => factory.CreateMqttServer(options));
    }

    /// <summary>Creates an asynchronous MQTT server sequence using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(logger);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerSignal(() => factory.CreateMqttServer(options, logger));
    }

    /// <summary>Creates an asynchronous MQTT server sequence using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerSignal(() => factory.CreateMqttServer(options, serverAdapters));
    }

    /// <summary>Creates an asynchronous MQTT server sequence using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);
        ArgumentNullException.ThrowIfNull(logger);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerSignal(() => factory.CreateMqttServer(options, serverAdapters, logger));
    }

    /// <summary>Creates an MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder) =>
        MqttServerWithRetainedMessages(builder, (string?)null);

    /// <summary>Creates an MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesObservable(() => factory.CreateMqttServer(options), storePath);
    }

    /// <summary>Creates an MQTT server sequence with retained messages using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger) =>
        MqttServerWithRetainedMessages(builder, logger, (string?)null);

    /// <summary>Creates an MQTT server sequence with retained messages using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(logger);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesObservable(() => factory.CreateMqttServer(options, logger), storePath);
    }

    /// <summary>Creates an MQTT server sequence with retained messages using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters) =>
        MqttServerWithRetainedMessages(builder, serverAdapters, (string?)null);

    /// <summary>Creates an MQTT server sequence with retained messages using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesObservable(
            () => factory.CreateMqttServer(options, serverAdapters),
            storePath);
    }

    /// <summary>Creates an MQTT server sequence with retained messages using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger) =>
        MqttServerWithRetainedMessages(builder, serverAdapters, logger, (string?)null);

    /// <summary>Creates an MQTT server sequence with retained messages using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);
        ArgumentNullException.ThrowIfNull(logger);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesObservable(
            () => factory.CreateMqttServer(options, serverAdapters, logger),
            storePath);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder) =>
        MqttServerWithRetainedMessagesSignal(builder, (string?)null);

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesSignal(() => factory.CreateMqttServer(options), storePath);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger) =>
        MqttServerWithRetainedMessagesSignal(builder, logger, (string?)null);

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IMqttNetLogger logger,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(logger);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesSignal(() => factory.CreateMqttServer(options, logger), storePath);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters) =>
        MqttServerWithRetainedMessagesSignal(builder, serverAdapters, (string?)null);

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using explicit adapters.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesSignal(
            () => factory.CreateMqttServer(options, serverAdapters),
            storePath);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger) =>
        MqttServerWithRetainedMessagesSignal(builder, serverAdapters, logger, (string?)null);

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages using explicit adapters and a logger.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="serverAdapters">The server adapters used by the created server.</param>
    /// <param name="logger">The MQTTnet logger used by the created server.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        IEnumerable<IMqttServerAdapter> serverAdapters,
        IMqttNetLogger logger,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serverAdapters);
        ArgumentNullException.ThrowIfNull(logger);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), RetainedMessagesFileName);
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        return CreateMqttServerWithRetainedMessagesSignal(
            () => factory.CreateMqttServer(options, serverAdapters, logger),
            storePath);
    }

    /// <summary>Creates an MQTT server observable sequence from a server factory callback.</summary>
    /// <param name="serverFactory">Creates the server.</param>
    /// <returns>An observable server sequence.</returns>
    private static IObservable<(MqttServer Server, MqttServerSession Disposable)> CreateMqttServerObservable(
        Func<MqttServer> serverFactory)
    {
        var lifetime = new MqttServerLifetime(serverFactory);
        return SignalFactory.Create<(MqttServer Server, MqttServerSession Disposable)>(async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return NotifyObserver(observer, session);
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Creates an asynchronous MQTT server sequence from a server factory callback.</summary>
    /// <param name="serverFactory">Creates the server.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    private static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> CreateMqttServerSignal(
        Func<MqttServer> serverFactory)
    {
        var lifetime = new MqttServerLifetime(serverFactory);
        return SignalAsync.Create<(MqttServer Server, MqttServerSession Disposable)>(
            async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return await NotifyObserverAsync(observer, session, cancellationToken).ConfigureAwait(false);
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Creates an MQTT server sequence with retained messages from a server factory callback.</summary>
    /// <param name="serverFactory">Creates the server.</param>
    /// <param name="storePath">The retained-message store path.</param>
    /// <returns>An observable server sequence.</returns>
    private static IObservable<(MqttServer Server, MqttServerSession Disposable)>
        CreateMqttServerWithRetainedMessagesObservable(
        Func<MqttServer> serverFactory,
        string storePath)
    {
        var lifetime = new MqttServerLifetime(serverFactory, storePath);
        return SignalFactory.Create<(MqttServer Server, MqttServerSession Disposable)>(async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return NotifyObserver(observer, session);
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages from a server factory callback.</summary>
    /// <param name="serverFactory">Creates the server.</param>
    /// <param name="storePath">The retained-message store path.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    private static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        CreateMqttServerWithRetainedMessagesSignal(
        Func<MqttServer> serverFactory,
        string storePath)
    {
        var lifetime = new MqttServerLifetime(serverFactory, storePath);
        return SignalAsync.Create<(MqttServer Server, MqttServerSession Disposable)>(
            async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return await NotifyObserverAsync(observer, session, cancellationToken).ConfigureAwait(false);
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Notifies a synchronous observer and releases the session if the observer rejects it.</summary>
    /// <param name="observer">The observer receiving the server session.</param>
    /// <param name="session">The acquired server session.</param>
    /// <returns>The accepted server session.</returns>
    private static MqttServerSession NotifyObserver(
        IObserver<(MqttServer Server, MqttServerSession Disposable)> observer,
        MqttServerSession session)
    {
        try
        {
            observer.OnNext((session.Server, session));
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    /// <summary>Notifies an asynchronous observer and releases the session if the observer rejects it.</summary>
    /// <param name="observer">The observer receiving the server session.</param>
    /// <param name="session">The acquired server session.</param>
    /// <param name="cancellationToken">Cancels the observer notification.</param>
    /// <returns>The accepted server session.</returns>
    private static async ValueTask<MqttServerSession> NotifyObserverAsync(
        IObserverAsync<(MqttServer Server, MqttServerSession Disposable)> observer,
        MqttServerSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            await observer.OnNextAsync((session.Server, session), cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch (Exception exception)
        {
            await session.DisposeAsync().ConfigureAwait(false);
            return await ValueTask.FromException<MqttServerSession>(exception).ConfigureAwait(false);
        }
    }

    /// <summary>Coordinates the lifecycle of a shared MQTT server instance.</summary>
    /// <param name="serverFactory">Creates the MQTT server instance.</param>
    /// <param name="retainedStorePath">The optional retained-message store path.</param>
    internal sealed class MqttServerLifetime(Func<MqttServer> serverFactory, string? retainedStorePath = null)
    {
        /// <summary>Serializes server acquisition and release operations.</summary>
        private readonly LifecycleGate _gate = new();

        /// <summary>Handles retained-message loading while the server is active.</summary>
        private Func<LoadingRetainedMessagesEventArgs, Task>? _retainedHandler;

        /// <summary>Stores the currently active shared server.</summary>
        private MqttServer? _server;

        /// <summary>Tracks the number of active server sessions.</summary>
        private int _subscriptionCount;

        /// <summary>Acquires a session for the shared MQTT server.</summary>
        /// <param name="cancellationToken">Cancels acquisition before the server is available.</param>
        /// <returns>A session that releases the server when disposed.</returns>
        internal async Task<MqttServerSession> AcquireAsync(CancellationToken cancellationToken)
        {
            await _gate.EnterAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_subscriptionCount == 0)
                {
                    var server = serverFactory();
                    _server = server;
                    try
                    {
                        AttachRetainedHandler(server);
                        await server.StartAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        DetachRetainedHandler(server);
                        _server = null;
                        server.Dispose();
                        throw;
                    }
                }

                _subscriptionCount++;
                return new(_server!, ReleaseAsync);
            }
            finally
            {
                _gate.Exit();
            }
        }

        /// <summary>Attaches the retained-message loader when persistent storage is configured.</summary>
        /// <param name="server">The server receiving the retained-message loader.</param>
        private void AttachRetainedHandler(MqttServer server)
        {
            if (retainedStorePath is null || _retainedHandler is not null)
            {
                return;
            }

            _retainedHandler = LoadRetainedMessagesAsync;
            server.LoadingRetainedMessageAsync += _retainedHandler;
        }

        /// <summary>Detaches the retained-message loader from the supplied server.</summary>
        /// <param name="server">The server from which the retained-message loader is removed.</param>
        private void DetachRetainedHandler(MqttServer server)
        {
            if (_retainedHandler is null)
            {
                return;
            }

            server.LoadingRetainedMessageAsync -= _retainedHandler;
            _retainedHandler = null;
        }

        /// <summary>Loads retained messages from persistent storage.</summary>
        /// <param name="eventArgs">The event arguments populated with retained messages.</param>
        /// <returns>A task that represents the asynchronous load operation.</returns>
        private async Task LoadRetainedMessagesAsync(LoadingRetainedMessagesEventArgs eventArgs)
        {
            if (!File.Exists(retainedStorePath))
            {
                return;
            }

            await using var stream = File.OpenRead(retainedStorePath!);
            var models = await JsonSerializer
                .DeserializeAsync<List<MqttRetainedMessageModel>>(stream)
                .ConfigureAwait(false) ?? [];
            eventArgs.LoadedRetainedMessages = models.ConvertAll(static model => model.ToApplicationMessage());
        }

        /// <summary>Releases one server session and stops the server after the final session.</summary>
        /// <returns>A value task that represents the asynchronous release operation.</returns>
        private async ValueTask ReleaseAsync()
        {
            await _gate.EnterAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _subscriptionCount--;
                if (_subscriptionCount != 0)
                {
                    return;
                }

                var server = _server!;
                DetachRetainedHandler(server);
                try
                {
                    await server.StopAsync().ConfigureAwait(false);
                }
                finally
                {
                    _server = null;
                    server.Dispose();
                }
            }
            finally
            {
                _gate.Exit();
            }
        }
    }

    /// <summary>Serializes asynchronous server lifecycle operations.</summary>
    internal sealed class LifecycleGate
    {
        /// <summary>Stores the single token that grants access to the lifecycle gate.</summary>
        private readonly Channel<byte> _tokens = System.Threading.Channels.Channel.CreateBounded<byte>(1);

        /// <summary>Initializes a new instance of the <see cref="LifecycleGate"/> class.</summary>
        internal LifecycleGate() => _ = _tokens.Writer.TryWrite(0);

        /// <summary>Enters the gate.</summary>
        /// <param name="cancellationToken">Cancels the wait to enter the gate.</param>
        /// <returns>A value task that completes after entering the gate.</returns>
        internal async ValueTask EnterAsync(CancellationToken cancellationToken) =>
            _ = await _tokens.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        /// <summary>Exits the gate.</summary>
        internal void Exit() => _ = _tokens.Writer.TryWrite(0);
    }
}
