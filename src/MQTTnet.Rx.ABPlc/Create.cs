// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using ABPlcRx;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.ABPlc;

/// <summary>
/// Provides extension methods for publishing and subscribing to Allen-Bradley PLC tags over MQTT using reactive
/// streams.
/// </summary>
/// <remarks>The Create class contains static methods that integrate Allen-Bradley PLC communication with
/// MQTT clients in a reactive programming model. These methods are intended for use with IObservable-based MQTT
/// client implementations, enabling seamless data flow between PLC tags and MQTT topics. All methods are static and
/// are designed to be used as extension methods.</remarks>
public static class Create
{
    /// <summary>
    /// Publishes the value of an Allen-Bradley PLC tag to the specified MQTT topic as an observable sequence.
    /// </summary>
    /// <remarks>The observable emits a publish result each time the specified PLC tag value is
    /// observed and published. The PLC connection must be properly configured in the provided action before
    /// publishing. This method requires that the PLC and MQTT client are both available and properly
    /// initialized.</remarks>
    /// <typeparam name="T">The type of the PLC tag value to observe and publish.</typeparam>
    /// <param name="client">An observable sequence of MQTT clients used to publish the message.</param>
    /// <param name="topic">The MQTT topic to which the PLC tag value will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the PLC variable (tag) to observe and publish. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the PLC connection before publishing. Cannot be null.</param>
    /// <returns>An observable sequence of results for each publish operation, representing the outcome of publishing the PLC
    /// tag value to the MQTT topic.</returns>
    public static IObservable<MqttClientPublishResult> PublishABPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IABPlcRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to an MQTT topic and updates an Allen-Bradley PLC variable with the deserialized payload from
    /// each received message.
    /// </summary>
    /// <remarks>The configurePlc action must assign a non-null IABPlcRx instance; otherwise, an
    /// exception is thrown. This method enables reactive integration between MQTT message streams and Allen-Bradley
    /// PLC variables.</remarks>
    /// <typeparam name="T">The type to which the MQTT message payload is deserialized before being written to the PLC variable.</typeparam>
    /// <param name="client">The observable sequence of MQTT client connections used to subscribe to the specified topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving messages.</param>
    /// <param name="plcVariable">The name of the Allen-Bradley PLC variable to update with the received payload.</param>
    /// <param name="configurePlc">An action that configures the PLC receiver instance before subscription. Must assign a valid IABPlcRx
    /// instance.</param>
    /// <param name="payloadFactory">A function that converts the received message payload string into an instance of type T for writing to the
    /// PLC variable.</param>
    public static void SubscribeABPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IABPlcRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        client.SubscribeToTopic(topic).Subscribe(message => plc.Value(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
    }

    /// <summary>
    /// Publishes the value of an Allen-Bradley PLC tag to the specified MQTT topic as an observable message stream.
    /// </summary>
    /// <remarks>The method observes the specified PLC tag and publishes its value to the given MQTT
    /// topic whenever it changes. The PLC connection must be properly configured within the configurePlc action.
    /// The returned observable emits an event for each message published.</remarks>
    /// <typeparam name="T">The type of the PLC tag value to observe and publish.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to publish messages.</param>
    /// <param name="topic">The MQTT topic to which the PLC tag value will be published. Cannot be null.</param>
    /// <param name="plcVariable">The name of the PLC variable (tag) to observe and publish. Cannot be null.</param>
    /// <param name="configurePlc">An action to configure the PLC connection before publishing. Cannot be null and must assign a valid IABPlcRx
    /// instance.</param>
    /// <returns>An observable sequence of ApplicationMessageProcessedEventArgs representing the result of each published
    /// message.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IABPlcRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
    }

    /// <summary>
    /// Subscribes to an MQTT topic and updates an Allen-Bradley PLC variable with values produced from incoming
    /// messages.
    /// </summary>
    /// <remarks>The PLC interface instance must be assigned within the configurePlc action;
    /// otherwise, an exception is thrown. This method enables reactive updates to PLC variables based on MQTT
    /// message streams.</remarks>
    /// <typeparam name="T">The type of the value produced from the MQTT message payload and written to the PLC variable.</typeparam>
    /// <param name="client">The observable sequence of resilient MQTT clients used to subscribe to the specified topic.</param>
    /// <param name="topic">The MQTT topic to subscribe to for receiving messages.</param>
    /// <param name="plcVariable">The name of the Allen-Bradley PLC variable to update with values from incoming messages.</param>
    /// <param name="configurePlc">An action that configures the PLC interface before subscription. The provided IABPlcRx instance must be
    /// initialized within this action.</param>
    /// <param name="payloadFactory">A function that converts the MQTT message payload (as a string) to the value of type T to be written to the
    /// PLC variable.</param>
    public static void SubscribeABPlcTag<T>(this IObservable<IResilientMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configurePlc);

        var plc = default(IABPlcRx)!;
        configurePlc?.Invoke(plc);
        ArgumentNullException.ThrowIfNull(plc);

        client.SubscribeToTopic(topic).Subscribe(message => plc.Value(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
    }
}
