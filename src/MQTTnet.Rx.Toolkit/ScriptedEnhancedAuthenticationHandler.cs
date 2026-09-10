// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit;

/// <summary>Runs ordered MQTT enhanced-authentication response steps.</summary>
/// <param name="steps">The ordered response steps to send during enhanced authentication.</param>
internal sealed class ScriptedEnhancedAuthenticationHandler(IReadOnlyList<EnhancedAuthenticationStepViewModel> steps) : IMqttEnhancedAuthenticationHandler
{
    /// <summary>Stores the next scripted response index.</summary>
    private int _nextStepIndex;

    /// <inheritdoc/>
    public async Task HandleEnhancedAuthenticationAsync(MqttEnhancedAuthenticationEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);
        if (eventArgs.ReasonCode == MqttAuthenticateReasonCode.Success)
        {
            Reset();
            return;
        }

        if (_nextStepIndex >= steps.Count)
        {
            throw new InvalidOperationException("No configured response remains for the enhanced-authentication challenge.");
        }

        var step = steps[_nextStepIndex];
        if (eventArgs.ReasonCode != step.ReasonCode)
        {
            throw new InvalidOperationException($"Expected enhanced-authentication challenge reason {step.ReasonCode} but received {eventArgs.ReasonCode}.");
        }

        _nextStepIndex++;
        var options = new SendMqttEnhancedAuthenticationDataOptions
        {
            Data = MqttPayloadEncoding.BuildBytes(step.Data, step.DataFormat),
            ReasonString = step.Reason,
        };
        await eventArgs.SendAsync(options, eventArgs.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts the configured response sequence for a new authentication exchange.</summary>
    internal void Reset() => _nextStepIndex = 0;
}
