// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Threading;
using MQTTnet.Rx.Toolkit.Controls;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using TUnit.Core.Executors;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies dashboard controls update from their model and retain edited values.</summary>
[TestExecutor<ToolkitHeadlessExecutor>]
public sealed class DashboardTileBindingTests
{
    /// <summary>The test gauge value.</summary>
    private const double GaugeValue = 42;

    /// <summary>The upper bound used when the gauge editor is cleared.</summary>
    private const double DefaultMaximum = 100;

    /// <summary>Verifies activation and visualization changes bind the tile controls.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task ActivationBindsGaugeAndEditorAsync()
    {
        var model = new DashboardTileViewModel("plant/value", static _ => Task.CompletedTask, static _ => { }, static (_, _) => { })
        {
            Unit = "bar",
            VisualKind = DashboardVisualKind.Gauge,
            NumericValue = GaugeValue,
            EditableValue = "editable",
        };
        var tile = new DashboardTileView { ViewModel = model };
        var window = new Window { Content = tile };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var topic = await Assert.That(tile.FindControl<TextBlock>("TopicLabel")).IsNotNull();
            await Assert.That(topic.Text).IsEqualTo(model.Topic);
            var editor = await Assert.That(tile.FindControl<TextBox>("ValueEditor")).IsNotNull();
            await Assert.That(editor.Text).IsEqualTo("editable");
            editor.Text = "changed";
            Dispatcher.UIThread.RunJobs();
            await Assert.That(model.EditableValue).IsEqualTo("changed");
            model.VisualKind = DashboardVisualKind.Toggle;
            model.BooleanValue = true;
            Dispatcher.UIThread.RunJobs();
            var indicator = await Assert.That(tile.FindControl<CheckBox>("BooleanIndicator")).IsNotNull();
            await Assert.That(indicator.IsVisible).IsTrue();
            await Assert.That(indicator.IsChecked).IsTrue();
            model.LastSeen = TimeProvider.System.GetUtcNow();
            var minimum = await Assert.That(tile.FindControl<NumericUpDown>("MinimumEditor")).IsNotNull();
            var maximum = await Assert.That(tile.FindControl<NumericUpDown>("MaximumEditor")).IsNotNull();
            minimum.Value = null;
            maximum.Value = null;
            Dispatcher.UIThread.RunJobs();
            await Assert.That(model.Minimum).IsEqualTo(0);
            await Assert.That(model.Maximum).IsEqualTo(DefaultMaximum);
            var seen = await Assert.That(tile.FindControl<TextBlock>("LastSeenLabel")).IsNotNull();
            await Assert.That(string.IsNullOrEmpty(seen.Text)).IsFalse();
        }
        finally
        {
            window.Close();
        }
    }
}
