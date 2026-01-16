// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Packets;

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides data for the event that is raised when a resilient process fails, including the exception and any changes
/// to subscriptions.
/// </summary>
/// <remarks>This event argument is typically used to notify subscribers about failures in a resilient process,
/// such as a background service or connection, along with information about which subscriptions were added or removed
/// as a result of the failure.</remarks>
public class ResilientProcessFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientProcessFailedEventArgs"/> class with the specified exception and.
    /// subscription changes.
    /// </summary>
    /// <param name="exception">The exception that caused the process to fail. Cannot be null.</param>
    /// <param name="addedSubscriptions">A list of topic filters representing subscriptions that were added before the failure occurred. Can be null if
    /// no subscriptions were added.</param>
    /// <param name="removedSubscriptions">A list of topic strings representing subscriptions that were removed before the failure occurred. Can be null if
    /// no subscriptions were removed.</param>
    /// <exception cref="ArgumentNullException">Thrown if exception is null.</exception>
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
    /// Gets the exception that caused the current operation to fail.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the list of subscription identifiers that have been added.
    /// </summary>
    public List<string> AddedSubscriptions { get; }

    /// <summary>
    /// Gets the list of subscription identifiers that have been removed.
    /// </summary>
    public List<string> RemovedSubscriptions { get; }
}
