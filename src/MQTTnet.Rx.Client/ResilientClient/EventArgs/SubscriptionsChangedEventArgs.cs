// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>Provides the results of a subscription synchronization operation.</summary>
/// <param name="subscribeResult">A list of results for each subscription attempt. Cannot be null.</param>
/// <param name="unsubscribeResult">A list of results for each unsubscription attempt. Cannot be null.</param>
public sealed class SubscriptionsChangedEventArgs(
    List<MqttClientSubscribeResult> subscribeResult,
    List<MqttClientUnsubscribeResult> unsubscribeResult) : EventArgs
{
    /// <summary>Gets the results of each topic subscription attempt made by the client.</summary>
    public List<MqttClientSubscribeResult> SubscribeResult { get; } =
        subscribeResult ?? throw new ArgumentNullException(nameof(subscribeResult));

    /// <summary>Gets the results of the unsubscribe operation for each topic filter.</summary>
    public List<MqttClientUnsubscribeResult> UnsubscribeResult { get; } =
        unsubscribeResult ?? throw new ArgumentNullException(nameof(unsubscribeResult));
}
