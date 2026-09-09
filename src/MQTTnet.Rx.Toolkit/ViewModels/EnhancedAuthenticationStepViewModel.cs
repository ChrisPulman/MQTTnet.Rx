// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Represents one scripted enhanced-authentication exchange step.</summary>
internal sealed partial class EnhancedAuthenticationStepViewModel : ViewModelBase
{
    /// <summary>Stores authentication data text for the step.</summary>
    [Reactive]
    private string _data = string.Empty;

    /// <summary>Stores the authentication data encoding format.</summary>
    [Reactive]
    private PayloadFormat _dataFormat;

    /// <summary>Stores the MQTT reason text sent with the step.</summary>
    [Reactive]
    private string _reason = string.Empty;

    /// <summary>Stores the expected challenge reason code associated with this scripted step.</summary>
    [Reactive]
    private MqttAuthenticateReasonCode _reasonCode = MqttAuthenticateReasonCode.ContinueAuthentication;

    /// <summary>Gets selectable payload formats.</summary>
    public IReadOnlyList<PayloadFormat> PayloadFormats { get; } = Enum.GetValues<PayloadFormat>();

    /// <summary>Gets selectable MQTT authenticate reason codes.</summary>
    public IReadOnlyList<MqttAuthenticateReasonCode> ReasonCodes { get; } = Enum.GetValues<MqttAuthenticateReasonCode>();
}
