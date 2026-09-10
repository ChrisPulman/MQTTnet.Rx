// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Server;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using MQTTnet.Server;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ServerCreate = MQTTnet.Rx.Server.Create;

namespace MQTTnet.Rx.Toolkit;

/// <summary>Manages Toolkit MQTT client and embedded broker sessions.</summary>
internal sealed partial class MqttToolkitSessionService : IDisposable, IAsyncDisposable
{
    /// <summary>Log source used for broker events.</summary>
    private const string BrokerSource = "Broker";

    /// <summary>Log source used for client events.</summary>
    private const string ClientSource = "Client";

    /// <summary>Log source used for server events.</summary>
    private const string ServerSource = "Server";

    /// <summary>Stores observable subscriptions connected to the MQTT client.</summary>
    private readonly MultipleDisposable _clientSubscriptions = [];

    /// <summary>Cancels pending MQTT operations during disposal.</summary>
    private readonly CancellationTokenSource _disposeCancellation = new();

    /// <summary>Serializes client and server lifecycle changes.</summary>
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    /// <summary>Stores observable subscriptions connected to the embedded MQTT server.</summary>
    private readonly MultipleDisposable _serverSubscriptions = [];

    /// <summary>Provides timestamps for received data and log messages.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>Stores the current MQTT client.</summary>
    private IMqttClient? _client;

    /// <summary>Tracks whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>Stores the current embedded MQTT server.</summary>
    private MqttServer? _server;

    /// <summary>Stores the asynchronous server session returned by the Rx server factory.</summary>
    private IAsyncDisposable? _serverSession;

    /// <summary>Stores the embedded server observable subscription lease.</summary>
    private IDisposable? _serverLease;

    /// <summary>Initializes a new instance of the <see cref="MqttToolkitSessionService"/> class.</summary>
    internal MqttToolkitSessionService()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MqttToolkitSessionService"/> class.</summary>
    /// <param name="timeProvider">The timestamp provider used for deterministic diagnostics.</param>
    internal MqttToolkitSessionService(TimeProvider timeProvider) => _timeProvider = timeProvider;

    /// <summary>Occurs when a client or embedded server observes an application message.</summary>
    internal event EventHandler<ReceivedMqttMessage>? MessageReceived;

    /// <summary>Occurs when a topic or payload diagnostic is detected.</summary>
    internal event EventHandler<TopicIssue>? TopicIssueDetected;

    /// <summary>Occurs when a Toolkit log entry is emitted.</summary>
    internal event EventHandler<MqttLogEntry>? LogReceived;

    /// <summary>Occurs when MQTT client connection state changes.</summary>
    internal event EventHandler<bool>? ConnectionChanged;

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>Asynchronously cancels pending operations and releases MQTT resources.</summary>
    /// <returns>A task representing asynchronous cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _disposeCancellation.CancelAsync().ConfigureAwait(false);
        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            StopTwinCatBridgeCore();
            _clientSubscriptions.Dispose();
            _serverSubscriptions.Dispose();
            _client?.Dispose();
            _serverLease?.Dispose();
            if (_serverSession is not null)
            {
                await _serverSession.DisposeAsync().ConfigureAwait(false);
            }

            _server?.Dispose();
        }
        finally
        {
            _ = _lifecycleGate.Release();
            _disposeCancellation.Dispose();
            _lifecycleGate.Dispose();
        }
    }

    /// <summary>Builds a Toolkit message model from MQTTnet application-message data.</summary>
    /// <param name="args">The MQTTnet received-message event arguments.</param>
    /// <returns>The Toolkit message model.</returns>
    internal ReceivedMqttMessage CreateReceivedMessage(MqttApplicationMessageReceivedEventArgs args) =>
        CreateReceivedMessage(args.ApplicationMessage, "Client received");

    /// <summary>Builds a Toolkit message model from MQTTnet application-message data.</summary>
    /// <param name="message">The MQTTnet application message.</param>
    /// <returns>The Toolkit message model.</returns>
    internal ReceivedMqttMessage CreateReceivedMessage(MqttApplicationMessage message) =>
        CreateReceivedMessage(message, "Client received");

    /// <summary>Builds a Toolkit message model from MQTTnet application-message data.</summary>
    /// <param name="message">The MQTTnet application message.</param>
    /// <param name="source">The Toolkit source that observed the message.</param>
    /// <returns>The Toolkit message model.</returns>
    internal ReceivedMqttMessage CreateReceivedMessage(MqttApplicationMessage message, string source) =>
        message.ToReceivedMqttMessage(source, _timeProvider);

    /// <summary>Starts the loopback MQTTnet.Rx broker used by the default Toolkit connection.</summary>
    /// <param name="port">The loopback TCP port used by the broker.</param>
    /// <param name="cancellationToken">Cancels the start operation.</param>
    /// <returns>A task that completes when the broker has started.</returns>
    internal async Task StartEmbeddedServerAsync(int port, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
            if (_server is not null)
            {
                return;
            }

            await StartEmbeddedServerCoreAsync(port, operationCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Connects the MQTT client using the supplied options.</summary>
    /// <param name="options">The MQTT client options to use.</param>
    /// <param name="cancellationToken">Cancels the connect operation.</param>
    /// <returns>A task that completes when the client has connected.</returns>
    internal async Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDisposed();
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
            await DisconnectCoreAsync(operationCancellation.Token).ConfigureAwait(false);
            await ConnectCoreAsync(options, operationCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Disconnects and disposes the MQTT client.</summary>
    /// <param name="cancellationToken">Cancels the disconnect operation.</param>
    /// <returns>A task that completes when the client has disconnected.</returns>
    internal async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
            await DisconnectCoreAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Subscribes the current client with the subscription view-model options.</summary>
    /// <param name="subscription">The subscription configuration.</param>
    /// <param name="cancellationToken">Cancels the subscribe operation.</param>
    /// <returns>A task that completes when the subscription has been acknowledged.</returns>
    internal async Task SubscribeAsync(SubscriptionViewModel subscription, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var client = RequireClient();
        var builder = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter =>
            {
                _ = filter
                    .WithTopic(subscription.TopicFilter)
                    .WithQualityOfServiceLevel(subscription.QualityOfService)
                    .WithNoLocal(subscription.NoLocal)
                    .WithRetainAsPublished(subscription.RetainAsPublished)
                    .WithRetainHandling(subscription.RetainHandling);
            });
        if (subscription.SubscriptionIdentifier != 0)
        {
            _ = builder.WithSubscriptionIdentifier(subscription.SubscriptionIdentifier);
        }

        foreach (var property in subscription.UserProperties)
        {
            if (property.IsValid)
            {
                _ = builder.WithUserProperty(
                    property.Name,
                    System.Text.Encoding.UTF8.GetBytes(property.Value).AsMemory());
            }
        }

        var options = builder.Build();
        var result = await client.SubscribeAsync(options, cancellationToken).ConfigureAwait(false);
        var failureText = GetSubscribeFailures(result);
        if (failureText.Length > 0)
        {
            throw new InvalidOperationException($"Subscribe failed for {subscription.TopicFilter}: {failureText}.");
        }

        Log("Info", "Subscribe", $"{subscription.TopicFilter} -> {GetSubscribeResults(result)}.");
    }

    /// <summary>Unsubscribes the current client from a topic filter.</summary>
    /// <param name="topicFilter">The MQTT topic filter to remove.</param>
    /// <param name="cancellationToken">Cancels the unsubscribe operation.</param>
    /// <returns>A task that completes when the unsubscribe has been acknowledged.</returns>
    internal async Task UnsubscribeAsync(string topicFilter, CancellationToken cancellationToken)
    {
        var client = RequireClient();
        var options = new MqttClientUnsubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter)
            .Build();
        var result = await client.UnsubscribeAsync(options, cancellationToken).ConfigureAwait(false);
        var failureText = GetUnsubscribeFailures(result);
        if (failureText.Length > 0)
        {
            throw new InvalidOperationException($"Unsubscribe failed for {topicFilter}: {failureText}.");
        }

        Log("Info", "Unsubscribe", $"{topicFilter} -> {GetUnsubscribeResults(result)}.");
    }

    /// <summary>Publishes an application message with the current client.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancels the publish operation.</param>
    /// <returns>A task that completes when the broker has acknowledged the publish.</returns>
    internal async Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var client = RequireClient();
        var result = await client.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        Log("Info", "Publish", $"{message.Topic} -> {result.ReasonCode}.");
    }

    /// <summary>Sends live MQTT enhanced-authentication exchange data with the current client.</summary>
    /// <param name="step">The enhanced-authentication exchange data to send.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>A task that completes when MQTTnet accepts the exchange data.</returns>
    internal async Task SendEnhancedAuthenticationExchangeDataAsync(
        EnhancedAuthenticationStepViewModel step,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        var client = RequireClient();
        if (step.ReasonCode == MqttAuthenticateReasonCode.Success)
        {
            throw new InvalidOperationException("Only an MQTT server can send authentication success.");
        }

        if (step.ReasonCode == MqttAuthenticateReasonCode.ReAuthenticate &&
            client.Options.EnhancedAuthenticationHandler is ScriptedEnhancedAuthenticationHandler handler)
        {
            handler.Reset();
        }

        var data = new MqttEnhancedAuthenticationExchangeData
        {
            AuthenticationData = MqttPayloadEncoding.BuildBytes(step.Data, step.DataFormat),
            ReasonCode = step.ReasonCode,
            ReasonString = step.Reason,
        };
        await client.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken).ConfigureAwait(false);
        Log("Info", "Authentication", $"Enhanced-authentication exchange data sent with reason {step.ReasonCode}.");
    }

    /// <summary>Stops the embedded MQTTnet.Rx broker when it is running.</summary>
    /// <param name="cancellationToken">Cancels the stop operation.</param>
    /// <returns>A task that completes when the broker has stopped.</returns>
    internal async Task StopEmbeddedServerAsync(CancellationToken cancellationToken)
    {
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
            await StopEmbeddedServerCoreAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Builds a comma-separated text view of subscribe failures.</summary>
    /// <param name="result">The MQTTnet subscribe result.</param>
    /// <returns>The failure text, or an empty string when all items succeeded.</returns>
    private static string GetSubscribeFailures(MqttClientSubscribeResult result)
    {
        var values = new List<string>();
        foreach (var item in result.Items)
        {
            if (item.ResultCode >= MqttClientSubscribeResultCode.UnspecifiedError)
            {
                values.Add(item.ResultCode.ToString());
            }
        }

        return string.Join(", ", values);
    }

    /// <summary>Builds a comma-separated text view of subscribe item results.</summary>
    /// <param name="result">The MQTTnet subscribe result.</param>
    /// <returns>The subscribe result text.</returns>
    private static string GetSubscribeResults(MqttClientSubscribeResult result)
    {
        var values = new string[result.Items.Count];
        var index = 0;
        foreach (var item in result.Items)
        {
            values[index] = item.ResultCode.ToString();
            index++;
        }

        return string.Join(", ", values);
    }

    /// <summary>Builds a comma-separated text view of unsubscribe failures.</summary>
    /// <param name="result">The MQTTnet unsubscribe result.</param>
    /// <returns>The failure text, or an empty string when all items succeeded.</returns>
    private static string GetUnsubscribeFailures(MqttClientUnsubscribeResult result)
    {
        var values = new List<string>();
        foreach (var item in result.Items)
        {
            if (item.ResultCode >= MqttClientUnsubscribeResultCode.UnspecifiedError)
            {
                values.Add(item.ResultCode.ToString());
            }
        }

        return string.Join(", ", values);
    }

    /// <summary>Builds a comma-separated text view of unsubscribe item results.</summary>
    /// <param name="result">The MQTTnet unsubscribe result.</param>
    /// <returns>The unsubscribe result text.</returns>
    private static string GetUnsubscribeResults(MqttClientUnsubscribeResult result)
    {
        var values = new string[result.Items.Count];
        var index = 0;
        foreach (var item in result.Items)
        {
            values[index] = item.ResultCode.ToString();
            index++;
        }

        return string.Join(", ", values);
    }

    /// <summary>Creates a token source linked to disposal cancellation.</summary>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The linked cancellation source.</returns>
    private CancellationTokenSource CreateOperationCancellation(CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellation.Token);

    /// <summary>Connects a new MQTT client without acquiring the lifecycle gate.</summary>
    /// <param name="options">The MQTT client options to use.</param>
    /// <param name="cancellationToken">Cancels the connect operation.</param>
    /// <returns>A task that completes when the client has connected.</returns>
    private async Task ConnectCoreAsync(MqttClientOptions options, CancellationToken cancellationToken)
    {
        var client = new MqttClientFactory().CreateMqttClient();
        try
        {
            _client = client;
            HookClient(client);
            var result = await client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
            if (result.ResultCode is not MqttClientConnectResultCode.Success)
            {
                throw new InvalidOperationException($"Connect failed: {result.ResultCode}.");
            }

            Log("Info", ClientSource, $"Connected as {options.ClientId}; result {result.ResultCode}.");
            ConnectionChanged?.Invoke(this, client.IsConnected);
        }
        catch
        {
            _clientSubscriptions.Clear();
            client.Dispose();
            _client = null;
            ConnectionChanged?.Invoke(this, false);
            throw;
        }
    }

    /// <summary>Disposes the embedded server session when present.</summary>
    /// <returns>A value task that completes when the session has been disposed.</returns>
    private async ValueTask DisposeServerSessionAsync()
    {
        if (_serverSession is null)
        {
            return;
        }

        await _serverSession.DisposeAsync().ConfigureAwait(false);
        _serverSession = null;
    }

    /// <summary>Disconnects the MQTT client without acquiring the lifecycle gate.</summary>
    /// <param name="cancellationToken">Cancels the disconnect operation.</param>
    /// <returns>A task that completes when the client has disconnected.</returns>
    private async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        StopTwinCatBridgeCore();
        _clientSubscriptions.Clear();
        if (_client is not null)
        {
            try
            {
                if (_client.IsConnected)
                {
                    await _client.DisconnectAsync(new(), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _client.Dispose();
                _client = null;
            }
        }

        ConnectionChanged?.Invoke(this, false);
    }

    /// <summary>Handles MQTT packet inspection diagnostics.</summary>
    /// <param name="args">The MQTTnet packet inspection event arguments.</param>
    private void InspectPacket(InspectMqttPacketEventArgs args) =>
        Log("Trace", "Packet", $"{args.Direction} packet ({args.Buffer.Length} bytes)");

    /// <summary>Returns the current client or throws when disconnected.</summary>
    /// <returns>The connected MQTT client.</returns>
    private IMqttClient RequireClient() =>
        _client ?? throw new InvalidOperationException("Connect before using MQTT operations.");

    /// <summary>Logs a Toolkit event.</summary>
    /// <param name="level">The log level text.</param>
    /// <param name="source">The log source text.</param>
    /// <param name="message">The log message text.</param>
    private void Log(string level, string source, string message) =>
        LogReceived?.Invoke(this, new(_timeProvider.GetLocalNow(), level, source, message));

    /// <summary>Starts the embedded server without acquiring the lifecycle gate.</summary>
    /// <param name="port">The loopback TCP port used by the broker.</param>
    /// <param name="cancellationToken">Cancels the start operation.</param>
    /// <returns>A task that completes when the broker has started.</returns>
    private async Task StartEmbeddedServerCoreAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            var ready = new TaskCompletionSource<(MqttServer Server, IAsyncDisposable Session)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _serverLease = ServerCreate
                .MqttServer(options => options
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
                    .WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Loopback)
                    .WithDefaultEndpointPort(port)
                    .Build())
                .SubscribePrimitives(
                    session => ready.TrySetResult((session.Server, session.Disposable)),
                    exception => ready.TrySetException(exception));

            var result = await ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            _server = result.Server;
            _serverSession = result.Session;
            HookServer(_server);
            Log("Info", BrokerSource, $"Embedded MQTTnet.Rx.Server started on port {port}.");
        }
        catch
        {
            await StopEmbeddedServerCoreAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Stops the embedded server without acquiring the lifecycle gate.</summary>
    /// <param name="cancellationToken">Cancels the stop operation.</param>
    /// <returns>A task that completes when the broker has stopped.</returns>
    private async Task StopEmbeddedServerCoreAsync(CancellationToken cancellationToken)
    {
        if (_server is null)
        {
            return;
        }

        try
        {
            await _server.StopAsync(new()).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _serverSubscriptions.Clear();
            await DisposeServerSessionAsync().ConfigureAwait(false);
            _serverLease?.Dispose();
            _serverLease = null;
            _server.Dispose();
            _server = null;
        }

        Log("Info", BrokerSource, "Embedded MQTTnet.Rx.Server stopped.");
    }

    /// <summary>Throws when this service has already been disposed.</summary>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>Hooks MQTT client observable events into Toolkit events.</summary>
    /// <param name="client">The MQTT client to observe.</param>
    private void HookClient(IMqttClient client)
    {
        _clientSubscriptions.Add(client.ApplicationMessageReceived()
            .Map(CreateReceivedMessage)
            .SubscribePrimitives(
                message =>
                {
                    MessageReceived?.Invoke(this, message);
                    foreach (var issue in TopicDiagnostics.Find(message))
                    {
                        TopicIssueDetected?.Invoke(this, issue);
                    }
                },
                exception => Log("Error", "Client", exception.Message)));

        _clientSubscriptions.Add(client.Connected()
            .SubscribePrimitives(_ => ConnectionChanged?.Invoke(this, true)));

        _clientSubscriptions.Add(client.Disconnected()
            .SubscribePrimitives(e =>
            {
                ConnectionChanged?.Invoke(this, false);
                Log("Warning", ClientSource, $"Disconnected: {e.Reason}; {e.Exception?.Message}");
            }));

        _clientSubscriptions.Add(client.InspectPacket()
            .SubscribePrimitives(InspectPacket));
    }

    /// <summary>Hooks embedded broker observable events into Toolkit events.</summary>
    /// <param name="server">The MQTT server to observe.</param>
    private void HookServer(MqttServer server)
    {
        _serverSubscriptions.Add(server.InterceptingPublish()
            .SubscribePrimitives(
                e =>
                {
                    var message = CreateReceivedMessage(e.ApplicationMessage, "Broker ingress");
                    MessageReceived?.Invoke(this, message);
                    foreach (var issue in TopicDiagnostics.Find(message))
                    {
                        TopicIssueDetected?.Invoke(this, issue);
                    }
                },
                exception => Log("Error", "Server", exception.Message)));

        _serverSubscriptions.Add(server.ClientConnected()
            .SubscribePrimitives(e => Log("Info", ServerSource, $"Client connected: {e.ClientId}.")));

        _serverSubscriptions.Add(server.ClientDisconnected()
            .SubscribePrimitives(e => Log("Info", ServerSource, $"Client disconnected: {e.ClientId}.")));

        _serverSubscriptions.Add(server.InterceptingSubscription()
            .SubscribePrimitives(e => Log("Info", ServerSource, $"Subscription requested by {e.ClientId}: {e.TopicFilter.Topic}.")));
    }
}
