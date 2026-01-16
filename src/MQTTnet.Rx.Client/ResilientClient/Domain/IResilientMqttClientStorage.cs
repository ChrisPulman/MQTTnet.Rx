// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Defines methods for persisting and retrieving queued MQTT application messages to support reliable message delivery
/// in resilient MQTT clients.
/// </summary>
/// <remarks>Implementations of this interface enable MQTT clients to store outgoing messages that have not yet
/// been delivered, allowing for message recovery after client restarts or network interruptions. This is typically used
/// to ensure at-least-once or exactly-once delivery guarantees in scenarios where message loss is
/// unacceptable.</remarks>
public interface IResilientMqttClientStorage
{
    /// <summary>
    /// Asynchronously saves a collection of queued MQTT application messages for later processing or delivery.
    /// </summary>
    /// <param name="messages">The list of messages to be saved. Cannot be null or contain null elements.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages);

    /// <summary>
    /// Asynchronously retrieves all messages that are currently queued for delivery.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
    /// cref="ResilientMqttApplicationMessage"/> objects representing the queued messages. The list is empty if there
    /// are no queued messages.</returns>
    Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync();
}
