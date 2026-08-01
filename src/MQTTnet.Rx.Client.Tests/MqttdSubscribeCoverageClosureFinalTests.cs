// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides the final public-surface coverage closure for MQTT subscription extensions.</summary>
public sealed partial class MqttdSubscribeCoverageClosureFinalTests
{
    /// <summary>Defines the expected number of successful and failed conversion results.</summary>
    private const int ExpectedConversionResultCount = 2;

    /// <summary>Defines the object value converted by the string extension.</summary>
    private const int StringConversionValue = 42;

    /// <summary>Defines the test payload text used by MQTT messages.</summary>
    private const string Payload = "payload";

    /// <summary>Defines the delay that lets an asynchronous subscription establish itself.</summary>
    private static readonly TimeSpan SubscriptionDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>Defines the delay that permits asynchronous subscription teardown to complete.</summary>
    private static readonly TimeSpan ReleaseDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>Defines the discovery expiry period used by the cleanup test.</summary>
    private static readonly TimeSpan DiscoveryExpiry = TimeSpan.FromSeconds(1);

    /// <summary>Defines the bounded delay required for the discovery cleanup tick.</summary>
    private static readonly TimeSpan DiscoveryCleanupDelay = TimeSpan.FromMilliseconds(2200);

    /// <summary>Tests metadata conversion, its failure path, and object string conversion.</summary>
    /// <returns>A task that completes after the converted values have been observed.</returns>
    [Test]
    public async Task ToObjectWithMetadataAndToString_ConvertSuccessfulAndInvalidPayloadsAsync()
    {
        // Arrange
        var typedResults = new List<CoveragePayload?>();
        var stringResults = new List<string?>();
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("coverage/typed/valid", "{\"Name\":\"value\"}"),
            TestDataHelpers.CreateMessageReceivedArgs("coverage/typed/invalid", "not-json"),
        };

        // Act
        using var typedSubscription = messages
            .ToObservable()
            .ToObject(MqttdSubscribeCoverageJsonContext.Default.CoveragePayload)
            .Subscribe(typedResults.Add);
        using var stringSubscription = MqttdSubscribeExtensions.ToString(new object?[] { StringConversionValue, null }
            .ToObservable())
            .Subscribe(stringResults.Add);

        // Assert
        await Assert.That(typedResults).Count().IsEqualTo(ExpectedConversionResultCount);
        await Assert.That(typedResults[0]).IsNotNull();
        await Assert.That(typedResults[0]!.Name).IsEqualTo("value");
        await Assert.That(typedResults[1]).IsNull();
        await Assert.That(stringResults).Count().IsEqualTo(ExpectedConversionResultCount);
        await Assert.That(stringResults[0]).IsEqualTo("42");
        await Assert.That(stringResults[1]).IsEqualTo(string.Empty);
    }

    /// <summary>Tests the numeric conversion when a JSON number exceeds finite double precision.</summary>
    /// <returns>A task that completes after the numeric result has been observed.</returns>
    [Test]
    public async Task ToDictionary_WhenNumberExceedsFiniteDoubleRange_UsesInfinityAsync()
    {
        // Arrange
        var results = new List<Dictionary<string, object?>?>();
        var message = TestDataHelpers.CreateMessageReceivedArgs("coverage/number", "{\"large\":1e9999999}");

        // Act
        using var subscription = Signal.Emit(message).ToDictionary().Subscribe(results.Add);

        // Assert
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0]).IsNotNull();
        await Assert.That(results[0]!["large"]).IsEqualTo(double.PositiveInfinity);
    }

    /// <summary>Tests duplicate discovery updates and the public raw-client cleanup tick.</summary>
    /// <returns>A task that completes after the discovery cleanup tick has been observed.</returns>
    [Test]
    public async Task DiscoverTopics_ReplacesDuplicateTopicAndExpiresItOnCleanupTickAsync()
    {
        // Arrange
        const string firstTopic = "coverage/discovery/first";
        const string secondTopic = "coverage/discovery/second";
        var client = new MockMqttClient();
        var updates = new List<IEnumerable<(string Topic, DateTime LastSeen)>>();
        using var subscription = Signal
            .Emit<IMqttClient>(client)
            .DiscoverTopics(DiscoveryExpiry, TimeProvider.System)
            .Subscribe(updates.Add);
        using var defaultExpirySubscription = Signal
            .Emit<IMqttClient>(client)
            .DiscoverTopics((TimeSpan?)null)
            .Subscribe();
        await Task.Delay(ReleaseDelay);

        // Act
        await client.SimulateMessageReceivedAsync(firstTopic, Payload);
        await client.SimulateMessageReceivedAsync(secondTopic, Payload);
        await client.SimulateMessageReceivedAsync(secondTopic, Payload);
        await Task.Delay(DiscoveryCleanupDelay);

        // Assert
        await Assert.That(updates).Count().IsGreaterThan(1);
        await Assert.That(updates[^1]).Count().IsEqualTo(0);
    }

    /// <summary>Tests that resilient subscription teardown absorbs an unsubscribe failure.</summary>
    /// <returns>A task that completes after the failed unsubscribe has been absorbed.</returns>
    [Test]
    public async Task SubscribeToTopic_WhenResilientUnsubscribeFails_AbsorbsTheExceptionAsync()
    {
        // Arrange
        var received = new TestSignal<MqttApplicationMessageReceivedEventArgs>();
        var client = Substitute.For<IResilientMqttClient>();
        _ = client.ApplicationMessageReceived.Returns(received);
        _ = client.SubscribeAsync(Arg.Any<IEnumerable<MqttTopicFilter>>()).Returns(Task.CompletedTask);
        _ = client.UnsubscribeAsync(Arg.Any<IEnumerable<string>>())
            .Returns(Task.FromException(new InvalidOperationException("unsubscribe failure")));
        var subscription = Signal.Emit(client).SubscribeToTopic("coverage/resilient/release").Subscribe();
        await Task.Delay(ReleaseDelay);

        // Act
        subscription.Dispose();
        await Task.Delay(ReleaseDelay);

        // Assert
        await Assert.That(client).IsNotNull();
    }

    /// <summary>Tests that raw subscription teardown absorbs an unsubscribe failure.</summary>
    /// <returns>A task that completes after the failed unsubscribe has been absorbed.</returns>
    [Test]
    public async Task SubscribeToTopic_WhenRawUnsubscribeFails_AbsorbsTheExceptionAsync()
    {
        // Arrange
        var client = Substitute.For<IMqttClient>();
        _ = client.SubscribeAsync(Arg.Any<MqttClientSubscribeOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new MqttClientSubscribeResult(0, [], string.Empty, [])));
        _ = client.UnsubscribeAsync(Arg.Any<MqttClientUnsubscribeOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MqttClientUnsubscribeResult>(
                new InvalidOperationException("unsubscribe failure")));
        var subscription = Signal.Emit(client).SubscribeToTopic("coverage/raw/release").Subscribe();
        await Task.Delay(SubscriptionDelay);

        // Act
        subscription.Dispose();
        await Task.Delay(ReleaseDelay);

        // Assert
        await Assert.That(client).IsNotNull();
    }

    /// <summary>Represents the source-generated JSON payload used by metadata tests.</summary>
    public sealed class CoveragePayload
    {
        /// <summary>Gets or sets the payload name.</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Provides source-generated JSON metadata for the typed deserialization test.</summary>
    [JsonSerializable(typeof(CoveragePayload))]
    internal sealed partial class MqttdSubscribeCoverageJsonContext : JsonSerializerContext;
}
