// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.AspNetCore.Reactive;
#else
namespace MQTTnet.Rx.AspNetCore;
#endif

/// <summary>Represents a point-in-time snapshot of an MQTT ASP.NET Core connection.</summary>
/// <param name="BytesReceived">The number of bytes received.</param>
/// <param name="BytesSent">The number of bytes sent.</param>
/// <param name="ClientCertificate">The optional client certificate.</param>
/// <param name="IsSecureConnection">Whether the connection is secure.</param>
/// <param name="LocalEndPoint">The optional local endpoint.</param>
/// <param name="RemoteEndPoint">The optional remote endpoint.</param>
/// <param name="PacketFormatterAdapter">The packet formatter adapter.</param>
public sealed record MqttConnectionProperties(
    long BytesReceived,
    long BytesSent,
    X509Certificate2? ClientCertificate,
    bool IsSecureConnection,
    EndPoint? LocalEndPoint,
    EndPoint? RemoteEndPoint,
    MqttPacketFormatterAdapter PacketFormatterAdapter);
