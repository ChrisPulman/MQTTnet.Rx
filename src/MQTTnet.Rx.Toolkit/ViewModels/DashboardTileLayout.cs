// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Toolkit.Models;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Stores persisted dashboard tile layout and visual settings.</summary>
/// <param name="Topic">The MQTT topic represented by the tile.</param>
/// <param name="VisualKind">The selected visual kind for the tile.</param>
/// <param name="AutoVisual">A value indicating whether incoming payloads can update the visual kind.</param>
/// <param name="Unit">The unit label displayed by the tile.</param>
/// <param name="Minimum">The minimum value used by numeric visuals.</param>
/// <param name="Maximum">The maximum value used by numeric visuals.</param>
/// <param name="Retain">A value indicating whether tile publishes should set MQTT retain.</param>
/// <param name="PreserveEditor">A value indicating whether incoming payloads should preserve editor text.</param>
/// <param name="QualityOfService">The quality of service used for tile publishes.</param>
internal sealed record DashboardTileLayout(
    string Topic,
    DashboardVisualKind VisualKind,
    bool AutoVisual,
    string Unit,
    double Minimum,
    double Maximum,
    bool Retain,
    bool PreserveEditor,
    MqttQualityOfServiceLevel QualityOfService);
