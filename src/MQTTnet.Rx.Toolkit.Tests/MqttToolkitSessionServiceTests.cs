// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies MQTT Toolkit session lifecycle behavior.</summary>
public sealed class MqttToolkitSessionServiceTests
{
    /// <summary>Stores the test MQTT client identifier.</summary>
    private const string ClientId = "toolkit-dispose-race";

    /// <summary>Stores the MQTT connect timeout used by the disposal race test.</summary>
    private const int ConnectTimeoutSeconds = 30;

    /// <summary>Stores the bounded test timeout in seconds.</summary>
    private const int TestTimeoutSeconds = 5;

    /// <summary>Stores the number of connections exercised by subscription reuse tests.</summary>
    private const int ConnectionCycles = 3;

    /// <summary>Stores the expected observations from the broker and subscribed client.</summary>
    private const int ObservationsPerPublish = 2;

    /// <summary>Stores the topic used to verify message subscriptions after reconnecting.</summary>
    private const string ReconnectTopic = "toolkit/reconnect/value";

    /// <summary>Verifies client and broker subscriptions remain usable across repeated connection lifetimes.</summary>
    /// <param name="restartBroker">Whether to also replace the embedded broker between connections.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ReconnectPreservesClientAndBrokerMessageSubscriptionsAsync(bool restartBroker)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        using var connection = new ConnectionOptionsViewModel
        {
            ClientId = "toolkit-reconnect",
            Host = IPAddress.Loopback.ToString(),
            Port = port,
        };
        var subscription = new SubscriptionViewModel { TopicFilter = ReconnectTopic };

        for (var cycle = 0; cycle < ConnectionCycles; cycle++)
        {
            var received = new ConcurrentQueue<ReceivedMqttMessage>();
            var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<ReceivedMqttMessage> handler = (_, message) =>
            {
                received.Enqueue(message);
                if (message.Source == "Client received")
                {
                    _ = delivered.TrySetResult();
                }
            };
            service.MessageReceived += handler;
            try
            {
                await service.StartEmbeddedServerAsync(port, timeout.Token);
                await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
                await service.SubscribeAsync(subscription, timeout.Token);
                var payload = $"value-{cycle}";
                await service.PublishAsync(
                    new MqttApplicationMessageBuilder()
                        .WithTopic(ReconnectTopic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build(),
                    timeout.Token);
                await delivered.Task.WaitAsync(timeout.Token);
                await service.DisconnectAsync(timeout.Token);
                if (restartBroker)
                {
                    await service.StopEmbeddedServerAsync(timeout.Token);
                }

                await AssertMessageObservationsAsync(received.ToArray(), payload);
            }
            finally
            {
                service.MessageReceived -= handler;
            }
        }

        await service.StopEmbeddedServerAsync(timeout.Token);
    }

    /// <summary>Verifies disposing during a pending connect does not surface disposal races.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DisposeAsyncDuringPendingConnectDoesNotThrowObjectDisposedExceptionAsync()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        using var connection = new ConnectionOptionsViewModel
        {
            ClientId = ClientId,
            Host = IPAddress.Loopback.ToString(),
            Port = ((IPEndPoint)listener.LocalEndpoint).Port,
            TimeoutSeconds = ConnectTimeoutSeconds,
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TestTimeoutSeconds));
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        listener.Start();
        connection.Port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var acceptTask = listener.AcceptTcpClientAsync(timeout.Token).AsTask();
        var connectTask = service.ConnectAsync(connection.BuildClientOptions(), CancellationToken.None);
        using var acceptedClient = await acceptTask.WaitAsync(timeout.Token);
        await service.DisposeAsync().AsTask().WaitAsync(timeout.Token);
        acceptedClient.Dispose();
        var observedException = await GetObservedExceptionAsync(connectTask);

        await Assert.That(observedException is ObjectDisposedException).IsFalse();
    }

    /// <summary>Verifies that one publish was observed once by both the broker and subscribed client.</summary>
    /// <param name="messages">The observations collected during the connection.</param>
    /// <param name="payload">The expected payload.</param>
    /// <returns>A task representing the asynchronous assertions.</returns>
    private static async Task AssertMessageObservationsAsync(ReceivedMqttMessage[] messages, string payload)
    {
        await Assert.That(messages).Count().IsEqualTo(ObservationsPerPublish);
        await Assert.That(messages[0].Source).IsEqualTo("Broker ingress");
        await Assert.That(messages[1].Source).IsEqualTo("Client received");
        foreach (var message in messages)
        {
            await Assert.That(message.Topic).IsEqualTo(ReconnectTopic);
            await Assert.That(message.Payload).IsEqualTo(payload);
        }
    }

    /// <summary>Gets the exception observed from a task, or <see langword="null"/> when it completed successfully.</summary>
    /// <param name="task">The task to observe.</param>
    /// <returns>The observed exception, if any.</returns>
    private static async Task<Exception?> GetObservedExceptionAsync(Task task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
