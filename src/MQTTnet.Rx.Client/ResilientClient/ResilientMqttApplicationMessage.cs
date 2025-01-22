// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// ResilientMqttApplicationMessage.
/// </summary>
public class ResilientMqttApplicationMessage
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the application message.
    /// </summary>
    /// <value>
    /// The application message.
    /// </value>
    public MqttApplicationMessage? ApplicationMessage { get; set; }
}
