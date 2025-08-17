// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Reactive.Linq;
using System.Text;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Payload helpers to avoid unnecessary allocations when handling MQTT messages.
/// </summary>
public static class PayloadExtensions
{
    /// <summary>
    /// Gets the payload as ReadOnlySequence without copying.
    /// </summary>
    /// <param name="e">The received message event args.</param>
    /// <returns>A ReadOnlySequence of payload bytes.</returns>
    public static ReadOnlySequence<byte> Payload(this MqttApplicationMessageReceivedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return e.ApplicationMessage.Payload;
    }

    /// <summary>
    /// Decodes the payload as UTF8 text using the underlying buffers. Allocates only if multi-segment.
    /// </summary>
    /// <param name="e">The received message event args.</param>
    /// <returns>UTF8 decoded string.</returns>
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
    /// Projects the observable of received events to UTF8 string payloads.
    /// </summary>
    /// <param name="source">The source message stream.</param>
    /// <returns>Observable of UTF8 strings.</returns>
    public static IObservable<string> ToUtf8String(this IObservable<MqttApplicationMessageReceivedEventArgs> source) =>
        source.Select(e => e.PayloadUtf8());
}
