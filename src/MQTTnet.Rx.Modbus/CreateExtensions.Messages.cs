// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides reactive MQTT extensions for Modbus reads and writes.</summary>
public static partial class CreateExtensions
{
    /// <summary>Extends standard MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes native Modbus readings as fully configured MQTT messages.</summary>
        /// <typeparam name="TReading">The native reading or slave event type.</typeparam>
        /// <param name="reader">The reading sequence, including serial-master and slave events.</param>
        /// <param name="messageFactory">Creates a message with all supported MQTT properties.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<MqttClientPublishResult> PublishModbusMessages<TReading>(
            IObservable<TReading> reader,
            Func<TReading, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(reader.Select(messageFactory));
        }
    }

    /// <summary>Extends resilient MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes native Modbus readings as fully configured MQTT messages.</summary>
        /// <typeparam name="TReading">The native reading or slave event type.</typeparam>
        /// <param name="reader">The reading sequence, including serial-master and slave events.</param>
        /// <param name="messageFactory">Creates a message with all supported MQTT properties.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishModbusMessages<TReading>(
            IObservable<TReading> reader,
            Func<TReading, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(reader.Select(messageFactory));
        }
    }

    /// <summary>Extends standard MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes native Modbus readings as fully configured MQTT messages.</summary>
        /// <typeparam name="TReading">The native reading or slave event type.</typeparam>
        /// <param name="reader">The reading sequence, including serial-master and slave events.</param>
        /// <param name="messageFactory">Creates a message with all supported MQTT properties.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishModbusMessages<TReading>(
            IObservableAsync<TReading> reader,
            Func<TReading, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishModbusMessages(reader.ToObservable(), messageFactory).ToMqttAsyncSignal();
        }
    }

    /// <summary>Extends resilient MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes native Modbus readings as fully configured MQTT messages.</summary>
        /// <typeparam name="TReading">The native reading or slave event type.</typeparam>
        /// <param name="reader">The reading sequence, including serial-master and slave events.</param>
        /// <param name="messageFactory">Creates a message with all supported MQTT properties.</param>
        /// <returns>The publish result sequence.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishModbusMessages<TReading>(
            IObservableAsync<TReading> reader,
            Func<TReading, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishModbusMessages(reader.ToObservable(), messageFactory).ToMqttAsyncSignal();
        }
    }
}
