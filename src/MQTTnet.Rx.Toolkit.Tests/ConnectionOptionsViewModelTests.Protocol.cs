// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Channel;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;
using NSubstitute;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies MQTT protocol option and composition behavior.</summary>
public sealed partial class ConnectionOptionsViewModelTests
{
    /// <summary>Stores the receive limit used by option translation tests.</summary>
    private const ushort ConfiguredReceiveMaximum = 12;

    /// <summary>Stores the topic alias limit used by option translation tests.</summary>
    private const ushort ConfiguredTopicAliasMaximum = 7;

    /// <summary>Stores the packet size limit used by option translation tests.</summary>
    private const uint ConfiguredPacketSize = 2048;

    /// <summary>Stores the initial packet writer buffer size.</summary>
    private const int ConfiguredWriterBufferSize = 1024;

    /// <summary>Stores the maximum packet writer buffer size.</summary>
    private const int ConfiguredWriterBufferSizeMax = 2048;

    /// <summary>Stores an option value that should be ignored.</summary>
    private const string IgnoredValue = "ignored";

    /// <summary>Stores the binary payload expected from hex and Base64 inputs.</summary>
    private const string ConfiguredBinaryDataHex = "00FF";

    /// <summary>Verifies configured protocol limits, timer clamps and option composition survive construction.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsLimitsAndClampsTimersAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            KeepAliveSeconds = -1,
            TimeoutSeconds = -1,
            ReceiveMaximum = ConfiguredReceiveMaximum,
            TopicAliasMaximum = ConfiguredTopicAliasMaximum,
            MaximumPacketSize = ConfiguredPacketSize,
            DisablePacketFragmentation = true,
            ValidateFeatures = false,
            WriterBufferSize = ConfiguredWriterBufferSize,
            WriterBufferSizeMax = ConfiguredWriterBufferSizeMax,
            RequestProblemInformation = false,
            RequestResponseInformation = true,
            ClientOptionsConfigurator = static builder => builder.WithClientId("composed-client"),
        };
        var options = connection.BuildClientOptions();
        await Assert.That(options.ClientId).IsEqualTo("composed-client");
        await Assert.That(options.KeepAlivePeriod).IsEqualTo(TimeSpan.Zero);
        await Assert.That(options.Timeout).IsEqualTo(TimeSpan.FromSeconds(1));
        await Assert.That(options.ReceiveMaximum).IsEqualTo(ConfiguredReceiveMaximum);
        await Assert.That(options.TopicAliasMaximum).IsEqualTo(ConfiguredTopicAliasMaximum);
        await Assert.That(options.MaximumPacketSize).IsEqualTo(ConfiguredPacketSize);
        await Assert.That(options.AllowPacketFragmentation).IsFalse();
        await Assert.That(options.ValidateFeatures).IsFalse();
        await Assert.That(options.WriterBufferSize).IsEqualTo(ConfiguredWriterBufferSize);
        await Assert.That(options.WriterBufferSizeMax).IsEqualTo(ConfiguredWriterBufferSizeMax);
        await Assert.That(options.RequestProblemInformation).IsFalse();
        await Assert.That(options.RequestResponseInformation).IsTrue();
        await Assert.That(options.TryPrivate).IsFalse();
    }

    /// <summary>Verifies private bridge identification is retained for supported MQTT protocol versions.</summary>
    /// <param name="protocolVersion">The MQTT protocol version.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(MqttProtocolVersion.V310)]
    [Arguments(MqttProtocolVersion.V311)]
    public async Task BuildClientOptions_MapsTryPrivateForMqtt3Async(MqttProtocolVersion protocolVersion)
    {
        using var connection = new ConnectionOptionsViewModel
        {
            ProtocolVersion = protocolVersion,
            TryPrivate = true,
            ReceiveMaximum = 0,
            MaximumPacketSize = 0,
        };
        var options = connection.BuildClientOptions();
        await Assert.That(options.ProtocolVersion).IsEqualTo(protocolVersion);
        await Assert.That(options.TryPrivate).IsTrue();
    }

    /// <summary>Verifies native MQTT validation rejects private bridge identification with MQTT 5.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_RejectsTryPrivateForMqtt5Async()
    {
        using var connection = new ConnectionOptionsViewModel { TryPrivate = true };
        var exception = await Assert.That(connection.BuildClientOptions).Throws<NotSupportedException>()
            ?? throw new InvalidOperationException("Expected protocol validation failure.");
        await Assert.That(exception.Message).Contains("TryPrivate");
    }

    /// <summary>Verifies an injected stream provider is assigned to an already configured TCP transport.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsStreamProviderToTcpTransportAsync()
    {
        var provider = Substitute.For<IMqttClientStreamProvider>();
        using var connection = new ConnectionOptionsViewModel { StreamProvider = provider };
        var tcp = (MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!;
        await Assert.That(tcp.StreamProvider).IsSameReferenceAs(provider);
    }

    /// <summary>Verifies binary will payloads and metadata are preserved while blank user property rows are omitted.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsBinaryWillMetadataAndFiltersBlankPropertiesAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            WillTopic = "state/offline",
            WillPayload = "00 FF",
            WillPayloadFormat = PayloadFormat.Hex,
            WillPayloadFormatIndicator = MqttPayloadFormatIndicator.Unspecified,
            WillCorrelationData = "AP8=",
            WillCorrelationDataFormat = PayloadFormat.Base64,
            WillResponseTopic = "state/reply",
            WillContentType = "application/octet-stream",
        };
        connection.UserProperties.Add(new() { Name = " ", Value = IgnoredValue });
        connection.UserProperties.Add(new() { Name = "name", Value = "välue" });
        connection.WillUserProperties.Add(new() { Name = string.Empty, Value = IgnoredValue });
        connection.WillUserProperties.Add(new() { Name = "will-name", Value = "will-value" });
        var options = connection.BuildClientOptions();
        await Assert.That(Convert.ToHexString(options.WillPayload)).IsEqualTo(ConfiguredBinaryDataHex);
        await Assert.That(Convert.ToHexString(options.WillCorrelationData)).IsEqualTo(ConfiguredBinaryDataHex);
        await Assert.That(options.WillResponseTopic).IsEqualTo("state/reply");
        await Assert.That(options.WillContentType).IsEqualTo("application/octet-stream");
        await Assert.That(options.WillPayloadFormatIndicator).IsEqualTo(MqttPayloadFormatIndicator.Unspecified);
        await Assert.That(options.UserProperties.Count).IsEqualTo(1);
        await Assert.That(options.UserProperties[0].Name).IsEqualTo("name");
        await Assert.That(System.Text.Encoding.UTF8.GetString(options.UserProperties[0].ValueBuffer.Span)).IsEqualTo("välue");
        await Assert.That(options.WillUserProperties.Count).IsEqualTo(1);
        await Assert.That(options.WillUserProperties[0].Name).IsEqualTo("will-name");
    }

    /// <summary>Verifies optional will metadata can be omitted without creating empty fields.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_BlankWillMetadataIsOmittedAsync()
    {
        using var connection = new ConnectionOptionsViewModel { WillTopic = "state/offline", WillContentType = " ", WillResponseTopic = " ", WillCorrelationData = " " };
        var options = connection.BuildClientOptions();
        await Assert.That(options.WillContentType).IsNull();
        await Assert.That(options.WillResponseTopic).IsNull();
        await Assert.That(options.WillCorrelationData).IsNull();
    }

    /// <summary>Verifies simple enhanced authentication preserves binary initial data.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsInitialEnhancedAuthenticationAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            EnhancedAuthenticationMethod = "challenge-method",
            EnhancedAuthenticationData = "AP8=",
            EnhancedAuthenticationDataFormat = PayloadFormat.Base64,
        };
        var options = connection.BuildClientOptions();
        await Assert.That(options.AuthenticationMethod).IsEqualTo("challenge-method");
        await Assert.That(Convert.ToHexString(options.AuthenticationData)).IsEqualTo(ConfiguredBinaryDataHex);
        await Assert.That(options.EnhancedAuthenticationHandler).IsNull();
    }

    /// <summary>Verifies an injected handler takes precedence over scripted responses and initial authentication.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_InjectedAuthenticationHandlerTakesPrecedenceAsync()
    {
        var handler = Substitute.For<IMqttEnhancedAuthenticationHandler>();
        using var connection = new ConnectionOptionsViewModel { EnhancedAuthenticationHandler = handler, EnhancedAuthenticationMethod = IgnoredValue };
        connection.EnhancedAuthenticationSteps.Add(new() { Data = IgnoredValue });
        var options = connection.BuildClientOptions();
        await Assert.That(options.EnhancedAuthenticationHandler).IsSameReferenceAs(handler);
        await Assert.That(options.AuthenticationMethod).IsNull();
    }

    /// <summary>Verifies scripted responses create the authentication handler used by MQTTnet.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_ScriptedAuthenticationCreatesHandlerAsync()
    {
        using var connection = new ConnectionOptionsViewModel { EnhancedAuthenticationMethod = IgnoredValue };
        connection.EnhancedAuthenticationSteps.Add(new() { Data = "response" });
        var options = connection.BuildClientOptions();
        await Assert.That(options.EnhancedAuthenticationHandler).IsTypeOf<ScriptedEnhancedAuthenticationHandler>();
        await Assert.That(options.AuthenticationMethod).IsNull();
    }
}
