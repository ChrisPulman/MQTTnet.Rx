// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides data for the event that occurs when an application message is skipped during processing.</summary>
/// <param name="applicationMessage">The application message that was skipped. Cannot be null.</param>
public sealed class ApplicationMessageSkippedEventArgs(
    ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>Gets the MQTT application message associated with this instance.</summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } =
        applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
}
