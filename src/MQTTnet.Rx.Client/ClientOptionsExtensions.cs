// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client;

/// <summary>Provides fluent extensions for configuring MQTT client connections.</summary>
/// <remarks>
/// These extensions simplify the configuration of MQTT client connections with various transport and security options.
/// All methods return the builder for fluent configuration.
/// </remarks>
public static class ClientOptionsExtensions
{
    /// <summary>The secure MQTT port.</summary>
    private const int SecureMqttPort = 8883;

    /// <summary>The default delay before reconnecting.</summary>
    private static readonly TimeSpan DefaultReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>Provides connection-recovery extensions for observable MQTT clients.</summary>
    /// <param name="client">The observable sequence of MQTT clients to monitor.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Monitors connection state changes and automatically reconnects.</summary>
        /// <returns>An observable sequence that emits the client and handles reconnection.</returns>
        public IObservable<IMqttClient> WithAutoReconnect() => client.WithAutoReconnect(null, 0);

        /// <summary>Monitors connection state changes and automatically reconnects.</summary>
        /// <param name="reconnectDelay">The delay before attempting to reconnect.</param>
        /// <returns>An observable sequence that emits the client and handles reconnection.</returns>
        public IObservable<IMqttClient> WithAutoReconnect(TimeSpan? reconnectDelay) =>
            client.WithAutoReconnect(reconnectDelay, 0);

        /// <summary>Monitors connection state changes and automatically reconnects.</summary>
        /// <param name="reconnectDelay">The delay before attempting to reconnect.</param>
        /// <param name="maxReconnectAttempts">The maximum number of reconnect attempts; zero is unlimited.</param>
        /// <returns>An observable sequence that emits the client and handles reconnection.</returns>
        public IObservable<IMqttClient> WithAutoReconnect(
            TimeSpan? reconnectDelay,
            int maxReconnectAttempts)
        {
            var delay = reconnectDelay ?? DefaultReconnectDelay;

            return Signal.Create<IMqttClient>(observer =>
            {
                var disposable = new MultipleDisposable();
                var reconnectState = new ReconnectState();

                void Subscribe(IMqttClient connectedClient)
                {
                    if (connectedClient.IsConnected)
                    {
                        observer.OnNext(connectedClient);
                    }

                    var handler = CreateDisconnectedHandler(
                        connectedClient,
                        delay,
                        maxReconnectAttempts,
                        observer,
                        disposable,
                        reconnectState);
                    connectedClient.DisconnectedAsync += handler;
                    disposable.Add(
                        Scope.Create(
                            (Client: connectedClient, Handler: handler),
                            static state => state.Client.DisconnectedAsync -= state.Handler));
                }

                disposable.Add(
                    client.Subscribe(
                        new DelegateObserver<IMqttClient>(
                            Subscribe,
                            observer.OnError,
                            observer.OnCompleted)));
                return disposable;
            });
        }
    }

    /// <summary>Provides configuration extensions for MQTT client option builders.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    extension(MqttClientOptionsBuilder builder)
    {
        /// <summary>Configures TLS/SSL encryption for the MQTT connection.</summary>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithTlsEnabled()
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.WithTlsOptions(static options => options.UseTls());
        }

        /// <summary>Configures TLS/SSL encryption with a client certificate.</summary>
        /// <param name="certificate">The client certificate to use for authentication.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithTlsClientCertificate(X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(certificate);

            return builder.WithTlsOptions(options =>
                options.UseTls().WithClientCertificates([certificate]));
        }

        /// <summary>Configures TLS/SSL encryption with multiple client certificates.</summary>
        /// <param name="certificates">The collection of client certificates.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithTlsClientCertificates(
            X509Certificate2Collection certificates)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(certificates);

            return builder.WithTlsOptions(options =>
                options.UseTls().WithClientCertificates(certificates));
        }

        /// <summary>Configures TLS/SSL encryption with custom certificate validation.</summary>
        /// <param name="certificateValidationHandler">A callback that validates the server certificate.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithTlsCertificateValidation(
            Func<MqttClientCertificateValidationEventArgs, bool> certificateValidationHandler)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(certificateValidationHandler);

            return builder.WithTlsOptions(options =>
                options.UseTls().WithCertificateValidationHandler(certificateValidationHandler));
        }

        /// <summary>Configures TLS/SSL encryption with the specified protocol versions.</summary>
        /// <param name="sslProtocols">The SSL/TLS protocol versions to allow.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithTlsProtocols(SslProtocols sslProtocols)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.WithTlsOptions(options =>
                options.UseTls().WithSslProtocols(sslProtocols));
        }

        /// <summary>Configures TLS/SSL to trust all certificates.</summary>
        /// <returns>The configured options builder for method chaining.</returns>
        /// <remarks>
        /// WARNING: This should only be used in development or testing environments.
        /// Using this in production makes the connection vulnerable to man-in-the-middle attacks.
        /// </remarks>
        public MqttClientOptionsBuilder WithTlsTrustAllCertificates()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.WithTlsOptions(static options =>
                options
                    .UseTls()
                    .WithIgnoreCertificateChainErrors()
                    .WithIgnoreCertificateRevocationErrors()
                    .WithCertificateValidationHandler(static _ => true));
        }

        /// <summary>Configures the MQTT connection to use WebSocket transport.</summary>
        /// <param name="uri">The WebSocket URI.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithWebSocketUri(string uri)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(uri);

            return builder.WithWebSocketServer(options => options.WithUri(uri));
        }

        /// <summary>Configures username and password authentication for the MQTT connection.</summary>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithUserCredentials(string username, string password)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.WithCredentials(username, password);
        }

        /// <summary>Configures username and password authentication with a binary password.</summary>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password as a byte array.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithUserCredentials(string username, byte[] password)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.WithCredentials(username, password);
        }

        /// <summary>Configures a clean MQTT session with no session-expiry interval.</summary>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithSessionOptions() => builder.WithSessionOptions(true, 0);

        /// <summary>Configures the MQTT session with no session-expiry interval.</summary>
        /// <param name="cleanStart">Whether to start with a clean session.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithSessionOptions(bool cleanStart) =>
            builder.WithSessionOptions(cleanStart, 0);

        /// <summary>Configures the MQTT session.</summary>
        /// <param name="cleanStart">Whether to start with a clean session.</param>
        /// <param name="sessionExpiryInterval">The session expiry interval in seconds.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithSessionOptions(
            bool cleanStart,
            uint sessionExpiryInterval)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder
                .WithCleanStart(cleanStart)
                .WithSessionExpiryInterval(sessionExpiryInterval);
        }

        /// <summary>Configures no explicit keep-alive period or connection timeout.</summary>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithConnectionSettings() =>
            builder.WithConnectionSettings(null, null);

        /// <summary>Configures the MQTT connection keep-alive period.</summary>
        /// <param name="keepAlivePeriod">The keep-alive period.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithConnectionSettings(TimeSpan? keepAlivePeriod) =>
            builder.WithConnectionSettings(keepAlivePeriod, null);

        /// <summary>Configures MQTT connection keep-alive and timeout settings.</summary>
        /// <param name="keepAlivePeriod">The keep-alive period.</param>
        /// <param name="timeout">The connection timeout.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder WithConnectionSettings(
            TimeSpan? keepAlivePeriod,
            TimeSpan? timeout)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (keepAlivePeriod.HasValue)
            {
                _ = builder.WithKeepAlivePeriod(keepAlivePeriod.Value);
            }

            if (timeout.HasValue)
            {
                _ = builder.WithTimeout(timeout.Value);
            }

            return builder;
        }

        /// <summary>Configures the MQTT connection for Azure IoT Hub.</summary>
        /// <param name="iotHubHostname">The IoT Hub hostname.</param>
        /// <param name="deviceId">The device ID.</param>
        /// <param name="sasToken">The SAS token for authentication.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder ForAzureIotHub(
            string iotHubHostname,
            string deviceId,
            string sasToken)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(iotHubHostname);
            ArgumentNullException.ThrowIfNull(deviceId);
            ArgumentNullException.ThrowIfNull(sasToken);

            return builder
                .WithTcpServer(iotHubHostname, SecureMqttPort)
                .WithTlsOptions(static options => options.UseTls())
                .WithCredentials($"{iotHubHostname}/{deviceId}/?api-version=2021-04-12", sasToken)
                .WithClientId(deviceId)
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311);
        }

        /// <summary>Configures the MQTT connection for Azure Event Grid.</summary>
        /// <param name="hostname">The Event Grid namespace MQTT hostname.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="authenticationName">The client authentication name.</param>
        /// <param name="certificate">The client certificate for authentication.</param>
        /// <returns>The configured options builder for method chaining.</returns>
        public MqttClientOptionsBuilder ForAzureEventGrid(
            string hostname,
            string clientId,
            string authenticationName,
            X509Certificate2 certificate)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(hostname);
            ArgumentNullException.ThrowIfNull(clientId);
            ArgumentNullException.ThrowIfNull(authenticationName);
            ArgumentNullException.ThrowIfNull(certificate);

            return builder
                .WithTcpServer(hostname, SecureMqttPort)
                .WithClientId(clientId)
                .WithCredentials(authenticationName, string.Empty)
                .WithTlsClientCertificate(certificate);
        }
    }

    /// <summary>Waits for the configured delay and reconnects the client.</summary>
    /// <param name="client">The client to reconnect.</param>
    /// <param name="delay">The delay before the reconnect attempt.</param>
    /// <param name="maxReconnectAttempts">The maximum number of attempts; zero is unlimited.</param>
    /// <param name="onReconnected">The action to run after reconnecting succeeds.</param>
    /// <param name="onError">The action to run when reconnecting fails.</param>
    /// <param name="cancellationToken">The cancellation token for the scheduled reconnect.</param>
    /// <returns>A task representing the reconnect attempt.</returns>
    private static async Task ReconnectAfterDelayAsync(
        IMqttClient client,
        TimeSpan delay,
        int maxReconnectAttempts,
        Action onReconnected,
        Action<Exception> onError,
        CancellationToken cancellationToken)
    {
        var reconnectAttempts = 0;
        while (true)
        {
            try
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                reconnectAttempts++;
                await client.ReconnectAsync(cancellationToken).ConfigureAwait(false);
                onReconnected();
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
                when (maxReconnectAttempts == 0 || reconnectAttempts < maxReconnectAttempts)
            {
                _ = exception;
            }
            catch (Exception exception)
            {
                onError(exception);
                return;
            }
        }
    }

    /// <summary>Creates the handler that schedules a single reconnect operation after a disconnection.</summary>
    /// <param name="client">The disconnected client.</param>
    /// <param name="delay">The delay before reconnecting.</param>
    /// <param name="maxReconnectAttempts">The maximum number of reconnect attempts; zero is unlimited.</param>
    /// <param name="observer">The observer that receives connection events.</param>
    /// <param name="disposables">The lifetime container for the reconnect operation.</param>
    /// <param name="reconnectState">The reconnect state shared by this source subscription.</param>
    /// <returns>The disconnection handler.</returns>
    private static Func<MqttClientDisconnectedEventArgs, Task> CreateDisconnectedHandler(
        IMqttClient client,
        TimeSpan delay,
        int maxReconnectAttempts,
        IObserver<IMqttClient> observer,
        MultipleDisposable disposables,
        ReconnectState reconnectState) =>
        disconnectedEvent =>
        {
            _ = disconnectedEvent;
            if (!reconnectState.TryBegin())
            {
                return Task.CompletedTask;
            }

            var cancellation = new CancellationTokenSource();
            disposables.Add(Scope.Create(cancellation, static source => _ = source.CancelAsync()));
            _ = ReconnectAfterDelayAsync(
                client,
                delay,
                maxReconnectAttempts,
                () =>
                {
                    reconnectState.Complete();
                    observer.OnNext(client);
                },
                exception =>
                {
                    reconnectState.Complete();
                    observer.OnError(exception);
                },
                cancellation.Token);
            return Task.CompletedTask;
        };

    /// <summary>Tracks whether a reconnect operation is currently running.</summary>
    private sealed class ReconnectState
    {
        private int _inProgress;

        /// <summary>Attempts to mark the reconnect operation as in progress.</summary>
        /// <returns><see langword="true"/> when no reconnect was already in progress.</returns>
        public bool TryBegin() => Interlocked.CompareExchange(ref _inProgress, 1, 0) == 0;

        /// <summary>Marks the reconnect operation as complete.</summary>
        public void Complete() => Volatile.Write(ref _inProgress, 0);
    }

    /// <summary>Adapts delegate callbacks to the standard observable observer contract.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The callback for observed values.</param>
    /// <param name="onError">The callback for terminal errors.</param>
    /// <param name="onCompleted">The callback for successful completion.</param>
    private sealed class DelegateObserver<T>(
        Action<T> onNext,
        Action<Exception> onError,
        Action onCompleted) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted() => onCompleted();

        /// <inheritdoc/>
        public void OnError(Exception error) => onError(error);

        /// <inheritdoc/>
        public void OnNext(T value) => onNext(value);
    }
}
