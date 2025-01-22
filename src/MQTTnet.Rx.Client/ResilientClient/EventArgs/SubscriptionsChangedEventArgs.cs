// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Subscriptions Changed EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionsChangedEventArgs"/> class.
/// </remarks>
/// <param name="subscribeResult">The subscribe result.</param>
/// <param name="unsubscribeResult">The unsubscribe result.</param>
/// <exception cref="ArgumentNullException">
/// subscribeResult
/// or
/// unsubscribeResult.
/// </exception>
public sealed class SubscriptionsChangedEventArgs(List<MqttClientSubscribeResult> subscribeResult, List<MqttClientUnsubscribeResult> unsubscribeResult) : EventArgs
{
    /// <summary>
    /// Gets the subscribe result.
    /// </summary>
    /// <value>
    /// The subscribe result.
    /// </value>
    public List<MqttClientSubscribeResult> SubscribeResult { get; } = subscribeResult ?? throw new ArgumentNullException(nameof(subscribeResult));

    /// <summary>
    /// Gets the unsubscribe result.
    /// </summary>
    /// <value>
    /// The unsubscribe result.
    /// </value>
    public List<MqttClientUnsubscribeResult> UnsubscribeResult { get; } = unsubscribeResult ?? throw new ArgumentNullException(nameof(unsubscribeResult));
}
