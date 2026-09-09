// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using System.Threading.Channels;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies actual nested ADS structures retain all initial values and stable member topics.</summary>
public sealed partial class TwinCatToolkitBridgeTests
{
    /// <summary>Stores the ADS port used by TwinCAT 2.</summary>
    private const int TwinCat2Port = 801;

    /// <summary>Stores the ADS port used by TwinCAT 3.</summary>
    private const int TwinCat3Port = 851;

    /// <summary>Stores the number of leaf topics in the nested test structure.</summary>
    private const int NestedLeafCount = 2;

    /// <summary>Verifies initial and changed nested values through the Toolkit connection for both ADS casing modes.</summary>
    /// <param name="adsPort">The simulated ADS runtime port.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(TwinCat2Port)]
    [Arguments(TwinCat3Port)]
    public async Task NestedStructurePublishesEveryInitialLeafAndStableChangedTopicsAsync(int adsPort)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        using var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(Symbol, new NestedRig { Sensor = new() { Pressure = InitialPressure }, Ready = true });
        await using var service = new MqttToolkitSessionService { TwinCatClientFactory = () => ads };
        var messages = System.Threading.Channels.Channel.CreateUnbounded<(string Topic, string Payload)>();
        service.MessageReceived += (_, message) =>
        {
            if (message.Source == "Client received")
            {
                _ = messages.Writer.TryWrite((message.Topic, message.Payload));
            }
        };
        using var connection = new ConnectionOptionsViewModel { Port = GetAvailablePort() };
        await service.StartEmbeddedServerAsync(connection.Port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.SubscribeAsync(new() { TopicFilter = "test/rig/#" }, timeout.Token);
        var configuration = CreateConfiguration();
        configuration.AdsPort = adsPort;
        await service.StartTwinCatBridgeAsync(configuration, timeout.Token);

        var initial = await ReadNestedSnapshotAsync(messages.Reader, timeout.Token);
        var pressureTopic = adsPort == TwinCat2Port ? "test/rig/SENSOR/PRESSURE" : "test/rig/Sensor/Pressure";
        var readyTopic = adsPort == TwinCat2Port ? "test/rig/READY" : "test/rig/Ready";
        await Assert.That(string.Join(",", initial.Keys.Order(StringComparer.Ordinal)))
            .IsEqualTo(string.Join(",", new[] { pressureTopic, readyTopic }.Order(StringComparer.Ordinal)));
        await Assert.That(initial[pressureTopic]).IsEqualTo("42");
        await Assert.That(initial[readyTopic]).IsEqualTo("True");
        ads.SetValue(Symbol, new NestedRig { Sensor = new() { Pressure = UpdatedPressure }, Ready = false });
        var changed = await ReadNestedSnapshotAsync(messages.Reader, timeout.Token);
        await Assert.That(changed[pressureTopic]).IsEqualTo("43");
        await Assert.That(changed[readyTopic]).IsEqualTo("False");
        await service.DisconnectAsync(timeout.Token);
        await Assert.That(ads.Connected).IsFalse();
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Reads one complete structure snapshot without accepting duplicate topic observations.</summary>
    /// <param name="reader">The received MQTT messages.</param>
    /// <param name="cancellationToken">The bounded test cancellation token.</param>
    /// <returns>The values indexed by topic.</returns>
    private static async Task<Dictionary<string, string>> ReadNestedSnapshotAsync(
        ChannelReader<(string Topic, string Payload)> reader,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < NestedLeafCount; index++)
        {
            var message = await reader.ReadAsync(cancellationToken);
            result.Add(message.Topic, message.Payload);
        }

        return result;
    }

    /// <summary>Represents nested PLC data discovered dynamically by ADS.</summary>
    public sealed class NestedRig
    {
        /// <summary>Gets or sets the sensor structure.</summary>
        public RigValues Sensor { get; set; } = new();

        /// <summary>Gets or sets the ready flag.</summary>
        public bool Ready { get; set; }
    }
}
#endif
