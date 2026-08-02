// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Linq;

/// <summary>Represents an observable sequence with a grouping key.</summary>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="TElement">The type of the elements in the group.</typeparam>
/// <remarks>
/// This lean equivalent preserves the System.Reactive grouping contract without adding a System.Reactive dependency.
/// </remarks>
public interface IGroupedObservable<out TKey, out TElement> : IObservable<TElement>
{
    /// <summary>Gets the key that identifies this group.</summary>
    TKey Key { get; }
}
#endif
