// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
using MQTTnet.Rx.TwinCAT.Reactive;
#else
using CP.Collections;
using MQTTnet.Rx.TwinCAT;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies replacing a client republishes the complete structure and stops the old publisher.</summary>
public sealed class TwinCatStructureClientSwitchTests
{
    /// <summary>Stores the timeout for asynchronous publication results.</summary>
    private const int TimeoutSeconds = 5;

    /// <summary>Stores the number of initial structure members.</summary>
    private const int MemberCount = 2;

    /// <summary>Stores the initial pressure value.</summary>
    private const int InitialPressure = 42;

    /// <summary>Stores the changed pressure value.</summary>
    private const int ChangedPressure = 43;

    /// <summary>Checks replacement clients receive every current member before subsequent updates.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ReplacingClientRepublishesSnapshotAndStopsOldPublisherAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        using var first = new MockMqttClient();
        using var second = new MockMqttClient();
        using var clients = new TestSignal<IMqttClient>();
        using var structure = new HashTableRx(useUpperCase: false);
        structure.Add("Pressure", InitialPressure);
        structure.Add("Ready", true);
        var results = System.Threading.Channels.Channel.CreateUnbounded<MqttClientPublishResult>();
        using var publication = clients.PublishTcStructure(structure, new() { TopicPrefix = "switch/rig" })
            .Subscribe(result => { _ = results.Writer.TryWrite(result); });

        clients.OnNext(first);
        for (var index = 0; index < MemberCount; index++)
        {
            _ = await results.Reader.ReadAsync(timeout.Token);
        }

        clients.OnNext(second);
        for (var index = 0; index < MemberCount; index++)
        {
            _ = await results.Reader.ReadAsync(timeout.Token);
        }

        structure["Pressure"] = ChangedPressure;
        _ = await results.Reader.ReadAsync(timeout.Token);
        await Assert.That(first.PublishedMessages).Count().IsEqualTo(MemberCount);
        await Assert.That(second.PublishedMessages).Count().IsEqualTo(MemberCount + 1);
        var topics = new List<string>();
        foreach (var message in second.PublishedMessages)
        {
            topics.Add(message.Topic);
        }

        await Assert.That(topics).Contains("switch/rig/Ready");
        await Assert.That(second.PublishedMessages[^1].Topic).IsEqualTo("switch/rig/Pressure");
        await Assert.That(second.PublishedMessages[^1].ConvertPayloadToString()).IsEqualTo("43");
    }
}
#endif
