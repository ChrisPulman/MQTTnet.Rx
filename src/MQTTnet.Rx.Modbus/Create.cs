// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides compatible static entry points for the Modbus MQTT bridge.</summary>
public static class Create
{
    /// <summary>Creates a connected sequence for an existing Modbus master.</summary>
    /// <param name="master">The existing Modbus master.</param>
    /// <returns>A connected Modbus master sequence.</returns>
    public static IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromMaster(
        ModbusIpMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master));
    }

    /// <summary>Creates a connected sequence whose Modbus master has a scoped lifetime.</summary>
    /// <param name="factory">Creates the Modbus master.</param>
    /// <returns>A connected Modbus master sequence.</returns>
    public static IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromFactory(
        Func<ModbusIpMaster> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Signal.Using(
            factory,
            static master =>
                Signal.Emit<(bool Connected, Exception? Error, ModbusIpMaster? Master)>((true, null, master)));
    }

    /// <summary>Publishes input registers through a standard MQTT client using default settings.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes input registers through a standard MQTT client at the specified interval.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes input registers with QoS through a standard MQTT client.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes input registers through a resilient MQTT client using default settings.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes input registers through a resilient MQTT client at the specified interval.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes input registers through a resilient client using the specified quality of service.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes holding registers through a standard MQTT client using default settings.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishHoldingRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes holding registers through a standard MQTT client at the specified interval.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishHoldingRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes holding registers through a standard client using the specified quality of service.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishHoldingRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes holding registers through a resilient MQTT client using default settings.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes holding registers through a resilient MQTT client at the specified interval.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes holding registers with QoS through a resilient MQTT client.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="numberOfPoints">The number of registers.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes discrete inputs through a standard MQTT client using default settings.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputs(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes discrete inputs through a standard MQTT client at the specified interval.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputs(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes discrete inputs through a standard client using the specified quality of service.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishInputs(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes discrete inputs through a resilient MQTT client using default settings.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes discrete inputs through a resilient MQTT client at the specified interval.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes discrete inputs through a resilient client using the specified quality of service.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first input address.</param>
    /// <param name="numberOfPoints">The number of inputs.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishInputs(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes coils through a standard MQTT client using default settings.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishCoils(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes coils through a standard MQTT client at the specified interval.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishCoils(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes coils through a standard client using the specified quality of service.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishCoils(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes coils through a resilient MQTT client using default settings.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints);

    /// <summary>Publishes coils through a resilient MQTT client at the specified interval.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints, interval);

    /// <summary>Publishes coils through a resilient client using the specified quality of service.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="numberOfPoints">The number of coils.</param>
    /// <param name="interval">The polling interval in milliseconds.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        ushort numberOfPoints,
        double interval,
        MqttQualityOfServiceLevel qos) =>
        EnsureClient(client).PublishCoils(modbus, topic, startAddress, numberOfPoints, interval, qos);

    /// <summary>Publishes a transformed reader value through a standard MQTT client using default settings.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory);

    /// <summary>Publishes a transformed reader value through a standard MQTT client.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory,
        MqttQualityOfServiceLevel qos)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory, qos);

    /// <summary>Publishes a transformed reader value through a standard client using explicit MQTT settings.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the MQTT message is retained.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory,
        MqttQualityOfServiceLevel qos,
        bool retain)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory, qos, retain);

    /// <summary>Publishes a transformed reader value through a resilient MQTT client using default settings.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory);

    /// <summary>Publishes a transformed reader value through a resilient MQTT client.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory,
        MqttQualityOfServiceLevel qos)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory, qos);

    /// <summary>Publishes a transformed reader value through a resilient client using explicit MQTT settings.</summary>
    /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="reader">The Modbus reader sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="payloadFactory">Creates the MQTT payload.</param>
    /// <param name="qos">The MQTT quality of service.</param>
    /// <param name="retain">Whether the MQTT message is retained.</param>
    /// <returns>The publish result sequence.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, object? Data)> reader,
        string topic,
        Func<object, TPayload> payloadFactory,
        MqttQualityOfServiceLevel qos,
        bool retain)
        where TPayload : notnull =>
        EnsureClient(client).PublishModbus(reader, topic, payloadFactory, qos, retain);

    /// <summary>Subscribes a standard client to synchronous Modbus writes.</summary>
    /// <typeparam name="T">The parsed value type.</typeparam>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="parse">Parses the MQTT payload.</param>
    /// <param name="writer">Writes the parsed value.</param>
    /// <returns>A disposable that ends both subscriptions.</returns>
    public static IDisposable SubscribeWrite<T>(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        Func<string, T> parse,
        Action<ModbusIpMaster, T> writer) =>
        EnsureClient(client).SubscribeWrite(modbus, topic, parse, writer);

    /// <summary>Subscribes a standard client to asynchronous Modbus writes.</summary>
    /// <typeparam name="T">The parsed value type.</typeparam>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="parse">Parses the MQTT payload.</param>
    /// <param name="writerAsync">Writes the parsed value asynchronously.</param>
    /// <returns>A disposable that ends both subscriptions.</returns>
    public static IDisposable SubscribeWrite<T>(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        Func<string, T> parse,
        Func<ModbusIpMaster, T, Task> writerAsync) =>
        EnsureClient(client).SubscribeWrite(modbus, topic, parse, writerAsync);

    /// <summary>Subscribes a resilient client to synchronous Modbus writes.</summary>
    /// <typeparam name="T">The parsed value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="parse">Parses the MQTT payload.</param>
    /// <param name="writer">Writes the parsed value.</param>
    /// <returns>A disposable that ends both subscriptions.</returns>
    public static IDisposable SubscribeWrite<T>(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        Func<string, T> parse,
        Action<ModbusIpMaster, T> writer) =>
        EnsureClient(client).SubscribeWrite(modbus, topic, parse, writer);

    /// <summary>Subscribes a resilient client to asynchronous Modbus writes.</summary>
    /// <typeparam name="T">The parsed value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="parse">Parses the MQTT payload.</param>
    /// <param name="writerAsync">Writes the parsed value asynchronously.</param>
    /// <returns>A disposable that ends both subscriptions.</returns>
    public static IDisposable SubscribeWrite<T>(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        Func<string, T> parse,
        Func<ModbusIpMaster, T, Task> writerAsync) =>
        EnsureClient(client).SubscribeWrite(modbus, topic, parse, writerAsync);

    /// <summary>Subscribes a standard client to single-register writes.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="address">The register address.</param>
    /// <param name="writer">Writes the address and parsed value.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteSingleRegister(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort address,
        Action<ModbusIpMaster, ushort, ushort> writer) =>
        EnsureClient(client).SubscribeWriteSingleRegister(modbus, topic, address, writer);

    /// <summary>Subscribes a resilient client to single-register writes.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="address">The register address.</param>
    /// <param name="writer">Writes the address and parsed value.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteSingleRegister(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort address,
        Action<ModbusIpMaster, ushort, ushort> writer) =>
        EnsureClient(client).SubscribeWriteSingleRegister(modbus, topic, address, writer);

    /// <summary>Subscribes a standard client to multiple-register writes.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="writer">Writes the starting address and parsed values.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteMultipleRegisters(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        Action<ModbusIpMaster, ushort, ushort[]> writer) =>
        EnsureClient(client).SubscribeWriteMultipleRegisters(modbus, topic, startAddress, writer);

    /// <summary>Subscribes a resilient client to multiple-register writes.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first register address.</param>
    /// <param name="writer">Writes the starting address and parsed values.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteMultipleRegisters(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        Action<ModbusIpMaster, ushort, ushort[]> writer) =>
        EnsureClient(client).SubscribeWriteMultipleRegisters(modbus, topic, startAddress, writer);

    /// <summary>Subscribes a standard client to single-coil writes.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="address">The coil address.</param>
    /// <param name="writer">Writes the address and parsed value.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteSingleCoil(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort address,
        Action<ModbusIpMaster, ushort, bool> writer) =>
        EnsureClient(client).SubscribeWriteSingleCoil(modbus, topic, address, writer);

    /// <summary>Subscribes a resilient client to single-coil writes.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="address">The coil address.</param>
    /// <param name="writer">Writes the address and parsed value.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteSingleCoil(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort address,
        Action<ModbusIpMaster, ushort, bool> writer) =>
        EnsureClient(client).SubscribeWriteSingleCoil(modbus, topic, address, writer);

    /// <summary>Subscribes a standard client to multiple-coil writes.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="writer">Writes the starting address and parsed values.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteMultipleCoils(
        IObservable<IMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        Action<ModbusIpMaster, ushort, bool[]> writer) =>
        EnsureClient(client).SubscribeWriteMultipleCoils(modbus, topic, startAddress, writer);

    /// <summary>Subscribes a resilient client to multiple-coil writes.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="modbus">The Modbus master state sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="startAddress">The first coil address.</param>
    /// <param name="writer">Writes the starting address and parsed values.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    public static IDisposable SubscribeWriteMultipleCoils(
        IObservable<IResilientMqttClient> client,
        IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus,
        string topic,
        ushort startAddress,
        Action<ModbusIpMaster, ushort, bool[]> writer) =>
        EnsureClient(client).SubscribeWriteMultipleCoils(modbus, topic, startAddress, writer);

    /// <summary>Serializes a value to JSON.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON representation.</returns>
    public static string Serialize(object? value) => System.Text.Json.JsonSerializer.Serialize(value);

    /// <summary>Deserializes a JSON string.</summary>
    /// <typeparam name="T">The destination type.</typeparam>
    /// <param name="value">The JSON string.</param>
    /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The deserialized value.</returns>
    public static T? DeSerialize<T>(string value, params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.DeSerialize(typeWitness);
    }

    /// <summary>Validates and returns an MQTT client sequence.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <returns>The validated MQTT client sequence.</returns>
    private static TClient EnsureClient<TClient>(TClient client)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(client);
        return client;
    }
}
