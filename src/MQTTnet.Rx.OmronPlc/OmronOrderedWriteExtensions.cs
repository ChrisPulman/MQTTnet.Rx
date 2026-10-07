// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.Primitives.Disposables;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
using ObserverFactory = System.Reactive.Observer;
namespace MQTTnet.Rx.OmronPlc.Reactive;
#else
using ReactiveUI.Primitives.Signals;
using ObserverFactory = ReactiveUI.Primitives.Advanced.Witness;
namespace MQTTnet.Rx.OmronPlc;
#endif

/// <summary>Serializes asynchronous typed Omron writes without blocking MQTT message callbacks.</summary>
public static class OmronOrderedWriteExtensions
{
    /// <summary>Provides ordered asynchronous Omron writes for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Queues MQTT payloads and awaits each native write before starting the next.</summary>
        /// <remarks>Disposal or cancellation removes the MQTT subscription, cancels the active write, and discards queued writes.
        /// Parser, source, and write failures end the bridge and invoke the required error callback.</remarks>
        /// <typeparam name="T">The registered tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tag">The registered destination tag.</param>
        /// <param name="plc">The native Omron driver.</param>
        /// <param name="payloadFactory">Converts each string payload to a tag value when its write begins.</param>
        /// <param name="onError">Receives source, parser, or native write failures.</param>
        /// <param name="cancellationToken">Cancels active and queued writes and the MQTT subscription.</param>
        /// <returns>The disposable lifetime of the ordered write bridge.</returns>
        public IDisposable SubscribeOmronPlcTagOrdered<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory,
            Action<Exception> onError,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);

            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);
            ArgumentNullException.ThrowIfNull(onError);

            return SubscribeCore(client.SubscribeToTopic(topic, false), tag, plc, payloadFactory, onError, cancellationToken);
        }
    }

    /// <summary>Provides ordered asynchronous Omron writes for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Queues MQTT payloads and awaits each native write before starting the next.</summary>
        /// <remarks>Disposal or cancellation removes the MQTT subscription, cancels the active write, and discards queued writes.
        /// Parser, source, and write failures end the bridge and invoke the required error callback.</remarks>
        /// <typeparam name="T">The registered tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tag">The registered destination tag.</param>
        /// <param name="plc">The native Omron driver.</param>
        /// <param name="payloadFactory">Converts each string payload to a tag value when its write begins.</param>
        /// <param name="onError">Receives source, parser, or native write failures.</param>
        /// <param name="cancellationToken">Cancels active and queued writes and the MQTT subscription.</param>
        /// <returns>The disposable lifetime of the ordered write bridge.</returns>
        public IDisposable SubscribeOmronPlcTagOrdered<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory,
            Action<Exception> onError,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);

            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);
            ArgumentNullException.ThrowIfNull(onError);

            return SubscribeCore(client.SubscribeToTopic(topic, false), tag, plc, payloadFactory, onError, cancellationToken);
        }
    }

    /// <summary>Provides ordered asynchronous Omron writes for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Queues MQTT payloads and awaits each native write before starting the next.</summary>
        /// <remarks>Disposal or cancellation removes the MQTT subscription, cancels the active write, and discards queued writes.
        /// Parser, source, and write failures end the bridge and invoke the required error callback.</remarks>
        /// <typeparam name="T">The registered tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tag">The registered destination tag.</param>
        /// <param name="plc">The native Omron driver.</param>
        /// <param name="payloadFactory">Converts each string payload to a tag value when its write begins.</param>
        /// <param name="onError">Receives source, parser, or native write failures.</param>
        /// <param name="cancellationToken">Cancels active and queued writes and the MQTT subscription.</param>
        /// <returns>The disposable lifetime of the ordered write bridge.</returns>
        public IDisposable SubscribeOmronPlcTagOrdered<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory,
            Action<Exception> onError,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.ToObservable()
                .SubscribeOmronPlcTagOrdered(topic, tag, plc, payloadFactory, onError, cancellationToken);
        }
    }

    /// <summary>Provides ordered asynchronous Omron writes for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Queues MQTT payloads and awaits each native write before starting the next.</summary>
        /// <remarks>Disposal or cancellation removes the MQTT subscription, cancels the active write, and discards queued writes.
        /// Parser, source, and write failures end the bridge and invoke the required error callback.</remarks>
        /// <typeparam name="T">The registered tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tag">The registered destination tag.</param>
        /// <param name="plc">The native Omron driver.</param>
        /// <param name="payloadFactory">Converts each string payload to a tag value when its write begins.</param>
        /// <param name="onError">Receives source, parser, or native write failures.</param>
        /// <param name="cancellationToken">Cancels active and queued writes and the MQTT subscription.</param>
        /// <returns>The disposable lifetime of the ordered write bridge.</returns>
        public IDisposable SubscribeOmronPlcTagOrdered<T>(
            string topic,
            LogicalTagKey<T> tag,
            IOmronPlcRx plc,
            Func<string, T> payloadFactory,
            Action<Exception> onError,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);

            return client.ToObservable()
                .SubscribeOmronPlcTagOrdered(topic, tag, plc, payloadFactory, onError, cancellationToken);
        }
    }

    /// <summary>Composes cold native writes into one sequential subscription.</summary>
    /// <typeparam name="T">The tag value type.</typeparam>
    /// <param name="messages">The received MQTT messages.</param>
    /// <param name="tag">The registered destination tag.</param>
    /// <param name="plc">The native driver.</param>
    /// <param name="payloadFactory">The payload parser.</param>
    /// <param name="onError">The failure callback.</param>
    /// <param name="cancellationToken">The external cancellation token.</param>
    /// <returns>The combined subscription and cancellation registration.</returns>
    private static IDisposable SubscribeCore<T>(
        IObservable<MqttApplicationMessageReceivedEventArgs> messages,
        LogicalTagKey<T> tag,
        IOmronPlcRx plc,
        Func<string, T> payloadFactory,
        Action<Exception> onError,
        CancellationToken cancellationToken)
    {
        var subscription = messages
            .Select(message =>
            {
                var payload = message.ApplicationMessage.ConvertPayloadToString();
                return Signal.FromAsync(
                    async token =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        token.ThrowIfCancellationRequested();
                        await plc.WriteValueAsync(tag, payloadFactory(payload), token).ConfigureAwait(false);
                        return true;
                    });
            })
            .Concat()
            .Subscribe(ObserverFactory.Create<bool>(static _ => { }, onError));
        var registration = cancellationToken.Register(subscription.Dispose);
        return MultipleDisposable.Create([subscription, registration]);
    }
}
