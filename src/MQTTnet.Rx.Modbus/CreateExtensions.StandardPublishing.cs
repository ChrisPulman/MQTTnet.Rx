// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Reactive;
using ModbusCreateExtensions = IoT.Driver.ModbusRx.CreateExtensions;
using ModbusMasterState = (bool Connected, System.Exception? Error, IoT.Driver.ModbusRx.Device.ModbusIpMaster? Master);
using ModbusReaderState = (bool Connected, System.Exception? Error, object? Data);

namespace MQTTnet.Rx.Modbus;

/// <summary>Provides reactive MQTT extensions for Modbus reads and writes.</summary>
public static partial class CreateExtensions
{
    /// <summary>Extends a standard MQTT client sequence with Modbus operations.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes input registers with the default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, DefaultInterval);

        /// <summary>Publishes input registers using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputRegisters(
            IObservable<ModbusMasterState> modbus,
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
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishInputRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos, false);

        /// <summary>Publishes input registers using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                ModbusCreateExtensions.ReadInputRegisters(modbus, startAddress, numberOfPoints, interval)
                    .Select(static reading => reading.Data!.Serialize())
                    .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>Publishes holding registers with the default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishHoldingRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, DefaultInterval);

        /// <summary>Publishes holding registers using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishHoldingRegisters(
            IObservable<ModbusMasterState> modbus,
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
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishHoldingRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishHoldingRegisters(modbus, topic, startAddress, numberOfPoints, interval, qos, false);

        /// <summary>Publishes holding registers using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="numberOfPoints">The number of registers.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishHoldingRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                ModbusCreateExtensions.ReadHoldingRegisters(modbus, startAddress, numberOfPoints, interval)
                    .Select(static reading => reading.Data!.Serialize())
                    .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>Publishes discrete inputs with the default MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputs(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishInputs(modbus, topic, startAddress, numberOfPoints, DefaultInterval);

        /// <summary>Publishes discrete inputs using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputs(
            IObservable<ModbusMasterState> modbus,
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
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputs(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishInputs(modbus, topic, startAddress, numberOfPoints, interval, qos, false);

        /// <summary>Publishes discrete inputs using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first input address.</param>
        /// <param name="numberOfPoints">The number of inputs.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishInputs(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                ModbusCreateExtensions.ReadInputs(modbus, startAddress, numberOfPoints, interval)
                    .Select(static reading => reading.Data!.Serialize())
                    .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>Publishes coils using the default interval, quality of service, and retain flag.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishCoils(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints) =>
            client.PublishCoils(modbus, topic, startAddress, numberOfPoints, DefaultInterval);

        /// <summary>Publishes coils using the specified interval.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishCoils(
            IObservable<ModbusMasterState> modbus,
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
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishCoils(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos) =>
            client.PublishCoils(modbus, topic, startAddress, numberOfPoints, interval, qos, false);

        /// <summary>Publishes coils using the specified MQTT settings.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="numberOfPoints">The number of coils.</param>
        /// <param name="interval">The polling interval in milliseconds.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishCoils(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            ushort numberOfPoints,
            double interval,
            MqttQualityOfServiceLevel qos,
            bool retain)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                ModbusCreateExtensions.ReadCoils(modbus, startAddress, numberOfPoints, interval)
                    .Select(static reading => reading.Data!.Serialize())
                    .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>Publishes reader data using default MQTT settings.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
            IObservable<ModbusReaderState> reader,
            string topic,
            Func<object, TPayload> payloadFactory)
            where TPayload : notnull =>
            client.PublishModbus(reader, topic, payloadFactory, MqttQualityOfServiceLevel.AtLeastOnce);

        /// <summary>Publishes reader data using the specified quality of service.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
            IObservable<ModbusReaderState> reader,
            string topic,
            Func<object, TPayload> payloadFactory,
            MqttQualityOfServiceLevel qos)
            where TPayload : notnull =>
            client.PublishModbus(reader, topic, payloadFactory, qos, false);

        /// <summary>Publishes reader data using the specified MQTT settings.</summary>
        /// <typeparam name="TPayload">The string or byte-array payload type.</typeparam>
        /// <param name="reader">The Modbus reader sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payloadFactory">Creates the MQTT payload.</param>
        /// <param name="qos">The MQTT quality of service.</param>
        /// <param name="retain">Whether the MQTT message is retained.</param>
        /// <returns>The publish result sequence.</returns>
        /// <exception cref="NotSupportedException">Thrown when the payload type is unsupported.</exception>
        public IObservable<MqttClientPublishResult> PublishModbus<TPayload>(
            IObservable<ModbusReaderState> reader,
            string topic,
            Func<object, TPayload> payloadFactory,
            MqttQualityOfServiceLevel qos,
            bool retain)
            where TPayload : notnull
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            if (typeof(TPayload) == typeof(string))
            {
                return client.PublishMessage(
                    reader.Where(static value => value.Data is not null)
                        .Select(value => (topic, (string)(object)payloadFactory(value.Data!))),
                    qos,
                    retain);
            }

            if (typeof(TPayload) == typeof(byte[]))
            {
                return client.PublishMessage(
                    reader.Where(static value => value.Data is not null)
                        .Select(value => (topic, (byte[])(object)payloadFactory(value.Data!))),
                    qos,
                    retain);
            }

            throw new NotSupportedException("TPayload must be string or byte[].");
        }
    }
}
