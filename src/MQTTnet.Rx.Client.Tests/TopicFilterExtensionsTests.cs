// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MQTTnet.Rx.Client.Tests.Helpers;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>
/// Tests for the TopicFilterExtensions class.
/// </summary>
public class TopicFilterExtensionsTests
{
    /// <summary>
    /// Tests that WhereTopicIsMatch matches exact topic.
    /// </summary>
    [Test]
    public async Task WhereTopicIsMatch_MatchesExactTopic()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temperature", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temperature", "26")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch("sensors/temperature")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that WhereTopicIsMatch matches single-level wildcard.
    /// </summary>
    [Test]
    public async Task WhereTopicIsMatch_MatchesSingleLevelWildcard()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp/room1", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity/room1", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("devices/temp/room1", "26")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch("sensors/+/room1")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that WhereTopicIsMatch matches multi-level wildcard.
    /// </summary>
    [Test]
    public async Task WhereTopicIsMatch_MatchesMultiLevelWildcard()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("home/living/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("home/kitchen/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("office/temp", "22"),
            TestDataHelpers.CreateMessageReceivedArgs("home/bedroom/lights/brightness", "80")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch("home/#")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
    }

    /// <summary>
    /// Tests that WhereTopicMatchesAny matches multiple patterns.
    /// </summary>
    [Test]
    public async Task WhereTopicMatchesAny_MatchesMultiplePatterns()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("devices/status", "online"),
            TestDataHelpers.CreateMessageReceivedArgs("alerts/fire", "true"),
            TestDataHelpers.CreateMessageReceivedArgs("other/topic", "data")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicMatchesAny("sensors/#", "alerts/#")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that WhereTopicIsNotMatch excludes matching topics.
    /// </summary>
    [Test]
    public async Task WhereTopicIsNotMatch_ExcludesMatchingTopics()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("debug/trace", "data"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity", "60")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsNotMatch("debug/#")
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that ExtractTopicValues extracts values correctly.
    /// </summary>
    [Test]
    public async Task ExtractTopicValues_ExtractsValues()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp123/readings/celsius", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/hum456/readings/percent", "60")
        };

        var results = new List<(MqttApplicationMessageReceivedEventArgs, Dictionary<string, string>)>();

        // Act
        using var subscription = messages.ToObservable()
            .ExtractTopicValues("sensors/{sensorId}/readings/{unit}")
            .Subscribe(x => results.Add(x));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0].Item2["sensorId"]).IsEqualTo("temp123");
        await Assert.That(results[0].Item2["unit"]).IsEqualTo("celsius");
        await Assert.That(results[1].Item2["sensorId"]).IsEqualTo("hum456");
    }

    /// <summary>
    /// Tests that WhereTopicLevelCount filters by level count.
    /// </summary>
    [Test]
    public async Task WhereTopicLevelCount_FiltersByLevelCount()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("a/b/c", "data"),
            TestDataHelpers.CreateMessageReceivedArgs("a/b", "data"),
            TestDataHelpers.CreateMessageReceivedArgs("a/b/c/d", "data"),
            TestDataHelpers.CreateMessageReceivedArgs("x/y/z", "data")
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicLevelCount(3)
            .Subscribe(m => results.Add(m));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
    }

    /// <summary>
    /// Tests that SelectTopicLevel extracts specific level.
    /// </summary>
    [Test]
    public async Task SelectTopicLevel_ExtractsSpecificLevel()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("home/living/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("home/kitchen/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("home/bedroom/lights", "on")
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .SelectTopicLevel(1) // Get the room names
            .Subscribe(level => results.Add(level));

        await Task.Delay(50);

        // Assert
        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results).Contains("living");
        await Assert.That(results).Contains("kitchen");
        await Assert.That(results).Contains("bedroom");
    }

    /// <summary>
    /// Tests that GroupByTopic groups messages correctly.
    /// </summary>
    [Test]
    public async Task GroupByTopic_GroupsMessagesCorrectly()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "a"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "b"),
            TestDataHelpers.CreateMessageReceivedArgs("topic1", "c"),
            TestDataHelpers.CreateMessageReceivedArgs("topic2", "d")
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopic()
            .Subscribe(group => groupKeys.Add(group.Key));

        await Task.Delay(50);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(2);
        await Assert.That(groupKeys).Contains("topic1");
        await Assert.That(groupKeys).Contains("topic2");
    }

    /// <summary>
    /// Tests that GroupByTopicLevel groups by specific level.
    /// </summary>
    [Test]
    public async Task GroupByTopicLevel_GroupsBySpecificLevel()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room1/temp", "25"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room2/temp", "22"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room1/humidity", "60"),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room2/humidity", "55")
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopicLevel(1) // Group by room
            .Subscribe(group => groupKeys.Add(group.Key));

        await Task.Delay(50);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(2);
        await Assert.That(groupKeys).Contains("room1");
        await Assert.That(groupKeys).Contains("room2");
    }
}
