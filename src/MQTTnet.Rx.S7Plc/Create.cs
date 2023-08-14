﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Rx.Client;
using S7PlcRx;

namespace MQTTnet.Rx.S7Plc
{
    /// <summary>
    /// Create.
    /// </summary>
    public static class Create
    {
        /// <summary>
        /// Publishes the s7 PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="plcVariable">The variable.</param>
        /// <param name="configurePlc">The configure S7PLC.</param>
        /// <returns>
        /// MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// configureS7plc
        /// or
        /// s7plc.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishS7PlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var s7plc = default(IRxS7)!;
            configurePlc(s7plc);
            if (s7plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(s7plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            return client.PublishMessage(s7plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /// <summary>
        /// Subscribes the s7 PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure PLC.</param>
        /// <param name="payloadFactory">The payload factory, convert from Json message to PLC type T.</param>
        /// <exception cref="ArgumentNullException">nameof(client)
        /// or
        /// nameof(configurePlc)
        /// or
        /// nameof(s7plc).</exception>
        public static void SubscribeS7PlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var s7plc = default(IRxS7)!;
            configurePlc(s7plc);
            if (s7plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(s7plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            client.SubscribeToTopic(topic).Subscribe(message => s7plc.Value<T>(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        }

        /// <summary>
        /// Publishes the s7 PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure S7PLC.</param>
        /// <returns>A ApplicationMessageProcessedEventArgs.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// configureS7plc
        /// or
        /// s7plc.
        /// </exception>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var s7plc = default(IRxS7)!;
            configurePlc(s7plc);
            if (s7plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(s7plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            return client.PublishMessage(s7plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /// <summary>
        /// Subscribes the s7 PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC type.</typeparam>
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
        /// nameof(s7plc).
        /// </exception>
        public static void SubscribeS7PlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var s7plc = default(IRxS7)!;
            configurePlc(s7plc);
            if (s7plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(s7plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            client.SubscribeToTopic(topic).Subscribe(message => s7plc.Value<T>(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())));
        }
    }
}
