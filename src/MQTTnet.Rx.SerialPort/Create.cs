// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.IO.Ports;
using MQTTnet.Client;
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
            client.ThrowArgumentNullExceptionIfNull(nameof(client));
            serialPort.ThrowArgumentNullExceptionIfNull(nameof(serialPort));

            return client.PublishMessage(serialPort?.DataReceived.BufferUntil(startsWith, endsWith, timeOut).Select(payLoad => (topic, payLoad))!);
        }
    }
}
