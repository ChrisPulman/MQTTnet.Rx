// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Provides MQTT helpers for publishing and subscribing to TwinCAT PLC variables.</summary>
/// <remarks>
/// All helpers bridge an MQTT client stream and an already configured TwinCAT connection or hash table.
/// </remarks>
public static class CreateExtensions
{
    /// <summary>The error raised when TwinCAT emits a null value.</summary>
    private const string NullObservedValueMessage = "The observed TwinCAT value cannot be null.";

    /// <summary>Provides MQTT helpers for standard MQTT clients.</summary>
    /// <param name="client">The observable sequence of MQTT clients.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes a TwinCAT PLC variable to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                TwinCatRxExtensions.Observe(plc, plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT hash-table value to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT PLC variable.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())))));
        }

    }

    /// <summary>Provides MQTT helpers for resilient MQTT clients.</summary>
    /// <param name="client">The observable sequence of resilient MQTT clients.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes a TwinCAT PLC variable through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                TwinCatRxExtensions.Observe(plc, plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT hash-table value through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through a TwinCAT connection.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())))));
        }
    }

    /// <summary>Converts a non-null TwinCAT value to the requested type.</summary>
    /// <typeparam name="T">The requested TwinCAT value type.</typeparam>
    /// <param name="value">The untyped TwinCAT value.</param>
    /// <returns>The typed TwinCAT value.</returns>
    private static T ConvertObservedValue<T>(object? value)
    {
        if (value is null)
        {
            throw new InvalidOperationException(NullObservedValueMessage);
        }

        return (T)value;
    }

    /// <summary>Converts a non-null TwinCAT value to its MQTT payload text.</summary>
    /// <typeparam name="T">The TwinCAT value type.</typeparam>
    /// <param name="value">The typed TwinCAT value.</param>
    /// <returns>The MQTT payload text.</returns>
    private static string ConvertPayloadToString<T>(T value) =>
        value?.ToString() ?? throw new InvalidOperationException(NullObservedValueMessage);

    /// <summary>Ensures a payload factory produced a value that can be written to TwinCAT.</summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="value">The payload factory result.</param>
    /// <returns>The non-null value to write.</returns>
    private static object RequireWriteValue<T>(T value) =>
        value is not null
            ? value
            : throw new InvalidOperationException("The converted TwinCAT value cannot be null.");
}
