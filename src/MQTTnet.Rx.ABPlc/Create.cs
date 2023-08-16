﻿// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using ABPlcRx;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Rx.Client;

namespace MQTTnet.Rx.ABPlc
{
    /// <summary>
    /// Create.
    /// </summary>
    public static class Create
    {
        /// <summary>
        /// Publishes the AB PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="plcVariable">The variable.</param>
        /// <param name="configurePlc">The configure AB PLC.</param>
        /// <returns>
        /// MqttClientPublishResult.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// configure AB plc
        /// or
        /// AB plc.
        /// </exception>
        public static IObservable<MqttClientPublishResult> PublishABPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(IABPlcRx)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

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
        public static void SubscribeABPlcTag<T>(this IObservable<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(IABPlcRx)!;
            configurePlc(plc);
            if (plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            client.SubscribeToTopic(topic).Subscribe(message => plc.Value(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
        }

        /// <summary>
        /// Publishes the AB PLC tag.
        /// </summary>
        /// <typeparam name="T">The PLC Tag Data Type.</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="topic">The publish topic.</param>
        /// <param name="plcVariable">The PLC variable.</param>
        /// <param name="configurePlc">The configure AB PLC.</param>
        /// <returns>A ApplicationMessageProcessedEventArgs.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// client
        /// or
        /// configure AB plc
        /// or
        /// AB plc.
        /// </exception>
        public static IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(IABPlcRx)!;
            configurePlc(plc);
            if (plc == null)
            {
                throw new ArgumentNullException(nameof(plc));
            }

            return client.PublishMessage(plc.Observe<T>(plcVariable).Select(payLoad => (topic, payLoad: payLoad!.ToString()!)));
        }

        /// <summary>
        /// Subscribes the ab PLC tag.
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
        public static void SubscribeABPlcTag<T>(this IObservable<IManagedMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (configurePlc == null)
            {
                throw new ArgumentNullException(nameof(configurePlc));
            }

            var plc = default(IABPlcRx)!;
            configurePlc(plc);
            if (plc == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(plc));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            client.SubscribeToTopic(topic).Subscribe(message => plc.Value(plcVariable, payloadFactory(message.ApplicationMessage.ConvertPayloadToString())!));
        }
    }
}
