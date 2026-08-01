// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides reflection type names for the active lean or Reactive production graph.</summary>
internal static class TestTypeNames
{
#if REACTIVE_SHIM
    /// <summary>The internal observable factory type name.</summary>
    internal const string CreateObservable = "MQTTnet.Rx.Client.Reactive.CreateObservable";

    /// <summary>The open generic Mitsubishi tag-write observer type name.</summary>
    internal const string MitsubishiTagWriteObserver =
        "MQTTnet.Rx.Mitsubishi.Reactive.MitsubishiTagWriteObserver`1";

    /// <summary>The internal resilient application-message builder type name.</summary>
    internal const string ResilientApplicationMessageBuilder =
        "MQTTnet.Rx.Client.Reactive.ResilientClient.Internal.ResilientMqttApplicationMessageBuilder";

    /// <summary>The internal resilient client type name.</summary>
    internal const string ResilientClient =
        "MQTTnet.Rx.Client.Reactive.ResilientClient.Internal.ResilientMqttClient";

    /// <summary>The internal resilient storage-manager type name.</summary>
    internal const string ResilientClientStorageManager =
        "MQTTnet.Rx.Client.Reactive.ResilientClient.Internal.ResilientMqttClientStorageManager";

    /// <summary>The internal subscription-results type name.</summary>
    internal const string SendSubscriptionResults =
        "MQTTnet.Rx.Client.Reactive.ResilientClient.Internal.SendSubscriptionResults";
#else
    /// <summary>The internal observable factory type name.</summary>
    internal const string CreateObservable = "MQTTnet.Rx.Client.CreateObservable";

    /// <summary>The open generic Mitsubishi tag-write observer type name.</summary>
    internal const string MitsubishiTagWriteObserver = "MQTTnet.Rx.Mitsubishi.MitsubishiTagWriteObserver`1";

    /// <summary>The internal resilient application-message builder type name.</summary>
    internal const string ResilientApplicationMessageBuilder =
        "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttApplicationMessageBuilder";

    /// <summary>The internal resilient client type name.</summary>
    internal const string ResilientClient = "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClient";

    /// <summary>The internal resilient storage-manager type name.</summary>
    internal const string ResilientClientStorageManager =
        "MQTTnet.Rx.Client.ResilientClient.Internal.ResilientMqttClientStorageManager";

    /// <summary>The internal subscription-results type name.</summary>
    internal const string SendSubscriptionResults =
        "MQTTnet.Rx.Client.ResilientClient.Internal.SendSubscriptionResults";
#endif
}
