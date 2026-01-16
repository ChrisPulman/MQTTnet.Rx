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

namespace MQTTnet.Rx.Modbus;

/// <summary>
/// Provides extension methods and helpers for integrating Modbus TCP masters with MQTT clients using reactive
/// programming patterns. Includes utilities for publishing Modbus data to MQTT topics, subscribing to MQTT topics
/// to perform Modbus writes, and JSON serialization helpers.
/// </summary>
/// <remarks>The Create class enables seamless bridging between Modbus TCP and MQTT by exposing methods
/// that wrap Modbus master state as observables, publish Modbus register and coil data to MQTT, and subscribe to
/// MQTT topics to trigger Modbus write operations. Both standard and resilient MQTT client types are supported.
/// Methods are designed for use in reactive pipelines and support both synchronous and asynchronous write
/// scenarios. All methods require non-null arguments unless otherwise specified. Thread safety and disposal
/// semantics depend on the underlying observables and client implementations.</remarks>
public static class Create
{
    /// <summary>
    /// Creates an observable sequence that emits a tuple indicating a successful connection using the specified
    /// ModbusIpMaster instance.
    /// </summary>
    /// <param name="master">The ModbusIpMaster instance to be wrapped in the observable sequence. Cannot be null.</param>
    /// <returns>An observable sequence that emits a single tuple with connected set to <see langword="true"/>, error set to
    /// null, and the provided master instance.</returns>
    public static IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> FromMaster(ModbusIpMaster master)
    {
        ArgumentNullException.ThrowIfNull(master);
        return Observable.Return<(bool connected, Exception? error, ModbusIpMaster? master)>((true, null, master));
    }

    /// <summary>
    /// Creates an observable sequence that provides a ModbusIpMaster instance from the specified factory, along with
    /// its connection status and any associated error.
    /// </summary>
    /// <param name="factory">A delegate that returns a new ModbusIpMaster instance to be used by the observable sequence. Cannot be null.</param>
    /// <returns>An observable sequence that emits a tuple containing a value indicating whether the connection was successful,
    /// an exception if an error occurred, and the created ModbusIpMaster instance.</returns>
    public static IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> FromFactory(Func<ModbusIpMaster> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Observable.Using(factory, m => Observable.Return<(bool connected, Exception? error, ModbusIpMaster? master)>((true, null, m)));
    }

    /// <summary>
    /// Publishes Modbus input register values to an MQTT topic at a specified interval.
    /// </summary>
    /// <remarks>The method reads the specified range of Modbus input registers at the given interval and
    /// publishes their values as MQTT messages. If the Modbus connection is lost or an error occurs, the observable may
    /// emit errors or incomplete data. The caller is responsible for handling connection state and error propagation as
    /// needed.</remarks>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, errors, and master instance used to read input registers.</param>
    /// <param name="topic">The MQTT topic to which the input register values are published.</param>
    /// <param name="startAddress">The starting address of the Modbus input registers to read.</param>
    /// <param name="numberOfPoints">The number of input registers to read and publish from the starting address.</param>
    /// <param name="interval">The interval, in milliseconds, between consecutive Modbus read and publish operations. Must be greater than
    /// zero.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages.</param>
    /// <param name="retain">true to set the MQTT retain flag on published messages; otherwise, false.</param>
    /// <returns>An observable sequence of results for each MQTT publish operation.</returns>
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
    /// Publishes Modbus holding register values to an MQTT topic at a specified interval.
    /// </summary>
    /// <remarks>The method reads the specified range of Modbus holding registers at the given interval and
    /// publishes their serialized values to the specified MQTT topic. The observable emits a result for each publish
    /// attempt. If the Modbus connection is not established or an error occurs, the publish operation may not proceed
    /// until the connection is restored.</remarks>
    /// <param name="client">The observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, errors, and master instance used to read holding registers.</param>
    /// <param name="topic">The MQTT topic to which the holding register values are published.</param>
    /// <param name="startAddress">The starting address of the Modbus holding registers to read.</param>
    /// <param name="numberOfPoints">The number of consecutive holding registers to read and publish.</param>
    /// <param name="interval">The interval, in milliseconds, between consecutive Modbus read and publish operations. Must be greater than
    /// zero.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages.</param>
    /// <param name="retain">true to retain the published MQTT messages on the broker; otherwise, false.</param>
    /// <returns>An observable sequence of results indicating the outcome of each MQTT publish operation.</returns>
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
    /// Publishes Modbus input register values to the specified MQTT topic at a defined interval.
    /// </summary>
    /// <remarks>This method continuously reads input registers from the Modbus device and publishes their
    /// serialized values to the specified MQTT topic. If the Modbus connection is lost or an error occurs, publishing
    /// may be interrupted until the connection is restored.</remarks>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, any connection errors, and the Modbus master used to read
    /// input registers.</param>
    /// <param name="topic">The MQTT topic to which the input register values are published.</param>
    /// <param name="startAddress">The starting address of the Modbus input registers to read.</param>
    /// <param name="numberOfPoints">The number of input registers to read from the Modbus device.</param>
    /// <param name="interval">The interval, in milliseconds, at which input registers are read and published. Must be greater than zero. The
    /// default is 100.0 milliseconds.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages. The default is AtLeastOnce.</param>
    /// <param name="retain">A value indicating whether published messages should be retained by the MQTT broker. The default is <see
    /// langword="false"/>.</param>
    /// <returns>An observable sequence of results for each MQTT publish operation, indicating the outcome of each message sent.</returns>
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
    /// Publishes Modbus coil values as MQTT messages at a specified interval.
    /// </summary>
    /// <remarks>The method reads the specified range of Modbus coils at the given interval and publishes
    /// their values as MQTT messages to the specified topic. The observable emits a result for each publish attempt,
    /// allowing subscribers to monitor the outcome of each operation. If the Modbus connection is lost or an error
    /// occurs, the observable may emit errors accordingly.</remarks>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable sequence providing the Modbus connection state, any connection errors, and the Modbus master used
    /// to read coil values.</param>
    /// <param name="topic">The MQTT topic to which the coil values are published.</param>
    /// <param name="startAddress">The starting address of the first Modbus coil to read.</param>
    /// <param name="numberOfPoints">The number of consecutive Modbus coils to read and publish.</param>
    /// <param name="interval">The interval, in milliseconds, between consecutive Modbus coil reads and MQTT publishes. Must be greater than
    /// zero.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages.</param>
    /// <param name="retain">true to set the MQTT retain flag on published messages; otherwise, false.</param>
    /// <returns>An observable sequence of results for each MQTT publish operation.</returns>
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
    /// Publishes Modbus data to the specified MQTT topic using the provided payload factory and quality of service
    /// settings.
    /// </summary>
    /// <remarks>The method only supports payloads of type string or byte[]. If the payload type is not
    /// supported, a NotSupportedException is thrown. The method is intended for use with Modbus data sources and MQTT
    /// clients that support reactive programming patterns.</remarks>
    /// <typeparam name="TPayload">The type of the payload to publish. Must be either string or byte[].</typeparam>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="reader">An observable sequence providing Modbus read results, including connection status, errors, and data to be
    /// published.</param>
    /// <param name="topic">The MQTT topic to which the Modbus data will be published.</param>
    /// <param name="payloadFactory">A function that creates a payload of type TPayload from the Modbus data object.</param>
    /// <param name="qos">The quality of service level to use when publishing the message. The default is AtLeastOnce.</param>
    /// <param name="retain">true to retain the message on the broker; otherwise, false. The default is false.</param>
    /// <returns>An observable sequence containing the results of each MQTT publish operation.</returns>
    /// <exception cref="NotSupportedException">Thrown if TPayload is not string or byte[].</exception>
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

    /// <summary>
    /// Publishes Modbus input register values to the specified MQTT topic at a regular interval.
    /// </summary>
    /// <remarks>If the Modbus connection is lost or an error occurs, publishing will be suspended until the
    /// connection is restored. The method emits an event for each publish attempt, including failures.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, errors, and master instance used to read input registers.</param>
    /// <param name="topic">The MQTT topic to which the input register values are published. Cannot be null.</param>
    /// <param name="startAddress">The starting address of the input registers to read from the Modbus device.</param>
    /// <param name="numberOfPoints">The number of input registers to read from the Modbus device. Must be greater than zero.</param>
    /// <param name="interval">The interval, in milliseconds, at which input registers are read and published. Must be positive. The default is
    /// 100.0 milliseconds.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages. The default is AtLeastOnce.</param>
    /// <param name="retain">A value indicating whether published MQTT messages should be retained by the broker. The default is <see
    /// langword="false"/>.</param>
    /// <returns>An observable sequence of events indicating the result of each published MQTT message.</returns>
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
    /// Publishes Modbus holding register values as MQTT messages at a specified interval.
    /// </summary>
    /// <remarks>This method continuously reads the specified range of Modbus holding registers and publishes
    /// their values as MQTT messages at the given interval. If the Modbus connection is lost or an error occurs,
    /// publishing is suspended until the connection is restored. The method is designed for use in reactive pipelines
    /// and supports resilient MQTT client scenarios.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, errors, and master instance used to read holding registers.</param>
    /// <param name="topic">The MQTT topic to which the holding register data will be published. Cannot be null.</param>
    /// <param name="startAddress">The starting address of the Modbus holding registers to read.</param>
    /// <param name="numberOfPoints">The number of consecutive holding registers to read from the starting address.</param>
    /// <param name="interval">The interval, in milliseconds, at which holding registers are read and published. Must be greater than zero. The
    /// default is 100.0 milliseconds.</param>
    /// <param name="qos">The MQTT Quality of Service level to use when publishing messages. The default is AtLeastOnce.</param>
    /// <param name="retain">A value indicating whether published messages should be retained by the MQTT broker. The default is <see
    /// langword="false"/>.</param>
    /// <returns>An observable sequence of <see cref="ApplicationMessageProcessedEventArgs"/> that provides the result of each
    /// published MQTT message.</returns>
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
    /// Publishes Modbus input values as MQTT messages at a specified interval.
    /// </summary>
    /// <remarks>This method continuously reads input values from a Modbus device and publishes them as MQTT
    /// messages at the specified interval. The method handles connection state and errors as provided by the modbus
    /// observable. The returned observable emits an event for each message published, allowing subscribers to monitor
    /// publishing results.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, any associated error, and the Modbus master instance used
    /// to read input values.</param>
    /// <param name="topic">The MQTT topic to which the input values are published. Cannot be null.</param>
    /// <param name="startAddress">The starting Modbus address from which to read input values.</param>
    /// <param name="numberOfPoints">The number of input values to read from the Modbus device, starting at the specified address.</param>
    /// <param name="interval">The interval, in milliseconds, at which input values are read and published. Must be greater than zero. The
    /// default is 100.0 milliseconds.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages. The default is AtLeastOnce.</param>
    /// <param name="retain">true to set the MQTT retain flag on published messages; otherwise, false. The default is false.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs that provides the result of each published MQTT
    /// message.</returns>
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
    /// Publishes Modbus coil values as MQTT messages at a specified interval.
    /// </summary>
    /// <remarks>The method continuously reads the specified range of coils from the Modbus device and
    /// publishes their values to the given MQTT topic at the defined interval. If the Modbus connection is lost or an
    /// error occurs, the observable may emit error information in the event arguments. The caller is responsible for
    /// subscribing to the returned observable to initiate publishing and to handle any errors or completion
    /// notifications.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="modbus">An observable sequence providing the Modbus connection state, errors, and master instance for reading coil
    /// values.</param>
    /// <param name="topic">The MQTT topic to which the coil values are published.</param>
    /// <param name="startAddress">The starting address of the first coil to read from the Modbus device.</param>
    /// <param name="numberOfPoints">The number of consecutive coils to read and publish.</param>
    /// <param name="interval">The interval, in milliseconds, between consecutive Modbus coil reads and MQTT publishes. Must be greater than
    /// zero.</param>
    /// <param name="qos">The quality of service level to use when publishing MQTT messages.</param>
    /// <param name="retain">true to set the MQTT retain flag on published messages; otherwise, false.</param>
    /// <returns>An observable sequence of events indicating the result of each MQTT publish operation.</returns>
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
    /// Publishes Modbus data to the specified MQTT topic using the provided payload factory and quality of service
    /// settings.
    /// </summary>
    /// <remarks>The method only supports payloads of type string or byte[]. If the Modbus data is null, it
    /// will not be published. This method is intended for scenarios where Modbus data needs to be relayed to an MQTT
    /// broker in a resilient and reactive manner.</remarks>
    /// <typeparam name="TPayload">The type of the payload to publish. Must be either string or byte[].</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="reader">An observable sequence providing Modbus read results, including connection status, errors, and data to be
    /// published.</param>
    /// <param name="topic">The MQTT topic to which the Modbus data will be published. Cannot be null.</param>
    /// <param name="payloadFactory">A function that creates a payload of type TPayload from the Modbus data object. Cannot be null.</param>
    /// <param name="qos">The quality of service level to use when publishing messages. The default is AtLeastOnce.</param>
    /// <param name="retain">true to retain the published message on the broker; otherwise, false. The default is false.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs that provides the result of each publish
    /// operation.</returns>
    /// <exception cref="NotSupportedException">Thrown if TPayload is not string or byte[].</exception>
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

        /// <summary>
        /// Subscribes to an MQTT topic and writes parsed messages to a Modbus device using the provided writer action.
        /// </summary>
        /// <remarks>The subscription remains active as long as the returned IDisposable is not disposed.
        /// The writer action is only invoked when a Modbus master is available. If the Modbus connection is lost,
        /// incoming MQTT messages are ignored until the connection is restored.</remarks>
        /// <typeparam name="T">The type of value produced by parsing the MQTT message payload.</typeparam>
        /// <param name="client">The observable sequence of MQTT client connections used to subscribe to the specified topic.</param>
        /// <param name="modbus">An observable sequence providing the current Modbus connection state, error information, and Modbus master
        /// instance.</param>
        /// <param name="topic">The MQTT topic to subscribe to for receiving messages.</param>
        /// <param name="parse">A function that parses the MQTT message payload string into a value of type T.</param>
        /// <param name="writer">An action that writes the parsed value to the Modbus device using the provided Modbus master.</param>
        /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and Modbus state updates.</returns>
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
    /// Subscribes to an MQTT topic and writes parsed messages to a Modbus device when available.
    /// </summary>
    /// <remarks>The method listens for messages on the specified MQTT topic. When a message is received and a
    /// Modbus master is available, it parses the payload and writes the value asynchronously to the Modbus device.
    /// Disposing the returned IDisposable will terminate both the MQTT and Modbus subscriptions.</remarks>
    /// <typeparam name="T">The type of value parsed from the MQTT message payload and written to the Modbus device.</typeparam>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the specified topic. Cannot be null.</param>
    /// <param name="modbus">An observable providing the current Modbus connection state, error information, and Modbus master instance.
    /// Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Cannot be null.</param>
    /// <param name="parse">A function that parses the MQTT message payload into a value of type T. Cannot be null.</param>
    /// <param name="writerAsync">An asynchronous function that writes the parsed value to the Modbus device using the provided Modbus master.
    /// Cannot be null.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and Modbus state updates.</returns>
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

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <remarks>This method uses the default serialization settings of Json.NET. The output may vary
    /// depending on the object's structure and any custom serialization attributes applied.</remarks>
    /// <param name="value">The object to serialize. Can be null.</param>
    /// <returns>A JSON-formatted string representing the serialized object, or "null" if the value is null.</returns>
    public static string Serialize(this object? value) =>
        JsonConvert.SerializeObject(value);

    /// <summary>
    /// Deserializes the JSON string to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
    /// <param name="value">The JSON string to deserialize. Cannot be null.</param>
    /// <returns>An instance of type T deserialized from the JSON string, or null if the JSON is null or empty.</returns>
    public static T? DeSerialize<T>(this string value) =>
        JsonConvert.DeserializeObject<T>(value);

    /// <summary>
    /// Subscribes to an MQTT topic and writes received values to a single Modbus register using the specified writer
    /// action.
    /// </summary>
    /// <remarks>The method parses incoming MQTT payloads as unsigned 16-bit integers using invariant culture
    /// before invoking the writer action. The subscription remains active until the returned IDisposable is
    /// disposed.</remarks>
    /// <param name="client">The observable sequence of MQTT clients to subscribe with.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, any connection errors, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving register values.</param>
    /// <param name="address">The Modbus register address to write to when a value is received.</param>
    /// <param name="writer">An action that writes a value to the specified Modbus register using the provided Modbus master and register
    /// address.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing to the Modbus register.</returns>
    public static IDisposable SubscribeWriteSingleRegister(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort> writer) =>
        client.SubscribeWrite(modbus, topic, s => ushort.Parse(s, CultureInfo.InvariantCulture), (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes multiple Modbus registers when messages are received.
    /// </summary>
    /// <remarks>The incoming MQTT message payload is expected to be a comma-separated list of unsigned 16-bit
    /// integer values, which are parsed and written to the Modbus registers starting at the specified address. The
    /// method manages the subscription lifecycle and ensures that writes occur only when the Modbus connection is
    /// available.</remarks>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the topic.</param>
    /// <param name="modbus">An observable that provides the Modbus connection state, any connection errors, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving register values.</param>
    /// <param name="startAddress">The starting Modbus register address to write to.</param>
    /// <param name="writer">An action that writes the received register values to the Modbus master. The action receives the Modbus master
    /// and an array of register values.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the topic and stop writing to the Modbus registers.</returns>
    public static IDisposable SubscribeWriteMultipleRegisters(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort[]> writer) =>
        client.SubscribeWrite(
            modbus,
            topic,
            s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => ushort.Parse(x.Trim(), CultureInfo.InvariantCulture))
                  .ToArray(),
            (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes a single coil value to a Modbus device when a message is received.
    /// </summary>
    /// <remarks>The incoming MQTT message payload is parsed as a boolean value before being written to the
    /// Modbus coil. The method manages the subscription lifecycle and ensures that writes occur only when the Modbus
    /// connection is established.</remarks>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="modbus">An observable that provides the Modbus connection state, any connection errors, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving coil value updates.</param>
    /// <param name="address">The Modbus address of the coil to write to when a message is received.</param>
    /// <param name="writer">An action that writes the parsed boolean value to the specified Modbus master at the given address.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing to the Modbus coil.</returns>
    public static IDisposable SubscribeWriteSingleCoil(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, bool> writer) =>
        client.SubscribeWrite(modbus, topic, s => bool.Parse(s), (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes multiple coil values to a Modbus device when messages are received.
    /// </summary>
    /// <remarks>Incoming MQTT messages are expected to contain comma-separated boolean values, which are
    /// parsed and written to the Modbus device as coil states. The method manages the subscription lifecycle and
    /// ensures that coil writes occur only when the Modbus connection is established.</remarks>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="modbus">An observable that provides the connection state, any connection errors, and the Modbus master instance for
    /// writing coil values.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving coil value updates. Cannot be null or empty.</param>
    /// <param name="startAddress">The starting address of the first coil to write to on the Modbus device.</param>
    /// <param name="writer">An action that writes the parsed coil values to the Modbus device using the provided Modbus master instance.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and release associated resources.</returns>
    public static IDisposable SubscribeWriteMultipleCoils(this IObservable<IMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, bool[]> writer) =>
        client.SubscribeWrite(
            modbus,
            topic,
            s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => bool.Parse(x.Trim()))
                  .ToArray(),
            (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes received values to a single Modbus register using the specified writer
    /// action.
    /// </summary>
    /// <remarks>The received MQTT payloads are parsed as unsigned 16-bit integers using invariant culture
    /// before being written to the Modbus register. The subscription remains active as long as the returned IDisposable
    /// is not disposed.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to manage the subscription lifecycle.</param>
    /// <param name="modbus">An observable providing the current Modbus connection state, error information, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving register values as strings.</param>
    /// <param name="address">The address of the Modbus register to write to.</param>
    /// <param name="writer">An action that writes a value to the specified Modbus register using the provided Modbus master and register
    /// address.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing to the Modbus register.</returns>
    public static IDisposable SubscribeWriteSingleRegister(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort> writer) =>
        client.SubscribeWrite(modbus, topic, s => ushort.Parse(s, CultureInfo.InvariantCulture), (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes multiple Modbus registers when messages are received.
    /// </summary>
    /// <remarks>Incoming MQTT messages are expected to contain comma-separated unsigned 16-bit integer
    /// values, which are parsed and written to the Modbus registers starting at the specified address. The subscription
    /// remains active until the returned IDisposable is disposed.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the topic.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, error information, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving register values.</param>
    /// <param name="startAddress">The starting Modbus register address to write to.</param>
    /// <param name="writer">An action that writes the received register values to the Modbus master. The action receives the Modbus master
    /// and an array of register values.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing to Modbus registers.</returns>
    public static IDisposable SubscribeWriteMultipleRegisters(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort[]> writer) =>
        client.SubscribeWrite(
            modbus,
            topic,
            s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => ushort.Parse(x.Trim(), CultureInfo.InvariantCulture))
                  .ToArray(),
            (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes a single coil value to a Modbus device when a message is received.
    /// </summary>
    /// <remarks>The incoming MQTT message payload is parsed as a boolean value before being written to the
    /// Modbus coil. If the Modbus connection is not available or an error occurs, the write operation will not be
    /// performed until the connection is restored.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="modbus">An observable providing the connection state, any errors, and the Modbus master instance for communication.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving coil value updates.</param>
    /// <param name="address">The Modbus address of the coil to write to. Must be a valid coil address supported by the Modbus device.</param>
    /// <param name="writer">An action that writes the parsed boolean value to the specified Modbus master at the given address.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing to the Modbus coil.</returns>
    public static IDisposable SubscribeWriteSingleCoil(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort address, Action<ModbusIpMaster, bool> writer) =>
        client.SubscribeWrite(modbus, topic, s => bool.Parse(s), (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes multiple coil values to a Modbus device when messages are received.
    /// </summary>
    /// <remarks>The incoming MQTT message payload is expected to be a comma-separated list of boolean values
    /// (e.g., "true,false,true"). Each value is parsed and passed to the writer action. The method manages the
    /// subscription lifecycle and ensures that coil writes are attempted only when the Modbus connection is
    /// available.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the topic.</param>
    /// <param name="modbus">An observable providing the Modbus connection state, any connection errors, and the Modbus master instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving coil value updates.</param>
    /// <param name="startAddress">The starting address of the first coil to write to on the Modbus device.</param>
    /// <param name="writer">An action that writes the parsed coil values to the Modbus device using the provided Modbus master.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and stop writing coil values.</returns>
    public static IDisposable SubscribeWriteMultipleCoils(this IObservable<IResilientMqttClient> client, IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, bool[]> writer) =>
        client.SubscribeWrite(
            modbus,
            topic,
            s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(x => bool.Parse(x.Trim()))
                  .ToArray(),
            (m, v) => writer(m, v));

    /// <summary>
    /// Subscribes to an MQTT topic and writes parsed messages to a Modbus device using the provided writer action.
    /// </summary>
    /// <remarks>The subscription remains active as long as the returned IDisposable is not disposed. The
    /// writer action is only invoked when a Modbus master is available. If the Modbus connection is lost, messages
    /// received from the MQTT topic are ignored until the connection is restored.</remarks>
    /// <typeparam name="T">The type of the value parsed from the MQTT message payload.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="modbus">An observable sequence providing the current Modbus connection state, error information, and Modbus master
    /// instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving messages.</param>
    /// <param name="parse">A function that parses the MQTT message payload into a value of type T.</param>
    /// <param name="writer">An action that writes the parsed value to the Modbus device using the provided Modbus master.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from both the MQTT topic and the Modbus state updates.</returns>
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
    /// Subscribes to an MQTT topic and writes parsed messages to a Modbus device using the provided asynchronous writer
    /// function.
    /// </summary>
    /// <remarks>The subscription remains active as long as the returned IDisposable is not disposed. The
    /// writer function is only invoked when a valid Modbus master is available. Exceptions thrown by the writer
    /// function are not handled by this method and may be propagated to the observer.</remarks>
    /// <typeparam name="T">The type of the value parsed from the MQTT message payload.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="modbus">An observable sequence providing the current Modbus connection state, error information, and Modbus master
    /// instance.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving messages.</param>
    /// <param name="parse">A function that parses the MQTT message payload into a value of type T.</param>
    /// <param name="writerAsync">An asynchronous function that writes the parsed value to the Modbus device using the provided Modbus master.</param>
    /// <returns>An IDisposable that can be used to unsubscribe from the MQTT topic and Modbus state updates.</returns>
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
