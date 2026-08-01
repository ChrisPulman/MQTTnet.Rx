// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.ABPlcRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.ABPlc;

/// <summary>Provides compatible static asynchronous MQTT helpers for Allen-Bradley PLC tags.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Publishes an Allen-Bradley PLC tag value to an MQTT topic asynchronously.</summary>
    /// <typeparam name="T">The PLC tag value type.</typeparam>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic to publish to.</param>
    /// <param name="plcVariable">The PLC variable to observe.</param>
    /// <param name="plc">The configured PLC connection.</param>
    /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>An asynchronous sequence of MQTT publish results.</returns>
    public static IObservableAsync<MqttClientPublishResult> PublishABPlcTag<T>(
        IObservableAsync<IMqttClient> client,
        string topic,
        string plcVariable,
        IABPlcRx plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(typeWitness);

        return client.PublishABPlcTag(topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Publishes an Allen-Bradley PLC tag value through a resilient MQTT client asynchronously.</summary>
    /// <typeparam name="T">The PLC tag value type.</typeparam>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic to publish to.</param>
    /// <param name="plcVariable">The PLC variable to observe.</param>
    /// <param name="plc">The configured PLC connection.</param>
    /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>(
        IObservableAsync<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IABPlcRx plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(typeWitness);

        return client.PublishABPlcTag(topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Subscribes to an MQTT topic and writes received values to an Allen-Bradley PLC tag.</summary>
    /// <typeparam name="T">The PLC tag value type.</typeparam>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="plcVariable">The PLC variable to update.</param>
    /// <param name="plc">The configured PLC connection.</param>
    /// <param name="payloadFactory">Converts an MQTT payload into a PLC tag value.</param>
    /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
    public static IDisposable SubscribeABPlcTag<T>(
        IObservableAsync<IMqttClient> client,
        string topic,
        string plcVariable,
        IABPlcRx plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        return client.SubscribeABPlcTag(topic, plcVariable, plc, payloadFactory);
    }

    /// <summary>Subscribes to a topic and writes values to a configured Allen-Bradley PLC connection.</summary>
    /// <typeparam name="T">The PLC tag value type.</typeparam>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic to subscribe to.</param>
    /// <param name="plcVariable">The PLC variable to update.</param>
    /// <param name="plc">The configured PLC connection.</param>
    /// <param name="payloadFactory">Converts an MQTT payload into a PLC tag value.</param>
    /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
    public static IDisposable SubscribeABPlcTag<T>(
        IObservableAsync<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IABPlcRx plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        return client.SubscribeABPlcTag(topic, plcVariable, plc, payloadFactory);
    }
}
