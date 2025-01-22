// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Resilient Mqtt Client Options.
/// </summary>
public sealed class ResilientMqttClientOptions
{
    /// <summary>
    /// Gets or sets the client options.
    /// </summary>
    /// <value>
    /// The client options.
    /// </value>
    public MqttClientOptions? ClientOptions { get; set; }

    /// <summary>
    /// Gets or sets the automatic reconnect delay.
    /// </summary>
    /// <value>
    /// The automatic reconnect delay.
    /// </value>
    public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the connection check interval.
    /// </summary>
    /// <value>
    /// The connection check interval.
    /// </value>
    public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the storage.
    /// </summary>
    /// <value>
    /// The storage.
    /// </value>
    public IResilientMqttClientStorage? Storage { get; set; }

    /// <summary>
    /// Gets or sets the maximum pending messages.
    /// </summary>
    /// <value>
    /// The maximum pending messages.
    /// </value>
    public int MaxPendingMessages { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the pending messages overflow strategy.
    /// </summary>
    /// <value>
    /// The pending messages overflow strategy.
    /// </value>
    public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropNewMessage;

    /// <summary>
    /// Gets or sets defines the maximum amount of topic filters which will be sent in a SUBSCRIBE/UNSUBSCRIBE packet.
    /// Amazon Web Services (AWS) limits this number to 8. The default is int.MaxValue.
    /// </summary>
    public int MaxTopicFiltersInSubscribeUnsubscribePackets { get; set; } = int.MaxValue;
}
