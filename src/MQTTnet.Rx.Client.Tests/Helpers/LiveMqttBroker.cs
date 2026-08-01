// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Hosts a real loopback MQTT broker and two real clients for integration tests.</summary>
public sealed class LiveMqttBroker : IAsyncDisposable
{
    /// <summary>The maximum number of bind attempts used to tolerate a port-reservation race.</summary>
    private const int MaximumBindAttempts = 8;

    /// <summary>The default upper bound for client and subscription operations.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>The factory that owns the server configuration surface.</summary>
    private readonly MqttServerFactory _serverFactory;

    /// <summary>The hosted in-process MQTT server.</summary>
    private readonly MqttServer _server;

    /// <summary>Tracks whether teardown has already run.</summary>
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="LiveMqttBroker"/> class.</summary>
    /// <param name="serverFactory">The server factory used to create the broker.</param>
    /// <param name="server">The started in-process broker.</param>
    /// <param name="port">The allocated loopback TCP port.</param>
    private LiveMqttBroker(MqttServerFactory serverFactory, MqttServer server, int port)
    {
        _serverFactory = serverFactory;
        _server = server;
        Port = port;

        var clientFactory = new MqttClientFactory();
        BridgeClient = clientFactory.CreateMqttClient();
        ProbeClient = clientFactory.CreateMqttClient();
        BridgeClientId = $"live-bridge-{Guid.NewGuid():N}";
        ProbeClientId = $"live-probe-{Guid.NewGuid():N}";
    }

    /// <summary>Gets the real client used to publish messages into the broker.</summary>
    public IMqttClient BridgeClient { get; }

    /// <summary>Gets a Primitives-backed observable containing the real bridge client.</summary>
    public IObservable<IMqttClient> Bridge => Signal.Emit(BridgeClient);

    /// <summary>Gets the MQTT client identifier assigned to the bridge client.</summary>
    public string BridgeClientId { get; }

    /// <summary>Gets a value indicating whether fixture teardown has completed.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Gets a value indicating whether the in-process broker is running.</summary>
    public bool IsStarted => _server.IsStarted;

    /// <summary>Gets the allocated loopback TCP port.</summary>
    public int Port { get; }

    /// <summary>Gets the real client used to observe published messages.</summary>
    public IMqttClient ProbeClient { get; }

    /// <summary>Gets a Primitives-backed observable containing the real probe client.</summary>
    public IObservable<IMqttClient> Probe => Signal.Emit(ProbeClient);

    /// <summary>Gets the MQTT client identifier assigned to the probe client.</summary>
    public string ProbeClientId { get; }

    /// <summary>Gets the first exception observed during deterministic teardown, if any.</summary>
    public Exception? TeardownException { get; private set; }

    /// <summary>Starts a real MQTTnet server on an ephemeral loopback port.</summary>
    /// <returns>The started live MQTT broker fixture.</returns>
    public static Task<LiveMqttBroker> StartAsync() => StartAsync(CancellationToken.None);

    /// <summary>Starts a real MQTTnet server on an ephemeral loopback port.</summary>
    /// <param name="cancellationToken">The token used to cancel the bounded startup operation.</param>
    /// <returns>The started live MQTT broker fixture.</returns>
    public static async Task<LiveMqttBroker> StartAsync(CancellationToken cancellationToken)
    {
        Exception? finalCollision = null;
        for (var attempt = 0; attempt < MaximumBindAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var port = ReserveEphemeralPort();
            var serverFactory = new MqttServerFactory();
            var serverOptions = serverFactory
                .CreateServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
                .WithDefaultEndpointPort(port)
                .Build();
            var server = serverFactory.CreateMqttServer(serverOptions);

            try
            {
                await server.StartAsync().WaitAsync(OperationTimeout, cancellationToken).ConfigureAwait(false);
                return new(serverFactory, server, port);
            }
            catch (Exception exception) when (IsAddressAlreadyInUse(exception))
            {
                finalCollision = exception;
                server.Dispose();
            }
            catch
            {
                server.Dispose();
                throw;
            }
        }

        throw new InvalidOperationException(
            $"Unable to start an MQTT broker after {MaximumBindAttempts} ephemeral-port attempts.",
            finalCollision);
    }

    /// <summary>Connects both real clients to the hosted broker.</summary>
    /// <returns>The CONNACK results for the bridge and probe clients.</returns>
    public Task<(MqttClientConnectResult Bridge, MqttClientConnectResult Probe)> ConnectClientsAsync() =>
        ConnectClientsAsync(CancellationToken.None);

    /// <summary>Connects both real clients to the hosted broker.</summary>
    /// <param name="cancellationToken">The token used to cancel the bounded connection operation.</param>
    /// <returns>The CONNACK results for the bridge and probe clients.</returns>
    public async Task<(MqttClientConnectResult Bridge, MqttClientConnectResult Probe)> ConnectClientsAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        using var timeoutSource = CreateTimeoutSource(cancellationToken);
        var bridgeOptions = new MqttClientOptionsBuilder()
            .WithClientId(BridgeClientId)
            .WithTcpServer(IPAddress.Loopback.ToString(), Port)
            .Build();
        var probeOptions = new MqttClientOptionsBuilder()
            .WithClientId(ProbeClientId)
            .WithTcpServer(IPAddress.Loopback.ToString(), Port)
            .Build();

        var bridgeResult = await BridgeClient.ConnectAsync(bridgeOptions, timeoutSource.Token).ConfigureAwait(false);
        var probeResult = await ProbeClient.ConnectAsync(probeOptions, timeoutSource.Token).ConfigureAwait(false);
        return (bridgeResult, probeResult);
    }

    /// <summary>Subscribes the probe client and waits for the broker's subscription event.</summary>
    /// <param name="topic">The exact topic to subscribe to and capture.</param>
    /// <returns>A subscription containing readiness and received-message completion tasks.</returns>
    public Task<LiveMqttSubscription> SubscribeProbeAsync(string topic) =>
        SubscribeProbeAsync(topic, CancellationToken.None);

    /// <summary>Subscribes the probe client and waits for the broker's subscription event.</summary>
    /// <param name="topic">The exact topic to subscribe to and capture.</param>
    /// <param name="cancellationToken">The token used to cancel the bounded subscription operation.</param>
    /// <returns>A subscription containing readiness and received-message completion tasks.</returns>
    public async Task<LiveMqttSubscription> SubscribeProbeAsync(
        string topic,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        if (!ProbeClient.IsConnected)
        {
            throw new InvalidOperationException("The probe client must be connected before subscribing.");
        }

        var subscription = new LiveMqttSubscription(_server, ProbeClient, ProbeClientId, topic);
        using var timeoutSource = CreateTimeoutSource(cancellationToken);
        try
        {
            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            var result = await ProbeClient.SubscribeAsync(options, timeoutSource.Token).ConfigureAwait(false);
            if (result.Items.Count != 1)
            {
                throw new InvalidOperationException("The live broker rejected the probe subscription.");
            }

            foreach (var item in result.Items)
            {
                if (!IsGranted(item.ResultCode))
                {
                    throw new InvalidOperationException("The live broker rejected the probe subscription.");
                }
            }

            await subscription.MarkReadyAsync(result, OperationTimeout, timeoutSource.Token).ConfigureAwait(false);
            return subscription;
        }
        catch
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Exception? failure = null;
        failure = await DisconnectAndDisposeAsync(ProbeClient, failure).ConfigureAwait(false);
        failure = await DisconnectAndDisposeAsync(BridgeClient, failure).ConfigureAwait(false);

        try
        {
            if (_server.IsStarted)
            {
                var stopOptions = _serverFactory.CreateMqttServerStopOptionsBuilder().Build();
                await _server.StopAsync(stopOptions).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }
        finally
        {
            _server.Dispose();
        }

        TeardownException = failure;
    }

    /// <summary>Creates a linked cancellation source with the fixture operation timeout.</summary>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The linked timeout source.</returns>
    private static CancellationTokenSource CreateTimeoutSource(CancellationToken cancellationToken)
    {
        var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(OperationTimeout);
        return timeoutSource;
    }

    /// <summary>Disconnects and disposes a client while preserving the first teardown failure.</summary>
    /// <param name="client">The client to tear down.</param>
    /// <param name="failure">The first prior teardown failure, if any.</param>
    /// <returns>The first teardown failure, if any.</returns>
    private static async Task<Exception?> DisconnectAndDisposeAsync(IMqttClient client, Exception? failure)
    {
        try
        {
            if (client.IsConnected)
            {
                var options = new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                    .Build();
                await client.DisconnectAsync(options, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }
        finally
        {
            client.Dispose();
        }

        return failure;
    }

    /// <summary>Determines whether an exception chain represents a bind collision.</summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when the address was already in use; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsAddressAlreadyInUse(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a subscription result is a granted QoS value.</summary>
    /// <param name="resultCode">The SUBACK result code.</param>
    /// <returns><see langword="true"/> when the subscription was granted.</returns>
    private static bool IsGranted(MqttClientSubscribeResultCode resultCode) =>
        resultCode is MqttClientSubscribeResultCode.GrantedQoS0
            or MqttClientSubscribeResultCode.GrantedQoS1
            or MqttClientSubscribeResultCode.GrantedQoS2;

    /// <summary>Briefly reserves an operating-system assigned loopback port.</summary>
    /// <returns>The ephemeral TCP port allocated by the operating system.</returns>
    private static int ReserveEphemeralPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Server.ExclusiveAddressUse = true;
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
