// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Internal;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Manages the persistent storage and retrieval of MQTT application messages for a resilient MQTT client, ensuring
/// reliable message delivery across client restarts or failures.
/// </summary>
/// <remarks>This class coordinates in-memory and persistent storage of outgoing MQTT messages to support reliable
/// delivery in scenarios where the client may disconnect or restart. It is intended for internal use by resilient MQTT
/// client implementations and is not thread safe unless otherwise synchronized. The class implements <see
/// cref="IDisposable"/> to release resources when no longer needed.</remarks>
/// <param name="storage">The storage provider used to persist and retrieve queued MQTT application messages. Cannot be null.</param>
internal class ResilientMqttClientStorageManager(IResilientMqttClientStorage storage) : IDisposable
{
    private readonly List<ResilientMqttApplicationMessage> _messages = [];
    private readonly AsyncLock _messagesLock = new();

    private readonly IResilientMqttClientStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private bool _disposedValue;

    /// <summary>
    /// Asynchronously loads all queued MQTT application messages from persistent storage and adds them to the in-memory
    /// message queue.
    /// </summary>
    /// <remarks>Subsequent calls to this method will return all messages currently in the in-memory queue,
    /// including any previously loaded messages that have not been removed. This method is thread-safe if the
    /// underlying storage and message queue are thread-safe.</remarks>
    /// <returns>A list of <see cref="ResilientMqttApplicationMessage"/> objects representing all messages currently queued,
    /// including those loaded from storage. The list will be empty if there are no queued messages.</returns>
    public async Task<List<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync()
    {
        var loadedMessages = await _storage.LoadQueuedMessagesAsync().ConfigureAwait(false);
        _messages.AddRange(loadedMessages);

        return _messages;
    }

    /// <summary>
    /// Asynchronously adds the specified application message to the collection and persists the updated state.
    /// </summary>
    /// <param name="applicationMessage">The application message to add. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    public async Task AddAsync(ResilientMqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        using (await _messagesLock.EnterAsync().ConfigureAwait(false))
        {
            _messages.Add(applicationMessage);
            await SaveAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously removes the specified application message from the collection, if it exists.
    /// </summary>
    /// <param name="applicationMessage">The application message to remove from the collection. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    public async Task RemoveAsync(ResilientMqttApplicationMessage applicationMessage)
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

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>Call this method when you are finished using the object to release unmanaged resources and
    /// perform other cleanup operations. After calling Dispose, the object should not be used further. This method
    /// suppresses finalization for the object.</remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _messagesLock.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    private Task SaveAsync() => _storage.SaveQueuedMessagesAsync(_messages);
}
