// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Packets;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>
/// Mock implementation of IMqttClient for testing purposes.
/// </summary>
public sealed class MockMqttClient : IMqttClient
{
    private readonly List<MqttApplicationMessage> _publishedMessages = [];
    private readonly List<MqttClientSubscribeOptions> _subscriptions = [];
    private readonly List<string> _unsubscriptions = [];
    private bool _isConnected;
    private bool _isDisposed;

    /// <summary>
    /// Gets the published messages.
    /// </summary>
    public IReadOnlyList<MqttApplicationMessage> PublishedMessages => _publishedMessages;

    /// <summary>
    /// Gets the subscriptions.
    /// </summary>
    public IReadOnlyList<MqttClientSubscribeOptions> Subscriptions => _subscriptions;

    /// <summary>
    /// Gets the unsubscriptions.
    /// </summary>
    public IReadOnlyList<string> Unsubscriptions => _unsubscriptions;

    /// <inheritdoc/>
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;

    /// <inheritdoc/>
    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

    /// <inheritdoc/>
    public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync;

    /// <inheritdoc/>
    public bool IsConnected => _isConnected;

    /// <inheritdoc/>
    public MqttClientOptions? Options { get; private set; }

    /// <summary>
    /// Simulates receiving an application message.
    /// </summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateMessageReceived(string topic, string payload)
    {
        if (ApplicationMessageReceivedAsync is null)
        {
            return;
        }

        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        var payloadSequence = new ReadOnlySequence<byte>(payloadBytes);
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payloadSequence
        };

        var publishPacket = new MqttPublishPacket
        {
            Topic = topic,
            Payload = payloadSequence
        };

        var args = new MqttApplicationMessageReceivedEventArgs("test-client", message, publishPacket, null);
        await ApplicationMessageReceivedAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates a connection event.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateConnected()
    {
        _isConnected = true;
        if (ConnectedAsync is not null)
        {
            var result = new MqttClientConnectResult();
            var args = new MqttClientConnectedEventArgs(result);
            await ConnectedAsync(args).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Simulates a disconnection event.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public async Task SimulateDisconnected()
    {
        _isConnected = false;
        if (DisconnectedAsync is not null)
        {
            var args = new MqttClientDisconnectedEventArgs(
                clientWasConnected: true,
                connectResult: null,
                reason: MqttClientDisconnectReason.NormalDisconnection,
                reasonString: "Test disconnection",
                userProperties: null,
                exception: null);
            await DisconnectedAsync(args).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
    {
        Options = options;
        _isConnected = true;
        return Task.FromResult(new MqttClientConnectResult());
    }

    /// <inheritdoc/>
    public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
    {
        _publishedMessages.Add(applicationMessage);
        return Task.FromResult(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        _subscriptions.Add(options);
        var items = options.TopicFilters.ConvertAll(f =>
            new MqttClientSubscribeResultItem(f, MqttClientSubscribeResultCode.GrantedQoS0));
        return Task.FromResult(new MqttClientSubscribeResult(0, items, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        _unsubscriptions.AddRange(options.TopicFilters);

        var items = options.TopicFilters.ConvertAll(f =>
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

    /// <summary>
    /// Attempts to re-establish a connection to the MQTT broker asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the reconnect operation.</param>
    /// <returns>A task that represents the asynchronous reconnect operation. The task result contains the outcome of the
    /// connection attempt.</returns>
    public Task<MqttClientConnectResult> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.FromResult(new MqttClientConnectResult());
    }
}
