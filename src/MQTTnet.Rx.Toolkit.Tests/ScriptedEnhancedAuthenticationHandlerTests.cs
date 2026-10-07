// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Adapter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using NSubstitute;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies scripted authentication rejects invalid input and forwards transport failures.</summary>
public sealed class ScriptedEnhancedAuthenticationHandlerTests
{
    /// <summary>Stores the first scripted response payload.</summary>
    private const string FirstResponse = "first";

    /// <summary>Stores the number of responses sent after a reset.</summary>
    private const int ResetResponseCount = 2;

    /// <summary>Verifies a missing challenge is rejected explicitly.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task HandleEnhancedAuthenticationAsync_RejectsMissingChallengeAsync()
    {
        var handler = new ScriptedEnhancedAuthenticationHandler([]);
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies a success signal needs no response even when no scripted steps remain.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task HandleEnhancedAuthenticationAsync_EmptyScriptAcceptsSuccessAndRejectsChallengeAsync()
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var packet = new MqttAuthPacket { ReasonCode = MqttAuthenticateReasonCode.Success };
        var args = new MqttEnhancedAuthenticationEventArgs(packet, adapter, CancellationToken.None);
        var handler = new ScriptedEnhancedAuthenticationHandler([]);
        await handler.HandleEnhancedAuthenticationAsync(args);
        packet.ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication;
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(args)).Throws<InvalidOperationException>();
        await Assert.That(adapter.ReceivedCalls()).IsEmpty();
    }

    /// <summary>Verifies reset starts the response script again without replacing the handler.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task Reset_ReplaysFirstResponseAsync()
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var packets = new List<MqttAuthPacket>();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            packets.Add((MqttAuthPacket)call.Arg<MqttPacket>());
            return Task.CompletedTask;
        });
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = FirstResponse }, new() { Data = "second" }]);
        var args = new MqttEnhancedAuthenticationEventArgs(new() { ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication }, adapter, CancellationToken.None);
        await handler.HandleEnhancedAuthenticationAsync(args);
        handler.Reset();
        await handler.HandleEnhancedAuthenticationAsync(args);
        await Assert.That(packets.Count).IsEqualTo(ResetResponseCount);
        await Assert.That(System.Text.Encoding.UTF8.GetString(packets[0].AuthenticationData)).IsEqualTo(FirstResponse);
        await Assert.That(System.Text.Encoding.UTF8.GetString(packets[1].AuthenticationData)).IsEqualTo(FirstResponse);
    }

    /// <summary>Verifies invalid response encodings fail before sending an authentication packet.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task HandleEnhancedAuthenticationAsync_InvalidEncodingDoesNotSendAsync()
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "invalid!", DataFormat = PayloadFormat.Base64 }]);
        var args = new MqttEnhancedAuthenticationEventArgs(new() { ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication }, adapter, CancellationToken.None);
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(args)).Throws<FormatException>();
        await Assert.That(adapter.ReceivedCalls()).IsEmpty();
    }

    /// <summary>Verifies a channel send failure is preserved for the caller.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task HandleEnhancedAuthenticationAsync_PropagatesSendFailureAsync()
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        var failure = new InvalidOperationException("channel failed");
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "response" }]);
        var args = new MqttEnhancedAuthenticationEventArgs(new() { ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication }, adapter, CancellationToken.None);
        var observed = await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(args)).Throws<InvalidOperationException>();
        await Assert.That(observed).IsSameReferenceAs(failure);
    }

    /// <summary>Verifies the challenge cancellation token is used for the response send.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task HandleEnhancedAuthenticationAsync_ForwardsChallengeCancellationAsync(CancellationToken cancellationToken)
    {
        using var adapter = Substitute.For<IMqttChannelAdapter>();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await cancellation.CancelAsync();
        _ = adapter.SendPacketAsync(Arg.Any<MqttPacket>(), cancellation.Token).Returns(Task.FromCanceled(cancellation.Token));
        var handler = new ScriptedEnhancedAuthenticationHandler([new() { Data = "response" }]);
        var args = new MqttEnhancedAuthenticationEventArgs(new() { ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication }, adapter, cancellation.Token);
        await Assert.That(() => handler.HandleEnhancedAuthenticationAsync(args)).Throws<OperationCanceledException>();
    }
}
