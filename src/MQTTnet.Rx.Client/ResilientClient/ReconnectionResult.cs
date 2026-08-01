// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Specifies the outcome of a reconnection attempt to a remote service or resource.</summary>
/// <remarks>Use this enumeration to determine the result of a reconnection operation, such as whether the
/// connection was maintained, successfully re-established, recovered after a failure, or could not be restored. The
/// specific meaning of each value may depend on the context in which the reconnection logic is used.</remarks>
public enum ReconnectionResult
{
    /// <summary>Gets a value indicating whether the connection to the remote server is still active.</summary>
    StillConnected,
    /// <summary>Indicates that the connection was re-established.</summary>
    Reconnected,

    /// <summary>Indicates that the operation or entity has completed a recovery process.</summary>
    Recovered,

    /// <summary>Indicates that the connection has not been established.</summary>
    NotConnected,
}
