// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides data for the event that occurs after an application message has been processed, including the message and
/// any exception that was thrown during processing.
/// </summary>
/// <param name="applicationMessage">The application message that was processed. Cannot be null.</param>
/// <param name="exception">The exception that occurred during message processing, or null if the message was processed successfully.</param>
public sealed class ApplicationMessageProcessedEventArgs(ResilientMqttApplicationMessage applicationMessage, Exception? exception) : EventArgs
{
    /// <summary>
    /// Gets the MQTT application message associated with this instance.
    /// </summary>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>
    /// Gets the exception that caused the current operation to fail, if any.
    /// </summary>
    public Exception? Exception { get; } = exception;
}
