// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MQTTnet.Rx.Toolkit.Views;

namespace MQTTnet.Rx.Toolkit;

/// <summary>The desktop application and workspace lifetime owner.</summary>
internal sealed class App : Application
{
    /// <summary>Tracks whether asynchronous connection cleanup has started.</summary>
    private bool _shutdownStarted;

    /// <summary>Allows the final close after connection cleanup completes.</summary>
    private bool _shutdownCompleted;

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                ViewModel = new(new MqttToolkitSessionService()),
            };
            desktop.MainWindow.Closing += OnMainWindowClosing;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Keeps the UI responsive while pending connections are cancelled and disposed.</summary>
    /// <param name="sender">The window requesting shutdown.</param>
    /// <param name="args">The cancellable window close event.</param>
    private async void OnMainWindowClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_shutdownCompleted || sender is not MainWindow { ViewModel: { } viewModel } window)
        {
            return;
        }

        args.Cancel = true;
        if (_shutdownStarted)
        {
            return;
        }

        _shutdownStarted = true;
        try
        {
            await viewModel.DisposeAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Trace.TraceError("MQTT Toolkit shutdown failed: {0}", exception);
        }
        finally
        {
            _shutdownCompleted = true;
            window.Close();
        }
    }
}
