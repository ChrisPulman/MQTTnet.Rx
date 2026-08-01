// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>Persists queued messages for a resilient MQTT client.</summary>
/// <remarks>Implementations of this interface enable MQTT clients to store outgoing messages that have not yet
/// been delivered, allowing for message recovery after client restarts or network interruptions. This is typically used
/// to ensure at-least-once or exactly-once delivery guarantees in scenarios where message loss is
/// unacceptable.</remarks>
public interface IResilientMqttClientStorage
{
    /// <summary>Asynchronously persists the queued messages.</summary>
    /// <param name="messages">The list of messages to be saved. Cannot be null or contain null elements.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages);

    /// <summary>Asynchronously retrieves all messages that are currently queued for delivery.</summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
    /// cref="ResilientMqttApplicationMessage"/> objects representing the queued messages. The list is empty if there
    /// are no queued messages.</returns>
    Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync();
}
