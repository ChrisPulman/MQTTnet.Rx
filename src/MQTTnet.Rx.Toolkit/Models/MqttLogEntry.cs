// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Represents a timestamped toolkit log item.</summary>
/// <param name="Timestamp">The local timestamp assigned to the log entry.</param>
/// <param name="Level">The severity level used by the Toolkit display.</param>
/// <param name="Source">The component that produced the log entry.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
internal sealed record MqttLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message);
