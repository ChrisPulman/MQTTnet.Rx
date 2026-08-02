// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.OmronPlc.Reactive;
#else
namespace MQTTnet.Rx.OmronPlc;
#endif

/// <summary>Provides asynchronous-observable MQTT helpers for typed Omron PLC tags.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides typed Omron tag helpers for asynchronous raw MQTT clients.</summary>
    /// <param name="client">The asynchronous raw MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes each observed value of a typed Omron PLC tag.</summary>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives tag values.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <returns>An asynchronous sequence containing each MQTT publish result.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc)
        {
            ArgumentNullException.ThrowIfNull(client);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronPlcTag(topic, tag, plc));
        }

        /// <summary>Writes MQTT payloads to a typed Omron PLC tag.</summary>
        /// <remarks>Dispose the returned subscription to stop the asynchronous MQTT-to-PLC flow.</remarks>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the PLC.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <param name="payloadFactory">Converts an MQTT string payload to the PLC value type.</param>
        /// <returns>A disposable subscription that owns the asynchronous MQTT-to-PLC write flow.</returns>
        public IDisposable SubscribeOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.ToObservable()
                .SubscribeOmronPlcTag(topic, tag, plc, payloadFactory);
        }
    }

    /// <summary>Provides typed Omron tag helpers for asynchronous resilient MQTT clients.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes each observed value of a typed Omron PLC tag.</summary>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives tag values.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <returns>An asynchronous sequence containing each resilient MQTT publish result.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc)
        {
            ArgumentNullException.ThrowIfNull(client);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronPlcTag(topic, tag, plc));
        }

        /// <summary>Writes MQTT payloads to a typed Omron PLC tag.</summary>
        /// <remarks>Dispose the returned subscription to stop the asynchronous MQTT-to-PLC flow.</remarks>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the PLC.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <param name="payloadFactory">Converts an MQTT string payload to the PLC value type.</param>
        /// <returns>A disposable subscription that owns the asynchronous MQTT-to-PLC write flow.</returns>
        public IDisposable SubscribeOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.ToObservable()
                .SubscribeOmronPlcTag(topic, tag, plc, payloadFactory);
        }
    }
}
