// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Modbus;

/// <summary>Provides compatible static entry points for asynchronous Modbus MQTT sequences.</summary>
/// <remarks>
/// The extension surface provides <c>PublishInputRegisters</c>, <c>PublishHoldingRegisters</c>,
/// <c>PublishInputs</c>, <c>PublishCoils</c>, and <c>PublishModbus</c> overload families for raw and
/// resilient MQTT clients.
/// </remarks>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Gets the legacy callable entry point for creating a sequence from an existing master.</summary>
    public static Func<
        ModbusIpMaster,
        IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)>> FromMasterAsync { get; } =
        FromMaster;

    /// <summary>Gets the legacy callable entry point for creating a sequence from a master factory.</summary>
    public static Func<
        Func<ModbusIpMaster>,
        IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)>> FromFactoryAsync { get; } =
        FromFactory;

    /// <summary>Creates an asynchronous observable sequence from a master factory.</summary>
    /// <param name="factory">Creates the Modbus master.</param>
    /// <returns>The asynchronous Modbus master sequence.</returns>
    private static IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromFactory(
        Func<ModbusIpMaster> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Create.FromFactory(factory).ToSignal();
    }

    /// <summary>Creates an asynchronous observable sequence from an existing master.</summary>
    /// <param name="master">The existing Modbus master.</param>
    /// <returns>The asynchronous Modbus master sequence.</returns>
    private static IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromMaster(
        ModbusIpMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return Create.FromMaster(master).ToSignal();
    }
}
