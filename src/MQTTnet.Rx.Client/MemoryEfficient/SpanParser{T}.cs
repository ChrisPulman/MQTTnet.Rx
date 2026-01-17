// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.MemoryEfficient;

/// <summary>
/// Delegate for parsing a span into a value type.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
/// <param name="data">The data span to parse.</param>
/// <returns>The parsed value.</returns>
public delegate T SpanParser<out T>(ReadOnlySpan<byte> data);
