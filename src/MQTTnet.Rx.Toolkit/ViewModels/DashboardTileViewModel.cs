// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Represents one editable tile on the dynamic MQTT dashboard.</summary>
internal sealed partial class DashboardTileViewModel : ViewModelBase
{
    /// <summary>Stores the amount added when a gauge expands its maximum range.</summary>
    private const double GaugeMaximumExpansion = 10;

    /// <summary>Stores the callback used to reorder this tile.</summary>
    private readonly Action<DashboardTileViewModel, int> _move;

    /// <summary>Stores the callback used to publish edited tile values.</summary>
    private readonly Func<DashboardTileViewModel, Task> _publishAsync;

    /// <summary>Stores the callback used to remove this tile.</summary>
    private readonly Action<DashboardTileViewModel> _remove;

    /// <summary>Stores the MQTT topic represented by this tile.</summary>
    [Reactive]
    private string _topic;

    /// <summary>Stores the payload text currently displayed by the tile.</summary>
    [Reactive]
    private string _displayValue = string.Empty;

    /// <summary>Stores the editable payload text for publishing from the tile.</summary>
    [Reactive]
    private string _editableValue = string.Empty;

    /// <summary>Stores the unit label displayed by the tile.</summary>
    [Reactive]
    private string _unit = string.Empty;

    /// <summary>Stores the selected visual kind.</summary>
    [Reactive]
    private DashboardVisualKind _visualKind;

    /// <summary>Stores whether incoming payloads can update the selected visual kind.</summary>
    [Reactive]
    private bool _autoVisual = true;

    /// <summary>Stores the last numeric payload value.</summary>
    [Reactive]
    private double _numericValue;

    /// <summary>Stores the minimum bound used by numeric visuals.</summary>
    [Reactive]
    private double _minimum;

    /// <summary>Stores the maximum bound used by numeric visuals.</summary>
    [Reactive]
    private double _maximum = 100;

    /// <summary>Stores the last boolean payload value.</summary>
    [Reactive]
    private bool _booleanValue;

    /// <summary>Stores whether tile publishes should set the MQTT retain flag.</summary>
    [Reactive]
    private bool _retain;

    /// <summary>Stores whether incoming payloads should preserve the editor text.</summary>
    [Reactive]
    private bool _preserveEditor;

    /// <summary>Stores the quality of service used by tile publishes.</summary>
    [Reactive]
    private MqttQualityOfServiceLevel _qualityOfService = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Stores the last timestamp observed for the tile topic.</summary>
    [Reactive]
    private DateTimeOffset? _lastSeen;

    /// <summary>Initializes a new instance of the <see cref="DashboardTileViewModel"/> class.</summary>
    /// <param name="topic">The MQTT topic represented by the tile.</param>
    /// <param name="publishAsync">The callback used to publish edited tile values.</param>
    /// <param name="remove">The callback used to remove this tile.</param>
    /// <param name="move">The callback used to reorder this tile.</param>
    internal DashboardTileViewModel(
        string topic,
        Func<DashboardTileViewModel, Task> publishAsync,
        Action<DashboardTileViewModel> remove,
        Action<DashboardTileViewModel, int> move)
    {
        _topic = topic;
        _publishAsync = publishAsync;
        _remove = remove;
        _move = move;
    }

    /// <summary>Gets the visual kinds available for dashboard tiles.</summary>
    public IReadOnlyList<DashboardVisualKind> VisualKinds { get; } =
        Enum.GetValues<DashboardVisualKind>();

    /// <summary>Gets the quality of service values available for tile publishes.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

    /// <summary>Applies a received MQTT message to the dashboard tile.</summary>
    /// <param name="message">The received MQTT message for this tile.</param>
    internal void Apply(ReceivedMqttMessage message)
    {
        DisplayValue = message.Payload;
        if (!PreserveEditor)
        {
            EditableValue = message.Payload;
        }

        LastSeen = message.Timestamp;
        if (AutoVisual)
        {
            VisualKind = SelectVisual(message);
        }

        if (bool.TryParse(message.Payload, out var parsedBool))
        {
            BooleanValue = parsedBool;
        }

        if (PayloadInspector.TryParseFiniteNumber(message.Payload, out var parsedNumber))
        {
            NumericValue = parsedNumber;
            if (parsedNumber > Maximum)
            {
                Maximum = Math.Ceiling(parsedNumber + GaugeMaximumExpansion);
            }
        }
    }

    /// <summary>Gets persisted layout settings for this tile.</summary>
    /// <returns>The persisted tile layout model.</returns>
    internal DashboardTileLayout ToLayout() =>
        new(Topic, VisualKind, AutoVisual, Unit, Minimum, Maximum, Retain, PreserveEditor, QualityOfService);

    /// <summary>Applies persisted layout settings to this tile.</summary>
    /// <param name="layout">The persisted layout model.</param>
    internal void ApplyLayout(DashboardTileLayout layout)
    {
        VisualKind = layout.VisualKind;
        AutoVisual = layout.AutoVisual;
        Unit = layout.Unit;
        Minimum = layout.Minimum;
        Maximum = layout.Maximum;
        Retain = layout.Retain;
        PreserveEditor = layout.PreserveEditor;
        QualityOfService = layout.QualityOfService;
    }

    /// <summary>Selects a dashboard visual kind from payload content.</summary>
    /// <param name="message">The received MQTT message to inspect.</param>
    /// <returns>The selected dashboard visual kind.</returns>
    private static DashboardVisualKind SelectVisual(ReceivedMqttMessage message)
    {
        if (message.DetectedFormat == PayloadFormat.Json)
        {
            return DashboardVisualKind.Json;
        }

        if (message.DetectedFormat is PayloadFormat.Base64 or PayloadFormat.Hex)
        {
            return DashboardVisualKind.Binary;
        }

        if (bool.TryParse(message.Payload, out _))
        {
            return DashboardVisualKind.Toggle;
        }

        return PayloadInspector.TryParseFiniteNumber(message.Payload, out _)
            ? DashboardVisualKind.Gauge
            : DashboardVisualKind.Text;
    }

    /// <summary>Publishes the editable tile value.</summary>
    /// <returns>A task that completes when the publish callback has completed.</returns>
    [ReactiveCommand]
    private Task PublishTileAsync() => _publishAsync(this);

    /// <summary>Removes this dashboard tile.</summary>
    [ReactiveCommand]
    private void RemoveTile() => _remove(this);

    /// <summary>Moves this dashboard tile one position left.</summary>
    [ReactiveCommand]
    private void MoveLeft() => _move(this, -1);

    /// <summary>Moves this dashboard tile one position right.</summary>
    [ReactiveCommand]
    private void MoveRight() => _move(this, 1);
}
