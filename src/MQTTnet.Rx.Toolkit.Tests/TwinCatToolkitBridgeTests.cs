// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using System.Net;
using System.Net.Sockets;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies the Toolkit configuration and structure publisher against simulated ADS and a real MQTT broker.</summary>
public sealed partial class TwinCatToolkitBridgeTests
{
    /// <summary>Stores the simulated PLC structure symbol.</summary>
    private const string Symbol = "GVL.Rig";

    /// <summary>Stores the initial pressure value.</summary>
    private const int InitialPressure = 42;

    /// <summary>Stores the updated pressure value.</summary>
    private const int UpdatedPressure = 43;

    /// <summary>Stores the integration timeout in seconds.</summary>
    private const int TimeoutSeconds = 15;

    /// <summary>Checks exported settings can configure another Toolkit instance.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ConfigurationRoundTripsCoreLibraryOptionsAsync()
    {
        var original = CreateConfiguration();
        var restored = new TwinCatBridgeViewModel { ConfigurationJson = original.ExportConfiguration() };
        restored.ImportConfiguration();
        await Assert.That(restored.AmsNetId).IsEqualTo(original.AmsNetId);
        await Assert.That(restored.AdsPort).IsEqualTo(original.AdsPort);
        await Assert.That(restored.PlcVariable).IsEqualTo(Symbol);
        await Assert.That(restored.TopicPrefix).IsEqualTo(original.TopicPrefix);
        await Assert.That(restored.Retain).IsTrue();
        await Assert.That(restored.QualityOfService).IsEqualTo(original.QualityOfService);
        await Assert.That(restored.ExportConfiguration()).IsEqualTo(original.ExportConfiguration());
    }

    /// <summary>Checks a structure publishes its initial and changed values through the Toolkit's existing MQTT connection.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StructureSubscriptionPublishesValuesAndDisconnectReleasesAdsAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        using var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(Symbol, new RigValues { Pressure = InitialPressure });
        await using var service = new MqttToolkitSessionService { TwinCatClientFactory = () => ads };
        var payloads = System.Threading.Channels.Channel.CreateUnbounded<string>();
        service.MessageReceived += (_, message) =>
        {
            if (message.Source == "Client received" && message.Topic == "test/rig/Pressure")
            {
                _ = payloads.Writer.TryWrite(message.Payload);
            }
        };
        using var connection = new ConnectionOptionsViewModel { Port = GetAvailablePort() };
        await service.StartEmbeddedServerAsync(connection.Port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.SubscribeAsync(new() { TopicFilter = "test/rig/#" }, timeout.Token);
        await service.StartTwinCatBridgeAsync(CreateConfiguration(), timeout.Token);

        await Assert.That(await payloads.Reader.ReadAsync(timeout.Token)).IsEqualTo("42");
        ads.SetValue(Symbol, new RigValues { Pressure = UpdatedPressure });
        await Assert.That(await payloads.Reader.ReadAsync(timeout.Token)).IsEqualTo("43");
        await service.DisconnectAsync(timeout.Token);
        await Assert.That(ads.Connected).IsFalse();
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Creates the reusable test bridge settings.</summary>
    /// <returns>The simulated connection settings.</returns>
    private static TwinCatBridgeViewModel CreateConfiguration() => new()
    {
        AmsNetId = "127.0.0.1.1.1",
        PlcVariable = Symbol,
        TopicPrefix = "test/rig",
    };

    /// <summary>Gets a free loopback port for the test broker.</summary>
    /// <returns>The loopback TCP port.</returns>
    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    /// <summary>Represents a dynamically discovered PLC structure in the ADS simulator.</summary>
    public sealed class RigValues
    {
        /// <summary>Gets or sets the structure pressure member.</summary>
        public int Pressure { get; set; }
    }
}
#endif
