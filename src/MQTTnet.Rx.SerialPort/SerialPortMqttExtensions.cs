// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Serial;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Advanced;

namespace MQTTnet.Rx.SerialPort;

/// <summary>Provides reactive MQTT and serial-port bridge operations.</summary>
public static class SerialPortMqttExtensions
{
    /// <summary>Provides serial-port bridge operations for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
        /// <param name="topic">The MQTT topic that receives framed payloads.</param>
        /// <param name="serialPort">The serial port that supplies received data.</param>
        /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
        /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
        /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
        /// <returns>The result of each MQTT publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishSerialPort(
            string topic,
            ISerialPortRx serialPort,
            IObservable<char> startsWith,
            IObservable<char> endsWith,
            int timeOut)
        {
            ArgumentNullException.ThrowIfNull(serialPort);
            return PublishSerialPortCore(client, topic, serialPort, startsWith, endsWith, timeOut);
        }

        /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the lines.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWriteLine(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.WriteLine(payload));

        /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the text.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.Write(payload));

        /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the bytes.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, byte[]> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.Write(payload));

    }

    /// <summary>Provides serial-port bridge operations for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
        /// <param name="topic">The MQTT topic that receives framed payloads.</param>
        /// <param name="serialPort">The serial port that supplies received data.</param>
        /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
        /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
        /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
        /// <returns>The result of each resilient MQTT publish operation.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort(
            string topic,
            ISerialPortRx serialPort,
            IObservable<char> startsWith,
            IObservable<char> endsWith,
            int timeOut)
        {
            ArgumentNullException.ThrowIfNull(serialPort);
            return PublishSerialPortCore(client, topic, serialPort, startsWith, endsWith, timeOut);
        }

        /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the lines.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWriteLine(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.WriteLine(payload));

        /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the text.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.Write(payload));

        /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the bytes.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, byte[]> payloadFactory) =>
            SubscribeSerialPortWriteCore(
                client,
                topic,
                serialPort,
                payloadFactory,
                static (port, payload) => port.Write(payload));
    }

    /// <summary>Publishes framed serial-port data through ordinary MQTT clients.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic that receives framed payloads.</param>
    /// <param name="serialPort">The serial port that supplies received data.</param>
    /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
    /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
    /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
    /// <returns>The result of each MQTT publish operation.</returns>
    private static IObservable<MqttClientPublishResult> PublishSerialPortCore(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith,
        int timeOut)
    {
        ValidatePublishArguments(client, topic, serialPort, startsWith, endsWith);
        return client.PublishMessage(SerialPortRxMixins
            .BufferUntil(serialPort.DataReceived, startsWith, endsWith, timeOut)
            .Select(payload => (topic, payload)));
    }

    /// <summary>Publishes framed serial-port data through resilient MQTT clients.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic that receives framed payloads.</param>
    /// <param name="serialPort">The serial port that supplies received data.</param>
    /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
    /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
    /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
    /// <returns>The result of each resilient MQTT publish operation.</returns>
    private static IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPortCore(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith,
        int timeOut)
    {
        ValidatePublishArguments(client, topic, serialPort, startsWith, endsWith);
        return client.PublishMessage(SerialPortRxMixins
            .BufferUntil(serialPort.DataReceived, startsWith, endsWith, timeOut)
            .Select(payload => (topic, payload)));
    }

    /// <summary>Subscribes ordinary MQTT clients and forwards each transformed payload to a serial port.</summary>
    /// <typeparam name="TPayload">The transformed serial-port payload type.</typeparam>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives transformed payloads.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to a serial-port payload.</param>
    /// <param name="write">Writes a transformed payload to the serial port.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    private static IDisposable SubscribeSerialPortWriteCore<TPayload>(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, TPayload> payloadFactory,
        Action<ISerialPortRx, TPayload> write)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(serialPort);
        ArgumentNullException.ThrowIfNull(payloadFactory);
        ArgumentNullException.ThrowIfNull(write);

        return client.SubscribeToTopic(topic).Subscribe(
            Witness.Create<MqttApplicationMessageReceivedEventArgs>(message =>
                write(serialPort, payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
    }

    /// <summary>Subscribes resilient MQTT clients and forwards each transformed payload to a serial port.</summary>
    /// <typeparam name="TPayload">The transformed serial-port payload type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives transformed payloads.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to a serial-port payload.</param>
    /// <param name="write">Writes a transformed payload to the serial port.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    private static IDisposable SubscribeSerialPortWriteCore<TPayload>(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, TPayload> payloadFactory,
        Action<ISerialPortRx, TPayload> write)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(serialPort);
        ArgumentNullException.ThrowIfNull(payloadFactory);
        ArgumentNullException.ThrowIfNull(write);

        return client.SubscribeToTopic(topic).Subscribe(
            Witness.Create<MqttApplicationMessageReceivedEventArgs>(message =>
                write(serialPort, payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
    }

    /// <summary>Validates the required serial-port publishing arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic that receives framed payloads.</param>
    /// <param name="serialPort">The serial port that supplies received data.</param>
    /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
    /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
    private static void ValidatePublishArguments<TClient>(
        IObservable<TClient> client,
        string topic,
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(serialPort);
        ArgumentNullException.ThrowIfNull(startsWith);
        ArgumentNullException.ThrowIfNull(endsWith);
    }
}
