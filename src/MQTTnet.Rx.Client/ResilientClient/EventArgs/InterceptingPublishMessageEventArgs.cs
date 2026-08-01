// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides data for a publish request that can be accepted or rejected.</summary>
/// <param name="applicationMessage">The application message associated with the intercepted publish event. Cannot be
/// null.</param>
public sealed class InterceptingPublishMessageEventArgs(
    ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>Gets the MQTT application message associated with this instance.</summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } =
        applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>Gets or sets a value indicating whether publish requests are accepted.</summary>
    public bool AcceptPublish { get; set; } = true;
}
