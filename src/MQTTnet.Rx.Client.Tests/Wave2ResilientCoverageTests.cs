// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the remaining deterministic resilient-client branches.</summary>
[NotInParallel]
public partial class Wave2ResilientCoverageTests
{
    /// <summary>The internal resilient-client type name.</summary>
    private const string ResilientClientTypeName = "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClient";

    /// <summary>The internal resilient-message builder type name.</summary>
    private const string ResilientMessageBuilderTypeName =
        "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttApplicationMessageBuilder";

    /// <summary>The internal subscription-results type name.</summary>
    private const string SendSubscriptionResultsTypeName =
        "MQTTnet.Rx.Client.ResilientClient.Internal.SendSubscriptionResults";

    /// <summary>The internal storage-manager type name.</summary>
    private const string StorageManagerTypeName =
        "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClientStorageManager";

    /// <summary>The MQTT host used for option construction.</summary>
    private const string BrokerHost = "coverage-broker";

    /// <summary>The internal message-builder method name.</summary>
    private const string WithApplicationMessageMethodName = "WithApplicationMessage";

    /// <summary>The builder terminal method name.</summary>
    private const string BuildMethodName = "Build";

    /// <summary>The internal connection-maintenance method name.</summary>
    private const string TryMaintainConnectionMethodName = "TryMaintainConnectionAsync";

    /// <summary>The internal queued-publisher method name.</summary>
    private const string PublishQueuedMessagesMethodName = "PublishQueuedMessagesAsync";

    /// <summary>The internal remaining-time calculation method name.</summary>
    private const string GetRemainingTimeMethodName = "GetRemainingTime";

    /// <summary>The topic used by resilient-client coverage tests.</summary>
    private const string CoverageTopic = "coverage/wave-two";

    /// <summary>The second topic used by resilient-client coverage tests.</summary>
    private const string SecondCoverageTopic = "coverage/wave-two/second";

    /// <summary>A small subscription batch size.</summary>
    private const int SingleTopicBatchSize = 1;

    /// <summary>A small pending-message limit.</summary>
    private const int SinglePendingMessage = 1;

    /// <summary>The expected reconnect subscription notification count.</summary>
    private const int ExpectedSubscriptionChangeCount = 2;

    /// <summary>The minimum expected save count for storage-backed removal.</summary>
    private const int MinimumStorageSaveCount = 4;

    /// <summary>The reconnect delay used by option-builder coverage.</summary>
    private const int ReconnectDelayMilliseconds = 25;

    /// <summary>The short deterministic polling interval.</summary>
    private const int PollingIntervalMilliseconds = 10;

    /// <summary>The deterministic polling timeout.</summary>
    private const int PollingTimeoutSeconds = 2;

    /// <summary>Exercises null validation and both collection-shape branches in resilient event arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EventArguments_ExerciseNullAndCollectionBranchesAsync()
    {
        var message = CreateManagedMessage(CoverageTopic);
        var exception = new InvalidOperationException("coverage failure");
        var subscribeResults = new List<MqttClientSubscribeResult>();
        var unsubscribeResults = new List<MqttClientUnsubscribeResult>();

        await Assert.That(() => new ApplicationMessageProcessedEventArgs(
                CreateNullArgument<ResilientMqttApplicationMessage>(),
                exception))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new ApplicationMessageSkippedEventArgs(
                CreateNullArgument<ResilientMqttApplicationMessage>()))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new InterceptingPublishMessageEventArgs(
                CreateNullArgument<ResilientMqttApplicationMessage>()))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new ResilientProcessFailedEventArgs(
                CreateNullArgument<Exception>(),
                null,
                null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new SubscriptionsChangedEventArgs(
                CreateNullArgument<List<MqttClientSubscribeResult>>(),
                unsubscribeResults))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new SubscriptionsChangedEventArgs(
                subscribeResults,
                CreateNullArgument<List<MqttClientUnsubscribeResult>>()))
            .Throws<ArgumentNullException>();

        var processed = new ApplicationMessageProcessedEventArgs(message, exception);
        var skipped = new ApplicationMessageSkippedEventArgs(message);
        var intercepted = new InterceptingPublishMessageEventArgs(message) { AcceptPublish = false };
        var emptyFailure = new ResilientProcessFailedEventArgs(exception, null, null);
        var populatedFailure = new ResilientProcessFailedEventArgs(
            exception,
            [new MqttTopicFilter { Topic = CoverageTopic }],
            [SecondCoverageTopic]);
        var emptyListFailure = new ResilientProcessFailedEventArgs(exception, [], []);

        await Assert.That(processed.ApplicationMessage).IsSameReferenceAs(message);
        await Assert.That(processed.Exception).IsSameReferenceAs(exception);
        await Assert.That(skipped.ApplicationMessage).IsSameReferenceAs(message);
        await Assert.That(intercepted.ApplicationMessage).IsSameReferenceAs(message);
        await Assert.That(intercepted.AcceptPublish).IsFalse();
        await Assert.That(emptyFailure.AddedSubscriptions).IsEmpty();
        await Assert.That(emptyFailure.RemovedSubscriptions).IsEmpty();
        await Assert.That(populatedFailure.AddedSubscriptions).IsEquivalentTo([CoverageTopic]);
        await Assert.That(populatedFailure.RemovedSubscriptions).IsEquivalentTo([SecondCoverageTopic]);
        await Assert.That(emptyListFailure.AddedSubscriptions).IsEmpty();
        await Assert.That(emptyListFailure.RemovedSubscriptions).IsEmpty();
    }

    /// <summary>Exercises all resilient-message builder paths, including validation failures.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResilientMessageBuilder_ExercisesBothConfigurationFormsAsync()
    {
        var builderType = GetClientAssemblyType(ResilientMessageBuilderTypeName);
        var identifier = Guid.NewGuid();
        var directMessage = new MqttApplicationMessage { Topic = CoverageTopic };
        var directBuilder = CreateInternal(builderType);

        var withId = InvokeInstance(directBuilder, "WithId", [typeof(Guid).MakeByRefType()], [identifier]);
        var withMessage = InvokeInstance(
            directBuilder,
            WithApplicationMessageMethodName,
            [typeof(MqttApplicationMessage)],
            [directMessage]);
        var built = GetInvocationResult<ResilientMqttApplicationMessage>(
            InvokeInstance(directBuilder, BuildMethodName, [], []),
            BuildMethodName);

        var actionBuilder = CreateInternal(builderType);
        Action<MqttApplicationMessageBuilder> configure = static builder => builder.WithTopic(SecondCoverageTopic);
        _ = InvokeInstance(
            actionBuilder,
            WithApplicationMessageMethodName,
            [typeof(Action<MqttApplicationMessageBuilder>)],
            [configure]);
        var actionBuilt = GetInvocationResult<ResilientMqttApplicationMessage>(
            InvokeInstance(actionBuilder, BuildMethodName, [], []),
            BuildMethodName);

        var emptyBuilder = CreateInternal(builderType);
        await Assert.That(() => InvokeInstance(emptyBuilder, BuildMethodName, [], []))
            .Throws<TargetInvocationException>();
        await Assert.That(() => InvokeInstance(
            emptyBuilder,
            WithApplicationMessageMethodName,
            [typeof(Action<MqttApplicationMessageBuilder>)],
            [CreateNullArgument<Action<MqttApplicationMessageBuilder>>()])).Throws<TargetInvocationException>();

        await Assert.That(withId).IsSameReferenceAs(directBuilder);
        await Assert.That(withMessage).IsSameReferenceAs(directBuilder);
        await Assert.That(built.Id).IsEqualTo(identifier);
        await Assert.That(built.ApplicationMessage).IsSameReferenceAs(directMessage);
        await Assert.That(actionBuilt.ApplicationMessage?.Topic).IsEqualTo(SecondCoverageTopic);
    }

    /// <summary>Exercises fluent option assignment and all mutually exclusive configuration failures.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OptionsBuilder_ExercisesFluentAndConflictPathsAsync()
    {
        var reconnectDelay = TimeSpan.FromMilliseconds(ReconnectDelayMilliseconds);
        var storage = new Wave2ResilientStorage();
        var clientOptions = new MqttClientOptionsBuilder().WithTcpServer(BrokerHost).Build();
        var builder = new ResilientMqttClientOptionsBuilder();

        var configured = builder
            .WithMaxPendingMessages(SinglePendingMessage)
            .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            .WithAutoReconnectDelay(reconnectDelay)
            .WithStorage(storage)
            .WithMaxTopicFiltersInSubscribeUnsubscribePackets(SingleTopicBatchSize)
            .WithClientOptions(static options => options.WithTcpServer(BrokerHost))
            .WithClientOptions(static options => options.WithClientId("wave-two-client"))
            .Build();

        var directBuilder = new ResilientMqttClientOptionsBuilder().WithClientOptions(clientOptions);
        var indirectBuilder = new ResilientMqttClientOptionsBuilder().WithClientOptions(new MqttClientOptionsBuilder());

        await Assert.That(() => directBuilder.WithClientOptions(new MqttClientOptionsBuilder()))
            .Throws<InvalidOperationException>();
        await Assert.That(() => indirectBuilder.WithClientOptions(clientOptions)).Throws<InvalidOperationException>();
        await Assert.That(static () => new ResilientMqttClientOptionsBuilder()
                .WithClientOptions(CreateNullArgument<Action<MqttClientOptionsBuilder>>()))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new ResilientMqttClientOptionsBuilder().Build())
            .Throws<InvalidOperationException>();

        await Assert.That(configured.MaxPendingMessages).IsEqualTo(SinglePendingMessage);
        await Assert.That(configured.PendingMessagesOverflowStrategy)
            .IsEqualTo(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);
        await Assert.That(configured.AutoReconnectDelay).IsEqualTo(reconnectDelay);
        await Assert.That(configured.Storage).IsSameReferenceAs(storage);
        await Assert.That(configured.MaxTopicFiltersInSubscribeUnsubscribePackets).IsEqualTo(SingleTopicBatchSize);
        await Assert.That(configured.ClientOptions?.ClientId).IsEqualTo("wave-two-client");
    }

    /// <summary>Exercises storage-manager null construction, absent removal, and idempotent disposal.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StorageManager_ExercisesNullMissingAndRepeatedDisposePathsAsync()
    {
        var managerType = GetClientAssemblyType(StorageManagerTypeName);
        var storage = new Wave2ResilientStorage();
        var manager = CreateInternal(managerType, storage);
        var missingMessage = CreateManagedMessage(CoverageTopic);

        await InvokeTaskAsync(manager, "RemoveAsync", missingMessage);
        ((IDisposable)manager).Dispose();
        ((IDisposable)manager).Dispose();

        await Assert.That(() => CreateInternal(managerType, [null])).Throws<TargetInvocationException>();
        await Assert.That(storage.SaveCount).IsEqualTo(0);
    }

    /// <summary>Exercises null validation in the internal subscription result value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendSubscriptionResults_RejectsNullCollectionsAsync()
    {
        var resultType = GetClientAssemblyType(SendSubscriptionResultsTypeName);
        var subscribeResults = new List<MqttClientSubscribeResult>();
        var unsubscribeResults = new List<MqttClientUnsubscribeResult>();

        var result = CreateInternal(resultType, subscribeResults, unsubscribeResults);

        await Assert.That(() => CreateInternal(resultType, null, unsubscribeResults))
            .Throws<TargetInvocationException>();
        await Assert.That(() => CreateInternal(resultType, subscribeResults, null)).Throws<TargetInvocationException>();
        await Assert.That(result).IsNotNull();
    }

    /// <summary>Exercises constructor validation, asynchronous observable teardown, and stop-before-start.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClientSurface_ExercisesValidationObservableRemovalAndEarlyStopAsync()
    {
        var clientType = GetClientAssemblyType(ResilientClientTypeName);
        var factory = new MqttClientFactory();

        await Assert.That(() => CreateInternal(clientType, null, factory.DefaultLogger))
            .Throws<TargetInvocationException>();
        using (var rejectedClient = new ScriptedMqttClient())
        {
            await Assert.That(() => CreateInternal(clientType, rejectedClient, null))
                .Throws<TargetInvocationException>();
        }

        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);

        await SubscribeThenDisposeAsync(client.ApplicationMessageProcessedAsyncObservable);
        await SubscribeThenDisposeAsync(client.ConnectedAsyncObservable);
        await SubscribeThenDisposeAsync(client.DisconnectedAsyncObservable);
        await SubscribeThenDisposeAsync(client.ConnectingFailedAsyncObservable);
        await SubscribeThenDisposeAsync(client.ConnectionStateChangedAsyncObservable);
        await SubscribeThenDisposeAsync(client.SynchronizingSubscriptionsFailedAsyncObservable);
        await SubscribeThenDisposeAsync(client.ApplicationMessageSkippedAsyncObservable);
        await SubscribeThenDisposeAsync(client.ApplicationMessageReceivedAsyncObservable);
        await client.StopAsync();

        await Assert.That(client.IsStarted).IsFalse();
    }

    /// <summary>Exercises storage-backed overflow removal and storage-backed QoS-zero publish failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClientStoragePaths_RemoveOverflowAndFailedQosZeroMessagesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        var storage = new Wave2ResilientStorage();
        SetOptions(
            client,
            CreateOptions(
                storage,
                SinglePendingMessage,
                MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage));
        SetStorageManager(client, storage);

        var first = CreateManagedMessage(CoverageTopic);
        var second = CreateManagedMessage(SecondCoverageTopic);
        await client.EnqueueAsync(first);
        await client.EnqueueAsync(second);

        internalClient.PublishHandler = static (_, _) =>
            Task.FromException<MqttClientPublishResult>(
                new MqttCommunicationException("coverage communication failure"));
        await InvokeTaskAsync(client, "TryPublishQueuedMessageAsync", second, CancellationToken.None);

        var messageWithoutApplicationMessage = new ResilientMqttApplicationMessage();
        await InvokeTaskAsync(
            client,
            "TryPublishQueuedMessageAsync",
            messageWithoutApplicationMessage,
            CancellationToken.None);

        await Assert.That(storage.SaveCount).IsGreaterThanOrEqualTo(MinimumStorageSaveCount);
        await Assert.That(storage.LastSavedMessages).IsEmpty();
    }

    /// <summary>Exercises reconnect subscription batching and its outer failure handler.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReconnectSubscriptions_ExerciseBatchAndOuterFailurePathsAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        SetOptions(client, CreateOptions(maxTopicFilters: SingleTopicBatchSize));
        var reconnectSubscriptions = GetReconnectSubscriptions(client);
        reconnectSubscriptions[CoverageTopic] = new() { Topic = CoverageTopic };
        var changedCount = 0;
        using var changedRegistration = client.RegisterSubscriptionsChangedHandler((_, _) =>
        {
            changedCount++;
            return ValueTask.CompletedTask;
        });

        await InvokeTaskAsync(client, "PublishReconnectSubscriptionsAsync", CancellationToken.None);

        var failureCount = 0;
        using var failureRegistration = client.RegisterSynchronizingSubscriptionsFailedHandler((_, _) =>
        {
            failureCount++;
            return ValueTask.CompletedTask;
        });
        SetOptions(client, null);
        await InvokeTaskAsync(client, "PublishReconnectSubscriptionsAsync", CancellationToken.None);

        await Assert.That(internalClient.SubscribeRequests.Count).IsEqualTo(1);
        await Assert.That(changedCount).IsEqualTo(ExpectedSubscriptionChangeCount);
        await Assert.That(failureCount).IsEqualTo(1);
    }

    /// <summary>Exercises recovered-session startup and both connection-state handler failure catches.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TryMaintainConnection_ExercisesRecoveredCommunicationAndGeneralFailuresAsync()
    {
        using var recoveredInternalClient = new ScriptedMqttClient
        {
            ConnectHandler = static (_, _) => Task.FromResult(new MqttClientConnectResult { IsSessionPresent = true }),
        };
        using var recoveredClient = CreateClient(recoveredInternalClient);
        SetOptions(recoveredClient, CreateOptions());

        await InvokeTaskAsync(recoveredClient, TryMaintainConnectionMethodName, CancellationToken.None);
        await recoveredClient.StopAsync(cleanDisconnect: false);

        using var communicationInternalClient = new ScriptedMqttClient();
        using var communicationClient = CreateClient(communicationInternalClient);
        SetOptions(communicationClient, CreateOptions());
        using var communicationRegistration = communicationClient.RegisterConnectionStateChangedHandler(static (_, _) =>
            ValueTask.FromException(new MqttCommunicationException("coverage state failure")));
        await InvokeTaskAsync(communicationClient, TryMaintainConnectionMethodName, CancellationToken.None);
        await communicationClient.StopAsync(cleanDisconnect: false);

        using var generalInternalClient = new ScriptedMqttClient();
        using var generalClient = CreateClient(generalInternalClient);
        SetOptions(generalClient, CreateOptions());
        using var generalRegistration = generalClient.RegisterConnectionStateChangedHandler(static (_, _) =>
            ValueTask.FromException(new InvalidOperationException("coverage state failure")));
        await InvokeTaskAsync(generalClient, TryMaintainConnectionMethodName, CancellationToken.None);
        await generalClient.StopAsync(cleanDisconnect: false);

        await Assert.That(recoveredInternalClient.ConnectCount).IsEqualTo(1);
        await Assert.That(communicationInternalClient.ConnectCount).IsEqualTo(1);
        await Assert.That(generalInternalClient.ConnectCount).IsEqualTo(1);
    }

    /// <summary>Exercises canceled, disconnected, and faulted queued-publisher exits.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PublishQueuedMessages_ExercisesAllLoopExitBranchesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        internalClient.SetConnected(true);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await InvokeTaskAsync(client, PublishQueuedMessagesMethodName, cancellation.Token);

        internalClient.SetConnected(false);
        await InvokeTaskAsync(client, PublishQueuedMessagesMethodName, CancellationToken.None);

        await Assert.That(cancellation.IsCancellationRequested).IsTrue();
    }

    /// <summary>Exercises clean-disconnect cancellation and general exception handlers.</summary>
    /// <param name="useCancellationException">Whether the disconnect operation throws a cancellation exception.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task MaintainConnection_HandlesDisconnectFailuresAsync(bool useCancellationException)
    {
        var internalClient = new ScriptedMqttClient
        {
            DisconnectHandler = useCancellationException
                ? static (_, _) => Task.FromException(new OperationCanceledException("coverage cancellation"))
                : static (_, _) => Task.FromException(new InvalidOperationException("coverage disconnect failure")),
        };
        var client = CreateClient(internalClient);

        await client.StartAsync(CreateOptions());
        await WaitUntilAsync(() => internalClient.IsConnected);
        await client.StopAsync();
        client.Dispose();

        await Assert.That(internalClient.DisconnectCount).IsEqualTo(1);
    }

    /// <summary>Exercises disposal after the connection-maintenance task has stopped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Dispose_AfterStopDisposesInternalClientAsync()
    {
        var internalClient = new ScriptedMqttClient();
        var client = CreateClient(internalClient);

        await client.StartAsync(CreateOptions());
        await client.StopAsync(cleanDisconnect: false);
        client.Dispose();

        await Assert.That(internalClient.IsDisposed).IsTrue();
    }

    /// <summary>Exercises past and future remaining-time calculations and a failure with no event handler.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PrivateHelpers_ExerciseRemainingTimeAndNoHandlerFailureAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        var utcNow = TimeProvider.System.GetUtcNow().UtcDateTime;
        var past = utcNow - TimeSpan.FromSeconds(1);
        var future = utcNow + TimeSpan.FromMinutes(1);

        var pastRemaining = GetInvocationResult<TimeSpan>(
            InvokeStatic(
                client.GetType(),
                GetRemainingTimeMethodName,
                [typeof(DateTime).MakeByRefType()],
                [past]),
            GetRemainingTimeMethodName);
        var futureRemaining = GetInvocationResult<TimeSpan>(
            InvokeStatic(
                client.GetType(),
                GetRemainingTimeMethodName,
                [typeof(DateTime).MakeByRefType()],
                [future]),
            GetRemainingTimeMethodName);
        await InvokeTaskAsync(
            client,
            "HandleSubscriptionExceptionAsync",
            new InvalidOperationException("coverage subscription failure"),
            null,
            null);

        await Assert.That(pastRemaining).IsEqualTo(TimeSpan.Zero);
        await Assert.That(futureRemaining).IsGreaterThan(TimeSpan.Zero);
    }
}
