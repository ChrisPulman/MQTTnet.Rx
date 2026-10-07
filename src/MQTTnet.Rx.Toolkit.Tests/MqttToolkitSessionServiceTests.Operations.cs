// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using MQTTnet.Server;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies operation acknowledgments and MQTT 5 subscription metadata.</summary>
public sealed partial class MqttToolkitSessionServiceTests
{
    /// <summary>Checks subscription options reach the broker and unsubscribe acknowledges removal.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task SubscribeAsync_CustomOptionsReachBrokerAndUnsubscribeRemovesFilterAsync()
    {
        const uint identifier = 37;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        var port = GetAvailablePort();
        using var broker = CreateExternalBroker(port);
        var requested = new TaskCompletionSource<InterceptingSubscriptionEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var packet = new TaskCompletionSource<MqttSubscribePacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        broker.InterceptingSubscriptionAsync += args =>
        {
            _ = requested.TrySetResult(args);
            return Task.CompletedTask;
        };
        broker.InterceptingInboundPacketAsync += args =>
        {
            if (args.Packet is MqttSubscribePacket subscribe)
            {
                _ = packet.TrySetResult(subscribe);
            }

            return Task.CompletedTask;
        };
        await broker.StartAsync();
        await using var service = new MqttToolkitSessionService();
        var logs = new ConcurrentQueue<MqttLogEntry>();
        service.LogReceived += (_, log) => logs.Enqueue(log);
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        var subscription = new SubscriptionViewModel
        {
            TopicFilter = "toolkit/options",
            QualityOfService = MqttQualityOfServiceLevel.ExactlyOnce,
            NoLocal = true,
            RetainAsPublished = true,
            RetainHandling = MqttRetainHandling.DoNotSendOnSubscribe,
            SubscriptionIdentifier = identifier,
        };
        subscription.UserProperties.Add(new() { Name = "source", Value = "héllo" });
        subscription.UserProperties.Add(new() { Name = string.Empty, Value = "ignored" });
        await service.SubscribeAsync(subscription, timeout.Token);
        var observed = await requested.Task.WaitAsync(timeout.Token);
        var observedPacket = await packet.Task.WaitAsync(timeout.Token);
        await Assert.That(observed.TopicFilter.NoLocal).IsTrue();
        await Assert.That(observed.TopicFilter.RetainAsPublished).IsTrue();
        await Assert.That(observed.TopicFilter.RetainHandling).IsEqualTo(MqttRetainHandling.DoNotSendOnSubscribe);
        await Assert.That(observed.TopicFilter.QualityOfServiceLevel).IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
        await Assert.That(observedPacket.SubscriptionIdentifier).IsEqualTo(identifier);
        await Assert.That(observed.UserProperties).Count().IsEqualTo(1);
        await Assert.That(observed.UserProperties[0].Name).IsEqualTo("source");
        await Assert.That(Encoding.UTF8.GetString(observed.UserProperties[0].ValueBuffer.Span)).IsEqualTo("héllo");
        await service.UnsubscribeAsync(subscription.TopicFilter, timeout.Token);
        await service.UnsubscribeAsync(subscription.TopicFilter, timeout.Token);
        await Assert.That(HasLog(logs, "Subscribe", "GrantedQoS")).IsTrue();
        await Assert.That(HasLog(logs, "Unsubscribe", "Success")).IsTrue();
        await Assert.That(HasLog(logs, "Unsubscribe", "NoSubscriptionExisted")).IsTrue();
        await service.DisconnectAsync(timeout.Token);
        await broker.StopAsync(new());
    }

    /// <summary>Checks broker rejection becomes an actionable Toolkit operation error.</summary>
    /// <param name="unsubscribe">Whether the broker rejects unsubscribe instead of subscribe.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SubscriptionOperationAsync_BrokerRejectsOperation_ReportsReasonAsync(bool unsubscribe)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        var port = GetAvailablePort();
        using var broker = CreateExternalBroker(port);
        broker.InterceptingSubscriptionAsync += static args =>
        {
            args.Response.ReasonCode = MqttSubscribeReasonCode.NotAuthorized;
            return Task.CompletedTask;
        };
        broker.InterceptingUnsubscriptionAsync += static args =>
        {
            args.Response.ReasonCode = MqttUnsubscribeReasonCode.NotAuthorized;
            return Task.CompletedTask;
        };
        await broker.StartAsync();
        await using var service = new MqttToolkitSessionService();
        await service.ConnectAsync(CreateExternalOptions(port), timeout.Token);
        var failure = await GetObservedExceptionAsync(unsubscribe
            ? service.UnsubscribeAsync(ReconnectTopic, timeout.Token)
            : service.SubscribeAsync(new() { TopicFilter = ReconnectTopic }, timeout.Token));
        await Assert.That(failure).IsTypeOf<InvalidOperationException>();
        await Assert.That(failure!.Message).Contains(ReconnectTopic);
        await Assert.That(failure.Message).Contains("NotAuthorized");
        await service.DisconnectAsync(timeout.Token);
        await broker.StopAsync(new());
    }

    /// <summary>Creates a loopback broker controlled by a test.</summary>
    /// <param name="port">The loopback port.</param>
    /// <returns>The unstarted broker.</returns>
    private static MqttServer CreateExternalBroker(int port) => new MqttServerFactory().CreateMqttServer(
        new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Loopback).WithDefaultEndpointPort(port).Build());

    /// <summary>Creates options for an independently owned broker.</summary>
    /// <param name="port">The loopback port.</param>
    /// <returns>The client options.</returns>
    private static MqttClientOptions CreateExternalOptions(int port) =>
        new MqttClientOptionsBuilder().WithTcpServer(IPAddress.Loopback.ToString(), port)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500).WithClientId("toolkit-external").Build();

    /// <summary>Finds a diagnostic with the requested component and message.</summary>
    /// <param name="logs">The observed diagnostics.</param>
    /// <param name="source">The expected component.</param>
    /// <param name="text">The message fragment.</param>
    /// <returns>Whether a matching diagnostic was emitted.</returns>
    private static bool HasLog(IEnumerable<MqttLogEntry> logs, string source, string text)
    {
        foreach (var log in logs)
        {
            if (log.Source == source && log.Message.Contains(text, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
