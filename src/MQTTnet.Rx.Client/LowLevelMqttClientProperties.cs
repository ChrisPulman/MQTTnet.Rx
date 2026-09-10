// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents a point-in-time low-level MQTT client property snapshot.</summary>
/// <param name="IsConnected">Whether the low-level MQTT client is connected.</param>
public sealed record LowLevelMqttClientProperties(bool IsConnected);
