// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using System.Text.Json;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies invalid saved configurations leave the editor unchanged.</summary>
public sealed partial class TwinCatToolkitBridgeTests
{
    /// <summary>Rejects intervals that cannot be represented by the configuration editor.</summary>
    /// <param name="interval">The invalid interval in invariant TimeSpan format.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments("-00:00:01")]
    [Arguments("1.00:00:01")]
    [Arguments("00:00:00.5000000")]
    public async Task InvalidSavedIntervalLeavesExistingConfigurationUnchangedAsync(string interval)
    {
        var configuration = CreateConfiguration();
        var original = configuration.ExportConfiguration();
        var invalid = configuration.BuildOptions();
        invalid.PlcVariable = "GVL.Other";
        invalid.RepublishInterval = TimeSpan.Parse(interval, System.Globalization.CultureInfo.InvariantCulture);
        configuration.ConfigurationJson = JsonSerializer.Serialize(invalid);

        await Assert.That(configuration.ImportConfiguration).Throws<FormatException>();
        await Assert.That(configuration.ExportConfiguration()).IsEqualTo(original);
    }
}
#endif
