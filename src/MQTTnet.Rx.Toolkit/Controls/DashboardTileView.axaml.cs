// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace MQTTnet.Rx.Toolkit.Controls;

/// <summary>A reusable topic monitor and message editor with activation-scoped bindings.</summary>
internal sealed partial class DashboardTileView : ReactiveUserControl<DashboardTileViewModel>
{
    /// <summary>The initial gauge upper bound when the editor is empty.</summary>
    private const decimal DefaultMaximum = 100;

    /// <summary>Initializes a new instance of the <see cref="DashboardTileView"/> class.</summary>
    public DashboardTileView() => InitializeComponent();

    /// <summary>Converts a gauge bound to the decimal range supported by the numeric editor.</summary>
    /// <param name="value">The gauge bound, which may exceed decimal precision.</param>
    /// <returns>A bounded decimal value for the editor.</returns>
    internal static decimal? ToEditorValue(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (value >= (double)decimal.MaxValue)
        {
            return decimal.MaxValue;
        }

        return value <= (double)decimal.MinValue ? decimal.MinValue : (decimal)value;
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = this.WhenActivated(disposables =>
        {
            _ = this.OneWayBind(ViewModel, static vm => vm.Topic, static view => view.TopicLabel.Text).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.DisplayValue, static view => view.ValueLabel.Text).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.Unit, static view => view.UnitLabel.Text).DisposeWith(disposables);
            _ = this.OneWayBind(
                ViewModel,
                static vm => vm.LastSeen,
                static view => view.LastSeenLabel.Text,
                static value => value.HasValue ? $"Received {value:T}" : "Waiting for a message").DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.NumericValue, static view => view.ValueGauge.Value).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.Minimum, static view => view.ValueGauge.MinValue).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.Maximum, static view => view.ValueGauge.MaxValue).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.VisualKind, static view => view.ValueGauge.IsVisible, static kind => kind == DashboardVisualKind.Gauge).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.VisualKind, static view => view.BooleanIndicator.IsVisible, static kind => kind == DashboardVisualKind.Toggle).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.BooleanValue, static view => view.BooleanIndicator.IsChecked).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.VisualKinds, static view => view.VisualSelector.ItemsSource).DisposeWith(disposables);
            _ = this.OneWayBind(ViewModel, static vm => vm.QualityOfServiceLevels, static view => view.QualitySelector.ItemsSource).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.VisualKind, static view => view.VisualSelector.SelectedItem).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.AutoVisual, static view => view.AutoVisualToggle.IsChecked).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.QualityOfService, static view => view.QualitySelector.SelectedItem).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.Unit, static view => view.UnitEditor.Text).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.Minimum, static view => view.MinimumEditor.Value, ToEditorValue, static value => (double)(value ?? 0)).DisposeWith(disposables);
            _ = this.Bind(
                ViewModel,
                static vm => vm.Maximum,
                static view => view.MaximumEditor.Value,
                ToEditorValue,
                static value => (double)(value ?? DefaultMaximum)).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.EditableValue, static view => view.ValueEditor.Text).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.Retain, static view => view.RetainToggle.IsChecked).DisposeWith(disposables);
            _ = this.Bind(ViewModel, static vm => vm.PreserveEditor, static view => view.PreserveEditorToggle.IsChecked).DisposeWith(disposables);
            _ = this.BindCommand(ViewModel, static vm => vm.PublishTileCommand, static view => view.PublishButton).DisposeWith(disposables);
            _ = this.BindCommand(ViewModel, static vm => vm.RemoveTileCommand, static view => view.RemoveButton).DisposeWith(disposables);
            _ = this.BindCommand(ViewModel, static vm => vm.MoveLeftCommand, static view => view.MoveLeftButton).DisposeWith(disposables);
            _ = this.BindCommand(ViewModel, static vm => vm.MoveRightCommand, static view => view.MoveRightButton).DisposeWith(disposables);
        });
    }
}
