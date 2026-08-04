// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides cold reactive projections for every public MQTT server property.</summary>
public static class MqttServerPropertyExtensions
{
    /// <summary>Provides property projections for an MQTT server.</summary>
    /// <param name="server">The MQTT server.</param>
    extension(MqttServer server)
    {
        /// <summary>Captures all public server properties immediately.</summary>
        /// <returns>The current server-property snapshot.</returns>
        public MqttServerProperties Properties() => new(
            server.AcceptNewConnections,
            server.IsStarted,
            MqttPropertySnapshot.Copy(server.ServerSessionItems));

        /// <summary>Reads an arbitrary server property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<MqttServer, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTask(_ => Task.FromResult(selector(server)));
        }

        /// <summary>Reads an arbitrary server property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<MqttServer, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTaskSignal(_ => Task.FromResult(selector(server)));
        }

        /// <summary>Captures all public server properties once per subscription.</summary>
        /// <returns>A cold server-property snapshot.</returns>
        public IObservable<MqttServerProperties> PropertySnapshots() =>
            server.Property(static value => value.Properties());

        /// <summary>Captures all public server properties once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous server-property snapshot.</returns>
        public IObservableAsync<MqttServerProperties> ObservePropertySnapshots() =>
            server.ObserveProperty(static value => value.Properties());

        /// <summary>Reads whether new connections are accepted once per subscription.</summary>
        /// <returns>A cold property projection.</returns>
        public IObservable<bool> AcceptNewConnectionsValue() =>
            server.Property(static value => value.AcceptNewConnections);

        /// <summary>Reads whether new connections are accepted once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<bool> ObserveAcceptNewConnections() =>
            server.ObserveProperty(static value => value.AcceptNewConnections);

        /// <summary>Copies server session items once per subscription.</summary>
        /// <returns>A cold session-item snapshot.</returns>
        public IObservable<IReadOnlyDictionary<object, object?>> ServerSessionItemsSnapshot() =>
            server.Property(static value => MqttPropertySnapshot.Copy(value.ServerSessionItems));

        /// <summary>Copies server session items once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous session-item snapshot.</returns>
        public IObservableAsync<IReadOnlyDictionary<object, object?>> ObserveServerSessionItemsSnapshot() =>
            server.ObserveProperty(static value => MqttPropertySnapshot.Copy(value.ServerSessionItems));

        /// <summary>Emits the current started state and every subsequent lifecycle transition.</summary>
        /// <returns>A lifecycle-state sequence.</returns>
        public IObservable<bool> IsStartedChanges() =>
            SignalFactory.Create<bool>(observer => new ServerStateSubscription(server, observer));

        /// <summary>Emits the current started state and every subsequent lifecycle transition asynchronously.</summary>
        /// <returns>An asynchronous lifecycle-state sequence.</returns>
        public IObservableAsync<bool> ObserveIsStartedChanges() =>
            SignalAsync.Create<bool>(async (observer, cancellationToken) =>
            {
                var subscription = new ServerStateAsyncSubscription(server, observer, cancellationToken);
                await subscription.InitializeAsync().ConfigureAwait(false);
                return subscription;
            });
    }

    /// <summary>Serializes and owns a synchronous lifecycle-state subscription.</summary>
    private sealed class ServerStateSubscription : IDisposable
    {
        /// <summary>Serializes notifications and state changes.</summary>
#if NET9_0_OR_GREATER
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif

        /// <summary>Receives lifecycle state.</summary>
        private readonly IObserver<bool> _observer;

        /// <summary>The observed server.</summary>
        private readonly MqttServer _server;

        /// <summary>Tracks disposal.</summary>
        private bool _disposed;

        /// <summary>Tracks whether a state has been emitted.</summary>
        private bool _hasValue;

        /// <summary>Stores the last emitted state.</summary>
        private bool _value;

        /// <summary>Initializes a new instance of the <see cref="ServerStateSubscription"/> class.</summary>
        /// <param name="server">The observed server.</param>
        /// <param name="observer">The lifecycle observer.</param>
        internal ServerStateSubscription(MqttServer server, IObserver<bool> observer)
        {
            _server = server;
            _observer = observer;
            lock (_gate)
            {
                server.StartedAsync += OnStartedAsync;
                server.StoppedAsync += OnStoppedAsync;
                try
                {
                    Publish(server.IsStarted);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            _server.StartedAsync -= OnStartedAsync;
            _server.StoppedAsync -= OnStoppedAsync;
        }

        /// <summary>Handles a server started event.</summary>
        /// <param name="_">The unused event arguments.</param>
        /// <returns>A completed task.</returns>
        private Task OnStartedAsync(EventArgs _)
        {
            Publish(true);
            return Task.CompletedTask;
        }

        /// <summary>Handles a server stopped event.</summary>
        /// <param name="_">The unused event arguments.</param>
        /// <returns>A completed task.</returns>
        private Task OnStoppedAsync(EventArgs _)
        {
            Publish(false);
            return Task.CompletedTask;
        }

        /// <summary>Publishes one distinct serialized state.</summary>
        /// <param name="value">The lifecycle state.</param>
        private void Publish(bool value)
        {
            lock (_gate)
            {
                if (_disposed || (_hasValue && _value == value))
                {
                    return;
                }

                _hasValue = true;
                _value = value;
                _observer.OnNext(value);
            }
        }
    }

    /// <summary>Initializes a new instance of the <see cref="ServerStateAsyncSubscription"/> class.</summary>
    /// <param name="server">The observed server.</param>
    /// <param name="observer">The lifecycle observer.</param>
    /// <param name="cancellationToken">Cancels notifications.</param>
    private sealed class ServerStateAsyncSubscription(
        MqttServer server,
        IObserverAsync<bool> observer,
        CancellationToken cancellationToken) : IAsyncDisposable
    {
        /// <summary>Serializes notifications.</summary>
        private readonly Create.LifecycleGate _delivery = new();

        /// <summary>Signals completion of the initial notification.</summary>
        private readonly TaskCompletionSource _initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Tracks disposal.</summary>
        private bool _disposed;

        /// <summary>Tracks whether a state has been emitted.</summary>
        private bool _hasValue;

        /// <summary>Stores the last emitted state.</summary>
        private bool _value;

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            server.StartedAsync -= OnStartedAsync;
            server.StoppedAsync -= OnStoppedAsync;
            _ = _initialized.TrySetResult();
            return ValueTask.CompletedTask;
        }

        /// <summary>Attaches lifecycle handlers and emits the initial state first.</summary>
        /// <returns>A value task that represents initialization.</returns>
        internal async ValueTask InitializeAsync()
        {
            server.StartedAsync += OnStartedAsync;
            server.StoppedAsync += OnStoppedAsync;
            var initialized = false;
            try
            {
                await PublishAsync(server.IsStarted).ConfigureAwait(false);
                initialized = true;
                _ = _initialized.TrySetResult();
            }
            finally
            {
                if (!initialized)
                {
                    await DisposeAsync().ConfigureAwait(false);
                    _ = _initialized.TrySetResult();
                }
            }
        }

        /// <summary>Handles a server started event.</summary>
        /// <param name="_">The unused event arguments.</param>
        /// <returns>A task that represents the notification.</returns>
        private Task OnStartedAsync(EventArgs _) => PublishAfterInitializationAsync(true);

        /// <summary>Handles a server stopped event.</summary>
        /// <param name="_">The unused event arguments.</param>
        /// <returns>A task that represents the notification.</returns>
        private Task OnStoppedAsync(EventArgs _) => PublishAfterInitializationAsync(false);

        /// <summary>Publishes a lifecycle state after the initial notification.</summary>
        /// <param name="value">The lifecycle state.</param>
        /// <returns>A task that represents the notification.</returns>
        private async Task PublishAfterInitializationAsync(bool value)
        {
            await _initialized.Task.ConfigureAwait(false);
            await PublishAsync(value).ConfigureAwait(false);
        }

        /// <summary>Publishes one distinct serialized lifecycle state.</summary>
        /// <param name="value">The lifecycle state.</param>
        /// <returns>A value task that represents the notification.</returns>
        private async ValueTask PublishAsync(bool value)
        {
            await _delivery.EnterAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (_disposed || (_hasValue && _value == value))
                {
                    return;
                }

                _hasValue = true;
                _value = value;
                await observer.OnNextAsync(value, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _delivery.Exit();
            }
        }
    }
}
