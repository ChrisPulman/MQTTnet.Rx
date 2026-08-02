// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive;
#else
namespace MQTTnet.Rx.Client;
#endif

/// <summary>Provides information about a failed resilient-client operation.</summary>
/// <remarks>This event argument is typically used to notify subscribers about failures in a resilient process,
/// such as a background service or connection, along with information about which subscriptions were added or removed
/// as a result of the failure.</remarks>
public class ResilientProcessFailedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="ResilientProcessFailedEventArgs"/> class.</summary>
    /// <param name="exception">The exception that caused the process to fail. Cannot be null.</param>
    /// <param name="addedSubscriptions">A list of topic filters representing subscriptions that were added before the
    /// failure occurred. Can be null if
    /// no subscriptions were added.</param>
    /// <param name="removedSubscriptions">A list of topic strings representing subscriptions that were removed before
    /// the failure occurred. Can be null if
    /// no subscriptions were removed.</param>
    /// <exception cref="ArgumentNullException">Thrown if exception is null.</exception>
    public ResilientProcessFailedEventArgs(
        Exception exception,
        List<MqttTopicFilter>? addedSubscriptions,
        List<string>? removedSubscriptions)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));

        if (addedSubscriptions is not null)
        {
            AddedSubscriptions = [];
            foreach (var addedSubscription in addedSubscriptions)
            {
                AddedSubscriptions.Add(addedSubscription.Topic);
            }
        }
        else
        {
            AddedSubscriptions = [];
        }

        if (removedSubscriptions is not null)
        {
            RemovedSubscriptions = [.. removedSubscriptions];
        }
        else
        {
            RemovedSubscriptions = [];
        }
    }

    /// <summary>Gets the exception that caused the current operation to fail.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the list of subscription identifiers that have been added.</summary>
    public List<string> AddedSubscriptions { get; }

    /// <summary>Gets the list of subscription identifiers that have been removed.</summary>
    public List<string> RemovedSubscriptions { get; }
}
