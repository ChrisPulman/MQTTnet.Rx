// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>Provides data for processing an application message.</summary>
/// <param name="applicationMessage">The application message that was processed. Cannot be null.</param>
/// <param name="exception">The exception that occurred during message processing, or null if the message was processed
/// successfully.</param>
public sealed class ApplicationMessageProcessedEventArgs(
    ResilientMqttApplicationMessage applicationMessage,
    Exception? exception) : EventArgs
{
    /// <summary>Gets the MQTT application message associated with this instance.</summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } =
        applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>Gets the exception that caused the current operation to fail, if any.</summary>
    public Exception? Exception { get; } = exception;
}
