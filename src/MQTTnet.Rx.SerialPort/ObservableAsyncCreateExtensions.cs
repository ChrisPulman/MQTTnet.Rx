// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CP.IO.Ports;
using MQTTnet.Rx.Client;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.SerialPort;

/// <summary>
/// Provides asynchronous observable counterparts for serial port MQTT helpers.
/// </summary>
public static class ObservableAsyncCreateExtensions
{
    public static IObservableAsync<MqttClientPublishResult> PublishSerialPort(this IObservableAsync<IMqttClient> client, string topic, ISerialPortRx serialPort, IObservableAsync<char> startsWith, IObservableAsync<char> endsWith, int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(startsWith);
        ArgumentNullException.ThrowIfNull(endsWith);
        return Create.PublishSerialPort(client.ToObservable(), topic, serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut).ToObservableAsync();
    }

    public static void SubscribeSerialPortWriteLine(this IObservableAsync<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWriteLine(client.ToObservable(), topic, configurePort, payloadFactory);
    }

    public static void SubscribeSerialPortWrite(this IObservableAsync<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWrite(client.ToObservable(), topic, configurePort, payloadFactory);
    }

    public static void SubscribeSerialPortWrite(this IObservableAsync<IMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWrite(client.ToObservable(), topic, configurePort, payloadFactory);
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort(this IObservableAsync<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, IObservableAsync<char> startsWith, IObservableAsync<char> endsWith, int timeOut)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(startsWith);
        ArgumentNullException.ThrowIfNull(endsWith);
        return Create.PublishSerialPort(client.ToObservable(), topic, serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut).ToObservableAsync();
    }

    public static void SubscribeSerialPortWriteLine(this IObservableAsync<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWriteLine(client.ToObservable(), topic, configurePort, payloadFactory);
    }

    public static void SubscribeSerialPortWrite(this IObservableAsync<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, string> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWrite(client.ToObservable(), topic, configurePort, payloadFactory);
    }

    public static void SubscribeSerialPortWrite(this IObservableAsync<IResilientMqttClient> client, string topic, Action<ISerialPortRx> configurePort, Func<string, byte[]> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeSerialPortWrite(client.ToObservable(), topic, configurePort, payloadFactory);
    }
}
