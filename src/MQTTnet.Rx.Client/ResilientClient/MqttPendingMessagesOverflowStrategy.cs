// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Specifies how a full pending-message queue handles new messages.</summary>
/// <remarks>Use this enumeration to control how the client handles situations where the number of pending
/// messages exceeds the configured limit. The selected strategy determines whether the oldest queued message is dropped
/// to make room for a new message, or whether the new message is discarded instead. This can affect message delivery
/// guarantees and should be chosen based on application requirements.</remarks>
public enum MqttPendingMessagesOverflowStrategy
{
    /// <summary>Drops the oldest queued message to make room for a new message.</summary>
    DropOldestQueuedMessage,

    /// <summary>Drops the new message when the queue is full.</summary>
    DropNewMessage,
}
