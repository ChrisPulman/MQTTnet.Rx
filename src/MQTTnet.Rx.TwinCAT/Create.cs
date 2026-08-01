// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.TwinCAT;

/// <summary>Provides string-variable compatibility helpers for TwinCAT MQTT bridges.</summary>
/// <remarks>Prefer <see cref="CreateExtensions"/> in new code.</remarks>
public static class Create
{
    /// <summary>Publishes a TwinCAT ADS variable to an MQTT topic.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT ADS client.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The MQTT publish results.</returns>
    public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
        IObservable<IMqttClient> client,
        string topic,
        string plcVariable,
        IRxTcAdsClient plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return CreateExtensions.PublishTcPlcTag(client, topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Publishes a TwinCAT hash-table value to an MQTT topic.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT hash table.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The MQTT publish results.</returns>
    public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
        IObservable<IMqttClient> client,
        string topic,
        string plcVariable,
        IHashTableRx plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return CreateExtensions.PublishTcPlcTag(client, topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Publishes a TwinCAT ADS variable through a resilient MQTT client.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT ADS client.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The resilient MQTT publish results.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
        IObservable<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IRxTcAdsClient plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return CreateExtensions.PublishTcPlcTag(client, topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Publishes a TwinCAT hash-table value through a resilient MQTT client.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="topic">The destination MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT hash table.</param>
    /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
    /// <returns>The resilient MQTT publish results.</returns>
    public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
        IObservable<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IHashTableRx plc,
        params T[] typeWitness)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);

        return CreateExtensions.PublishTcPlcTag(client, topic, plcVariable, plc, typeWitness);
    }

    /// <summary>Subscribes to an MQTT topic and writes converted values to a TwinCAT ADS variable.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The source MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT ADS client.</param>
    /// <param name="payloadFactory">Converts MQTT payloads to TwinCAT values.</param>
    public static void SubscribeTcTag<T>(
        IObservable<IMqttClient> client,
        string topic,
        string plcVariable,
        IRxTcAdsClient plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        _ = CreateExtensions.SubscribeTcTag(client, topic, plcVariable, plc, payloadFactory);
    }

    /// <summary>Subscribes to an MQTT topic and writes converted values to a TwinCAT ADS variable.</summary>
    /// <typeparam name="T">The PLC variable value type.</typeparam>
    /// <param name="client">The resilient MQTT client sequence.</param>
    /// <param name="topic">The source MQTT topic.</param>
    /// <param name="plcVariable">The TwinCAT variable name.</param>
    /// <param name="plc">The TwinCAT ADS client.</param>
    /// <param name="payloadFactory">Converts MQTT payloads to TwinCAT values.</param>
    public static void SubscribeTcTag<T>(
        IObservable<IResilientMqttClient> client,
        string topic,
        string plcVariable,
        IRxTcAdsClient plc,
        Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(topic);
        ArgumentNullException.ThrowIfNull(plcVariable);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        _ = CreateExtensions.SubscribeTcTag(client, topic, plcVariable, plc, payloadFactory);
    }
}
