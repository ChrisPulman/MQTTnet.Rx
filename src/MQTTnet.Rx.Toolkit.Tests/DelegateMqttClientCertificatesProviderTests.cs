// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies certificate provider callback failures reach the connection caller.</summary>
public sealed class DelegateMqttClientCertificatesProviderTests
{
    /// <summary>Verifies provider failures retain the original exception.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task GetCertificates_PropagatesProviderFailureAsync()
    {
        var failure = new InvalidOperationException("certificate provider failed");
        var provider = new DelegateMqttClientCertificatesProvider(() => throw failure);
        var observed = await Assert.That(provider.GetCertificates).Throws<InvalidOperationException>();
        await Assert.That(observed).IsSameReferenceAs(failure);
    }
}
