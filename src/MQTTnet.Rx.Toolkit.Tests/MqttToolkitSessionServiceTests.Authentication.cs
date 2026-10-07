// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using NSubstitute;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies live MQTT authentication packets and scripted reauthentication reset.</summary>
public sealed partial class MqttToolkitSessionServiceTests
{
    /// <summary>Stores the enhanced authentication method negotiated with the test broker.</summary>
    private const string AuthenticationMethod = "toolkit-test";

    /// <summary>Checks outgoing authentication preserves binary data and reason metadata.</summary>
    /// <param name="reasonCode">The client authentication phase.</param>
    /// <param name="scripted">Whether the connection owns a scripted challenge handler.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments(MqttAuthenticateReasonCode.ContinueAuthentication, false)]
    [Arguments(MqttAuthenticateReasonCode.ReAuthenticate, false)]
    [Arguments(MqttAuthenticateReasonCode.ReAuthenticate, true)]
    public async Task SendEnhancedAuthenticationExchangeDataAsync_ValidPhase_SendsMetadataAsync(MqttAuthenticateReasonCode reasonCode, bool scripted)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        var port = GetAvailablePort();
        using var broker = CreateExternalBroker(port);
        var observed = new TaskCompletionSource<MqttAuthPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        broker.InterceptingInboundPacketAsync += args =>
        {
            if (args.Packet is MqttAuthPacket packet)
            {
                args.ProcessPacket = false;
                _ = observed.TrySetResult(packet);
            }

            return Task.CompletedTask;
        };
        await broker.StartAsync();
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "scripted" }]);
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var responses = new ConcurrentQueue<MqttPacket>();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            responses.Enqueue(call.Arg<MqttPacket>());
            return Task.CompletedTask;
        });
        var challenge = new MqttEnhancedAuthenticationEventArgs(
            new MqttAuthPacket { AuthenticationMethod = AuthenticationMethod, ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication },
            adapter,
            timeout.Token);
        if (scripted)
        {
            await handler.HandleEnhancedAuthenticationAsync(challenge);
        }

        var builder = new MqttClientOptionsBuilder().WithTcpServer(IPAddress.Loopback.ToString(), port)
            .WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("toolkit-external").WithEnhancedAuthentication(AuthenticationMethod);
        if (scripted)
        {
            _ = builder.WithEnhancedAuthenticationHandler(handler);
        }

        await using var service = new MqttToolkitSessionService();
        var logs = new ConcurrentQueue<MqttLogEntry>();
        service.LogReceived += (_, log) => logs.Enqueue(log);
        await service.ConnectAsync(builder.Build(), timeout.Token);
        await service.SendEnhancedAuthenticationExchangeDataAsync(
            new() { ReasonCode = reasonCode, Data = "00FF", DataFormat = PayloadFormat.Hex, Reason = "renew identity" },
            timeout.Token);
        var packet = await observed.Task.WaitAsync(timeout.Token);
        await AssertAuthenticationPacketAsync(packet, reasonCode, logs);
        if (scripted)
        {
            await handler.HandleEnhancedAuthenticationAsync(challenge);
            const int expectedResponses = 2;
            await Assert.That(responses).Count().IsEqualTo(expectedResponses);
        }

        await service.DisconnectAsync(timeout.Token);
        await broker.StopAsync(new());
    }

    /// <summary>Checks the broker received the requested authentication metadata and its diagnostic.</summary>
    /// <param name="packet">The authentication packet captured by the broker.</param>
    /// <param name="reasonCode">The requested client authentication phase.</param>
    /// <param name="logs">The service diagnostics.</param>
    /// <returns>The asynchronous assertions.</returns>
    private static async Task AssertAuthenticationPacketAsync(MqttAuthPacket packet, MqttAuthenticateReasonCode reasonCode, IEnumerable<MqttLogEntry> logs)
    {
        await Assert.That(packet.AuthenticationMethod).IsEqualTo(AuthenticationMethod);
        await Assert.That(Convert.ToHexString(packet.AuthenticationData)).IsEqualTo("00FF");
        await Assert.That(packet.ReasonCode).IsEqualTo(reasonCode);
        await Assert.That(packet.ReasonString).IsEqualTo("renew identity");
        await Assert.That(HasLog(logs, "Authentication", reasonCode.ToString())).IsTrue();
    }
}
