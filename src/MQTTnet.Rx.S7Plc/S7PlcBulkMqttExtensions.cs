// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.S7Plc.Reactive;
#else
namespace MQTTnet.Rx.S7Plc;
#endif

/// <summary>Provides MQTT bridges for S7 batch and logical-tag bulk operations.</summary>
public static class S7PlcBulkMqttExtensions
{
    /// <summary>Formats one value using invariant culture.</summary>
    private static readonly System.Text.CompositeFormat LogicalValueFormat = System.Text.CompositeFormat.Parse("{0}");

    /// <summary>Provides S7 batch bridge operations for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            params string[] variables) =>
            client.PublishS7PlcTags(topic, plc, typeWitness, SerializeValues, variables);

        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="payloadFormatter">Formats each batch into an MQTT payload.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            Func<IReadOnlyDictionary<string, T?>, string> payloadFormatter,
            params string[] variables)
        {
            ValidateBatch(client, topic, plc, payloadFormatter, variables);
            return client.PublishMessage(
                ObserveBatch(plc, typeWitness, variables)
                    .Select(values => (topic, payloadFormatter(values))));
        }

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateBatchSubscribe(client, topic, plc, payloadParser);
            var observer = new S7BatchWriteObserver<T>(plc, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateBatchSubscribe(client, topic, plc, payloadParser);
            var observer = new S7BatchWriteObserver<T>(plc, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            params string[] tagNames) =>
            client.PublishS7LogicalTags(topic, logicalTags, FormatLogicalTagValue, tagNames);

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames)
        {
            ValidateLogical(client, topic, logicalTags, payloadFormatter, tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new S7LogicalTagBulkWriteObserver(logicalTags, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new S7LogicalTagBulkWriteObserver(logicalTags, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides S7 batch bridge operations for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            params string[] variables) =>
            client.PublishS7PlcTags(topic, plc, typeWitness, SerializeValues, variables);

        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="payloadFormatter">Formats each batch into an MQTT payload.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            Func<IReadOnlyDictionary<string, T?>, string> payloadFormatter,
            params string[] variables)
        {
            ValidateBatch(client, topic, plc, payloadFormatter, variables);
            return client.PublishMessage(
                ObserveBatch(plc, typeWitness, variables)
                    .Select(values => (topic, payloadFormatter(values))));
        }

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateBatchSubscribe(client, topic, plc, payloadParser);
            var observer = new S7BatchWriteObserver<T>(plc, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateBatchSubscribe(client, topic, plc, payloadParser);
            var observer = new S7BatchWriteObserver<T>(plc, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            params string[] tagNames)
        {
            ValidateLogical(client, topic, logicalTags, tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, FormatLogicalTagValue(value))));
        }

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames)
        {
            ValidateLogical(client, topic, logicalTags, payloadFormatter, tagNames);
            return client.PublishMessage(
                logicalTags.ObserveMany(tagNames).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new S7LogicalTagBulkWriteObserver(logicalTags, payloadParser, null, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new S7LogicalTagBulkWriteObserver(logicalTags, payloadParser, onError, cancellationToken);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides S7 batch bridge operations for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7PlcTags(topic, plc, typeWitness, variables));

        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="payloadFormatter">Formats each batch into an MQTT payload.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            Func<IReadOnlyDictionary<string, T?>, string> payloadFormatter,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7PlcTags(topic, plc, typeWitness, payloadFormatter, variables));

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7PlcTags(topic, plc, payloadParser, cancellationToken);

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7PlcTags(topic, plc, payloadParser, onError, cancellationToken);

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishS7LogicalTags(topic, logicalTags, tagNames));

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7LogicalTags(topic, logicalTags, payloadFormatter, tagNames));

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7LogicalTags(topic, logicalTags, payloadParser, cancellationToken);

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7LogicalTags(topic, logicalTags, payloadParser, onError, cancellationToken);
    }

    /// <summary>Provides S7 batch bridge operations for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7PlcTags(topic, plc, typeWitness, variables));

        /// <summary>Publishes observed S7 batch values as one MQTT payload.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic that receives the batch payload.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
        /// <param name="payloadFormatter">Formats each batch into an MQTT payload.</param>
        /// <param name="variables">The variables to observe as a batch.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            T typeWitness,
            Func<IReadOnlyDictionary<string, T?>, string> payloadFormatter,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7PlcTags(topic, plc, typeWitness, payloadFormatter, variables));

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7PlcTags(topic, plc, payloadParser, cancellationToken);

        /// <summary>Writes MQTT batch payloads to S7 variables.</summary>
        /// <typeparam name="T">The PLC value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The S7 PLC connection.</param>
        /// <param name="payloadParser">Parses an MQTT payload into tag values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeS7PlcTags<T>(
            string topic,
            IRxS7 plc,
            Func<string, IReadOnlyDictionary<string, T>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7PlcTags(topic, plc, payloadParser, onError, cancellationToken);

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishS7LogicalTags(topic, logicalTags, tagNames));

        /// <summary>Publishes observed S7 logical-tag values for many tags.</summary>
        /// <param name="topic">The MQTT topic that receives logical tag payloads.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadFormatter">Formats each logical tag value.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<LogicalTagValue, string> payloadFormatter,
            params string[] tagNames) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishS7LogicalTags(topic, logicalTags, payloadFormatter, tagNames));

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7LogicalTags(topic, logicalTags, payloadParser, cancellationToken);

        /// <summary>Writes MQTT payloads to multiple S7 logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The S7 logical-tag client.</param>
        /// <param name="payloadParser">Parses an MQTT payload into logical tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <param name="cancellationToken">The cancellation token for queued writes.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeS7LogicalTags(
            string topic,
            S7LogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError,
            CancellationToken cancellationToken) =>
            client.ToObservable().SubscribeS7LogicalTags(topic, logicalTags, payloadParser, onError, cancellationToken);
    }

    /// <summary>Serializes batch values to JSON.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values to serialize.</param>
    /// <returns>The JSON payload.</returns>
    private static string SerializeValues<T>(IReadOnlyDictionary<string, T?> values) =>
        System.Text.Json.JsonSerializer.Serialize(values);

    /// <summary>Formats one logical tag value.</summary>
    /// <param name="value">The logical tag value.</param>
    /// <returns>The formatted value.</returns>
    private static string FormatLogicalTagValue(LogicalTagValue value) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, LogicalValueFormat, value.Value);

    /// <summary>Observes a batch through the S7 advanced extension namespace.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="typeWitness">A value used to infer <typeparamref name="T"/>.</param>
    /// <param name="variables">The variables to observe.</param>
    /// <returns>The observed batch values.</returns>
    private static IObservable<IReadOnlyDictionary<string, T?>> ObserveBatch<T>(
        IRxS7 plc,
        T typeWitness,
        string[] variables)
    {
#if REACTIVE_SHIM
        return IoT.Driver.S7PlcRx.Reactive.Advanced.AdvancedExtensions.ObserveBatch(plc, typeWitness, variables);
#else
        return IoT.Driver.S7PlcRx.Advanced.AdvancedExtensions.ObserveBatch(plc, typeWitness, variables);
#endif
    }

    /// <summary>Validates batch publish arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <typeparam name="TValue">The PLC value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="payloadFormatter">The payload formatter.</param>
    /// <param name="variables">The variables to observe.</param>
    private static void ValidateBatch<TClient, TValue>(
        IObservable<TClient> client,
        string topic,
        IRxS7 plc,
        Func<IReadOnlyDictionary<string, TValue?>, string> payloadFormatter,
        string[] variables)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFormatter);
        ValidateNames(variables, nameof(variables));
    }

    /// <summary>Validates batch subscribe arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <typeparam name="TValue">The PLC value type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="payloadParser">The payload parser.</param>
    private static void ValidateBatchSubscribe<TClient, TValue>(
        IObservable<TClient> client,
        string topic,
        IRxS7 plc,
        Func<string, IReadOnlyDictionary<string, TValue>> payloadParser)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadParser);
    }

    /// <summary>Validates logical publish arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="tagNames">The logical tag names.</param>
    private static void ValidateLogical<TClient>(
        IObservable<TClient> client,
        string topic,
        S7LogicalTagClient logicalTags,
        string[] tagNames)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ValidateNames(tagNames, nameof(tagNames));
    }

    /// <summary>Validates logical publish arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadFormatter">The payload formatter.</param>
    /// <param name="tagNames">The logical tag names.</param>
    private static void ValidateLogical<TClient>(
        IObservable<TClient> client,
        string topic,
        S7LogicalTagClient logicalTags,
        Func<LogicalTagValue, string> payloadFormatter,
        string[] tagNames)
    {
        ValidateLogical(client, topic, logicalTags, tagNames);
        ArgumentNullException.ThrowIfNull(payloadFormatter);
    }

    /// <summary>Validates logical subscribe arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The payload parser.</param>
    private static void ValidateLogicalSubscribe<TClient>(
        IObservable<TClient> client,
        string topic,
        S7LogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ArgumentNullException.ThrowIfNull(payloadParser);
    }

    /// <summary>Validates a list of names.</summary>
    /// <param name="names">The names to validate.</param>
    /// <param name="parameterName">The parameter name.</param>
    private static void ValidateNames(string[] names, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length == 0)
        {
            throw new ArgumentException("At least one name is required.", parameterName);
        }
    }

    /// <summary>Serializes MQTT payloads into ordered S7 batch writes.</summary>
    /// <typeparam name="T">The PLC value type.</typeparam>
    /// <param name="plc">The S7 PLC connection.</param>
    /// <param name="payloadParser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    internal sealed class S7BatchWriteObserver<T>(
        IRxS7 plc,
        Func<string, IReadOnlyDictionary<string, T>> payloadParser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
        : OrderedWriteObserver<IReadOnlyDictionary<string, T>>(onError, cancellationToken)
    {
        /// <inheritdoc/>
        internal override IReadOnlyDictionary<string, T> Parse(string payload)
        {
            var values = payloadParser(payload);
            ArgumentNullException.ThrowIfNull(values);
            return values;
        }

        /// <inheritdoc/>
        internal override Task WriteAsync(
            IReadOnlyDictionary<string, T> value,
            CancellationToken cancellationToken)
        {
#if REACTIVE_SHIM
            return IoT.Driver.S7PlcRx.Reactive.Advanced.AsyncExtensions.WriteValuesAsync(
                plc,
                value,
                cancellationToken).AsTask();
#else
            return IoT.Driver.S7PlcRx.Advanced.AsyncExtensions.WriteValuesAsync(
                plc,
                value,
                cancellationToken).AsTask();
#endif
        }
    }

    /// <summary>Serializes MQTT payloads into ordered S7 logical-tag writes.</summary>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    internal sealed class S7LogicalTagBulkWriteObserver(
        S7LogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
        Action<Exception>? onError,
        CancellationToken cancellationToken)
        : OrderedWriteObserver<IReadOnlyCollection<LogicalTagValue>>(onError, cancellationToken)
    {
        /// <inheritdoc/>
        internal override IReadOnlyCollection<LogicalTagValue> Parse(string payload)
        {
            var values = payloadParser(payload);
            ArgumentNullException.ThrowIfNull(values);
            return values;
        }

        /// <inheritdoc/>
        internal override Task WriteAsync(
            IReadOnlyCollection<LogicalTagValue> value,
            CancellationToken cancellationToken) =>
            logicalTags.WriteManyAsync(value, cancellationToken);
    }

    /// <summary>Serializes MQTT writes without blocking the receive callback thread.</summary>
    /// <typeparam name="T">The parsed write value type.</typeparam>
    /// <param name="onError">The optional error callback.</param>
    /// <param name="cancellationToken">The write cancellation token.</param>
    internal abstract class OrderedWriteObserver<T>(Action<Exception>? onError, CancellationToken cancellationToken)
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
            Dispose(true);
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

        /// <summary>Parses one MQTT payload.</summary>
        /// <param name="payload">The MQTT payload.</param>
        /// <returns>The parsed value.</returns>
        internal abstract T Parse(string payload);

        /// <summary>Writes one parsed value to the driver.</summary>
        /// <param name="value">The parsed value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the write operation.</returns>
        internal abstract Task WriteAsync(T value, CancellationToken cancellationToken);

        /// <summary>Releases the observer resources.</summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            _ = disposing;
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
                await WriteAsync(Parse(payload), cancellationToken).ConfigureAwait(false);
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
