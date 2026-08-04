// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents a point-in-time snapshot of every public MQTT client property.</summary>
/// <param name="IsConnected">Whether the client is connected.</param>
/// <param name="Options">The current client options, when available.</param>
public sealed record MqttClientProperties(bool IsConnected, MqttClientOptions? Options);
