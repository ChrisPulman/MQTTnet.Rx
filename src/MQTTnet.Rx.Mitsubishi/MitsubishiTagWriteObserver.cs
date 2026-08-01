// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Mitsubishi.Reactive;
#else
namespace MQTTnet.Rx.Mitsubishi;
#endif

/// <summary>Serializes MQTT payloads into ordered Mitsubishi logical-tag writes.</summary>
/// <typeparam name="T">The logical-tag value type.</typeparam>
internal sealed class MitsubishiTagWriteObserver<T> : IObserver<MqttApplicationMessageReceivedEventArgs>, IDisposable
{
    /// <summary>Synchronizes subscription lifetime and write-queue updates.</summary>
    private readonly Lock _gate = new();

    /// <summary>Stores the typed tag to write.</summary>
    private readonly LogicalTagKey<T> _tag;

    /// <summary>Stores the logical-tag client.</summary>
    private readonly MitsubishiLogicalTagClient _logicalTags;

    /// <summary>Stores the MQTT payload parser.</summary>
    private readonly Func<string, T> _payloadParser;

    /// <summary>Stores the optional error callback.</summary>
    private readonly Action<Exception>? _onError;

    /// <summary>Cancels queued and in-flight writes during teardown.</summary>
    private readonly CancellationTokenSource _stopping;

    /// <summary>Stores the tail of the serialized write queue.</summary>
    private Task _pendingWrite = Task.CompletedTask;

    /// <summary>Stores the upstream MQTT subscription.</summary>
    private IDisposable? _subscription;

    /// <summary>Stores whether this observer has been disposed.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="MitsubishiTagWriteObserver{T}"/> class.</summary>
    /// <param name="tag">The typed tag to write.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The MQTT payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The external cancellation token.</param>
    internal MitsubishiTagWriteObserver(
        LogicalTagKey<T> tag,
        MitsubishiLogicalTagClient logicalTags,
        Func<string, T> payloadParser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
    {
        _tag = tag;
        _logicalTags = logicalTags;
        _payloadParser = payloadParser;
        _onError = onError;
        _stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IDisposable? subscription;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            subscription = _subscription;
            _subscription = null;
        }

        subscription?.Dispose();
        _stopping.Cancel();
        _stopping.Dispose();
    }

    /// <inheritdoc/>
    public void OnCompleted() => Dispose();

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _onError?.Invoke(error);
        Dispose();
    }

    /// <inheritdoc/>
    public void OnNext(MqttApplicationMessageReceivedEventArgs value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var payload = value.ApplicationMessage.ConvertPayloadToString();
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _pendingWrite = WriteAfterAsync(_pendingWrite, payload, _stopping.Token);
        }
    }

    /// <summary>Attaches the upstream MQTT subscription.</summary>
    /// <param name="subscription">The upstream subscription.</param>
    internal void Attach(IDisposable subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        lock (_gate)
        {
            if (_disposed)
            {
                subscription.Dispose();
                return;
            }

            _subscription = subscription;
        }
    }

    /// <summary>Writes one value after the previous queued write finishes.</summary>
    /// <param name="previous">The previous queued write.</param>
    /// <param name="payload">The MQTT payload.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    /// <returns>A task representing the queued write.</returns>
    private async Task WriteAfterAsync(Task previous, string payload, CancellationToken cancellationToken)
    {
        try
        {
            await previous.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var value = _payloadParser(payload);
            var result = await _logicalTags
                .WriteAsync(_tag.Name, value, cancellationToken)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                _onError?.Invoke(new InvalidOperationException(result.Error));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            _onError?.Invoke(error);
        }
    }
}
