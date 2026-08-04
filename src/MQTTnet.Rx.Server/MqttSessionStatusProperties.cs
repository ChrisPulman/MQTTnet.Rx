// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Represents a point-in-time snapshot of all public MQTT session-status properties.</summary>
/// <param name="CreatedTimestamp">The session creation timestamp.</param>
/// <param name="DisconnectedTimestamp">The optional disconnection timestamp.</param>
/// <param name="ExpiryInterval">The session expiry interval.</param>
/// <param name="Id">The session identifier.</param>
/// <param name="Items">A copy of the session items.</param>
/// <param name="PendingApplicationMessagesCount">The pending application-message count.</param>
public sealed record MqttSessionStatusProperties(
    DateTimeOffset CreatedTimestamp,
    DateTimeOffset? DisconnectedTimestamp,
    uint ExpiryInterval,
    string Id,
    IReadOnlyDictionary<object, object?> Items,
    long PendingApplicationMessagesCount);
