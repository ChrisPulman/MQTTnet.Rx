// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Rx.Client;
using S7PlcRx;

namespace MQTTnet.Rx.S7Plc;

/// <summary>
/// Provides extension methods for publishing and subscribing to S7 PLC tags over MQTT using reactive streams.
/// </summary>
/// <remarks>The Create class contains static methods that integrate S7 PLC communication with MQTT
/// clients in a reactive programming model. These methods enable seamless data exchange between PLC variables and
/// MQTT topics, supporting both publishing and subscribing scenarios. All methods are implemented as extension
/// methods for IObservable{IMqttClient}, allowing for fluent integration in reactive pipelines. Ensure that all
/// required parameters are provided and properly configured before invoking these methods.</remarks>
public static class Create
{
    /// <summary>
    /// Publishes the value of a specified S7 PLC tag to an MQTT topic as it changes.
    /// </summary>
    /// <remarks>The method observes changes to the specified PLC variable and publishes each new value to the
    /// given MQTT topic. The PLC connection must be configured using the provided action before publishing
    /// begins.</remarks>
    /// <typeparam name="T">The type of the PLC tag value to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC tag value will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the S7 PLC variable to observe. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the S7 PLC connection before publishing. Cannot be null.</param>
    /// <returns>An observable sequence of results for each publish operation to the specified MQTT topic.</returns>
    public static IObservable<MqttClientPublishResult> PublishS7PlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var s7plc = default(IRxS7)!;
        configurePlc?.Invoke(s7plc);
        ArgumentNullException.ThrowIfNull(s7plc);

        return client.PublishMessage(s7plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to an MQTT topic and writes the received payload to a specified S7 PLC variable using a custom
    /// payload conversion.
    /// </summary>
    /// <remarks>The method requires that the PLC connection is properly configured via the <paramref
    /// name="configurePlc"/> action before subscription. The method writes each received MQTT message payload to the
    /// specified PLC variable after conversion. This method is typically used to bridge MQTT messages to S7 PLC
    /// variables in real time.</remarks>
    /// <typeparam name="T">The type to which the MQTT message payload is converted before being written to the PLC variable.</typeparam>
    /// <param name="client">The observable sequence of MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Must not be null.</param>
    /// <param name="plcVariable">The name of the S7 PLC variable to which the converted payload will be written.</param>
    /// <param name="configurePlc">An action that configures the S7 PLC connection before subscribing. Must not be null and must assign a valid
    /// IRxS7 instance.</param>
    /// <param name="payloadFactory">A function that converts the received MQTT message payload (as a string) to the type <typeparamref name="T"/>
    /// before writing it to the PLC variable.</param>
    public static void SubscribeS7PlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var s7plc = default(IRxS7)!;
        configurePlc?.Invoke(s7plc);
        ArgumentNullException.ThrowIfNull(s7plc);

        client.SubscribeToTopic(topic).Subscribe(message => s7plc.Value<T>(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }

    /// <summary>
    /// Publishes the value of a specified S7 PLC tag to an MQTT topic as an observable sequence.
    /// </summary>
    /// <remarks>The method observes the specified PLC variable and publishes its value to the given MQTT
    /// topic whenever it changes. The PLC connection must be configured using the provided action before publishing.
    /// The returned observable can be used to monitor the status of each published message.</remarks>
    /// <typeparam name="T">The type to which the PLC tag value is converted before publishing.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC tag value is published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the S7 PLC variable to observe and publish. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the S7 PLC connection before publishing. Cannot be null.</param>
    /// <returns>An observable sequence that signals when each application message has been processed after publishing the PLC
    /// tag value.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var s7plc = default(IRxS7)!;
        configurePlc?.Invoke(s7plc);
        ArgumentNullException.ThrowIfNull(s7plc);

        return client.PublishMessage(s7plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to an MQTT topic and updates a specified S7 PLC variable with values derived from incoming messages.
    /// </summary>
    /// <remarks>This method links incoming MQTT messages to updates on a specified S7 PLC variable. The PLC
    /// connection must be configured using the provided action before subscription. The method does not return a value
    /// and is intended for side-effect usage.</remarks>
    /// <typeparam name="T">The type of the value to be written to the PLC variable.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to. Cannot be null.</param>
    /// <param name="plcVariable">The name of the S7 PLC variable to update. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the S7 PLC connection before subscribing. Cannot be null.</param>
    /// <param name="payloadFactory">A function that converts the incoming message payload to the value of type T to be written to the PLC variable.</param>
    public static void SubscribeS7PlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var s7plc = default(IRxS7)!;
        configurePlc?.Invoke(s7plc);
        ArgumentNullException.ThrowIfNull(s7plc);

        client.SubscribeToTopic(topic).Subscribe(message => s7plc.Value<T>(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
    }
}
