// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
using ObservableSignalConversion = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
namespace MQTTnet.Rx.TwinCAT;
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
        public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
            string tag,
            IHashTableRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return client.PublishMessage(
                plc.Observe(tag, static value => (T?)value).Select(messageFactory));
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
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string tag,
            IHashTableRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return client.PublishMessage(
                plc.Observe(tag, static value => (T?)value).Select(messageFactory));
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
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(
            string tag,
            IHashTableRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishTcPlcTag(tag, plc, messageFactory));
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
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string tag,
            IHashTableRx plc,
            Func<T?, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(messageFactory);

            return ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishTcPlcTag(tag, plc, messageFactory));
        }
    }
}
