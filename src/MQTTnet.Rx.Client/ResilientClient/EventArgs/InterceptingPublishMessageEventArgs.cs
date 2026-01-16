// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides data for an event that occurs when an MQTT publish message is intercepted before it is processed or
/// forwarded.
/// </summary>
/// <param name="applicationMessage">The application message associated with the intercepted publish event. Cannot be null.</param>
public sealed class InterceptingPublishMessageEventArgs(ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>
    /// Gets the MQTT application message associated with this instance.
    /// </summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>
    /// Gets or sets a value indicating whether publish requests are accepted.
    /// </summary>
    public bool AcceptPublish { get; set; } = true;
}
