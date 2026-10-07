// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Publishes complete messages without discarding MQTT protocol metadata.</summary>
public static class MqttApplicationMessagePublishingExtensions
{
    /// <summary>Provides complete-message publishing for MQTT client streams.</summary>
    /// <param name="client">The MQTT client stream.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes messages with their original payload and MQTT delivery properties.</summary>
        /// <param name="messages">The complete application messages.</param>
        /// <returns>The publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishMessage(IObservable<MqttApplicationMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messages);
            return client.PublishMany(messages);
        }
    }

    /// <summary>Provides complete-message publishing for resilient MQTT client streams.</summary>
    /// <param name="client">The resilient MQTT client stream.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Enqueues complete messages and observes their eventual processing results.</summary>
        /// <param name="messages">The complete application messages.</param>
        /// <returns>The processed-message events.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage(IObservable<MqttApplicationMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messages);
            return client
                .CombineLatest(messages, static (mqttClient, message) => (mqttClient, message))
                .Publish(static shared => shared
                    .Take(1)
                    .SelectMany(static publish => publish.mqttClient.ApplicationMessageProcessed)
                    .Merge(shared.SelectMany(static publish => publish.mqttClient.Enqueue(publish.message)
                        .SelectMany(static _ => Signal.Empty<ApplicationMessageProcessedEventArgs>()))));
        }
    }

    /// <summary>Provides complete-message publishing for asynchronous MQTT client streams.</summary>
    /// <param name="client">The asynchronous MQTT client stream.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes messages with their original payload and MQTT delivery properties.</summary>
        /// <param name="messages">The complete application messages.</param>
        /// <returns>The asynchronous publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishMessage(IObservableAsync<MqttApplicationMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messages);
            return client.PublishMany(messages);
        }
    }

    /// <summary>Provides complete-message publishing for asynchronous resilient MQTT client streams.</summary>
    /// <param name="client">The asynchronous resilient MQTT client stream.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Enqueues complete messages and observes their eventual processing results.</summary>
        /// <param name="messages">The complete application messages.</param>
        /// <returns>The asynchronous processed-message events.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage(IObservableAsync<MqttApplicationMessage> messages)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(messages);
            return client.ToObservable().PublishMessage(messages.ToObservable()).ToSignal();
        }
    }
}
