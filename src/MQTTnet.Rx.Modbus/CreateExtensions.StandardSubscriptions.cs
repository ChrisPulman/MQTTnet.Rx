// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using ModbusMasterState = (bool Connected, System.Exception? Error, IoT.Driver.ModbusRx.Reactive.Device.ModbusIpMaster? Master);
#else
using ModbusMasterState = (bool Connected, System.Exception? Error, IoT.Driver.ModbusRx.Device.ModbusIpMaster? Master);
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides reactive MQTT extensions for Modbus reads and writes.</summary>
public static partial class CreateExtensions
{
    /// <summary>Extends a standard MQTT client sequence with Modbus subscriptions.</summary>
    /// <param name="client">The standard MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Subscribes to MQTT messages and writes parsed values synchronously.</summary>
        /// <typeparam name="T">The parsed value type.</typeparam>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="parse">Parses the MQTT payload.</param>
        /// <param name="writer">Writes a value through the current Modbus master.</param>
        /// <returns>A disposable that ends both subscriptions.</returns>
        public IDisposable SubscribeWrite<T>(
            IObservable<ModbusMasterState> modbus,
            string topic,
            Func<string, T> parse,
            Action<ModbusIpMaster, T> writer)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writer);

            var subscriptions = new MultipleDisposable();
            ModbusIpMaster? latestMaster = null;
            subscriptions.Add(modbus.Subscribe(
                Witness.Create<ModbusMasterState>(state => latestMaster = state.Master)));
            subscriptions.Add(client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(message =>
                {
                    var master = latestMaster;
                    if (master is null)
                    {
                        return;
                    }

                    writer(master, parse(message.ApplicationMessage.ConvertPayloadToString()));
                })));
            return subscriptions;
        }

        /// <summary>Subscribes to MQTT messages and writes parsed values asynchronously.</summary>
        /// <typeparam name="T">The parsed value type.</typeparam>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="parse">Parses the MQTT payload.</param>
        /// <param name="writerAsync">Writes a value asynchronously through the current Modbus master.</param>
        /// <returns>A disposable that ends both subscriptions.</returns>
        public IDisposable SubscribeWrite<T>(
            IObservable<ModbusMasterState> modbus,
            string topic,
            Func<string, T> parse,
            Func<ModbusIpMaster, T, Task> writerAsync)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(modbus);
            ArgumentNullException.ThrowIfNull(parse);
            ArgumentNullException.ThrowIfNull(writerAsync);

            var subscriptions = new MultipleDisposable();
            ModbusIpMaster? latestMaster = null;
            subscriptions.Add(modbus.Subscribe(
                Witness.Create<ModbusMasterState>(state => latestMaster = state.Master)));
            subscriptions.Add(client.SubscribeToTopic(topic)
                .Select(message => (
                    LatestMaster: latestMaster,
                    Value: parse(message.ApplicationMessage.ConvertPayloadToString())))
                .Where(static request => request.LatestMaster is not null)
                .SelectMany(request => Signal.FromAsync(async () =>
                {
                    await writerAsync(request.LatestMaster!, request.Value).ConfigureAwait(false);
                    return true;
                }))
                .Subscribe(Witness.Create<bool>(static _ => { })));
            return subscriptions;
        }

        /// <summary>Subscribes to a single-register write topic.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="address">The register address.</param>
        /// <param name="writer">Writes the address and parsed register value.</param>
        /// <returns>A disposable that ends the subscription.</returns>
        public IDisposable SubscribeWriteSingleRegister(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort address,
            Action<ModbusIpMaster, ushort, ushort> writer) =>
            client.SubscribeWrite(modbus, topic, RegisterParser, (master, value) => writer(master, address, value));

        /// <summary>Subscribes to a multiple-register write topic.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first register address.</param>
        /// <param name="writer">Writes the starting address and parsed register values.</param>
        /// <returns>A disposable that ends the subscription.</returns>
        public IDisposable SubscribeWriteMultipleRegisters(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            Action<ModbusIpMaster, ushort, ushort[]> writer) =>
            client.SubscribeWrite(
                modbus,
                topic,
                RegistersParser,
                (master, values) => writer(master, startAddress, values));

        /// <summary>Subscribes to a single-coil write topic.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="address">The coil address.</param>
        /// <param name="writer">Writes the address and parsed coil value.</param>
        /// <returns>A disposable that ends the subscription.</returns>
        public IDisposable SubscribeWriteSingleCoil(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort address,
            Action<ModbusIpMaster, ushort, bool> writer) =>
            client.SubscribeWrite(modbus, topic, CoilParser, (master, value) => writer(master, address, value));

        /// <summary>Subscribes to a multiple-coil write topic.</summary>
        /// <param name="modbus">The Modbus master state sequence.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="startAddress">The first coil address.</param>
        /// <param name="writer">Writes the starting address and parsed coil values.</param>
        /// <returns>A disposable that ends the subscription.</returns>
        public IDisposable SubscribeWriteMultipleCoils(
            IObservable<ModbusMasterState> modbus,
            string topic,
            ushort startAddress,
            Action<ModbusIpMaster, ushort, bool[]> writer) =>
            client.SubscribeWrite(modbus, topic, CoilsParser, (master, values) => writer(master, startAddress, values));
    }
}
