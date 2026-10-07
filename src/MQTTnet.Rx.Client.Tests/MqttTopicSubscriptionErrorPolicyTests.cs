// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies explicit topic-subscription error policies across observable variants.</summary>
public sealed class MqttTopicSubscriptionErrorPolicyTests
{
    /// <summary>The test topic filter.</summary>
    private const string Topic = "policy/#";

    /// <summary>The invalid topic used for input validation.</summary>
    private const string BlankTopic = " ";

    /// <summary>The subscription count after one retry.</summary>
    private const int RetriedAttempts = 2;

    /// <summary>The maximum completion wait.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies raw subscription failures are either forwarded or retried.</summary>
    /// <param name="retry">Whether to retry the failure.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public Task RawSubscriptionPolicyAsync(bool retry, bool asynchronous) => VerifyPolicyAsync<IMqttClient>(
        source => asynchronous
            ? source.ToSignal().SubscribeToTopic(Topic, retry).ToObservable()
            : source.SubscribeToTopic(Topic, retry),
        retry);

    /// <summary>Verifies resilient subscription failures are either forwarded or retried.</summary>
    /// <param name="retry">Whether to retry the failure.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public Task ResilientSubscriptionPolicyAsync(bool retry, bool asynchronous) => VerifyPolicyAsync<IResilientMqttClient>(
        source => asynchronous
            ? source.ToSignal().SubscribeToTopic(Topic, retry).ToObservable()
            : source.SubscribeToTopic(Topic, retry),
        retry);

    /// <summary>Verifies broker subscription errors reach callers when retry is disabled.</summary>
    /// <param name="resilient">Whether to use a resilient client.</param>
    /// <param name="asynchronous">Whether to use asynchronous observables.</param>
    /// <returns>The test task.</returns>
    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task SubscriptionFailuresReachCallerAsync(bool resilient, bool asynchronous)
    {
        var failure = new InvalidOperationException("broker subscription failed");
        if (resilient)
        {
            using var client = Substitute.For<IResilientMqttClient>();
            _ = client.ApplicationMessageReceived.Returns(Signal.Empty<MqttApplicationMessageReceivedEventArgs>());
            _ = client.SubscribeAsync(Arg.Any<IEnumerable<MqttTopicFilter>>()).Returns(Task.FromException(failure));
            var source = Signal.Emit(client);
            var operation = asynchronous
                ? source.ToSignal().SubscribeToTopic(Topic, false).ToObservable()
                : source.SubscribeToTopic(Topic, false);
            await Assert.That(async () => await operation.FirstAsync(Timeout)).Throws<InvalidOperationException>();
        }
        else
        {
            using var client = new ScriptedMqttClient
            {
                SubscribeHandler = (_, _) => Task.FromException<MqttClientSubscribeResult>(failure),
            };
            var source = Signal.Emit<IMqttClient>(client);
            var operation = asynchronous
                ? source.ToSignal().SubscribeToTopic(Topic, false).ToObservable()
                : source.SubscribeToTopic(Topic, false);
            await Assert.That(async () => await operation.FirstAsync(Timeout)).Throws<InvalidOperationException>();
        }
    }

    /// <summary>Verifies source and topic validation for every overload.</summary>
    /// <returns>The test task.</returns>
    [Test]
    public async Task SubscriptionPolicyRejectsNullSourcesAndBlankTopicsAsync()
    {
        IObservable<IMqttClient> raw = null!;
        IObservable<IResilientMqttClient> resilient = null!;
        IObservableAsync<IMqttClient> asyncRaw = null!;
        IObservableAsync<IResilientMqttClient> asyncResilient = null!;
        await Assert.That(() => raw.SubscribeToTopic(Topic, false)).Throws<ArgumentNullException>();
        await Assert.That(() => resilient.SubscribeToTopic(Topic, false)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncRaw.SubscribeToTopic(Topic, false)).Throws<ArgumentNullException>();
        await Assert.That(() => asyncResilient.SubscribeToTopic(Topic, false)).Throws<ArgumentNullException>();
        await Assert.That(static () => Signal.Empty<IMqttClient>().SubscribeToTopic(BlankTopic, false)).Throws<ArgumentException>();
        await Assert.That(static () => Signal.Empty<IResilientMqttClient>().SubscribeToTopic(BlankTopic, false)).Throws<ArgumentException>();
        await Assert.That(static () => Signal.Empty<IMqttClient>().ToSignal().SubscribeToTopic(BlankTopic, false)).Throws<ArgumentException>();
        await Assert.That(static () => Signal.Empty<IResilientMqttClient>().ToSignal().SubscribeToTopic(BlankTopic, false)).Throws<ArgumentException>();
    }

    /// <summary>Runs an error followed by successful completion and checks the number of subscriptions.</summary>
    /// <typeparam name="T">The client type.</typeparam>
    /// <param name="subscribe">Creates the topic subscription.</param>
    /// <param name="retry">Whether a second subscription is expected.</param>
    /// <returns>The test task.</returns>
    private static async Task VerifyPolicyAsync<T>(
        Func<IObservable<T>, IObservable<MqttApplicationMessageReceivedEventArgs>> subscribe,
        bool retry)
    {
        var attempts = 0;
        var lifetime = Substitute.For<IDisposable>();
        var source = Signal.Create<T>(observer =>
        {
            attempts++;
            if (attempts == 1)
            {
                observer.OnError(new InvalidOperationException("source failed"));
            }
            else
            {
                observer.OnCompleted();
            }

            return lifetime;
        });
        var operation = subscribe(source);
        if (retry)
        {
            await Assert.That((await operation.CollectAsync(Timeout)).Count).IsEqualTo(0);
        }
        else
        {
            await Assert.That(async () => await operation.CollectAsync(Timeout)).Throws<InvalidOperationException>();
        }

        await Assert.That(attempts).IsEqualTo(retry ? RetriedAttempts : 1);
    }
}
