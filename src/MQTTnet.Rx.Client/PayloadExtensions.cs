// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reactive.Linq;
using System.Text;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides extension methods for accessing and decoding the payload of MQTT application messages.
/// </summary>
/// <remarks>These methods enable efficient access to message payloads as byte sequences or UTF-8 strings, and
/// support integration with observable message streams. The extensions are designed to minimize allocations when
/// possible and to simplify common payload handling scenarios in MQTT client applications.</remarks>
public static class PayloadExtensions
{
    /// <summary>
    /// Gets the payload of the received MQTT application message as a read-only sequence of bytes.
    /// </summary>
    /// <param name="e">The event data containing the received MQTT application message. Cannot be null.</param>
    /// <returns>A read-only sequence of bytes representing the payload of the received MQTT application message. The sequence
    /// will be empty if the message has no payload.</returns>
    public static ReadOnlySequence<byte> Payload(this MqttApplicationMessageReceivedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return e.ApplicationMessage.Payload;
    }

    /// <summary>
    /// Decodes the payload of the received MQTT application message as a UTF-8 encoded string.
    /// </summary>
    /// <remarks>Use this method to conveniently access the textual content of an MQTT message when the
    /// payload is known to be UTF-8 encoded. If the payload is not valid UTF-8, decoding errors may occur.</remarks>
    /// <param name="e">The event arguments containing the received MQTT application message. Cannot be null.</param>
    /// <returns>A string containing the decoded UTF-8 payload of the message. Returns an empty string if the payload is empty.</returns>
    public static string PayloadUtf8(this MqttApplicationMessageReceivedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var seq = e.ApplicationMessage.Payload;
        if (seq.IsSingleSegment)
        {
            return Encoding.UTF8.GetString(seq.FirstSpan);
        }

        // Fallback: consolidate
        return Encoding.UTF8.GetString(seq.ToArray());
    }

    /// <summary>
    /// Projects each received MQTT application message to its UTF-8 encoded string payload.
    /// </summary>
    /// <remarks>If a message payload is not valid UTF-8, the resulting string may be incomplete or contain
    /// replacement characters. The sequence preserves the order of the original messages.</remarks>
    /// <param name="source">An observable sequence of <see cref="MqttApplicationMessageReceivedEventArgs"/> representing received MQTT
    /// application messages.</param>
    /// <returns>An observable sequence of strings containing the UTF-8 decoded payloads of the received messages.</returns>
    public static IObservable<string> ToUtf8String(this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.Select(e => e.PayloadUtf8());
}
