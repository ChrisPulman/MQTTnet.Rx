// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Adapter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;
using NSubstitute;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies scripted authentication uses one response per broker challenge.</summary>
public sealed class EnhancedAuthenticationTests
{
    /// <summary>Verifies ordered, lossless responses and reports an exhausted script promptly.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task SendsOneResponsePerChallengeAsync()
    {
        const int responseCount = 2;
        var packets = new List<MqttAuthPacket>();
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        using var cancellation = new CancellationTokenSource();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), cancellation.Token).Returns(call =>
        {
            packets.Add((MqttAuthPacket)call.Arg<MqttPacket>());
            return Task.CompletedTask;
        });
        var handler = new ScriptedEnhancedAuthenticationHandler(
        [
            new() { Data = "AP8=", DataFormat = PayloadFormat.Base64, Reason = "first" },
            new() { Data = "second" },
        ]);
        var challenge = new MqttEnhancedAuthenticationEventArgs(
            new MqttAuthPacket { AuthenticationMethod = "challenge", ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication },
            adapter,
            cancellation.Token);

        await handler.HandleEnhancedAuthenticationAsync(challenge);
        await Assert.That(packets.Count).IsEqualTo(1);
        await Assert.That(Convert.ToHexString(packets[0].AuthenticationData)).IsEqualTo("00FF");
        await Assert.That(packets[0].AuthenticationMethod).IsEqualTo("challenge");
        await Assert.That(packets[0].ReasonString).IsEqualTo("first");
        await Assert.That(packets[0].ReasonCode).IsEqualTo(MqttAuthenticateReasonCode.ContinueAuthentication);

        await handler.HandleEnhancedAuthenticationAsync(challenge);
        await Assert.That(packets.Count).IsEqualTo(responseCount);
        await Assert.That(System.Text.Encoding.UTF8.GetString(packets[1].AuthenticationData)).IsEqualTo("second");
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(challenge)).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies an unexpected challenge cannot consume a configured response.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task RejectsUnexpectedChallengeAsync()
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "response" }]);
        var challenge = new MqttAuthPacket { ReasonCode = MqttAuthenticateReasonCode.ReAuthenticate };
        var eventArgs = new MqttEnhancedAuthenticationEventArgs(challenge, adapter, CancellationToken.None);
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(eventArgs)).Throws<InvalidOperationException>();
        challenge.ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication;
        await handler.HandleEnhancedAuthenticationAsync(eventArgs);
        using var connection = new ConnectionOptionsViewModel();
        await Assert.That(connection.EnhancedAuthenticationStepReasonCode).IsEqualTo(MqttAuthenticateReasonCode.ContinueAuthentication);
    }

    /// <summary>Verifies server success completes an exchange without sending another response.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task SuccessResetsTheScriptForReauthenticationAsync()
    {
        const int expectedResponses = 2;
        var responseCount = 0;
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            responseCount++;
            return Task.CompletedTask;
        });
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "response" }]);
        var challenge = new MqttAuthPacket { ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication };
        var eventArgs = new MqttEnhancedAuthenticationEventArgs(challenge, adapter, CancellationToken.None);
        await handler.HandleEnhancedAuthenticationAsync(eventArgs);
        challenge.ReasonCode = MqttAuthenticateReasonCode.Success;
        await handler.HandleEnhancedAuthenticationAsync(eventArgs);
        await Assert.That(responseCount).IsEqualTo(1);
        challenge.ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication;
        await handler.HandleEnhancedAuthenticationAsync(eventArgs);
        await Assert.That(responseCount).IsEqualTo(expectedResponses);
    }
}
