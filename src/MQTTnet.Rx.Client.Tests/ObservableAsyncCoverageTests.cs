// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
#if REACTIVE_SHIM
using MQTTnet.Rx.Client.Reactive.MemoryEfficient;
#else
using MQTTnet.Rx.Client.MemoryEfficient;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using LowAllocAsyncBridge = MQTTnet.Rx.Client.Reactive.MemoryEfficient.ObservableAsyncBridgeExtensions;
#else
using LowAllocAsyncBridge = MQTTnet.Rx.Client.MemoryEfficient.ObservableAsyncBridgeExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies that all public observable APIs expose async observable counterparts.</summary>
public sealed class ObservableAsyncCoverageTests
{
    /// <summary>Verifies that client observable extension methods expose async counterparts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ClientObservableExtensionTypes_HaveAsyncCounterpartsAsync()
    {
        var missing = new List<string>();

        AssertAsyncCoverage(typeof(PayloadExtensions), typeof(ClientAsyncBridge), missing);
        AssertAsyncCoverage(typeof(TopicFilterExtensions), typeof(ClientAsyncBridge), missing);
        AssertAsyncCoverage(typeof(MqttdPublishExtensions), typeof(ClientAsyncBridge), missing);
        AssertAsyncCoverage(typeof(MqttdSubscribeExtensions), typeof(ClientAsyncBridge), missing);
        AssertAsyncCoverage(typeof(LowAllocExtensions), typeof(LowAllocAsyncBridge), missing);

        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies that protocol integration factories expose async counterparts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ProtocolCreateTypes_HaveAsyncCounterpartsAsync()
    {
        var missing = new List<string>();

        AssertFileContainsAll(
            "MQTTnet.Rx.ABPlc\\ObservableAsyncCreateExtensions.cs",
            [
                "IObservableAsync<MqttClientPublishResult> PublishABPlcTag",
                "IDisposable SubscribeABPlcTag",
                "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag",
            ],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.Modbus\\ObservableAsyncCreateExtensions.cs",
            [
                "FromMasterAsync",
                "FromFactoryAsync",
                "PublishInputRegisters",
                "PublishHoldingRegisters",
                "PublishInputs",
                "PublishCoils",
                "PublishModbus",
                "IObservableAsync",
            ],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.S7Plc\\ObservableAsyncCreateExtensions.cs",
            [
                "IObservableAsync<MqttClientPublishResult> PublishS7PlcTag",
                "IDisposable SubscribeS7PlcTag",
                "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTag",
            ],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.SerialPort\\ObservableAsyncCreateExtensions.cs",
            [
                "IObservableAsync<MqttClientPublishResult> PublishSerialPort",
                "IObservableAsync<char> startsWith",
                "IDisposable SubscribeSerialPortWriteLine",
                "IDisposable SubscribeSerialPortWrite",
                "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort",
            ],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.TwinCAT\\ObservableAsyncCreateExtensions.cs",
            [
                "IObservableAsync<MqttClientPublishResult> PublishTcPlcTag",
                "IDisposable SubscribeTcTag",
                "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag",
            ],
            missing);

        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies that resilient client observable properties expose async counterparts.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientClientInterface_HasAsyncObservablePropertiesAsync()
    {
        var interfaceType = typeof(IResilientMqttClient);
        var missing = new List<string>();

        foreach (var property in interfaceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsObservable(property.PropertyType))
            {
                continue;
            }

            var asyncProperty = interfaceType.GetProperty(
                $"{property.Name}AsyncObservable",
                BindingFlags.Instance | BindingFlags.Public);
            if (asyncProperty is null || asyncProperty.PropertyType != TranslateObservableType(property.PropertyType))
            {
                missing.Add(property.Name);
            }
        }

        await Assert.That(string.Join(Environment.NewLine, missing)).IsEqualTo(string.Empty);
    }

    /// <summary>Records client observable methods without equivalent async methods.</summary>
    /// <param name="sourceType">The synchronous extension type.</param>
    /// <param name="asyncType">The async extension type.</param>
    /// <param name="missing">The collection receiving missing method names.</param>
    private static void AssertAsyncCoverage(Type sourceType, Type asyncType, List<string> missing)
    {
        var candidates = asyncType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var method in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (!UsesObservableSurface(method))
            {
                continue;
            }

            var expectedName = Array.Exists(
                method.GetParameters(),
                static parameter => IsObservable(parameter.ParameterType))
                ? method.Name
                : $"{method.Name}Async";

            if (!HasMatchingCandidate(method, expectedName, candidates))
            {
                missing.Add($"{sourceType.FullName}.{method}");
            }
        }
    }

    /// <summary>Determines whether an asynchronous candidate matches the expected method.</summary>
    /// <param name="sourceMethod">The synchronous method.</param>
    /// <param name="expectedName">The expected asynchronous method name.</param>
    /// <param name="candidates">The asynchronous candidates.</param>
    /// <returns><see langword="true"/> when a matching candidate exists.</returns>
    private static bool HasMatchingCandidate(MethodInfo sourceMethod, string expectedName, MethodInfo[] candidates)
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

    /// <summary>Determines whether a method uses an observable in its return type or parameters.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns><see langword="true"/> when the method uses an observable.</returns>
    private static bool UsesObservableSurface(MethodInfo method) =>
        IsObservable(method.ReturnType)
        || Array.Exists(method.GetParameters(), static parameter => IsObservable(parameter.ParameterType));

    /// <summary>Determines whether two methods have equivalent synchronous and async signatures.</summary>
    /// <param name="sourceMethod">The synchronous method.</param>
    /// <param name="asyncMethod">The async method.</param>
    /// <returns><see langword="true"/> when the signatures are equivalent.</returns>
    private static bool HasEquivalentAsyncSignature(MethodInfo sourceMethod, MethodInfo asyncMethod)
    {
        if (sourceMethod.GetGenericArguments().Length != asyncMethod.GetGenericArguments().Length)
        {
            return false;
        }

        if (!TypesEquivalent(sourceMethod.ReturnType, asyncMethod.ReturnType))
        {
            return false;
        }

        var sourceParameters = sourceMethod.GetParameters();
        var asyncParameters = asyncMethod.GetParameters();
        if (sourceParameters.Length != asyncParameters.Length)
        {
            return false;
        }

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
        if (sourceType.IsGenericParameter)
        {
            return GenericParametersEquivalent(sourceType, asyncType);
        }

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

    /// <summary>Determines whether generic parameters occupy the same position.</summary>
    /// <param name="sourceType">The synchronous generic parameter.</param>
    /// <param name="asyncType">The asynchronous generic parameter.</param>
    /// <returns><see langword="true"/> when the parameters are equivalent.</returns>
    private static bool GenericParametersEquivalent(Type sourceType, Type asyncType) =>
        asyncType.IsGenericParameter && sourceType.GenericParameterPosition == asyncType.GenericParameterPosition;

    /// <summary>Determines whether observable types carry equivalent element types.</summary>
    /// <param name="sourceType">The synchronous observable type.</param>
    /// <param name="asyncType">The asynchronous observable type.</param>
    /// <returns><see langword="true"/> when the observable types are equivalent.</returns>
    private static bool ObservableTypesEquivalent(Type sourceType, Type asyncType) =>
        asyncType.IsGenericType
        && asyncType.GetGenericTypeDefinition() == typeof(IObservableAsync<>)
        && TypesEquivalent(sourceType.GetGenericArguments()[0], asyncType.GetGenericArguments()[0]);

    /// <summary>Determines whether array types have the same rank and equivalent element types.</summary>
    /// <param name="sourceType">The synchronous array type.</param>
    /// <param name="asyncType">The asynchronous array type.</param>
    /// <returns><see langword="true"/> when the array types are equivalent.</returns>
    private static bool ArrayTypesEquivalent(Type sourceType, Type asyncType)
    {
        var sourceElementType = sourceType.GetElementType();
        var asyncElementType = asyncType.GetElementType();
        return asyncType.IsArray
            && sourceElementType is not null
            && asyncElementType is not null
            && sourceType.GetArrayRank() == asyncType.GetArrayRank()
            && TypesEquivalent(sourceElementType, asyncElementType);
    }

    /// <summary>Determines whether constructed generic types and their arguments are equivalent.</summary>
    /// <param name="sourceType">The synchronous generic type.</param>
    /// <param name="asyncType">The asynchronous generic type.</param>
    /// <returns><see langword="true"/> when the generic types are equivalent.</returns>
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

    /// <summary>Translates an observable type to its async observable equivalent.</summary>
    /// <param name="type">The source type.</param>
    /// <returns>The translated type.</returns>
    private static Type TranslateObservableType(Type type) =>
        IsObservable(type)
            ? typeof(IObservableAsync<>).MakeGenericType(type.GetGenericArguments()[0])
            : type;

    /// <summary>Determines whether a type is an observable.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is an observable.</returns>
    private static bool IsObservable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>);

    /// <summary>Finds the solution source directory from the test output path.</summary>
    /// <returns>The source directory.</returns>
    private static DirectoryInfo FindSrcDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !string.Equals(directory.Name, "src", StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException(
            "Could not locate the src directory from the test base path.");
    }

    /// <summary>Records expected fragments that are absent from a source file.</summary>
    /// <param name="relativePath">The source-relative file path.</param>
    /// <param name="fragments">The expected source fragments.</param>
    /// <param name="missing">The collection receiving missing fragments.</param>
    private static void AssertFileContainsAll(string relativePath, string[] fragments, List<string> missing)
    {
        var filePath = Path.Combine(FindSrcDirectory().FullName, relativePath);
        var content = File.ReadAllText(filePath);
        foreach (var fragment in fragments)
        {
            if (!content.Contains(fragment, StringComparison.Ordinal))
            {
                missing.Add($"{relativePath}:{fragment}");
            }
        }
    }
}
