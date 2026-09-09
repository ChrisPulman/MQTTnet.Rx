// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Coordinates the TwinCAT structure publisher and reusable configuration.</summary>
internal sealed partial class MainWindowViewModel
{
    /// <summary>Gets editable TwinCAT bridge settings.</summary>
    public TwinCatBridgeViewModel TwinCat { get; } = new();

#if !WINDOWS
    /// <summary>Reports configuration unavailability in a portable build.</summary>
    /// <param name="cancellationToken">The command cancellation token.</param>
    /// <returns>A task representing the unsupported operation.</returns>
    private static Task UnsupportedTwinCatConfiguration(CancellationToken cancellationToken) =>
        throw new PlatformNotSupportedException("TwinCAT configuration requires the Windows Toolkit build.");
#endif

    /// <summary>Starts publishing every member of the configured PLC structure.</summary>
    /// <returns>The asynchronous bridge startup operation.</returns>
    [ReactiveCommand]
    private Task StartTwinCatBridgeAsync() => RunOperationAsync(
        "Subscribe structure",
        cancellationToken => _session.StartTwinCatBridgeAsync(TwinCat, cancellationToken));

    /// <summary>Stops the active PLC structure publisher.</summary>
    /// <returns>The asynchronous bridge stop operation.</returns>
    [ReactiveCommand]
    private Task StopTwinCatBridgeAsync() => RunOperationAsync(
        "Stop structure",
        _session.StopTwinCatBridgeAsync);

    /// <summary>Exports settings for reuse by an application using the TwinCAT library.</summary>
    /// <returns>The asynchronous configuration operation.</returns>
    [ReactiveCommand]
    private Task ExportTwinCatConfigurationAsync()
    {
#if WINDOWS
        return RunOperationAsync("Export TwinCAT configuration", _ =>
        {
            var json = TwinCat.ExportConfiguration();
            _postToUi(() => TwinCat.ConfigurationJson = json);
            return Task.CompletedTask;
        });
#else
        return RunOperationAsync("Export TwinCAT configuration", UnsupportedTwinCatConfiguration);
#endif
    }

    /// <summary>Applies a saved TwinCAT bridge configuration to the editors.</summary>
    /// <returns>The asynchronous configuration operation.</returns>
    [ReactiveCommand]
    private Task ImportTwinCatConfigurationAsync()
    {
#if WINDOWS
        return RunOperationAsync("Import TwinCAT configuration", _ =>
        {
            TwinCat.ImportConfiguration();
            return Task.CompletedTask;
        });
#else
        return RunOperationAsync("Import TwinCAT configuration", UnsupportedTwinCatConfiguration);
#endif
    }
}
