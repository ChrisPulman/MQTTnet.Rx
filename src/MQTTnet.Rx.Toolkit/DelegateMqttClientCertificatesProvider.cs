// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Rx.Toolkit;

/// <summary>Supplies MQTTnet client certificates from a callback.</summary>
/// <param name="getCertificates">The callback used to get client certificates.</param>
internal sealed class DelegateMqttClientCertificatesProvider(Func<X509CertificateCollection> getCertificates) : IMqttClientCertificatesProvider
{
    /// <inheritdoc/>
    public X509CertificateCollection GetCertificates() => getCertificates();
}
