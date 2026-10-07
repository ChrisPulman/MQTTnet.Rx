// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies observable diagnostics reach Toolkit consumers.</summary>
public sealed partial class MqttToolkitSessionServiceTests
{
    /// <summary>Checks invalid JSON reaches both ingress and client diagnostics alongside packet traces.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishAsync_InvalidJson_ReportsBrokerAndClientDiagnosticsAsync()
    {
        const int expectedIssues = 2;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        var issues = new ConcurrentQueue<TopicIssue>();
        var logs = new ConcurrentQueue<MqttLogEntry>();
        var diagnosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.LogReceived += (_, entry) => logs.Enqueue(entry);
        service.TopicIssueDetected += (_, issue) =>
        {
            issues.Enqueue(issue);
            if (issues.Count == expectedIssues)
            {
                _ = diagnosed.TrySetResult();
            }
        };
        var port = GetAvailablePort();
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        await service.SubscribeAsync(new() { TopicFilter = ReconnectTopic }, timeout.Token);
        await service.PublishAsync(
            new MqttApplicationMessageBuilder().WithTopic(ReconnectTopic).WithPayload("{invalid")
                .WithContentType("application/json").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build(),
            timeout.Token);
        await diagnosed.Task.WaitAsync(timeout.Token);
        await Assert.That(issues).Count().IsEqualTo(expectedIssues);
        foreach (var issue in issues)
        {
            await Assert.That(issue.Topic).IsEqualTo(ReconnectTopic);
            await Assert.That(issue.Kind).IsEqualTo(TopicIssueKind.InvalidPayload);
        }

        await Assert.That(HasLog(logs, "Packet", "bytes")).IsTrue();
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Checks remote disconnect produces a warning and false connection state.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ConnectAsync_BrokerDisconnects_ReportsLostConnectionAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        var port = GetAvailablePort();
        using var broker = CreateExternalBroker(port);
        await broker.StartAsync();
        await using var service = new MqttToolkitSessionService();
        var disconnected = new TaskCompletionSource<MqttLogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
        var states = new ConcurrentQueue<bool>();
        service.ConnectionChanged += (_, state) => states.Enqueue(state);
        service.LogReceived += (_, log) =>
        {
            if (log.Source == "Client" && log.Level == "Warning")
            {
                _ = disconnected.TrySetResult(log);
            }
        };
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        await broker.DisconnectClientAsync("toolkit-external", new() { ReasonCode = MqttDisconnectReasonCode.AdministrativeAction });
        var warning = await disconnected.Task.WaitAsync(timeout.Token);
        await Assert.That(warning.Message).Contains("Disconnected:");
        await Assert.That(states.ToArray()[^1]).IsFalse();
        await service.DisconnectAsync(timeout.Token);
        await broker.StopAsync(new());
    }

    /// <summary>Checks authentication success is rejected locally because only the broker may send it.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task SendEnhancedAuthenticationExchangeDataAsync_Success_RejectsClientOriginAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService();
        var port = GetAvailablePort();
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        await Assert.That(() => service.SendEnhancedAuthenticationExchangeDataAsync(
            new EnhancedAuthenticationStepViewModel { ReasonCode = MqttAuthenticateReasonCode.Success },
            timeout.Token))
            .Throws<InvalidOperationException>();
        await service.DisconnectAsync(timeout.Token);
        await service.StopEmbeddedServerAsync(timeout.Token);
    }
}
