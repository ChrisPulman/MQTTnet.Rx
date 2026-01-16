// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.IO.Ports;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.SerialPort;

/// <summary>
/// Provides extension methods for integrating serial port communication with MQTT clients using reactive streams.
/// </summary>
/// <remarks>The Create class contains static methods that enable publishing data from a serial port to
/// MQTT topics and subscribing to MQTT topics to write data to a serial port. These methods are designed to be used
/// as extension methods on IObservable{IMqttClient} instances, facilitating reactive, event-driven integration
/// between serial port devices and MQTT messaging workflows.</remarks>
public static class Create
{
    /// <summary>
    /// Publishes data received from a serial port to an MQTT topic as messages, using specified start and end
    /// delimiters to define message boundaries.
    /// </summary>
    /// <remarks>This method buffers incoming serial port data and publishes each complete message
    /// frame as a separate MQTT message. If a message frame is not completed within the specified timeout, the
    /// incomplete data is discarded. The method requires the client and serialPort parameters to be
    /// non-null.</remarks>
    /// <param name="client">An observable sequence of connected MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the serial port data will be published.</param>
    /// <param name="serialPort">The serial port source from which data is read and published.</param>
    /// <param name="startsWith">An observable sequence of characters that indicate the start of a message frame in the serial data.</param>
    /// <param name="endsWith">An observable sequence of characters that indicate the end of a message frame in the serial data.</param>
    /// <param name="timeOut">The maximum time, in milliseconds, to wait for a complete message frame before discarding incomplete data.
    /// Must be a non-negative integer.</param>
    /// <returns>An observable sequence of results indicating the outcome of each publish operation to the MQTT broker.</returns>
    public static IObservable<MqttClientPublishResult> PublishSerialPort(this IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(serialPort);

        return client.PublishMessage(serialPort?.DataReceived.BufferUntil(startsWith, endsWith, timeOut).Select(payLoad => (topic, payLoad))!);
    }

    /// <summary>
    /// Subscribes to the specified MQTT topic and writes each received message to a serial port using a custom
    /// payload transformation.
    /// </summary>
    /// <remarks>The method subscribes to the specified topic for each MQTT client in the observable
    /// sequence. For each received message, the payload is transformed using the provided factory function and then
    /// written to the configured serial port. The serial port must be properly configured and assigned within the
    /// configurePort action before messages are processed.</remarks>
    /// <param name="client">The observable sequence of MQTT clients to subscribe with. Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages published to this topic will be processed and written to the serial
    /// port.</param>
    /// <param name="configurePort">An action that configures the serial port to be used for writing messages. The action must assign a valid
    /// instance to the provided serial port parameter. Cannot be null.</param>
    /// <param name="payloadFactory">A function that transforms the received message payload into a string to be written to the serial port. The
    /// function receives the message payload as a string and returns the string to write.</param>
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
    /// Subscribes to the specified MQTT topic and writes each received message to a serial port using the provided
    /// configuration and payload factory.
    /// </summary>
    /// <remarks>The serial port must be properly configured and assigned within the <paramref
    /// name="configurePort"/> action before messages can be written. This method subscribes to the topic for each
    /// client in the observable sequence and writes the transformed payload to the configured serial port upon
    /// message receipt.</remarks>
    /// <param name="client">The observable sequence of MQTT clients to subscribe with. Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages published to this topic will be written to the serial port.</param>
    /// <param name="configurePort">An action that configures the serial port to be used for writing messages. Cannot be null and must assign a
    /// valid serial port instance.</param>
    /// <param name="payloadFactory">A function that generates the payload to write to the serial port from the received MQTT message payload.</param>
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
    /// Subscribes to an MQTT topic and writes received messages to a serial port using a custom payload format.
    /// </summary>
    /// <remarks>This method allows integration between MQTT message streams and serial port
    /// communication by forwarding messages from the specified topic to the configured serial port. The serial port
    /// must be properly initialized in the configuration action before messages are received. The method does not
    /// return a subscription handle; to manage the subscription's lifetime, use the returned IDisposable from the
    /// underlying Subscribe method if needed.</remarks>
    /// <param name="client">The observable sequence of MQTT clients to subscribe to the specified topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages received on this topic will be written to the serial port.</param>
    /// <param name="configurePort">An action that configures the serial port before use. The provided serial port instance must be initialized
    /// within this action.</param>
    /// <param name="payloadFactory">A function that converts the received message payload (as a string) into a byte array to be written to the
    /// serial port.</param>
    public static void SubscribeSerialPortWrite(this IObservable<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePort);

        var serialPort = default(ISerialPortRx)!;
        configurePort?.Invoke(serialPort);
        ArgumentNullException.ThrowIfNull(serialPort);

        client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }

    /// <summary>
    /// Publishes data received from a serial port to the specified MQTT topic, using delimiters to define message
    /// boundaries.
    /// </summary>
    /// <remarks>This method buffers incoming serial data and publishes each complete message frame to
    /// the specified MQTT topic. If a message frame is not completed within the specified timeout, the incomplete
    /// data is discarded. The method requires both the client and serialPort parameters to be non-null.</remarks>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the serial port data will be published.</param>
    /// <param name="serialPort">The serial port source that provides the data to be published. Cannot be null.</param>
    /// <param name="startsWith">An observable sequence of characters that indicate the start of a message frame in the serial data.</param>
    /// <param name="endsWith">An observable sequence of characters that indicate the end of a message frame in the serial data.</param>
    /// <param name="timeOut">The maximum time, in milliseconds, to wait for a complete message frame before discarding incomplete data.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs representing the result of each published
    /// message.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort(this IObservable<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(serialPort);

        return client.PublishMessage(serialPort?.DataReceived.BufferUntil(startsWith, endsWith, timeOut).Select(payLoad => (topic, payLoad))!);
    }

    /// <summary>
    /// Subscribes to the specified MQTT topic and writes each received message to a serial port using a custom
    /// payload transformation.
    /// </summary>
    /// <remarks>The method requires that the serial port is properly configured and assigned within
    /// the provided configuration action. If the serial port is not assigned, an exception will be thrown. The
    /// method subscribes to the MQTT topic and writes each message to the serial port as it is received.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients to subscribe with. Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages published to this topic will be processed and written to the serial
    /// port.</param>
    /// <param name="configurePort">An action that configures the serial port to be used for writing messages. The action must assign a valid
    /// instance to the provided serial port parameter. Cannot be null.</param>
    /// <param name="payloadFactory">A function that transforms the received message payload into a string to be written to the serial port. The
    /// function receives the message payload as a string and returns the string to write.</param>
    public static void SubscribeSerialPortWriteLine(this IObservable<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePort);

        var serialPort = default(ISerialPortRx)!;
        configurePort?.Invoke(serialPort);
        ArgumentNullException.ThrowIfNull(serialPort);

        client.SubscribeToTopic(topic).Subscribe(message => serialPort.WriteLine(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }

    /// <summary>
    /// Subscribes to the specified MQTT topic and writes received messages to a serial port using a custom payload
    /// transformation.
    /// </summary>
    /// <remarks>The method requires that the serial port is properly configured and assigned within
    /// the <paramref name="configurePort"/> action. If the serial port is not assigned, an <see
    /// cref="ArgumentNullException"/> is thrown. The subscription writes each received MQTT message to the serial
    /// port after applying the specified payload transformation.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients to subscribe with. Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages received on this topic will be written to the serial port.</param>
    /// <param name="configurePort">An action that configures the serial port to be used for writing messages. The action must assign a valid
    /// instance to the provided serial port parameter. Cannot be null.</param>
    /// <param name="payloadFactory">A function that transforms the received message payload into a string to be written to the serial port. The
    /// function receives the message payload as a string and returns the string to write.</param>
    public static void SubscribeSerialPortWrite(this IObservable<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePort);

        var serialPort = default(ISerialPortRx)!;
        configurePort?.Invoke(serialPort);
        ArgumentNullException.ThrowIfNull(serialPort);

        client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }

    /// <summary>
    /// Subscribes to the specified MQTT topic and writes received messages to a serial port using a custom payload
    /// factory.
    /// </summary>
    /// <remarks>The method subscribes to the specified topic for each MQTT client in the observable
    /// sequence. For each received message, the payload is generated using the provided factory and written to the
    /// configured serial port. The serial port must be properly configured and assigned within the configurePort
    /// action before use.</remarks>
    /// <param name="client">The observable sequence of resilient MQTT clients to subscribe with. Cannot be null.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Messages received on this topic will be written to the serial port.</param>
    /// <param name="configurePort">An action that configures the serial port to be used for writing messages. The action must assign a valid
    /// instance to the provided serial port parameter. Cannot be null.</param>
    /// <param name="payloadFactory">A function that generates the byte array payload to write to the serial port from the received MQTT message
    /// string.</param>
    public static void SubscribeSerialPortWrite(this IObservable<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePort);

        var serialPort = default(ISerialPortRx)!;
        configurePort?.Invoke(serialPort);
        ArgumentNullException.ThrowIfNull(serialPort);

        client.SubscribeToTopic(topic).Subscribe(message => serialPort.Write(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }
}
