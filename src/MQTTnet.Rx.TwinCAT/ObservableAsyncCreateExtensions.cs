// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Provides asynchronous MQTT helpers for TwinCAT PLC variables.</summary>
public static class ObservableAsyncCreateExtensions
{
    /// <summary>Provides asynchronous MQTT helpers for standard MQTT clients.</summary>
    /// <param name="client">The asynchronous observable sequence of MQTT clients.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Reads a TwinCAT PLC variable and publishes the response to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a correlated TwinCAT PLC variable and publishes the response to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, id, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a TwinCAT PLC array or string variable and publishes the response asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, arrayLength, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a correlated TwinCAT PLC array or string variable and publishes the response asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, arrayLength, id, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT PLC variable to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT hash-table value to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT structure table member to an MQTT topic asynchronously.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="memberName">The structure table member name to observe.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);

            return client.ToObservable().PublishTcStructMember(topic, memberName, structure, typeWitness).ToMqttAsyncSignal();
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, payloadFactory);
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, id, payloadFactory);
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

            return client.ToObservable().SubscribeTcStructMember(topic, memberName, structure, payloadFactory);
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

            return client.ToObservable().SubscribeTcStructWrite(topic, memberName, structure, payloadFactory);
        }

        /// <summary>Publishes observed TwinCAT logical-tag values to an MQTT topic asynchronously.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag value into MQTT payload text.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcLogicalTags(
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

            return client.ToObservable().PublishTcLogicalTags(topic, tagNames, tags, payloadFormatter).ToMqttAsyncSignal();
        }

        /// <summary>Reads TwinCAT logical tags and publishes each read result to an MQTT topic asynchronously.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to read.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag operation result into MQTT payload text.</param>
        /// <returns>An asynchronous sequence of MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcLogicalTagReads(
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

            return client.ToObservable().PublishTcLogicalTagReads(topic, tagNames, tags, payloadFormatter).ToMqttAsyncSignal();
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

            return client.ToObservable().SubscribeTcLogicalTag(topic, tagName, tags, payloadFactory);
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

            return client.ToObservable().SubscribeTcLogicalTags(topic, tags, payloadFactory);
        }

    }

    /// <summary>Provides asynchronous MQTT helpers for resilient MQTT clients.</summary>
    /// <param name="client">The asynchronous observable sequence of resilient MQTT clients.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Reads a TwinCAT PLC variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a correlated TwinCAT PLC variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, id, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a TwinCAT PLC array or string variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, arrayLength, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Reads a correlated TwinCAT PLC array or string variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to read.</param>
        /// <param name="arrayLength">The array or string length to request from ADS.</param>
        /// <param name="id">The operation correlation identifier.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcRead<T>(
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

            return client.ToObservable().PublishTcPlcRead(topic, plcVariable, arrayLength, id, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT PLC variable through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured TwinCAT connection.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IRxTcAdsClient plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT hash-table value through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The PLC variable value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="plcVariable">The PLC variable to observe.</param>
        /// <param name="plc">The configured PLC hash table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(
            string topic,
            string plcVariable,
            IHashTableRx plc,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(plcVariable);
            ArgumentNullException.ThrowIfNull(plc);

            return client.ToObservable().PublishTcPlcTag(topic, plcVariable, plc, typeWitness).ToMqttAsyncSignal();
        }

        /// <summary>Publishes a TwinCAT structure table member through a resilient MQTT client asynchronously.</summary>
        /// <typeparam name="T">The structure member value type.</typeparam>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="memberName">The structure table member name to observe.</param>
        /// <param name="structure">The configured TwinCAT structure table.</param>
        /// <param name="typeWitness">Optional values used only to infer <typeparamref name="T"/>.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcStructMember<T>(
            string topic,
            string memberName,
            HashTableRx structure,
            params T[] typeWitness)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(topic);
            ArgumentNullException.ThrowIfNull(memberName);
            ArgumentNullException.ThrowIfNull(structure);

            return client.ToObservable().PublishTcStructMember(topic, memberName, structure, typeWitness).ToMqttAsyncSignal();
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, payloadFactory);
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

            return client.ToObservable().SubscribeTcTag(topic, plcVariable, plc, id, payloadFactory);
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

            return client.ToObservable().SubscribeTcStructMember(topic, memberName, structure, payloadFactory);
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

            return client.ToObservable().SubscribeTcStructWrite(topic, memberName, structure, payloadFactory);
        }

        /// <summary>Publishes observed TwinCAT logical-tag values through a resilient MQTT client asynchronously.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to observe.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag value into MQTT payload text.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcLogicalTags(
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

            return client.ToObservable().PublishTcLogicalTags(topic, tagNames, tags, payloadFormatter).ToMqttAsyncSignal();
        }

        /// <summary>Reads TwinCAT logical tags and publishes each read result through a resilient MQTT client asynchronously.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="tagNames">The logical tag names to read.</param>
        /// <param name="tags">The configured TwinCAT logical-tag client.</param>
        /// <param name="payloadFormatter">Converts a logical tag operation result into MQTT payload text.</param>
        /// <returns>An asynchronous sequence of resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcLogicalTagReads(
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

            return client.ToObservable().PublishTcLogicalTagReads(topic, tagNames, tags, payloadFormatter).ToMqttAsyncSignal();
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

            return client.ToObservable().SubscribeTcLogicalTag(topic, tagName, tags, payloadFactory);
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

            return client.ToObservable().SubscribeTcLogicalTags(topic, tags, payloadFactory);
        }
    }
}
