// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Represents an MQTT application message with an associated unique identifier for use in resilient messaging
/// scenarios.
/// </summary>
/// <remarks>This class is typically used to track and manage MQTT messages that require reliable delivery or
/// retry logic. The unique identifier can be used to correlate messages across retries or application
/// restarts.</remarks>
public class ResilientMqttApplicationMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the MQTT application message associated with this instance.
    /// </summary>
    public MqttApplicationMessage? ApplicationMessage { get; set; }
}
