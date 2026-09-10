// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Represents a topic or payload diagnostic detected by the inspector.</summary>
/// <param name="Timestamp">The timestamp assigned to the diagnostic.</param>
/// <param name="Topic">The MQTT topic related to the diagnostic.</param>
/// <param name="Kind">The diagnostic category.</param>
/// <param name="Detail">The human-readable diagnostic detail.</param>
public sealed record TopicIssue(
    DateTimeOffset Timestamp,
    string Topic,
    TopicIssueKind Kind,
    string Detail);
