// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides scheduler compatibility for the shared lean and Reactive test sources.</summary>
internal static class TestSchedulers
{
    /// <summary>Gets the shared task-pool scheduler or sequencer.</summary>
    internal static TestScheduler TaskPool { get; } =
#if REACTIVE_SHIM
        System.Reactive.Concurrency.TaskPoolScheduler.Default;
#else
        ReactiveUI.Primitives.Concurrency.TaskPoolSequencer.Instance;
#endif
}
