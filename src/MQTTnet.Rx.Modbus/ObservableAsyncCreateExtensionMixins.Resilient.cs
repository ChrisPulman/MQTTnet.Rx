// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;
using ModbusMasterSignal = ReactiveUI.Primitives.Async.IObservableAsync<
    (bool Connected, System.Exception? Error, IoT.Driver.ModbusRx.Device.ModbusIpMaster? Master)>;
using ModbusReaderSignal = ReactiveUI.Primitives.Async.IObservableAsync<
    (bool Connected, System.Exception? Error, object? Data)>;
using ResilientResult = ReactiveUI.Primitives.Async.IObservableAsync<
    MQTTnet.Rx.Client.ApplicationMessageProcessedEventArgs>;

namespace MQTTnet.Rx.Modbus;

/// <summary>Provides asynchronous observable extensions for resilient Modbus MQTT clients.</summary>
public static partial class ObservableAsyncCreateExtensionMixins
{
    /// <summary>Extends an asynchronous resilient MQTT client sequence.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes input registers using default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input-register address.</param>
        /// <param name="numberOfPoints">The number of input registers.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishInputRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                DefaultInterval);

        /// <summary>Publishes input registers using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input-register address.</param>
        /// <param name="numberOfPoints">The number of input registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval) =>
            client.PublishInputRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes input registers using the specified interval and quality of service.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input-register address.</param>
        /// <param name="numberOfPoints">The number of input registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishInputRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                qos,
                false);

        /// <summary>Publishes input registers using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input-register address.</param>
        /// <param name="numberOfPoints">The number of input registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            return client.ToObservable()
                .PublishInputRegisters(
                    modbus.ToObservable(),
                    topic,
                    startAddress,
                    numberOfPoints,
                    interval,
                    qos,
                    retain)
                .ToSignal();
        }

        /// <summary>Publishes holding registers using default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first holding-register address.</param>
        /// <param name="numberOfPoints">The number of holding registers.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishHoldingRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishHoldingRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                DefaultInterval);

        /// <summary>Publishes holding registers using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first holding-register address.</param>
        /// <param name="numberOfPoints">The number of holding registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishHoldingRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval) =>
            client.PublishHoldingRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes holding registers using the specified interval and quality of service.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first holding-register address.</param>
        /// <param name="numberOfPoints">The number of holding registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishHoldingRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishHoldingRegisters(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                qos,
                false);

        /// <summary>Publishes holding registers using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first holding-register address.</param>
        /// <param name="numberOfPoints">The number of holding registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishHoldingRegisters(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            return client.ToObservable()
                .PublishHoldingRegisters(
                    modbus.ToObservable(),
                    topic,
                    startAddress,
                    numberOfPoints,
                    interval,
                    qos,
                    retain)
                .ToSignal();
        }

        /// <summary>Publishes discrete inputs using default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputs(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishInputs(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                DefaultInterval);

        /// <summary>Publishes discrete inputs using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputs(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval) =>
            client.PublishInputs(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes discrete inputs using the specified interval and quality of service.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputs(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishInputs(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                qos,
                false);

        /// <summary>Publishes discrete inputs using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishInputs(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            return client.ToObservable()
                .PublishInputs(
                    modbus.ToObservable(),
                    topic,
                    startAddress,
                    numberOfPoints,
                    interval,
                    qos,
                    retain)
                .ToSignal();
        }

        /// <summary>Publishes coils using default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishCoils(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishCoils(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                DefaultInterval);

        /// <summary>Publishes coils using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishCoils(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval) =>
            client.PublishCoils(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes coils using the specified interval and quality of service.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishCoils(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishCoils(
                modbus,
                topic,
                startAddress,
                numberOfPoints,
                interval,
                qos,
                false);

        /// <summary>Publishes coils using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishCoils(
            ModbusMasterSignal modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            return client.ToObservable()
                .PublishCoils(
                    modbus.ToObservable(),
                    topic,
                    startAddress,
                    numberOfPoints,
                    interval,
                    qos,
                    retain)
                .ToSignal();
        }

        /// <summary>Publishes reader data using default MQTT settings.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishModbus<TPayload>(
            ModbusReaderSignal reader,
            string topic,
            Func<object, TPayload> payloadFactory)
            where TPayload : notnull => client.PublishModbus(
                reader,
                topic,
                payloadFactory,
                MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes reader data using the specified quality of service.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishModbus<TPayload>(
            ModbusReaderSignal reader,
            string topic,
            Func<object, TPayload> payloadFactory,
            MqttQualityOfServiceLevel qos)
            where TPayload : notnull => client.PublishModbus(
                reader,
                topic,
                payloadFactory,
                qos,
                false);

        /// <summary>Publishes reader data using the specified MQTT settings.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The asynchronous resilient publish result sequence.</returns>
        public ResilientResult PublishModbus<TPayload>(
            ModbusReaderSignal reader,
            string topic,
            Func<object, TPayload> payloadFactory,
            MqttQualityOfServiceLevel qos,
            bool retain)
            where TPayload : notnull
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            return client.ToObservable()
                .PublishModbus(
                    reader.ToObservable(),
                    topic,
                    payloadFactory,
                    qos,
                    retain)
                .ToSignal();
        }
    }
}
