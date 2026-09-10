// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies MQTT client connection option construction.</summary>
public sealed class ConnectionOptionsViewModelTests
{
    /// <summary>Stores a duplicate MQTT user-property name used by connection tests.</summary>
    private const string DuplicatePropertyName = "duplicate";

    /// <summary>Stores the first duplicate MQTT user-property value.</summary>
    private const string FirstPropertyValue = "first";

    /// <summary>Stores the second duplicate MQTT user-property value.</summary>
    private const string SecondPropertyValue = "second";

    /// <summary>Stores the expected duplicate property count.</summary>
    private const int DuplicatePropertyCount = 2;

    /// <summary>Stores the integration test timeout in seconds.</summary>
    private const int IntegrationTimeoutSeconds = 10;

    /// <summary>Stores the expected will payload hexadecimal value.</summary>
    private const string WillPayloadHex = "6F66666C696E65";

    /// <summary>Stores the maximum MQTT interval value.</summary>
    private const uint MaximumMqttInterval = uint.MaxValue;

    /// <summary>Stores the expected WebSocket deflate client window bits.</summary>
    private const int ClientWindowBits = 12;

    /// <summary>Stores the expected WebSocket deflate server window bits.</summary>
    private const int ServerWindowBits = 13;

    /// <summary>Stores the WebSocket endpoint used by option mapping tests.</summary>
    private const string WebSocketEndpoint = "ws://localhost:8083/mqtt";

    /// <summary>Stores the WebSocket credential username used by option mapping tests.</summary>
    private const string WebSocketUsername = "ws-user";

    /// <summary>Stores the WebSocket credential password used by option mapping tests.</summary>
    private const string WebSocketPassword = "ws-password";

    /// <summary>Stores the WebSocket credential domain used by option mapping tests.</summary>
    private const string WebSocketDomain = "ws-domain";

    /// <summary>Stores the TCP linger time used by option mapping tests.</summary>
    private const int ConfiguredTcpLingerTimeSeconds = 2;

    /// <summary>Verifies unsigned MQTT 5 intervals and duplicate properties survive option construction.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildClientOptionsPreservesUnsignedIntervalsAndDuplicatePropertiesAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            ClientId = "toolkit-test-client",
            CleanStart = false,
            SessionExpirySeconds = MaximumMqttInterval,
            WillTopic = "toolkit/status",
            WillPayload = "offline",
            WillMessageExpirySeconds = MaximumMqttInterval,
            WillDelaySeconds = MaximumMqttInterval,
            WillQualityOfService = MqttQualityOfServiceLevel.ExactlyOnce,
            WillRetain = true,
        };
        connection.UserProperties.Add(new() { Name = DuplicatePropertyName, Value = FirstPropertyValue });
        connection.UserProperties.Add(new() { Name = DuplicatePropertyName, Value = SecondPropertyValue });
        connection.WillUserProperties.Add(new() { Name = DuplicatePropertyName, Value = FirstPropertyValue });
        connection.WillUserProperties.Add(new() { Name = DuplicatePropertyName, Value = SecondPropertyValue });

        var options = connection.BuildClientOptions();

        await Assert.That(options.ClientId).IsEqualTo("toolkit-test-client");
        await Assert.That(options.CleanSession).IsFalse();
        await Assert.That(options.SessionExpiryInterval).IsEqualTo(MaximumMqttInterval);
        await Assert.That(options.WillDelayInterval).IsEqualTo(MaximumMqttInterval);
        await Assert.That(options.WillMessageExpiryInterval).IsEqualTo(MaximumMqttInterval);
        await Assert.That(options.WillTopic).IsEqualTo("toolkit/status");
        await Assert.That(Convert.ToHexString(options.WillPayload)).IsEqualTo(WillPayloadHex);
        await Assert.That(options.WillQualityOfServiceLevel).IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
        await Assert.That(options.WillRetain).IsTrue();
        await Assert.That(options.UserProperties).Count().IsEqualTo(DuplicatePropertyCount);
        await Assert.That(options.WillUserProperties).Count().IsEqualTo(DuplicatePropertyCount);
    }

    /// <summary>Verifies default TCP options can connect to the embedded MQTTnet.Rx broker.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildClientOptionsConnectsToEmbeddedBrokerWithDefaultTcpTransportAsync()
    {
        var port = GetAvailableLoopbackPort();
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(IntegrationTimeoutSeconds));
        await service.StartEmbeddedServerAsync(port, cancellation.Token);
        using var connection = new ConnectionOptionsViewModel
        {
            ClientId = "toolkit-default-tcp-test",
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            TransportMode = MqttTransportMode.Tcp,
            TcpLocalAddress = IPAddress.Loopback.ToString(),
            TcpLocalPort = 0,
            TcpUseLinger = true,
            TcpLingerEnabled = true,
            TcpLingerTimeSeconds = ConfiguredTcpLingerTimeSeconds,
        };
        var options = connection.BuildClientOptions();
        var tcpOptions = (MqttClientTcpOptions)options.ChannelOptions!;
        var localEndpoint = (IPEndPoint)tcpOptions.LocalEndpoint!;

        await Assert.That(localEndpoint.Port).IsEqualTo(0);
        await Assert.That(tcpOptions.LingerState).IsNotNull();
        await Assert.That(tcpOptions.LingerState!.Enabled).IsTrue();
        await Assert.That(tcpOptions.LingerState.LingerTime).IsEqualTo(ConfiguredTcpLingerTimeSeconds);

        await service.ConnectAsync(options, cancellation.Token);
        await service.DisconnectAsync(cancellation.Token);
        await service.StopEmbeddedServerAsync(cancellation.Token);

        await Assert.That(cancellation.IsCancellationRequested).IsFalse();
    }

    /// <summary>Verifies WebSocket cookies and deflate options map to MQTTnet WebSocket options.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildClientOptionsMapsWebSocketCookiesAndDeflateAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.WebSocket,
            WebSocketUri = WebSocketEndpoint,
            WebSocketCredentialUsername = WebSocketUsername,
            WebSocketCredentialPassword = WebSocketPassword,
            WebSocketCredentialDomain = WebSocketDomain,
            WebSocketCookies = "session=abc\r\nmode=test",
            UseWebSocketDeflate = true,
            WebSocketDeflateClientMaxWindowBits = ClientWindowBits,
            WebSocketDeflateServerMaxWindowBits = ServerWindowBits,
            WebSocketDeflateClientContextTakeover = false,
            WebSocketDeflateServerContextTakeover = true,
        };

        var options = connection.BuildClientOptions();
        var webSocketOptions = (MqttClientWebSocketOptions)options.ChannelOptions!;
        var endpoint = new Uri(WebSocketEndpoint);
        var cookies = webSocketOptions.CookieContainer.GetCookies(endpoint);
        var credentials = webSocketOptions.Credentials.GetCredential(endpoint, "Basic")!;

        await Assert.That(cookies).Count().IsEqualTo(DuplicatePropertyCount);
        await Assert.That(credentials.UserName).IsEqualTo(WebSocketUsername);
        await Assert.That(credentials.Password).IsEqualTo(WebSocketPassword);
        await Assert.That(credentials.Domain).IsEqualTo(WebSocketDomain);
        await Assert.That(webSocketOptions.DangerousDeflateOptions).IsNotNull();
        await Assert.That(webSocketOptions.DangerousDeflateOptions!.ClientMaxWindowBits).IsEqualTo(ClientWindowBits);
        await Assert.That(webSocketOptions.DangerousDeflateOptions.ServerMaxWindowBits).IsEqualTo(ServerWindowBits);
        await Assert.That(webSocketOptions.DangerousDeflateOptions.ClientContextTakeover).IsFalse();
        await Assert.That(webSocketOptions.DangerousDeflateOptions.ServerContextTakeover).IsTrue();
    }

    /// <summary>Verifies certificate callback/provider choices only appear when corresponding hooks are available.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task CertificateOptionListsExposeInjectedChoicesOnlyWhenHooksExistAsync()
    {
        using var connection = new ConnectionOptionsViewModel();

        await Assert.That(ContainsClientCertificateSource(connection.ClientCertificateSources, ClientCertificateSource.Provider)).IsFalse();
        await Assert.That(ContainsCertificateValidationMode(connection.CertificateValidationModes, CertificateValidationMode.Callback)).IsFalse();
        await Assert.That(ContainsCertificateSelectionMode(connection.CertificateSelectionModes, CertificateSelectionMode.Callback)).IsFalse();

        connection.ClientCertificateProvider = static () => [];
        connection.CertificateValidationHandler = static _ => true;
        connection.CertificateSelectionHandler = static args => ConnectionOptionHelpers.SelectFirstCertificate(args.LocalCertificates);

        await Assert.That(connection.ClientCertificateSources).Contains(ClientCertificateSource.Provider);
        await Assert.That(connection.CertificateValidationModes).Contains(CertificateValidationMode.Callback);
        await Assert.That(connection.CertificateSelectionModes).Contains(CertificateSelectionMode.Callback);
    }

    /// <summary>Verifies invalid advanced certificate and WebSocket compression configurations fail explicitly.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildClientOptionsRejectsIncompleteAdvancedSecurityOptionsAsync()
    {
        using var pinnedConnection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateValidationMode = CertificateValidationMode.PinnedThumbprint,
        };
        await Assert.That(pinnedConnection.BuildClientOptions).Throws<InvalidOperationException>();

        using var selectionConnection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateSelectionMode = CertificateSelectionMode.Thumbprint,
        };
        await Assert.That(selectionConnection.BuildClientOptions).Throws<InvalidOperationException>();

        using var webSocketConnection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.WebSocket,
            UseWebSocketDeflate = true,
            WebSocketDeflateClientMaxWindowBits = 1,
        };
        await Assert.That(webSocketConnection.BuildClientOptions).Throws<InvalidOperationException>();
    }

    /// <summary>Determines whether certificate source choices contain a value.</summary>
    /// <param name="values">The values to inspect.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when the expected value is present.</returns>
    private static bool ContainsClientCertificateSource(
        IReadOnlyList<ClientCertificateSource> values,
        ClientCertificateSource expected)
    {
        foreach (var value in values)
        {
            if (value == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether certificate validation mode choices contain a value.</summary>
    /// <param name="values">The values to inspect.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when the expected value is present.</returns>
    private static bool ContainsCertificateValidationMode(
        IReadOnlyList<CertificateValidationMode> values,
        CertificateValidationMode expected)
    {
        foreach (var value in values)
        {
            if (value == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether certificate selection mode choices contain a value.</summary>
    /// <param name="values">The values to inspect.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns><see langword="true"/> when the expected value is present.</returns>
    private static bool ContainsCertificateSelectionMode(
        IReadOnlyList<CertificateSelectionMode> values,
        CertificateSelectionMode expected)
    {
        foreach (var value in values)
        {
            if (value == expected)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Finds an available loopback TCP port.</summary>
    /// <returns>The available TCP port.</returns>
    private static int GetAvailableLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
