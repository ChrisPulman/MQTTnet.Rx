// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
#if REACTIVE_SHIM
using MQTTnet.Rx.Server.Reactive;
#else
using MQTTnet.Rx.Server;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ServerCreate = MQTTnet.Rx.Server.Reactive.Create;
#else
using ServerCreate = MQTTnet.Rx.Server.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies that the server observable APIs expose async counterparts.</summary>
public class ServerAsyncCoverageTests
{
    /// <summary>Verifies that server observable event extensions expose async counterparts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerObservableExtensionTypes_HaveAsyncCounterpartsAsync()
    {
        var missing = new List<string>();
        var sourceType = typeof(MqttServerExtensions);
        var methods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var candidates = new List<MethodInfo>();
        foreach (var method in methods)
        {
            if (method.Name.StartsWith("Observe", StringComparison.Ordinal)
                && IsAsyncObservable(method.ReturnType))
            {
                candidates.Add(method);
            }
        }

        foreach (var method in methods)
        {
            if (!IsObservable(method.ReturnType))
            {
                continue;
            }

            var expectedName = $"Observe{method.Name}";
            if (!HasMatchingCandidate(method, expectedName, candidates))
            {
                missing.Add(method.Name);
            }
        }

        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies that server factories expose async counterparts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerCreateType_HasAsyncFactoryCounterpartsAsync()
    {
        var available = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in typeof(ServerCreate).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            _ = available.Add(method.Name);
        }

        var missing = new List<string>();
        AddIfMissing(available, nameof(ServerCreate.MqttServerSignal), missing);
        AddIfMissing(available, nameof(ServerCreate.MqttServerWithRetainedMessagesSignal), missing);
        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Adds an expected member name when it is absent.</summary>
    /// <param name="available">The available member names.</param>
    /// <param name="expectedName">The expected member name.</param>
    /// <param name="missing">The missing member names.</param>
    private static void AddIfMissing(HashSet<string> available, string expectedName, List<string> missing)
    {
        if (available.Contains(expectedName))
        {
            return;
        }

        missing.Add(expectedName);
    }

    /// <summary>Determines whether an asynchronous candidate matches the expected method.</summary>
    /// <param name="sourceMethod">The synchronous source method.</param>
    /// <param name="expectedName">The expected asynchronous method name.</param>
    /// <param name="candidates">The asynchronous method candidates.</param>
    /// <returns><see langword="true"/> when a candidate matches.</returns>
    private static bool HasMatchingCandidate(
        MethodInfo sourceMethod,
        string expectedName,
        List<MethodInfo> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (StringComparer.Ordinal.Compare(candidate.Name, expectedName) == 0
                && HasEquivalentAsyncSignature(sourceMethod, candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether two methods have equivalent synchronous and async signatures.</summary>
    /// <param name="sourceMethod">The synchronous method.</param>
    /// <param name="asyncMethod">The async method.</param>
    /// <returns><see langword="true"/> when the signatures are equivalent.</returns>
    private static bool HasEquivalentAsyncSignature(MethodInfo sourceMethod, MethodInfo asyncMethod)
    {
        if (sourceMethod.GetParameters().Length != asyncMethod.GetParameters().Length)
        {
            return false;
        }

        if (!TypesEquivalent(sourceMethod.ReturnType, asyncMethod.ReturnType))
        {
            return false;
        }

        var sourceParameters = sourceMethod.GetParameters();
        var asyncParameters = asyncMethod.GetParameters();
        for (var i = 0; i < sourceParameters.Length; i++)
        {
            if (!TypesEquivalent(sourceParameters[i].ParameterType, asyncParameters[i].ParameterType))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether synchronous and async surface types are equivalent.</summary>
    /// <param name="sourceType">The synchronous type.</param>
    /// <param name="asyncType">The async type.</param>
    /// <returns><see langword="true"/> when the types are equivalent.</returns>
    private static bool TypesEquivalent(Type sourceType, Type asyncType)
    {
        if (IsObservable(sourceType))
        {
            return ObservableTypesEquivalent(sourceType, asyncType);
        }

        if (sourceType.IsArray)
        {
            return ArrayTypesEquivalent(sourceType, asyncType);
        }

        return sourceType.IsGenericType
            ? GenericTypesEquivalent(sourceType, asyncType)
            : sourceType == asyncType;
    }

    /// <summary>Determines whether observable types carry equivalent element types.</summary>
    /// <param name="sourceType">The synchronous observable type.</param>
    /// <param name="asyncType">The asynchronous observable type.</param>
    /// <returns><see langword="true"/> when the types are equivalent.</returns>
    private static bool ObservableTypesEquivalent(Type sourceType, Type asyncType) =>
        asyncType.IsGenericType
        && asyncType.GetGenericTypeDefinition() == typeof(IObservableAsync<>)
        && TypesEquivalent(sourceType.GetGenericArguments()[0], asyncType.GetGenericArguments()[0]);

    /// <summary>Determines whether array types have equivalent ranks and element types.</summary>
    /// <param name="sourceType">The synchronous array type.</param>
    /// <param name="asyncType">The asynchronous array type.</param>
    /// <returns><see langword="true"/> when the types are equivalent.</returns>
    private static bool ArrayTypesEquivalent(Type sourceType, Type asyncType) =>
        asyncType.IsArray
        && sourceType.GetArrayRank() == asyncType.GetArrayRank()
        && sourceType.GetElementType() is { } sourceElementType
        && asyncType.GetElementType() is { } asyncElementType
        && TypesEquivalent(sourceElementType, asyncElementType);

    /// <summary>Determines whether generic definitions and arguments are equivalent.</summary>
    /// <param name="sourceType">The synchronous generic type.</param>
    /// <param name="asyncType">The asynchronous generic type.</param>
    /// <returns><see langword="true"/> when the types are equivalent.</returns>
    private static bool GenericTypesEquivalent(Type sourceType, Type asyncType)
    {
        if (!asyncType.IsGenericType || sourceType.GetGenericTypeDefinition() != asyncType.GetGenericTypeDefinition())
        {
            return false;
        }

        var sourceArguments = sourceType.GetGenericArguments();
        var asyncArguments = asyncType.GetGenericArguments();
        for (var i = 0; i < sourceArguments.Length; i++)
        {
            if (!TypesEquivalent(sourceArguments[i], asyncArguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a type is an observable.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is an observable.</returns>
    private static bool IsObservable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>);

    /// <summary>Determines whether a type is an async-native observable.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is an async-native observable.</returns>
    private static bool IsAsyncObservable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservableAsync<>);
}
