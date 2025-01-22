// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// SendSubscribeUnsubscribeResult.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SendSubscriptionResults"/> class.
/// </remarks>
/// <param name="subscribeResults">The subscribe results.</param>
/// <param name="unsubscribeResults">The unsubscribe results.</param>
/// <exception cref="ArgumentNullException">
/// subscribeResults
/// or
/// unsubscribeResults.
/// </exception>
internal sealed class SendSubscriptionResults(List<MqttClientSubscribeResult> subscribeResults, List<MqttClientUnsubscribeResult> unsubscribeResults)
{
    /// <summary>
    /// Gets the subscribe results.
    /// </summary>
    /// <value>
    /// The subscribe results.
    /// </value>
    public List<MqttClientSubscribeResult> SubscribeResults { get; } = subscribeResults ?? throw new ArgumentNullException(nameof(subscribeResults));

    /// <summary>
    /// Gets the unsubscribe results.
    /// </summary>
    /// <value>
    /// The unsubscribe results.
    /// </value>
    public List<MqttClientUnsubscribeResult> UnsubscribeResults { get; } = unsubscribeResults ?? throw new ArgumentNullException(nameof(unsubscribeResults));
}
