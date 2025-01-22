// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// IResilient Mqtt Client Storage.
/// </summary>
public interface IResilientMqttClientStorage
{
    /// <summary>
    /// Saves the queued messages asynchronous.
    /// </summary>
    /// <param name="messages">The messages.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages);

    /// <summary>
    /// Loads the queued messages asynchronous.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync();
}
