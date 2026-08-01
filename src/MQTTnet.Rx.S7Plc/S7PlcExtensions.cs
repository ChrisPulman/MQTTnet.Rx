// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Advanced;
using ReactiveUI.Primitives.Reactive;

namespace MQTTnet.Rx.S7Plc;

/// <summary>Provides reactive MQTT bridges for typed S7 PLC tags.</summary>
/// <remarks>
/// The helpers use the S7 driver's <see cref="LogicalTagKey{T}"/> contract so a tag's value type is inferred from
/// the call site. Dispose subscriptions returned by the write helpers to stop the MQTT-to-PLC bridge.
/// </remarks>
public static class S7PlcExtensions
{
    /// <summary>Provides S7 publishing for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence that publishes observed S7 values.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes each observed S7 tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the tag values.</param>
        /// <param name="tag">The typed S7 tag to observe.</param>
        /// <param name="plc">The S7 PLC connection that supplies tag values.</param>
        /// <returns>An observable sequence containing the result of each MQTT publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(tag).Select(
                    payload => (topic, Payload: payload?.ToString() ?? string.Empty)));
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

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Value(
                        tag.Name,
                        payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
        }
    }

    /// <summary>Provides S7 publishing for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence that publishes observed S7 values.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes each observed S7 tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The S7 tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the tag values.</param>
        /// <param name="tag">The typed S7 tag to observe.</param>
        /// <param name="plc">The S7 PLC connection that supplies tag values.</param>
        /// <returns>An observable sequence containing the result of each resilient MQTT publish operation.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IRxS7 plc)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(tag).Select(
                    payload => (topic, Payload: payload?.ToString() ?? string.Empty)));
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

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Value(
                        tag.Name,
                        payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
        }
    }
}
