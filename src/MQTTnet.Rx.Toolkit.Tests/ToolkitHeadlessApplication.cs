// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Headless;
using ReactiveUI.Avalonia;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Configures the real application with an in-memory window backend.</summary>
public static class ToolkitHeadlessApplication
{
    /// <summary>Creates the application builder used by isolated UI tests.</summary>
    /// <returns>The headless application builder.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseHeadless(new())
        .UseReactiveUI(static _ => { });
}
