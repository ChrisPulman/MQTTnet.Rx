// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Application Message Skipped EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationMessageSkippedEventArgs"/> class.
/// </remarks>
/// <param name="applicationMessage">The application message.</param>
/// <exception cref="ArgumentNullException">applicationMessage.</exception>
public sealed class ApplicationMessageSkippedEventArgs(ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>
    /// Gets the application message.
    /// </summary>
    /// <value>
    /// The application message.
    /// </value>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
}
