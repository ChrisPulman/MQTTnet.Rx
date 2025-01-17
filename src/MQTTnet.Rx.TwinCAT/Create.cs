// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.TwinCAT
{
    /// <summary>
    /// Create.
    /// </summary>
    public static class Create
    {
        /// <summary>
        /// Publishes the ab PLC tag.
        /// </summary>
        /// <typeparam name="T">>The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <returns>MqttClientPublishResult.</returns>
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
        /// Subscribes the tc tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <param name="payloadFactory">The payload factory.</param>
        /// <exception cref="ArgumentNullException">
        /// nameof(client)
        /// or
        /// nameof(configurePlc)
        /// or
        /// nameof(plc).
        /// </exception>
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
        /// Publishes the ab PLC tag.
        /// </summary>
        /// <typeparam name="T">>The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <returns>MqttClientPublishResult.</returns>
        public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(configurePlc);

            var plc = default(HashTableRx)!;
            configurePlc?.Invoke(plc);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /////// <summary>
        /////// Publishes the tc PLC tag.
        /////// </summary>
        /////// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="plcVariable">The PLC variable.</param>
        /////// <param name="configurePlc">The configure PLC.</param>
        /////// <returns>MqttClientPublishResult.</returns>
        /////// <exception cref="ArgumentNullException">
        /////// nameof(client)
        /////// or
        /////// nameof(configurePlc)
        /////// or
        /////// nameof(plc).
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePlc);

        ////    var plc = default(IRxTcAdsClient)!;
        ////    configurePlc?.Invoke(plc);
        ////    ArgumentNullException.ThrowIfNull(plc);

        ////    return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        ////}

        /////// <summary>
        /////// Subscribes the tc tag.
        /////// </summary>
        /////// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="plcVariable">The PLC variable.</param>
        /////// <param name="configurePlc">The configure PLC.</param>
        /////// <param name="payloadFactory">The payload factory.</param>
        /////// <exception cref="ArgumentNullException">
        /////// nameof(client)
        /////// or
        /////// nameof(configurePlc)
        /////// or
        /////// nameof(s7plc).
        /////// </exception>
        ////public static void SubscribeTcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc, Func<string, T> payloadFactory)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePlc);

        ////    var plc = default(IRxTcAdsClient)!;
        ////    configurePlc?.Invoke(plc);
        ////    ArgumentNullException.ThrowIfNull(plc);

        ////    client.SubscribeToTopic(topic).Subscribe(message => plc.Write(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
        ////}

        /////// <summary>
        /////// Publishes the tc PLC tag.
        /////// </summary>
        /////// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /////// <param name="client">The client.</param>
        /////// <param name="topic">The topic.</param>
        /////// <param name="plcVariable">The PLC variable.</param>
        /////// <param name="configurePlc">The configure PLC.</param>
        /////// <returns>MqttClientPublishResult.</returns>
        /////// <exception cref="ArgumentNullException">
        /////// nameof(client)
        /////// or
        /////// nameof(configurePlc)
        /////// or
        /////// nameof(plc).
        /////// </exception>
        ////public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
        ////{
        ////    ArgumentNullException.ThrowIfNull(client);
        ////    ArgumentNullException.ThrowIfNull(configurePlc);

        ////    var plc = default(HashTableRx)!;
        ////    configurePlc?.Invoke(plc);
        ////    ArgumentNullException.ThrowIfNull(plc);

        ////    return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        ////}
    }
}
