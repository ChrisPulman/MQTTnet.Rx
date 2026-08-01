// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using ReactiveUI.Primitives.Reactive;
#else
using ReactiveUI.Primitives;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes behavioral coverage for topic-filtering extensions.</summary>
public class TopicFilterCoverageClosureTests
{
    /// <summary>The expected number of filtered messages.</summary>
    private const int SingleResultCount = 1;

    /// <summary>The expected number of matching messages or topic groups.</summary>
    private const int TwoResultCount = 2;

    /// <summary>The required number of topic levels.</summary>
    private const int ThreeTopicLevels = 3;

    /// <summary>The unavailable topic-level index.</summary>
    private const int UnavailableTopicLevel = 5;

    /// <summary>A topic used when exercising placeholder extraction.</summary>
    private const string AlphaValueTopic = "site/alpha/value";

    /// <summary>The placeholder value obtained from <see cref="AlphaValueTopic"/>.</summary>
    private const string AlphaPlaceholderValue = "alpha";

    /// <summary>Exercises the topic-filter switch paths and MQTT wildcard matching.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task WhereTopicMatchesAny_HandlesEmptySingleAndMultipleFiltersAsync()
    {
        var matching = TestDataHelpers.CreateMessageReceivedArgs("building/one/temperature", "21");
        var nonMatching = TestDataHelpers.CreateMessageReceivedArgs("building/two/humidity", "44");
        var empty = new List<MqttApplicationMessageReceivedEventArgs>();
        var single = new List<MqttApplicationMessageReceivedEventArgs>();
        var multiple = new List<MqttApplicationMessageReceivedEventArgs>();

        using var emptySubscription = new[] { matching }.ToObservable().WhereTopicMatchesAny().Subscribe(empty.Add);
        using var singleSubscription = new[] { matching, nonMatching }
            .ToObservable()
            .WhereTopicMatchesAny("building/+/temperature")
            .Subscribe(single.Add);
        using var multipleSubscription = new[] { matching, nonMatching }
            .ToObservable()
            .WhereTopicMatchesAny("alerts/#", "building/+/humidity")
            .Subscribe(multiple.Add);

        await Assert.That(empty).IsEmpty();
        await Assert.That(single).Count().IsEqualTo(SingleResultCount);
        await Assert.That(multiple).Count().IsEqualTo(SingleResultCount);
        await Assert.That(multiple[0].ApplicationMessage.Topic).IsEqualTo("building/two/humidity");
    }

    /// <summary>Exercises negated matching and placeholder extraction rejection paths.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task TopicFilters_RejectMismatchesAndInvalidPlaceholdersAsync()
    {
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(AlphaValueTopic, "1"),
            TestDataHelpers.CreateMessageReceivedArgs("site/beta/value", "2"),
            TestDataHelpers.CreateMessageReceivedArgs("site/alpha/other", "3"),
            TestDataHelpers.CreateMessageReceivedArgs("site/alpha/value/extra", "4"),
        };

        var excluded = new List<MqttApplicationMessageReceivedEventArgs>();
        var extracted = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var malformed = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var emptyName = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var emptyCapture = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var invalidName = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var underscoredName = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();

        using var excludedSubscription = messages.ToObservable()
            .WhereTopicIsNotMatch("site/alpha/#")
            .Subscribe(excluded.Add);
        using var extractedSubscription = messages.ToObservable()
            .ExtractTopicValues("site/{name}/value")
            .Subscribe(extracted.Add);
        using var malformedSubscription = messages.ToObservable()
            .ExtractTopicValues("site/{name/value")
            .Subscribe(malformed.Add);
        using var emptyNameSubscription = messages.ToObservable()
            .ExtractTopicValues("site/{}/value")
            .Subscribe(emptyName.Add);
        using var emptyCaptureSubscription = new[] { TestDataHelpers.CreateMessageReceivedArgs("root//value", "5") }
            .ToObservable()
            .ExtractTopicValues("root/{name}/value")
            .Subscribe(emptyCapture.Add);
        using var invalidNameSubscription = messages.ToObservable()
            .ExtractTopicValues("site/{bad-name}/value")
            .Subscribe(invalidName.Add);
        using var underscoredNameSubscription = new[]
            { TestDataHelpers.CreateMessageReceivedArgs(AlphaValueTopic, "5") }
            .ToObservable()
            .ExtractTopicValues("site/{sensor_1}/value")
            .Subscribe(underscoredName.Add);

        await Assert.That(excluded).Count().IsEqualTo(SingleResultCount);
        await Assert.That(excluded[0].ApplicationMessage.Topic).IsEqualTo("site/beta/value");
        await Assert.That(extracted).Count().IsEqualTo(TwoResultCount);
        await Assert.That(extracted[0].Values["name"]).IsEqualTo(AlphaPlaceholderValue);
        await Assert.That(extracted[1].Values["name"]).IsEqualTo("beta");
        await Assert.That(malformed).IsEmpty();
        await Assert.That(emptyName).IsEmpty();
        await Assert.That(emptyCapture).IsEmpty();
        await Assert.That(invalidName).IsEmpty();
        await Assert.That(underscoredName[0].Values["sensor_1"]).IsEqualTo(AlphaPlaceholderValue);
    }

    /// <summary>Exercises embedded, multiple, and repeated placeholders within one topic level.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExtractTopicValues_CapturesEmbeddedMultipleAndRepeatedPlaceholdersAsync()
    {
        var suffixed = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var multiple = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();
        var repeated = new List<
            (MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();

        using var suffixedSubscription = new[]
            { TestDataHelpers.CreateMessageReceivedArgs("root/alphabetx/value", "1") }
            .ToObservable()
            .ExtractTopicValues("root/{name}x/value")
            .Subscribe(suffixed.Add);
        using var multipleSubscription = new[]
            { TestDataHelpers.CreateMessageReceivedArgs("devices/prefix-a1-mid-b2-suffix/value", "2") }
            .ToObservable()
            .ExtractTopicValues("devices/prefix-{model}-mid-{serial}-suffix/value")
            .Subscribe(multiple.Add);
        using var repeatedSubscription = new[]
            { TestDataHelpers.CreateMessageReceivedArgs("device/first-lastx/value", "3") }
            .ToObservable()
            .ExtractTopicValues("device/{id}-{id}x/value")
            .Subscribe(repeated.Add);

        await Assert.That(suffixed).Count().IsEqualTo(SingleResultCount);
        await Assert.That(suffixed[0].Values["name"]).IsEqualTo("alphabet");
        await Assert.That(multiple).Count().IsEqualTo(SingleResultCount);
        await Assert.That(multiple[0].Values["model"]).IsEqualTo("a1");
        await Assert.That(multiple[0].Values["serial"]).IsEqualTo("b2");
        await Assert.That(repeated).Count().IsEqualTo(SingleResultCount);
        await Assert.That(repeated[0].Values["id"]).IsEqualTo("last");
    }

    /// <summary>Exercises level counting, unavailable level selection, and both grouping forms.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task TopicLevelOperations_HandleEmptyLevelsAndUnavailableIndexesAsync()
    {
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("site//value", "1"),
            TestDataHelpers.CreateMessageReceivedArgs("site/alpha/value", "2"),
            TestDataHelpers.CreateMessageReceivedArgs("site/alpha", "3"),
        };

        var counted = new List<MqttApplicationMessageReceivedEventArgs>();
        var missing = new List<string>();
        var topics = new List<string>();
        var levels = new List<string>();

        using var countedSubscription = messages.ToObservable()
            .WhereTopicLevelCount(ThreeTopicLevels)
            .Subscribe(counted.Add);
        using var missingSubscription = messages.ToObservable()
            .SelectTopicLevel(UnavailableTopicLevel)
            .Subscribe(missing.Add);
        using var topicsSubscription = messages.ToObservable()
            .GroupByTopic()
            .Select(static group => group.Key)
            .Subscribe(topics.Add);
        using var levelsSubscription = messages.ToObservable()
            .GroupByTopicLevel(SingleResultCount)
            .Select(static group => group.Key)
            .Subscribe(levels.Add);

        await Assert.That(counted).Count().IsEqualTo(TwoResultCount);
        await Assert.That(missing).IsEmpty();
        await Assert.That(topics).Count().IsEqualTo(ThreeTopicLevels);
        await Assert.That(levels).Contains(string.Empty);
        await Assert.That(levels).Contains("alpha");
    }
}
