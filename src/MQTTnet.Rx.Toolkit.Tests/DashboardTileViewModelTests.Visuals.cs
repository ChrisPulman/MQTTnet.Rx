// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI.Primitives;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies automatic visualization, layout restoration, and tile command callbacks.</summary>
public sealed partial class DashboardTileViewModelTests
{
    /// <summary>Stores the topic used by visualization tests.</summary>
    private const string TileTopic = "plant/value";

    /// <summary>Stores the telemetry that expands the gauge.</summary>
    private const double ExpandedGaugeValue = 120.5;

    /// <summary>Stores the expanded gauge maximum.</summary>
    private const double ExpandedGaugeMaximum = 131;

    /// <summary>Stores the persisted gauge minimum.</summary>
    private const double GaugeMinimum = -10;

    /// <summary>Stores the persisted gauge maximum.</summary>
    private const double GaugeMaximum = 200;

    /// <summary>Stores the left and right command count.</summary>
    private const int ExpectedMoveCount = 2;

    /// <summary>Checks automatic visual selection and editor updates reflect received content.</summary>
    /// <param name="payload">The payload text.</param>
    /// <param name="format">The detected format.</param>
    /// <param name="kind">The expected visual.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments("{\"value\":42}", PayloadFormat.Json, (int)DashboardVisualKind.Json)]
    [Arguments("00FF", PayloadFormat.Hex, (int)DashboardVisualKind.Binary)]
    [Arguments("AA==", PayloadFormat.Base64, (int)DashboardVisualKind.Binary)]
    [Arguments("false", PayloadFormat.Boolean, (int)DashboardVisualKind.Toggle)]
    [Arguments("120.5", PayloadFormat.Number, (int)DashboardVisualKind.Gauge)]
    [Arguments("hello", PayloadFormat.Utf8Text, (int)DashboardVisualKind.Text)]
    public async Task ApplyChoosesVisualAndUpdatesEditorAsync(string payload, PayloadFormat format, int kind)
    {
        var tile = new DashboardTileViewModel(TileTopic, static _ => Task.CompletedTask, static _ => { }, static (_, _) => { });
        var message = new ReceivedMqttMessage(
            DateTimeOffset.UnixEpoch,
            "Client received",
            TileTopic,
            payload,
            format,
            MqttQualityOfServiceLevel.AtLeastOnce,
            false,
            payload.Length,
            System.Text.Encoding.UTF8.GetBytes(payload),
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
        await Assert.That(tile.VisualKind).IsEqualTo((DashboardVisualKind)kind);
        await Assert.That(tile.EditableValue).IsEqualTo(payload);
        await Assert.That(tile.DisplayValue).IsEqualTo(payload);
        await Assert.That(tile.LastSeen).IsEqualTo(DateTimeOffset.UnixEpoch);
        if (kind == (int)DashboardVisualKind.Gauge)
        {
            await Assert.That(tile.NumericValue).IsEqualTo(ExpandedGaugeValue);
            await Assert.That(tile.Maximum).IsEqualTo(ExpandedGaugeMaximum);
        }
    }

    /// <summary>Checks saved layout controls survive restoration and commands delegate the same tile instance.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task LayoutRestoresPublishingControlsAndCommandsDelegateTileAsync()
    {
        DashboardTileViewModel? published = null;
        DashboardTileViewModel? removed = null;
        var moves = new List<int>();
        var tile = new DashboardTileViewModel(
            TileTopic,
            current =>
            {
                published = current;
                return Task.CompletedTask;
            },
            current => removed = current,
            (_, offset) => moves.Add(offset));
        var layout = new DashboardTileLayout(TileTopic, DashboardVisualKind.Gauge, false, "bar", GaugeMinimum, GaugeMaximum, true, true, MqttQualityOfServiceLevel.ExactlyOnce);
        tile.ApplyLayout(layout);
        await Assert.That(tile.ToLayout()).IsEqualTo(layout);
        await tile.PublishTileCommand.Execute().FirstAsync();
        await tile.MoveLeftCommand.Execute().FirstAsync();
        await tile.MoveRightCommand.Execute().FirstAsync();
        await tile.RemoveTileCommand.Execute().FirstAsync();
        await Assert.That(published).IsSameReferenceAs(tile);
        await Assert.That(removed).IsSameReferenceAs(tile);
        await Assert.That(moves).Count().IsEqualTo(ExpectedMoveCount);
        await Assert.That(moves[0]).IsEqualTo(-1);
        await Assert.That(moves[1]).IsEqualTo(1);
    }
}
