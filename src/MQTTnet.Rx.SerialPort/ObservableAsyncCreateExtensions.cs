// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.SerialPort.Reactive;
#else
namespace MQTTnet.Rx.SerialPort;
#endif

/// <summary>Provides asynchronous-observable MQTT and serial-port bridge operations.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides serial-port bridge operations for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence used by the bridge.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
        /// <param name="topic">The MQTT topic that receives framed payloads.</param>
        /// <param name="serialPort">The serial port that supplies received data.</param>
        /// <param name="startsWith">The asynchronous sequence that identifies frame starts.</param>
        /// <param name="endsWith">The asynchronous sequence that identifies frame ends.</param>
        /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
        /// <returns>The result of each MQTT publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishSerialPort(
            string topic,
            ISerialPortRx serialPort,
            IObservableAsync<char> startsWith,
            IObservableAsync<char> endsWith,
            int timeOut)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);

            return client.ToObservable()
                .PublishSerialPort(topic, serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut)
                .ToMqttAsyncSignal();
        }

        /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the lines.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWriteLine(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWriteLine(topic, serialPort, payloadFactory);
        }

        /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the text.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
        }

        /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the bytes.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, byte[]> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
        }
    }

    /// <summary>Provides serial-port bridge operations for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence used by the bridge.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
        /// <param name="topic">The MQTT topic that receives framed payloads.</param>
        /// <param name="serialPort">The serial port that supplies received data.</param>
        /// <param name="startsWith">The asynchronous sequence that identifies frame starts.</param>
        /// <param name="endsWith">The asynchronous sequence that identifies frame ends.</param>
        /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
        /// <returns>The result of each resilient MQTT publish operation.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort(
            string topic,
            ISerialPortRx serialPort,
            IObservableAsync<char> startsWith,
            IObservableAsync<char> endsWith,
            int timeOut)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);

            return client.ToObservable()
                .PublishSerialPort(topic, serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut)
                .ToMqttAsyncSignal();
        }

        /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the lines.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWriteLine(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWriteLine(topic, serialPort, payloadFactory);
        }

        /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the text.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, string> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
        }

        /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="serialPort">The serial port that receives the bytes.</param>
        /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
        /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
        public IDisposable SubscribeSerialPortWrite(
            string topic,
            ISerialPortRx serialPort,
            Func<string, byte[]> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
        }
    }
}
