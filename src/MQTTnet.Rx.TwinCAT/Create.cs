// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.TwinCAT;

/// <summary>
/// Provides extension methods for integrating TwinCAT PLC variables with MQTT clients, enabling publishing and
/// subscribing to PLC tag values via MQTT topics.
/// </summary>
/// <remarks>The Create class contains static methods that facilitate the connection between PLC tag data
/// and MQTT messaging. These methods allow developers to publish PLC variable changes to MQTT topics or subscribe
/// to MQTT topics and write received values to PLC variables. All methods are designed for use with reactive
/// streams and require proper configuration of the PLC client before use. These methods are intended for advanced
/// scenarios involving industrial automation and IoT integration.</remarks>
public static class Create
{
    /// <summary>
    /// Publishes updates of a TwinCAT PLC variable to the specified MQTT topic as messages.
    /// </summary>
    /// <remarks>The method observes changes to the specified PLC variable and publishes each update as a
    /// message to the given MQTT topic. The PLC client must be properly configured and connected before publishing. The
    /// observable completes or errors if the underlying MQTT client sequence completes or errors.</remarks>
    /// <typeparam name="T">The type of the PLC variable to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of connected MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC variable updates will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the TwinCAT PLC variable to observe for value changes. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the TwinCAT ADS client before observing the PLC variable. Cannot be null and must assign
    /// a valid IRxTcAdsClient instance.</param>
    /// <returns>An observable sequence of results for each published MQTT message corresponding to updates of the specified PLC
    /// variable.</returns>
    public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IRxTcAdsClient)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to an MQTT topic and writes the received payload to a specified PLC variable using a configured
    /// TwinCAT ADS client.
    /// </summary>
    /// <remarks>The method requires that the PLC client is properly configured and initialized within the
    /// provided action. The payload factory is invoked for each received message to transform the payload before
    /// writing it to the PLC variable.</remarks>
    /// <typeparam name="T">The type to which the MQTT message payload is converted before being written to the PLC variable.</typeparam>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Cannot be null.</param>
    /// <param name="plcVariable">The name of the PLC variable to which the payload will be written. Cannot be null.</param>
    /// <param name="configurePlc">An action that configures the TwinCAT ADS client before use. Must initialize the provided client instance.
    /// Cannot be null.</param>
    /// <param name="payloadFactory">A function that converts the MQTT message payload (as a string) to the type required by the PLC variable.</param>
    public static void SubscribeTcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IRxTcAdsClient)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        client.SubscribeToTopic(topic).Subscribe(message => plc.Write(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
    }

    /// <summary>
    /// Publishes the value of a TwinCAT PLC variable to the specified MQTT topic as an observable sequence.
    /// </summary>
    /// <remarks>The method observes changes to the specified PLC variable and publishes each new value to the
    /// given MQTT topic. The returned observable emits a result for each publish attempt. The caller is responsible for
    /// ensuring that the PLC variable exists and is accessible.</remarks>
    /// <typeparam name="T">The type of the PLC variable to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of connected MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC variable value will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the PLC variable to observe and publish. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the PLC hash table before publishing. Cannot be null.</param>
    /// <returns>An observable sequence that emits the result of each MQTT publish operation.</returns>
    public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(HashTableRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Publishes updates of a TwinCAT PLC variable to the specified MQTT topic as messages, using the provided PLC
    /// client configuration.
    /// </summary>
    /// <remarks>The method observes changes to the specified PLC variable and publishes each update as a
    /// message to the given MQTT topic. The PLC client must be properly configured using the provided action before
    /// publishing begins.</remarks>
    /// <typeparam name="T">The type of the PLC variable to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC variable updates will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the TwinCAT PLC variable to observe for value changes. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the PLC client before observing the variable. Cannot be null.</param>
    /// <returns>An observable sequence of event arguments representing the result of each published MQTT message.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IRxTcAdsClient)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to the specified MQTT topic and writes the received payload to a TwinCAT PLC variable using the
    /// provided payload factory and PLC configuration.
    /// </summary>
    /// <remarks>This method enables integration between MQTT message streams and TwinCAT PLC variables by
    /// subscribing to a topic and writing each received message to the specified PLC variable. The PLC client must be
    /// properly configured in the configurePlc action before use.</remarks>
    /// <typeparam name="T">The type of the value to be written to the PLC variable, as produced by the payload factory.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Cannot be null.</param>
    /// <param name="plcVariable">The name of the TwinCAT PLC variable to which the payload will be written. Cannot be null.</param>
    /// <param name="configurePlc">An action that configures the TwinCAT ADS client before writing values. Cannot be null.</param>
    /// <param name="payloadFactory">A function that converts the received message payload (as a string) to the value of type T to be written to the
    /// PLC variable. Cannot be null.</param>
    public static void SubscribeTcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IRxTcAdsClient)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        client.SubscribeToTopic(topic).Subscribe(message => plc.Write(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
    }

    /// <summary>
    /// Publishes a TwinCAT PLC variable as an MQTT message to the specified topic using the provided resilient MQTT
    /// client sequence.
    /// </summary>
    /// <remarks>The method observes changes to the specified PLC variable and publishes each new value as an
    /// MQTT message. The PLC configuration must be set up using the configurePlc action before publishing. The returned
    /// observable emits an event for each message processed.</remarks>
    /// <typeparam name="T">The type of the PLC variable to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of resilient MQTT clients used to publish the message.</param>
    /// <param name="topic">The MQTT topic to which the PLC variable value will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the TwinCAT PLC variable to observe and publish. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the PLC hash table before publishing. Cannot be null.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs representing the result of each published
    /// message.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(HashTableRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }
}
