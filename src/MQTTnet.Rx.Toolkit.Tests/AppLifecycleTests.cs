// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Avalonia;
using Avalonia.Threading;
using ReactiveUI.Primitives;
using TUnit.Core.Executors;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies startup creates a workspace and closing releases its session.</summary>
[TestExecutor<ToolkitHeadlessExecutor>]
public sealed class AppLifecycleTests
{
    /// <summary>The bounded time allowed for connection cancellation and close.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies desktop initialization and cleanup through the real window close event.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task DesktopStartupAndCloseDisposeWorkspaceAsync()
    {
        var application = await Assert.That(Application.Current).IsTypeOf<App>()
            ?? throw new InvalidOperationException("The application was not initialized.");
        var window = application.CreateMainWindow();
        var model = await Assert.That(window.ViewModel).IsNotNull();
        model.DashboardTiles.Add(new("lifetime", static _ => Task.CompletedTask, static _ => { }, static (_, _) => { }));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(window.IsVisible).IsTrue();
        window.Close();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(window.IsVisible).IsFalse();
        await Assert.That(model.DashboardTiles).IsEmpty();
        await model.DisposeAsync();
    }

    /// <summary>Verifies closing during connection setup cancels work and keeps repeated close requests safe.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task CloseDuringPendingConnectCancelsAndDisposesAsync()
    {
        using var timeout = new CancellationTokenSource(Timeout);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var application = await Assert.That(Application.Current).IsTypeOf<App>()
            ?? throw new InvalidOperationException("The application was not initialized.");
        var window = application.CreateMainWindow();
        var model = await Assert.That(window.ViewModel).IsNotNull();
        model.DashboardTiles.Add(new("lifetime", static _ => Task.CompletedTask, static _ => { }, static (_, _) => { }));
        model.Connection.Host = IPAddress.Loopback.ToString();
        model.Connection.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        model.Connection.StartEmbeddedServer = false;
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult();
        window.Show();
        var accepted = listener.AcceptTcpClientAsync(timeout.Token);
        var connecting = model.ConnectCommand.Execute().FirstAsync();
        using var socket = await accepted;
        window.Close();
        window.Close();
        await closed.Task.WaitAsync(timeout.Token);
        await connecting.WaitAsync(timeout.Token);
        await Assert.That(window.IsVisible).IsFalse();
        await Assert.That(model.DashboardTiles).IsEmpty();
        await model.DisposeAsync();
    }
}
