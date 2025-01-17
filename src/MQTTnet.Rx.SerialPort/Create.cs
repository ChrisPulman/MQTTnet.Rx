// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.IO.Ports;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.SerialPort
{
    /// <summary>
    /// Create.
    /// </summary>
    public static class Create
    {
        /// <summary>
        /// Publishes the serial port.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="serialPort">The serial port.</param>
        /// <param name="startsWith">The starts with.</param>
        /// <param name="endsWith">The ends with.</param>
        /// <param name="timeOut">The time out.</param>
        /// <returns>MqttClientPublishResult.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// serialPort.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishSerialPort(this IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);

            return client.PublishMessage(serialPort?.DataReceived.BufferUntil(startsWith, endsWith, timeOut).Select(payLoad => (topic, payLoad))!);
        }

        /// <summary>
        /// Subscribes the serial port.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="configurePort">The configure port.</param>
        /// <param name="payloadFactory">The payload factory.</param>
        public static void SubscribeSerialPortWriteLine(this IObservable<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configurePort);

            var serialPort = default(ISerialPortRx)!;
            configurePort?.Invoke(serialPort);
            ArgumentNullException.ThrowIfNull(serialPort);

            client.SubscribeToTopic(topic).Subscribe(message => serialPort.WriteLine(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        }

        /// <summary>
        /// Subscribes the serial port.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="configurePort">The configure port.</param>
        /// <param name="payloadFactory">The payload factory.</param>
        public static void SubscribeSerialPortWrite(this IObservable<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configurePort);

            var serialPort = default(ISerialPortRx)!;
            configurePort?.Invoke(serialPort);
            ArgumentNullException.ThrowIfNull(serialPort);

            client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        }

        /// <summary>
        /// Subscribes the serial port.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="configurePort">The configure port.</param>
        /// <param name="payloadFactory">The payload factory.</param>
        public static void SubscribeSerialPortWrite(this IObservable<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configurePort);

            var serialPort = default(ISerialPortRx)!;
            configurePort?.Invoke(serialPort);
            ArgumentNullException.ThrowIfNull(serialPort);

            client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        }

        /////// <summary>
        /////// Publishes the serial port.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="serialPort">The serial port.</param>
        /////// <param name="startsWith">The starts with.</param>
        /////// <param name="endsWith">The ends with.</param>
        /////// <param name="timeOut">The time out.</param>
        /////// <returns>A ApplicationMessageProcessedEventArgs.</returns>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort(this IObservable<IManagedMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(serialPort);

        ////    return client.PublishMessage(serialPort?.DataReceived.BufferUntil(startsWith, endsWith, timeOut).Select(payLoad => (topic, payLoad))!);
        ////}

        /////// <summary>
        /////// Subscribes the serial port.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="configurePort">The configure port.</param>
        /////// <param name="payloadFactory">The payload factory.</param>
        ////public static void SubscribeSerialPortWriteLine(this IObservable<IManagedMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePort);

        ////    var serialPort = default(ISerialPortRx)!;
        ////    configurePort?.Invoke(serialPort);
        ////    ArgumentNullException.ThrowIfNull(serialPort);

        ////    client.SubscribeToTopic(topic).Subscribe(message => serialPort.WriteLine(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        ////}

        /////// <summary>
        /////// Subscribes the serial port.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="configurePort">The configure port.</param>
        /////// <param name="payloadFactory">The payload factory.</param>
        ////public static void SubscribeSerialPortWrite(this IObservable<IManagedMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePort);

        ////    var serialPort = default(ISerialPortRx)!;
        ////    configurePort?.Invoke(serialPort);
        ////    ArgumentNullException.ThrowIfNull(serialPort);

        ////    client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        ////}

        /////// <summary>
        /////// Subscribes the serial port.
        /////// </summary>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="configurePort">The configure port.</param>
        /////// <param name="payloadFactory">The payload factory.</param>
        ////public static void SubscribeSerialPortWrite(this IObservable<IManagedMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePort);

        ////    var serialPort = default(ISerialPortRx)!;
        ////    configurePort?.Invoke(serialPort);
        ////    ArgumentNullException.ThrowIfNull(serialPort);

        ////    client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        ////}
    }
}
