// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the resilient client's deterministic state-machine and queue paths.</summary>
[NotInParallel]
public class ResilientMqttClientCoverageTests
{
    /// <summary>Valid broker host used to build client options without opening a network connection.</summary>
    private const string BrokerHost = "coverage-broker";

    /// <summary>First topic used by subscription and queue tests.</summary>
    private const string FirstTopic = "coverage/first";

    /// <summary>Second topic used by subscription and queue tests.</summary>
    private const string SecondTopic = "coverage/second";

    /// <summary>The internal queued-message publishing method name.</summary>
    private const string TryPublishQueuedMessageMethodName = "TryPublishQueuedMessageAsync";

    /// <summary>The internal reconnection method name.</summary>
    private const string ReconnectIfRequiredMethodName = "ReconnectIfRequiredAsync";

    /// <summary>The minimum number of subscription change notifications.</summary>
    private const int MinimumSubscriptionChanges = 4;

    /// <summary>The minimum number of saved queue snapshots.</summary>
    private const int MinimumSaveCount = 3;

    /// <summary>The expected number of attempted publishes in the failure-path test.</summary>
    private const int ExpectedPublishAttempts = 4;

    /// <summary>The expected number of messages left pending after failed publishes.</summary>
    private const int ExpectedPendingMessages = 2;

    /// <summary>The expected number of processed-message notifications.</summary>
    private const int ExpectedProcessedMessages = 5;

    /// <summary>The expected number of communication failures.</summary>
    private const int ExpectedCommunicationFailures = 2;

    /// <summary>Maximum duration allowed for an asynchronous background transition.</summary>
    private static readonly TimeSpan TransitionTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Short connection-loop interval used by deterministic tests.</summary>
    private static readonly TimeSpan TestConnectionCheckInterval = TimeSpan.FromMilliseconds(20);

    /// <summary>Exercises observable accessors, forwarded events, ping, and state properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EventSurfaces_AttachDetachAndForwardUnderlyingEventsAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);

        SubscribeAndDispose(client.ApplicationMessageProcessed);
        SubscribeAndDispose(client.Connected);
        SubscribeAndDispose(client.Disconnected);
        SubscribeAndDispose(client.ConnectingFailed);
        SubscribeAndDispose(client.ConnectionStateChanged);
        SubscribeAndDispose(client.SynchronizingSubscriptionsFailed);
        SubscribeAndDispose(client.ApplicationMessageSkipped);
        SubscribeAndDispose(client.ApplicationMessageReceived);
        await SubscribeAndDisposeAsync(client.ApplicationMessageProcessedAsyncObservable);
        await SubscribeAndDisposeAsync(client.ConnectedAsyncObservable);
        await SubscribeAndDisposeAsync(client.DisconnectedAsyncObservable);
        await SubscribeAndDisposeAsync(client.ConnectingFailedAsyncObservable);
        await SubscribeAndDisposeAsync(client.ConnectionStateChangedAsyncObservable);
        await SubscribeAndDisposeAsync(client.SynchronizingSubscriptionsFailedAsyncObservable);
        await SubscribeAndDisposeAsync(client.ApplicationMessageSkippedAsyncObservable);
        await SubscribeAndDisposeAsync(client.ApplicationMessageReceivedAsyncObservable);

        var connectedCount = 0;
        var disconnectedCount = 0;
        var receivedCount = 0;
        EventHandler<MqttClientConnectedEventArgs> connectedHandler = (_, _) => connectedCount++;
        EventHandler<MqttClientDisconnectedEventArgs> disconnectedHandler = (_, _) => disconnectedCount++;
        EventHandler<MqttApplicationMessageReceivedEventArgs> receivedHandler = (_, _) => receivedCount++;

        client.ConnectedEvent += connectedHandler;
        client.DisconnectedEvent += disconnectedHandler;
        client.ApplicationMessageReceivedEvent += receivedHandler;

        await internalClient.RaiseConnectedAsync();
        await internalClient.RaiseDisconnectedAsync();
        await internalClient.RaiseApplicationMessageReceivedAsync(CreateReceivedEventArgs(FirstTopic));
        await client.PingAsync();

        client.ConnectedEvent -= connectedHandler;
        client.DisconnectedEvent -= disconnectedHandler;
        client.ApplicationMessageReceivedEvent -= receivedHandler;
        TouchInternalEvents(client);
        internalClient.TouchAuxiliaryEvents();

        await Assert.That(client.InternalClient).IsSameReferenceAs(internalClient);
        await Assert.That(client.IsConnected).IsFalse();
        await Assert.That(client.IsStarted).IsFalse();
        await Assert.That(client.Options).IsNull();
        await Assert.That(client.PendingApplicationMessagesCount).IsEqualTo(0);
        await Assert.That(connectedCount).IsEqualTo(1);
        await Assert.That(disconnectedCount).IsEqualTo(1);
        await Assert.That(receivedCount).IsEqualTo(1);
        await Assert.That(internalClient.PingCount).IsEqualTo(1);
    }

    /// <summary>Exercises startup validation and publishing validation before a client is started.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValidationPaths_RejectInvalidStartupAndMessagesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);

        await Assert.That(() => client.StartAsync(CreateNullArgument<ResilientMqttClientOptions>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.StartAsync(new())).Throws<ArgumentException>();
        await Assert.That(() => client.EnqueueAsync(CreateNullArgument<MqttApplicationMessage>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.EnqueueAsync(CreateNullArgument<ResilientMqttApplicationMessage>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.EnqueueAsync(CreateManagedMessage(FirstTopic)))
            .Throws<InvalidOperationException>();
        await Assert.That(() => client.SubscribeAsync(CreateNullArgument<IEnumerable<MqttTopicFilter>>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.UnsubscribeAsync(CreateNullArgument<IEnumerable<string>>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.SubscribeAsync([
                new MqttTopicFilter { Topic = "invalid/#/tail" },
            ]))
            .Throws<MqttProtocolViolationException>();
    }

    /// <summary>Exercises connection, subscription, publication, and clean shutdown.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Lifecycle_ConnectsSynchronizesPublishesAndStopsCleanlyAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        var connectionChanges = 0;
        var subscriptionChanges = 0;
        var processedMessages = 0;
        using var connectionRegistration = client.RegisterConnectionStateChangedHandler((_, _) =>
        {
            connectionChanges++;
            return ValueTask.CompletedTask;
        });
        using var subscriptionRegistration = client.RegisterSubscriptionsChangedHandler((_, _) =>
        {
            subscriptionChanges++;
            return ValueTask.CompletedTask;
        });
        using var processedRegistration = client.RegisterApplicationMessageProcessedHandler((_, _) =>
        {
            processedMessages++;
            return ValueTask.CompletedTask;
        });

        await client.StartAsync(CreateOptions(maxTopicFilters: 1));
        await WaitUntilAsync(() => internalClient.ConnectCount == 1);
        await Assert.That(client.IsStarted).IsTrue();
        await Assert.That(client.IsConnected).IsTrue();

        await client.SubscribeAsync([
            new MqttTopicFilter { Topic = FirstTopic },
            new MqttTopicFilter { Topic = SecondTopic },
        ]);
        await WaitUntilAsync(() => internalClient.SubscribeRequests.Count >= 2);

        await client.UnsubscribeAsync([FirstTopic, SecondTopic]);
        await WaitUntilAsync(() => internalClient.UnsubscribeRequests.Count >= 2);

        await client.EnqueueAsync(new MqttApplicationMessage { Topic = FirstTopic });
        await WaitUntilAsync(() => internalClient.PublishedMessages.Count == 1);
        await WaitUntilAsync(() => client.PendingApplicationMessagesCount == 0);

        await Assert.That(() => client.StartAsync(CreateOptions())).Throws<InvalidOperationException>();

        await client.StopAsync();

        await Assert.That(client.IsStarted).IsFalse();
        await Assert.That(internalClient.DisconnectCount).IsEqualTo(1);
        await Assert.That(connectionChanges).IsGreaterThanOrEqualTo(1);
        await Assert.That(subscriptionChanges).IsGreaterThanOrEqualTo(MinimumSubscriptionChanges);
        await Assert.That(processedMessages).IsEqualTo(1);
    }

    /// <summary>Exercises both bounded-queue overflow strategies and skipped-message notifications.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnqueueAsync_AppliesDropNewAndDropOldestStrategiesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        var skippedTopics = new List<string>();
        using var skippedRegistration = client.RegisterApplicationMessageSkippedHandler((args, _) =>
        {
            skippedTopics.Add(args.ApplicationMessage.ApplicationMessage?.Topic ?? string.Empty);
            return ValueTask.CompletedTask;
        });

        SetOptions(client, CreateOptions(1));
        await client.EnqueueAsync(CreateManagedMessage(FirstTopic));
        await client.EnqueueAsync(CreateManagedMessage(SecondTopic));

        await Assert.That(client.PendingApplicationMessagesCount).IsEqualTo(1);
        await Assert.That(skippedTopics).IsEquivalentTo([SecondTopic]);

        SetOptions(client, CreateOptions(1, MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage));
        await client.EnqueueAsync(CreateManagedMessage(SecondTopic));

        await Assert.That(client.PendingApplicationMessagesCount).IsEqualTo(1);
        await Assert.That(skippedTopics).IsEquivalentTo([SecondTopic, FirstTopic]);
    }

    /// <summary>Exercises storage loading, queue persistence, and persisted removal after publish.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Storage_LoadsSavesAndRemovesQueuedMessagesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        var storedMessage = CreateManagedMessage(FirstTopic);
        var storage = new RecordingStorage([storedMessage]);

        await client.StartAsync(CreateOptions(storage: storage));
        await WaitUntilAsync(() => internalClient.ConnectCount == 1);
        await WaitUntilAsync(() => internalClient.PublishedMessages.Count == 1);
        await WaitUntilAsync(() => storage.SaveCount >= 1);

        await client.EnqueueAsync(CreateManagedMessage(SecondTopic));
        await WaitUntilAsync(() => internalClient.PublishedMessages.Count == 2);
        await WaitUntilAsync(() => storage.SaveCount >= MinimumSaveCount);
        await client.StopAsync(cleanDisconnect: false);

        await Assert.That(storage.LoadCount).IsEqualTo(1);
        await Assert.That(storage.LastSavedMessages).IsEmpty();
        await Assert.That(internalClient.DisconnectCount).IsEqualTo(0);
    }

    /// <summary>Exercises all queued-publish outcomes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TryPublishQueuedMessageAsync_HandlesAllPublishOutcomesAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        SetOptions(client, CreateOptions());
        var processed = new List<ApplicationMessageProcessedEventArgs>();
        using var processedRegistration = client.RegisterApplicationMessageProcessedHandler((args, _) =>
        {
            processed.Add(args);
            return ValueTask.CompletedTask;
        });

        Func<InterceptingPublishMessageEventArgs, Task> rejectHandler = static args =>
        {
            args.AcceptPublish = false;
            return Task.CompletedTask;
        };
        InvokeVoid(client, "AddInterceptPublishMessageHandler", rejectHandler);
        var rejected = CreateManagedMessage(FirstTopic);
        await client.EnqueueAsync(rejected);
        await InvokePrivateTaskAsync(client, TryPublishQueuedMessageMethodName, rejected, CancellationToken.None);
        InvokeVoid(client, "RemoveInterceptPublishMessageHandler", rejectHandler);

        var successful = CreateManagedMessage(SecondTopic);
        await client.EnqueueAsync(successful);
        await InvokePrivateTaskAsync(client, TryPublishQueuedMessageMethodName, successful, CancellationToken.None);

        internalClient.PublishHandler = static (_, _) =>
            Task.FromException<MqttClientPublishResult>(
                new MqttCommunicationException("communication failure"));
        var qosZeroFailure = CreateManagedMessage("coverage/qos-zero");
        await client.EnqueueAsync(qosZeroFailure);
        await InvokePrivateTaskAsync(client, TryPublishQueuedMessageMethodName, qosZeroFailure, CancellationToken.None);

        var qosOneFailure = CreateManagedMessage("coverage/qos-one", MqttQualityOfServiceLevel.AtLeastOnce);
        await client.EnqueueAsync(qosOneFailure);
        await InvokePrivateTaskAsync(client, TryPublishQueuedMessageMethodName, qosOneFailure, CancellationToken.None);

        internalClient.PublishHandler = static (_, _) =>
            Task.FromException<MqttClientPublishResult>(
                new InvalidOperationException("general failure"));
        var generalFailure = CreateManagedMessage("coverage/general-failure");
        await client.EnqueueAsync(generalFailure);
        await InvokePrivateTaskAsync(client, TryPublishQueuedMessageMethodName, generalFailure, CancellationToken.None);

        await Assert.That(internalClient.PublishedMessages.Count).IsEqualTo(ExpectedPublishAttempts);
        await Assert.That(client.PendingApplicationMessagesCount).IsEqualTo(ExpectedPendingMessages);
        await Assert.That(processed.Count).IsEqualTo(ExpectedProcessedMessages);
        await Assert.That(CountExceptions<MqttCommunicationException>(processed))
            .IsEqualTo(ExpectedCommunicationFailures);
        await Assert.That(CountExceptions<InvalidOperationException>(processed)).IsEqualTo(1);
    }

    /// <summary>Exercises connection failures and subscription synchronization failures.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BackgroundFailures_RaiseFailureEventsAndRemainStoppableAsync()
    {
        using var internalClient = new ScriptedMqttClient
        {
            ConnectHandler = static (_, _) =>
                Task.FromException<MqttClientConnectResult>(
                    new MqttCommunicationException("connect failure")),
        };
        using var client = CreateClient(internalClient);
        var connectionFailures = 0;
        using var connectionFailureRegistration = client.RegisterConnectingFailedHandler((_, _) =>
        {
            connectionFailures++;
            return ValueTask.CompletedTask;
        });

        await client.StartAsync(CreateOptions());
        await WaitUntilAsync(() => connectionFailures >= 1);
        await client.StopAsync(cleanDisconnect: false);

        internalClient.ConnectHandler = null;
        internalClient.SetConnected(true);
        internalClient.SubscribeHandler = static (_, _) =>
            Task.FromException<MqttClientSubscribeResult>(
                new InvalidOperationException("subscribe failure"));
        var subscriptionFailures = 0;
        using var synchronizationFailureRegistration = client.RegisterSynchronizingSubscriptionsFailedHandler((_, _) =>
        {
            subscriptionFailures++;
            return ValueTask.CompletedTask;
        });

        await client.StartAsync(CreateOptions());
        await client.SubscribeAsync([new MqttTopicFilter { Topic = FirstTopic }]);
        await WaitUntilAsync(() => subscriptionFailures >= 1);
        await client.StopAsync(cleanDisconnect: false);

        await Assert.That(connectionFailures).IsGreaterThanOrEqualTo(1);
        await Assert.That(subscriptionFailures).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>Exercises reconnect recovery, continuity, and refusal results.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReconnectIfRequiredAsync_ReturnsRecoveredStillConnectedAndFailureResultsAsync()
    {
        using var internalClient = new ScriptedMqttClient
        {
            ConnectHandler = static (_, _) => Task.FromResult(new MqttClientConnectResult { IsSessionPresent = true }),
        };
        using var client = CreateClient(internalClient);
        SetOptions(client, CreateOptions());

        var recovered = await InvokePrivateTaskResultAsync<ReconnectionResult>(
            client,
            ReconnectIfRequiredMethodName,
            CancellationToken.None);
        var stillConnected = await InvokePrivateTaskResultAsync<ReconnectionResult>(
            client,
            ReconnectIfRequiredMethodName,
            CancellationToken.None);

        internalClient.SetConnected(false);
        internalClient.ConnectHandler = static (_, _) => Task.FromResult(new MqttClientConnectResult
        {
            ResultCode = MqttClientConnectResultCode.BadUserNameOrPassword,
        });
        var failed = await InvokePrivateTaskResultAsync<ReconnectionResult>(
            client,
            ReconnectIfRequiredMethodName,
            CancellationToken.None);

        await Assert.That(recovered).IsEqualTo(ReconnectionResult.Recovered);
        await Assert.That(stillConnected).IsEqualTo(ReconnectionResult.StillConnected);
        await Assert.That(failed).IsEqualTo(ReconnectionResult.NotConnected);
    }

    /// <summary>Creates the internal resilient client around a deterministic MQTT client.</summary>
    /// <param name="internalClient">The underlying client.</param>
    /// <returns>The resilient client instance.</returns>
    private static IResilientMqttClient CreateClient(IMqttClient internalClient)
    {
        var resilientType = typeof(Create).Assembly.GetType(
            "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClient",
            throwOnError: true)
            ?? throw new InvalidOperationException("The resilient MQTT client type could not be resolved.");
        var factory = new MqttClientFactory();
        if (Activator.CreateInstance(
            resilientType,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            [internalClient, factory.DefaultLogger],
            culture: null) is not IResilientMqttClient client)
        {
            throw new InvalidOperationException("The resilient MQTT client could not be created.");
        }

        return client;
    }

    /// <summary>Creates resilient client options for a deterministic test.</summary>
    /// <param name="maxPendingMessages">The queue capacity.</param>
    /// <param name="overflowStrategy">The bounded-queue overflow strategy.</param>
    /// <param name="maxTopicFilters">The broker batch size.</param>
    /// <param name="storage">The optional persistent storage.</param>
    /// <returns>The configured options.</returns>
    private static ResilientMqttClientOptions CreateOptions(
        int maxPendingMessages = int.MaxValue,
        MqttPendingMessagesOverflowStrategy overflowStrategy = MqttPendingMessagesOverflowStrategy.DropNewMessage,
        int maxTopicFilters = int.MaxValue,
        IResilientMqttClientStorage? storage = null) =>
        new()
        {
            ClientOptions = new MqttClientOptionsBuilder().WithTcpServer(BrokerHost).Build(),
            AutoReconnectDelay = TestConnectionCheckInterval,
            ConnectionCheckInterval = TestConnectionCheckInterval,
            MaxPendingMessages = maxPendingMessages,
            PendingMessagesOverflowStrategy = overflowStrategy,
            MaxTopicFiltersInSubscribeUnsubscribePackets = maxTopicFilters,
            Storage = storage,
        };

    /// <summary>Creates a managed application message.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="qualityOfServiceLevel">The message quality-of-service level.</param>
    /// <returns>The managed message.</returns>
    private static ResilientMqttApplicationMessage CreateManagedMessage(
        string topic,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce) =>
        new()
        {
            ApplicationMessage = new MqttApplicationMessage
            {
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
            },
        };

    /// <summary>Creates a null reference for argument-validation tests.</summary>
    /// <typeparam name="T">The reference type to represent as null.</typeparam>
    /// <returns>A null reference represented as the requested type.</returns>
    private static T CreateNullArgument<T>()
        where T : class
    {
        object? nullArgument = null;
        return System.Runtime.CompilerServices.Unsafe.As<object?, T>(ref nullArgument);
    }

    /// <summary>Creates received-message event arguments.</summary>
    /// <param name="topic">The received topic.</param>
    /// <returns>The event arguments.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateReceivedEventArgs(string topic)
    {
        var message = new MqttApplicationMessage { Topic = topic };
        return new("coverage-client", message, new MqttPublishPacket { Topic = topic }, null);
    }

    /// <summary>Sets the internal client's options without starting its background loops.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="options">The options to assign.</param>
    private static void SetOptions(IResilientMqttClient client, ResilientMqttClientOptions options)
    {
        var property = client.GetType().GetProperty(nameof(IResilientMqttClient.Options))
            ?? throw new MissingMemberException("The resilient client options property could not be resolved.");
        property.SetValue(client, options);
    }

    /// <summary>Invokes an internal void method with a single handler argument.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="handler">The handler argument.</param>
    private static void InvokeVoid(
        IResilientMqttClient client,
        string methodName,
        Func<InterceptingPublishMessageEventArgs, Task> handler)
    {
        var method = client.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(client.GetType().FullName, methodName);
        _ = method.Invoke(client, [handler]);
    }

    /// <summary>Invokes an internal task-returning method.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The method arguments.</param>
    /// <returns>A task representing the internal operation.</returns>
    private static async Task InvokePrivateTaskAsync(
        IResilientMqttClient client,
        string methodName,
        params object?[] arguments)
    {
        var method = client.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(client.GetType().FullName, methodName);
        if (method.Invoke(client, arguments) is not Task task)
        {
            throw new InvalidOperationException($"The {methodName} invocation did not return a task.");
        }

        await task.ConfigureAwait(false);
    }

    /// <summary>Invokes an internal task-returning method and obtains its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="client">The resilient client.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The method arguments.</param>
    /// <returns>The internal operation result.</returns>
    private static async Task<T> InvokePrivateTaskResultAsync<T>(
        IResilientMqttClient client,
        string methodName,
        params object?[] arguments)
    {
        var method = client.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(client.GetType().FullName, methodName);
        if (method.Invoke(client, arguments) is not Task<T> task)
        {
            throw new InvalidOperationException($"The {methodName} invocation did not return the expected task type.");
        }

        return await task.ConfigureAwait(false);
    }

    /// <summary>Counts processed messages whose exception has the requested type.</summary>
    /// <typeparam name="TException">The exception type to count.</typeparam>
    /// <param name="processedMessages">The processed-message notifications.</param>
    /// <returns>The number of matching exceptions.</returns>
    private static int CountExceptions<TException>(IEnumerable<ApplicationMessageProcessedEventArgs> processedMessages)
        where TException : Exception
    {
        var count = 0;
        foreach (var processedMessage in processedMessages)
        {
            if (processedMessage.Exception is TException)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Adds and removes all internal event handlers.</summary>
    /// <param name="client">The resilient client.</param>
    private static void TouchInternalEvents(IResilientMqttClient client)
    {
        EventHandler<ApplicationMessageProcessedEventArgs> processed = static (_, _) => { };
        EventHandler<ApplicationMessageSkippedEventArgs> skipped = static (_, _) => { };
        EventHandler<ConnectingFailedEventArgs> connectingFailed = static (_, _) => { };
        EventHandler<EventArgs> stateChanged = static (_, _) => { };
        EventHandler<ResilientProcessFailedEventArgs> synchronizationFailed = static (_, _) => { };
        EventHandler<SubscriptionsChangedEventArgs> subscriptionsChanged = static (_, _) => { };
        client.ApplicationMessageProcessedEvent += processed;
        client.ApplicationMessageProcessedEvent -= processed;
        client.ApplicationMessageSkippedEvent += skipped;
        client.ApplicationMessageSkippedEvent -= skipped;
        client.ConnectingFailedEvent += connectingFailed;
        client.ConnectingFailedEvent -= connectingFailed;
        client.ConnectionStateChangedEvent += stateChanged;
        client.ConnectionStateChangedEvent -= stateChanged;
        client.SynchronizingSubscriptionsFailedEvent += synchronizationFailed;
        client.SynchronizingSubscriptionsFailedEvent -= synchronizationFailed;
        client.SubscriptionsChangedEvent += subscriptionsChanged;
        client.SubscriptionsChangedEvent -= subscriptionsChanged;
    }

    /// <summary>Attaches and immediately removes a synchronous observable subscription.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="observable">The observable to exercise.</param>
    private static void SubscribeAndDispose<T>(IObservable<T> observable)
    {
        using var subscription = observable.Subscribe();
    }

    /// <summary>Attaches and immediately removes an asynchronous observable subscription.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="observable">The asynchronous observable to exercise.</param>
    /// <returns>A task representing the asynchronous subscription.</returns>
    private static async Task SubscribeAndDisposeAsync<T>(IObservableAsync<T> observable)
    {
        await using var subscription = await observable.SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
    }

    /// <summary>Waits until a deterministic asynchronous condition becomes true.</summary>
    /// <param name="condition">The completion condition.</param>
    /// <returns>A task representing the wait.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(TransitionTimeout);
        using var timer = new PeriodicTimer(TestConnectionCheckInterval);
        while (!condition())
        {
            _ = await timer.WaitForNextTickAsync(cancellation.Token);
        }
    }

    /// <summary>Records queued-message persistence activity.</summary>
    /// <param name="initialMessages">The messages returned on first load.</param>
    private sealed class RecordingStorage(
        IEnumerable<ResilientMqttApplicationMessage> initialMessages) : IResilientMqttClientStorage
    {
        /// <summary>Stores the initial load result.</summary>
        private readonly List<ResilientMqttApplicationMessage> _initialMessages = [.. initialMessages];

        /// <summary>Gets the number of load operations.</summary>
        internal int LoadCount { get; private set; }

        /// <summary>Gets the number of save operations.</summary>
        internal int SaveCount { get; private set; }

        /// <summary>Gets the most recently saved messages.</summary>
        internal List<ResilientMqttApplicationMessage> LastSavedMessages { get; private set; } = [];

        /// <inheritdoc/>
        public Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync()
        {
            LoadCount++;
            return Task.FromResult<IList<ResilientMqttApplicationMessage>>([.. _initialMessages]);
        }

        /// <inheritdoc/>
        public Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages)
        {
            SaveCount++;
            LastSavedMessages = [.. messages];
            return Task.CompletedTask;
        }
    }
}
