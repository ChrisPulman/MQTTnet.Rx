// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI.Primitives;
using DeliveryChannel = System.Threading.Channels.Channel<MQTTnet.Rx.Client.ReceivedMqttMessage>;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies real broker events update message, topic, dashboard, and log state.</summary>
public sealed partial class MainWindowViewModelTests
{
    /// <summary>Stores the broker operation deadline in seconds.</summary>
    private const int IntegrationTimeoutSeconds = 15;

    /// <summary>Stores the configured retained message and issue bound.</summary>
    private const int MessageLimit = 500;

    /// <summary>Stores the configured retained log bound.</summary>
    private const int LogLimit = 800;

    /// <summary>Stores the broker and client observation count for one publication.</summary>
    private const int ObservationPairCount = 2;

    /// <summary>Stores the observation count after two matching publications.</summary>
    private const int TwoPublishObservationCount = 4;

    /// <summary>Stores the initial gauge telemetry.</summary>
    private const int InitialTelemetryValue = 41;

    /// <summary>Stores the diagnostic payload topic.</summary>
    private const string InvalidPayloadTopic = "plant/invalid";

    /// <summary>Checks a concurrent MQTT command reports contention without taking over the pending operation.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PendingConnectRejectsConcurrentOperationAndReleasesBusyStateAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(IntegrationTimeoutSeconds));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var pending = new ConcurrentQueue<Action>();
        await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, pending.Enqueue, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), LayoutFileName));
        model.Connection.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        model.Connection.Host = IPAddress.Loopback.ToString();
        var command = model.ConnectCommand.Execute().FirstAsync();
        using var accepted = await listener.AcceptTcpClientAsync(timeout.Token);
        DrainUi(pending);
        await Assert.That(model.IsBusy).IsTrue();
        var statusBeforeContention = model.Status;
        await model.SubscribeCommand.Execute().FirstAsync();
        await Assert.That(model.IsBusy).IsTrue();
        await Assert.That(model.Status).IsEqualTo(statusBeforeContention);
        await Assert.That(model.LogEntries[0].Message).IsEqualTo("Another MQTT operation is already running.");
        accepted.Dispose();
        await command.WaitAsync(timeout.Token);
        DrainUi(pending);
        await Assert.That(model.IsBusy).IsFalse();
        await Assert.That(model.Status).IsEqualTo("Connect failed");
        await model.DisconnectCommand.Execute().FirstAsync();
        DrainUi(pending);
        await Assert.That(model.Status).IsEqualTo("Disconnect complete");
    }

    /// <summary>Checks search filters affect the message list while topics and dashboard continue observing traffic.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task BrokerTrafficUpdatesDashboardAndTopicsBeforeApplyingSearchAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var pending = new ConcurrentQueue<Action>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(IntegrationTimeoutSeconds));
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        await using var model = new MainWindowViewModel(service, TimeProvider.System, pending.Enqueue, Path.Combine(directory, LayoutFileName));
        var delivered = ObserveClientMessages(service);
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            model.Connection.Port = port;
            model.Connection.StartEmbeddedServer = true;
            model.Connection.EmbeddedServerPort = port;
            model.Subscription.TopicFilter = "plant/#";
            model.Publisher.Topic = ObservedTopic;
            await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
            await model.ConnectCommand.Execute().FirstAsync();
            DrainUi(pending);
            await Assert.That(model.IsConnected).IsTrue();
            await Assert.That(model.Status).IsEqualTo("Connect complete");
            await AssertSearchBehaviorAsync(model, service, delivered.Reader, pending, timeout.Token);
            model.Publisher.Topic = "plant/connected";
            model.SelectedMessage = null;
            await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
            DrainUi(pending);
            await Assert.That(model.Status).IsEqualTo("Subscribe dashboard complete");
            await model.SubscribeCommand.Execute().FirstAsync();
            DrainUi(pending);
            await Assert.That(model.Status).IsEqualTo("Subscribe complete");
            await model.UnsubscribeCommand.Execute().FirstAsync();
            DrainUi(pending);
            await Assert.That(model.Status).IsEqualTo("Unsubscribe complete");
            await model.DisconnectCommand.Execute().FirstAsync();
            DrainUi(pending);
            await Assert.That(model.IsConnected).IsFalse();
            await Assert.That(model.Status).IsEqualTo("Disconnect complete");
            await model.ClearMessagesCommand.Execute().FirstAsync();
            await Assert.That(model.Messages).IsEmpty();
            await Assert.That(model.TopicIssues).IsEmpty();
            await Assert.That(model.Topics[0].Children).IsEmpty();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>Checks incoming broker traffic evicts oldest entries from the retained lists.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task BrokerEventsBoundMessagesIssuesAndLogsAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(IntegrationTimeoutSeconds));
        var pending = new ConcurrentQueue<Action>();
        await using var service = new MqttToolkitSessionService(TimeProvider.System);
        await using var model = new MainWindowViewModel(service, TimeProvider.System, pending.Enqueue, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), LayoutFileName));
        var oldest = CreateMessage("old/topic", "old", PayloadFormat.Utf8Text);
        for (var index = 0; index < MessageLimit; index++)
        {
            model.Messages.Add(oldest);
            model.TopicIssues.Add(new(DateTimeOffset.UnixEpoch, "old/topic", TopicIssueKind.InvalidPayload, "old"));
        }

        model.LogEntries.Clear();
        for (var index = 0; index < LogLimit; index++)
        {
            model.LogEntries.Add(new(DateTimeOffset.UnixEpoch, "Info", "old", "old"));
        }

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var connection = new ConnectionOptionsViewModel { Port = port };
        var delivered = ObserveClientMessages(service);
        var issues = System.Threading.Channels.Channel.CreateUnbounded<TopicIssue>();
        service.TopicIssueDetected += (_, issue) => _ = issues.Writer.TryWrite(issue);
        await service.StartEmbeddedServerAsync(port, timeout.Token);
        await service.ConnectAsync(connection.BuildClientOptions(), timeout.Token);
        await service.SubscribeAsync(new() { TopicFilter = "plant/#" }, timeout.Token);
        var invalid = new MqttApplicationMessageBuilder()
            .WithTopic(InvalidPayloadTopic)
            .WithPayload("invalid JSON")
            .WithContentType("application/json")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        await service.PublishAsync(invalid, timeout.Token);
        _ = await delivered.Reader.ReadAsync(timeout.Token);
        _ = await issues.Reader.ReadAsync(timeout.Token);
        _ = await issues.Reader.ReadAsync(timeout.Token);
        await service.DisconnectAsync(timeout.Token);
        DrainUi(pending);
        await Assert.That(model.Messages).Count().IsEqualTo(MessageLimit);
        await Assert.That(model.Messages[0].Topic).IsEqualTo(InvalidPayloadTopic);
        await Assert.That(model.TopicIssues).Count().IsEqualTo(MessageLimit);
        await Assert.That(model.TopicIssues[0].Topic).IsEqualTo(InvalidPayloadTopic);
        await Assert.That(model.LogEntries).Count().IsEqualTo(LogLimit);
        await Assert.That(model.LogEntries[0].Source).IsNotEqualTo("old");
    }

    /// <summary>Checks unmatched traffic still updates topics and dashboard while topic and payload searches retain matching messages.</summary>
    /// <param name="model">The active Toolkit model.</param>
    /// <param name="service">The live MQTT session.</param>
    /// <param name="delivered">The client delivery reader.</param>
    /// <param name="pending">The queued UI callbacks.</param>
    /// <param name="cancellationToken">The integration timeout.</param>
    /// <returns>The asynchronous search assertions.</returns>
    private static async Task AssertSearchBehaviorAsync(
        MainWindowViewModel model,
        MqttToolkitSessionService service,
        ChannelReader<ReceivedMqttMessage> delivered,
        ConcurrentQueue<Action> pending,
        CancellationToken cancellationToken)
    {
        model.MessageSearch = "does not match";
        await PublishAndDrainAsync(service, delivered, pending, ObservedTopic, "41", cancellationToken);
        await Assert.That(model.Messages).IsEmpty();
        await Assert.That(model.SelectedMessage).IsNull();
        await Assert.That(model.DashboardTiles[0].NumericValue).IsEqualTo(InitialTelemetryValue);
        var leaf = model.Topics[0].Children[0].Children[0];
        await Assert.That(leaf.FullTopic).IsEqualTo(ObservedTopic);
        await Assert.That(leaf.MessageCount).IsEqualTo(ObservationPairCount);
        model.MessageSearch = "VALUE";
        await PublishAndDrainAsync(service, delivered, pending, ObservedTopic, "42", cancellationToken);
        await Assert.That(model.Messages).Count().IsEqualTo(ObservationPairCount);
        await Assert.That(model.SelectedMessage?.Payload).IsEqualTo("42");
        model.MessageSearch = "HELLO";
        await PublishAndDrainAsync(service, delivered, pending, "plant/other", "hello world", cancellationToken);
        await Assert.That(model.Messages).Count().IsEqualTo(TwoPublishObservationCount);
        await Assert.That(model.SelectedMessage?.Payload).IsEqualTo("42");
        await Assert.That(model.Topics[0].Children[0].Children).Count().IsEqualTo(ObservationPairCount);
    }

    /// <summary>Drains UI work after a completed service event.</summary>
    /// <param name="pending">The queued UI callbacks.</param>
    private static void DrainUi(ConcurrentQueue<Action> pending)
    {
        while (pending.TryDequeue(out var action))
        {
            action();
        }
    }

    /// <summary>Publishes through the real broker, awaits delivery, and applies queued state.</summary>
    /// <param name="service">The live MQTT session.</param>
    /// <param name="delivered">The client delivery reader.</param>
    /// <param name="pending">The UI callback queue.</param>
    /// <param name="topic">The published topic.</param>
    /// <param name="payload">The published text.</param>
    /// <param name="cancellationToken">The integration timeout.</param>
    /// <returns>The asynchronous publish and dispatch.</returns>
    private static async Task PublishAndDrainAsync(
        MqttToolkitSessionService service,
        ChannelReader<ReceivedMqttMessage> delivered,
        ConcurrentQueue<Action> pending,
        string topic,
        string payload,
        CancellationToken cancellationToken)
    {
        var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
        await service.PublishAsync(message, cancellationToken);
        _ = await delivered.ReadAsync(cancellationToken);
        DrainUi(pending);
    }

    /// <summary>Creates a reader receiving each completed client delivery.</summary>
    /// <param name="service">The service whose client deliveries are observed.</param>
    /// <returns>The delivery channel.</returns>
    private static DeliveryChannel ObserveClientMessages(MqttToolkitSessionService service)
    {
        var delivered = System.Threading.Channels.Channel.CreateUnbounded<ReceivedMqttMessage>();
        service.MessageReceived += (_, message) =>
        {
            if (message.Source == "Client received")
            {
                _ = delivered.Writer.TryWrite(message);
            }
        };
        return delivered;
    }
}
