// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Application Message Processed EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationMessageProcessedEventArgs"/> class.
/// </remarks>
/// <param name="applicationMessage">The application message.</param>
/// <param name="exception">The exception.</param>
/// <exception cref="ArgumentNullException">applicationMessage.</exception>
public sealed class ApplicationMessageProcessedEventArgs(ResilientMqttApplicationMessage applicationMessage, Exception? exception) : EventArgs
{
    /// <summary>
    /// Gets the application message.
    /// </summary>
    /// <value>
    /// The application message.
    /// </value>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>
    /// Gets then this is _null_ the message was processed successfully without any error.
    /// </summary>
    public Exception? Exception { get; } = exception;
}
