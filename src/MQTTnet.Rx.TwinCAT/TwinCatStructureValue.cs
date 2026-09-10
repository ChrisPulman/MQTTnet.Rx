// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Describes one TwinCAT structure member value prepared for MQTT publication.</summary>
/// <param name="MemberName">The structure member name.</param>
/// <param name="Topic">The generated MQTT topic.</param>
/// <param name="Value">The current member value.</param>
public readonly record struct TwinCatStructureValue(string MemberName, string Topic, object? Value);
