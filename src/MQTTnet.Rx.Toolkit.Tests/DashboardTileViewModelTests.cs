// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies dashboard tile editing and visualization behavior.</summary>
public sealed class DashboardTileViewModelTests
{
    /// <summary>Stores the byte count of the boolean payload fixture.</summary>
    private const int BooleanPayloadByteCount = 4;

    /// <summary>Verifies that manual visualization and editor choices survive incoming messages.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ApplyPreservesManualVisualAndEditorWhenConfiguredAsync()
    {
        var tile = new DashboardTileViewModel(
            "plant/value",
            static _ => Task.CompletedTask,
            static _ => { },
            static (_, _) => { })
        {
            AutoVisual = false,
            VisualKind = DashboardVisualKind.Text,
            PreserveEditor = true,
            EditableValue = "manual edit",
        };
        var message = new ReceivedMqttMessage(
            DateTimeOffset.UnixEpoch,
            "Client received",
            "plant/value",
            "true",
            PayloadFormat.Boolean,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false,
            BooleanPayloadByteCount,
            [0x74, 0x72, 0x75, 0x65],
            null,
            MqttPayloadFormatIndicator.CharacterData,
            null,
            null,
            0,
            [],
            0,
            false,
            []);

        tile.Apply(message);

        await Assert.That(tile.DisplayValue).IsEqualTo("true");
        await Assert.That(tile.EditableValue).IsEqualTo("manual edit");
        await Assert.That(tile.VisualKind).IsEqualTo(DashboardVisualKind.Text);
        await Assert.That(tile.BooleanValue).IsTrue();
    }
}
