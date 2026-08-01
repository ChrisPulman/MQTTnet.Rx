// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Server;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Owns one live probe subscription and captures its first matching message.</summary>
public sealed class LiveMqttSubscription : IAsyncDisposable
{
    /// <summary>The subscribed client.</summary>
    private readonly IMqttClient _client;

    /// <summary>The broker event handler used to observe subscription readiness.</summary>
    private readonly Func<ClientSubscribedTopicEventArgs, Task> _clientSubscribedHandler;

    /// <summary>The client event handler used to capture the first matching message.</summary>
    private readonly Func<MqttApplicationMessageReceivedEventArgs, Task> _messageReceivedHandler;

    /// <summary>The hosted server that emits subscription events.</summary>
    private readonly MqttServer _server;

    /// <summary>Completes when the broker has installed the subscription.</summary>
    private readonly TaskCompletionSource _subscriptionReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes with the first matching topic and payload.</summary>
    private readonly TaskCompletionSource<LiveMqttMessage> _messageReceived =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The exact topic observed by this subscription.</summary>
    private readonly string _topic;

    /// <summary>Tracks whether event handlers have been detached.</summary>
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="LiveMqttSubscription"/> class.</summary>
    /// <param name="server">The hosted server.</param>
    /// <param name="client">The probe client.</param>
    /// <param name="clientId">The probe client identifier.</param>
    /// <param name="topic">The exact topic to observe.</param>
    internal LiveMqttSubscription(MqttServer server, IMqttClient client, string clientId, string topic)
    {
        _server = server;
        _client = client;
        _topic = topic;
        _clientSubscribedHandler = eventArgs =>
        {
            if (string.Equals(eventArgs.ClientId, clientId, StringComparison.Ordinal)
                && string.Equals(eventArgs.TopicFilter.Topic, topic, StringComparison.Ordinal))
            {
                _ = _subscriptionReady.TrySetResult();
            }

            return Task.CompletedTask;
        };
        _messageReceivedHandler = eventArgs =>
        {
            if (string.Equals(eventArgs.ApplicationMessage.Topic, topic, StringComparison.Ordinal))
            {
                var payload = eventArgs.ApplicationMessage.Payload.ToArray();
                _ = _messageReceived.TrySetResult(new(eventArgs.ApplicationMessage.Topic, payload));
            }

            return Task.CompletedTask;
        };

        _server.ClientSubscribedTopicAsync += _clientSubscribedHandler;
        _client.ApplicationMessageReceivedAsync += _messageReceivedHandler;
    }

    /// <summary>Gets the task that completes with the first matching topic and payload.</summary>
    public Task<LiveMqttMessage> MessageReceived => _messageReceived.Task;

    /// <summary>Gets the SUBACK returned for this subscription.</summary>
    public MqttClientSubscribeResult SubscribeResult { get; private set; } = new(0, [], string.Empty, []);

    /// <summary>Gets the single result code returned by the broker for this exact-topic subscription.</summary>
    public MqttClientSubscribeResultCode SubscribeResultCode { get; private set; }

    /// <summary>Gets the task that completes when the server reports subscription readiness.</summary>
    public Task SubscriptionReady => _subscriptionReady.Task;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _server.ClientSubscribedTopicAsync -= _clientSubscribedHandler;
        _client.ApplicationMessageReceivedAsync -= _messageReceivedHandler;
        _ = _subscriptionReady.TrySetCanceled();
        _ = _messageReceived.TrySetCanceled();

        if (_client.IsConnected)
        {
            var options = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(_topic)
                .Build();
            _ = await _client.UnsubscribeAsync(options, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Stores the SUBACK and awaits the server-side subscription event.</summary>
    /// <param name="result">The SUBACK returned by the probe client.</param>
    /// <param name="timeout">The maximum time allowed for the server event.</param>
    /// <param name="cancellationToken">The token used to cancel the readiness wait.</param>
    /// <returns>A task that completes when the broker confirms readiness.</returns>
    internal async Task MarkReadyAsync(
        MqttClientSubscribeResult result,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        SubscribeResult = result;
        foreach (var item in result.Items)
        {
            SubscribeResultCode = item.ResultCode;
        }

        await _subscriptionReady.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
}
