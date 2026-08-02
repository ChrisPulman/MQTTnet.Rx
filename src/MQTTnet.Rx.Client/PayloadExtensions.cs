// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides MQTT application message payload helpers.</summary>
public static class PayloadExtensions
{
    /// <summary>Provides payload helpers for MQTT message observables.</summary>
    /// <param name="source">The received MQTT message stream.</param>
    extension(IObservable<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Projects received messages to UTF-8 payload strings.</summary>
        /// <returns>An observable sequence of UTF-8 payload strings.</returns>
        public IObservable<string> ToUtf8String() => source.Select(static e => e.PayloadUtf8());
    }

    /// <summary>Provides payload helpers for received MQTT application messages.</summary>
    /// <param name="e">The received MQTT application message.</param>
    extension(MqttApplicationMessageReceivedEventArgs e)
    {
        /// <summary>Gets the message payload.</summary>
        /// <returns>The message payload.</returns>
        public ReadOnlySequence<byte> Payload()
        {
            ArgumentNullException.ThrowIfNull(e);
            return e.ApplicationMessage.Payload;
        }

        /// <summary>Decodes the message payload as UTF-8.</summary>
        /// <returns>The UTF-8 decoded message payload.</returns>
        public string PayloadUtf8()
        {
            ArgumentNullException.ThrowIfNull(e);
            var sequence = e.ApplicationMessage.Payload;
            return sequence.IsSingleSegment
                ? Encoding.UTF8.GetString(sequence.FirstSpan)
                : Encoding.UTF8.GetString(sequence.ToArray());
        }
    }
}
