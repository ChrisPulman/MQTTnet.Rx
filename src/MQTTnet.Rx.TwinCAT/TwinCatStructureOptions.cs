// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MQTTnet.Protocol;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Configures automatic TwinCAT structure publication to MQTT topics.</summary>
public class TwinCatStructureOptions
{
    /// <summary>Gets or sets the AMS Net ID used by owned ADS client factories.</summary>
    public string AmsNetId { get; set; } = string.Empty;

    /// <summary>Gets or sets the ADS runtime port used by owned ADS client factories.</summary>
    public int AdsPort { get; set; } = 851;

    /// <summary>Gets or sets the ADS symbol path of the PLC structure.</summary>
    public string PlcVariable { get; set; } = string.Empty;

    /// <summary>Gets or sets the MQTT topic prefix used for generated member topics.</summary>
    public string TopicPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the publish quality of service.</summary>
    public MqttQualityOfServiceLevel QualityOfService { get; set; } = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Gets or sets a value indicating whether published member values should be retained.</summary>
    public bool Retain { get; set; } = true;

    /// <summary>Gets or sets the optional interval used to republish the current structure snapshot.</summary>
    public TimeSpan? RepublishInterval { get; set; }

    /// <summary>Gets or sets a predicate that chooses which structure members are published.</summary>
    [JsonIgnore]
    public Func<string, bool>? MemberFilter { get; set; }

    /// <summary>Gets or sets the generated MQTT topic factory.</summary>
    [JsonIgnore]
    public Func<string, string, string>? TopicFactory { get; set; }

    /// <summary>Gets or sets the payload formatter used for generated messages.</summary>
    [JsonIgnore]
    public Func<TwinCatStructureValue, string>? PayloadFormatter { get; set; }

    /// <summary>Gets or sets a callback that receives asynchronous ADS setup errors.</summary>
    [JsonIgnore]
    public Action<Exception>? ErrorHandler { get; set; }

    /// <summary>Gets or sets a callback invoked after the TwinCAT structure table links.</summary>
    [JsonIgnore]
    public Action<HashTableRx>? StructureLinked { get; set; }
}
