// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies channel option validation rejects unsupported WebSocket window sizes.</summary>
public sealed class ConnectionOptionHelpersTests
{
    /// <summary>Verifies both supported window-size boundaries are accepted.</summary>
    /// <param name="bits">The requested window size.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(8)]
    [Arguments(15)]
    public async Task ValidateWebSocketDeflateWindowBits_AcceptsBoundariesAsync(int bits)
    {
        await Assert.That(() => ConnectionOptionHelpers.ValidateWebSocketDeflateWindowBits(bits, "window")).ThrowsNothing();
    }

    /// <summary>Verifies unsupported window sizes identify the invalid property.</summary>
    /// <param name="bits">The requested window size.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(7)]
    [Arguments(16)]
    public async Task ValidateWebSocketDeflateWindowBits_RejectsOutsideBoundariesAsync(int bits)
    {
        var exception = await Assert.That(() => ConnectionOptionHelpers.ValidateWebSocketDeflateWindowBits(bits, "client-window"))
            .Throws<InvalidOperationException>() ?? throw new InvalidOperationException("Expected validation failure.");
        await Assert.That(exception.Message).Contains("client-window");
    }
}
