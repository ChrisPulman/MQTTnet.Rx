// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Identifies how MQTT TLS client certificate selection is handled.</summary>
internal enum CertificateSelectionMode
{
    /// <summary>Let MQTTnet and the platform select a certificate.</summary>
    Automatic,

    /// <summary>Select the first available local certificate.</summary>
    First,

    /// <summary>Select a configured certificate thumbprint.</summary>
    Thumbprint,

    /// <summary>Use an injected certificate selection callback.</summary>
    Callback,
}
