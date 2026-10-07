// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies publish validation and optional metadata are represented by the built MQTT message.</summary>
public sealed class PublishMessageViewModelTests
{
    /// <summary>Checks unspecified metadata is omitted and blank user-property names are ignored.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task BuildMessageOmitsUnsetMetadataAndInvalidUserPropertiesAsync()
    {
        var model = new PublishMessageViewModel
        {
            Topic = "plant/value",
            Payload = "hello",
            PayloadFormat = PayloadFormat.Utf8Text,
            ContentType = " ",
            UsePayloadFormatIndicator = false,
            ResponseTopic = " ",
            CorrelationData = " ",
        };
        model.UserProperties.Add(new() { Name = string.Empty, Value = "ignored" });
        model.UserProperties.Add(new() { Name = " ", Value = "ignored" });
        model.UserProperties.Add(new() { Name = "tag", Value = string.Empty });
        await Assert.That(model.Validate()).IsEqualTo(string.Empty);
        var message = model.BuildMessage();
        await Assert.That(message.ContentType).IsNull();
        await Assert.That(message.ResponseTopic).IsNull();
        await Assert.That(message.CorrelationData).IsNull();
        await Assert.That(message.MessageExpiryInterval).IsEqualTo(0U);
        await Assert.That(message.TopicAlias).IsEqualTo((ushort)0);
        await Assert.That(message.PayloadFormatIndicator).IsEqualTo(MqttPayloadFormatIndicator.Unspecified);
        await Assert.That(message.UserProperties).Count().IsEqualTo(1);
        await Assert.That(message.UserProperties[0].Name).IsEqualTo("tag");
        await Assert.That(System.Text.Encoding.UTF8.GetString(message.Payload.ToArray())).IsEqualTo("hello");
    }

    /// <summary>Checks correlation data is validated before publication and JSON metadata accepts valid JSON bytes.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task ValidateChecksCorrelationDataAndJsonContentTypeAsync()
    {
        var model = new PublishMessageViewModel
        {
            Topic = "plant/value",
            Payload = "{\"value\":42}",
            PayloadFormat = PayloadFormat.Utf8Text,
            ContentType = "application/problem+json",
            CorrelationData = "00FF",
            CorrelationDataFormat = PayloadFormat.Hex,
        };
        await Assert.That(model.Validate()).IsEqualTo(string.Empty);
        model.CorrelationData = "0";
        await Assert.That(model.Validate()).IsEqualTo("The input is not a valid hex string as its length is not a multiple of 2.");
        model.CorrelationData = "not base64";
        model.CorrelationDataFormat = PayloadFormat.Base64;
        await Assert.That(model.Validate()).IsNotEqualTo(string.Empty);
        model.CorrelationData = "NDI=";
        await Assert.That(model.Validate()).IsEqualTo(string.Empty);
        await Assert.That(Convert.ToHexString(model.BuildMessage().CorrelationData ?? [])).IsEqualTo("3432");
    }
}
