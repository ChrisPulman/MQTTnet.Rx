// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using ModbusRx.Device;
using ModbusRx.Reactive;
using MQTTnet.Rx.Client;
using Newtonsoft.Json;

namespace MQTTnet.Rx.Modbus
{
    /// <summary>
    /// Create.
    /// </summary>
    public static class PublishMixins
    {
        /// <summary>
        /// Publishes the Input Registers.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="modbusMaster">The modbus master.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>
        /// MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// modbusMaster.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishInputRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbusMaster);

            return client.PublishMessage(modbusMaster.ReadInputRegisters(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        }

        /////// <summary>
        /////// Publishes the Input Registers.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="modbusMaster">The modbus master.</param>
        /////// <param name="topic">The publish topic.</param>
        /////// <param name="startAddress">The start address.</param>
        /////// <param name="numberOfPoints">The number of points.</param>
        /////// <param name="interval">The interval.</param>
        /////// <returns>
        /////// ApplicationMessageProcessedEventArgs.
        /////// </returns>
        /////// <exception cref="System.ArgumentNullException">
        /////// client
        /////// or
        /////// modbusMaster.
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters(this IObservable<IManagedMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(modbusMaster);

        ////    return client.PublishMessage(modbusMaster.ReadInputRegisters(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        ////}

        /// <summary>
        /// Publishes the Holding Registers.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="modbusMaster">The modbus master.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>
        /// A MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// modbusMaster.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishHoldingRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbusMaster);

            return client.PublishMessage(modbusMaster.ReadHoldingRegisters(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        }

        /////// <summary>
        /////// Publishes the Holding Registers.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="modbusMaster">The modbus master.</param>
        /////// <param name="topic">The publish topic.</param>
        /////// <param name="startAddress">The start address.</param>
        /////// <param name="numberOfPoints">The number of points.</param>
        /////// <param name="interval">The interval.</param>
        /////// <returns>
        /////// A ApplicationMessageProcessedEventArgs.
        /////// </returns>
        /////// <exception cref="System.ArgumentNullException">
        /////// client
        /////// or
        /////// modbusMaster.
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(this IObservable<IManagedMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(modbusMaster);

        ////    return client.PublishMessage(modbusMaster.ReadHoldingRegisters(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        ////}

        /// <summary>
        /// Publishes the Inputs.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="modbusMaster">The modbus master.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>
        /// A MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// modbusMaster.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishInputs(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbusMaster);

            return client.PublishMessage(modbusMaster.ReadInputs(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        }

        /////// <summary>
        /////// Publishes the Inputs.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="modbusMaster">The modbus master.</param>
        /////// <param name="topic">The publish topic.</param>
        /////// <param name="startAddress">The start address.</param>
        /////// <param name="numberOfPoints">The number of points.</param>
        /////// <param name="interval">The interval.</param>
        /////// <returns>
        /////// A ApplicationMessageProcessedEventArgs.
        /////// </returns>
        /////// <exception cref="System.ArgumentNullException">
        /////// client
        /////// or
        /////// modbusMaster.
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs(this IObservable<IManagedMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(modbusMaster);

        ////    return client.PublishMessage(modbusMaster.ReadInputs(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        ////}

        /// <summary>
        /// Publishes the Coils.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="modbusMaster">The modbus master.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>
        /// A MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// modbusMaster.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishCoils(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbusMaster);

            return client.PublishMessage(modbusMaster.ReadCoils(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        }

        /////// <summary>
        /////// Publishes the Coils.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="modbusMaster">The modbus master.</param>
        /////// <param name="topic">The publish topic.</param>
        /////// <param name="startAddress">The start address.</param>
        /////// <param name="numberOfPoints">The number of points.</param>
        /////// <param name="interval">The interval.</param>
        /////// <returns>
        /////// A ApplicationMessageProcessedEventArgs.
        /////// </returns>
        /////// <exception cref="System.ArgumentNullException">
        /////// client
        /////// or
        /////// modbusMaster.
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils(this IObservable<IManagedMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbusMaster, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(modbusMaster);

        ////    return client.PublishMessage(modbusMaster.ReadCoils(startAddress, numberOfPoints, interval).Select(d => d.data!.Serialize()).Select(payLoad => (topic, payLoad)));
        ////}

        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string Serialize(this object? value) =>
            JsonConvert.SerializeObject(value);

        /// <summary>
        /// Deserializes the JSON to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="value">The JSON to deserialize.</param>
        /// <returns>
        /// The deserialized object from the JSON string.
        /// </returns>
        public static T? DeSerialize<T>(this string value) =>
            JsonConvert.DeserializeObject<T>(value);
    }
}
