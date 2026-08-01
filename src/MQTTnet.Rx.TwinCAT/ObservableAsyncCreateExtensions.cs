// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.TwinCAT;

/// <summary>Provides asynchronous MQTT helpers for TwinCAT PLC variables.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides asynchronous MQTT helpers for standard MQTT clients.</summary>
    /// <param name="client">The asynchronous observable sequence of MQTT clients.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes a TwinCAT PLC variable to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToSignal();
        }

        /// <summary>Publishes a TwinCAT hash-table value to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToSignal();
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT PLC variable.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, payloadFactory);
        }

    }

    /// <summary>Provides asynchronous MQTT helpers for resilient MQTT clients.</summary>
    /// <param name="client">The asynchronous observable sequence of resilient MQTT clients.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes a TwinCAT PLC variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToSignal();
        }

        /// <summary>Publishes a TwinCAT hash-table value through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToSignal();
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through a TwinCAT connection.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, payloadFactory);
        }
    }
}
