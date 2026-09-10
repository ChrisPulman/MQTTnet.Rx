// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI.Primitives;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies top-level Toolkit view-model command behavior.</summary>
public sealed class MainWindowViewModelTests
{
    /// <summary>Verifies queued UI dispatch cannot leave a completed publish command in the running state.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task PublishCommandUsesFinalStatusWhenUiDispatcherIsQueuedAsync()
    {
        var pendingUiWork = new Queue<Action>();
        var layoutPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using var viewModel = new MainWindowViewModel(
            new(TimeProvider.System),
            TimeProvider.System,
            pendingUiWork.Enqueue,
            layoutPath);

        await viewModel.PublishCommand.Execute().FirstAsync();
        while (pendingUiWork.TryDequeue(out var action))
        {
            action();
        }

        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.Status).IsEqualTo("Publish failed");
    }

    /// <summary>Verifies disconnected enhanced-authentication sends are reported as command failures without escaping.</summary>
    /// <returns>A task representing the asynchronous assertions.</returns>
    [Test]
    public async Task SendEnhancedAuthenticationExchangeCommandReportsDisconnectedErrorAsync()
    {
        var pendingUiWork = new Queue<Action>();
        var thrownExceptions = new List<Exception>();
        var layoutPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using var viewModel = new MainWindowViewModel(
            new(TimeProvider.System),
            TimeProvider.System,
            pendingUiWork.Enqueue,
            layoutPath);
        using var thrownSubscription = viewModel.SendEnhancedAuthenticationExchangeCommand.ThrownExceptions.SubscribePrimitives(thrownExceptions.Add);

        await viewModel.SendEnhancedAuthenticationExchangeCommand.Execute().FirstAsync();
        while (pendingUiWork.TryDequeue(out var action))
        {
            action();
        }

        await Assert.That(thrownExceptions).Count().IsEqualTo(0);
        await Assert.That(viewModel.IsBusy).IsFalse();
        await Assert.That(viewModel.Status).IsEqualTo("Send enhanced authentication failed");
        await Assert.That(ContainsLogMessage(viewModel, "Connect before using MQTT operations.")).IsTrue();
    }

    /// <summary>Determines whether the view model contains a log message.</summary>
    /// <param name="viewModel">The view model to inspect.</param>
    /// <param name="message">The message text to find.</param>
    /// <returns><see langword="true"/> when the message is present.</returns>
    private static bool ContainsLogMessage(MainWindowViewModel viewModel, string message)
    {
        foreach (var logEntry in viewModel.LogEntries)
        {
            if (string.Equals(logEntry.Message, message, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
