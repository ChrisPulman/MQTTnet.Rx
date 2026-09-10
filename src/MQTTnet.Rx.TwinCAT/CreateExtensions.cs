// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive.Signals;
#else
using ReactiveUI.Primitives.Signals;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Provides MQTT helpers for publishing and subscribing to TwinCAT PLC variables.</summary>
/// <remarks>
/// All helpers bridge an MQTT client stream and an already configured TwinCAT connection or hash table.
/// </remarks>
public static class CreateExtensions
{
    /// <summary>The error raised when TwinCAT emits a null value.</summary>
    private const string NullObservedValueMessage = "The observed TwinCAT value cannot be null.";

    /// <summary>The default quality assigned to logical tag values written from MQTT payloads.</summary>
    private const string DefaultLogicalTagQuality = "Good";

    /// <summary>Options used to preserve TwinCAT structured values, including public-field ADS structs.</summary>
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        IncludeFields = true,
    };

    /// <summary>Provides MQTT helpers for standard MQTT clients.</summary>
    /// <param name="client">The observable sequence of MQTT clients.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Reads a TwinCAT PLC variable and publishes the read response to an MQTT topic when subscribed.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, null)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a TwinCAT PLC variable by correlation ID and publishes the read response to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            string id,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, id)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a TwinCAT PLC array or string variable and publishes the read response to an MQTT topic when subscribed.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            int arrayLength,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, arrayLength, null)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a TwinCAT PLC array or string variable by correlation ID and publishes the read response.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            int arrayLength,
            string id,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, arrayLength, id)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT PLC variable to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                TwinCatRxExtensions.Observe(plc, plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT hash-table value to an MQTT topic.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT structure table member to an MQTT topic.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="memberName">The structure table member name to observe.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);

            return client.PublishMessage(
                structure.Observe(memberName, ConvertObservedValue<T>)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT PLC variable.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT PLC variable with a correlation identifier.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="id">The write correlation identifier.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            string id,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())),
                        id)));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT hash-table member.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="memberName">The hash-table member to update.</param>
        /// <param name="structure">The configured hash table or TwinCAT structure table.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a member value.</param>
        /// <returns>A disposable that ends the MQTT-to-hash subscription.</returns>
        public IDisposable SubscribeTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => structure[memberName] =
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through TwinCAT structure clone/write semantics.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="memberName">The structure member to update.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a member value.</param>
        /// <returns>A disposable that ends the MQTT-to-structure subscription.</returns>
        public IDisposable SubscribeTcStructWrite<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => WriteStructureValue(
                        structure,
                        memberName,
                        payloadFactory,
                        message.ApplicationMessage.ConvertPayloadToString())));
        }

        /// <summary>Publishes observed TwinCAT logical-tag values to an MQTT topic.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag value into MQTT payload text.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcLogicalTags(
            string topic,
            IReadOnlyCollection<string> tagNames,
            TwinCatLogicalTagClient tags,
            Func<LogicalTagValue, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                tags.ObserveMany(tagNames)
                    .Select(value => (topic, Payload: payloadFormatter(value))));
        }

        /// <summary>Reads TwinCAT logical tags and publishes each read result to an MQTT topic when subscribed.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to read.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag operation result into MQTT payload text.</param>
        /// <returns>A sequence of MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcLogicalTagReads(
            string topic,
            IReadOnlyCollection<string> tagNames,
            TwinCatLogicalTagClient tags,
            Func<TagOperationResult<LogicalTagValue>, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                Signal.FromAsync(cancellationToken => ReadLogicalTagsAsync(tags, tagNames, cancellationToken))
                    .SelectMany(CreateLogicalTagResultEnumerator())
                    .Select(result => (topic, Payload: payloadFormatter(result))));
        }

        /// <summary>Subscribes to an MQTT topic and writes parsed values to a TwinCAT logical tag.</summary>
        /// <typeparam name="T">The logical tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tagName">The logical tag to update.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a logical tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeTcLogicalTag<T>(
            string topic,
            string tagName,
            TwinCatLogicalTagClient tags,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagName);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return CreateExtensions.SubscribeTcLogicalTags(
                client,
                topic,
                tags,
                payload => new[]
                {
                    new LogicalTagValue(
                        tagName,
                        RequireWriteValue(payloadFactory(payload)),
                        TimeProvider.System.GetUtcNow(),
                        DefaultLogicalTagQuality),
                });
        }

        /// <summary>Subscribes to an MQTT topic and writes parsed values to one or more TwinCAT logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into logical tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeTcLogicalTags(
            string topic,
            TwinCatLogicalTagClient tags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic)
                .SelectMany(
                    message => Signal.FromAsync(
                        cancellationToken => WriteLogicalTagsAsync(
                            tags,
                            payloadFactory(message.ApplicationMessage.ConvertPayloadToString()),
                            cancellationToken)))
                .Subscribe(Witness.Create<IReadOnlyList<TagOperationResult<LogicalTagValue>>>(static _ => { }));
        }
    }

    /// <summary>Provides MQTT helpers for resilient MQTT clients.</summary>
    /// <param name="client">The observable sequence of resilient MQTT clients.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Reads a TwinCAT PLC variable and publishes the read response through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, null)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a correlated TwinCAT PLC variable and publishes the read response through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            string id,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, id)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a TwinCAT PLC array or string variable and publishes the read response through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            int arrayLength,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, arrayLength, null)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Reads a correlated TwinCAT PLC array or string variable through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            int arrayLength,
            string id,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                ReadOnce<T>(plc, plcVariable, arrayLength, id)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT PLC variable through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                TwinCatRxExtensions.Observe(plc, plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT hash-table value through a resilient MQTT client.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.PublishMessage(
                plc.Observe(plcVariable, ConvertObservedValue<T>)
                    .Select(
                        payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Publishes a TwinCAT structure table member through a resilient MQTT client.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="memberName">The structure table member name to observe.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);

            return client.PublishMessage(
                structure.Observe(memberName, ConvertObservedValue<T>)
                    .Select(payload => (topic, Payload: ConvertPayloadToString(payload))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through a TwinCAT connection.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through a TwinCAT connection with a correlation ID.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plcVariable">The PLC variable to update.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="id">The write correlation identifier.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a PLC variable value.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeTcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            string id,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);
            ArgumentNullException.ThrowIfNull(id);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => plc.Write(
                        plcVariable,
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString())),
                        id)));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values to a TwinCAT hash-table member.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="memberName">The hash-table member to update.</param>
        /// <param name="structure">The configured hash table or TwinCAT structure table.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a member value.</param>
        /// <returns>A disposable that ends the MQTT-to-hash subscription.</returns>
        public IDisposable SubscribeTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => structure[memberName] =
                        RequireWriteValue(payloadFactory(message.ApplicationMessage.ConvertPayloadToString()))));
        }

        /// <summary>Subscribes to an MQTT topic and writes received values through TwinCAT structure clone/write semantics.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="memberName">The structure member to update.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a member value.</param>
        /// <returns>A disposable that ends the MQTT-to-structure subscription.</returns>
        public IDisposable SubscribeTcStructWrite<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic).Subscribe(
                Witness.Create<MqttApplicationMessageReceivedEventArgs>(
                    message => WriteStructureValue(
                        structure,
                        memberName,
                        payloadFactory,
                        message.ApplicationMessage.ConvertPayloadToString())));
        }

        /// <summary>Publishes observed TwinCAT logical-tag values through a resilient MQTT client.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag value into MQTT payload text.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcLogicalTags(
            string topic,
            IReadOnlyCollection<string> tagNames,
            TwinCatLogicalTagClient tags,
            Func<LogicalTagValue, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                tags.ObserveMany(tagNames)
                    .Select(value => (topic, Payload: payloadFormatter(value))));
        }

        /// <summary>Reads TwinCAT logical tags and publishes each read result through a resilient MQTT client.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to read.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag operation result into MQTT payload text.</param>
        /// <returns>A sequence of resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcLogicalTagReads(
            string topic,
            IReadOnlyCollection<string> tagNames,
            TwinCatLogicalTagClient tags,
            Func<TagOperationResult<LogicalTagValue>, string> payloadFormatter)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagNames);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFormatter);

            return client.PublishMessage(
                Signal.FromAsync(cancellationToken => ReadLogicalTagsAsync(tags, tagNames, cancellationToken))
                    .SelectMany(CreateLogicalTagResultEnumerator())
                    .Select(result => (topic, Payload: payloadFormatter(result))));
        }

        /// <summary>Subscribes to an MQTT topic and writes parsed values to one or more TwinCAT logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into logical tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeTcLogicalTags(
            string topic,
            TwinCatLogicalTagClient tags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return client.SubscribeToTopic(topic)
                .SelectMany(
                    message => Signal.FromAsync(
                        cancellationToken => WriteLogicalTagsAsync(
                            tags,
                            payloadFactory(message.ApplicationMessage.ConvertPayloadToString()),
                            cancellationToken)))
                .Subscribe(Witness.Create<IReadOnlyList<TagOperationResult<LogicalTagValue>>>(static _ => { }));
        }

        /// <summary>Subscribes to an MQTT topic and writes parsed values to a TwinCAT logical tag.</summary>
        /// <typeparam name="T">The logical tag value type.</typeparam>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="tagName">The logical tag to update.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFactory">Converts an MQTT payload into a logical tag value.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeTcLogicalTag<T>(
            string topic,
            string tagName,
            TwinCatLogicalTagClient tags,
            Func<string, T> payloadFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(tagName);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(payloadFactory);

            return CreateExtensions.SubscribeTcLogicalTags(
                client,
                topic,
                tags,
                payload => new[]
                {
                    new LogicalTagValue(
                        tagName,
                        RequireWriteValue(payloadFactory(payload)),
                        TimeProvider.System.GetUtcNow(),
                        DefaultLogicalTagQuality),
                });
        }
    }

    /// <summary>Creates an observable that performs one scalar ADS read for each subscription.</summary>
    /// <typeparam name="T">The expected read value type.</typeparam>
    /// <param name="plc">The configured TwinCAT client.</param>
    /// <param name="plcVariable">The variable to read.</param>
    /// <param name="id">The optional correlation identifier.</param>
    /// <returns>A one-value read response sequence.</returns>
    private static IObservable<T> ReadOnce<T>(IRxTcAdsClient plc, string plcVariable, string? id) =>
        Signal.Create<T>(observer =>
        {
            var subscription = TwinCatRxExtensions
                .Observe(plc, plcVariable, id ?? string.Empty, ConvertObservedValue<T>)
                .Take(1)
                .Subscribe(observer);
            try
            {
                if (id is null)
                {
                    plc.Read(plcVariable);
                }
                else
                {
                    plc.Read(plcVariable, id);
                }
            }
            catch
            {
                subscription.Dispose();
                throw;
            }

            return subscription;
        });

    /// <summary>Creates an observable that performs one array or string ADS read for each subscription.</summary>
    /// <typeparam name="T">The expected read value type.</typeparam>
    /// <param name="plc">The configured TwinCAT client.</param>
    /// <param name="plcVariable">The variable to read.</param>
    /// <param name="arrayLength">The requested array or string length.</param>
    /// <param name="id">The optional correlation identifier.</param>
    /// <returns>A one-value read response sequence.</returns>
    private static IObservable<T> ReadOnce<T>(IRxTcAdsClient plc, string plcVariable, int arrayLength, string? id) =>
        Signal.Create<T>(observer =>
        {
            var subscription = TwinCatRxExtensions
                .Observe(plc, plcVariable, id ?? string.Empty, ConvertObservedValue<T>)
                .Take(1)
                .Subscribe(observer);
            try
            {
                if (id is null)
                {
                    plc.Read(plcVariable, arrayLength);
                }
                else
                {
                    plc.Read(plcVariable, arrayLength, id);
                }
            }
            catch
            {
                subscription.Dispose();
                throw;
            }

            return subscription;
        });

    /// <summary>Reads a batch of logical tags without exposing an optional cancellation token in the bridge API.</summary>
    /// <param name="tags">The configured logical-tag client.</param>
    /// <param name="tagNames">The tag names to read.</param>
    /// <param name="cancellationToken">A token that cancels the read operation.</param>
    /// <returns>The logical tag read results.</returns>
    private static Task<IReadOnlyList<TagOperationResult<LogicalTagValue>>> ReadLogicalTagsAsync(
        TwinCatLogicalTagClient tags,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken) =>
        tags.ReadManyAsync(tagNames, cancellationToken);

    /// <summary>Writes logical tags and raises failed result records as observable errors.</summary>
    /// <param name="tags">The configured logical-tag client.</param>
    /// <param name="values">The logical-tag values to write.</param>
    /// <param name="cancellationToken">A token that cancels the write operation.</param>
    /// <returns>The logical-tag write results.</returns>
    private static async Task<IReadOnlyList<TagOperationResult<LogicalTagValue>>> WriteLogicalTagsAsync(
        TwinCatLogicalTagClient tags,
        IReadOnlyCollection<LogicalTagValue> values,
        CancellationToken cancellationToken)
    {
        var results = await tags.WriteManyAsync(values, cancellationToken).ConfigureAwait(false);
        foreach (var result in results)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Error);
            }
        }

        return results;
    }

    /// <summary>Creates the logical tag result enumerator used by read publishers.</summary>
    /// <returns>The logical tag result enumerator.</returns>
    private static Func<
        IReadOnlyList<TagOperationResult<LogicalTagValue>>,
        IObservable<TagOperationResult<LogicalTagValue>>> CreateLogicalTagResultEnumerator() =>
        new(EnumerateLogicalTagResults);

    /// <summary>Converts a logical-tag result batch into individual observable results.</summary>
    /// <param name="results">The batch returned by the logical-tag client.</param>
    /// <returns>The individual logical-tag operation results.</returns>
    private static IObservable<TagOperationResult<LogicalTagValue>> EnumerateLogicalTagResults(
        IReadOnlyList<TagOperationResult<LogicalTagValue>> results) =>
        Signal.FromEnumerable(results);

    /// <summary>Converts a non-null TwinCAT value to the requested type.</summary>
    /// <typeparam name="T">The requested TwinCAT value type.</typeparam>
    /// <param name="value">The untyped TwinCAT value.</param>
    /// <returns>The typed TwinCAT value.</returns>
    private static T ConvertObservedValue<T>(object? value)
    {
        if (value is null)
        {
            throw new InvalidOperationException(NullObservedValueMessage);
        }

        return (T)value;
    }

    /// <summary>Converts a non-null TwinCAT value to its MQTT payload text.</summary>
    /// <typeparam name="T">The TwinCAT value type.</typeparam>
    /// <param name="value">The typed TwinCAT value.</param>
    /// <returns>The MQTT payload text.</returns>
    private static string ConvertPayloadToString<T>(T value)
    {
        if (value is null)
        {
            throw new InvalidOperationException(NullObservedValueMessage);
        }

        return value switch
        {
            string text => text,
            bool boolean => boolean.ToString(),
            char character => character.ToString(),
            IFormattable formattable when value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal =>
                formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => JsonSerializer.Serialize(value, PayloadSerializerOptions),
        };
    }

    /// <summary>Writes one structure value while retaining the underlying TwinCAT bulk write integration point.</summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="structure">The structure table to update.</param>
    /// <param name="memberName">The structure member name.</param>
    /// <param name="payloadFactory">The payload parser.</param>
    /// <param name="payload">The inbound MQTT payload.</param>
    private static void WriteStructureValue<T>(
        HashTableRx structure,
        string memberName,
        Func<string, T> payloadFactory,
        string payload)
    {
        var value = RequireWriteValue(payloadFactory(payload));
        if (!TwinCatRxExtensions.WriteValues(structure, values => values[memberName] = value))
        {
            structure[memberName] = value;
        }
    }

    /// <summary>Ensures a payload factory produced a value that can be written to TwinCAT.</summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="value">The payload factory result.</param>
    /// <returns>The non-null value to write.</returns>
    private static object RequireWriteValue<T>(T value) =>
        value is not null
            ? value
            : throw new InvalidOperationException("The converted TwinCAT value cannot be null.");
}
