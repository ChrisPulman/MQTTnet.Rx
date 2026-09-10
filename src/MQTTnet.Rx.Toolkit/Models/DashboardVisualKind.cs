// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Toolkit.Models;

/// <summary>Describes how a dashboard tile should render a topic value.</summary>
internal enum DashboardVisualKind
{
    /// <summary>Displays payload content as text.</summary>
    Text,

    /// <summary>Displays boolean payload content as a toggle.</summary>
    Toggle,

    /// <summary>Displays numeric payload content as a gauge.</summary>
    Gauge,

    /// <summary>Displays JSON payload content in a structured text panel.</summary>
    Json,

    /// <summary>Displays binary payload content as encoded bytes.</summary>
    Binary,
}
