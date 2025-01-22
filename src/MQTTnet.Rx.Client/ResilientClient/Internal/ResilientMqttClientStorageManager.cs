// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Internal;

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Resilient Mqtt Client Storage Manager.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ResilientMqttClientStorageManager"/> class.
/// </remarks>
/// <param name="storage">The storage.</param>
/// <exception cref="ArgumentNullException">storage.</exception>
internal class ResilientMqttClientStorageManager(IResilientMqttClientStorage storage) : IDisposable
{
    private readonly List<ResilientMqttApplicationMessage> _messages = [];
    private readonly AsyncLock _messagesLock = new();

    private readonly IResilientMqttClientStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private bool _disposedValue;

    /// <summary>
    /// Loads the queued messages asynchronous.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<List<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync()
    {
        var loadedMessages = await _storage.LoadQueuedMessagesAsync().ConfigureAwait(false);
        _messages.AddRange(loadedMessages);

        return _messages;
    }

    /// <summary>
    /// Adds the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <exception cref="ArgumentNullException">applicationMessage.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
    /// Removes the asynchronous.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <exception cref="ArgumentNullException">applicationMessage.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
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
