// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Serial;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.SerialPort;

/// <summary>Provides static compatibility entry points for serial-port MQTT bridge operations.</summary>
public static class Create
{
    /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic that receives framed payloads.</param>
    /// <param name="serialPort">The serial port that supplies received data.</param>
    /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
    /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
    /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
    /// <returns>The result of each MQTT publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishSerialPort(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith,
        int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PublishSerialPort(topic, serialPort, startsWith, endsWith, timeOut);
    }

    /// <summary>Publishes framed serial-port payloads to an MQTT topic.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic that receives framed payloads.</param>
    /// <param name="serialPort">The serial port that supplies received data.</param>
    /// <param name="startsWith">The observable sequence that identifies frame starts.</param>
    /// <param name="endsWith">The observable sequence that identifies frame ends.</param>
    /// <param name="timeOut">The maximum time to wait for a complete frame.</param>
    /// <returns>The result of each resilient MQTT publish operation.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith,
        int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PublishSerialPort(topic, serialPort, startsWith, endsWith, timeOut);
    }

    /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the lines.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWriteLine(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWriteLine(topic, serialPort, payloadFactory);
    }

    /// <summary>Writes matching MQTT payloads as serial-port lines.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the lines.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to a serial-port line.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWriteLine(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWriteLine(topic, serialPort, payloadFactory);
    }

    /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the text.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWrite(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
    }

    /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
    /// <param name="client">The MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the bytes.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWrite(
        IObservable<IMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
    }

    /// <summary>Writes matching MQTT payloads as serial-port text.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the text.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to serial-port text.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWrite(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
    }

    /// <summary>Writes matching MQTT payloads as serial-port bytes.</summary>
    /// <param name="client">The resilient MQTT client sequence used by the bridge.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="serialPort">The serial port that receives the bytes.</param>
    /// <param name="payloadFactory">Converts each MQTT payload to serial-port bytes.</param>
    /// <returns>A disposable that ends the MQTT-to-serial subscription.</returns>
    public static IDisposable SubscribeSerialPortWrite(
        IObservable<IResilientMqttClient> client,
        string topic,
        ISerialPortRx serialPort,
        Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SubscribeSerialPortWrite(topic, serialPort, payloadFactory);
    }
}
