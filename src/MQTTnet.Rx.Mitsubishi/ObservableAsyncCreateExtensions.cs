// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Mitsubishi.Reactive;
#else
namespace MQTTnet.Rx.Mitsubishi;
#endif

/// <summary>Provides asynchronous-observable MQTT bridges for Mitsubishi logical tags.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides asynchronous-observable Mitsubishi logical-tag bridge operations.</summary>
    /// <param name="client">The asynchronous observable MQTT clients used by the bridge.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes each observed Mitsubishi logical-tag value to an MQTT topic.</summary>
        /// <typeparam name="T">The logical-tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the formatted values.</param>
        /// <param name="tag">The typed logical tag to observe.</param>
        /// <param name="logicalTags">The Mitsubishi logical-tag client that supplies values.</param>
        /// <param name="payloadFormatter">Converts each tag value to an MQTT string payload.</param>
        /// <returns>An asynchronous observable containing the result of each MQTT publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMitsubishiTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            MitsubishiLogicalTagClient logicalTags,
            Func<T, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            return ObservableSignalConversion.ToSignal(
                client
                    .ToObservable()
                    .PublishMitsubishiTag(topic, tag, logicalTags, payloadFormatter));
        }

        /// <summary>Subscribes to MQTT and writes parsed payloads to a Mitsubishi logical tag.</summary>
        /// <remarks>
        /// Writes are serialized in receive order. The returned disposable owns the converted MQTT subscription and
        /// cancels queued writes when disposed, including when the Mitsubishi client uses
        /// <see cref="MitsubishiSimulatorTransport"/>.
        /// </remarks>
        /// <typeparam name="T">The logical-tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tag">The typed logical tag to write.</param>
        /// <param name="logicalTags">The Mitsubishi logical-tag client that performs writes.</param>
        /// <param name="payloadParser">Converts each MQTT string payload to a logical-tag value.</param>
        /// <param name="onError">A callback for source, conversion, or logical-tag write failures, or
        /// <see langword="null"/>.</param>
        /// <param name="cancellationToken">A token that cancels queued and in-flight logical-tag writes.</param>
        /// <returns>A disposable subscription that deterministically tears down the bridge.</returns>
        public IDisposable SubscribeMitsubishiTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            MitsubishiLogicalTagClient logicalTags,
            Func<string, T> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client
                .ToObservable()
                .SubscribeMitsubishiTag(
                    topic,
                    tag,
                    logicalTags,
                    payloadParser,
                    onError,
                    cancellationToken);
        }
    }
}
