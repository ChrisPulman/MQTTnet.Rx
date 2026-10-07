// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia.Headless;
using TUnit.Core.Interfaces;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Runs a UI test in an isolated application and dispatcher.</summary>
public sealed class ToolkitHeadlessExecutor : ITestExecutor
{
    /// <inheritdoc/>
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        await using var session = HeadlessUnitTestSession.StartNew(typeof(ToolkitHeadlessApplication));
        _ = await session.Dispatch(
            async () =>
            {
                await action();
                return true;
            },
            context.Execution.CancellationToken);
    }
}
