// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Classifies topic and payload validation findings.</summary>
public enum TopicIssueKind
{
    /// <summary>The MQTT topic name is empty.</summary>
    EmptyTopic,

    /// <summary>The MQTT publish topic contains wildcard characters.</summary>
    ContainsWildcard,

    /// <summary>The MQTT topic contains whitespace and is shown as a convention warning only.</summary>
    ContainsWhitespace,

    /// <summary>The MQTT topic starts with a slash and is shown as a convention warning only.</summary>
    LeadingSlash,

    /// <summary>The MQTT topic contains an empty level and is shown as a convention warning only.</summary>
    ConsecutiveSlash,

    /// <summary>The payload does not match the declared metadata.</summary>
    InvalidPayload,
}
