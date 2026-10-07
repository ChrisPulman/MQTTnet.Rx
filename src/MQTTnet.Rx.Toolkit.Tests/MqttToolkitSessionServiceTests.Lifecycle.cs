// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies lifecycle cleanup, cancellation and retry after transport failures.</summary>
public sealed partial class MqttToolkitSessionServiceTests
{
    /// <summary>Checks disposal releases a running client and broker, and can safely be repeated.</summary>
    /// <param name="synchronous">Whether to release the session through IDisposable.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Dispose_ConnectedSession_ReleasesBrokerAndRejectsFurtherConnectionsAsync(bool synchronous)
    {
        var port = GetAvailablePort();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        if (synchronous)
        {
            service.Dispose();
        }
        else
        {
            await service.DisposeAsync();
        }

        await service.DisposeAsync();
        await Assert.That(() => service.ConnectAsync(CreateExternalOptions(port), timeout.Token)).Throws<ObjectDisposedException>();
        await Assert.That(() => service.StartEmbeddedServerAsync(port, timeout.Token)).Throws<ObjectDisposedException>();
        using var replacement = CreateExternalBroker(port);
        await replacement.StartAsync();
        using var client = new MqttClientFactory().CreateMqttClient();
        await Assert.That((await client.ConnectAsync(CreateExternalOptions(port), timeout.Token)).ResultCode).IsEqualTo(MqttClientConnectResultCode.Success);
        await client.DisconnectAsync(new(), timeout.Token);
        await replacement.StopAsync(new());
    }

    /// <summary>Checks a refused TCP connection clears client state and permits a later successful connect.</summary>
    /// <param name="webSocket">Whether to use the WebSocket transport for the refused connection.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConnectAsync_RefusedConnection_ClearsClientAndCanRecoverAsync(bool webSocket)
    {
        var port = GetAvailablePort();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        var states = new ConcurrentQueue<bool>();
        service.ConnectionChanged += (_, state) => states.Enqueue(state);
        var options = webSocket
            ? new MqttClientOptionsBuilder().WithWebSocketServer(builder => builder.WithUri($"ws://127.0.0.1:{port}/mqtt")).Build()
            : CreateExternalOptions(port);
        await Assert.That(await GetObservedExceptionAsync(service.ConnectAsync(options, timeout.Token))).IsNotNull();
        await Assert.That(states.ToArray()[^1]).IsFalse();
        await Assert.That(() => service.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(ReconnectTopic).Build(), timeout.Token))
            .Throws<InvalidOperationException>();
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        await Assert.That(states.ToArray()[^1]).IsTrue();
        await service.DisconnectAsync(timeout.Token);
        await Assert.That(states.ToArray()[^1]).IsFalse();
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks a rejected CONNACK is propagated and does not retain an unusable client.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ConnectAsync_BrokerRejectsConnection_ClearsClientAsync()
    {
        var port = GetAvailablePort();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        using var broker = CreateExternalBroker(port);
        broker.ValidatingConnectionAsync += static args =>
        {
            args.ReasonCode = MqttConnectReasonCode.NotAuthorized;
            return Task.CompletedTask;
        };
        await broker.StartAsync();
        await using var service = new MqttToolkitSessionService();
        await Assert.That(await GetObservedExceptionAsync(service.ConnectAsync(CreateExternalOptions(port), timeout.Token))).IsNotNull();
        await Assert.That(() => service.UnsubscribeAsync(ReconnectTopic, timeout.Token)).Throws<InvalidOperationException>();
        await service.DisconnectAsync(timeout.Token);
        await broker.StopAsync(new());
    }

    /// <summary>Checks failed broker startup can be retried once an occupied port becomes free.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StartEmbeddedServerAsync_OccupiedPort_CanRetryAfterFailureAsync()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        await Assert.That(await GetObservedExceptionAsync(service.StartEmbeddedServerAsync(port, timeout.Token))).IsNotNull();
        listener.Stop();
        await service.StopEmbeddedServerAsync(timeout.Token);
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks null arguments are rejected before MQTT connection state is consulted.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task MqttOperationsAsync_NullArguments_RejectWithoutConnectionAsync()
    {
        await using var service = new MqttToolkitSessionService();
        await Assert.That(() => service.ConnectAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => service.SubscribeAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => service.PublishAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => service.SendEnhancedAuthenticationExchangeDataAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
    }

    /// <summary>Checks caller cancellation stops lifecycle operations before they create any resources.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task LifecycleOperationsAsync_PreCanceledToken_DoesNotPoisonLaterStartupAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await using var service = new MqttToolkitSessionService();
        var port = GetAvailablePort();
        await Assert.That(() => service.StartEmbeddedServerAsync(port, cancellation.Token)).Throws<OperationCanceledException>();
        await Assert.That(() => service.ConnectAsync(CreateExternalOptions(port), cancellation.Token)).Throws<OperationCanceledException>();
        await Assert.That(() => service.DisconnectAsync(cancellation.Token)).Throws<OperationCanceledException>();
        await Assert.That(() => service.StopEmbeddedServerAsync(cancellation.Token)).Throws<OperationCanceledException>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks DNS and IP loopback endpoints can reconnect using the same embedded broker client identifier.</summary>
    /// <param name="dns">Whether to resolve the loopback endpoint through localhost.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task DisconnectAsync_EmbeddedLoopbackEndpoint_AllowsClientIdentifierReuseAsync(bool dns)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        var port = GetAvailablePort();
        var builder = new MqttClientOptionsBuilder().WithClientId("toolkit-loopback");
        _ = dns ? builder.WithTcpServer("localhost", port) : builder.WithEndPoint(new IPEndPoint(IPAddress.Loopback, port));
        await using var service = new MqttToolkitSessionService();
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(builder.Build(), timeout.Token);
        await service.DisconnectAsync(timeout.Token);
        await service.ConnectAsync(builder.Build(), timeout.Token);
        await service.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(ReconnectTopic).WithPayload("reused").Build(), timeout.Token);
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }
}
