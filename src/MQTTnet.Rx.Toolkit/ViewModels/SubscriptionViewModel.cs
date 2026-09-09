// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using MQTTnet.Protocol;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Captures editable MQTT subscription options.</summary>
internal sealed partial class SubscriptionViewModel : ViewModelBase
{
    /// <summary>Stores the MQTT topic filter to subscribe to.</summary>
    [Reactive]
    private string _topicFilter = "#";

    /// <summary>Stores the requested subscription quality of service level.</summary>
    [Reactive]
    private MqttQualityOfServiceLevel _qualityOfService = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Stores whether matching own-published messages should be suppressed.</summary>
    [Reactive]
    private bool _noLocal;

    /// <summary>Stores whether retained messages should preserve their retain flag.</summary>
    [Reactive]
    private bool _retainAsPublished;

    /// <summary>Stores the MQTT retain handling mode.</summary>
    [Reactive]
    private MqttRetainHandling _retainHandling;

    /// <summary>Stores the MQTT 5 subscription identifier.</summary>
    [Reactive]
    private uint _subscriptionIdentifier;

    /// <summary>Gets editable MQTT user properties for the subscribe packet.</summary>
    public ObservableCollection<UserPropertyViewModel> UserProperties { get; } = [];

    /// <summary>Gets the quality of service values available in the UI.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

    /// <summary>Gets the retain handling values available in the UI.</summary>
    public IReadOnlyList<MqttRetainHandling> RetainHandlingOptions { get; } =
        Enum.GetValues<MqttRetainHandling>();
}
