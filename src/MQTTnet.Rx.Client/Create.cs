// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.ResilientClient.Internal;
#else
using MQTTnet.Rx.Client.ResilientClient.Internal;
#endif
using ReactiveUI.Primitives.Async;
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

/// <summary>Provides factory methods for creating MQTT clients and related options.</summary>
/// <remarks>The Create class offers static members to facilitate the creation and configuration of MQTT clients,
/// resilient clients, and their associated options builders. It is intended to simplify the setup and management of
/// MQTT client instances in applications that use reactive programming patterns. All members are thread-safe and
/// designed for use in multi-threaded environments.</remarks>
public static class Create
{
    /// <summary>Stores the current MQTT client factory.</summary>
    private static MqttClientFactory _mqttFactory = new();

    /// <summary>Gets the default factory instance for creating MQTT clients.</summary>
    /// <remarks>Use this property to obtain a shared instance of the MQTT client factory when creating new
    /// MQTT client connections. The returned factory is thread-safe and intended for reuse throughout the
    /// application.</remarks>
    public static MqttClientFactory MqttFactory => Volatile.Read(ref _mqttFactory);

    /// <summary>Sets the global MQTT client factory instance to use for creating MQTT clients.</summary>
    /// <remarks>Use this method to replace the default MQTT client factory with a custom implementation. This
    /// affects all future MQTT client creation operations that rely on the global factory instance.</remarks>
    /// <param name="mqttFactory">The MQTT client factory to be used for subsequent client creation. Cannot be
    /// null.</param>
    public static void NewMqttFactory(MqttClientFactory mqttFactory)
    {
        ArgumentNullException.ThrowIfNull(mqttFactory);
        _ = Interlocked.Exchange(ref _mqttFactory, mqttFactory);
    }

    /// <summary>Creates an observable sequence that provides a shared instance of an MQTT client.</summary>
    /// <remarks>The returned observable shares a single underlying MQTT client instance among all
    /// subscribers. The client is disposed automatically when the last subscription is disposed. Subscribers should not
    /// dispose the client directly. The observable sequence will retry on errors, resubscribing as needed.</remarks>
    /// <returns>An observable sequence that emits a single shared <see cref="IMqttClient"/> instance to each
    /// subscriber. The
    /// client is disposed when all subscriptions are disposed.</returns>
    public static IObservable<IMqttClient> MqttClient()
    {
        var lifetime = new SharedClientLifetime<IMqttClient>(static () => MqttFactory.CreateMqttClient());
        return CreateObservable.RetryForever(
            Signal.Create<IMqttClient>(observer =>
            {
                var lease = lifetime.Acquire();
                return NotifyObserver(observer, lease);
            }));
    }

    /// <summary>Creates an asynchronous observable sequence that provides a shared MQTT client.</summary>
    /// <returns>An asynchronous observable sequence that emits a shared <see cref="IMqttClient"/> instance.</returns>
    public static IObservableAsync<IMqttClient> MqttClientSignal()
    {
        var lifetime = new SharedClientLifetime<IMqttClient>(static () => MqttFactory.CreateMqttClient());
        return SignalAsync
            .Create<IMqttClient>(
                async (observer, cancellationToken) =>
                {
                    var lease = lifetime.Acquire();
                    return await NotifyObserverAsync(observer, lease, cancellationToken).ConfigureAwait(false);
                })
            .Retry();
    }

    /// <summary>Creates an observable sequence that provides a shared resilient MQTT client.</summary>
    /// <remarks>The returned observable ensures that the underlying MQTT client is shared among all
    /// subscribers and is disposed only when the last subscription is disposed. If a subscriber unsubscribes and then
    /// resubscribes, a new subscription will reuse the same client instance as long as at least one subscription
    /// remains active. The observable automatically retries on errors, providing resilience against transient
    /// failures.</remarks>
    /// <returns>An observable sequence that emits a single instance of an <see cref="IResilientMqttClient"/>. The
    /// client is
    /// disposed when all subscriptions are disposed.</returns>
    public static IObservable<IResilientMqttClient> ResilientMqttClient()
    {
        var lifetime = new SharedClientLifetime<IResilientMqttClient>(
            static () => CreateResilientMqttClient(MqttFactory));
        return CreateObservable.RetryForever(
            Signal.Create<IResilientMqttClient>(observer =>
            {
                var lease = lifetime.Acquire();
                return NotifyObserver(observer, lease);
            }));
    }

    /// <summary>Creates an asynchronous observable sequence that provides a shared resilient MQTT client.</summary>
    /// <returns>An asynchronous observable sequence that emits a shared <see cref="IResilientMqttClient"/>
    /// instance.</returns>
    public static IObservableAsync<IResilientMqttClient> ResilientMqttClientSignal()
    {
        var lifetime = new SharedClientLifetime<IResilientMqttClient>(
            static () => CreateResilientMqttClient(MqttFactory));
        return SignalAsync
            .Create<IResilientMqttClient>(
                async (observer, cancellationToken) =>
                {
                    var lease = lifetime.Acquire();
                    return await NotifyObserverAsync(observer, lease, cancellationToken).ConfigureAwait(false);
                })
            .Retry();
    }

    /// <summary>Configures MQTT clients in an observable sequence with the specified options.</summary>
    /// <param name="client">The MQTT client sequence to configure.</param>
    /// <param name="optionsBuilder">The action used to configure the MQTT client options.</param>
    /// <returns>An observable sequence of configured MQTT clients.</returns>
    public static IObservable<IMqttClient> WithClientOptions(
        IObservable<IMqttClient> client,
        Action<MqttClientOptionsBuilder> optionsBuilder) => CreateExtensions.WithClientOptions(client, optionsBuilder);

    /// <summary>Configures MQTT clients in an asynchronous observable sequence with the specified options.</summary>
    /// <param name="client">The asynchronous MQTT client sequence to configure.</param>
    /// <param name="optionsBuilder">The action used to configure the MQTT client options.</param>
    /// <returns>An asynchronous observable sequence of configured MQTT clients.</returns>
    public static IObservableAsync<IMqttClient> WithClientOptions(
        IObservableAsync<IMqttClient> client,
        Action<MqttClientOptionsBuilder> optionsBuilder) => CreateExtensions.WithClientOptions(client, optionsBuilder);

    /// <summary>Configures a resilient MQTT client options builder with MQTT client options.</summary>
    /// <param name="builder">The resilient MQTT client options builder to configure.</param>
    /// <param name="clientBuilder">The action used to configure MQTT client options.</param>
    /// <returns>The configured resilient MQTT client options builder.</returns>
    public static ResilientMqttClientOptionsBuilder WithClientOptions(
        ResilientMqttClientOptionsBuilder builder,
        Action<MqttClientOptionsBuilder> clientBuilder) => CreateExtensions.WithClientOptions(builder, clientBuilder);

    /// <summary>Configures resilient MQTT clients in an observable sequence with the specified options.</summary>
    /// <param name="client">The resilient MQTT client sequence to configure.</param>
    /// <param name="optionsBuilder">The action used to configure resilient MQTT client options.</param>
    /// <returns>An observable sequence of configured resilient MQTT clients.</returns>
    public static IObservable<IResilientMqttClient> WithResilientClientOptions(
        IObservable<IResilientMqttClient> client,
        Action<ResilientMqttClientOptionsBuilder> optionsBuilder) =>
        CreateExtensions.WithResilientClientOptions(client, optionsBuilder);

    /// <summary>Configures resilient MQTT client signals with the specified options.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence to configure.</param>
    /// <param name="optionsBuilder">The action used to configure resilient MQTT client options.</param>
    /// <returns>An asynchronous observable sequence of configured resilient MQTT clients.</returns>
    public static IObservableAsync<IResilientMqttClient> WithResilientClientOptions(
        IObservableAsync<IResilientMqttClient> client,
        Action<ResilientMqttClientOptionsBuilder> optionsBuilder) =>
        CreateExtensions.WithResilientClientOptions(client, optionsBuilder);

    /// <summary>Creates a resilient MQTT client options builder.</summary>
    /// <param name="factory">The MQTT client factory.</param>
    /// <returns>A new resilient MQTT client options builder.</returns>
    public static ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder(
        MqttClientFactory factory) => CreateExtensions.CreateResilientClientOptionsBuilder(factory);

    /// <summary>Creates a resilient MQTT client using the supplied factory and optional existing MQTT client.</summary>
    /// <param name="factory">The factory used to create the MQTT client when one is not supplied.</param>
    /// <returns>A new resilient MQTT client.</returns>
    private static ResilientMqttClient CreateResilientMqttClient(MqttClientFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return new(factory.CreateMqttClient(), factory.DefaultLogger);
    }

    /// <summary>Notifies a synchronous observer and releases a rejected client lease.</summary>
    /// <typeparam name="T">The client type.</typeparam>
    /// <param name="observer">The receiving observer.</param>
    /// <param name="lease">The acquired client lease.</param>
    /// <returns>The accepted lease.</returns>
    private static SharedClientLifetime<T>.ClientLease NotifyObserver<T>(
        IObserver<T> observer,
        SharedClientLifetime<T>.ClientLease lease)
        where T : class, IDisposable
    {
        try
        {
            observer.OnNext(lease.Client);
            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    /// <summary>Notifies an asynchronous observer and releases a rejected client lease.</summary>
    /// <typeparam name="T">The client type.</typeparam>
    /// <param name="observer">The receiving observer.</param>
    /// <param name="lease">The acquired client lease.</param>
    /// <param name="cancellationToken">Cancels notification.</param>
    /// <returns>The accepted lease.</returns>
    private static async ValueTask<SharedClientLifetime<T>.ClientLease> NotifyObserverAsync<T>(
        IObserverAsync<T> observer,
        SharedClientLifetime<T>.ClientLease lease,
        CancellationToken cancellationToken)
        where T : class, IDisposable
    {
        try
        {
            await observer.OnNextAsync(lease.Client, cancellationToken).ConfigureAwait(false);
            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    /// <summary>Owns one shared client per active subscription wave.</summary>
    /// <typeparam name="T">The disposable client type.</typeparam>
    /// <param name="factory">Creates a client for a new subscription wave.</param>
    internal sealed class SharedClientLifetime<T>(Func<T> factory)
        where T : class, IDisposable
    {
        /// <summary>Serializes acquisition and release.</summary>
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif

        /// <summary>Stores the active shared client.</summary>
        private T? _client;

        /// <summary>Tracks active leases.</summary>
        private int _leaseCount;

        /// <summary>Acquires a client lease.</summary>
        /// <returns>A lease over the current shared client.</returns>
        internal ClientLease Acquire()
        {
            lock (_gate)
            {
                _client ??= factory();
                _leaseCount++;
                return new(this, _client);
            }
        }

        /// <summary>Releases one lease and disposes the client after the final release.</summary>
        private void Release()
        {
            T? client = null;
            lock (_gate)
            {
                _leaseCount--;
                if (_leaseCount == 0)
                {
                    client = _client;
                    _client = null;
                }
            }

            client?.Dispose();
        }

        /// <summary>Owns one subscription's share of the client lifetime.</summary>
        /// <param name="owner">The shared lifetime owner.</param>
        /// <param name="client">The leased client.</param>
        internal sealed class ClientLease(SharedClientLifetime<T> owner, T client) : IDisposable, IAsyncDisposable
        {
            /// <summary>Tracks whether this lease was released.</summary>
            private int _disposed;

            /// <summary>Gets the leased client.</summary>
            internal T Client { get; } = client;

            /// <inheritdoc/>
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    owner.Release();
                }
            }

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
