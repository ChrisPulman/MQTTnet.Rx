// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Defines the MQTT client transport selected in the toolkit.</summary>
internal enum MqttTransportMode
{
    /// <summary>Connects through a TCP endpoint.</summary>
    Tcp,

    /// <summary>Connects through MQTT over WebSockets.</summary>
    WebSocket,

    /// <summary>Connects using an MQTTnet connection URI.</summary>
    Uri,
}
