// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// ReconnectionResult.
/// </summary>
public enum ReconnectionResult
{
    /// <summary>
    /// The still connected.
    /// </summary>
    StillConnected,
    /// <summary>
    /// The reconnected.
    /// </summary>
    Reconnected,
    /// <summary>
    /// The recovered.
    /// </summary>
    Recovered,
    /// <summary>
    /// The not connected.
    /// </summary>
    NotConnected
}
