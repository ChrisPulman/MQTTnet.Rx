// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.ABPlc.Reactive;
#else
namespace MQTTnet.Rx.ABPlc;
#endif

/// <summary>Provides MQTT bridges for Allen-Bradley bulk and logical-tag operations.</summary>
public static class ABPlcBulkMqttExtensions
{
    /// <summary>Provides bulk Allen-Bradley bridges for standard MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            params string[] variables)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(plc);
            ValidateVariables(variables);
            return client.PublishMessage(plc.ObserveMany(variables).Select(values => (topic, SerializeValues(values))));
        }

        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFormatter">Formats the observed variable/value map.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<IReadOnlyDictionary<string, object?>, string> payloadFormatter,
            params string[] variables)
        {
            ValidateBulk(client, topic, plc, payloadFormatter, variables);
            return client.PublishMessage(plc.ObserveMany(variables).Select(values => (topic, payloadFormatter(values))));
        }

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser) =>
            client.SubscribeABPlcTags(topic, plc, payloadParser, null);

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser,
            Action<Exception>? onError)
        {
            ValidateSubscribe(client, topic, plc, payloadParser);
            var observer = new ABPlcBulkWriteObserver(plc, payloadParser, onError);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName)
        {
            var publishResults = client.PublishABLogicalTag(topic, logicalTags, tagName, FormatLogicalTagValue);
            return publishResults;
        }

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <param name="payloadFormatter">Formats each logical-tag value as an MQTT payload.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName,
            Func<LogicalTagValue, string> payloadFormatter)
        {
            ValidateLogical(client, topic, logicalTags, tagName, payloadFormatter);
            return client.PublishMessage(logicalTags.Observe(tagName).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser) =>
            client.SubscribeABLogicalTags(topic, logicalTags, payloadParser, null);

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new ABLogicalTagBulkWriteObserver(logicalTags, payloadParser, onError);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides bulk Allen-Bradley bridges for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            params string[] variables)
        {
            var publishResults = client.PublishABPlcTags(topic, plc, SerializeValues, variables);
            return publishResults;
        }

        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFormatter">Formats the observed variable/value map.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<IReadOnlyDictionary<string, object?>, string> payloadFormatter,
            params string[] variables)
        {
            ValidateBulk(client, topic, plc, payloadFormatter, variables);
            return client.PublishMessage(plc.ObserveMany(variables).Select(values => (topic, payloadFormatter(values))));
        }

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser) =>
            client.SubscribeABPlcTags(topic, plc, payloadParser, null);

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser,
            Action<Exception>? onError)
        {
            ValidateSubscribe(client, topic, plc, payloadParser);
            var observer = new ABPlcBulkWriteObserver(plc, payloadParser, onError);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentNullException.ThrowIfNull(logicalTags);
            ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
            return client.PublishMessage(logicalTags.Observe(tagName).Select(value => (topic, FormatLogicalTagValue(value))));
        }

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <param name="payloadFormatter">Formats each logical-tag value as an MQTT payload.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName,
            Func<LogicalTagValue, string> payloadFormatter)
        {
            ValidateLogical(client, topic, logicalTags, tagName, payloadFormatter);
            return client.PublishMessage(logicalTags.Observe(tagName).Select(value => (topic, payloadFormatter(value))));
        }

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser) =>
            client.SubscribeABLogicalTags(topic, logicalTags, payloadParser, null);

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError)
        {
            ValidateLogicalSubscribe(client, topic, logicalTags, payloadParser);
            var observer = new ABLogicalTagBulkWriteObserver(logicalTags, payloadParser, onError);
            observer.Attach(client.SubscribeToTopic(topic).Subscribe(observer));
            return observer;
        }
    }

    /// <summary>Provides bulk Allen-Bradley bridges for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The asynchronous MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishABPlcTags(topic, plc, variables));

        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFormatter">Formats the observed variable/value map.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<IReadOnlyDictionary<string, object?>, string> payloadFormatter,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishABPlcTags(topic, plc, payloadFormatter, variables));

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser) =>
            client.ToObservable().SubscribeABPlcTags(topic, plc, payloadParser);

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser,
            Action<Exception>? onError) =>
            client.ToObservable().SubscribeABPlcTags(topic, plc, payloadParser, onError);

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishABLogicalTag(topic, logicalTags, tagName));

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <param name="payloadFormatter">Formats each logical-tag value as an MQTT payload.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName,
            Func<LogicalTagValue, string> payloadFormatter) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishABLogicalTag(topic, logicalTags, tagName, payloadFormatter));

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser) =>
            client.ToObservable().SubscribeABLogicalTags(topic, logicalTags, payloadParser);

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError) =>
            client.ToObservable().SubscribeABLogicalTags(topic, logicalTags, payloadParser, onError);
    }

    /// <summary>Provides bulk Allen-Bradley bridges for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The asynchronous resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishABPlcTags(topic, plc, variables));

        /// <summary>Publishes observed Allen-Bradley values for multiple variables as one MQTT payload.</summary>
        /// <param name="topic">The MQTT topic that receives the bulk payload.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadFormatter">Formats the observed variable/value map.</param>
        /// <param name="variables">The PLC variables to observe in one bulk read.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<IReadOnlyDictionary<string, object?>, string> payloadFormatter,
            params string[] variables) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishABPlcTags(topic, plc, payloadFormatter, variables));

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser) =>
            client.ToObservable().SubscribeABPlcTags(topic, plc, payloadParser);

        /// <summary>Writes a bulk MQTT payload to multiple Allen-Bradley PLC variables.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="plc">The configured PLC connection.</param>
        /// <param name="payloadParser">Converts an MQTT payload into variable values.</param>
        /// <param name="onError">A callback for payload conversion or PLC write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-PLC subscription.</returns>
        public IDisposable SubscribeABPlcTags(
            string topic,
            IABPlcRx plc,
            Func<string, IReadOnlyDictionary<string, object?>> payloadParser,
            Action<Exception>? onError) =>
            client.ToObservable().SubscribeABPlcTags(topic, plc, payloadParser, onError);

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName) =>
            ObservableSignalConversion.ToSignal(client.ToObservable().PublishABLogicalTag(topic, logicalTags, tagName));

        /// <summary>Publishes observed logical-tag values through the Allen-Bradley logical tag client.</summary>
        /// <param name="topic">The MQTT topic that receives logical-tag values.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="tagName">The registered logical tag name.</param>
        /// <param name="payloadFormatter">Formats each logical-tag value as an MQTT payload.</param>
        /// <returns>The resilient MQTT publish results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABLogicalTag(
            string topic,
            ABLogicalTagClient logicalTags,
            string tagName,
            Func<LogicalTagValue, string> payloadFormatter) =>
            ObservableSignalConversion.ToSignal(
                client.ToObservable().PublishABLogicalTag(topic, logicalTags, tagName, payloadFormatter));

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser) =>
            client.ToObservable().SubscribeABLogicalTags(topic, logicalTags, payloadParser);

        /// <summary>Writes MQTT payloads to registered Allen-Bradley logical tags.</summary>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="logicalTags">The Allen-Bradley logical-tag client.</param>
        /// <param name="payloadParser">Converts MQTT payloads into logical-tag values.</param>
        /// <param name="onError">A callback for payload conversion or logical-tag write failures.</param>
        /// <returns>A disposable that ends the MQTT-to-logical-tag subscription.</returns>
        public IDisposable SubscribeABLogicalTags(
            string topic,
            ABLogicalTagClient logicalTags,
            Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
            Action<Exception>? onError) =>
            client.ToObservable().SubscribeABLogicalTags(topic, logicalTags, payloadParser, onError);
    }

    /// <summary>Converts a dictionary payload to JSON.</summary>
    /// <param name="values">The PLC values.</param>
    /// <returns>The JSON payload.</returns>
    private static string SerializeValues(IReadOnlyDictionary<string, object?> values) =>
        System.Text.Json.JsonSerializer.Serialize(values);

    /// <summary>Formats a logical-tag value.</summary>
    /// <param name="value">The logical-tag value.</param>
    /// <returns>The formatted logical-tag value.</returns>
    private static string FormatLogicalTagValue(LogicalTagValue value)
    {
        return string.Concat(Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Validates bulk publish arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="plc">The PLC connection.</param>
    /// <param name="payloadFormatter">The payload formatter.</param>
    /// <param name="variables">The PLC variables.</param>
    private static void ValidateBulk<TClient>(
        IObservable<TClient> client,
        string topic,
        IABPlcRx plc,
        Func<IReadOnlyDictionary<string, object?>, string> payloadFormatter,
        string[] variables)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(plc);
        ArgumentNullException.ThrowIfNull(payloadFormatter);
        ValidateVariables(variables);
    }

    /// <summary>Validates that at least one PLC variable has been supplied.</summary>
    /// <param name="variables">The PLC variables to publish.</param>
    private static void ValidateVariables(string[] variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        if (variables.Length == 0)
        {
            throw new ArgumentException("At least one PLC variable is required.", nameof(variables));
        }
    }

    /// <summary>Validates bulk subscribe arguments.</summary>
    /// <typeparam name="TClient">The MQTT client type.</typeparam>
    /// <param name="client">The MQTT client sequence.</param>
    /// <param name="topic">The MQTT topic.</param>
    /// <param name="plc">The PLC connection.</param>
    /// <param name="payloadParser">The payload parser.</param>
    private static void ValidateSubscribe<TClient>(
        IObservable<TClient> client,
        string topic,
        IABPlcRx plc,
        Func<string, IReadOnlyDictionary<string, object?>> payloadParser)
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
    /// <param name="tagName">The logical tag name.</param>
    /// <param name="payloadFormatter">The payload formatter.</param>
    private static void ValidateLogical<TClient>(
        IObservable<TClient> client,
        string topic,
        ABLogicalTagClient logicalTags,
        string tagName,
        Func<LogicalTagValue, string> payloadFormatter)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
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
        ABLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(logicalTags);
        ArgumentNullException.ThrowIfNull(payloadParser);
    }

    /// <summary>Serializes MQTT payloads into ordered Allen-Bradley bulk writes.</summary>
    /// <param name="plc">The PLC connection.</param>
    /// <param name="payloadParser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    internal sealed class ABPlcBulkWriteObserver(
        IABPlcRx plc,
        Func<string, IReadOnlyDictionary<string, object?>> payloadParser,
        Action<Exception>? onError) :
        OrderedWriteObserver<IReadOnlyDictionary<string, object?>>(onError)
    {
        /// <inheritdoc/>
        protected override IReadOnlyDictionary<string, object?> Parse(string payload)
        {
            var values = payloadParser(payload);
            ArgumentNullException.ThrowIfNull(values);
            return values;
        }

        /// <inheritdoc/>
        protected override async Task WriteAsync(
            IReadOnlyDictionary<string, object?> value,
            CancellationToken cancellationToken)
        {
            _ = await plc.WriteManyAsync(value, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Serializes MQTT payloads into ordered Allen-Bradley logical-tag writes.</summary>
    /// <param name="logicalTags">The logical-tag client.</param>
    /// <param name="payloadParser">The payload parser.</param>
    /// <param name="onError">The optional error callback.</param>
    internal sealed class ABLogicalTagBulkWriteObserver(
        ABLogicalTagClient logicalTags,
        Func<string, IReadOnlyCollection<LogicalTagValue>> payloadParser,
        Action<Exception>? onError) :
        OrderedWriteObserver<IReadOnlyCollection<LogicalTagValue>>(onError)
    {
        /// <inheritdoc/>
        protected override IReadOnlyCollection<LogicalTagValue> Parse(string payload)
        {
            var values = payloadParser(payload);
            ArgumentNullException.ThrowIfNull(values);
            return values;
        }

        /// <inheritdoc/>
        protected override async Task WriteAsync(
            IReadOnlyCollection<LogicalTagValue> value,
            CancellationToken cancellationToken) =>
            _ = await logicalTags.WriteManyAsync(value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Serializes MQTT writes without blocking the MQTT callback thread.</summary>
    /// <typeparam name="T">The parsed write value type.</typeparam>
    /// <param name="onError">The optional error callback.</param>
    internal abstract class OrderedWriteObserver<T>(Action<Exception>? onError)
        : IObserver<MqttApplicationMessageReceivedEventArgs>, IDisposable
    {
        /// <summary>Synchronizes subscription lifetime and queued writes.</summary>
        private readonly Lock _gate = new();

        /// <summary>Cancels queued writes when the bridge is disposed.</summary>
        private readonly CancellationTokenSource _stopping = new();

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

        /// <summary>Attaches the MQTT subscription owned by this observer.</summary>
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
        /// <returns>The parsed write value.</returns>
        protected abstract T Parse(string payload);

        /// <summary>Writes one parsed value to the underlying driver.</summary>
        /// <param name="value">The parsed write value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the write operation.</returns>
        protected abstract Task WriteAsync(T value, CancellationToken cancellationToken);

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
