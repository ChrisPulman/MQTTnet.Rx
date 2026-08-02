// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Internal;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Client.Reactive.ResilientClient.Internal;
#else
namespace MQTTnet.Rx.Client.ResilientClient.Internal;
#endif

/// <summary>Coordinates in-memory and persistent queued-message storage.</summary>
/// <remarks>This class coordinates in-memory and persistent storage of outgoing MQTT messages to support reliable
/// delivery in scenarios where the client may disconnect or restart. It is intended for internal use by resilient MQTT
/// client implementations and is not thread safe unless otherwise synchronized. The class implements <see
/// cref="IDisposable"/> to release resources when no longer needed.</remarks>
/// <param name="storage">The storage provider used to persist and retrieve queued MQTT application messages. Cannot be
/// null.</param>
internal class ResilientMqttClientStorageManager(IResilientMqttClientStorage storage) : IDisposable
{
    /// <summary>Stores the queued application messages.</summary>
    private readonly List<ResilientMqttApplicationMessage> _messages = [];

    /// <summary>Synchronizes access to the queued application messages.</summary>
    private readonly AsyncLock _messagesLock = new();

    /// <summary>Stores the persistence provider for queued application messages.</summary>
    private readonly IResilientMqttClientStorage _storage =
        storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposedValue;

    /// <summary>Releases all resources used by the current instance of the class.</summary>
    /// <remarks>Call this method when you are finished using the object to release unmanaged resources and
    /// perform other cleanup operations. After calling Dispose, the object should not be used further. This method
    /// suppresses finalization for the object.</remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Loads the messages queued for delivery.</summary>
    /// <remarks>Subsequent calls return all currently queued in-memory messages, including loaded messages.</remarks>
    /// <returns>A list of <see cref="ResilientMqttApplicationMessage"/> objects representing all messages currently
    /// queued, including those loaded from storage. The list is empty when no messages are queued.</returns>
    internal async Task<List<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync()
    {
        var loadedMessages = await _storage.LoadQueuedMessagesAsync().ConfigureAwait(false);
        _messages.AddRange(loadedMessages);

        return _messages;
    }

    /// <summary>Adds a message to the queue and persists it.</summary>
    /// <remarks>The operation is thread-safe when the underlying storage and message queue are thread-safe.</remarks>
    /// <param name="applicationMessage">The application message to add. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    internal async Task AddAsync(ResilientMqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        using (await _messagesLock.EnterAsync().ConfigureAwait(false))
        {
            _messages.Add(applicationMessage);
            await SaveAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Asynchronously removes the specified application message from the collection, if it exists.</summary>
    /// <param name="applicationMessage">The application message to remove from the collection. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    internal async Task RemoveAsync(ResilientMqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        using (await _messagesLock.EnterAsync().ConfigureAwait(false))
        {
            var index = _messages.IndexOf(applicationMessage);
            if (index == -1)
            {
                return;
            }

            _messages.RemoveAt(index);
            await SaveAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
    /// only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _messagesLock.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        _disposedValue = true;
    }

    /// <summary>Saves the queued application messages to persistent storage.</summary>
    /// <returns>A task representing the save operation.</returns>
    private Task SaveAsync() => _storage.SaveQueuedMessagesAsync(_messages);
}
