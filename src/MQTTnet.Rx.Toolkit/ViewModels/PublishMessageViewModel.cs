// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Captures editable MQTT application message publish options.</summary>
internal sealed partial class PublishMessageViewModel : ViewModelBase
{
    /// <summary>Stores the MQTT topic used when publishing.</summary>
    [Reactive]
    private string _topic = "demo/device/value";

    /// <summary>Stores the payload format selected by the message builder.</summary>
    [Reactive]
    private PayloadFormat _payloadFormat = PayloadFormat.Json;

    /// <summary>Stores the editable payload text.</summary>
    [Reactive]
    private string _payload = "{\"value\": 42, \"unit\": \"c\"}";

    /// <summary>Stores the quality of service used when publishing.</summary>
    [Reactive]
    private MqttQualityOfServiceLevel _qualityOfService = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Stores whether published messages should set the MQTT retain flag.</summary>
    [Reactive]
    private bool _retain = true;

    /// <summary>Stores MQTT 5 content type metadata.</summary>
    [Reactive]
    private string _contentType = "application/json";

    /// <summary>Stores MQTT 5 response topic metadata.</summary>
    [Reactive]
    private string _responseTopic = string.Empty;

    /// <summary>Stores MQTT 5 correlation data text metadata.</summary>
    [Reactive]
    private string _correlationData = string.Empty;

    /// <summary>Stores the encoding used for MQTT 5 correlation data.</summary>
    [Reactive]
    private PayloadFormat _correlationDataFormat;

    /// <summary>Stores MQTT 5 message expiry seconds.</summary>
    [Reactive]
    private uint _messageExpirySeconds;

    /// <summary>Stores MQTT 5 topic alias metadata.</summary>
    [Reactive]
    private ushort _topicAlias;

    /// <summary>Stores whether MQTT payload format indicator metadata should be sent.</summary>
    [Reactive]
    private bool _usePayloadFormatIndicator = true;

    /// <summary>Stores the explicit MQTT payload format indicator value.</summary>
    [Reactive]
    private MqttPayloadFormatIndicator _payloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData;

    /// <summary>Gets editable MQTT user properties for the published message.</summary>
    public ObservableCollection<UserPropertyViewModel> UserProperties { get; } = [];

    /// <summary>Gets the payload formats available in the UI.</summary>
    public IReadOnlyList<PayloadFormat> PayloadFormats { get; } = Enum.GetValues<PayloadFormat>();

    /// <summary>Gets the quality of service values available in the UI.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

    /// <summary>Gets the payload format indicators available in the UI.</summary>
    public IReadOnlyList<MqttPayloadFormatIndicator> PayloadFormatIndicators { get; } =
        Enum.GetValues<MqttPayloadFormatIndicator>();

    /// <summary>Builds the MQTT application message represented by this view model.</summary>
    /// <returns>The MQTT application message to publish.</returns>
    internal MqttApplicationMessage BuildMessage()
    {
        var builder = new MqttApplicationMessageBuilder()
            .WithTopic(Topic)
            .WithQualityOfServiceLevel(QualityOfService)
            .WithRetainFlag(Retain)
            .WithPayload(BuildPayload());

        if (!string.IsNullOrWhiteSpace(ContentType))
        {
            _ = builder.WithContentType(ContentType);
        }

        if (UsePayloadFormatIndicator)
        {
            _ = builder.WithPayloadFormatIndicator(PayloadFormatIndicator);
        }

        if (!string.IsNullOrWhiteSpace(ResponseTopic))
        {
            _ = builder.WithResponseTopic(ResponseTopic);
        }

        if (!string.IsNullOrWhiteSpace(CorrelationData))
        {
            _ = builder.WithCorrelationData(MqttPayloadEncoding.BuildBytes(CorrelationData, CorrelationDataFormat));
        }

        if (MessageExpirySeconds != 0)
        {
            _ = builder.WithMessageExpiryInterval(MessageExpirySeconds);
        }

        if (TopicAlias != 0)
        {
            _ = builder.WithTopicAlias(TopicAlias);
        }

        foreach (var property in UserProperties)
        {
            if (property.IsValid)
            {
                _ = builder.WithUserProperty(property.Name, Encoding.UTF8.GetBytes(property.Value).AsMemory());
            }
        }

        return builder.Build();
    }

    /// <summary>Validates topic and payload values before publishing.</summary>
    /// <returns>An empty string when valid; otherwise, a displayable validation error.</returns>
    internal string Validate()
    {
        if (Topic.Length == 0)
        {
            return "Publish topic is required.";
        }

        if (Topic.Contains('#', StringComparison.Ordinal) || Topic.Contains('+', StringComparison.Ordinal))
        {
            return "Publish topics cannot contain MQTT wildcards.";
        }

        try
        {
            var payload = BuildPayload();
            ValidatePayloadMetadata(payload);
            if (!string.IsNullOrWhiteSpace(CorrelationData))
            {
                _ = MqttPayloadEncoding.BuildBytes(CorrelationData, CorrelationDataFormat);
            }

            return string.Empty;
        }
        catch (FormatException exception)
        {
            return exception.Message;
        }
        catch (JsonException exception)
        {
            return $"JSON payload is invalid: {exception.Message}";
        }
    }

    /// <summary>Validates payload bytes against explicit MQTT metadata.</summary>
    /// <param name="payload">The payload bytes that will be sent.</param>
    private void ValidatePayloadMetadata(byte[] payload)
    {
        if (UsePayloadFormatIndicator && PayloadFormatIndicator == MqttPayloadFormatIndicator.CharacterData &&
            !PayloadInspector.IsStrictUtf8(payload))
        {
            throw new FormatException("Payload format indicator CharacterData requires valid UTF-8 payload bytes.");
        }

        if (DeclaresJsonContentType() && !PayloadInspector.IsValidJsonPayload(payload))
        {
            throw new FormatException("Content type application/json requires JSON payload bytes.");
        }
    }

    /// <summary>Determines whether the content type declares JSON payload bytes.</summary>
    /// <returns><see langword="true"/> when content type declares JSON.</returns>
    private bool DeclaresJsonContentType() =>
        ContentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Builds the raw MQTT payload bytes.</summary>
    /// <returns>The payload bytes represented by the selected format.</returns>
    private byte[] BuildPayload() => MqttPayloadEncoding.BuildBytes(Payload, PayloadFormat);
}
