// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Rx.Toolkit;

/// <summary>Provides reusable helpers for MQTT client option mapping.</summary>
internal static class ConnectionOptionHelpers
{
    /// <summary>Selects the first available local certificate.</summary>
    /// <param name="certificates">The available local certificates.</param>
    /// <returns>The selected certificate.</returns>
    internal static X509Certificate SelectFirstCertificate(X509CertificateCollection certificates)
    {
        if (certificates.Count > 0)
        {
            return certificates[0];
        }

        throw new InvalidOperationException("No local client certificate is available for selection.");
    }

    /// <summary>Validates a WebSocket deflate window bit value.</summary>
    /// <param name="value">The configured window bit value.</param>
    /// <param name="propertyName">The property name reported in validation exceptions.</param>
    internal static void ValidateWebSocketDeflateWindowBits(int value, string propertyName)
    {
        const int minimumWindowBits = 8;
        const int maximumWindowBits = 15;
        if (value is < minimumWindowBits or > maximumWindowBits)
        {
            throw new InvalidOperationException($"{propertyName} must be between 8 and 15.");
        }
    }
}
