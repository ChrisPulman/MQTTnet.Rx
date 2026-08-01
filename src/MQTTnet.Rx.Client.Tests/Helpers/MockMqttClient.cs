// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Packets;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Mock implementation of IMqttClient for testing purposes.</summary>
public sealed class MockMqttClient : IMqttClient
{
    /// <summary>Stores published messages.</summary>
    private readonly List<MqttApplicationMessage> _publishedMessages = [];

    /// <summary>Stores subscription requests.</summary>
    private readonly List<MqttClientSubscribeOptions> _subscriptions = [];

    /// <summary>Stores unsubscription requests.</summary>
    private readonly List<string> _unsubscriptions = [];

    /// <summary>Stores connecting event handlers.</summary>
    private Func<MqttClientConnectingEventArgs, Task>? _connectingAsync;

    /// <summary>Stores packet-inspection event handlers.</summary>
    private Func<InspectMqttPacketEventArgs, Task>? _inspectPacketAsync;

    /// <summary>Indicates whether the client is connected.</summary>
    private bool _isConnected;

    /// <summary>Indicates whether the client has been disposed.</summary>
    private bool _isDisposed;

    /// <inheritdoc/>
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync
    {
        add => _connectingAsync += value;
        remove => _connectingAsync -= value;
    }

    /// <inheritdoc/>
    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

    /// <inheritdoc/>
    public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync
    {
        add => _inspectPacketAsync += value;
        remove => _inspectPacketAsync -= value;
    }

    /// <summary>Gets the published messages.</summary>
    public IReadOnlyList<MqttApplicationMessage> PublishedMessages => _publishedMessages;

    /// <summary>Gets the subscriptions.</summary>
    public IReadOnlyList<MqttClientSubscribeOptions> Subscriptions => _subscriptions;

    /// <summary>Gets the unsubscriptions.</summary>
    public IReadOnlyList<string> Unsubscriptions => _unsubscriptions;

    /// <summary>Gets the number of times ConnectAsync was invoked.</summary>
    public int ConnectCount { get; private set; }

    /// <summary>Gets the number of times DisconnectAsync was invoked.</summary>
    public int DisconnectCount { get; private set; }

    /// <summary>Gets the number of registered connection handlers.</summary>
    public int ConnectedHandlerCount => ConnectedAsync?.GetInvocationList().Length ?? 0;

    /// <summary>Gets the number of registered disconnection handlers.</summary>
    public int DisconnectedHandlerCount => DisconnectedAsync?.GetInvocationList().Length ?? 0;

    /// <summary>Gets the number of times PingAsync was invoked.</summary>
    public int PingCount { get; private set; }

    /// <summary>Gets the number of times ReconnectAsync was invoked.</summary>
    public int ReconnectCount { get; private set; }

    /// <summary>Gets or sets the number of reconnect attempts that should fail before succeeding.</summary>
    public int ReconnectFailuresRemaining { get; set; }

    /// <inheritdoc/>
    public bool IsConnected => _isConnected;

    /// <inheritdoc/>
    public MqttClientOptions? Options { get; private set; }

    /// <summary>Simulates receiving an application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateMessageReceivedAsync(string topic, string payload)
    {
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(payload);

        if (ApplicationMessageReceivedAsync is null)
        {
            return;
        }

        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        var payloadSequence = new ReadOnlySequence<byte>(payloadBytes);
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        var publishPacket = new MqttPublishPacket
        {
            Topic = topic,
            Payload = payloadSequence,
        };

        var args = new MqttApplicationMessageReceivedEventArgs("test-client", message, publishPacket, null);
        await ApplicationMessageReceivedAsync(args).ConfigureAwait(false);
    }

    /// <summary>Simulates a connection event.</summary>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateConnectedAsync()
    {
        _isConnected = true;
        if (ConnectedAsync is null)
        {
            return;
        }

        var result = new MqttClientConnectResult();
        var args = new MqttClientConnectedEventArgs(result);
        await ConnectedAsync(args).ConfigureAwait(false);
    }

    /// <summary>Simulates a disconnection event.</summary>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateDisconnectedAsync()
    {
        _isConnected = false;
        if (DisconnectedAsync is null)
        {
            return;
        }

        var args = new MqttClientDisconnectedEventArgs(
            clientWasConnected: true,
            connectResult: null,
            reason: MqttClientDisconnectReason.NormalDisconnection,
            reasonString: "Test disconnection",
            userProperties: null,
            exception: null);
        await DisconnectedAsync(args).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<MqttClientConnectResult> ConnectAsync(
        MqttClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        Options = options;
        ConnectCount++;
        if (ReconnectFailuresRemaining > 0)
        {
            ReconnectFailuresRemaining--;
            return Task.FromException<MqttClientConnectResult>(
                new InvalidOperationException("Configured reconnect failure."));
        }

        _isConnected = true;
        return Task.FromResult(new MqttClientConnectResult());
    }

    /// <inheritdoc/>
    public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        DisconnectCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken = default)
    {
        PingCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<MqttClientPublishResult> PublishAsync(
        MqttApplicationMessage applicationMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        _publishedMessages.Add(applicationMessage);
        return Task.FromResult(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task SendEnhancedAuthenticationExchangeDataAsync(
        MqttEnhancedAuthenticationExchangeData data,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MqttClientSubscribeResult> SubscribeAsync(
        MqttClientSubscribeOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        _subscriptions.Add(options);
        var items = options.TopicFilters.ConvertAll(static f =>
            new MqttClientSubscribeResultItem(f, MqttClientSubscribeResultCode.GrantedQoS0));
        return Task.FromResult(new MqttClientSubscribeResult(0, items, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(
        MqttClientUnsubscribeOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        _unsubscriptions.AddRange(options.TopicFilters);

        var items = options.TopicFilters.ConvertAll(static f =>
            new MqttClientUnsubscribeResultItem(f, MqttClientUnsubscribeResultCode.Success));
        return Task.FromResult(new MqttClientUnsubscribeResult(0, items, string.Empty, []));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isConnected = false;
    }

    /// <summary>Attempts to re-establish a connection to the MQTT broker asynchronously.</summary>
    /// <returns>A task that produces the reconnect result.</returns>
    public Task<MqttClientConnectResult> ReconnectAsync() => ReconnectAsync(default);

    /// <summary>Attempts to re-establish a connection to the MQTT broker asynchronously.</summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the reconnect operation.</param>
    /// <returns>
    /// A task that represents the asynchronous reconnect operation. The task result contains the outcome of the
    /// connection attempt.
    /// </returns>
    public Task<MqttClientConnectResult> ReconnectAsync(in CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<MqttClientConnectResult>(cancellationToken);
        }

        ReconnectCount++;
        if (ReconnectFailuresRemaining > 0)
        {
            ReconnectFailuresRemaining--;
            return Task.FromException<MqttClientConnectResult>(
                new InvalidOperationException("Configured reconnect failure."));
        }

        _isConnected = true;
        return Task.FromResult(new MqttClientConnectResult());
    }
}
