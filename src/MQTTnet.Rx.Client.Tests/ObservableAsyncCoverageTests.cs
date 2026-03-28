// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using MQTTnet.Rx.Client.MemoryEfficient;
using ReactiveUI.Extensions.Async;
using TUnit.Assertions.Extensions;
using TUnit.Core;

#pragma warning disable SA1600

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Verifies that all public observable APIs expose async observable counterparts.
/// </summary>
public class ObservableAsyncCoverageTests
{
    [Test]
    public async Task ClientObservableExtensionTypes_HaveAsyncCounterparts()
    {
        var missing = new List<string>();

        AssertAsyncCoverage(typeof(PayloadExtensions), typeof(MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions), missing);
        AssertAsyncCoverage(typeof(TopicFilterExtensions), typeof(MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions), missing);
        AssertAsyncCoverage(typeof(MqttdPublishExtensions), typeof(MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions), missing);
        AssertAsyncCoverage(typeof(MqttdSubscribeExtensions), typeof(MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions), missing);
        AssertAsyncCoverage(typeof(LowAllocExtensions), typeof(MQTTnet.Rx.Client.MemoryEfficient.ObservableAsyncBridgeExtensions), missing);

        if (missing.Count > 0)
        {
            throw new InvalidOperationException("Missing async counterparts: " + string.Join(", ", missing));
        }

        await Assert.That(missing).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ProtocolCreateTypes_HaveAsyncCounterparts()
    {
        var missing = new List<string>();

        AssertFileContainsAll(
            "MQTTnet.Rx.ABPlc\\ObservableAsyncCreateExtensions.cs",
            ["IObservableAsync<MqttClientPublishResult> PublishABPlcTag", "void SubscribeABPlcTag", "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag"],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.Modbus\\ObservableAsyncCreateExtensions.cs",
            ["FromMasterAsync", "FromFactoryAsync", "PublishInputRegisters", "PublishHoldingRegisters", "PublishInputs", "PublishCoils", "PublishModbus", "IObservableAsync"],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.S7Plc\\ObservableAsyncCreateExtensions.cs",
            ["IObservableAsync<MqttClientPublishResult> PublishS7PlcTag", "void SubscribeS7PlcTag", "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTag"],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.SerialPort\\ObservableAsyncCreateExtensions.cs",
            ["IObservableAsync<MqttClientPublishResult> PublishSerialPort", "IObservableAsync<char> startsWith", "void SubscribeSerialPortWriteLine", "void SubscribeSerialPortWrite", "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort"],
            missing);
        AssertFileContainsAll(
            "MQTTnet.Rx.TwinCAT\\ObservableAsyncCreateExtensions.cs",
            ["IObservableAsync<MqttClientPublishResult> PublishTcPlcTag", "void SubscribeTcTag", "IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag"],
            missing);

        if (missing.Count > 0)
        {
            throw new InvalidOperationException("Missing protocol async counterparts: " + string.Join(", ", missing));
        }

        await Assert.That(missing).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ResilientClientInterface_HasAsyncObservableProperties()
    {
        var interfaceType = typeof(IResilientMqttClient);
        var missing = new List<string>();

        foreach (var property in interfaceType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => IsObservable(property.PropertyType)))
        {
            var asyncProperty = interfaceType.GetProperty(property.Name + "AsyncObservable", BindingFlags.Instance | BindingFlags.Public);
            if (asyncProperty == null || asyncProperty.PropertyType != TranslateObservableType(property.PropertyType))
            {
                missing.Add(property.Name);
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException("Missing resilient client async properties: " + string.Join(", ", missing));
        }

        await Assert.That(missing).Count().IsEqualTo(0);
    }

    private static void AssertAsyncCoverage(Type sourceType, Type asyncType, ICollection<string> missing)
    {
        var candidates = asyncType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var method in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                     .Where(UsesObservableSurface))
        {
            var expectedName = method.GetParameters().Any(parameter => IsObservable(parameter.ParameterType))
                ? method.Name
                : method.Name + "Async";

            var match = candidates.Any(candidate => candidate.Name == expectedName && HasEquivalentAsyncSignature(method, candidate));
            if (!match)
            {
                missing.Add(sourceType.FullName + "." + method.Name);
            }
        }
    }

    private static bool UsesObservableSurface(MethodInfo method) =>
        IsObservable(method.ReturnType) || method.GetParameters().Any(parameter => IsObservable(parameter.ParameterType));

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

    private static bool TypesEquivalent(Type sourceType, Type asyncType)
    {
        if (sourceType.IsGenericParameter)
        {
            return asyncType.IsGenericParameter && sourceType.GenericParameterPosition == asyncType.GenericParameterPosition;
        }

        if (IsObservable(sourceType))
        {
            return asyncType.IsGenericType
                && asyncType.GetGenericTypeDefinition() == typeof(IObservableAsync<>)
                && TypesEquivalent(sourceType.GetGenericArguments()[0], asyncType.GetGenericArguments()[0]);
        }

        if (sourceType.IsArray)
        {
            return asyncType.IsArray && sourceType.GetArrayRank() == asyncType.GetArrayRank() && TypesEquivalent(sourceType.GetElementType()!, asyncType.GetElementType()!);
        }

        if (sourceType.IsGenericType)
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

        return sourceType == asyncType;
    }

    private static Type TranslateObservableType(Type type)
    {
        if (IsObservable(type))
        {
            return typeof(IObservableAsync<>).MakeGenericType(type.GetGenericArguments()[0]);
        }

        return type;
    }

    private static bool IsObservable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>);

    private static DirectoryInfo FindSrcDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !string.Equals(directory.Name, "src", StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent!;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the src directory from the test base path.");
    }

    private static void AssertFileContainsAll(string relativePath, IEnumerable<string> fragments, ICollection<string> missing)
    {
        var filePath = Path.Combine(FindSrcDirectory().FullName, relativePath);
        var content = File.ReadAllText(filePath);
        foreach (var fragment in fragments)
        {
            if (!content.Contains(fragment, StringComparison.Ordinal))
            {
                missing.Add(relativePath + ":" + fragment);
            }
        }
    }
}
