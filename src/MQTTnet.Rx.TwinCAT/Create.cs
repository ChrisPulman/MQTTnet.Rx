// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using CP.Collections;
using CP.TwinCatRx;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
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
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(RxTcAdsClient)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
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
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(HashTableRx)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /// <summary>
        /// Publishes the tc PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <returns>MqttClientPublishResult.</returns>
        /// <exception cref="ArgumentNullException">
        /// nameof(client)
        /// or
        /// nameof(configurePlc)
        /// or
        /// nameof(plc).
        /// </exception>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(RxTcAdsClient)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /// <summary>
        /// Publishes the tc PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <returns>MqttClientPublishResult.</returns>
        /// <exception cref="ArgumentNullException">
        /// nameof(client)
        /// or
        /// nameof(configurePlc)
        /// or
        /// nameof(plc).
        /// </exception>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(HashTableRx)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }
    }
}