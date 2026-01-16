// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Represents the results of subscribe and unsubscribe operations for an MQTT client.
/// </summary>
/// <param name="subscribeResults">A list of results for each subscribe operation performed. Cannot be null.</param>
/// <param name="unsubscribeResults">A list of results for each unsubscribe operation performed. Cannot be null.</param>
internal sealed class SendSubscriptionResults(List<MqttClientSubscribeResult> subscribeResults, List<MqttClientUnsubscribeResult> unsubscribeResults)
{
    /// <summary>
    /// Gets the results of each topic subscription attempt made by the client.
    /// </summary>
    public List<MqttClientSubscribeResult> SubscribeResults { get; } = subscribeResults ?? throw new ArgumentNullException(nameof(subscribeResults));

    /// <summary>
    /// Gets the results of each unsubscribe operation performed by the client.
    /// </summary>
    public List<MqttClientUnsubscribeResult> UnsubscribeResults { get; } = unsubscribeResults ?? throw new ArgumentNullException(nameof(unsubscribeResults));
}
