// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>Contains the exact topic and copied payload captured from a live MQTT message.</summary>
/// <param name="Topic">The received topic.</param>
/// <param name="Payload">The copied payload bytes.</param>
public sealed record LiveMqttMessage(string Topic, byte[] Payload);
