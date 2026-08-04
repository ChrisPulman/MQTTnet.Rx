// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Copies non-generic MQTTnet property dictionaries into immutable snapshots.</summary>
internal static class MqttPropertySnapshot
{
    /// <summary>Copies a dictionary at subscription time.</summary>
    /// <param name="source">The source dictionary.</param>
    /// <returns>An independent read-only snapshot.</returns>
    internal static IReadOnlyDictionary<object, object?> Copy(IDictionary source)
    {
        var snapshot = new Dictionary<object, object?>(source.Count);
        foreach (DictionaryEntry item in source)
        {
            snapshot[item.Key] = item.Value;
        }

        return snapshot;
    }
}
