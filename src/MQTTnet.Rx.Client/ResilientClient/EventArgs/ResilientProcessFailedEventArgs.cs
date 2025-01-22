// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Packets;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Resilient Process Failed EventArgs.
/// </summary>
/// <seealso cref="EventArgs" />
public class ResilientProcessFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientProcessFailedEventArgs"/> class.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="addedSubscriptions">The added subscriptions.</param>
    /// <param name="removedSubscriptions">The removed subscriptions.</param>
    /// <exception cref="ArgumentNullException">exception.</exception>
    public ResilientProcessFailedEventArgs(Exception exception, List<MqttTopicFilter>? addedSubscriptions, List<string>? removedSubscriptions)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));

        if (addedSubscriptions != null)
        {
            AddedSubscriptions = new List<string>(addedSubscriptions.Select(item => item.Topic));
        }
        else
        {
            AddedSubscriptions = [];
        }

        if (removedSubscriptions != null)
        {
            RemovedSubscriptions = new List<string>(removedSubscriptions);
        }
        else
        {
            RemovedSubscriptions = [];
        }
    }

    /// <summary>
    /// Gets the exception.
    /// </summary>
    /// <value>
    /// The exception.
    /// </value>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the added subscriptions.
    /// </summary>
    /// <value>
    /// The added subscriptions.
    /// </value>
    public List<string> AddedSubscriptions { get; }

    /// <summary>
    /// Gets the removed subscriptions.
    /// </summary>
    /// <value>
    /// The removed subscriptions.
    /// </value>
    public List<string> RemovedSubscriptions { get; }
}
