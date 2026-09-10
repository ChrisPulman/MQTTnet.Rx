// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Identifies how MQTT TLS certificate validation is handled.</summary>
internal enum CertificateValidationMode
{
    /// <summary>Use MQTTnet and platform default certificate validation.</summary>
    System,

    /// <summary>Use a pinned server certificate thumbprint.</summary>
    PinnedThumbprint,

    /// <summary>Accept every server certificate.</summary>
    AllowAll,

    /// <summary>Reject every server certificate.</summary>
    RejectAll,

    /// <summary>Use an injected validation callback.</summary>
    Callback,
}
