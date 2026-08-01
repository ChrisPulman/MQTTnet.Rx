// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents an MQTT application message queued for resilient delivery.</summary>
/// <remarks>This class is typically used to track and manage MQTT messages that require reliable delivery or
/// retry logic. The unique identifier can be used to correlate messages across retries or application
/// restarts.</remarks>
public class ResilientMqttApplicationMessage
{
    /// <summary>Gets or sets the unique identifier for the entity.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the MQTT application message associated with this instance.</summary>
    public MqttApplicationMessage? ApplicationMessage { get; set; }
}
