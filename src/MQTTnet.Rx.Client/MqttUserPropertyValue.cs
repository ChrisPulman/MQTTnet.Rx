// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents one MQTT user property while preserving duplicate property names.</summary>
/// <param name="Name">The MQTT user property name.</param>
/// <param name="Value">The decoded MQTT user property value.</param>
public sealed record MqttUserPropertyValue(string Name, string Value);
