// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ModbusRx.Device;
using ModbusRx.Reactive;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using Newtonsoft.Json;

namespace MQTTnet.Rx.Modbus
{
    /// <summary>
    /// Create Modbus MQTT helpers.
    /// </summary>
    public static class Create
    {
        //// --------------------------
        //// Helper: master providers
        //// --------------------------

        /// <summary>
        /// Wraps a pre-configured ModbusIpMaster into the expected state observable.
        /// </summary>
        /// <param name="master">The pre-configured master.</param>
        /// <returns>Observable yielding (connected, error, master).</returns>
        public static IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> FromMaster(ModbusIpMaster master)
        {
            ArgumentNullException.ThrowIfNull(master);
            return Observable.Return<(bool connected, Exception? error, ModbusIpMaster? master)>((true, null, master));
        }

        /// <summary>
        /// Wraps a factory for creating a ModbusIpMaster into the expected state observable.
        /// The created master instance is disposed when the subscription is disposed if it implements IDisposable.
        /// </summary>
        /// <param name="factory">Factory creating a configured master.</param>
        /// <returns>Observable yielding (connected, error, master).</returns>
        public static IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> FromFactory(Func<ModbusIpMaster> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            return Observable.Using(factory, m => Observable.Return<(bool connected, Exception? error, ModbusIpMaster? master)>((true, null, m)));
        }

        //// --------------------------
        //// Publish (raw client)
        //// --------------------------

        /// <summary>
        /// Publishes the Input Registers.
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>MQTT publish result observable.</returns>
        public static IObservable<MqttClientPublishResult> PublishInputRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadInputRegisters(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes the Holding Registers.
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>MQTT publish result observable.</returns>
        public static IObservable<MqttClientPublishResult> PublishHoldingRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadHoldingRegisters(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes Discrete Inputs.
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>MQTT publish result observable.</returns>
        public static IObservable<MqttClientPublishResult> PublishInputs(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadInputs(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes Coils.
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>MQTT publish result observable.</returns>
        public static IObservable<MqttClientPublishResult> PublishCoils(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadCoils(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes with a custom payload factory (raw client). The factory maps the Modbus read result to either string or byte[] payloads.
        /// </summary>
        /// <typeparam name="TPayload">Type of payload; supported: string or byte[].</typeparam>
        /// <param name="client">The MQTT client.</param>
        /// <param name="reader">A Modbus read observable (e.g., modbus.ReadHoldingRegisters(...)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="payloadFactory">Factory that creates the payload from the read data object.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>MQTT publish result observable.</returns>
        public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, object? data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
            where TPayload : notnull
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            if (typeof(TPayload) == typeof(string))
            {
                return client.PublishMessage(
                    reader.Where(t => t.data != null).Select(d => (topic, (string)(object)payloadFactory(d.data!))),
                    qos,
                    retain);
            }

            if (typeof(TPayload) == typeof(byte[]))
            {
                return client.PublishMessage(
                    reader.Where(t => t.data != null).Select(d => (topic, (byte[])(object)payloadFactory(d.data!))),
                    qos,
                    retain);
            }

            throw new NotSupportedException("TPayload must be string or byte[]");
        }

        //// --------------------------
        //// Publish (resilient client)
        //// --------------------------

        /// <summary>
        /// Publishes Input Registers using the resilient client.
        /// </summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>ApplicationMessageProcessed events.</returns>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadInputRegisters(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes Holding Registers using the resilient client.
        /// </summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>ApplicationMessageProcessed events.</returns>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadHoldingRegisters(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes Discrete Inputs using the resilient client.
        /// </summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>ApplicationMessageProcessed events.</returns>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadInputs(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes Coils using the resilient client.
        /// </summary>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="startAddress">The start address.</param>
        /// <param name="numberOfPoints">The number of points.</param>
        /// <param name="interval">The poll interval in milliseconds.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>ApplicationMessageProcessed events.</returns>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval = 100.0, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);

            return client.PublishMessage(
                modbus.ReadCoils(startAddress, numberOfPoints, interval)
                      .Select(d => d.data!.Serialize())
                      .Select(payload => (topic, payload)),
                qos,
                retain);
        }

        /// <summary>
        /// Publishes with a custom payload factory (resilient client). The factory maps the Modbus read result to either string or byte[] payloads.
        /// </summary>
        /// <typeparam name="TPayload">Type of payload; supported: string or byte[].</typeparam>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="reader">A Modbus read observable.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="payloadFactory">Factory that creates the payload from the read data object.</param>
        /// <param name="qos">QoS level.</param>
        /// <param name="retain">Retain flag.</param>
        /// <returns>ApplicationMessageProcessed events.</returns>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, object? data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce, bool retain = false)
            where TPayload : notnull
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            if (typeof(TPayload) == typeof(string))
            {
                return client.PublishMessage(
                    reader.Where(t => t.data != null).Select(d => (topic, (string)(object)payloadFactory(d.data!))),
                    qos,
                    retain);
            }

            if (typeof(TPayload) == typeof(byte[]))
            {
                return client.PublishMessage(
                    reader.Where(t => t.data != null).Select(d => (topic, (byte[])(object)payloadFactory(d.data!))),
                    qos,
                    retain);
            }

            throw new NotSupportedException("TPayload must be string or byte[]");
        }

        //// --------------------------
        //// Subscribe/write (MQTT -> Modbus)
        //// --------------------------

        /// <summary>
        /// Subscribes to a topic and forwards parsed MQTT payloads to a Modbus write action.
        /// </summary>
        /// <typeparam name="T">Parsed payload type.</typeparam>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="parse">Parser from MQTT payload string to value.</param>
        /// <param name="writer">Action that receives (master, value) and performs the write.</param>
        /// <returns>A disposable subscription.</returns>
        public static IDisposable SubscribeWrite<T>(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writer);

            var cd = new CompositeDisposable();
            var latestMaster = default(ModbusIpMaster);
            cd.Add(modbus.Subscribe(s => latestMaster = s.master!));
            cd.Add(client.SubscribeToTopic(topic).Subscribe(m =>
            {
                var payload = m.ApplicationMessage.ConvertPayloadToString();
                var value = parse(payload);
                if (latestMaster != null)
                {
                    writer(latestMaster, value);
                }
            }));
            return cd;
        }

        /// <summary>
        /// Subscribes to a topic and forwards parsed MQTT payloads to an async Modbus write function.
        /// </summary>
        /// <typeparam name="T">Parsed payload type.</typeparam>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state (IObservable of (connected, error, master)).</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="parse">Parser from MQTT payload string to value.</param>
        /// <param name="writerAsync">Async function that receives (master, value) and performs the write.</param>
        /// <returns>A disposable subscription.</returns>
        public static IDisposable SubscribeWrite<T>(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writerAsync);

            var cd = new CompositeDisposable();
            var latestMaster = default(ModbusIpMaster);
            cd.Add(modbus.Subscribe(s => latestMaster = s.master!));
            cd.Add(client.SubscribeToTopic(topic).Subscribe(async m =>
            {
                var payload = m.ApplicationMessage.ConvertPayloadToString();
                var value = parse(payload);
                if (latestMaster != null)
                {
                    await writerAsync(latestMaster, value).ConfigureAwait(false);
                }
            }));
            return cd;
        }

        //// --------------------------
        //// JSON helpers
        //// --------------------------

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
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T? DeSerialize<T>(this string value) =>
            JsonConvert.DeserializeObject<T>(value);

        // --------------------------
        // Subscribe/write helpers (common operations) - RAW CLIENT
        // --------------------------
        /// <summary>
        /// Subscribes to topic and writes a single register (default parse: ushort.Parse, InvariantCulture).
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="address">Register address to write.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteSingleRegister(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort> writer) =>
            client.SubscribeWrite(modbus, topic, s => ushort.Parse(s, CultureInfo.InvariantCulture), (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes multiple registers parsed from CSV (e.g. "1,2,3").
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="startAddress">Starting register address.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteMultipleRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort[]> writer) =>
            client.SubscribeWrite(
                modbus,
                topic,
                s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => ushort.Parse(x.Trim(), CultureInfo.InvariantCulture))
                      .ToArray(),
                (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes a single coil (default parse: bool.Parse).
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="address">Coil address to write.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteSingleCoil(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, bool> writer) =>
            client.SubscribeWrite(modbus, topic, s => bool.Parse(s), (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes multiple coils parsed from CSV (e.g. "true,false,true").
        /// </summary>
        /// <param name="client">The MQTT client.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="startAddress">Starting coil address.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteMultipleCoils(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, bool[]> writer) =>
            client.SubscribeWrite(
                modbus,
                topic,
                s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => bool.Parse(x.Trim()))
                      .ToArray(),
                (m, v) => writer(m, v));

        //// --------------------------
        //// Subscribe/write helpers (common operations) - RESILIENT CLIENT
        //// --------------------------

        /// <summary>
        /// Subscribes to topic and writes a single register (resilient client; default parse ushort.Parse).
        /// </summary>
        /// <param name="client">The resilient client observable.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="address">Register address to write.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteSingleRegister(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort> writer) =>
            client.SubscribeWrite(modbus, topic, s => ushort.Parse(s, CultureInfo.InvariantCulture), (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes multiple registers from CSV (resilient client).
        /// </summary>
        /// <param name="client">The resilient client observable.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="startAddress">Starting register address.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteMultipleRegisters(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort[]> writer) =>
            client.SubscribeWrite(
                modbus,
                topic,
                s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => ushort.Parse(x.Trim(), CultureInfo.InvariantCulture))
                      .ToArray(),
                (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes a single coil (resilient client; default parse bool.Parse).
        /// </summary>
        /// <param name="client">The resilient client observable.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="address">Coil address to write.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteSingleCoil(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, bool> writer) =>
            client.SubscribeWrite(modbus, topic, s => bool.Parse(s), (m, v) => writer(m, v));

        /// <summary>
        /// Subscribes to topic and writes multiple coils from CSV (resilient client).
        /// </summary>
        /// <param name="client">The resilient client observable.</param>
        /// <param name="modbus">The Modbus master state observable.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="startAddress">Starting coil address.</param>
        /// <param name="writer">Writer action performing the actual write.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWriteMultipleCoils(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, bool[]> writer) =>
            client.SubscribeWrite(
                modbus,
                topic,
                s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => bool.Parse(x.Trim()))
                      .ToArray(),
                (m, v) => writer(m, v));

        //// --------------------------
        //// Delegate-based generic subscribe for resilient client
        //// --------------------------

        /// <summary>
        /// Subscribes to a topic and forwards parsed MQTT payloads to a Modbus write action (resilient client).
        /// </summary>
        /// <typeparam name="T">Parsed payload type.</typeparam>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="parse">Parser from payload to value.</param>
        /// <param name="writer">Writer delegate.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWrite<T>(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writer);

            var cd = new CompositeDisposable();
            var latestMaster = default(ModbusIpMaster);
            cd.Add(modbus.Subscribe(s => latestMaster = s.master!));
            cd.Add(client.SubscribeToTopic(topic).Subscribe(m =>
            {
                var payload = m.ApplicationMessage.ConvertPayloadToString();
                var value = parse(payload);
                if (latestMaster != null)
                {
                    writer(latestMaster, value);
                }
            }));
            return cd;
        }

        /// <summary>
        /// Subscribes to a topic and forwards parsed MQTT payloads to an async Modbus write function (resilient client).
        /// </summary>
        /// <typeparam name="T">Parsed payload type.</typeparam>
        /// <param name="client">The resilient MQTT client.</param>
        /// <param name="modbus">The Modbus master state.</param>
        /// <param name="topic">The topic to subscribe.</param>
        /// <param name="parse">Parser from payload to value.</param>
        /// <param name="writerAsync">Async writer delegate.</param>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable SubscribeWrite<T>(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writerAsync);

            var cd = new CompositeDisposable();
            var latestMaster = default(ModbusIpMaster);
            cd.Add(modbus.Subscribe(s => latestMaster = s.master!));
            cd.Add(client.SubscribeToTopic(topic).Subscribe(async m =>
            {
                var payload = m.ApplicationMessage.ConvertPayloadToString();
                var value = parse(payload);
                if (latestMaster != null)
                {
                    await writerAsync(latestMaster, value).ConfigureAwait(false);
                }
            }));
            return cd;
        }
    }
}
