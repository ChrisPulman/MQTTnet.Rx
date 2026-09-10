// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Identifies how client certificates are supplied to MQTTnet TLS options.</summary>
internal enum ClientCertificateSource
{
    /// <summary>No client certificate is supplied.</summary>
    None,

    /// <summary>A PKCS12 certificate file is loaded from the configured path.</summary>
    File,

    /// <summary>Certificates are loaded from an operating-system certificate store.</summary>
    Store,

    /// <summary>Certificates are supplied by an injected provider callback.</summary>
    Provider,
}
