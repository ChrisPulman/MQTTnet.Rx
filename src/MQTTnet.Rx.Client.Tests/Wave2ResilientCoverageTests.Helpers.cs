// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides reflection and deterministic test infrastructure for resilient-client coverage.</summary>
public partial class Wave2ResilientCoverageTests
{
    /// <summary>Creates the internal resilient client around a scripted MQTT client.</summary>
    /// <param name="internalClient">The underlying MQTT client.</param>
    /// <returns>The resilient client instance.</returns>
    private static IResilientMqttClient CreateClient(IMqttClient internalClient)
    {
        var factory = new MqttClientFactory();
        return GetInvocationResult<IResilientMqttClient>(
            CreateInternal(
                GetClientAssemblyType(ResilientClientTypeName),
                internalClient,
                factory.DefaultLogger),
            ResilientClientTypeName);
    }

    /// <summary>Creates deterministic resilient-client options.</summary>
    /// <param name="storage">The optional storage implementation.</param>
    /// <param name="maxPendingMessages">The maximum pending-message count.</param>
    /// <param name="overflowStrategy">The pending-message overflow strategy.</param>
    /// <param name="maxTopicFilters">The maximum subscription batch size.</param>
    /// <returns>The configured options.</returns>
    private static ResilientMqttClientOptions CreateOptions(
        IResilientMqttClientStorage? storage = null,
        int maxPendingMessages = int.MaxValue,
        MqttPendingMessagesOverflowStrategy overflowStrategy = MqttPendingMessagesOverflowStrategy.DropNewMessage,
        int maxTopicFilters = int.MaxValue) =>
        new()
        {
            ClientOptions = new MqttClientOptionsBuilder().WithTcpServer(BrokerHost).Build(),
            AutoReconnectDelay = TimeSpan.Zero,
            ConnectionCheckInterval = TimeSpan.FromMilliseconds(PollingIntervalMilliseconds),
            Storage = storage,
            MaxPendingMessages = maxPendingMessages,
            PendingMessagesOverflowStrategy = overflowStrategy,
            MaxTopicFiltersInSubscribeUnsubscribePackets = maxTopicFilters,
        };

    /// <summary>Creates a resilient application message.</summary>
    /// <param name="topic">The MQTT topic.</param>
    /// <returns>The managed application message.</returns>
    private static ResilientMqttApplicationMessage CreateManagedMessage(string topic) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApplicationMessage = new()
            {
                Topic = topic,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            },
        };

    /// <summary>Gets an internal type from the client assembly.</summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The resolved type.</returns>
    private static Type GetClientAssemblyType(string typeName) =>
        typeof(Create).Assembly.GetType(typeName, throwOnError: true)
        ?? throw new InvalidOperationException($"The {typeName} type could not be resolved.");

    /// <summary>Creates an internal object through reflection.</summary>
    /// <param name="type">The object type.</param>
    /// <param name="arguments">The constructor arguments.</param>
    /// <returns>The created object.</returns>
    private static object CreateInternal(Type type, params object?[] arguments)
    {
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            arguments,
            culture: null)
            ?? throw new InvalidOperationException($"The {type.FullName} instance could not be created.");
    }

    /// <summary>Invokes an instance method with exact parameter types.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="parameterTypes">The exact parameter types.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>The invocation result.</returns>
    private static object? InvokeInstance(
        object instance,
        string methodName,
        Type[] parameterTypes,
        object?[] arguments)
    {
        var method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            parameterTypes,
            modifiers: null)
            ?? throw new MissingMethodException(instance.GetType().FullName, methodName);
        return method.Invoke(instance, arguments);
    }

    /// <summary>Invokes a static method with exact parameter types.</summary>
    /// <param name="type">The declaring type.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="parameterTypes">The exact parameter types.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>The invocation result.</returns>
    private static object? InvokeStatic(Type type, string methodName, Type[] parameterTypes, object?[] arguments)
    {
        var method = type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            parameterTypes,
            modifiers: null)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return method.Invoke(null, arguments);
    }

    /// <summary>Invokes a private task-returning method.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>The method task.</returns>
    private static Task InvokeTaskCoreAsync(object instance, string methodName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(instance.GetType().FullName, methodName);
        return method.Invoke(instance, arguments) as Task
            ?? throw new InvalidOperationException($"The {methodName} invocation did not return a task.");
    }

    /// <summary>Invokes and awaits a private task-returning method.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="arguments">The invocation arguments.</param>
    /// <returns>A task representing the method invocation.</returns>
    private static Task InvokeTaskAsync(object instance, string methodName, params object?[] arguments) =>
        InvokeTaskCoreAsync(instance, methodName, arguments);

    /// <summary>Assigns the private options property.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="options">The options value.</param>
    private static void SetOptions(IResilientMqttClient client, ResilientMqttClientOptions? options)
    {
        var property = client.GetType().GetProperty(nameof(IResilientMqttClient.Options))
            ?? throw new MissingMemberException("The resilient client options property could not be resolved.");
        property.SetValue(client, options);
    }

    /// <summary>Creates and assigns a storage manager to the resilient client.</summary>
    /// <param name="client">The resilient client.</param>
    /// <param name="storage">The storage implementation.</param>
    private static void SetStorageManager(IResilientMqttClient client, IResilientMqttClientStorage storage)
    {
        var manager = CreateInternal(GetClientAssemblyType(StorageManagerTypeName), storage);
        var field = client.GetType().GetField("_storageManager", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(client.GetType().FullName, "_storageManager");
        field.SetValue(client, manager);
    }

    /// <summary>Gets the private reconnect-subscription dictionary.</summary>
    /// <param name="client">The resilient client.</param>
    /// <returns>The reconnect-subscription dictionary.</returns>
    private static Dictionary<string, MqttTopicFilter> GetReconnectSubscriptions(IResilientMqttClient client) =>
        GetFieldValue<Dictionary<string, MqttTopicFilter>>(client, "_reconnectSubscriptions");

    /// <summary>Gets a private field value.</summary>
    /// <typeparam name="T">The field value type.</typeparam>
    /// <param name="instance">The target instance.</param>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The field value.</returns>
    private static T GetFieldValue<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(instance.GetType().FullName, fieldName);
        return GetInvocationResult<T>(field.GetValue(instance), fieldName);
    }

    /// <summary>Creates a null reference for argument-validation tests.</summary>
    /// <typeparam name="T">The reference type to represent as null.</typeparam>
    /// <returns>A null reference represented as the requested type.</returns>
    private static T CreateNullArgument<T>()
        where T : class
    {
        object? nullArgument = null;
        return System.Runtime.CompilerServices.Unsafe.As<object?, T>(ref nullArgument);
    }

    /// <summary>Converts a reflection result to its expected non-null type.</summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="result">The reflection result.</param>
    /// <param name="operationName">The operation that produced the result.</param>
    /// <returns>The typed reflection result.</returns>
    private static T GetInvocationResult<T>(object? result, string operationName) =>
        result is T typedResult
            ? typedResult
            : throw new InvalidOperationException($"The {operationName} operation returned an unexpected result.");

    /// <summary>Subscribes to and explicitly disposes an asynchronous observable.</summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    /// <param name="observable">The observable to exercise.</param>
    /// <returns>A task representing subscription disposal.</returns>
    private static async Task SubscribeThenDisposeAsync<T>(IObservableAsync<T> observable)
    {
        var subscription = await observable.SubscribeAsync(
            static (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);
        await subscription.DisposeAsync();
    }

    /// <summary>Waits for a deterministic condition to become true.</summary>
    /// <param name="condition">The completion condition.</param>
    /// <returns>A task representing the wait.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(PollingTimeoutSeconds));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingIntervalMilliseconds));
        while (!condition())
        {
            _ = await timer.WaitForNextTickAsync(cancellation.Token);
        }
    }

    /// <summary>Records messages saved by resilient-client persistence.</summary>
    private sealed class Wave2ResilientStorage : IResilientMqttClientStorage
    {
        /// <summary>Gets the number of save calls.</summary>
        internal int SaveCount { get; private set; }

        /// <summary>Gets the most recently saved messages.</summary>
        internal List<ResilientMqttApplicationMessage> LastSavedMessages { get; private set; } = [];

        /// <inheritdoc/>
        public Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync() =>
            Task.FromResult<IList<ResilientMqttApplicationMessage>>([]);

        /// <inheritdoc/>
        public Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages)
        {
            SaveCount++;
            LastSavedMessages = [.. messages];
            return Task.CompletedTask;
        }
    }
}
