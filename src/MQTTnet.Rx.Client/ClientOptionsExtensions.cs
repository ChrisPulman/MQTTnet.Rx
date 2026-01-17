// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for configuring MQTT client connection options including TLS, WebSocket, and authentication.
/// </summary>
/// <remarks>
/// These extensions simplify the configuration of MQTT client connections with various transport and security options.
/// All methods return the builder for fluent configuration.
/// </remarks>
public static class ClientOptionsExtensions
{
    /// <summary>
    /// Configures TLS/SSL encryption for the MQTT connection.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithTlsEnabled(this MqttClientOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithTlsOptions(o => o.UseTls());
    }

    /// <summary>
    /// Configures TLS/SSL encryption with a client certificate.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="certificate">The client certificate to use for authentication.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithTlsClientCertificate(
        this MqttClientOptionsBuilder builder,
        X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificate);

        return builder.WithTlsOptions(o =>
            o.UseTls()
             .WithClientCertificates([certificate]));
    }

    /// <summary>
    /// Configures TLS/SSL encryption with multiple client certificates.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="certificates">The collection of client certificates.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithTlsClientCertificates(
        this MqttClientOptionsBuilder builder,
        X509Certificate2Collection certificates)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificates);

        return builder.WithTlsOptions(o =>
            o.UseTls()
             .WithClientCertificates(certificates));
    }

    /// <summary>
    /// Configures TLS/SSL encryption with custom certificate validation.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="certificateValidationHandler">
    /// A callback that validates the server certificate. Return true to accept the certificate.
    /// </param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithTlsCertificateValidation(
        this MqttClientOptionsBuilder builder,
        Func<MqttClientCertificateValidationEventArgs, bool> certificateValidationHandler)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(certificateValidationHandler);

        return builder.WithTlsOptions(o =>
            o.UseTls()
             .WithCertificateValidationHandler(certificateValidationHandler));
    }

    /// <summary>
    /// Configures TLS/SSL encryption with specified protocol versions.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="sslProtocols">The SSL/TLS protocol versions to allow.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithTlsProtocols(
        this MqttClientOptionsBuilder builder,
        SslProtocols sslProtocols)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithTlsOptions(o =>
            o.UseTls()
             .WithSslProtocols(sslProtocols));
    }

    /// <summary>
    /// Configures TLS/SSL to trust all certificates (for development/testing only).
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    /// <remarks>
    /// WARNING: This should only be used in development or testing environments.
    /// Using this in production makes the connection vulnerable to man-in-the-middle attacks.
    /// </remarks>
    public static MqttClientOptionsBuilder WithTlsTrustAllCertificates(this MqttClientOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithTlsOptions(o =>
            o.UseTls()
             .WithIgnoreCertificateChainErrors()
             .WithIgnoreCertificateRevocationErrors()
             .WithCertificateValidationHandler(_ => true));
    }

    /// <summary>
    /// Configures the MQTT connection to use WebSocket transport.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="uri">The WebSocket URI (e.g., "ws://broker.example.com/mqtt" or "wss://broker.example.com/mqtt").</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithWebSocketUri(
        this MqttClientOptionsBuilder builder,
        string uri)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(uri);

        return builder.WithWebSocketServer(o => o.WithUri(uri));
    }

    /// <summary>
    /// Configures username and password authentication for the MQTT connection.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithUserCredentials(
        this MqttClientOptionsBuilder builder,
        string username,
        string password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithCredentials(username, password);
    }

    /// <summary>
    /// Configures username and password authentication with binary password.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password as a byte array.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithUserCredentials(
        this MqttClientOptionsBuilder builder,
        string username,
        byte[] password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithCredentials(username, password);
    }

    /// <summary>
    /// Configures automatic reconnection settings for the MQTT connection.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="cleanStart">Whether to start with a clean session. Default is true.</param>
    /// <param name="sessionExpiryInterval">
    /// The session expiry interval in seconds. Set to 0 for session to expire on disconnect.
    /// Default is 0.
    /// </param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithSessionOptions(
        this MqttClientOptionsBuilder builder,
        bool cleanStart = true,
        uint sessionExpiryInterval = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithCleanStart(cleanStart)
            .WithSessionExpiryInterval(sessionExpiryInterval);
    }

    /// <summary>
    /// Configures keep-alive settings for the MQTT connection.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="keepAlivePeriod">The keep-alive period. Default is 15 seconds.</param>
    /// <param name="timeout">The connection timeout. Default is 10 seconds.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder WithConnectionSettings(
        this MqttClientOptionsBuilder builder,
        TimeSpan? keepAlivePeriod = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (keepAlivePeriod.HasValue)
        {
            builder.WithKeepAlivePeriod(keepAlivePeriod.Value);
        }

        if (timeout.HasValue)
        {
            builder.WithTimeout(timeout.Value);
        }

        return builder;
    }

    /// <summary>
    /// Configures the MQTT connection for Azure IoT Hub.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="iotHubHostname">The IoT Hub hostname (e.g., "myhub.azure-devices.net").</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="sasToken">The SAS token for authentication.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder ForAzureIotHub(
        this MqttClientOptionsBuilder builder,
        string iotHubHostname,
        string deviceId,
        string sasToken)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(iotHubHostname);
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(sasToken);

        return builder
            .WithTcpServer(iotHubHostname, 8883)
            .WithTlsOptions(o => o.UseTls(true))
            .WithCredentials($"{iotHubHostname}/{deviceId}/?api-version=2021-04-12", sasToken)
            .WithClientId(deviceId)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311);
    }

    /// <summary>
    /// Configures the MQTT connection for Azure Event Grid.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="hostname">The Event Grid namespace MQTT hostname.</param>
    /// <param name="clientId">The client ID (usually the session identifier).</param>
    /// <param name="authenticationName">The client authentication name.</param>
    /// <param name="certificate">The client certificate for authentication.</param>
    /// <returns>The configured options builder for method chaining.</returns>
    public static MqttClientOptionsBuilder ForAzureEventGrid(
        this MqttClientOptionsBuilder builder,
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
            .WithTcpServer(hostname, 8883)
            .WithClientId(clientId)
            .WithCredentials(authenticationName, string.Empty)
            .WithTlsClientCertificate(certificate);
    }

    /// <summary>
    /// Creates an observable sequence that monitors connection state changes and automatically reconnects.
    /// </summary>
    /// <param name="client">The observable MQTT client to monitor.</param>
    /// <param name="reconnectDelay">The delay before attempting to reconnect. Default is 5 seconds.</param>
    /// <param name="maxReconnectAttempts">Maximum number of reconnect attempts. 0 for unlimited. Default is 0.</param>
    /// <returns>An observable sequence that emits the client when connected and handles reconnection.</returns>
    public static IObservable<IMqttClient> WithAutoReconnect(
        this IObservable<IMqttClient> client,
        TimeSpan? reconnectDelay = null,
        int maxReconnectAttempts = 0)
    {
        var delay = reconnectDelay ?? TimeSpan.FromSeconds(5);

        return Observable.Create<IMqttClient>(observer =>
        {
            var disposable = new CompositeDisposable();
            var reconnectCount = 0;

            void Subscribe(IMqttClient c)
            {
                if (c.IsConnected)
                {
                    reconnectCount = 0;
                    observer.OnNext(c);
                }

                disposable.Add(c.Disconnected().Subscribe(_ =>
                {
                    if (maxReconnectAttempts > 0 && reconnectCount >= maxReconnectAttempts)
                    {
                        observer.OnError(new InvalidOperationException($"Max reconnect attempts ({maxReconnectAttempts}) exceeded."));
                        return;
                    }

                    reconnectCount++;
                    disposable.Add(Observable.Timer(delay)
                        .SelectMany(_ => Observable.FromAsync(ct => c.ReconnectAsync(ct)))
                        .Subscribe(
                            _ => Subscribe(c),
                            ex => observer.OnError(ex)));
                }));
            }

            disposable.Add(client.Subscribe(Subscribe, observer.OnError, observer.OnCompleted));
            return disposable;
        }).Retry();
    }
}
