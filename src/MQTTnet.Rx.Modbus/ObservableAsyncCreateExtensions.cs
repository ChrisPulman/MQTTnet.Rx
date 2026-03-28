// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ModbusRx.Device;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.Modbus;

/// <summary>
/// Provides asynchronous observable counterparts for Modbus MQTT helpers.
/// </summary>
public static class ObservableAsyncCreateExtensions
{
    public static IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> FromMasterAsync(ModbusIpMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return Create.FromMaster(master).ToObservableAsync();
    }

    public static IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> FromFactoryAsync(Func<ModbusIpMaster> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Create.FromFactory(factory).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishInputRegisters(this IObservableAsync<IMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishInputRegisters(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishHoldingRegisters(this IObservableAsync<IMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishHoldingRegisters(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishInputs(this IObservableAsync<IMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishInputs(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishCoils(this IObservableAsync<IMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishCoils(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<MqttClientPublishResult> PublishModbus<TPayload>(this IObservableAsync<IMqttClient> client, IObservableAsync<(bool connected, Exception? error, object? data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        where TPayload : notnull
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(reader);
        return Create.PublishModbus(client.ToObservable(), reader.ToObservable(), topic, payloadFactory, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputRegisters(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishInputRegisters(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishHoldingRegisters(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputs(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishInputs(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishCoils(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modbus);
        return Create.PublishCoils(client.ToObservable(), modbus.ToObservable(), topic, startAddress, numberOfPoints, interval, qos, retain).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>(this IObservableAsync<IResilientMqttClient> client, IObservableAsync<(bool connected, Exception? error, object? data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        where TPayload : notnull
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(reader);
        return Create.PublishModbus(client.ToObservable(), reader.ToObservable(), topic, payloadFactory, qos, retain).ToObservableAsync();
    }
}
