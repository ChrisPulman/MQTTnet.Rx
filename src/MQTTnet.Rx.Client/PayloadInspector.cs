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

/// <summary>Detects and decodes MQTT payload data for display and validation.</summary>
public static class PayloadInspector
{
    /// <summary>Detects the most suitable display format for a decoded payload.</summary>
    /// <param name="text">The decoded payload text.</param>
    /// <param name="payload">The raw payload bytes.</param>
    /// <returns>The detected payload display format.</returns>
    public static PayloadFormat Detect(string text, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length == 0)
        {
            return PayloadFormat.Empty;
        }

        if (!IsStrictUtf8(payload) || !IsMostlyText(Encoding.UTF8.GetString(payload)))
        {
            return PayloadFormat.Hex;
        }

        if (IsJson(text))
        {
            return PayloadFormat.Json;
        }

        if (bool.TryParse(text, out _))
        {
            return PayloadFormat.Boolean;
        }

        return TryParseFiniteNumber(text, out _)
            ? PayloadFormat.Number
            : PayloadFormat.Utf8Text;
    }

    /// <summary>Decodes displayable UTF-8 text or hexadecimal for binary payloads.</summary>
    /// <param name="payload">The payload bytes to decode.</param>
    /// <returns>The decoded display text.</returns>
    public static string Decode(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.Length == 0)
        {
            return string.Empty;
        }

        if (!IsStrictUtf8(payload))
        {
            return Convert.ToHexString(payload);
        }

        var text = Encoding.UTF8.GetString(payload);
        return IsMostlyText(text) ? text : Convert.ToHexString(payload);
    }

    /// <summary>Determines whether payload bytes contain strict UTF-8 JSON.</summary>
    /// <param name="payload">The payload bytes to validate.</param>
    /// <returns><see langword="true"/> when the payload contains JSON.</returns>
    public static bool IsValidJsonPayload(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!IsStrictUtf8(payload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.ValueKind is not JsonValueKind.Undefined;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Determines whether the bytes form strict UTF-8 data.</summary>
    /// <param name="payload">The payload bytes to inspect.</param>
    /// <returns><see langword="true"/> when the bytes are strict UTF-8.</returns>
    public static bool IsStrictUtf8(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        try
        {
            _ = new UTF8Encoding(false, true).GetString(payload);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>Attempts to parse a finite invariant-culture number.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed finite number.</param>
    /// <returns><see langword="true"/> when the text is a finite number.</returns>
    public static bool TryParseFiniteNumber(string text, out double value)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (double.TryParse(text, CultureInfo.InvariantCulture, out value) && double.IsFinite(value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Determines whether text is a JSON object or array.</summary>
    /// <param name="text">The text to inspect.</param>
    /// <returns><see langword="true"/> when the text is valid object or array JSON.</returns>
    private static bool IsJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.TrimStart();
        if (trimmed[0] is not ('{' or '['))
        {
            return false;
        }

        try
        {
            using var _ = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Determines whether decoded text is suitable for display as text.</summary>
    /// <param name="text">The decoded text to inspect.</param>
    /// <returns><see langword="true"/> when no non-display control characters are present.</returns>
    private static bool IsMostlyText(string text)
    {
        var controlCharacters = 0;
        foreach (var character in text)
        {
            if (char.IsControl(character) && character is not '\r' and not '\n' and not '\t')
            {
                controlCharacters++;
            }
        }

        return controlCharacters == 0;
    }
}
