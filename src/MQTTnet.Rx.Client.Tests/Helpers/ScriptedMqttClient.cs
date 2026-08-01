// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Provides a deterministic MQTT client whose protocol operations can be scripted by coverage tests.</summary>
internal sealed class ScriptedMqttClient : IMqttClient
{
    /// <summary>Stores connecting handlers.</summary>
    private Func<MqttClientConnectingEventArgs, Task>? _connectingAsync;

    /// <summary>Stores packet-inspection handlers.</summary>
    private Func<InspectMqttPacketEventArgs, Task>? _inspectPacketAsync;

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

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public MqttClientOptions? Options { get; private set; }

    /// <summary>Gets or sets the function used by <see cref="ConnectAsync"/>.</summary>
    internal Func<MqttClientOptions, CancellationToken, Task<MqttClientConnectResult>>? ConnectHandler { get; set; }

    /// <summary>Gets or sets the function used by <see cref="DisconnectAsync"/>.</summary>
    internal Func<MqttClientDisconnectOptions, CancellationToken, Task>? DisconnectHandler { get; set; }

    /// <summary>Gets or sets the function used by <see cref="PublishAsync"/>.</summary>
    internal Func<MqttApplicationMessage, CancellationToken, Task<MqttClientPublishResult>>? PublishHandler
    {
        get;
        set;
    }

    /// <summary>Gets or sets the function used by <see cref="SubscribeAsync"/>.</summary>
    internal Func<MqttClientSubscribeOptions, CancellationToken, Task<MqttClientSubscribeResult>>? SubscribeHandler
    {
        get;
        set;
    }

    /// <summary>Gets or sets the function used by <see cref="UnsubscribeAsync"/>.</summary>
    internal Func<
        MqttClientUnsubscribeOptions,
        CancellationToken,
        Task<MqttClientUnsubscribeResult>>? UnsubscribeHandler
    {
        get;
        set;
    }

    /// <summary>Gets the connect invocation count.</summary>
    internal int ConnectCount { get; private set; }

    /// <summary>Gets the disconnect invocation count.</summary>
    internal int DisconnectCount { get; private set; }

    /// <summary>Gets the ping invocation count.</summary>
    internal int PingCount { get; private set; }

    /// <summary>Gets the messages passed to the publish operation.</summary>
    internal List<MqttApplicationMessage> PublishedMessages { get; } = [];

    /// <summary>Gets the options passed to the subscribe operation.</summary>
    internal List<MqttClientSubscribeOptions> SubscribeRequests { get; } = [];

    /// <summary>Gets the options passed to the unsubscribe operation.</summary>
    internal List<MqttClientUnsubscribeOptions> UnsubscribeRequests { get; } = [];

    /// <summary>Gets a value indicating whether this client has been disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public async Task<MqttClientConnectResult> ConnectAsync(
        MqttClientOptions options,
        CancellationToken cancellationToken = default)
    {
        Options = options;
        ConnectCount++;

        if (ConnectHandler is not null)
        {
            var result = await ConnectHandler(options, cancellationToken).ConfigureAwait(false);
            IsConnected = result.ResultCode == MqttClientConnectResultCode.Success;
            return result;
        }

        IsConnected = true;
        return new();
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(
        MqttClientDisconnectOptions options,
        CancellationToken cancellationToken = default)
    {
        DisconnectCount++;

        if (DisconnectHandler is not null)
        {
            await DisconnectHandler(options, cancellationToken).ConfigureAwait(false);
        }

        IsConnected = false;
    }

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PingCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<MqttClientPublishResult> PublishAsync(
        MqttApplicationMessage applicationMessage,
        CancellationToken cancellationToken = default)
    {
        PublishedMessages.Add(applicationMessage);
        return PublishHandler?.Invoke(applicationMessage, cancellationToken)
            ?? Task.FromResult(new MqttClientPublishResult(0, MqttClientPublishReasonCode.Success, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task SendEnhancedAuthenticationExchangeDataAsync(
        MqttEnhancedAuthenticationExchangeData data,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MqttClientSubscribeResult> SubscribeAsync(
        MqttClientSubscribeOptions options,
        CancellationToken cancellationToken = default)
    {
        SubscribeRequests.Add(options);
        if (SubscribeHandler is not null)
        {
            return SubscribeHandler(options, cancellationToken);
        }

        var items = options.TopicFilters.ConvertAll(static filter =>
            new MqttClientSubscribeResultItem(filter, MqttClientSubscribeResultCode.GrantedQoS0));
        return Task.FromResult(new MqttClientSubscribeResult(0, items, string.Empty, []));
    }

    /// <inheritdoc/>
    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(
        MqttClientUnsubscribeOptions options,
        CancellationToken cancellationToken = default)
    {
        UnsubscribeRequests.Add(options);
        if (UnsubscribeHandler is not null)
        {
            return UnsubscribeHandler(options, cancellationToken);
        }

        var items = options.TopicFilters.ConvertAll(static filter =>
            new MqttClientUnsubscribeResultItem(filter, MqttClientUnsubscribeResultCode.Success));
        return Task.FromResult(new MqttClientUnsubscribeResult(0, items, string.Empty, []));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IsDisposed = true;
        IsConnected = false;
    }

    /// <summary>Sets the connection state without invoking a protocol operation.</summary>
    /// <param name="isConnected">The new connection state.</param>
    internal void SetConnected(bool isConnected) => IsConnected = isConnected;

    /// <summary>Raises the connected event when handlers are registered.</summary>
    /// <returns>A task representing the asynchronous event invocation.</returns>
    internal Task RaiseConnectedAsync() => ConnectedAsync?.Invoke(new(new())) ?? Task.CompletedTask;

    /// <summary>Raises the disconnected event when handlers are registered.</summary>
    /// <returns>A task representing the asynchronous event invocation.</returns>
    internal Task RaiseDisconnectedAsync() => DisconnectedAsync?.Invoke(new(
        true,
        null,
        MqttClientDisconnectReason.NormalDisconnection,
        "coverage disconnect",
        null,
        null)) ?? Task.CompletedTask;

    /// <summary>Raises the application-message event when handlers are registered.</summary>
    /// <param name="eventArgs">The event arguments to publish.</param>
    /// <returns>A task representing the asynchronous event invocation.</returns>
    internal Task RaiseApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs) =>
        ApplicationMessageReceivedAsync?.Invoke(eventArgs) ?? Task.CompletedTask;

    /// <summary>Touches otherwise unused interface events so their generated accessors are exercised.</summary>
    internal void TouchAuxiliaryEvents()
    {
        Func<MqttClientConnectingEventArgs, Task> connectingHandler = static _ => Task.CompletedTask;
        Func<InspectMqttPacketEventArgs, Task> inspectHandler = static _ => Task.CompletedTask;
        ConnectingAsync += connectingHandler;
        ConnectingAsync -= connectingHandler;
        InspectPacketAsync += inspectHandler;
        InspectPacketAsync -= inspectHandler;
    }
}
