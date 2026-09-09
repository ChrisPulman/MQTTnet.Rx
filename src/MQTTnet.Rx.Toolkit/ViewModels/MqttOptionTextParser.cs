// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Parses editable option text entered in advanced MQTT Toolkit forms.</summary>
internal static class MqttOptionTextParser
{
    /// <summary>Parses key-colon-value header text into request headers.</summary>
    /// <param name="value">The UI header text to parse.</param>
    /// <returns>The parsed request headers.</returns>
    internal static Dictionary<string, string> ParseHeaders(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return result;
    }

    /// <summary>Splits semicolon, comma, or newline separated UI text.</summary>
    /// <param name="value">The UI text to split.</param>
    /// <returns>The non-empty trimmed values.</returns>
    internal static List<string> SplitList(string value)
    {
        var result = new List<string>();
        foreach (var item in value.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result.Add(item);
        }

        return result;
    }
}
