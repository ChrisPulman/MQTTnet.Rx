// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Extensions.TopicTemplate;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Async.Disposables;
#if REACTIVE_SHIM
using MQTTnet.Rx.Extensions.Rpc.Reactive;
using MQTTnet.Rx.Extensions.TopicTemplate.Reactive;
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using MQTTnet.Rx.Extensions.Rpc;
using MQTTnet.Rx.Extensions.TopicTemplate;
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises the MQTTnet.Extensions.Rpc and MQTTnet.Extensions.TopicTemplate reactive wrappers.</summary>
public sealed class RpcAndTopicTemplateExtensionCoverageTests
{
    /// <summary>The MQTT topic template used by the tests.</summary>
    private const string TemperatureTemplate = "factory/{line}/temperature";

    /// <summary>The concrete MQTT topic created from <see cref="TemperatureTemplate"/>.</summary>
    private const string TemperatureTopic = "factory/a/temperature";

    /// <summary>The concrete subtree MQTT topic created from <see cref="TemperatureTemplate"/>.</summary>
    private const string TemperatureSubtreeTopic = "factory/a/temperature/#";

    /// <summary>The MQTT response topic used by response-building tests.</summary>
    private const string ResponseTopic = "responses/1";

    /// <summary>The MQTT request topic used by response-building tests.</summary>
    private const string RequestTopic = "requests/1";

    /// <summary>The text payload shared by RPC wrapper tests.</summary>
    private const string PayloadText = "payload";

    /// <summary>The string payload used by QoS wrapper tests.</summary>
    private const string HelloText = "hello";

    /// <summary>The RPC response payload used by synchronous wrapper tests.</summary>
    private const string PongText = "pong";

    /// <summary>The RPC response payload used by asynchronous wrapper tests.</summary>
    private const string ObservedText = "observed";

    /// <summary>The default RPC method name used by argument validation tests.</summary>
    private const string MethodName = "method";

    /// <summary>The pending RPC timeout duration in seconds.</summary>
    private const int PendingTimeoutSeconds = 30;

    /// <summary>The expected number of synchronous RPC calls made by the wrapper test.</summary>
    private const int ExpectedSynchronousRpcCallCount = 7;

    /// <summary>The expected number of topic-template subscribe and publish operations.</summary>
    private const int ExpectedTopicTemplateOperationCount = 4;

    /// <summary>The expected number of subtree topic-template matches.</summary>
    private const int ExpectedSubtreeMatchCount = 2;

    /// <summary>The fourth published message index.</summary>
    private const int FourthPublishedMessageIndex = 3;

    /// <summary>The maximum time allowed for cold observable operations to produce a result.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(2);

    /// <summary>The RPC response bytes used by synchronous wrapper tests.</summary>
    private static readonly byte[] PongBytes = "pong"u8.ToArray();

    /// <summary>The RPC response bytes used by asynchronous wrapper tests.</summary>
    private static readonly byte[] ObservedBytes = "observed"u8.ToArray();

    /// <summary>The UTF-8 bytes for <see cref="HelloText"/>.</summary>
    private static readonly byte[] HelloBytes = "hello"u8.ToArray();

    /// <summary>The binary request payload used by RPC wrapper tests.</summary>
    private static readonly byte[] BinaryPayload = [1, 2];

    /// <summary>The default binary request payload used by RPC wrapper tests.</summary>
    private static readonly byte[] DefaultBinaryPayload = [3];

    /// <summary>The pending binary request payload used by RPC cancellation tests.</summary>
    private static readonly byte[] PendingBinaryPayload = [1];

    /// <summary>The first asynchronous binary request payload.</summary>
    private static readonly byte[] FirstObservedPayload = [1];

    /// <summary>The second asynchronous binary request payload.</summary>
    private static readonly byte[] SecondObservedPayload = [2];

    /// <summary>The failure asynchronous binary request payload.</summary>
    private static readonly byte[] FailurePayload = [3];

    /// <summary>The expected correlation data copied into response messages.</summary>
    private static readonly byte[] CorrelationData = [1, 2, 3];

    /// <summary>Verifies RPC client factory and option-builder helpers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RpcFactoryAndOptionsHelpers_CreateConfiguredClientsAsync()
    {
        using var mqttClient = new MockMqttClient();
        var strategy = new CapturingTopicGenerationStrategy();

        var defaultClient = mqttClient.CreateRpcClient();
        var configuredClient = mqttClient.CreateRpcClient(options => options.UseTopicGenerationStrategy(strategy));
        var options = new MqttRpcClientOptionsBuilder().UseTopicGenerationStrategy(strategy).Build();

        await Assert.That(defaultClient).IsNotNull();
        await Assert.That(configuredClient).IsNotNull();
        await Assert.That(options.TopicGenerationStrategy).IsSameReferenceAs(strategy);
        await Assert.That(static () => ((IMqttClient)null!).CreateRpcClient()).Throws<ArgumentNullException>();
        await Assert.That(() => mqttClient.CreateRpcClient(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((MqttRpcClientOptionsBuilder)null!).UseTopicGenerationStrategy(strategy))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new MqttRpcClientOptionsBuilder().UseTopicGenerationStrategy(null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Verifies synchronous RPC wrappers are cold, cancellation-aware, and decode string responses.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RpcSynchronousExecuteWrappers_UseCancellationAwareExecuteAsync()
    {
        using var client = new RecordingRpcClient(PongBytes);
        var parameters = new Dictionary<string, object> { ["device"] = "line-1" };

        var response = await client.ExecuteString(
                OperationTimeout,
                "ping",
                PayloadText,
                MqttQualityOfServiceLevel.AtLeastOnce,
                parameters)
            .FirstAsync(OperationTimeout);

        await Assert.That(client.LastParameters).IsSameReferenceAs(parameters);

        var binaryResponse = await client.Execute(
                OperationTimeout,
                "ping-bytes",
                BinaryPayload,
                MqttQualityOfServiceLevel.ExactlyOnce)
            .FirstAsync(OperationTimeout);
        var defaultResponse = await client.Execute(OperationTimeout, "ping-default", DefaultBinaryPayload).FirstAsync(OperationTimeout);
        var stringResponse = await client.Execute(OperationTimeout, "ping-string", HelloText).FirstAsync(OperationTimeout);
        var qosStringBytesResponse = await client.Execute(
                OperationTimeout,
                "ping-string-qos-bytes",
                HelloText,
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);
        var defaultStringResponse = await client.ExecuteString(OperationTimeout, "ping-string-default", PayloadText)
            .FirstAsync(OperationTimeout);
        var qosStringResponse = await client.ExecuteString(
                OperationTimeout,
                "ping-string-qos",
                HelloText,
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);

        await Assert.That(response).IsEqualTo(PongText);
        await Assert.That(binaryResponse).IsEquivalentTo(PongBytes);
        await Assert.That(defaultResponse).IsEquivalentTo(PongBytes);
        await Assert.That(stringResponse).IsEquivalentTo(PongBytes);
        await Assert.That(qosStringBytesResponse).IsEquivalentTo(PongBytes);
        await Assert.That(defaultStringResponse).IsEqualTo(PongText);
        await Assert.That(qosStringResponse).IsEqualTo(PongText);
        await Assert.That(client.TimeoutExecuteCount).IsEqualTo(0);
        await Assert.That(client.CancellationExecuteCount).IsEqualTo(ExpectedSynchronousRpcCallCount);
        await Assert.That(client.LastMethodName).IsEqualTo("ping-string-qos");
        await Assert.That(client.LastPayload).IsEquivalentTo(HelloBytes);
        await Assert.That(client.LastQualityOfServiceLevel).IsEqualTo(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    /// <summary>Verifies disposing an RPC observable cancels the token-based upstream operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RpcSynchronousExecuteWrappers_CancelUpstreamOperationWhenDisposedAsync()
    {
        using var client = new RecordingRpcClient { KeepCancellationExecutePending = true };
        using var subscription = client.Execute(TimeSpan.FromSeconds(PendingTimeoutSeconds), "pending", PendingBinaryPayload).Subscribe(
            static _ => { },
            static _ => { });
        var token = await client.CapturedCancellationToken.Task.WaitAsync(OperationTimeout);

        subscription.Dispose();

        await Assert.That(SpinWait.SpinUntil(() => token.IsCancellationRequested, OperationTimeout)).IsTrue();
        await Assert.That(client.TimeoutExecuteCount).IsEqualTo(0);
        await Assert.That(client.CancellationExecuteCount).IsEqualTo(1);
    }

    /// <summary>Verifies RPC asynchronous observable wrappers emit successes and failures.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RpcAsynchronousExecuteWrappers_EmitResponsesAndFailuresAsync()
    {
        using var client = new RecordingRpcClient(ObservedBytes);

        var defaultBytes = await client.ObserveExecute("observe-default", FirstObservedPayload).FirstAsync(OperationTimeout);
        var qosBytes = await client.ObserveExecute("observe-qos", SecondObservedPayload, MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);
        var stringBytes = await client.ObserveExecute("observe-string", PayloadText).FirstAsync(OperationTimeout);
        var qosStringBytes = await client.ObserveExecute(
                "observe-string-qos",
                PayloadText,
                MqttQualityOfServiceLevel.ExactlyOnce)
            .FirstAsync(OperationTimeout);
        var decoded = await client.ObserveExecuteString("observe-decode", PayloadText).FirstAsync(OperationTimeout);
        var decodedWithQos = await client.ObserveExecuteString(
                "observe-decode-qos",
                PayloadText,
                MqttQualityOfServiceLevel.AtLeastOnce)
            .FirstAsync(OperationTimeout);
        var decodedWithParameters = await client.ObserveExecuteString(
                "observe-decode-parameters",
                PayloadText,
                MqttQualityOfServiceLevel.AtMostOnce,
                new Dictionary<string, object>())
            .FirstAsync(OperationTimeout);
        var decodedWithCachedSelector = await client.ObserveExecuteString(
                "observe-decode-parameters-cached",
                PayloadText,
                MqttQualityOfServiceLevel.AtMostOnce,
                new Dictionary<string, object>())
            .FirstAsync(OperationTimeout);

        client.Failure = new InvalidOperationException("RPC failed.");

        await Assert.That(defaultBytes).IsEquivalentTo(ObservedBytes);
        await Assert.That(qosBytes).IsEquivalentTo(ObservedBytes);
        await Assert.That(stringBytes).IsEquivalentTo(ObservedBytes);
        await Assert.That(qosStringBytes).IsEquivalentTo(ObservedBytes);
        await Assert.That(decoded).IsEqualTo(ObservedText);
        await Assert.That(decodedWithQos).IsEqualTo(ObservedText);
        await Assert.That(decodedWithParameters).IsEqualTo(ObservedText);
        await Assert.That(decodedWithCachedSelector).IsEqualTo(ObservedText);
        await Assert.That(async () => await client.ObserveExecute("failure", FailurePayload).FirstAsync(OperationTimeout))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Verifies RPC helper argument validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RpcWrappers_RejectMissingArgumentsAsync()
    {
        using var client = new RecordingRpcClient();
        var parameters = new Dictionary<string, object>();

        await Assert.That(static () => ((IMqttRpcClient)null!).Execute(OperationTimeout, MethodName, PendingBinaryPayload))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.Execute(OperationTimeout, null!, PendingBinaryPayload, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.Execute(OperationTimeout, MethodName, (byte[])null!, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.Execute(OperationTimeout, MethodName, PendingBinaryPayload, MqttQualityOfServiceLevel.AtMostOnce, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.Execute(OperationTimeout, MethodName, (string)null!, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.ObserveExecute(null!, PendingBinaryPayload, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.ObserveExecute(MethodName, (byte[])null!, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.ObserveExecute(MethodName, PendingBinaryPayload, MqttQualityOfServiceLevel.AtMostOnce, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => client.ObserveExecute(MethodName, (string)null!, MqttQualityOfServiceLevel.AtMostOnce, parameters))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Verifies TopicTemplate subscribe and publish client wrappers build complete MQTT operations.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TopicTemplateClientWrappers_SubscribeAndPublishWithTemplateOptionsAsync()
    {
        using var client = new MockMqttClient();
        var template = MqttTopicTemplateReactiveExtensions
            .TopicTemplate(TemperatureTemplate)
            .WithParameter("line", "a");

        var subscribe = await client.SubscribeTopicTemplate(template).FirstAsync(OperationTimeout);
        var configuredSubscribe = await client.SubscribeTopicTemplate(
                template,
                MqttQualityOfServiceLevel.ExactlyOnce,
                true,
                true,
                MqttRetainHandling.DoNotSendOnSubscribe,
                true)
            .FirstAsync(OperationTimeout);
        var observedSubscribe = await client.ObserveSubscribeTopicTemplate(template).FirstAsync(OperationTimeout);
        var configuredObservedSubscribe = await client.ObserveSubscribeTopicTemplate(
                template,
                MqttQualityOfServiceLevel.AtLeastOnce,
                true,
                false,
                MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly,
                false)
            .FirstAsync(OperationTimeout);
        var publish = await client.PublishTopicTemplate(template).FirstAsync(OperationTimeout);
        var configuredPublish = await client.PublishTopicTemplate(
                template,
                static builder => builder.WithPayload("42").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .FirstAsync(OperationTimeout);
        var observedPublish = await client.ObservePublishTopicTemplate(template).FirstAsync(OperationTimeout);
        var configuredObservedPublish = await client.ObservePublishTopicTemplate(
                template,
                static builder => builder.WithPayload("84").WithRetainFlag())
            .FirstAsync(OperationTimeout);

        await Assert.That(subscribe.Items).Count().IsEqualTo(1);
        await Assert.That(configuredSubscribe.Items).Count().IsEqualTo(1);
        await Assert.That(observedSubscribe.Items).Count().IsEqualTo(1);
        await Assert.That(configuredObservedSubscribe.Items).Count().IsEqualTo(1);
        await Assert.That(publish.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(configuredPublish.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(observedPublish.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(configuredObservedPublish.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(client.Subscriptions).Count().IsEqualTo(ExpectedTopicTemplateOperationCount);
        await Assert.That(client.Subscriptions[1].TopicFilters[0].Topic).IsEqualTo(TemperatureSubtreeTopic);
        await Assert.That(client.Subscriptions[1].TopicFilters[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.ExactlyOnce);
        await Assert.That(client.Subscriptions[1].TopicFilters[0].NoLocal).IsTrue();
        await Assert.That(client.Subscriptions[1].TopicFilters[0].RetainAsPublished).IsTrue();
        await Assert.That(client.Subscriptions[1].TopicFilters[0].RetainHandling)
            .IsEqualTo(MqttRetainHandling.DoNotSendOnSubscribe);
        await Assert.That(client.PublishedMessages).Count().IsEqualTo(ExpectedTopicTemplateOperationCount);
        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(TemperatureTopic);
        await Assert.That(Encoding.UTF8.GetString(client.PublishedMessages[1].Payload.ToArray())).IsEqualTo("42");
        await Assert.That(Encoding.UTF8.GetString(client.PublishedMessages[FourthPublishedMessageIndex].Payload.ToArray()))
            .IsEqualTo("84");
        await Assert.That(client.PublishedMessages[FourthPublishedMessageIndex].Retain).IsTrue();
    }

    /// <summary>Verifies TopicTemplate stream filters and parameter projection for sync and async observables.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TopicTemplateStreamWrappers_FilterAndExtractParametersAsync()
    {
        var template = MqttTopicTemplateReactiveExtensions.TopicTemplate(TemperatureTemplate);
        var nonMatch = CreateReceivedMessage("factory/a/humidity", "40");
        var match = CreateReceivedMessage(TemperatureTopic, "42");
        var subtreeMatch = CreateReceivedMessage("factory/a/temperature/raw", "43");
        var source = Signal.FromEnumerable([nonMatch, match, subtreeMatch]);
        var asyncSource = SignalAsync.Create<MqttApplicationMessageReceivedEventArgs>(async (observer, cancellationToken) =>
        {
            await observer.OnNextAsync(nonMatch, cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync(match, cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync(subtreeMatch, cancellationToken).ConfigureAwait(false);
            await observer.OnCompletedAsync(ReactiveUI.Primitives.Result.Success).ConfigureAwait(false);
            return DisposableAsync.Empty;
        });

        var exactMatches = await source.WhereTopicTemplate(template).CollectAsync(OperationTimeout);
        var subtreeMatches = await source.WhereTopicTemplate(template, true).CollectAsync(OperationTimeout);
        var parameterValues = await source.SelectTopicTemplateParameters(template).FirstAsync(OperationTimeout);
        var asyncExactMatches = await CollectAsync(asyncSource.WhereTopicTemplate(template));
        var asyncSubtreeMatches = await CollectAsync(asyncSource.WhereTopicTemplate(template, true));
        var asyncParameterValues = await asyncSource.SelectTopicTemplateParameters(template).FirstAsync(OperationTimeout);

        await Assert.That(exactMatches).Count().IsEqualTo(1);
        await Assert.That(subtreeMatches).Count().IsEqualTo(ExpectedSubtreeMatchCount);
        await Assert.That(parameterValues["line"]).IsEqualTo("a");
        await Assert.That(asyncExactMatches).Count().IsEqualTo(1);
        await Assert.That(asyncSubtreeMatches).Count().IsEqualTo(ExpectedSubtreeMatchCount);
        await Assert.That(asyncParameterValues["line"]).IsEqualTo("a");
    }

    /// <summary>Verifies TopicTemplate direct builders and response helpers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TopicTemplateBuilders_CreateSubscribePublishAndResponseMessagesAsync()
    {
        var template = MqttTopicTemplateReactiveExtensions
            .TopicTemplate(TemperatureTemplate)
            .WithParameter("line", "a");
        var defaultSubscribe = MqttTopicTemplateReactiveExtensions.BuildSubscribeOptions(template);
        var configuredSubscribe = MqttTopicTemplateReactiveExtensions.BuildSubscribeOptions(
            template,
            MqttQualityOfServiceLevel.AtLeastOnce,
            true,
            true,
            MqttRetainHandling.DoNotSendOnSubscribe,
            true);
        var defaultMessage = MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(template);
        var cachedDefaultMessage = MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(template);
        var configuredMessage = MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(
            template,
            static builder => builder.WithPayload("100").WithRetainFlag());
        var request = new MqttApplicationMessageBuilder()
            .WithTopic(RequestTopic)
            .WithResponseTopic(ResponseTopic)
            .WithCorrelationData(CorrelationData)
            .Build();
        var defaultResponse = request.BuildResponseMessage();
        var cachedDefaultResponse = request.BuildResponseMessage();
        var configuredResponse = request.BuildResponseMessage(static builder => builder.WithPayload("accepted"));

        await Assert.That(defaultSubscribe.TopicFilters[0].Topic).IsEqualTo(TemperatureTopic);
        await Assert.That(configuredSubscribe.TopicFilters[0].Topic).IsEqualTo(TemperatureSubtreeTopic);
        await Assert.That(configuredSubscribe.TopicFilters[0].QualityOfServiceLevel)
            .IsEqualTo(MqttQualityOfServiceLevel.AtLeastOnce);
        await Assert.That(defaultMessage.Topic).IsEqualTo(TemperatureTopic);
        await Assert.That(cachedDefaultMessage.Topic).IsEqualTo(TemperatureTopic);
        await Assert.That(configuredMessage.Topic).IsEqualTo(TemperatureTopic);
        await Assert.That(Encoding.UTF8.GetString(configuredMessage.Payload.ToArray())).IsEqualTo("100");
        await Assert.That(configuredMessage.Retain).IsTrue();
        await Assert.That(defaultResponse.Topic).IsEqualTo(ResponseTopic);
        await Assert.That(cachedDefaultResponse.Topic).IsEqualTo(ResponseTopic);
        await Assert.That(configuredResponse.Topic).IsEqualTo(ResponseTopic);
        await Assert.That(Encoding.UTF8.GetString(configuredResponse.Payload.ToArray())).IsEqualTo("accepted");
        await Assert.That(configuredResponse.CorrelationData.Length).IsEqualTo(CorrelationData.Length);
        await Assert.That(configuredResponse.CorrelationData[0]).IsEqualTo(CorrelationData[0]);
        await Assert.That(configuredResponse.CorrelationData[1]).IsEqualTo(CorrelationData[1]);
        await Assert.That(configuredResponse.CorrelationData[2]).IsEqualTo(CorrelationData[2]);
    }

    /// <summary>Verifies TopicTemplate helper argument validation paths.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TopicTemplateWrappers_RejectMissingArgumentsAsync()
    {
        using var client = new MockMqttClient();
        var template = MqttTopicTemplateReactiveExtensions.TopicTemplate(TemperatureTemplate);
        IObservable<MqttApplicationMessageReceivedEventArgs> source = Signal.None<MqttApplicationMessageReceivedEventArgs>();
        IObservableAsync<MqttApplicationMessageReceivedEventArgs> asyncSource =
            SignalAsync.None<MqttApplicationMessageReceivedEventArgs>();
        var message = new MqttApplicationMessageBuilder().WithTopic(RequestTopic).WithResponseTopic(ResponseTopic).Build();
        var missingResponseMessage = new MqttApplicationMessageBuilder().WithTopic("requests/empty").Build();

        await Assert.That(static () => MqttTopicTemplateReactiveExtensions.TopicTemplate(null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => ((IMqttClient)null!).SubscribeTopicTemplate(template)).Throws<ArgumentNullException>();
        await Assert.That(() => client.SubscribeTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IMqttClient)null!).ObserveSubscribeTopicTemplate(template)).Throws<ArgumentNullException>();
        await Assert.That(() => client.ObserveSubscribeTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IMqttClient)null!).PublishTopicTemplate(template)).Throws<ArgumentNullException>();
        await Assert.That(() => client.PublishTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => client.PublishTopicTemplate(template, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IMqttClient)null!).ObservePublishTopicTemplate(template)).Throws<ArgumentNullException>();
        await Assert.That(() => client.ObservePublishTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => client.ObservePublishTopicTemplate(template, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<MqttApplicationMessageReceivedEventArgs>)null!).WhereTopicTemplate(template))
            .Throws<ArgumentNullException>();
        await Assert.That(() => source.WhereTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservable<MqttApplicationMessageReceivedEventArgs>)null!).SelectTopicTemplateParameters(template))
            .Throws<ArgumentNullException>();
        await Assert.That(() => source.SelectTopicTemplateParameters(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<MqttApplicationMessageReceivedEventArgs>)null!).WhereTopicTemplate(template))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncSource.WhereTopicTemplate(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => ((IObservableAsync<MqttApplicationMessageReceivedEventArgs>)null!).SelectTopicTemplateParameters(template))
            .Throws<ArgumentNullException>();
        await Assert.That(() => asyncSource.SelectTopicTemplateParameters(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => ((MqttApplicationMessage)null!).BuildResponseMessage()).Throws<ArgumentNullException>();
        await Assert.That(() => message.BuildResponseMessage(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => missingResponseMessage.BuildResponseMessage()).Throws<ArgumentException>();
        await Assert.That(static () => MqttTopicTemplateReactiveExtensions.BuildSubscribeOptions(null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => MqttTopicTemplateReactiveExtensions.BuildApplicationMessage(template, null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Collects all values from an asynchronous observable.</summary>
    /// <typeparam name="T">The observable element type.</typeparam>
    /// <param name="observable">The asynchronous observable.</param>
    /// <returns>The collected values.</returns>
    private static async Task<List<T>> CollectAsync<T>(IObservableAsync<T> observable)
    {
        var values = new List<T>();
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        await using var subscription = await observable.SubscribeAsync(
                (value, cancellationToken) =>
                {
                    values.Add(value);
                    return ValueTask.CompletedTask;
                },
                cancellation.Token)
            .ConfigureAwait(false);
        return values;
    }

    /// <summary>Creates received-message event args for topic template tests.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The message payload.</param>
    /// <returns>The event args.</returns>
    private static MqttApplicationMessageReceivedEventArgs CreateReceivedMessage(string topic, string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadSequence = new ReadOnlySequence<byte>(payloadBytes);
        var message = new MqttApplicationMessage { Topic = topic, Payload = payloadSequence };
        var packet = new MqttPublishPacket { Topic = topic, Payload = payloadSequence };
        return new("client", message, packet, null);
    }

    /// <summary>Captures RPC topic generation contexts.</summary>
    private sealed class CapturingTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
    {
        /// <inheritdoc/>
        public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new MqttRpcTopicPair
            {
                RequestTopic = $"rpc/{context.MethodName}",
                ResponseTopic = $"rpc/{context.MethodName}/response",
            };
        }
    }

    /// <summary>Records RPC calls made by the reactive wrappers.</summary>
    private sealed class RecordingRpcClient : IMqttRpcClient
    {
        /// <summary>The response returned by token-based execute calls.</summary>
        private readonly byte[] _cancellationResponse;

        /// <summary>Initializes a new instance of the <see cref="RecordingRpcClient"/> class.</summary>
        /// <param name="cancellationResponse">The response returned by token-based execute calls.</param>
        public RecordingRpcClient(byte[]? cancellationResponse = null) =>
            _cancellationResponse = cancellationResponse ?? "response"u8.ToArray();

        /// <summary>Gets the token captured by the token-based execute overload.</summary>
        public TaskCompletionSource<CancellationToken> CapturedCancellationToken { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets or sets the exception returned by token-based execute calls.</summary>
        public Exception? Failure { get; set; }

        /// <summary>Gets or sets whether the token-based execute operation should stay pending until cancellation.</summary>
        public bool KeepCancellationExecutePending { get; set; }

        /// <summary>Gets the number of timeout-overload execute calls.</summary>
        public int TimeoutExecuteCount { get; private set; }

        /// <summary>Gets the number of token-overload execute calls.</summary>
        public int CancellationExecuteCount { get; private set; }

        /// <summary>Gets the last method name passed to the token-based execute overload.</summary>
        public string? LastMethodName { get; private set; }

        /// <summary>Gets the last payload passed to the token-based execute overload.</summary>
        public byte[]? LastPayload { get; private set; }

        /// <summary>Gets the last QoS value passed to the token-based execute overload.</summary>
        public MqttQualityOfServiceLevel LastQualityOfServiceLevel { get; private set; }

        /// <summary>Gets the last parameters passed to the token-based execute overload.</summary>
        public IDictionary<string, object>? LastParameters { get; private set; }

        /// <inheritdoc/>
        public Task<byte[]> ExecuteAsync(
            TimeSpan timeout,
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            GC.KeepAlive(timeout);
            GC.KeepAlive(methodName);
            GC.KeepAlive(payload);
            GC.KeepAlive(qualityOfServiceLevel);
            GC.KeepAlive(parameters);
            TimeoutExecuteCount++;
            return Task.FromResult("timeout-overload"u8.ToArray());
        }

        /// <inheritdoc/>
        public Task<byte[]> ExecuteAsync(
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters,
            CancellationToken cancellationToken)
        {
            CancellationExecuteCount++;
            LastMethodName = methodName;
            LastPayload = payload;
            LastQualityOfServiceLevel = qualityOfServiceLevel;
            LastParameters = parameters;
            _ = CapturedCancellationToken.TrySetResult(cancellationToken);

            if (Failure is not null)
            {
                return Task.FromException<byte[]>(Failure);
            }

            if (!KeepCancellationExecutePending)
            {
                return Task.FromResult(_cancellationResponse);
            }

            var pending = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = cancellationToken.Register(
                static state =>
                {
                    if (state is TaskCompletionSource<byte[]> completion)
                    {
                        _ = completion.TrySetCanceled();
                    }
                },
                pending);
            return pending.Task;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
