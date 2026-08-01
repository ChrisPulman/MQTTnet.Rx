// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Threading.Channels;
using MQTTnet.Server;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Server;

/// <summary>Creates MQTT server observable sequences.</summary>
public static class Create
{
    private const int MaximumServerRetries = 3;

    /// <summary>Gets the MQTT server factory.</summary>
    public static MqttServerFactory MqttFactory { get; private set; } = new();

    /// <summary>Sets the MQTT server factory.</summary>
    /// <param name="mqttFactory">The MQTT server factory.</param>
    public static void NewMqttFactory(MqttServerFactory mqttFactory) => MqttFactory = mqttFactory;

    /// <summary>Creates an MQTT server observable sequence.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        var lifetime = new MqttServerLifetime(() => factory.CreateMqttServer(options));
        return Signal.Create<(MqttServer Server, MqttServerSession Disposable)>(async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
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
        }).Retry(MaximumServerRetries);
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
        var lifetime = new MqttServerLifetime(() => factory.CreateMqttServer(options));
        return SignalAsync.Create<(MqttServer Server, MqttServerSession Disposable)>(
            async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync((session.Server, session), cancellationToken).ConfigureAwait(false);
            return session;
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Creates an MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder) =>
        MqttServerWithRetainedMessages(builder, null);

    /// <summary>Creates an MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <param name="retainedMessageDirectory">The retained-message directory.</param>
    /// <returns>An observable server sequence.</returns>
    public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder,
        string? retainedMessageDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), "RetainedMessages.json");
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        var lifetime = new MqttServerLifetime(() => factory.CreateMqttServer(options), storePath);
        return Signal.Create<(MqttServer Server, MqttServerSession Disposable)>(async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
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
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Creates an asynchronous MQTT server sequence with retained messages.</summary>
    /// <param name="builder">Configures the server options.</param>
    /// <returns>An asynchronous observable server sequence.</returns>
    public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)>
        MqttServerWithRetainedMessagesSignal(
        Func<MqttServerOptionsBuilder, MqttServerOptions> builder) =>
        MqttServerWithRetainedMessagesSignal(builder, null);

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

        var storePath = Path.Combine(retainedMessageDirectory ?? Path.GetTempPath(), "RetainedMessages.json");
        var factory = MqttFactory;
        var options = builder(factory.CreateServerOptionsBuilder());
        var lifetime = new MqttServerLifetime(() => factory.CreateMqttServer(options), storePath);
        return SignalAsync.Create<(MqttServer Server, MqttServerSession Disposable)>(
            async (observer, cancellationToken) =>
        {
            var session = await lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync((session.Server, session), cancellationToken).ConfigureAwait(false);
            return session;
        }).Retry(MaximumServerRetries);
    }

    /// <summary>Coordinates the lifecycle of a shared MQTT server instance.</summary>
    /// <param name="serverFactory">Creates the MQTT server instance.</param>
    /// <param name="retainedStorePath">The optional retained-message store path.</param>
    internal sealed class MqttServerLifetime(Func<MqttServer> serverFactory, string? retainedStorePath = null)
    {
        private readonly LifecycleGate _gate = new();

        private Func<LoadingRetainedMessagesEventArgs, Task>? _retainedHandler;

        private MqttServer? _server;

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

        private void AttachRetainedHandler(MqttServer server)
        {
            if (retainedStorePath is null || _retainedHandler is not null)
            {
                return;
            }

            _retainedHandler = LoadRetainedMessagesAsync;
            server.LoadingRetainedMessageAsync += _retainedHandler;
        }

        private void DetachRetainedHandler(MqttServer server)
        {
            if (_retainedHandler is null)
            {
                return;
            }

            server.LoadingRetainedMessageAsync -= _retainedHandler;
            _retainedHandler = null;
        }

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
