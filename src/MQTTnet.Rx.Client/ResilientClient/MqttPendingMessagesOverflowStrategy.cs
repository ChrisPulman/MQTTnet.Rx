// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Specifies the strategy to use when the pending messages queue exceeds its capacity in an MQTT client.
/// </summary>
/// <remarks>Use this enumeration to control how the client handles situations where the number of pending
/// messages exceeds the configured limit. The selected strategy determines whether the oldest queued message is dropped
/// to make room for a new message, or whether the new message is discarded instead. This can affect message delivery
/// guarantees and should be chosen based on application requirements.</remarks>
public enum MqttPendingMessagesOverflowStrategy
{
    /// <summary>
    /// Gets or sets a value indicating whether the oldest queued message should be dropped when the message queue
    /// reaches its capacity.
    /// </summary>
    /// <remarks>Set this property to <see langword="true"/> to enable automatic removal of the oldest message
    /// when the queue is full, allowing new messages to be enqueued. If set to <see langword="false"/>, new messages
    /// may be rejected or an exception may be thrown when the queue is at capacity, depending on the
    /// implementation.</remarks>
    DropOldestQueuedMessage,

    /// <summary>
    /// Gets or sets a value indicating whether new incoming messages should be dropped.
    /// </summary>
    DropNewMessage
}
