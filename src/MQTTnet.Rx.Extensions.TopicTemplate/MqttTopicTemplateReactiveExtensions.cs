// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Extensions.TopicTemplate;
using MQTTnet.Protocol;
using PrimitivesResult = ReactiveUI.Primitives.Result;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Extensions.TopicTemplate.Reactive;
#else
namespace MQTTnet.Rx.Extensions.TopicTemplate;
#endif

/// <summary>Provides ReactiveUI.Primitives wrappers for MQTTnet topic templates.</summary>
public static class MqttTopicTemplateReactiveExtensions
{
    /// <summary>Creates a topic template from an MQTT topic template string.</summary>
    /// <param name="template">The topic template string.</param>
    /// <returns>The created topic template.</returns>
    public static MqttTopicTemplate TopicTemplate(string template) => new(template);

    /// <summary>Provides reactive topic-template MQTT client operations.</summary>
    /// <param name="client">The MQTT client.</param>
    extension(IMqttClient client)
    {
        /// <summary>Subscribes to a topic template as a cold observable operation.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<MqttClientSubscribeResult> SubscribeTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            client.SubscribeTopicTemplate(
                topicTemplate,
                MqttQualityOfServiceLevel.AtMostOnce,
                false,
                false,
                MqttRetainHandling.SendAtSubscribe,
                false);

        /// <summary>Subscribes to a topic template as a cold observable operation.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="noLocal">Whether messages published by this client are excluded.</param>
        /// <param name="retainAsPublished">Whether retained messages keep their original retain flag.</param>
        /// <param name="retainHandling">The retain handling behavior.</param>
        /// <param name="subscribeTreeRoot">Whether to subscribe to the whole topic tree.</param>
        /// <returns>A cold subscribe operation.</returns>
        public IObservable<MqttClientSubscribeResult> SubscribeTopicTemplate(
            MqttTopicTemplate topicTemplate,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool noLocal,
            bool retainAsPublished,
            MqttRetainHandling retainHandling,
            bool subscribeTreeRoot)
        {
            ArgumentNullException.ThrowIfNull(client);
            var options = BuildSubscribeOptions(
                topicTemplate,
                qualityOfServiceLevel,
                noLocal,
                retainAsPublished,
                retainHandling,
                subscribeTreeRoot);
            return Signal.FromAsync(cancellationToken => client.SubscribeAsync(options, cancellationToken));
        }

        /// <summary>Subscribes to a topic template as a cold asynchronous observable operation.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribeTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            client.ObserveSubscribeTopicTemplate(
                topicTemplate,
                MqttQualityOfServiceLevel.AtMostOnce,
                false,
                false,
                MqttRetainHandling.SendAtSubscribe,
                false);

        /// <summary>Subscribes to a topic template as a cold asynchronous observable operation.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
        /// <param name="noLocal">Whether messages published by this client are excluded.</param>
        /// <param name="retainAsPublished">Whether retained messages keep their original retain flag.</param>
        /// <param name="retainHandling">The retain handling behavior.</param>
        /// <param name="subscribeTreeRoot">Whether to subscribe to the whole topic tree.</param>
        /// <returns>A cold asynchronous subscribe operation.</returns>
        public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribeTopicTemplate(
            MqttTopicTemplate topicTemplate,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            bool noLocal,
            bool retainAsPublished,
            MqttRetainHandling retainHandling,
            bool subscribeTreeRoot)
        {
            ArgumentNullException.ThrowIfNull(client);
            var options = BuildSubscribeOptions(
                topicTemplate,
                qualityOfServiceLevel,
                noLocal,
                retainAsPublished,
                retainHandling,
                subscribeTreeRoot);
            return FromAsyncTask(cancellationToken => client.SubscribeAsync(options, cancellationToken));
        }

        /// <summary>Publishes to a concrete topic template as a cold observable operation.</summary>
        /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            client.PublishTopicTemplate(topicTemplate, NoConfigureMessage);

        /// <summary>Publishes to a concrete topic template as a cold observable operation.</summary>
        /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
        /// <param name="configure">Configures the message builder.</param>
        /// <returns>A cold publish operation.</returns>
        public IObservable<MqttClientPublishResult> PublishTopicTemplate(
            MqttTopicTemplate topicTemplate,
            Action<MqttApplicationMessageBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(client);
            var message = MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(topicTemplate, configure);
            return Signal.FromAsync(cancellationToken => client.PublishAsync(message, cancellationToken));
        }

        /// <summary>Publishes to a concrete topic template as a cold asynchronous observable operation.</summary>
        /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublishTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            client.ObservePublishTopicTemplate(topicTemplate, NoConfigureMessage);

        /// <summary>Publishes to a concrete topic template as a cold asynchronous observable operation.</summary>
        /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
        /// <param name="configure">Configures the message builder.</param>
        /// <returns>A cold asynchronous publish operation.</returns>
        public IObservableAsync<MqttClientPublishResult> ObservePublishTopicTemplate(
            MqttTopicTemplate topicTemplate,
            Action<MqttApplicationMessageBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(client);
            var message = MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(topicTemplate, configure);
            return FromAsyncTask(cancellationToken => client.PublishAsync(message, cancellationToken));
        }
    }

    /// <summary>Provides topic-template filtering for received MQTT message streams.</summary>
    /// <param name="source">The received message stream.</param>
    extension(IObservable<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Filters received MQTT messages by a topic template.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>The matching message stream.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            source.WhereTopicTemplate(topicTemplate, false);

        /// <summary>Filters received MQTT messages by a topic template.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <param name="subtree">Whether to include the topic subtree.</param>
        /// <returns>The matching message stream.</returns>
        public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicTemplate(
            MqttTopicTemplate topicTemplate,
            bool subtree)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicTemplate);
            return source.Where(message => message.ApplicationMessage.MatchesTopicTemplate(topicTemplate, subtree));
        }

        /// <summary>Extracts topic template parameters from received messages.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>The template parameter stream.</returns>
        public IObservable<IReadOnlyDictionary<string, string>> SelectTopicTemplateParameters(
            MqttTopicTemplate topicTemplate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicTemplate);
            return source
                .WhereTopicTemplate(topicTemplate)
                .Select(message => (IReadOnlyDictionary<string, string>)ToDictionary(
                    topicTemplate.ParseParameterValues(message.ApplicationMessage)));
        }
    }

    /// <summary>Provides topic-template filtering for asynchronous received MQTT message streams.</summary>
    /// <param name="source">The received message stream.</param>
    extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source)
    {
        /// <summary>Filters received MQTT messages by a topic template.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>The matching message stream.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicTemplate(
            MqttTopicTemplate topicTemplate) =>
            source.WhereTopicTemplate(topicTemplate, false);

        /// <summary>Filters received MQTT messages by a topic template.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <param name="subtree">Whether to include the topic subtree.</param>
        /// <returns>The matching message stream.</returns>
        public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicTemplate(
            MqttTopicTemplate topicTemplate,
            bool subtree)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicTemplate);
            return source.Where(message => message.ApplicationMessage.MatchesTopicTemplate(topicTemplate, subtree));
        }

        /// <summary>Extracts topic template parameters from received messages.</summary>
        /// <param name="topicTemplate">The topic template.</param>
        /// <returns>The template parameter stream.</returns>
        public IObservableAsync<IReadOnlyDictionary<string, string>> SelectTopicTemplateParameters(
            MqttTopicTemplate topicTemplate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(topicTemplate);
            return source
                .WhereTopicTemplate(topicTemplate)
                .Select(message => (IReadOnlyDictionary<string, string>)ToDictionary(
                    topicTemplate.ParseParameterValues(message.ApplicationMessage)));
        }
    }

    /// <summary>Provides response-building helpers for MQTT messages.</summary>
    /// <param name="request">The request message containing response topic metadata.</param>
    extension(MqttApplicationMessage request)
    {
        /// <summary>Builds a response message for a request message.</summary>
        /// <returns>The built response message.</returns>
        public MqttApplicationMessage BuildResponseMessage()
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrEmpty(request.ResponseTopic);
            return new MqttApplicationMessageBuilder()
                .WithTopic(request.ResponseTopic)
                .WithCorrelationData(request.CorrelationData)
                .Build();
        }

        /// <summary>Builds a response message for a request message.</summary>
        /// <param name="configure">Configures the response message builder.</param>
        /// <returns>The built response message.</returns>
        public MqttApplicationMessage BuildResponseMessage(Action<MqttApplicationMessageBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(configure);
            ArgumentException.ThrowIfNullOrEmpty(request.ResponseTopic);

            var builder = new MqttApplicationMessageBuilder()
                .WithTopic(request.ResponseTopic)
                .WithCorrelationData(request.CorrelationData);
            configure(builder);
            return builder.Build();
        }
    }

    /// <summary>Builds MQTT subscribe options from a topic template.</summary>
    /// <param name="topicTemplate">The topic template.</param>
    /// <returns>The subscribe options.</returns>
    public static MqttClientSubscribeOptions BuildSubscribeOptions(MqttTopicTemplate topicTemplate) =>
        BuildSubscribeOptions(
            topicTemplate,
            MqttQualityOfServiceLevel.AtMostOnce,
            false,
            false,
            MqttRetainHandling.SendAtSubscribe,
            false);

    /// <summary>Builds MQTT subscribe options from a topic template.</summary>
    /// <param name="topicTemplate">The topic template.</param>
    /// <param name="qualityOfServiceLevel">The quality-of-service level.</param>
    /// <param name="noLocal">Whether messages published by this client are excluded.</param>
    /// <param name="retainAsPublished">Whether retained messages keep their original retain flag.</param>
    /// <param name="retainHandling">The retain handling behavior.</param>
    /// <param name="subscribeTreeRoot">Whether to subscribe to the whole topic tree.</param>
    /// <returns>The subscribe options.</returns>
    public static MqttClientSubscribeOptions BuildSubscribeOptions(
        MqttTopicTemplate topicTemplate,
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        bool noLocal,
        bool retainAsPublished,
        MqttRetainHandling retainHandling,
        bool subscribeTreeRoot)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        return new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter
                .WithTopicTemplate(topicTemplate, subscribeTreeRoot)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithNoLocal(noLocal)
                .WithRetainAsPublished(retainAsPublished)
                .WithRetainHandling(retainHandling))
            .Build();
    }

    /// <summary>Builds an MQTT application message from a concrete topic template.</summary>
    /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
    /// <returns>The built MQTT application message.</returns>
    public static MqttApplicationMessage BuildApplicationMessage(MqttTopicTemplate topicTemplate)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        return topicTemplate.BuildMessage().Build();
    }

    /// <summary>Builds an MQTT application message from a concrete topic template.</summary>
    /// <param name="topicTemplate">The parameterized topic template with all parameters supplied.</param>
    /// <param name="configure">Configures the message builder.</param>
    /// <returns>The built MQTT application message.</returns>
    public static MqttApplicationMessage BuildApplicationMessage(
        MqttTopicTemplate topicTemplate,
        Action<MqttApplicationMessageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        ArgumentNullException.ThrowIfNull(configure);
        var builder = topicTemplate.BuildMessage();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Converts topic template parameter tuples to a dictionary.</summary>
    /// <param name="parameters">The parameter tuples.</param>
    /// <returns>A dictionary keyed by parameter name.</returns>
    private static Dictionary<string, string> ToDictionary(
        IEnumerable<(string Parameter, int Index, string Value)> parameters)
    {
        var values = new Dictionary<string, string>();
        foreach (var (parameter, _, value) in parameters)
        {
            values[parameter] = value;
        }

        return values;
    }

    /// <summary>Configures no additional message options.</summary>
    /// <param name="builder">The message builder.</param>
    private static void NoConfigureMessage(MqttApplicationMessageBuilder builder) =>
        ArgumentNullException.ThrowIfNull(builder);

    /// <summary>Wraps a task result as a cold asynchronous observable operation.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold asynchronous observable operation.</returns>
    private static IObservableAsync<T> FromAsyncTask<T>(Func<CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return SignalAsync.Create<T>(async (observer, cancellationToken) =>
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync(result, cancellationToken).ConfigureAwait(false);
            await observer.OnCompletedAsync(PrimitivesResult.Success).ConfigureAwait(false);
            return DisposableAsync.Empty;
        });
    }
}
