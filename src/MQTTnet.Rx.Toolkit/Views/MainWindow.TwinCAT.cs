// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace MQTTnet.Rx.Toolkit.Views;

/// <summary>Binds the structure bridge configuration and its commands.</summary>
internal sealed partial class MainWindow
{
    /// <summary>Binds the TwinCAT configuration for this activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindTwinCat(MultipleDisposable disposables)
    {
        _ = this.OneWayBind(ViewModel, static vm => vm.TwinCat.IsSupported, static view => view.TwinCatTab.IsVisible).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.AmsNetId, static view => view.TwinCatAddressEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.PlcVariable, static view => view.TwinCatSymbolEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.TopicPrefix, static view => view.TwinCatTopicEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.Retain, static view => view.TwinCatRetainToggle.IsChecked).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.TwinCat.QualityOfServiceLevels, static view => view.TwinCatQualitySelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.QualityOfService, static view => view.TwinCatQualitySelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.TwinCat.ConfigurationJson, static view => view.TwinCatConfigurationEditor.Text).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.StartTwinCatBridgeCommand, static view => view.StartTwinCatBridgeButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.StopTwinCatBridgeCommand, static view => view.StopTwinCatBridgeButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.ExportTwinCatConfigurationCommand, static view => view.ExportTwinCatConfigurationButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.ImportTwinCatConfigurationCommand, static view => view.ImportTwinCatConfigurationButton).DisposeWith(disposables);
    }
}
