// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides coverage for client configuration and event projection extensions.</summary>
public sealed class ClientConfigurationCoverageTests
{
    /// <summary>The client identifier used by configuration tests.</summary>
    private const string ClientId = "coverage-client";

    /// <summary>The Event Grid authentication name used by configuration tests.</summary>
    private const string EventGridAuthenticationName = "coverage-authentication";

    /// <summary>The Event Grid hostname used by configuration tests.</summary>
    private const string EventGridHostname = "coverage.eventgrid.example";

    /// <summary>The IoT Hub hostname used by configuration tests.</summary>
    private const string IotHubHostname = "coverage.azure-devices.example";

    /// <summary>The JSON content type used by last-will tests.</summary>
    private const string JsonContentType = "application/json";

    /// <summary>The password used by credential configuration tests.</summary>
    private const string Password = "coverage-password";

    /// <summary>The payload used by last-will tests.</summary>
    private const string Payload = "offline";

    /// <summary>The SAS token used by IoT Hub configuration tests.</summary>
    private const string SasToken = "SharedAccessSignature coverage";

    /// <summary>The topic used by last-will tests.</summary>
    private const string Topic = "coverage/client/status";

    /// <summary>The user name used by credential configuration tests.</summary>
    private const string UserName = "coverage-user";

    /// <summary>The WebSocket endpoint used by connection configuration tests.</summary>
    private const string WebSocketUri = "wss://coverage.example/mqtt";

    /// <summary>The configured connection timeout.</summary>
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(3);

    /// <summary>The lifetime applied to the temporary TLS certificate.</summary>
    private static readonly TimeSpan CertificateLifetime = TimeSpan.FromDays(1);

    /// <summary>The JSON serializer options used by last-will tests.</summary>
    private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new();

    /// <summary>The configured keep-alive period.</summary>
    private static readonly TimeSpan KeepAlivePeriod = TimeSpan.FromSeconds(2);

    /// <summary>The configured last-will delay.</summary>
    private static readonly TimeSpan LastWillDelay = TimeSpan.FromSeconds(4);

    /// <summary>The binary payload used by options and last-will tests.</summary>
    private static readonly byte[] BinaryPayload = [1];

    /// <summary>Exercises all MQTT client option-builder configuration methods.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ClientOptionsExtensions_ConfigureBuilderAndReturnSameInstanceAsync()
    {
        // Arrange
        using var certificate = CreateCertificate();
        var certificates = new X509Certificate2Collection(certificate);
        var builder = new MqttClientOptionsBuilder().WithTcpServer(IotHubHostname);

        // Act & Assert
        await Assert.That(builder.WithTlsEnabled()).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsClientCertificate(certificate)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsClientCertificates(certificates)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsCertificateValidation(static _ => true)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsProtocols(SslProtocols.None)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithTlsTrustAllCertificates()).IsSameReferenceAs(builder);
        await Assert.That(builder.WithWebSocketUri(WebSocketUri)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithUserCredentials(UserName, Password)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithUserCredentials(UserName, BinaryPayload)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithSessionOptions()).IsSameReferenceAs(builder);
        await Assert.That(builder.WithSessionOptions(false)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithSessionOptions(false, 1U)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithConnectionSettings()).IsSameReferenceAs(builder);
        await Assert.That(builder.WithConnectionSettings(KeepAlivePeriod)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithConnectionSettings(KeepAlivePeriod, ConnectionTimeout))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.ForAzureIotHub(IotHubHostname, ClientId, SasToken))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.ForAzureEventGrid(
                    EventGridHostname,
                    ClientId,
                    EventGridAuthenticationName,
                    certificate))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.Build()).IsNotNull();
    }

    /// <summary>Exercises all Last Will convenience and configuration methods.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task LastWillExtensions_ConfigureAllPayloadAndMetadataVariantsAsync()
    {
        // Arrange
        var stringProperties = new Dictionary<string, string> { [ClientId] = Payload };
        var segmentProperties = new Dictionary<string, ArraySegment<byte>>
        {
            [ClientId] = new(BinaryPayload),
        };
        var memoryProperties = new Dictionary<string, ReadOnlyMemory<byte>> { [ClientId] = BinaryPayload };
        var builder = new MqttClientOptionsBuilder().WithTcpServer(IotHubHostname);

        // Act & Assert
        await AssertBasicLastWillVariantsAsync(builder);
        await AssertDelayedAndMetadataLastWillVariantsAsync(builder);
        await AssertStringPropertyLastWillVariantsAsync(builder, stringProperties);
        await AssertBinaryPropertyLastWillVariantsAsync(builder, segmentProperties, memoryProperties);
        await Assert.That(builder.Build()).IsNotNull();
    }

    /// <summary>Ensures all MQTT client event projections can subscribe and release their handlers.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task MqttClientExtensions_SubscribeAndDisposeAllEventProjectionsAsync()
    {
        // Arrange
        using var client = new MockMqttClient();

        // Act
        using var connectingSubscription = client.Connecting().Subscribe(static _ => { });
        await using var connectingAsyncSubscription = await client.ObserveConnecting().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var disconnectedSubscription = client.Disconnected().Subscribe(static _ => { });
        await using var disconnectedAsyncSubscription = await client.ObserveDisconnected().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        using var inspectionSubscription = client.InspectPackage().Subscribe(static _ => { });
        await using var inspectionAsyncSubscription = await client.ObserveInspectPackage().SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        // Assert
        await Assert.That(connectingSubscription).IsNotNull();
        await Assert.That(connectingAsyncSubscription).IsNotNull();
        await Assert.That(disconnectedSubscription).IsNotNull();
        await Assert.That(disconnectedAsyncSubscription).IsNotNull();
        await Assert.That(inspectionSubscription).IsNotNull();
        await Assert.That(inspectionAsyncSubscription).IsNotNull();
    }

    /// <summary>Ensures readiness and client-option projections handle already-ready clients.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ConnectionAndCreateExtensions_EmitAlreadyReadyClientsWithoutStartingAgainAsync()
    {
        // Arrange
        using var mqttClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        await mqttClient.SimulateConnectedAsync();
        await resilientClient.SimulateConnectedAsync();

        // Act
        var configuredClient = await Signal.Emit<IMqttClient>(mqttClient)
            .WithClientOptions(static options => options.WithClientId(ClientId))
            .FirstAsync();
        var configuredResilientClient = await Signal.Emit<IResilientMqttClient>(resilientClient)
            .WithResilientClientOptions(
                static options => options.WithClientOptions(
                    static clientOptions => clientOptions.WithTcpServer(IotHubHostname)))
            .FirstAsync();
        var readyClient = await Signal.Emit<IResilientMqttClient>(resilientClient)
            .WhenReady()
            .FirstAsync();
        var configuredAsyncClient = await SignalAsync.Return<IMqttClient>(mqttClient)
            .WithClientOptions(static options => options.WithClientId(ClientId))
            .FirstAsync();
        var configuredAsyncResilientClient = await SignalAsync.Return<IResilientMqttClient>(resilientClient)
            .WithResilientClientOptions(
                static options => options.WithClientOptions(
                    static clientOptions => clientOptions.WithTcpServer(IotHubHostname)))
            .FirstAsync();
        var readyAsyncClient = await SignalAsync.Return<IResilientMqttClient>(resilientClient)
            .WhenReady()
            .FirstAsync();

        // Assert
        await Assert.That(configuredClient).IsSameReferenceAs(mqttClient);
        await Assert.That(configuredResilientClient).IsSameReferenceAs(resilientClient);
        await Assert.That(readyClient).IsSameReferenceAs(resilientClient);
        await Assert.That(configuredAsyncClient).IsSameReferenceAs(mqttClient);
        await Assert.That(configuredAsyncResilientClient).IsSameReferenceAs(resilientClient);
        await Assert.That(readyAsyncClient).IsSameReferenceAs(resilientClient);
    }

    /// <summary>Exercises synchronous and asynchronous shared-client factory disposal branches.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CreateFactories_ReleaseSharedClientsAfterTheLastSubscriptionAsync()
    {
        // Arrange
        IMqttClient? firstClient = null;
        IMqttClient? secondClient = null;
        var clients = Create.MqttClient();

        // Act
        using var firstSubscription = clients.Subscribe(client => firstClient = client);
        var secondSubscription = clients.Subscribe(client => secondClient = client);
        secondSubscription.Dispose();

        IMqttClient? firstAsyncClient = null;
        IMqttClient? secondAsyncClient = null;
        var asynchronousClients = Create.MqttClientSignal();
        var firstAsyncSubscription = await asynchronousClients.SubscribeAsync(
            CaptureFirstAsyncClient,
            CancellationToken.None);
        var secondAsyncSubscription = await asynchronousClients.SubscribeAsync(
            CaptureSecondAsyncClient,
            CancellationToken.None);
        await secondAsyncSubscription.DisposeAsync();
        await firstAsyncSubscription.DisposeAsync();

        // Assert
        await Assert.That(firstClient).IsSameReferenceAs(secondClient);
        await Assert.That(firstAsyncClient).IsSameReferenceAs(secondAsyncClient);

        ValueTask CaptureFirstAsyncClient(IMqttClient client, CancellationToken _)
        {
            firstAsyncClient = client;
            return ValueTask.CompletedTask;
        }

        ValueTask CaptureSecondAsyncClient(IMqttClient client, CancellationToken _)
        {
            secondAsyncClient = client;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Verifies Last Will payload and presence convenience variants.</summary>
    /// <param name="builder">The options builder under test.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    private static async Task AssertBasicLastWillVariantsAsync(MqttClientOptionsBuilder builder)
    {
        await Assert.That(builder.WithLastWill(Topic, Payload)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWill(Topic, Payload, MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWill(Topic, BinaryPayload)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWill(Topic, BinaryPayload, MqttQualityOfServiceLevel.ExactlyOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWill(Topic, Payload, MqttQualityOfServiceLevel.AtLeastOnce, false))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWill(Topic, BinaryPayload, MqttQualityOfServiceLevel.AtLeastOnce, false))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillJson(Topic, Payload)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillJson(Topic, Payload, MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillJson(Topic, Payload, MqttQualityOfServiceLevel.ExactlyOnce, false))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillJson(
                    Topic,
                    Payload,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    true,
                    SerializerOptions))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithPresenceLastWill(Topic)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithPresenceLastWill(Topic, Payload)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithPresenceLastWill(Topic, Payload, MqttQualityOfServiceLevel.ExactlyOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithPresenceLastWillJson(Topic, ClientId)).IsSameReferenceAs(builder);
        await Assert.That(builder.WithPresenceLastWillJson(Topic, ClientId, MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithPresenceLastWillJson(
                    Topic,
                    ClientId,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    TimeProvider.System))
            .IsSameReferenceAs(builder);
    }

    /// <summary>Verifies delayed Last Will and metadata configuration variants.</summary>
    /// <param name="builder">The options builder under test.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    private static async Task AssertDelayedAndMetadataLastWillVariantsAsync(MqttClientOptionsBuilder builder)
    {
        await Assert.That(builder.WithDelayedLastWill(Topic, Payload, LastWillDelay))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithDelayedLastWill(
                    Topic,
                    Payload,
                    LastWillDelay,
                    MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithDelayedLastWill(
                    Topic,
                    Payload,
                    LastWillDelay,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    false))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillMetadata(Topic, Payload, JsonContentType))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillMetadata(Topic, Payload, JsonContentType, BinaryPayload))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillMetadata(
                    Topic,
                    Payload,
                    JsonContentType,
                    BinaryPayload,
                    MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillMetadata(
                    Topic,
                    Payload,
                    JsonContentType,
                    null,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    false))
            .IsSameReferenceAs(builder);
    }

    /// <summary>Verifies string user-property Last Will configuration variants.</summary>
    /// <param name="builder">The options builder under test.</param>
    /// <param name="properties">The string user properties to configure.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    private static async Task AssertStringPropertyLastWillVariantsAsync(
        MqttClientOptionsBuilder builder,
        IDictionary<string, string> properties)
    {
        await Assert.That(builder.WithLastWillUserProperties(Topic, Payload, properties))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    properties,
                    MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    properties,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    false))
            .IsSameReferenceAs(builder);
    }

    /// <summary>Verifies binary user-property Last Will configuration variants.</summary>
    /// <param name="builder">The options builder under test.</param>
    /// <param name="segmentProperties">The array-segment user properties to configure.</param>
    /// <param name="memoryProperties">The read-only-memory user properties to configure.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    private static async Task AssertBinaryPropertyLastWillVariantsAsync(
        MqttClientOptionsBuilder builder,
        IDictionary<string, ArraySegment<byte>> segmentProperties,
        IDictionary<string, ReadOnlyMemory<byte>> memoryProperties)
    {
        await Assert.That(builder.WithLastWillUserProperties(Topic, Payload, segmentProperties))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    segmentProperties,
                    MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    segmentProperties,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    false))
            .IsSameReferenceAs(builder);
        await Assert.That(builder.WithLastWillUserProperties(Topic, Payload, memoryProperties))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    memoryProperties,
                    MqttQualityOfServiceLevel.AtMostOnce))
            .IsSameReferenceAs(builder);
        await Assert.That(
                builder.WithLastWillUserProperties(
                    Topic,
                    Payload,
                    memoryProperties,
                    MqttQualityOfServiceLevel.ExactlyOnce,
                    false))
            .IsSameReferenceAs(builder);
    }

    /// <summary>Creates a valid short-lived certificate for TLS-builder tests.</summary>
    /// <returns>The generated certificate.</returns>
    private static X509Certificate2 CreateCertificate()
    {
        using var algorithm = RSA.Create();
        var request = new CertificateRequest(
            $"CN={ClientId}",
            algorithm,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var issuedAt = TimeProvider.System.GetUtcNow();
        return request.CreateSelfSigned(issuedAt, issuedAt + CertificateLifetime);
    }
}
