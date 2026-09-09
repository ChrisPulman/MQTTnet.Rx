// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Creates reusable received-message snapshots from MQTTnet application messages.</summary>
public static class ReceivedMqttMessageExtensions
{
    /// <summary>Provides received-message snapshot helpers for MQTTnet application messages.</summary>
    /// <param name="message">The MQTTnet application message.</param>
    extension(MqttApplicationMessage message)
    {
        /// <summary>Builds a received-message snapshot from an MQTTnet application message.</summary>
        /// <param name="source">The source that observed the message.</param>
        /// <param name="timeProvider">The timestamp provider.</param>
        /// <returns>The received-message snapshot.</returns>
        public ReceivedMqttMessage ToReceivedMqttMessage(string source, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(timeProvider);

            var bytes = CopyPayload(message.Payload);
            var text = PayloadInspector.Decode(bytes);
            var detected = PayloadInspector.Detect(text, bytes);
            return new(
                timeProvider.GetLocalNow(),
                source,
                message.Topic ?? string.Empty,
                detected == PayloadFormat.Hex ? Convert.ToHexString(bytes) : text,
                detected,
                message.QualityOfServiceLevel,
                message.Retain,
                bytes.Length,
                bytes,
                message.ContentType,
                message.PayloadFormatIndicator,
                message.ResponseTopic,
                CopyCorrelationData(message.CorrelationData),
                message.MessageExpiryInterval,
                CopySubscriptionIdentifiers(message),
                message.TopicAlias,
                message.Dup,
                ReadUserProperties(message));
        }

        /// <summary>Builds a client-observed received-message snapshot using the system clock.</summary>
        /// <returns>The received-message snapshot.</returns>
        public ReceivedMqttMessage ToReceivedMqttMessage() =>
            message.ToReceivedMqttMessage("Client received", TimeProvider.System);
    }

    /// <summary>Provides received-message snapshot helpers for MQTTnet received-message event arguments.</summary>
    /// <param name="args">The MQTTnet received-message event arguments.</param>
    extension(MqttApplicationMessageReceivedEventArgs args)
    {
        /// <summary>Builds a received-message snapshot from MQTTnet received-message event arguments.</summary>
        /// <param name="source">The source that observed the message.</param>
        /// <param name="timeProvider">The timestamp provider.</param>
        /// <returns>The received-message snapshot.</returns>
        public ReceivedMqttMessage ToReceivedMqttMessage(string source, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(args);
            return args.ApplicationMessage.ToReceivedMqttMessage(source, timeProvider);
        }

        /// <summary>Builds a client-observed received-message snapshot using the system clock.</summary>
        /// <returns>The received-message snapshot.</returns>
        public ReceivedMqttMessage ToReceivedMqttMessage() =>
            args.ToReceivedMqttMessage("Client received", TimeProvider.System);
    }

    /// <summary>Copies MQTT payload sequence bytes into an array.</summary>
    /// <param name="payload">The MQTT payload sequence.</param>
    /// <returns>The copied payload bytes.</returns>
    private static byte[] CopyPayload(ReadOnlySequence<byte> payload) => payload.ToArray();

    /// <summary>Copies MQTT correlation data into an immutable snapshot array.</summary>
    /// <param name="correlationData">The correlation data from the source message.</param>
    /// <returns>The copied correlation data, or <see langword="null"/> when absent.</returns>
    private static byte[]? CopyCorrelationData(byte[]? correlationData)
    {
        if (correlationData is null)
        {
            return null;
        }

        var copy = new byte[correlationData.Length];
        Array.Copy(correlationData, copy, correlationData.Length);
        return copy;
    }

    /// <summary>Copies MQTT subscription identifiers into an immutable array.</summary>
    /// <param name="message">The message containing subscription identifiers.</param>
    /// <returns>The copied subscription identifiers.</returns>
    private static uint[] CopySubscriptionIdentifiers(MqttApplicationMessage message)
    {
        if (message.SubscriptionIdentifiers is null)
        {
            return [];
        }

        var identifiers = new uint[message.SubscriptionIdentifiers.Count];
        for (var index = 0; index < identifiers.Length; index++)
        {
            identifiers[index] = message.SubscriptionIdentifiers[index];
        }

        return identifiers;
    }

    /// <summary>Reads UTF-8 MQTT user properties into a display dictionary.</summary>
    /// <param name="message">The message containing user properties.</param>
    /// <returns>The copied user properties.</returns>
    private static List<MqttUserPropertyValue> ReadUserProperties(MqttApplicationMessage message)
    {
        if (message.UserProperties is null)
        {
            return [];
        }

        var values = new List<MqttUserPropertyValue>(message.UserProperties.Count);
        foreach (var property in message.UserProperties)
        {
            values.Add(new(property.Name, PayloadInspector.Decode(property.ValueBuffer.ToArray())));
        }

        return values;
    }
}
