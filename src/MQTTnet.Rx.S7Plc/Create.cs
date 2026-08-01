// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.S7Plc;

/// <summary>Provides string-tag compatibility helpers for S7 MQTT bridges.</summary>
/// <remarks>Prefer <see cref="S7PlcExtensions"/> with a <see cref="LogicalTagKey{T}"/> in new code.</remarks>
public static class Create
{
    /// <summary>Publishes an S7 tag to an MQTT topic.</summary>
    /// <typeparam name="T">The S7 tag value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The S7 tag name.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The MQTT publish results.</returns>
    public static IObservable<MqttClientPublishResult> PublishS7PlcTag<T>(
        IObservable<IMqttClient> client,
        string topic,
        string plcVariable,
        IRxS7 plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return S7PlcExtensions.PublishS7PlcTag<T>(client, topic, new(plcVariable), plc);
    }

    /// <summary>Publishes an S7 tag through a resilient MQTT client.</summary>
    /// <typeparam name="T">The S7 tag value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The S7 tag name.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The resilient MQTT publish results.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(
        IObservable<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IRxS7 plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return S7PlcExtensions.PublishS7PlcTag<T>(client, topic, new(plcVariable), plc);
    }

    /// <summary>Subscribes to an MQTT topic and writes converted values to an S7 tag.</summary>
    /// <typeparam name="T">The S7 tag value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The source MQTT topic.</param>
    /// <param name="plcVariable">The S7 tag name.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="payloadFactory">Converts MQTT payloads to S7 values.</param>
    public static void SubscribeS7PlcTag<T>(
        IObservable<IMqttClient> client,
        string topic,
        string plcVariable,
        IRxS7 plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        _ = S7PlcExtensions.SubscribeS7PlcTag(client, topic, new(plcVariable), plc, payloadFactory);
    }

    /// <summary>Subscribes to an MQTT topic and writes converted values to an S7 tag.</summary>
    /// <typeparam name="T">The S7 tag value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="topic">The source MQTT topic.</param>
    /// <param name="plcVariable">The S7 tag name.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="payloadFactory">Converts MQTT payloads to S7 values.</param>
    public static void SubscribeS7PlcTag<T>(
        IObservable<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IRxS7 plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        _ = S7PlcExtensions.SubscribeS7PlcTag(client, topic, new(plcVariable), plc, payloadFactory);
    }
}
