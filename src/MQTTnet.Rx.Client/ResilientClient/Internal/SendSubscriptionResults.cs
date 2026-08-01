// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>Represents the results of subscribe and unsubscribe operations for an MQTT client.</summary>
/// <param name="subscribeResults">A list of results for each subscribe operation performed. Cannot be null.</param>
/// <param name="unsubscribeResults">A list of results for each unsubscribe operation performed. Cannot be null.</param>
internal sealed class SendSubscriptionResults(
    List<MqttClientSubscribeResult> subscribeResults,
    List<MqttClientUnsubscribeResult> unsubscribeResults)
{
    /// <summary>Gets the results of each topic subscription attempt made by the client.</summary>
    internal List<MqttClientSubscribeResult> SubscribeResults { get; } =
        subscribeResults ?? throw new ArgumentNullException(nameof(subscribeResults));

    /// <summary>Gets the results of each unsubscribe operation performed by the client.</summary>
    internal List<MqttClientUnsubscribeResult> UnsubscribeResults { get; } =
        unsubscribeResults ?? throw new ArgumentNullException(nameof(unsubscribeResults));
}
