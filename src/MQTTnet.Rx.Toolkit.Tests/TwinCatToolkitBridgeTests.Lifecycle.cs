// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Toolkit.Models;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies bridge stopping, startup failures and ADS ownership.</summary>
public sealed partial class TwinCatToolkitBridgeTests
{
    /// <summary>Stores the log component used for ADS lifecycle diagnostics.</summary>
    private const string BridgeLogSource = "TwinCAT";

    /// <summary>Checks stopping an active bridge disconnects ADS and permits idempotent stopping.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StopTwinCatBridgeAsync_ActiveBridge_ReleasesAdsAndReportsStopAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        using var ads = new InMemoryAdsClient();
        _ = ads.RegisterStructure(Symbol, new RigValues { Pressure = InitialPressure });
        await using var service = new MqttToolkitSessionService { TwinCatClientFactory = () => ads };
        var linked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logs = new System.Collections.Concurrent.ConcurrentQueue<MqttLogEntry>();
        service.LogReceived += (_, log) =>
        {
            logs.Enqueue(log);
            if (log.Source == BridgeLogSource && log.Message.Contains("linked;", StringComparison.Ordinal))
            {
                _ = linked.TrySetResult();
            }
        };
        using var connection = new ViewModels.ConnectionOptionsViewModel { Port = GetAvailablePort() };
        await service.StartEmbeddedServerAsync(connection.Port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.StartTwinCatBridgeAsync(CreateConfiguration(), timeout.Token);
        await linked.Task.WaitAsync(timeout.Token);
        await service.StopTwinCatBridgeAsync(timeout.Token);
        await service.StopTwinCatBridgeAsync(timeout.Token);
        await Assert.That(ads.Connected).IsFalse();
        var stopCount = 0;
        foreach (var log in logs)
        {
            if (log.Source == BridgeLogSource && log.Message == "Structure subscription stopped.")
            {
                stopCount++;
            }
        }

        await Assert.That(stopCount).IsEqualTo(1);
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks a failed ADS factory is reported through Toolkit logs and can be stopped.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StartTwinCatBridgeAsync_AdsFactoryFails_ReportsErrorAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        await using var service = new MqttToolkitSessionService
        {
            TwinCatClientFactory = static () => throw new InvalidOperationException("ADS factory unavailable"),
        };
        var error = new TaskCompletionSource<MqttLogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.LogReceived += (_, log) =>
        {
            if (log.Source == BridgeLogSource && log.Level == "Error")
            {
                _ = error.TrySetResult(log);
            }
        };
        using var connection = new ViewModels.ConnectionOptionsViewModel { Port = GetAvailablePort() };
        await service.StartEmbeddedServerAsync(connection.Port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.StartTwinCatBridgeAsync(CreateConfiguration(), timeout.Token);
        await Assert.That((await error.Task.WaitAsync(timeout.Token)).Message).Contains("ADS factory unavailable");
        await service.StopTwinCatBridgeAsync(timeout.Token);
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks bridge startup validates its arguments and requires a live MQTT connection.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StartTwinCatBridgeAsync_NoMqttClient_RejectsStartupAsync()
    {
        await using var service = new MqttToolkitSessionService();
        await Assert.That(() => service.StartTwinCatBridgeAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => service.StartTwinCatBridgeAsync(CreateConfiguration(), CancellationToken.None)).Throws<InvalidOperationException>();
        await service.StopTwinCatBridgeAsync(CancellationToken.None);
        await service.DisposeAsync();
        await Assert.That(() => service.StartTwinCatBridgeAsync(CreateConfiguration(), CancellationToken.None)).Throws<ObjectDisposedException>();
    }

    /// <summary>Checks a client whose broker has gone away cannot own a new ADS connection.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task StartTwinCatBridgeAsync_MqttClientDisconnected_RejectsStartupAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.LogReceived += (_, log) =>
        {
            if (log.Source == "Client" && log.Level == "Warning")
            {
                _ = disconnected.TrySetResult();
            }
        };
        using var connection = new ViewModels.ConnectionOptionsViewModel { Port = GetAvailablePort() };
        await service.StartEmbeddedServerAsync(connection.Port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
        await disconnected.Task.WaitAsync(timeout.Token);
        await Assert.That(() => service.StartTwinCatBridgeAsync(CreateConfiguration(), timeout.Token)).Throws<InvalidOperationException>();
        await service.DisconnectAsync(timeout.Token);
    }
}
#endif
