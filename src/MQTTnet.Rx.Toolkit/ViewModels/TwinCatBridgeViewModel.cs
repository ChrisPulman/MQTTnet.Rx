// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using System.Text.Json;
#endif
using MQTTnet.Protocol;
#if WINDOWS
using MQTTnet.Rx.TwinCAT;
#endif
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Edits the connection and publication settings for a TwinCAT structure.</summary>
internal sealed partial class TwinCatBridgeViewModel : ViewModelBase
{
#if WINDOWS
    /// <summary>Stores the maximum interval supported by the configuration editor.</summary>
    private const int MaximumRepublishSeconds = 86_400;

    /// <summary>Stores formatting options for reusable bridge configuration.</summary>
    private static readonly JsonSerializerOptions ConfigurationSerializerOptions = new() { WriteIndented = true };
#endif

    /// <summary>Stores the ADS network identifier.</summary>
    [Reactive]
    private string _amsNetId = string.Empty;

    /// <summary>Stores the ADS runtime port.</summary>
    [Reactive]
    private int _adsPort = 851;

    /// <summary>Stores the fully qualified structure symbol.</summary>
    [Reactive]
    private string _plcVariable = "GVL.Rig";

    /// <summary>Stores the MQTT topic prefix for structure members.</summary>
    [Reactive]
    private string _topicPrefix = "plc/rig";

    /// <summary>Stores the selected delivery quality.</summary>
    [Reactive]
    private MqttQualityOfServiceLevel _qualityOfService = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Stores whether member values are retained for later subscribers.</summary>
    [Reactive]
    private bool _retain = true;

    /// <summary>Stores the optional snapshot republish interval; zero publishes only changes.</summary>
    [Reactive]
    private int _republishSeconds;

    /// <summary>Stores the editable exported bridge configuration.</summary>
    [Reactive]
    private string _configurationJson = string.Empty;

    /// <summary>Gets whether this build supports the Windows ADS driver.</summary>
    public bool IsSupported =>
#if WINDOWS
        true;
#else
        false;
#endif

    /// <summary>Gets the available MQTT delivery qualities.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

#if WINDOWS
    /// <summary>Exports a reusable core-library configuration.</summary>
    /// <returns>The serialized TwinCAT structure settings.</returns>
    internal string ExportConfiguration()
    {
        return JsonSerializer.Serialize(BuildOptions(), ConfigurationSerializerOptions);
    }

    /// <summary>Applies a core-library configuration to the editor.</summary>
    internal void ImportConfiguration()
    {
        var options = JsonSerializer.Deserialize<TwinCatStructureOptions>(ConfigurationJson)
            ?? throw new FormatException("The TwinCAT configuration must be a JSON object.");
        var seconds = options.RepublishInterval?.TotalSeconds ?? 0;
        if (seconds < 0 || seconds > MaximumRepublishSeconds || options.RepublishInterval.GetValueOrDefault().Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw new FormatException("The republish interval must be a whole number of seconds between 0 and 86400.");
        }

        AmsNetId = options.AmsNetId;
        AdsPort = options.AdsPort;
        PlcVariable = options.PlcVariable;
        TopicPrefix = options.TopicPrefix;
        QualityOfService = options.QualityOfService;
        Retain = options.Retain;
        RepublishSeconds = checked((int)seconds);
    }

    /// <summary>Creates a configuration snapshot for the core TwinCAT publisher.</summary>
    /// <returns>The publication configuration.</returns>
    internal TwinCatStructureOptions BuildOptions() => new()
    {
        AmsNetId = AmsNetId,
        AdsPort = AdsPort,
        PlcVariable = PlcVariable,
        TopicPrefix = TopicPrefix,
        QualityOfService = QualityOfService,
        Retain = Retain,
        RepublishInterval = RepublishSeconds > 0 ? TimeSpan.FromSeconds(RepublishSeconds) : null,
    };
#endif
}
