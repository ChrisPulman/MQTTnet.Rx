// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia;
using ReactiveUI.Avalonia;

namespace MQTTnet.Rx.Toolkit;

/// <summary>The desktop entry point.</summary>
internal static class Program
{
    /// <summary>Starts the desktop application.</summary>
    /// <param name="args">The desktop command-line arguments.</param>
    [STAThread]
    private static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Configures Avalonia and ReactiveUI.</summary>
    /// <returns>The configured application builder.</returns>
    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI(static _ => { })
            .LogToTrace();
}
