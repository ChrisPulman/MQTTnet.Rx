// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.OmronPlc.Reactive;
#else
namespace MQTTnet.Rx.OmronPlc;
#endif

/// <summary>Publishes PLC values using complete MQTT application messages.</summary>
public static class ApplicationMessageBridgeExtensions
{
    /// <summary>Provides complete application-message publishing for PLC values.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes observed PLC values without changing application-message properties.</summary>
        /// <typeparam name="T">The observed PLC value type.</typeparam>
        /// <param name="tag">The PLC tag to observe.</param>
        /// <param name="plc">The configured native driver.</param>
        /// <param name="messageFactory">Creates a message, including binary payload and MQTT 5 properties, for each value.</param>
        /// <returns>The results of publishing the generated messages.</returns>
        public IObservable<MqttClientPublishResult> PublishOmronPlcTag<T>(
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return client.PublishMessage(
                plc.Observe(tag).Select(messageFactory));
        }
    }

    /// <summary>Provides complete application-message publishing for PLC values.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed PLC values without changing application-message properties.</summary>
        /// <typeparam name="T">The observed PLC value type.</typeparam>
        /// <param name="tag">The PLC tag to observe.</param>
        /// <param name="plc">The configured native driver.</param>
        /// <param name="messageFactory">Creates a message, including binary payload and MQTT 5 properties, for each value.</param>
        /// <returns>The results of publishing the generated messages.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>(
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return client.PublishMessage(
                plc.Observe(tag).Select(messageFactory));
        }
    }

    /// <summary>Provides complete application-message publishing for PLC values.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes observed PLC values without changing application-message properties.</summary>
        /// <typeparam name="T">The observed PLC value type.</typeparam>
        /// <param name="tag">The PLC tag to observe.</param>
        /// <param name="plc">The configured native driver.</param>
        /// <param name="messageFactory">Creates a message, including binary payload and MQTT 5 properties, for each value.</param>
        /// <returns>The results of publishing the generated messages.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishOmronPlcTag<T>(
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronPlcTag(tag, plc, messageFactory));
        }
    }

    /// <summary>Provides complete application-message publishing for PLC values.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed PLC values without changing application-message properties.</summary>
        /// <typeparam name="T">The observed PLC value type.</typeparam>
        /// <param name="tag">The PLC tag to observe.</param>
        /// <param name="plc">The configured native driver.</param>
        /// <param name="messageFactory">Creates a message, including binary payload and MQTT 5 properties, for each value.</param>
        /// <returns>The results of publishing the generated messages.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>(
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronPlcTag(tag, plc, messageFactory));
        }
    }
}
