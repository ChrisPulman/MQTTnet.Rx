// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>Contains queued-message operations of the resilient MQTT client.</summary>
internal sealed partial class ResilientMqttClient
{
    /// <summary>Enqueues an MQTT application message for asynchronous publishing by the managed client.</summary>
    /// <remarks>The message is added to the internal queue and will be published according to the client's
    /// configured delivery and retry policies. This method does not guarantee immediate delivery.</remarks>
    /// <param name="applicationMessage">The MQTT application message to enqueue. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous enqueue operation.</returns>
    public async Task EnqueueAsync(MqttApplicationMessage applicationMessage)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(applicationMessage);

        var managedMqttApplicationMessage =
            new ResilientMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage);
        await EnqueueAsync(managedMqttApplicationMessage.Build()).ConfigureAwait(false);
    }

    /// <summary>Enqueues a resilient application message for asynchronous publication.</summary>
    /// <remarks>If the internal message queue has reached its maximum capacity, the behavior depends on the
    /// configured overflow strategy: the new message may be dropped, or the oldest queued message may be removed to
    /// make space. If a message is skipped or removed due to overflow, the ApplicationMessageSkipped event is raised.
    /// This method is thread-safe and can be called concurrently.</remarks>
    /// <param name="applicationMessage">The application message to enqueue for publishing. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous enqueue operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the client has not been started. Call StartAsync before
    /// publishing messages.</exception>
    public async Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(applicationMessage);

        if (Options is null)
        {
            throw new InvalidOperationException("call StartAsync before publishing messages");
        }

        MqttTopicValidator.ThrowIfInvalid(applicationMessage.ApplicationMessage);

        ResilientMqttApplicationMessage? removedMessage = null;
        ApplicationMessageSkippedEventArgs? applicationMessageSkippedEventArgs = null;

        try
        {
            using (await _messageQueueLock.EnterAsync().ConfigureAwait(false))
            {
                if (_messageQueue.Count >= Options.MaxPendingMessages)
                {
                    if (
                        Options.PendingMessagesOverflowStrategy
                        == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                    {
                        _logger.Verbose(
                            "Skipping publish of new application message because internal queue is full.");
                        applicationMessageSkippedEventArgs = new(applicationMessage);
                        return;
                    }

                    if (
                        Options.PendingMessagesOverflowStrategy
                        == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                    {
                        removedMessage = _messageQueue.RemoveFirst();
                        _logger.Verbose(
                            "Removed oldest application message from internal queue because it is full.");
                        applicationMessageSkippedEventArgs = new(removedMessage);
                    }
                }

                _messageQueue.Enqueue(applicationMessage);

                if (_storageManager is not null)
                {
                    if (removedMessage is not null)
                    {
                        await _storageManager.RemoveAsync(removedMessage).ConfigureAwait(false);
                    }

                    await _storageManager.AddAsync(applicationMessage).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (applicationMessageSkippedEventArgs is not null)
            {
                await PublishApplicationMessageSkippedAsync(applicationMessageSkippedEventArgs)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>Publishes one queued message and raises processing notifications.</summary>
    /// <param name="message">The queued MQTT application message to be published.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel publishing.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    private async Task TryPublishQueuedMessageAsync(
        ResilientMqttApplicationMessage message,
        CancellationToken cancellationToken)
    {
        Exception? transmitException = null;
        try
        {
            var acceptPublish = await ShouldPublishAsync(message).ConfigureAwait(false);
            if (acceptPublish)
            {
                using var publishTimeout = NewTimeoutToken(cancellationToken);
                await InternalClient
                    .PublishAsync(message.ApplicationMessage, publishTimeout.Token)
                    .ConfigureAwait(false);
            }

            await RemoveQueuedMessageAsync(message).ConfigureAwait(false);
        }
        catch (MqttCommunicationException exception)
        {
            transmitException = exception;
            _logger.Warning(exception, "Publishing application message ({0}) failed.", message.Id);

            if (message.ApplicationMessage?.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                await RemoveQueuedMessageAsync(message).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            transmitException = exception;
            _logger.Error(
                exception,
                "Error while publishing application message ({0}).",
                message.Id);
        }
        finally
        {
            await PublishApplicationMessageProcessedAsync(message, transmitException).ConfigureAwait(false);
        }
    }

    /// <summary>Determines whether a queued message should be published.</summary>
    /// <param name="message">The queued message being evaluated.</param>
    /// <returns>A task that returns <c>true</c> when publishing is accepted.</returns>
    private async Task<bool> ShouldPublishAsync(ResilientMqttApplicationMessage message)
    {
        if (!_interceptingPublishMessageEvent.HasHandlers)
        {
            return true;
        }

        var interceptEventArgs = new InterceptingPublishMessageEventArgs(message);
        await _interceptingPublishMessageEvent.InvokeAsync(interceptEventArgs).ConfigureAwait(false);
        return interceptEventArgs.AcceptPublish;
    }

    /// <summary>Removes a message from the in-memory and persistent queues.</summary>
    /// <param name="message">The message to remove.</param>
    /// <returns>A task that represents the asynchronous removal operation.</returns>
    private async Task RemoveQueuedMessageAsync(ResilientMqttApplicationMessage message)
    {
        using (await _messageQueueLock.EnterAsync(CancellationToken.None).ConfigureAwait(false))
        {
            _messageQueue.RemoveFirst(item => item.Id.Equals(message.Id));

            if (_storageManager is not null)
            {
                await _storageManager.RemoveAsync(message).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Raises completion notifications for a queued-message publication attempt.</summary>
    /// <param name="message">The message that was processed.</param>
    /// <param name="transmitException">The exception raised while publishing, if any.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task PublishApplicationMessageProcessedAsync(
        ResilientMqttApplicationMessage message,
        Exception? transmitException)
    {
        var eventArgs = new ApplicationMessageProcessedEventArgs(message, transmitException);
        ApplicationMessageProcessedEvent?.Invoke(this, eventArgs);
        if (_applicationMessageProcessedEvent.HasHandlers)
        {
            await _applicationMessageProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }
    }

    /// <summary>Raises skip notifications for a queued application message.</summary>
    /// <param name="eventArgs">The details of the skipped message.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    private async Task PublishApplicationMessageSkippedAsync(ApplicationMessageSkippedEventArgs eventArgs)
    {
        ApplicationMessageSkippedEvent?.Invoke(this, eventArgs);
        if (_applicationMessageSkippedEvent.HasHandlers)
        {
            await _applicationMessageSkippedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }
    }
}
