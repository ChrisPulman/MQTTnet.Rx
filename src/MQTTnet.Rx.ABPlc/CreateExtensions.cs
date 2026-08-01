// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.ABPlc.Reactive;
#else
namespace MQTTnet.Rx.ABPlc;
#endif

/// <summary>Provides MQTT extensions for publishing and subscribing to Allen-Bradley PLC tags.</summary>
public static class CreateExtensions
{
    /// <summary>Extends a standard MQTT client sequence.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes an Allen-Bradley PLC tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishABPlcTag<T>(
            string topic,
            string plcVariable,
            IABPlcRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(typeWitness);

            return client.PublishMessage(
                plc.Observe(plcVariable, default(T), -1)
                    .Select(payload => (topic, Payload: payload?.ToString() ?? string.Empty)));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to an Allen-Bradley PLC tag.</summary>
        /// <typeparam name="T">The PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTag<T>(
            string topic,
            string plcVariable,
            IABPlcRx plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                ObserverFactory.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Value(
                        plcVariable,
                        payloadFactory(message.ApplicationMessage.ConvertPayloadToString()),
                        -1)));
        }
    }

    /// <summary>Extends a resilient MQTT client sequence.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes an Allen-Bradley PLC tag value through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="typeWitness">Values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>(
            string topic,
            string plcVariable,
            IABPlcRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(typeWitness);

            return client.PublishMessage(
                plc.Observe(plcVariable, default(T), -1)
                    .Select(payload => (topic, Payload: payload?.ToString() ?? string.Empty)));
        }

        /// <summary>Subscribes to a topic and writes values to a configured Allen-Bradley PLC connection.</summary>
        /// <typeparam name="T">The PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTag<T>(
            string topic,
            string plcVariable,
            IABPlcRx plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                ObserverFactory.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Value(
                        plcVariable,
                        payloadFactory(message.ApplicationMessage.ConvertPayloadToString()),
                        -1)));
        }
    }
}
