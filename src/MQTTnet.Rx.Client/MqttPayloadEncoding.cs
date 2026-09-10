// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Encodes formatted payload text into MQTT payload bytes.</summary>
public static class MqttPayloadEncoding
{
    /// <summary>Builds bytes from text using the selected payload format.</summary>
    /// <param name="value">The value text to encode.</param>
    /// <param name="format">The encoding format to use.</param>
    /// <returns>The encoded bytes.</returns>
    public static byte[] BuildBytes(string value, PayloadFormat format)
    {
        ArgumentNullException.ThrowIfNull(value);
        return format switch
        {
            PayloadFormat.Empty => [],
            PayloadFormat.Hex => Convert.FromHexString(value.Replace(" ", string.Empty, StringComparison.Ordinal)),
            PayloadFormat.Base64 => Convert.FromBase64String(value),
            PayloadFormat.Boolean => Encoding.UTF8.GetBytes(bool.Parse(value).ToString(CultureInfo.InvariantCulture).ToLowerInvariant()),
            PayloadFormat.Number => BuildNumberPayload(value),
            PayloadFormat.Json => BuildJsonPayload(value),
            _ => Encoding.UTF8.GetBytes(value),
        };
    }

    /// <summary>Builds a normalized UTF-8 JSON payload.</summary>
    /// <param name="payload">The JSON payload text.</param>
    /// <returns>The UTF-8 JSON payload bytes.</returns>
    private static byte[] BuildJsonPayload(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return Encoding.UTF8.GetBytes(document.RootElement.GetRawText());
    }

    /// <summary>Builds a finite numeric UTF-8 payload.</summary>
    /// <param name="payload">The numeric payload text.</param>
    /// <returns>The UTF-8 numeric payload bytes.</returns>
    private static byte[] BuildNumberPayload(string payload)
    {
        var value = double.Parse(payload, CultureInfo.InvariantCulture);
        if (!double.IsFinite(value))
        {
            throw new FormatException("Number payload must be finite.");
        }

        return Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));
    }
}
