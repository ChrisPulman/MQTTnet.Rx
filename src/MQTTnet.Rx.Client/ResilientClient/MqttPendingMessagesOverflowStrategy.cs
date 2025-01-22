// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// MqttPendingMessagesOverflowStrategy.
/// </summary>
public enum MqttPendingMessagesOverflowStrategy
{
    /// <summary>
    /// The drop oldest queued message.
    /// </summary>
    DropOldestQueuedMessage,

    /// <summary>
    /// The drop new message.
    /// </summary>
    DropNewMessage
}
