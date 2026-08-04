// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents a point-in-time snapshot of every resilient MQTT client property.</summary>
/// <param name="InternalClient">The underlying MQTTnet client.</param>
/// <param name="IsConnected">Whether the client is connected.</param>
/// <param name="IsStarted">Whether the resilient client is started.</param>
/// <param name="Options">The current resilient-client options.</param>
/// <param name="PendingApplicationMessagesCount">The pending application-message count.</param>
public sealed record ResilientMqttClientProperties(
    IMqttClient InternalClient,
    bool IsConnected,
    bool IsStarted,
    ResilientMqttClientOptions? Options,
    int PendingApplicationMessagesCount);
