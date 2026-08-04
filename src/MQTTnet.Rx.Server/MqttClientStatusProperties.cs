// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using MQTTnet.Formatter;
using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Represents a point-in-time snapshot of all public MQTT client-status properties.</summary>
/// <param name="BytesReceived">The received byte count.</param>
/// <param name="BytesSent">The sent byte count.</param>
/// <param name="ConnectedTimestamp">The connection timestamp.</param>
/// <param name="RemoteEndPoint">The remote endpoint.</param>
/// <param name="Endpoint">The remote endpoint text.</param>
/// <param name="Id">The client identifier.</param>
/// <param name="LastNonKeepAlivePacketReceivedTimestamp">The last non-keep-alive receive timestamp.</param>
/// <param name="LastPacketReceivedTimestamp">The last packet receive timestamp.</param>
/// <param name="LastPacketSentTimestamp">The last packet send timestamp.</param>
/// <param name="ProtocolVersion">The negotiated protocol version.</param>
/// <param name="ReceivedApplicationMessagesCount">The received application-message count.</param>
/// <param name="ReceivedPacketsCount">The received packet count.</param>
/// <param name="SentApplicationMessagesCount">The sent application-message count.</param>
/// <param name="SentPacketsCount">The sent packet count.</param>
/// <param name="Session">The associated session status.</param>
public sealed record MqttClientStatusProperties(
    long BytesReceived,
    long BytesSent,
    DateTimeOffset ConnectedTimestamp,
    EndPoint? RemoteEndPoint,
    string? Endpoint,
    string Id,
    DateTimeOffset LastNonKeepAlivePacketReceivedTimestamp,
    DateTimeOffset LastPacketReceivedTimestamp,
    DateTimeOffset LastPacketSentTimestamp,
    MqttProtocolVersion ProtocolVersion,
    long ReceivedApplicationMessagesCount,
    long ReceivedPacketsCount,
    long SentApplicationMessagesCount,
    long SentPacketsCount,
    MqttSessionStatus Session);
