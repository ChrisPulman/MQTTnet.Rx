// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides asynchronous counterparts for explicit topic-subscription error policies.</summary>
public static partial class ObservableAsyncBridgeExtensions
{
    /// <summary>Provides topic error policies for asynchronous raw client streams.</summary>
    /// <param name="client">The asynchronous MQTT client stream.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Subscribes asynchronously with an explicit retry policy.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <param name="retryOnError">Whether to retry failures instead of forwarding them.</param>
        /// <returns>The shared asynchronous received-message sequence.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(string topic, bool retryOnError)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeToTopic(topic, retryOnError).ToSignal();
        }
    }

    /// <summary>Provides topic error policies for asynchronous resilient client streams.</summary>
    /// <param name="client">The asynchronous resilient MQTT client stream.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Subscribes asynchronously with an explicit retry policy.</summary>
        /// <param name="topic">The MQTT topic filter.</param>
        /// <param name="retryOnError">Whether to retry failures instead of forwarding them.</param>
        /// <returns>The shared asynchronous received-message sequence.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic(string topic, bool retryOnError)
        {
            ArgumentNullException.ThrowIfNull(client);
            return client.ToObservable().SubscribeToTopic(topic, retryOnError).ToSignal();
        }
    }
}
