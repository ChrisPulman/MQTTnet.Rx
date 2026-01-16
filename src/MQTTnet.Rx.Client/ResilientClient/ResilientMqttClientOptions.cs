// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides configuration options for a resilient MQTT client, including connection settings, reconnection behavior,
/// message handling, and storage.
/// </summary>
/// <remarks>Use this class to customize how the resilient MQTT client manages connections, handles message
/// queuing, and interacts with persistent storage. Some properties, such as
/// MaxTopicFiltersInSubscribeUnsubscribePackets, may need to be adjusted to comply with broker-specific limitations
/// (for example, AWS limits this value to 8).</remarks>
public sealed class ResilientMqttClientOptions
{
    /// <summary>
    /// Gets or sets the options used to configure the MQTT client connection.
    /// </summary>
    public MqttClientOptions? ClientOptions { get; set; }

    /// <summary>
    /// Gets or sets the delay interval to wait before attempting to automatically reconnect after a disconnection.
    /// </summary>
    /// <remarks>Set this property to control how long the system waits before initiating an automatic
    /// reconnection attempt. Adjusting this value can help balance responsiveness and resource usage in environments
    /// with frequent connection interruptions.</remarks>
    public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the interval at which the connection status is checked.
    /// </summary>
    public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the storage provider used for persisting MQTT client state and messages across sessions.
    /// </summary>
    /// <remarks>Set this property to enable reliable message delivery and session recovery in scenarios where
    /// the client may disconnect or restart. If not set, message persistence and recovery features may be
    /// unavailable.</remarks>
    public IResilientMqttClientStorage? Storage { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages that can be queued for processing before new messages are rejected
    /// or delayed.
    /// </summary>
    public int MaxPendingMessages { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the strategy to apply when the pending messages queue reaches its capacity.
    /// </summary>
    /// <remarks>Use this property to control how the client handles situations where the number of pending
    /// messages exceeds the allowed limit. The selected strategy determines whether new messages are dropped, old
    /// messages are removed, or another overflow behavior is applied. Choose a strategy that best fits your
    /// application's reliability and throughput requirements.</remarks>
    public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropNewMessage;

    /// <summary>
    /// Gets or sets the maximum number of topic filters allowed in a single SUBSCRIBE or UNSUBSCRIBE packet.
    /// </summary>
    /// <remarks>This property can be used to enforce protocol limits or application-specific constraints on
    /// the number of topic filters that may be included in a single SUBSCRIBE or UNSUBSCRIBE operation. Setting this
    /// value to a lower number may help prevent excessively large packets or resource exhaustion.</remarks>
    public int MaxTopicFiltersInSubscribeUnsubscribePackets { get; set; } = int.MaxValue;
}
