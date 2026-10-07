// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.SerialPort.Reactive;
#else
namespace MQTTnet.Rx.SerialPort;
#endif

/// <summary>Publishes serial values as complete MQTT application messages.</summary>
public static class SerialPortApplicationMessageExtensions
{
    /// <summary>Provides complete-message serial publishing.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes any serial data stream with all MQTT delivery and metadata properties.</summary>
        /// <typeparam name="T">The serial value type.</typeparam>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="sourceFactory">Selects lines, bytes, errors, state, or another serial stream.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each serial value.</param>
        /// <returns>The publication notifications.</returns>
        public IObservable<MqttClientPublishResult> PublishSerialPortMessages<T>(
            ISerialPortRx serialPort,
            Func<ISerialPortRx, IObservable<T>> sourceFactory,
            Func<T, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(sourceFactory(serialPort).Select(messageFactory));
        }

        /// <summary>Publishes complete framed serial messages with all MQTT properties.</summary>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="startsWith">The frame start markers.</param>
        /// <param name="endsWith">The frame end markers.</param>
        /// <param name="timeOut">The frame timeout in milliseconds.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each frame.</param>
        /// <returns>The publication notifications.</returns>
        public IObservable<MqttClientPublishResult> PublishSerialPort(
            ISerialPortRx serialPort,
            IObservable<char> startsWith,
            IObservable<char> endsWith,
            int timeOut,
            Func<string, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(Frames(serialPort, startsWith, endsWith, timeOut).Select(messageFactory));
        }
    }

    /// <summary>Provides complete-message serial publishing.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes any serial data stream with all MQTT delivery and metadata properties.</summary>
        /// <typeparam name="T">The serial value type.</typeparam>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="sourceFactory">Selects lines, bytes, errors, state, or another serial stream.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each serial value.</param>
        /// <returns>The publication notifications.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPortMessages<T>(
            ISerialPortRx serialPort,
            Func<ISerialPortRx, IObservable<T>> sourceFactory,
            Func<T, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(sourceFactory(serialPort).Select(messageFactory));
        }

        /// <summary>Publishes complete framed serial messages with all MQTT properties.</summary>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="startsWith">The frame start markers.</param>
        /// <param name="endsWith">The frame end markers.</param>
        /// <param name="timeOut">The frame timeout in milliseconds.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each frame.</param>
        /// <returns>The publication notifications.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort(
            ISerialPortRx serialPort,
            IObservable<char> startsWith,
            IObservable<char> endsWith,
            int timeOut,
            Func<string, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.PublishMessage(Frames(serialPort, startsWith, endsWith, timeOut).Select(messageFactory));
        }
    }

    /// <summary>Provides complete-message serial publishing.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes any serial data stream with all MQTT delivery and metadata properties.</summary>
        /// <typeparam name="T">The serial value type.</typeparam>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="sourceFactory">Selects lines, bytes, errors, state, or another serial stream.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each serial value.</param>
        /// <returns>The publication notifications.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishSerialPortMessages<T>(
            ISerialPortRx serialPort,
            Func<ISerialPortRx, IObservable<T>> sourceFactory,
            Func<T, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishSerialPortMessages(serialPort, sourceFactory, messageFactory).ToMqttAsyncSignal();
        }

        /// <summary>Publishes complete framed serial messages with all MQTT properties.</summary>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="startsWith">The frame start markers.</param>
        /// <param name="endsWith">The frame end markers.</param>
        /// <param name="timeOut">The frame timeout in milliseconds.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each frame.</param>
        /// <returns>The publication notifications.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishSerialPort(
            ISerialPortRx serialPort,
            IObservableAsync<char> startsWith,
            IObservableAsync<char> endsWith,
            int timeOut,
            Func<string, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishSerialPort(serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut, messageFactory).ToMqttAsyncSignal();
        }
    }

    /// <summary>Provides complete-message serial publishing.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes any serial data stream with all MQTT delivery and metadata properties.</summary>
        /// <typeparam name="T">The serial value type.</typeparam>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="sourceFactory">Selects lines, bytes, errors, state, or another serial stream.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each serial value.</param>
        /// <returns>The publication notifications.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPortMessages<T>(
            ISerialPortRx serialPort,
            Func<ISerialPortRx, IObservable<T>> sourceFactory,
            Func<T, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(sourceFactory);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishSerialPortMessages(serialPort, sourceFactory, messageFactory).ToMqttAsyncSignal();
        }

        /// <summary>Publishes complete framed serial messages with all MQTT properties.</summary>
        /// <param name="serialPort">The serial transport.</param>
        /// <param name="startsWith">The frame start markers.</param>
        /// <param name="endsWith">The frame end markers.</param>
        /// <param name="timeOut">The frame timeout in milliseconds.</param>
        /// <param name="messageFactory">Creates the complete MQTT message for each frame.</param>
        /// <returns>The publication notifications.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort(
            ISerialPortRx serialPort,
            IObservableAsync<char> startsWith,
            IObservableAsync<char> endsWith,
            int timeOut,
            Func<string, MqttApplicationMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serialPort);
            ArgumentNullException.ThrowIfNull(startsWith);
            ArgumentNullException.ThrowIfNull(endsWith);
            ArgumentNullException.ThrowIfNull(messageFactory);
            return client.ToObservable().PublishSerialPort(serialPort, startsWith.ToObservable(), endsWith.ToObservable(), timeOut, messageFactory).ToMqttAsyncSignal();
        }
    }

    /// <summary>Routes source errors directly to frame observers.</summary>
    /// <param name="serialPort">The serial transport.</param>
    /// <param name="startsWith">The frame start markers.</param>
    /// <param name="endsWith">The frame end markers.</param>
    /// <param name="timeOut">The frame timeout in milliseconds.</param>
    /// <returns>The framed serial values.</returns>
    internal static IObservable<string> Frames(
        ISerialPortRx serialPort,
        IObservable<char> startsWith,
        IObservable<char> endsWith,
        int timeOut) =>
        Signal.Create<string>(observer => SerialPortRxMixins.BufferUntil(
            ForwardFrameErrors(serialPort.DataReceived, observer.OnError),
            ForwardFrameErrors(startsWith, observer.OnError),
            ForwardFrameErrors(endsWith, observer.OnError),
            timeOut).Subscribe(observer));

    /// <summary>Protects the driver's framing subscriptions while preserving errors for callers.</summary>
    /// <param name="source">The source used by the framing operator.</param>
    /// <param name="onError">Reports errors to the publication observer.</param>
    /// <returns>The framing input.</returns>
    private static IObservable<char> ForwardFrameErrors(IObservable<char> source, Action<Exception> onError) =>
        Signal.Create<char>(observer => source.Subscribe(observer.OnNext, onError, observer.OnCompleted));
}
