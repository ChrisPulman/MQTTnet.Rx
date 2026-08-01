// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies the reusable live MQTT fixture against real MQTTnet network transports.</summary>
public class LiveMqttBrokerContractTests
{
    /// <summary>The exact topic used by the live broker contract.</summary>
    private const string Topic = "tests/live/fixture";

    /// <summary>The UTF-8 payload used by the live broker contract.</summary>
    private const string Payload = "mqttnet-rx-live-payload";

    /// <summary>The maximum time allowed for receiving a live message.</summary>
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Proves that two real clients can connect, subscribe, publish, receive, and tear down.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LiveBroker_TwoRealClientsExchangeMessageAndTearDownAsync()
    {
        var broker = await LiveMqttBroker.StartAsync();
        try
        {
            var connectResults = await broker.ConnectClientsAsync();
            await using var subscription = await broker.SubscribeProbeAsync(Topic);

            var bridge = await broker.Bridge.FirstAsync(MessageTimeout);
            var probe = await broker.Probe.FirstAsync(MessageTimeout);
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(Topic)
                .WithPayload(Payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            var publishResult = await bridge.PublishAsync(applicationMessage, CancellationToken.None);
            var received = await subscription.MessageReceived.WaitAsync(MessageTimeout);

            await Assert.That(broker.Port).IsGreaterThan(0);
            await Assert.That(broker.IsStarted).IsTrue();
            await Assert.That(broker.IsDisposed).IsFalse();
            await Assert.That(connectResults.Bridge.ResultCode).IsEqualTo(MqttClientConnectResultCode.Success);
            await Assert.That(connectResults.Probe.ResultCode).IsEqualTo(MqttClientConnectResultCode.Success);
            await Assert.That(bridge).IsSameReferenceAs(broker.BridgeClient);
            await Assert.That(probe).IsSameReferenceAs(broker.ProbeClient);
            await Assert.That(bridge.IsConnected).IsTrue();
            await Assert.That(probe.IsConnected).IsTrue();
            await Assert.That(subscription.SubscriptionReady.IsCompletedSuccessfully).IsTrue();
            await Assert.That(subscription.SubscribeResult.Items).Count().IsEqualTo(1);
            await Assert.That(subscription.SubscribeResultCode)
                .IsEqualTo(MqttClientSubscribeResultCode.GrantedQoS1);
            await Assert.That(publishResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
            await Assert.That(received.Topic).IsEqualTo(Topic);
            await Assert.That(Encoding.UTF8.GetString(received.Payload)).IsEqualTo(Payload);
            await Assert.That(((MqttClientTcpOptions)bridge.Options.ChannelOptions).RemoteEndpoint)
                .IsEqualTo(new DnsEndPoint(IPAddress.Loopback.ToString(), broker.Port));
        }
        finally
        {
            await broker.DisposeAsync();
        }

        await Assert.That(broker.IsDisposed).IsTrue();
        await Assert.That(broker.IsStarted).IsFalse();
        await Assert.That(broker.BridgeClient.IsConnected).IsFalse();
        await Assert.That(broker.ProbeClient.IsConnected).IsFalse();
        await Assert.That(broker.TeardownException).IsNull();

        await broker.DisposeAsync();
    }
}
