// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MQTTnet.Rx.Toolkit.ViewModels;
using MQTTnet.Rx.Toolkit.Views;
using TUnit.Core.Executors;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies the real workspace connects editor controls to its view model.</summary>
[TestExecutor<ToolkitHeadlessExecutor>]
public sealed class MainWindowBindingTests
{
    /// <summary>Verifies activation loads the workspace and synchronizes connection edits in both directions.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task ActivationBindsConnectionEditorsAsync()
    {
        var layoutPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), layoutPath);
        var window = new MainWindow { ViewModel = model };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var host = await Assert.That(window.FindControl<TextBox>("ConnectionHostTextBox")).IsNotNull();
            await Assert.That(host.Text).IsEqualTo(model.Connection.Host);
            model.Connection.Host = "broker.example";
            Dispatcher.UIThread.RunJobs();
            await Assert.That(host.Text).IsEqualTo("broker.example");
            host.Text = "edited.example";
            Dispatcher.UIThread.RunJobs();
            await Assert.That(model.Connection.Host).IsEqualTo("edited.example");
            var status = await Assert.That(window.FindControl<TextBlock>("StatusTextBlock")).IsNotNull();
            await Assert.That(status.Text).IsEqualTo(model.Status);
        }
        finally
        {
            window.Close();
            File.Delete(layoutPath);
        }
    }

    /// <summary>Verifies metadata and authentication rows materialize with editable bound values.</summary>
    /// <returns>The asynchronous test.</returns>
    [Test]
    public async Task MetadataCollectionsRenderEditableRowsAsync()
    {
        var layoutPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await using var model = new MainWindowViewModel(new(TimeProvider.System), TimeProvider.System, static action => action(), layoutPath);
        model.Connection.UserProperties.Add(new() { Name = "connection-name", Value = "connection-value" });
        model.Connection.WillUserProperties.Add(new() { Name = "will-name", Value = "will-value" });
        model.Subscription.UserProperties.Add(new() { Name = "subscription-name", Value = "subscription-value" });
        model.Publisher.UserProperties.Add(new() { Name = "publish-name", Value = "publish-value" });
        model.Connection.EnhancedAuthenticationSteps.Add(new() { Data = "challenge-response" });
        var window = new MainWindow { ViewModel = model };
        try
        {
            window.Show();
            var expanders = new List<Expander>();
            foreach (var element in window.GetLogicalDescendants())
            {
                if (element is Expander expander)
                {
                    expanders.Add(expander);
                }
            }

            foreach (var expander in expanders)
            {
                expander.IsExpanded = true;
            }

            Dispatcher.UIThread.RunJobs();
            await Assert.That(HasEditor(window, "connection-name")).IsTrue();
            await Assert.That(HasEditor(window, "will-name")).IsTrue();
            await Assert.That(HasEditor(window, "subscription-name")).IsTrue();
            await Assert.That(HasEditor(window, "challenge-response")).IsTrue();
            TabControl? workspaceTabs = null;
            foreach (var element in window.GetLogicalDescendants())
            {
                if (element is TabControl tabControl)
                {
                    workspaceTabs = tabControl;
                    break;
                }
            }

            var tabs = await Assert.That(workspaceTabs).IsNotNull();
            tabs.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            foreach (var element in window.GetLogicalDescendants())
            {
                if (element is Expander expander)
                {
                    expander.IsExpanded = true;
                }
            }

            Dispatcher.UIThread.RunJobs();
            await Assert.That(HasEditor(window, "publish-name")).IsTrue();
        }
        finally
        {
            window.Close();
            File.Delete(layoutPath);
        }
    }

    /// <summary>Finds an editable metadata value in the rendered workspace.</summary>
    /// <param name="window">The active workspace.</param>
    /// <param name="text">The bound editor text.</param>
    /// <returns>Whether the editor is present.</returns>
    private static bool HasEditor(MainWindow window, string text)
    {
        foreach (var element in window.GetVisualDescendants())
        {
            if (element is TextBox editor && editor.Text == text)
            {
                return true;
            }
        }

        return false;
    }
}
