// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes the final deterministic resilient-client coverage branches.</summary>
[NotInParallel]
public sealed class FinalResilientCoverageClosureTests
{
    /// <summary>The internal resilient-client type name.</summary>
    private const string ResilientClientTypeName =
        "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClient";

    /// <summary>The internal connection-maintenance method name.</summary>
    private const string MaintainConnectionMethodName = "MaintainConnectionAsync";

    /// <summary>The internal queued-publisher method name.</summary>
    private const string PublishQueuedMessagesMethodName = "PublishQueuedMessagesAsync";

    /// <summary>The internal reconnect method name.</summary>
    private const string ReconnectIfRequiredMethodName = "ReconnectIfRequiredAsync";

    /// <summary>The internal one-cycle connection-maintenance method name.</summary>
    private const string TryMaintainConnectionMethodName = "TryMaintainConnectionAsync";

    /// <summary>The topic used for resilient-client coverage.</summary>
    private const string CoverageTopic = "coverage/final-resilient";

    /// <summary>The short connection-check interval.</summary>
    private const int ConnectionCheckMilliseconds = 10;

    /// <summary>The expected number of synchronous event calls.</summary>
    /// <remarks>Subscription processing reports its subscribe and unsubscribe phases, then reports the
    /// reconnect.</remarks>
    private const int ExpectedEventCalls = 10;

    /// <summary>Exercises both outer connection-maintenance exception handlers.</summary>
    /// <param name="throwCancellation">Whether the one-shot logger throws cancellation.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task FinalResilientCoverageClosure_MaintenanceHandlesEscapedLoggerFailuresAsync(
        bool throwCancellation)
    {
        using var internalClient = new ScriptedMqttClient();
        Exception loggerException = throwCancellation
            ? new OperationCanceledException("final resilient cancellation")
            : new InvalidOperationException("final resilient failure");
        using var client = CreateClient(internalClient, new OneShotThrowingLogger(loggerException));

        await InvokeTaskAsync(client, MaintainConnectionMethodName, CancellationToken.None);

        await Assert.That(internalClient.ConnectCount).IsEqualTo(0);
    }

    /// <summary>Exercises the queued-publisher general exception handler.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FinalResilientCoverageClosure_PublisherHandlesProcessedEventFailureAsync()
    {
        using var internalClient = new ScriptedMqttClient();
        using var client = CreateClient(internalClient);
        SetOptions(client, CreateOptions());
        internalClient.SetConnected(true);
        using var processedRegistration = client.RegisterApplicationMessageProcessedHandler(
            static (_, _) => ValueTask.FromException(
                new InvalidOperationException("final resilient processed-event failure")));
        await client.EnqueueAsync(new MqttApplicationMessage { Topic = CoverageTopic });

        await InvokeTaskAsync(client, PublishQueuedMessagesMethodName, CancellationToken.None);

        await Assert.That(internalClient.PublishedMessages).HasSingleItem();
    }

    /// <summary>Exercises all remaining synchronous resilient-client event branches.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FinalResilientCoverageClosure_SynchronousEventsAreForwardedAsync()
    {
        var eventCalls = 0;
        using var internalClient = new ScriptedMqttClient
        {
            ConnectHandler = static (_, _) =>
                Task.FromException<MqttClientConnectResult>(
                    new InvalidOperationException("final resilient connect failure")),
        };
        using var client = CreateClient(internalClient);
        SetOptions(client, CreateOptions());
        EventHandler<MqttApplicationMessageReceivedEventArgs> receivedHandler = (_, _) => eventCalls++;
        client.ApplicationMessageReceivedEvent += receivedHandler;
        client.ConnectingFailedEvent += (_, _) => eventCalls++;
        client.SynchronizingSubscriptionsFailedEvent += (_, _) => eventCalls++;
        client.SubscriptionsChangedEvent += (_, _) => eventCalls++;

        await internalClient.RaiseApplicationMessageReceivedAsync(CreateReceivedEventArgs());
        client.ApplicationMessageReceivedEvent -= receivedHandler;
        using var receivedRegistration = client.RegisterApplicationMessageReceivedHandler(
            (_, _) =>
            {
                eventCalls++;
                return default;
            });
        await internalClient.RaiseApplicationMessageReceivedAsync(CreateReceivedEventArgs());
        await InvokeTaskAsync(client, ReconnectIfRequiredMethodName, CancellationToken.None);
        await InvokeTaskAsync(
            client,
            "HandleSubscriptionExceptionAsync",
            new InvalidOperationException("final resilient subscription failure"),
            null,
            null);

        internalClient.ConnectHandler = null;
        internalClient.SetConnected(true);
        await client.SubscribeAsync([new MqttTopicFilter { Topic = CoverageTopic }]);
        await InvokeTaskAsync(client, TryMaintainConnectionMethodName, CancellationToken.None);

        internalClient.SetConnected(false);
        client.ConnectionStateChangedEvent += (_, _) => eventCalls++;
        client.DisconnectedEvent += (_, _) => eventCalls++;
        using var disconnectedRegistration = client.RegisterDisconnectedHandler(
            (_, _) =>
            {
                eventCalls++;
                return default;
            });
        await internalClient.RaiseDisconnectedAsync();
        await InvokeTaskAsync(client, TryMaintainConnectionMethodName, CancellationToken.None);
        await client.StopAsync(cleanDisconnect: false);

        await Assert.That(eventCalls).IsEqualTo(ExpectedEventCalls);
    }

    /// <summary>Creates an internal resilient client around a deterministic MQTT client.</summary>
    /// <param name="internalClient">The underlying client.</param>
    /// <param name="logger">The optional logger.</param>
    /// <returns>The resilient client instance.</returns>
    private static IResilientMqttClient CreateClient(
        IMqttClient internalClient,
        IMqttNetLogger? logger = null)
    {
        var clientType = typeof(Create).Assembly.GetType(ResilientClientTypeName, throwOnError: true)
            ?? throw new InvalidOperationException("The resilient client type could not be resolved.");
        var resolvedLogger = logger ?? new MqttClientFactory().DefaultLogger;
        return Activator.CreateInstance(
                clientType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                [internalClient, resolvedLogger],
                culture: null) as IResilientMqttClient
            ?? throw new InvalidOperationException("The resilient client could not be created.");
    }

    /// <summary>Creates deterministic resilient-client options.</summary>
    /// <returns>The configured options.</returns>
    private static ResilientMqttClientOptions CreateOptions() =>
        new()
        {
            ClientOptions = new MqttClientOptionsBuilder().WithTcpServer("coverage-broker").Build(),
            AutoReconnectDelay = TimeSpan.Zero,
            ConnectionCheckInterval = TimeSpan.FromMilliseconds(ConnectionCheckMilliseconds),
        };

    /// <summary>Creates received-message event arguments.</summary>
    /// <returns>The event arguments.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateReceivedEventArgs()
    {
        var message = new MqttApplicationMessage { Topic = CoverageTopic };
        return new(
            "final-resilient-client",
            message,
            new MqttPublishPacket { Topic = CoverageTopic },
            acknowledgeHandler: null);
    }

    /// <summary>Sets the internal client's options without starting background processing.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="options">The options to assign.</param>
    private static void SetOptions(IResilientMqttClient client, ResilientMqttClientOptions options)
    {
        var property = client.GetType().GetProperty(nameof(IResilientMqttClient.Options))
            ?? throw new MissingMemberException("The resilient client options property could not be resolved.");
        property.SetValue(client, options);
    }

    /// <summary>Invokes a private task-returning method.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The method arguments.</param>
    /// <returns>A task representing the method invocation.</returns>
    private static Task InvokeTaskAsync(
        IResilientMqttClient client,
        string methodName,
        params object?[] arguments)
    {
        var method = client.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(client.GetType().FullName, methodName);
        return method.Invoke(client, arguments) as Task
            ?? throw new InvalidOperationException($"The {methodName} invocation did not return a task.");
    }

    /// <summary>Throws one configured exception from the first log publication.</summary>
    private sealed class OneShotThrowingLogger : IMqttNetLogger
    {
        /// <summary>The exception thrown from the first log publication.</summary>
        private readonly Exception _exception;

        /// <summary>Tracks whether the configured exception has been thrown.</summary>
        private int _published;

        /// <summary>Initializes a new instance of the <see cref="OneShotThrowingLogger"/> class.</summary>
        /// <param name="exception">The exception to throw on the first publication.</param>
        internal OneShotThrowingLogger(Exception exception) => _exception = exception;

        /// <inheritdoc/>
        public bool IsEnabled => true;

        /// <inheritdoc/>
        public void Publish(
            MqttNetLogLevel level,
            string source,
            string message,
            object[] parameters,
            Exception exception)
        {
            if (Interlocked.Increment(ref _published) == 1)
            {
                throw _exception;
            }
        }
    }
}
