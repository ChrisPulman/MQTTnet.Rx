// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Mitsubishi.Reactive;
#else
namespace MQTTnet.Rx.Mitsubishi;
#endif

/// <summary>Provides an MQTT bridge for typed logical tags exposed by a Mitsubishi MELSEC client.</summary>
public static class MitsubishiMqttExtensions
{
    /// <summary>Provides synchronous-observable Mitsubishi logical-tag bridge operations.</summary>
    /// <param name="client">The connected MQTT clients used by the bridge.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes each observed Mitsubishi logical-tag value to an MQTT topic.</summary>
        /// <remarks>
        /// The logical-tag client may be backed by a live transport or by
        /// <see cref="MitsubishiSimulatorTransport"/>. Subscribe to the returned observable to start the bridge and
        /// dispose
        /// that subscription to stop it.
        /// </remarks>
        /// <typeparam name="T">The logical-tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the formatted values.</param>
        /// <param name="tag">The typed logical tag to observe.</param>
        /// <param name="logicalTags">The Mitsubishi logical-tag client that supplies values.</param>
        /// <param name="payloadFormatter">Converts each tag value to an MQTT string payload.</param>
        /// <returns>An observable sequence containing the result of each MQTT publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishMitsubishiTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            MitsubishiLogicalTagClient logicalTags,
            Func<T, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                logicalTags.Observe(tag).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Subscribes to an MQTT topic and writes each parsed payload to a Mitsubishi logical tag.</summary>
        /// <remarks>
        /// Writes are serialized in receive order. Disposing the returned value removes the MQTT subscription, cancels
        /// queued writes, and prevents later messages from reaching either a live PLC or a simulator-backed client.
        /// Expected logical-tag failures are reported through <paramref name="onError"/> as
        /// <see cref="InvalidOperationException"/> instances.
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
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ArgumentNullException.ThrowIfNull(payloadParser);

            var observer = new MitsubishiTagWriteObserver<T>(
                tag,
                logicalTags,
                payloadParser,
                onError,
                cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides synchronous-observable Mitsubishi logical-tag bridge operations.</summary>
    /// <param name="client">The resilient MQTT clients used by the bridge.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes each observed Mitsubishi logical-tag value through a resilient MQTT client.</summary>
        /// <typeparam name="T">The logical-tag value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the formatted values.</param>
        /// <param name="tag">The typed logical tag to observe.</param>
        /// <param name="logicalTags">The Mitsubishi logical-tag client that supplies values.</param>
        /// <param name="payloadFormatter">Converts each tag value to an MQTT string payload.</param>
        /// <returns>An observable sequence containing each resilient MQTT publish result.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMitsubishiTag<T>(
            string topic,
            LogicalTagKey<T> tag,
            MitsubishiLogicalTagClient logicalTags,
            Func<T, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                logicalTags.Observe(tag).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Subscribes through a resilient MQTT client and writes payloads to a Mitsubishi logical tag.</summary>
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
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ArgumentNullException.ThrowIfNull(payloadParser);

            var observer = new MitsubishiTagWriteObserver<T>(
                tag,
                logicalTags,
                payloadParser,
                onError,
                cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }
}
