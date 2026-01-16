// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides data for the event that occurs when an application message is skipped during processing.
/// </summary>
/// <param name="applicationMessage">The application message that was skipped. Cannot be null.</param>
public sealed class ApplicationMessageSkippedEventArgs(ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>
    /// Gets the MQTT application message associated with this instance.
    /// </summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
}
