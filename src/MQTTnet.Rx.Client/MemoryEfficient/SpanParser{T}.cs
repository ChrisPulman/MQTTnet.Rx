// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>Delegate for parsing a span into a value type.</summary>
/// <typeparam name="T">The target type.</typeparam>
/// <param name="data">The data span to parse.</param>
/// <returns>The parsed value.</returns>
public delegate T SpanParser<out T>(ReadOnlySpan<byte> data);
