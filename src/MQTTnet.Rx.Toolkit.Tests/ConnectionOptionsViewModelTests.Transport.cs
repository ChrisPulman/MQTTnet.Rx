// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies TCP and WebSocket option translation and validation.</summary>
public sealed partial class ConnectionOptionsViewModelTests
{
    /// <summary>Stores the TCP buffer size configured by socket mapping tests.</summary>
    private const int ConfiguredTcpBufferSize = 16_384;

    /// <summary>Stores the local TCP bind port configured by socket mapping tests.</summary>
    private const int ConfiguredLocalTcpPort = 1234;

    /// <summary>Stores an unsupported transport value.</summary>
    private const MqttTransportMode UnknownTransport = (MqttTransportMode)99;

    /// <summary>Stores the keep-alive override supplied through composition.</summary>
    private const int ComposedWebSocketKeepAliveSeconds = 2;

    /// <summary>Stores an unsupported server deflate window size.</summary>
    private const int InvalidServerWindowBits = 16;

    /// <summary>Stores the failure reason for a missing TCP channel.</summary>
    private const string MissingTcpChannelMessage = "TCP channel missing.";

    /// <summary>Stores the expected parsed WebSocket subprotocols.</summary>
    private static readonly string[] ExpectedWebSocketSubProtocols = ["mqtt", "mqttv5", "custom"];

    /// <summary>Stores the expected parsed proxy bypass patterns.</summary>
    private static readonly string[] ExpectedWebSocketProxyBypassList = ["localhost", ".*\\.internal", "127.0.0.1"];

    /// <summary>Verifies literal addresses and DNS names retain the configured TCP channel settings.</summary>
    /// <param name="host">The remote endpoint.</param>
    /// <param name="family">The explicitly configured address family.</param>
    /// <param name="expectedFamily">The resulting channel address family.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments("broker.example", AddressFamily.Unspecified, AddressFamily.InterNetwork)]
    [Arguments("broker.example", AddressFamily.InterNetworkV6, AddressFamily.InterNetworkV6)]
    [Arguments("127.0.0.1", AddressFamily.Unspecified, AddressFamily.InterNetwork)]
    [Arguments("::1", AddressFamily.Unspecified, AddressFamily.InterNetworkV6)]
    public async Task BuildClientOptions_MapsTcpAddressAndSocketSettingsAsync(string host, AddressFamily family, AddressFamily expectedFamily)
    {
        using var connection = new ConnectionOptionsViewModel
        {
            Host = host,
            TcpAddressFamily = family,
            TcpNoDelay = false,
            TcpDualMode = true,
            TcpBufferSize = ConfiguredTcpBufferSize,
            TcpLocalAddress = "::1",
            TcpLocalPort = ConfiguredLocalTcpPort,
        };
        var options = connection.BuildClientOptions();
        var tcp = await Assert.That(options.ChannelOptions).IsTypeOf<MqttClientTcpOptions>() ?? throw new InvalidOperationException(MissingTcpChannelMessage);
        await Assert.That(tcp.AddressFamily).IsEqualTo(expectedFamily);
        await Assert.That(tcp.NoDelay).IsFalse();
        await Assert.That(tcp.BufferSize).IsEqualTo(ConfiguredTcpBufferSize);
        await Assert.That(tcp.ProtocolType).IsEqualTo(ProtocolType.Tcp);
        await Assert.That(tcp.LingerState.Enabled).IsTrue();
        await Assert.That(tcp.LingerState.LingerTime).IsEqualTo(0);
        await Assert.That(tcp.LocalEndpoint).IsEqualTo(new IPEndPoint(IPAddress.IPv6Loopback, ConfiguredLocalTcpPort));
        if (expectedFamily == AddressFamily.InterNetworkV6)
        {
            await Assert.That(tcp.DualMode).IsTrue();
        }

        await Assert.That(tcp.RemoteEndpoint.ToString()).Contains(host);
    }

    /// <summary>Verifies URI transport derives the broker endpoint from the entered connection URI.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_UriUsesItsEndpointAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.Uri,
            ConnectionUri = "mqtt://uri-broker.example:2883",
            Host = "ignored.example",
        };
        var tcp = await Assert.That(connection.BuildClientOptions().ChannelOptions).IsTypeOf<MqttClientTcpOptions>() ?? throw new InvalidOperationException(MissingTcpChannelMessage);
        await Assert.That(tcp.RemoteEndpoint.ToString()).Contains("uri-broker.example");
        await Assert.That(tcp.RemoteEndpoint.ToString()).Contains("2883");
    }

    /// <summary>Verifies unsupported transport values use the configured TCP endpoint.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_UnknownTransportFallsBackToTcpAsync()
    {
        using var connection = new ConnectionOptionsViewModel { TransportMode = UnknownTransport, Host = "fallback.example" };
        var tcp = await Assert.That(connection.BuildClientOptions().ChannelOptions).IsTypeOf<MqttClientTcpOptions>() ?? throw new InvalidOperationException(MissingTcpChannelMessage);
        await Assert.That(tcp.RemoteEndpoint.ToString()).Contains("fallback.example");
    }

    /// <summary>Verifies malformed local bind addresses fail before opening a socket.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_RejectsInvalidLocalAddressAsync()
    {
        using var connection = new ConnectionOptionsViewModel { TcpLocalAddress = "not-an-address" };
        await Assert.That(connection.BuildClientOptions).Throws<FormatException>();
    }

    /// <summary>Verifies proxy credentials, metadata and cookies survive text parsing.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsWebSocketProxyAndMetadataAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.WebSocket,
            WebSocketKeepAliveSeconds = -1,
            WebSocketUseDefaultCredentials = true,
            WebSocketSubProtocols = " mqtt; mqttv5, custom\r\n",
            WebSocketHeaders = "Authorization: first\r\nauthorization: second:part\ninvalid\n: ignored",
            WebSocketProxyAddress = "http://proxy.example:8080",
            WebSocketProxyUsername = "proxy-user",
            WebSocketProxyPassword = "proxy-password",
            WebSocketProxyDomain = "proxy-domain",
            WebSocketProxyBypassOnLocal = false,
            WebSocketProxyUseDefaultCredentials = true,
            WebSocketProxyBypassList = " localhost; .*\\.internal, 127.0.0.1\n",
            WebSocketCookies = "invalid\n=ignored\nsession=abc=def",
            WebSocketOptionsConfigurator = static builder => builder.WithKeepAliveInterval(TimeSpan.FromSeconds(ComposedWebSocketKeepAliveSeconds)),
        };
        var webSocket = await Assert.That(connection.BuildClientOptions().ChannelOptions).IsTypeOf<MqttClientWebSocketOptions>() ?? throw new InvalidOperationException("WebSocket channel missing.");
        await Assert.That(webSocket.KeepAliveInterval).IsEqualTo(TimeSpan.FromSeconds(ComposedWebSocketKeepAliveSeconds));
        await Assert.That(webSocket.UseDefaultCredentials).IsTrue();
        await Assert.That(webSocket.SubProtocols).IsEquivalentTo(ExpectedWebSocketSubProtocols);
        await Assert.That(webSocket.RequestHeaders.Count).IsEqualTo(1);
        await Assert.That(webSocket.RequestHeaders["Authorization"]).IsEqualTo("second:part");
        await Assert.That(webSocket.CookieContainer.GetCookies(new(connection.WebSocketUri))["session"]!.Value).IsEqualTo("abc=def");
        await Assert.That(webSocket.ProxyOptions.Address).IsEqualTo(connection.WebSocketProxyAddress);
        await Assert.That(webSocket.ProxyOptions.Username).IsEqualTo("proxy-user");
        await Assert.That(webSocket.ProxyOptions.Password).IsEqualTo("proxy-password");
        await Assert.That(webSocket.ProxyOptions.Domain).IsEqualTo("proxy-domain");
        await Assert.That(webSocket.ProxyOptions.BypassOnLocal).IsFalse();
        await Assert.That(webSocket.ProxyOptions.UseDefaultCredentials).IsTrue();
        await Assert.That(webSocket.ProxyOptions.BypassList).IsEquivalentTo(ExpectedWebSocketProxyBypassList);
    }

    /// <summary>Verifies an empty bypass list and subprotocol list do not create empty entries.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_EmptyWebSocketMetadataUsesDefaultsAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.WebSocket,
            WebSocketProxyAddress = "http://proxy.example:8080",
            WebSocketSubProtocols = " ;, \r\n",
            WebSocketKeepAliveSeconds = -1,
        };
        var webSocket = await Assert.That(connection.BuildClientOptions().ChannelOptions).IsTypeOf<MqttClientWebSocketOptions>() ?? throw new InvalidOperationException("WebSocket channel missing.");
        await Assert.That(webSocket.KeepAliveInterval).IsEqualTo(TimeSpan.Zero);
        await Assert.That(webSocket.SubProtocols).Contains("mqtt");
        await Assert.That(webSocket.ProxyOptions.BypassList).IsNull();
        await Assert.That(webSocket.DangerousDeflateOptions).IsNull();
    }

    /// <summary>Verifies server deflate validation identifies the invalid server setting.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_RejectsInvalidServerDeflateWindowAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            TransportMode = MqttTransportMode.WebSocket,
            UseWebSocketDeflate = true,
            WebSocketDeflateServerMaxWindowBits = InvalidServerWindowBits,
        };
        var exception = await Assert.That(connection.BuildClientOptions).Throws<InvalidOperationException>() ?? throw new InvalidOperationException("Expected validation failure.");
        await Assert.That(exception.Message).Contains(nameof(connection.WebSocketDeflateServerMaxWindowBits));
    }
}
