// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using IoT.Driver.Core;
using IoT.Driver.OmronPlcRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Reactive;

namespace MQTTnet.Rx.OmronPlc;

/// <summary>Provides typed MQTT publishing and subscription helpers for Omron PLC tags.</summary>
/// <remarks>
/// The helpers depend on <see cref="IOmronPlcRx"/> so production drivers and the deterministic
/// <see cref="OmronPlcSimulator"/> can be supplied without changing the MQTT integration.
/// Register each tag with the PLC before using its matching <see cref="LogicalTagKey{T}"/>.
/// </remarks>
public static class OmronPlcCreateExtensions
{
    /// <summary>Provides typed Omron tag helpers for raw MQTT clients.</summary>
    /// <param name="client">The raw MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes each observed value of a typed Omron PLC tag.</summary>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives tag values.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <returns>A sequence containing the result of each MQTT publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc)
        {
            Validate(client, topic, tag, plc);

            return client.PublishMessage(
                plc.Observe(tag).Select(value => (topic, Payload: ToPayload(value))));
        }

        /// <summary>Writes MQTT payloads to a typed Omron PLC tag.</summary>
        /// <remarks>
        /// Each value waits for the PLC write to complete. Dispose the returned subscription to stop the flow.
        /// MQTT subscription errors are written to trace listeners.
        /// </remarks>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the PLC.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <param name="payloadFactory">Converts an MQTT string payload to the PLC value type.</param>
        /// <returns>A disposable subscription that owns the MQTT-to-PLC write flow.</returns>
        public IDisposable SubscribeOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory)
        {
            Validate(client, topic, tag, plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic)
                .Subscribe(new OmronWriteObserver<T>(plc, tag, payloadFactory));
        }
    }

    /// <summary>Provides typed Omron tag helpers for resilient MQTT clients.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes each observed value of a typed Omron PLC tag.</summary>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives tag values.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <returns>A sequence containing the result of each resilient MQTT publish operation.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc)
        {
            Validate(client, topic, tag, plc);

            return client.PublishMessage(
                plc.Observe(tag).Select(value => (topic, Payload: ToPayload(value))));
        }

        /// <summary>Writes MQTT payloads to a typed Omron PLC tag.</summary>
        /// <remarks>
        /// Each value waits for the PLC write to complete. Dispose the returned subscription to stop the flow.
        /// MQTT subscription errors are written to trace listeners.
        /// </remarks>
        /// <typeparam name="T">The registered PLC tag value type.</typeparam>
        /// <param name="topic">The MQTT topic whose payloads are written to the PLC.</param>
        /// <param name="tag">The typed logical key of the registered Omron tag.</param>
        /// <param name="plc">
        /// The Omron PLC facade; an <see cref="OmronPlcSimulator"/> may be supplied for tests.
        /// </param>
        /// <param name="payloadFactory">Converts an MQTT string payload to the PLC value type.</param>
        /// <returns>A disposable subscription that owns the MQTT-to-PLC write flow.</returns>
        public IDisposable SubscribeOmronPlcTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory)
        {
            Validate(client, topic, tag, plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic)
                .Subscribe(new OmronWriteObserver<T>(plc, tag, payloadFactory));
        }
    }

    /// <summary>Validates common publish and subscription arguments.</summary>
    /// <typeparam name="TClient">The observable client value type.</typeparam>
    /// <typeparam name="TValue">The registered PLC tag value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="tag">The typed tag key.</param>
    /// <param name="plc">The PLC facade.</param>
    private static void Validate<TClient, TValue>(
        IObservable<TClient> client,
        string topic,
        LogicalTagKey<TValue> tag,
        IOmronPlcRx plc)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(plc);
    }

    /// <summary>Formats a typed PLC value as an invariant MQTT string payload.</summary>
    /// <typeparam name="T">The PLC value type.</typeparam>
    /// <param name="value">The PLC value.</param>
    /// <returns>The invariant string payload, or an empty string for a null value.</returns>
    private static string ToPayload<T>(T? value) =>
        Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Writes received MQTT payloads to a typed Omron PLC tag.</summary>
    /// <typeparam name="T">The registered PLC tag value type.</typeparam>
    /// <param name="plc">The PLC facade that receives values.</param>
    /// <param name="tag">The typed destination tag.</param>
    /// <param name="payloadFactory">The incoming payload converter.</param>
    private sealed class OmronWriteObserver<T>(
        IOmronPlcRx plc,
        LogicalTagKey<T> tag,
        Func<string, T> payloadFactory) : IObserver<MqttApplicationMessageReceivedEventArgs>
    {
        /// <summary>The PLC facade that receives values.</summary>
        private readonly IOmronPlcRx _plc = plc;

        /// <summary>The typed destination tag.</summary>
        private readonly LogicalTagKey<T> _tag = tag;

        /// <summary>The incoming payload converter.</summary>
        private readonly Func<string, T> _payloadFactory = payloadFactory;

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Trace.TraceError("Omron MQTT subscription failed: {0}", error);
        }

        /// <inheritdoc/>
        public void OnNext(MqttApplicationMessageReceivedEventArgs value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var payload = value.ApplicationMessage.ConvertPayloadToString();
            var typedValue = _payloadFactory(payload);
            _plc.WriteValueAsync(_tag, typedValue, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
