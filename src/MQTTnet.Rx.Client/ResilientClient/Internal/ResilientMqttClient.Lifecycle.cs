// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>Contains lifecycle helpers of the resilient MQTT client.</summary>
internal sealed partial class ResilientMqttClient
{
    /// <summary>Manages persistent queued-message storage.</summary>
    private ResilientMqttClientStorageManager? _storageManager;

    /// <summary>Indicates whether the most recent disconnect was intentional.</summary>
    private bool _isCleanDisconnect;

    /// <summary>Releases unmanaged and, optionally, managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release managed resources; otherwise, <c>false</c>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopPublishing();
            StopMaintainingConnection();

            if (_maintainConnectionTask is not null)
            {
                _maintainConnectionTask.GetAwaiter().GetResult();
                _maintainConnectionTask = null;
            }

            _messageQueue.Dispose();
            _messageQueueLock.Dispose();
            InternalClient.ApplicationMessageReceivedAsync -= HandleApplicationMessageReceivedAsync;
            InternalClient.ConnectedAsync -= HandleConnectedAsync;
            InternalClient.DisconnectedAsync -= HandleDisconnectedAsync;
            InternalClient.Dispose();
            _subscriptionsQueuedSignal.Dispose();
            _storageManager?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Gets the duration from now until the specified UTC time.</summary>
    /// <param name="endTime">The end time, expressed as UTC.</param>
    /// <returns>The remaining duration, or zero when the time has passed.</returns>
    private static TimeSpan GetRemainingTime(in DateTime endTime)
    {
        var remainingTime = endTime - TimeProvider.System.GetUtcNow().UtcDateTime;
        return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
    }
}
