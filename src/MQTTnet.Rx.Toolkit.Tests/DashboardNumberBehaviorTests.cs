// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Controls;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies inbound finite-number dashboard behavior.</summary>
public sealed class DashboardNumberBehaviorTests
{
    /// <summary>Stores the non-finite payload byte count.</summary>
    private const int NonFinitePayloadByteCount = 8;

    /// <summary>Stores the default dashboard maximum.</summary>
    private const int DefaultDashboardMaximum = 100;

    /// <summary>Verifies non-finite inbound payloads do not become gauge values.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task ApplyKeepsNonFiniteInboundPayloadAsTextAsync()
    {
        var tile = new DashboardTileViewModel(
            "plant/value",
            static _ => Task.CompletedTask,
            static _ => { },
            static (_, _) => { });
        var message = new ReceivedMqttMessage(
            DateTimeOffset.UnixEpoch,
            "Client received",
            "plant/value",
            "Infinity",
            PayloadFormat.Utf8Text,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false,
            NonFinitePayloadByteCount,
            "Infinity"u8.ToArray(),
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

        await Assert.That(tile.VisualKind).IsEqualTo(DashboardVisualKind.Text);
        await Assert.That(tile.NumericValue).IsEqualTo(0);
        await Assert.That(tile.Maximum).IsEqualTo(DefaultDashboardMaximum);
    }

    /// <summary>Verifies large finite telemetry cannot overflow decimal UI range editors.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task RangeEditorBoundsLargeNumbersAsync()
    {
        await Assert.That(DashboardTileView.ToEditorValue(double.MaxValue)).IsEqualTo(decimal.MaxValue);
        await Assert.That(DashboardTileView.ToEditorValue(double.MinValue)).IsEqualTo(decimal.MinValue);
        await Assert.That(DashboardTileView.ToEditorValue(double.NaN)).IsEqualTo(0M);
        await Assert.That(DashboardTileView.ToEditorValue(DefaultDashboardMaximum)).IsEqualTo((decimal)DefaultDashboardMaximum);
    }
}
