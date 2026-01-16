// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Specifies the outcome of a reconnection attempt to a remote service or resource.
/// </summary>
/// <remarks>Use this enumeration to determine the result of a reconnection operation, such as whether the
/// connection was maintained, successfully re-established, recovered after a failure, or could not be restored. The
/// specific meaning of each value may depend on the context in which the reconnection logic is used.</remarks>
public enum ReconnectionResult
{
    /// <summary>
    /// Gets a value indicating whether the connection to the remote server is still active.
    /// </summary>
    StillConnected,
    /// <summary>
    /// Gets a value indicating whether the connection has been successfully re-established after a disconnection.
    /// </summary>
    Reconnected,
    /// <summary>
    /// Indicates that the operation or entity has completed a recovery process.
    /// </summary>
    Recovered,
    /// <summary>
    /// Indicates that the connection has not been established.
    /// </summary>
    NotConnected
}
