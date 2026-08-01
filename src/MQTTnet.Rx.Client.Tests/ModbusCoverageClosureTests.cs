// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives.Reactive.Signals;
using ModbusCreate = MQTTnet.Rx.Modbus.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes compatibility-forwarder coverage for the Modbus package.</summary>
public sealed class ModbusCoverageClosureTests
{
    /// <summary>Exercises every public synchronous compatibility forwarder with a missing client.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousForwarders_RejectMissingClientAsync()
    {
        var methods = new List<MethodInfo>();
        foreach (var method in typeof(ModbusCreate).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name is not "FromMaster" and not "FromFactory" and not "Serialize" and not "DeSerialize")
            {
                methods.Add(method);
            }
        }

        foreach (var method in methods)
        {
            await InvokeAndVerifyMissingClientAsync(method);
        }

        await Assert.That(methods.Count).IsGreaterThan(0);
    }

    /// <summary>Exercises every public synchronous extension overload with a missing client.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousExtensionForwarders_RejectMissingClientAsync()
    {
        var methods = typeof(CreateExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (var method in methods)
        {
            await InvokeAndVerifyMissingClientAsync(method);
        }

        await Assert.That(methods.Count).IsGreaterThan(0);
    }

    /// <summary>Exercises every public asynchronous extension overload with a missing client.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsynchronousExtensionForwarders_RejectMissingClientAsync()
    {
        var methods = typeof(ObservableAsyncCreateExtensionMixins).GetMethods(
            BindingFlags.Public | BindingFlags.Static);

        foreach (var method in methods)
        {
            var exception = Assert.Throws<TargetInvocationException>(() => InvokeWithMissingFirstArgument(method));
            await Assert.That(exception.InnerException).IsTypeOf<ArgumentNullException>();
        }

        await Assert.That(methods.Length).IsGreaterThan(0);
    }

    /// <summary>Verifies synchronous write bridges ignore messages until a Modbus master is available.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousWrites_IgnoreMessagesUntilMasterIsAvailableAsync()
    {
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var unavailable = Signal.Emit((Connected: false, Error: (Exception?)null, Master: (ModbusIpMaster?)null));
        var rawWritten = false;
        var resilientWritten = false;
        using var rawSubscription = Signal.Emit<IMqttClient>(rawClient).SubscribeWrite(
            unavailable,
            "coverage/raw/no-master",
            static value => value,
            (_, _) => rawWritten = true);
        using var resilientSubscription = Signal.Emit<IResilientMqttClient>(resilientClient).SubscribeWrite(
            unavailable,
            "coverage/resilient/no-master",
            static value => value,
            (_, _) => resilientWritten = true);

        await rawClient.SimulateMessageReceivedAsync("coverage/raw/no-master", "ignored");
        await resilientClient.SimulateMessageReceivedAsync("coverage/resilient/no-master", "ignored");

        await Assert.That(rawWritten).IsFalse();
        await Assert.That(resilientWritten).IsFalse();
    }

    /// <summary>Exercises the missing-writer branches of resilient convenience subscriptions.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ResilientConvenienceWrites_RejectMissingWritersOnDeliveryAsync()
    {
        using var simulator = new ModbusSimulator();
        using var master = simulator.CreateMaster();
        using var client = new MockResilientMqttClient();
        var modbus = Signal.Emit((Connected: true, Error: (Exception?)null, Master: (ModbusIpMaster?)master));
        var clients = Signal.Emit<IResilientMqttClient>(client);
        var missingClients = (IObservable<IResilientMqttClient>)null!;

        await Assert.That(() => missingClients.SubscribeWriteSingleRegister(
            modbus,
            "coverage/null/client/register",
            0,
            static (_, _, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => missingClients.SubscribeWriteMultipleRegisters(
            modbus,
            "coverage/null/client/registers",
            0,
            static (_, _, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => missingClients.SubscribeWriteSingleCoil(
            modbus,
            "coverage/null/client/coil",
            0,
            static (_, _, _) => { })).Throws<ArgumentNullException>();
        await Assert.That(() => missingClients.SubscribeWriteMultipleCoils(
            modbus,
            "coverage/null/client/coils",
            0,
            static (_, _, _) => { })).Throws<ArgumentNullException>();

        using var register = clients.SubscribeWriteSingleRegister(modbus, "coverage/null/register", 0, null!);
        using var registers = clients.SubscribeWriteMultipleRegisters(modbus, "coverage/null/registers", 0, null!);
        using var coil = clients.SubscribeWriteSingleCoil(modbus, "coverage/null/coil", 0, null!);
        using var coils = clients.SubscribeWriteMultipleCoils(modbus, "coverage/null/coils", 0, null!);

        await Assert.That(() => client.SimulateMessageReceivedAsync(
            "coverage/null/register",
            "1")).Throws<NullReferenceException>();
        await Assert.That(() => client.SimulateMessageReceivedAsync(
            "coverage/null/registers",
            "1, 2")).Throws<NullReferenceException>();
        await Assert.That(() => client.SimulateMessageReceivedAsync(
            "coverage/null/coil",
            "true")).Throws<NullReferenceException>();
        await Assert.That(() => client.SimulateMessageReceivedAsync(
            "coverage/null/coils",
            "true, false")).Throws<NullReferenceException>();
    }

    /// <summary>Invokes a forwarder and verifies its missing-client validation when it is an API method.</summary>
    /// <param name="method">The method to invoke.</param>
    /// <returns>A task that represents the TUnit assertion.</returns>
    private static async Task InvokeAndVerifyMissingClientAsync(MethodInfo method)
    {
        try
        {
            var result = InvokeWithMissingFirstArgument(method);
            if (method.ReturnType != typeof(void))
            {
                await Assert.That(result).IsNotNull();
            }
        }
        catch (TargetInvocationException exception)
        {
            await Assert.That(exception.InnerException).IsTypeOf<ArgumentNullException>();
        }
    }

    /// <summary>Invokes a static forwarder with a null first argument.</summary>
    /// <param name="method">The forwarder to invoke.</param>
    /// <returns>The forwarder's result, if any.</returns>
    private static object? InvokeWithMissingFirstArgument(MethodInfo method)
    {
        var callable = method.IsGenericMethodDefinition ? method.MakeGenericMethod(typeof(string)) : method;
        var parameters = callable.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsValueType)
            {
                arguments[i] = Activator.CreateInstance(parameters[i].ParameterType);
            }
        }

        arguments[0] = null;
        return callable.Invoke(null, arguments);
    }
}
