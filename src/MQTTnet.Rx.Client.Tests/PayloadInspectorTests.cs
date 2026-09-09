// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies MQTT payload detection and display behavior.</summary>
public sealed class PayloadInspectorTests
{
    /// <summary>Stores a finite numeric payload used by parse assertions.</summary>
    private const double FiniteNumberValue = 1.25;

    /// <summary>Verifies that invalid UTF-8 bytes are not parsed after hexadecimal fallback.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectReturnsHexForInvalidUtf8ThatLooksNumericAfterFallbackAsync()
    {
        var payload = new byte[] { 0x83 };
        var decoded = PayloadInspector.Decode(payload);
        var detected = PayloadInspector.Detect(decoded, payload);

        await Assert.That(decoded).IsEqualTo("83");
        await Assert.That(detected).IsEqualTo(PayloadFormat.Hex);
    }

    /// <summary>Verifies that strict UTF-8 control payloads stay binary for display.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectReturnsHexForStrictUtf8ControlPayloadAsync()
    {
        var payload = new byte[] { 0x00 };
        var decoded = PayloadInspector.Decode(payload);
        var detected = PayloadInspector.Detect(decoded, payload);

        await Assert.That(decoded).IsEqualTo("00");
        await Assert.That(detected).IsEqualTo(PayloadFormat.Hex);
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes(decoded, detected))).IsEqualTo("00");
    }

    /// <summary>Verifies that displayable UTF-8 numbers are still classified as numbers.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectReturnsNumberForDisplayableUtf8NumberAsync()
    {
        var payload = "83"u8.ToArray();
        var decoded = PayloadInspector.Decode(payload);
        var detected = PayloadInspector.Detect(decoded, payload);

        await Assert.That(detected).IsEqualTo(PayloadFormat.Number);
    }

    /// <summary>Verifies that an empty payload has explicit empty display behavior.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectReturnsEmptyForEmptyPayloadAsync()
    {
        var payload = Array.Empty<byte>();
        var decoded = PayloadInspector.Decode(payload);
        var detected = PayloadInspector.Detect(decoded, payload);

        await Assert.That(decoded).IsEmpty();
        await Assert.That(detected).IsEqualTo(PayloadFormat.Empty);
    }

    /// <summary>Verifies display classification for common strict UTF-8 payload shapes.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectClassifiesStrictUtf8JsonBooleanAndTextAsync()
    {
        await AssertPayloadFormatAsync("{\"value\":42}"u8.ToArray(), PayloadFormat.Json);
        await AssertPayloadFormatAsync("true"u8.ToArray(), PayloadFormat.Boolean);
        await AssertPayloadFormatAsync("plain text"u8.ToArray(), PayloadFormat.Utf8Text);
    }

    /// <summary>Verifies that array JSON is accepted and invalid object-shaped text falls back to text.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectHandlesJsonArraysAndInvalidJsonTextAsync()
    {
        await AssertPayloadFormatAsync("[1,2]"u8.ToArray(), PayloadFormat.Json);
        await AssertPayloadFormatAsync("{invalid"u8.ToArray(), PayloadFormat.Utf8Text);
    }

    /// <summary>Verifies formatted payload text can be encoded for publishing and metadata fields.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildBytesEncodesSupportedFormatsAsync()
    {
        await Assert.That(MqttPayloadEncoding.BuildBytes(string.Empty, PayloadFormat.Empty)).IsEmpty();
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("01 02", PayloadFormat.Hex))).IsEqualTo("0102");
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("AQI=", PayloadFormat.Base64))).IsEqualTo("0102");
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("TRUE", PayloadFormat.Boolean))).IsEqualTo("74727565");
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("42.5", PayloadFormat.Number))).IsEqualTo("34322E35");
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("{ \"value\" : 42 }", PayloadFormat.Json))).IsEqualTo("7B202276616C756522203A203432207D");
        await Assert.That(Convert.ToHexString(MqttPayloadEncoding.BuildBytes("text", PayloadFormat.Utf8Text))).IsEqualTo("74657874");
    }

    /// <summary>Verifies non-finite numeric payloads are rejected during encoding.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task BuildBytesRejectsNonFiniteNumberAsync() =>
        await Assert.That(static () => MqttPayloadEncoding.BuildBytes("Infinity", PayloadFormat.Number))
            .Throws<FormatException>();

    /// <summary>Verifies JSON payload validation requires strict UTF-8 and parseable JSON.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task IsValidJsonPayloadRejectsInvalidUtf8AndMalformedJsonAsync()
    {
        await Assert.That(PayloadInspector.IsValidJsonPayload("{\"value\":42}"u8.ToArray())).IsTrue();
        await Assert.That(PayloadInspector.IsValidJsonPayload([0x83])).IsFalse();
        await Assert.That(PayloadInspector.IsValidJsonPayload("{invalid"u8.ToArray())).IsFalse();
    }

    /// <summary>Verifies finite number parsing rejects non-finite values.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task TryParseFiniteNumberRejectsNonFiniteValuesAsync()
    {
        await Assert.That(PayloadInspector.TryParseFiniteNumber("1.25", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(FiniteNumberValue);
        await Assert.That(PayloadInspector.TryParseFiniteNumber("Infinity", out _)).IsFalse();
    }

    /// <summary>Verifies whitespace and primitive JSON payloads are displayed as text values.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task DetectTreatsWhitespaceAndPrimitiveJsonAsTextAsync()
    {
        await AssertPayloadFormatAsync("  "u8.ToArray(), PayloadFormat.Utf8Text);
        await AssertPayloadFormatAsync("42"u8.ToArray(), PayloadFormat.Number);
        await AssertPayloadFormatAsync("null"u8.ToArray(), PayloadFormat.Utf8Text);
    }

    /// <summary>Asserts the detected format for a payload fixture.</summary>
    /// <param name="payload">The raw payload bytes.</param>
    /// <param name="expected">The expected payload format.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertPayloadFormatAsync(byte[] payload, PayloadFormat expected)
    {
        var decoded = PayloadInspector.Decode(payload);
        var detected = PayloadInspector.Detect(decoded, payload);

        await Assert.That(detected).IsEqualTo(expected);
    }
}
