// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Tests the public compositional resilient-client construction seam.</summary>
[NotInParallel]
public sealed class ResilientMqttClientFactoryCoverageTests
{
    /// <summary>Creates a resilient client around a caller-owned MQTTnet client.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Create_WrapsTheSuppliedClientAsync()
    {
        using var mqttClient = new ScriptedMqttClient();
        var factory = new MqttClientFactory();
        using var resilientClient = ResilientMqttClientFactory.Create(mqttClient, factory.DefaultLogger);

        await Assert.That(resilientClient.InternalClient).IsSameReferenceAs(mqttClient);
        await Assert.That(resilientClient.IsStarted).IsFalse();
    }

    /// <summary>Rejects missing required factory collaborators.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Create_RejectsNullArgumentsAsync()
    {
        var factory = new MqttClientFactory();
        using var mqttClient = new ScriptedMqttClient();

        await Assert.That(() => ResilientMqttClientFactory.Create(null!, factory.DefaultLogger))
            .Throws<ArgumentNullException>();
        await Assert.That(() => ResilientMqttClientFactory.Create(mqttClient, null!)).Throws<ArgumentNullException>();
    }
}
