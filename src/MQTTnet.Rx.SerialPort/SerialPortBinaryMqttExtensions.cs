// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.SerialPort.Reactive;
#else
namespace MQTTnet.Rx.SerialPort;
#endif

/// <summary>Bridges serial receive counts and binary MQTT payloads.</summary>
public static class SerialPortBinaryMqttExtensions
{
    /// <summary>Provides binary serial bridges for MQTT clients.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes each serial receive byte count.</summary>
        /// <param name="topic">The MQTT publish topic.</param>
        /// <param name="serialPort">The serial transport that supplies receive notifications.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishSerialPortReceiveCount(string topic, IPortRx serialPort)
        {
            Validate(client, topic, serialPort);
            return client.PublishMessage(serialPort.BytesReceived.Select(count => (topic, count.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>Writes matching MQTT payload bytes directly to the serial port.</summary>
        /// <param name="topic">The MQTT subscription filter.</param>
        /// <param name="serialPort">The serial transport that receives payloads.</param>
        /// <returns>The subscription lifetime.</returns>
        public IDisposable SubscribeSerialPortWriteBytes(string topic, ISerialPortRx serialPort)
        {
            Validate(client, topic, serialPort);
            return client.SubscribeToTopic(topic).Subscribe(Witness.Create<MqttApplicationMessageReceivedEventArgs>(message => WritePayload(serialPort, message)));
        }
    }

    /// <summary>Provides binary serial bridges for resilient MQTT clients.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes each serial receive byte count.</summary>
        /// <param name="topic">The MQTT publish topic.</param>
        /// <param name="serialPort">The serial transport that supplies receive notifications.</param>
        /// <returns>The processed message notifications.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPortReceiveCount(string topic, IPortRx serialPort)
        {
            Validate(client, topic, serialPort);
            return client.PublishMessage(serialPort.BytesReceived.Select(count => (topic, count.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>Writes matching MQTT payload bytes directly to the serial port.</summary>
        /// <param name="topic">The MQTT subscription filter.</param>
        /// <param name="serialPort">The serial transport that receives payloads.</param>
        /// <returns>The subscription lifetime.</returns>
        public IDisposable SubscribeSerialPortWriteBytes(string topic, ISerialPortRx serialPort)
        {
            Validate(client, topic, serialPort);
            return client.SubscribeToTopic(topic).Subscribe(Witness.Create<MqttApplicationMessageReceivedEventArgs>(message => WritePayload(serialPort, message)));
        }
    }

    /// <summary>Provides binary serial bridges for asynchronous MQTT clients.</summary>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes each serial receive byte count.</summary>
        /// <param name="topic">The MQTT publish topic.</param>
        /// <param name="serialPort">The serial transport that supplies receive notifications.</param>
        /// <returns>The publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishSerialPortReceiveCount(string topic, IPortRx serialPort)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().PublishSerialPortReceiveCount(topic, serialPort).ToMqttAsyncSignal();
        }

        /// <summary>Writes matching MQTT payload bytes directly to the serial port.</summary>
        /// <param name="topic">The MQTT subscription filter.</param>
        /// <param name="serialPort">The serial transport that receives payloads.</param>
        /// <returns>The subscription lifetime.</returns>
        public IDisposable SubscribeSerialPortWriteBytes(string topic, ISerialPortRx serialPort)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWriteBytes(topic, serialPort);
        }
    }

    /// <summary>Provides binary serial bridges for asynchronous resilient MQTT clients.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes each serial receive byte count.</summary>
        /// <param name="topic">The MQTT publish topic.</param>
        /// <param name="serialPort">The serial transport that supplies receive notifications.</param>
        /// <returns>The processed message notifications.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPortReceiveCount(string topic, IPortRx serialPort)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().PublishSerialPortReceiveCount(topic, serialPort).ToMqttAsyncSignal();
        }

        /// <summary>Writes matching MQTT payload bytes directly to the serial port.</summary>
        /// <param name="topic">The MQTT subscription filter.</param>
        /// <param name="serialPort">The serial transport that receives payloads.</param>
        /// <returns>The subscription lifetime.</returns>
        public IDisposable SubscribeSerialPortWriteBytes(string topic, ISerialPortRx serialPort)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWriteBytes(topic, serialPort);
        }
    }

    /// <summary>Writes one payload while avoiding copies for contiguous buffers.</summary>
    /// <param name="serialPort">The serial transport.</param>
    /// <param name="message">The received MQTT message.</param>
    private static void WritePayload(ISerialPortRx serialPort, MqttApplicationMessageReceivedEventArgs message)
    {
        var payload = message.ApplicationMessage.Payload;
        serialPort.Write(payload.IsSingleSegment ? payload.First : payload.ToArray().AsMemory());
    }

    /// <summary>Validates a serial bridge's required arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic or filter.</param>
    /// <param name="serialPort">The serial transport.</param>
    private static void Validate<TClient>(IObservable<TClient> client, string topic, IPortRx serialPort)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(serialPort);
    }
}
