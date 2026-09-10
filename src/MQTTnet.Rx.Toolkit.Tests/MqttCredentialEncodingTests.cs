// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies lossless encoding and presence of MQTT credentials.</summary>
public sealed class MqttCredentialEncodingTests
{
    /// <summary>Verifies a binary password preserves bytes that cannot be represented as UTF-8.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PreservesBinaryPasswordAndWhitespaceUsernameAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            Username = " ",
            Password = "AP8=",
            PasswordFormat = PayloadFormat.Base64,
        };
        var options = connection.BuildClientOptions();
        await Assert.That(options.Credentials.GetUserName(options)).IsEqualTo(" ");
        await Assert.That(Convert.ToHexString(options.Credentials.GetPassword(options) ?? [])).IsEqualTo("00FF");
    }

    /// <summary>Verifies explicit empty credentials can be sent independently of anonymous defaults.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task IncludesExplicitEmptyCredentialsAsync()
    {
        using var connection = new ConnectionOptionsViewModel { UseCredentials = true };
        var options = connection.BuildClientOptions();
        await Assert.That(options.Credentials.GetUserName(options)).IsEmpty();
        await Assert.That(options.Credentials.GetPassword(options)).IsEmpty();
    }
}
