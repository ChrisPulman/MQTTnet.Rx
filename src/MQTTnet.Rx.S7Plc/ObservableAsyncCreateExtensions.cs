// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.S7Plc;

/// <summary>Provides asynchronous-observable MQTT bridges for typed S7 PLC tags.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides S7 publishing for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence that publishes observed S7 values.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes each observed S7 tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the tag values.</param>
        /// <param name="tag">The typed S7 tag to observe.</param>
        /// <param name="plc">The S7 PLC connection that supplies tag values.</param>
        /// <returns>An asynchronous observable sequence containing the result of each MQTT publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishS7PlcTag(topic, tag, plc).ToSignal();
        }

        /// <summary>Writes converted MQTT payloads to an S7 tag.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the S7 tag.</param>
        /// <param name="tag">The typed S7 tag to update.</param>
        /// <param name="plc">The S7 PLC connection that receives the values.</param>
        /// <param name="payloadFactory">Converts each MQTT payload into an S7 tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.ToObservable().SubscribeS7PlcTag(topic, tag, plc, payloadFactory);
        }
    }

    /// <summary>Provides S7 publishing for resilient asynchronous MQTT client sequences.</summary>
    /// <param name="client">The resilient asynchronous MQTT client sequence that publishes observed S7 values.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes each observed S7 tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the tag values.</param>
        /// <param name="tag">The typed S7 tag to observe.</param>
        /// <param name="plc">The S7 PLC connection that supplies tag values.</param>
        /// <returns>
        /// An asynchronous observable containing the result of each resilient MQTT publish operation.
        /// </returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishS7PlcTag(topic, tag, plc).ToSignal();
        }

        /// <summary>Writes converted MQTT payloads to an S7 tag.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the S7 tag.</param>
        /// <param name="tag">The typed S7 tag to update.</param>
        /// <param name="plc">The S7 PLC connection that receives the values.</param>
        /// <param name="payloadFactory">Converts each MQTT payload into an S7 tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.ToObservable().SubscribeS7PlcTag(topic, tag, plc, payloadFactory);
        }
    }
}
