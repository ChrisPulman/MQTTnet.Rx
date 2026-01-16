// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides data for the event that is raised when an attempt to connect to the MQTT server fails.
/// </summary>
/// <param name="connectResult">The result of the connection attempt, or null if the server was not reachable.</param>
/// <param name="exception">The exception that caused the connection attempt to fail. Cannot be null.</param>
public sealed class ConnectingFailedEventArgs(MqttClientConnectResult? connectResult, Exception exception) : EventArgs
{
    /// <summary>
    /// Gets the result of the most recent MQTT client connection attempt, if available.
    /// </summary>
    public MqttClientConnectResult? ConnectResult { get; } = connectResult;

    /// <summary>
    /// Gets the exception that caused the current operation to fail.
    /// </summary>
    public Exception Exception { get; } = exception;
}
