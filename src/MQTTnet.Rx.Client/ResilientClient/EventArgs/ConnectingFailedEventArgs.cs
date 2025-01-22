// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Connecting Failed EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
public sealed class ConnectingFailedEventArgs(MqttClientConnectResult? connectResult, Exception exception) : EventArgs
{
    /// <summary>
    /// Gets this is null when the connection was failing and the server was not reachable.
    /// </summary>
    public MqttClientConnectResult? ConnectResult { get; } = connectResult;

    /// <summary>
    /// Gets the exception.
    /// </summary>
    /// <value>
    /// The exception.
    /// </value>
    public Exception Exception { get; } = exception;
}
