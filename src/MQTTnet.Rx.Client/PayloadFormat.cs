// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Identifies a display or text-entry format for MQTT payload bytes.</summary>
public enum PayloadFormat
{
    /// <summary>Strict UTF-8 text.</summary>
    Utf8Text,

    /// <summary>JSON object or array text.</summary>
    Json,

    /// <summary>Boolean text.</summary>
    Boolean,

    /// <summary>Invariant-culture numeric text.</summary>
    Number,

    /// <summary>Hexadecimal encoded bytes.</summary>
    Hex,

    /// <summary>Base64 encoded bytes.</summary>
    Base64,

    /// <summary>An empty payload.</summary>
    Empty,
}
