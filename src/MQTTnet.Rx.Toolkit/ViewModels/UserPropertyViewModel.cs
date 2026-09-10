// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Captures one editable MQTT user property row.</summary>
internal sealed partial class UserPropertyViewModel : ViewModelBase
{
    /// <summary>Stores the MQTT user property name.</summary>
    [Reactive]
    private string _name = string.Empty;

    /// <summary>Stores the MQTT user property value.</summary>
    [Reactive]
    private string _value = string.Empty;

    /// <summary>Gets a value indicating whether this row can be added to MQTT options.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name);
}
