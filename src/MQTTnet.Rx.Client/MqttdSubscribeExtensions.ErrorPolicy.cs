// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides explicit error policies for shared MQTT topic subscriptions.</summary>
public static partial class MqttdSubscribeExtensions
{
    /// <summary>Provides topic error policies for raw client streams.</summary>
    /// <param name="client">The MQTT client stream.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Subscribes to a topic, optionally retrying source or subscription failures.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <param name="retryOnError">Whether to retry failures instead of forwarding them.</param>
        /// <returns>The shared received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(string topic, bool retryOnError)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            return ShareTopicSubscription(client.SelectMany(new RawTopicSubscription(topic).Create), retryOnError);
        }
    }

    /// <summary>Provides topic error policies for resilient client streams.</summary>
    /// <param name="client">The resilient MQTT client stream.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Subscribes to a topic, optionally retrying source or subscription failures.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <param name="retryOnError">Whether to retry failures instead of forwarding them.</param>
        /// <returns>The shared received-message sequence.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(string topic, bool retryOnError)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            return ShareTopicSubscription(client.SelectMany(new ResilientTopicSubscription(topic).Create), retryOnError);
        }
    }

    /// <summary>Shares a subscription with the requested failure policy.</summary>
    /// <param name="source">The topic subscription.</param>
    /// <param name="retryOnError">Whether to retry failures.</param>
    /// <returns>The shared subscription.</returns>
    private static IObservable<MqttApplicationMessageReceivedEventArgs> ShareTopicSubscription(
        IObservable<MqttApplicationMessageReceivedEventArgs> source,
        bool retryOnError) => (retryOnError ? source.Retry() : source).Publish().RefCount();
}
