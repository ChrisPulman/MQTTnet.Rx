// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Modbus.Reactive;
#else
namespace MQTTnet.Rx.Modbus;
#endif

/// <summary>Provides reactive MQTT extensions for Modbus reads and writes.</summary>
public static partial class CreateExtensions
{
    /// <summary>The default Modbus polling interval in milliseconds.</summary>
    private const double DefaultInterval = 100.0;

    /// <summary>Parses a single register without per-overload lazy delegate caches.</summary>
    private static readonly Func<string, ushort> RegisterParser = ParseRegister;

    /// <summary>Parses multiple registers without per-overload lazy delegate caches.</summary>
    private static readonly Func<string, ushort[]> RegistersParser = ParseRegisters;

    /// <summary>Parses a single coil without per-overload lazy delegate caches.</summary>
    private static readonly Func<string, bool> CoilParser = bool.Parse;

    /// <summary>Parses multiple coils without per-overload lazy delegate caches.</summary>
    private static readonly Func<string, bool[]> CoilsParser = ParseCoils;

    /// <summary>Parses a comma-separated sequence of coil values.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed coil values.</returns>
    private static bool[] ParseCoils(string payload)
    {
        var parts = payload.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var values = new bool[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            values[index] = bool.Parse(parts[index].Trim());
        }

        return values;
    }

    /// <summary>Parses a single register value.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed register value.</returns>
    private static ushort ParseRegister(string payload) =>
        ushort.Parse(payload, CultureInfo.InvariantCulture);

    /// <summary>Parses a comma-separated sequence of register values.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed register values.</returns>
    private static ushort[] ParseRegisters(string payload)
    {
        var parts = payload.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var values = new ushort[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            values[index] = ushort.Parse(parts[index].Trim(), CultureInfo.InvariantCulture);
        }

        return values;
    }
}
