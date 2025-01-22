// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Intercepting Publish Message EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
public sealed class InterceptingPublishMessageEventArgs(ResilientMqttApplicationMessage applicationMessage) : EventArgs
{
    /// <summary>
    /// Gets the application message.
    /// </summary>
    /// <value>
    /// The application message.
    /// </value>
    public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));

    /// <summary>
    /// Gets or sets a value indicating whether [accept publish].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [accept publish]; otherwise, <c>false</c>.
    /// </value>
    public bool AcceptPublish { get; set; } = true;
}
