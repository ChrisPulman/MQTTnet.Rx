// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Returns scripted subscription behavior and retains the supplied observer through the script.</summary>
/// <typeparam name="T">The observable element type.</typeparam>
/// <param name="subscribe">The subscription behavior.</param>
internal sealed class ScriptedObservable<T>(Func<int, IObserver<T>, IDisposable> subscribe) : IObservable<T>
{
    /// <summary>Gets the number of subscription attempts.</summary>
    internal int SubscribeCount { get; private set; }

    /// <inheritdoc/>
    IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
    {
        SubscribeCount++;
        return subscribe(SubscribeCount, observer);
    }
}
