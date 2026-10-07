// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI.Primitives;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies persisted dashboard layouts and dashboard publishing commands.</summary>
public sealed partial class MainWindowViewModelTests
{
    /// <summary>Stores the first persisted tile topic.</summary>
    private const string FirstTileTopic = "plant/first";

    /// <summary>Stores the configured persisted gauge minimum.</summary>
    private const double TileMinimum = -10;

    /// <summary>Stores the configured persisted gauge maximum.</summary>
    private const double TileMaximum = 250;

    /// <summary>Stores the number of tiles before removal.</summary>
    private const int TwoTileCount = 2;

#if WINDOWS
    /// <summary>Stores the largest supported republish interval in seconds.</summary>
    private const int MaximumRepublishSeconds = 86_400;
#endif

    /// <summary>Checks disposal releases dashboard tiles and supports repeated synchronous and asynchronous calls.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DisposeReleasesDashboardAndIsIdempotentAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), Path.Combine(directory, LayoutFileName));
            await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
            await Assert.That(model.DashboardTiles).Count().IsEqualTo(1);
            await model.DisposeAsync();
            model.Dispose();
            await model.DisposeAsync();
            await Assert.That(model.DashboardTiles).IsEmpty();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>Checks selecting duplicate topics, reordering, and removing tiles preserve a reusable layout.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DashboardCommandsPersistTileOrderAndSettingsAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(directory, LayoutFileName);
        try
        {
            await using (var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), path))
            {
                model.Publisher.Topic = FirstTileTopic;
                await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
                var first = model.DashboardTiles[0];
                first.Unit = "bar";
                first.AutoVisual = false;
                first.VisualKind = DashboardVisualKind.Gauge;
                first.Minimum = TileMinimum;
                first.Maximum = TileMaximum;
                first.Retain = true;
                first.PreserveEditor = true;
                first.QualityOfService = MqttQualityOfServiceLevel.ExactlyOnce;
                model.SelectedMessage = CreateMessage("plant/second", "false", PayloadFormat.Boolean);
                await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
                var second = model.DashboardTiles[1];
                await second.MoveRightCommand.Execute().FirstAsync();
                await second.MoveLeftCommand.Execute().FirstAsync();
                await Assert.That(model.DashboardTiles[0]).IsSameReferenceAs(second);
                await first.MoveLeftCommand.Execute().FirstAsync();
                await first.MoveLeftCommand.Execute().FirstAsync();
                await Assert.That(model.DashboardTiles[0]).IsSameReferenceAs(first);
                model.SelectedTopicNode = new(nameof(first), FirstTileTopic);
                await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
                await Assert.That(model.DashboardTiles).Count().IsEqualTo(TwoTileCount);
                await Assert.That(model.SelectedDashboardTile).IsSameReferenceAs(first);
                await second.RemoveTileCommand.Execute().FirstAsync();
                await second.MoveLeftCommand.Execute().FirstAsync();
                await second.MoveRightCommand.Execute().FirstAsync();
                await Assert.That(model.DashboardTiles).Count().IsEqualTo(1);
                await Assert.That(model.SelectedDashboardTile).IsSameReferenceAs(first);
                await model.SaveDashboardLayoutCommand.Execute().FirstAsync();
            }

            await using var restored = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), path);
            await Assert.That(restored.DashboardTiles).Count().IsEqualTo(1);
            var tile = restored.DashboardTiles[0];
            await Assert.That(tile.ToLayout()).IsEqualTo(new(FirstTileTopic, DashboardVisualKind.Gauge, false, "bar", TileMinimum, TileMaximum, true, true, MqttQualityOfServiceLevel.ExactlyOnce));
            await tile.RemoveTileCommand.Execute().FirstAsync();
            await Assert.That(restored.DashboardTiles).IsEmpty();
            await Assert.That(restored.SelectedDashboardTile).IsNull();
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo("[]");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>Checks invalid and empty saved layouts are reported or ignored and save failures remain visible.</summary>
    /// <param name="json">The saved layout contents.</param>
    /// <param name="warns">Whether loading should warn.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments("{invalid", true)]
    [Arguments("null", false)]
    [Arguments("[{\"topic\":\" \"}]", false)]
    public async Task DashboardLoadHandlesInvalidAndEmptyLayoutsAsync(string json, bool warns)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllTextAsync(path, json);
        try
        {
            await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), path);
            await Assert.That(model.DashboardTiles).IsEmpty();
            await Assert.That(model.LogEntries[0].Message.StartsWith("Dashboard layout was not loaded:", StringComparison.Ordinal)).IsEqualTo(warns);
            model.Publisher.Topic = string.Empty;
            await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
            await Assert.That(model.LogEntries[0].Message).IsEqualTo("Select or enter a concrete topic before adding a dashboard tile.");
            await model.SaveDashboardLayoutCommand.Execute().FirstAsync();
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo("[]");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Checks filesystem errors while saving are surfaced to the dashboard log.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DashboardSaveReportsDirectoryCreationFailureAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllTextAsync(path, "occupied");
        try
        {
            await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), Path.Combine(path, LayoutFileName));
            await model.SaveDashboardLayoutCommand.Execute().FirstAsync();
            await Assert.That(model.LogEntries[0].Level).IsEqualTo("Error");
            await Assert.That(model.LogEntries[0].Message).StartsWith("Dashboard layout was not saved:");
            await Assert.That(await File.ReadAllTextAsync(path)).IsEqualTo("occupied");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Checks tile publishes configure the shared message builder for each supported visual.</summary>
    /// <param name="kind">The tile visual kind.</param>
    /// <param name="payload">The editor value.</param>
    /// <param name="format">The expected publish format.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments((int)DashboardVisualKind.Toggle, "true", PayloadFormat.Boolean)]
    [Arguments((int)DashboardVisualKind.Gauge, "42", PayloadFormat.Number)]
    [Arguments((int)DashboardVisualKind.Json, "{}", PayloadFormat.Json)]
    [Arguments((int)DashboardVisualKind.Binary, "4142", PayloadFormat.Hex)]
    [Arguments((int)DashboardVisualKind.Text, "hello", PayloadFormat.Utf8Text)]
    public async Task TilePublishConfiguresMessageBuilderAsync(int kind, string payload, PayloadFormat format)
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), Path.Combine(directory, LayoutFileName));
            await model.AddSelectedTopicDashboardTileCommand.Execute().FirstAsync();
            var tile = model.DashboardTiles[0];
            tile.VisualKind = (DashboardVisualKind)kind;
            tile.EditableValue = payload;
            tile.Retain = false;
            tile.QualityOfService = MqttQualityOfServiceLevel.ExactlyOnce;
            model.Publisher.ContentType = string.Empty;
            await tile.PublishTileCommand.Execute().FirstAsync();
            await Assert.That(model.Publisher.Topic).IsEqualTo(tile.Topic);
            await Assert.That(model.Publisher.Payload).IsEqualTo(payload);
            await Assert.That(model.Publisher.PayloadFormat).IsEqualTo(format);
            await Assert.That(model.Publisher.Retain).IsFalse();
            await Assert.That(model.Publisher.QualityOfService).IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
            await Assert.That(model.Status).IsEqualTo("Publish failed");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

#if WINDOWS
    /// <summary>Checks configuration commands round-trip the TwinCAT editor and log import errors.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task TwinCatConfigurationCommandsRoundTripAndReportInvalidInputAsync()
    {
        await using var model = CreateModel();
        await Assert.That(model.TwinCat.IsSupported).IsTrue();
        model.TwinCat.RepublishSeconds = MaximumRepublishSeconds;
        await model.ExportTwinCatConfigurationCommand.Execute().FirstAsync();
        var exported = model.TwinCat.ConfigurationJson;
        model.TwinCat.PlcVariable = "GVL.Other";
        await model.ImportTwinCatConfigurationCommand.Execute().FirstAsync();
        await Assert.That(model.TwinCat.PlcVariable).IsEqualTo("GVL.Rig");
        await Assert.That(model.TwinCat.RepublishSeconds).IsEqualTo(MaximumRepublishSeconds);
        await Assert.That(model.Status).IsEqualTo("Import TwinCAT configuration complete");
        model.TwinCat.ConfigurationJson = "null";
        await model.ImportTwinCatConfigurationCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Import TwinCAT configuration failed");
        await Assert.That(model.TwinCat.ExportConfiguration()).IsEqualTo(exported);
        model.TwinCat.ConfigurationJson = "{invalid";
        await model.ImportTwinCatConfigurationCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Import TwinCAT configuration failed");
    }
#else
    /// <summary>Checks portable configuration commands report the unsupported Windows operation.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task TwinCatConfigurationCommandsReportUnsupportedPlatformAsync()
    {
        await using var model = CreateModel();
        await model.ExportTwinCatConfigurationCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Export TwinCAT configuration failed");
        await model.ImportTwinCatConfigurationCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Import TwinCAT configuration failed");
        await Assert.That(model.TwinCat.IsSupported).IsFalse();
    }
#endif
}
