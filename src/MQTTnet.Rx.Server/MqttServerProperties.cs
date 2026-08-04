// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Represents a point-in-time snapshot of all public MQTT server properties.</summary>
/// <param name="AcceptNewConnections">Whether the server accepts new connections.</param>
/// <param name="IsStarted">Whether the server is started.</param>
/// <param name="ServerSessionItems">A copy of the server session-item collection.</param>
public sealed record MqttServerProperties(
    bool AcceptNewConnections,
    bool IsStarted,
    IReadOnlyDictionary<object, object?> ServerSessionItems);
