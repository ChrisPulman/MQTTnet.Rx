// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.OmronPlc.Reactive;
#else
namespace MQTTnet.Rx.OmronPlc;
#endif

/// <summary>Provides MQTT bridges for Omron logical-tag bulk operations.</summary>
public static class OmronPlcBulkMqttExtensions
{
    /// <summary>Provides Omron logical-tag bulk bridges for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            params string[] tagNames) =>
            client.PublishOmronLogicalTags(topic, logicalTags, FormatLogicalTagValue, tagNames);

        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames)
        {
            ValidatePublish(client, topic, logicalTags, payloadFormatter, tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new OmronLogicalTagBulkWriteObserver(logicalTags, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new OmronLogicalTagBulkWriteObserver(logicalTags, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides Omron logical-tag bulk bridges for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            params string[] tagNames)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ValidateNames(tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, FormatLogicalTagValue(value))));
        }

        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames)
        {
            ValidatePublish(client, topic, logicalTags, payloadFormatter, tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new OmronLogicalTagBulkWriteObserver(logicalTags, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new OmronLogicalTagBulkWriteObserver(logicalTags, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides Omron logical-tag bulk bridges for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishOmronLogicalTags(topic, logicalTags, tagNames));

        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronLogicalTags(topic, logicalTags, payloadFormatter, tagNames));

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeOmronLogicalTags(topic, logicalTags, payloadParser, cancellationToken);

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeOmronLogicalTags(topic, logicalTags, payloadParser, onError, cancellationToken);
    }

    /// <summary>Provides Omron logical-tag bulk bridges for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishOmronLogicalTags(topic, logicalTags, tagNames));

        /// <summary>Publishes observed Omron logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishOmronLogicalTags(topic, logicalTags, payloadFormatter, tagNames));

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeOmronLogicalTags(topic, logicalTags, payloadParser, cancellationToken);

        /// <summary>Writes MQTT payloads to multiple Omron logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Omron logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeOmronLogicalTags(
            string topic,
            OmronLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeOmronLogicalTags(topic, logicalTags, payloadParser, onError, cancellationToken);
    }

    /// <summary>Formats one logical tag value.</summary>
    /// <param name="value">The logical tag value.</param>
    /// <returns>The formatted value.</returns>
    private static string FormatLogicalTagValue(LogicalTagValue value) =>
        Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Validates logical publish arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadFormatter">The payload formatter.</param>
    /// <param name="tagNames">The logical tag names.</param>
    private static void ValidatePublish<TClient>(
        IObservable<TClient> client,
        string topic,
        OmronLogicalTagClient logicalTags,
        Func<LogicalTagValue, string> payloadFormatter,
        string[] tagNames)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ArgumentNullException.ThrowIfNull(payloadFormatter);
        ValidateNames(tagNames);
    }

    /// <summary>Validates logical subscribe arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The payload parser.</param>
    private static void ValidateSubscribe<TClient>(
        IObservable<TClient> client,
        string topic,
        OmronLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ArgumentNullException.ThrowIfNull(payloadParser);
    }

    /// <summary>Validates a list of logical tag names.</summary>
    /// <param name="tagNames">The tag names to validate.</param>
    private static void ValidateNames(string[] tagNames)
    {
        ArgumentNullException.ThrowIfNull(tagNames);
        if (tagNames.Length == 0)
        {
            throw new ArgumentException("At least one logical tag name is required.", nameof(tagNames));
        }
    }

    /// <summary>Serializes MQTT payloads into ordered Omron logical-tag writes.</summary>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    private sealed class OmronLogicalTagBulkWriteObserver(
        OmronLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
        : IObserver<MqttApplicationMessageReceivedEventArgs>, IDisposable
    {
        /// <summary>Synchronizes subscription lifetime and queued writes.</summary>
        private readonly Lock _gate = new();

        /// <summary>Cancels queued writes when the bridge is disposed.</summary>
        private readonly CancellationTokenSource _stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        /// <summary>Stores the current ordered write tail.</summary>
        private Task _pendingWrite = Task.CompletedTask;

        /// <summary>Stores the MQTT subscription.</summary>
        private IDisposable? _subscription;

        /// <summary>Stores whether this observer has been disposed.</summary>
        private bool _disposed;

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
            try
            {
                NotifyError(error);
            }
            finally
            {
                Dispose();
            }
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

        /// <summary>Attaches the MQTT subscription.</summary>
        /// <param name="subscription">The MQTT subscription.</param>
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

        /// <summary>Runs one write after the previous queued write completes.</summary>
        /// <param name="previous">The previous queued write.</param>
        /// <param name="payload">The MQTT payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the queued write.</returns>
        private async Task WriteAfterAsync(Task previous, string payload, CancellationToken cancellationToken)
        {
            try
            {
                await previous.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var values = payloadParser(payload);
                ArgumentNullException.ThrowIfNull(values);
                _ = await logicalTags.WriteManyAsync(values, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                NotifyError(error);
            }
        }

        /// <summary>Invokes the error callback without re-entering write-failure handling.</summary>
        /// <param name="error">The error to report.</param>
        private void NotifyError(Exception error)
        {
            try
            {
                onError?.Invoke(error);
            }
            catch (Exception callbackError)
            {
                System.Diagnostics.Trace.TraceError(callbackError.ToString());
            }
        }
    }
}
