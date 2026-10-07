// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.LowLevelClient;
using MQTTnet.Server;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using MQTTnet.Rx.Server.Reactive;
using ClientEventExtensions = MQTTnet.Rx.Client.Reactive.MqttClientExtensions;
using ServerEventExtensions = MQTTnet.Rx.Server.Reactive.MqttServerExtensions;
#else
using MQTTnet.Rx.Server;
using ClientEventExtensions = MQTTnet.Rx.Client.MqttClientExtensions;
using ServerEventExtensions = MQTTnet.Rx.Server.MqttServerExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Detects underlying MQTTnet API additions that lack paired reactive wrappers.</summary>
public sealed class MqttUnderlyingApiContractTests
{
    /// <summary>The suffix for task-based MQTTnet operations and events.</summary>
    private const string AsyncSuffix = "Async";

    /// <summary>Checks every underlying client and broker event against both observable surfaces.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task EveryUnderlyingEventHasTypedObservablePairsAsync()
    {
        var missing = new List<string>();
        CheckEvents(typeof(IMqttClient), typeof(ClientEventExtensions), missing);
        CheckEvents(typeof(ILowLevelMqttClient), typeof(LowLevelMqttClientOperationExtensions), missing);
        CheckEvents(typeof(MqttServer), typeof(ServerEventExtensions), missing);
        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Checks complete option and packet arguments are retained by client operation wrappers.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task EveryUnderlyingClientOperationHasCompleteObservablePairsAsync()
    {
        var missing = new List<string>();
        CheckOperations(typeof(IMqttClient), typeof(MqttClientOperationExtensions), missing);
        CheckOperations(typeof(ILowLevelMqttClient), typeof(LowLevelMqttClientOperationExtensions), missing);
        CheckOperations(typeof(MqttServer), typeof(MqttServerOperationExtensions), missing);
        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Checks typed event arguments remain accessible without copying or projecting their properties.</summary>
    /// <param name="underlying">The underlying event owner.</param>
    /// <param name="extensions">The wrapper extensions.</param>
    /// <param name="missing">The missing wrappers.</param>
    private static void CheckEvents(Type underlying, Type extensions, List<string> missing)
    {
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var eventInfo in underlying.GetEvents())
        {
            var name = eventInfo.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal)
                ? eventInfo.Name[..^AsyncSuffix.Length]
                : eventInfo.Name;
            var handler = eventInfo.EventHandlerType ?? throw new InvalidOperationException("The event has no handler type.");
            var argument = handler.GenericTypeArguments[0];
            CheckWrapper(methods, name, [underlying], typeof(IObservable<>).MakeGenericType(argument), missing);
            CheckWrapper(methods, $"Observe{name}", [underlying], typeof(IObservableAsync<>).MakeGenericType(argument), missing);
        }
    }

    /// <summary>Checks every underlying operation retains its full arguments except subscription-owned cancellation.</summary>
    /// <param name="underlying">The underlying operation owner.</param>
    /// <param name="extensions">The wrapper extensions.</param>
    /// <param name="missing">The missing wrappers.</param>
    private static void CheckOperations(Type underlying, Type extensions, List<string> missing)
    {
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var operation in underlying.GetMethods())
        {
            if (operation.IsSpecialName || !typeof(Task).IsAssignableFrom(operation.ReturnType))
            {
                continue;
            }

            var name = operation.Name.EndsWith(AsyncSuffix, StringComparison.Ordinal)
                ? operation.Name[..^AsyncSuffix.Length]
                : operation.Name;
            var (observableName, asyncName) = GetOperationNames(underlying, name);
            List<Type> parameters = [underlying];
            foreach (var parameter in operation.GetParameters())
            {
                if (parameter.ParameterType != typeof(CancellationToken))
                {
                    parameters.Add(parameter.ParameterType);
                }
            }

            var observableResult = typeof(IObservable<>);
            var asyncResult = typeof(IObservableAsync<>);
            if (operation.ReturnType.IsGenericType)
            {
                var result = operation.ReturnType.GenericTypeArguments[0];
                observableResult = observableResult.MakeGenericType(result);
                asyncResult = asyncResult.MakeGenericType(result);
            }

            CheckWrapper(methods, observableName, parameters, observableResult, missing);
            CheckWrapper(methods, asyncName, parameters, asyncResult, missing);
        }
    }

    /// <summary>Maps broker operations whose wrapper names avoid native instance-member conflicts.</summary>
    /// <param name="underlying">The operation owner.</param>
    /// <param name="name">The underlying operation name.</param>
    /// <returns>The classic and asynchronous wrapper names.</returns>
    private static (string Observable, string Async) GetOperationNames(Type underlying, string name)
    {
        if (underlying != typeof(MqttServer))
        {
            return (name, $"Observe{name}");
        }

        var observable = name switch
        {
            "InjectApplicationMessage" => "InjectApplicationMessageOperation",
            "Subscribe" => "SubscribeClient",
            "Unsubscribe" => "UnsubscribeClient",
            _ => name,
        };
        var asynchronous = name switch
        {
            "GetClients" => "ObserveClients",
            "GetRetainedMessage" => "ObserveRetainedMessage",
            "GetRetainedMessages" => "ObserveRetainedMessages",
            "GetSessions" => "ObserveSessions",
            "GetSession" => "ObserveSession",
            "Subscribe" => "ObserveSubscribeClient",
            "Unsubscribe" => "ObserveUnsubscribeClient",
            _ => $"Observe{name}",
        };
        return (observable, asynchronous);
    }

    /// <summary>Records a missing wrapper when its complete signature is absent.</summary>
    /// <param name="methods">The wrapper methods.</param>
    /// <param name="name">The expected name.</param>
    /// <param name="parameters">The complete expected argument types.</param>
    /// <param name="returnType">The expected exact or open generic return type.</param>
    /// <param name="missing">The missing wrappers.</param>
    private static void CheckWrapper(MethodInfo[] methods, string name, List<Type> parameters, Type returnType, List<string> missing)
    {
        foreach (var method in methods)
        {
            if (method.Name != name || !HasParameters(method, parameters))
            {
                continue;
            }

            if (method.ReturnType == returnType || (returnType.IsGenericTypeDefinition
                && method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == returnType))
            {
                return;
            }
        }

        missing.Add($"{parameters[0].Name}.{name}");
    }

    /// <summary>Checks each argument type against the underlying operation.</summary>
    /// <param name="method">The wrapper method.</param>
    /// <param name="expected">The underlying argument types.</param>
    /// <returns>Whether the argument types match.</returns>
    private static bool HasParameters(MethodInfo method, List<Type> expected)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != expected.Count)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].ParameterType != expected[index])
            {
                return false;
            }
        }

        return true;
    }
}
