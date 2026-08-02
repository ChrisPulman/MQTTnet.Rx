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

/// <summary>Tests for the TopicFilterExtensions class.</summary>
public class TopicFilterExtensionsTests
{
    /// <summary>The number of results expected for two matches.</summary>
    private const int ExpectedDoubleResultCount = 2;

    /// <summary>The number of results expected for three matches.</summary>
    private const int ExpectedTripleResultCount = 3;

    /// <summary>The index of the first result.</summary>
    private const int FirstResultIndex = 0;

    /// <summary>The index of the second result.</summary>
    private const int SecondResultIndex = 1;

    /// <summary>The topic level containing the room name.</summary>
    private const int RoomTopicLevel = 1;

    /// <summary>The delay used to allow observable handlers to process test events.</summary>
    private const int ObservableProcessingDelayMilliseconds = 50;

    /// <summary>The payload representing a temperature reading.</summary>
    private const string TemperaturePayload = "25";

    /// <summary>The payload representing a humidity reading.</summary>
    private const string HumidityPayload = "60";

    /// <summary>The alternate payload representing a temperature reading.</summary>
    private const string AlternateTemperaturePayload = "26";

    /// <summary>The payload representing an additional temperature reading.</summary>
    private const string AdditionalTemperaturePayload = "22";

    /// <summary>The generic payload used by topic tests.</summary>
    private const string DataPayload = "data";

    /// <summary>The topic for sensor temperature readings.</summary>
    private const string SensorTemperatureTopic = "sensors/temp";

    /// <summary>The dictionary key for a sensor identifier.</summary>
    private const string SensorIdentifierKey = "sensorId";

    /// <summary>The topic for sensor humidity readings.</summary>
    private const string SensorHumidityTopic = "sensors/humidity";

    /// <summary>The topic for living-room temperature readings.</summary>
    private const string LivingRoomTemperatureTopic = "home/living/temp";

    /// <summary>The topic for kitchen humidity readings.</summary>
    private const string KitchenHumidityTopic = "home/kitchen/humidity";

    /// <summary>The first generic topic used by grouping tests.</summary>
    private const string FirstTopic = "topic1";

    /// <summary>The second generic topic used by grouping tests.</summary>
    private const string SecondTopic = "topic2";

    /// <summary>Tests that WhereTopicIsMatch matches exact topic.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicIsMatch_MatchesExactTopicAsync()
    {
        // Arrange
        const string temperatureTopic = "sensors/temperature";
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(temperatureTopic, TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs(SensorHumidityTopic, HumidityPayload),
            TestDataHelpers.CreateMessageReceivedArgs(temperatureTopic, AlternateTemperaturePayload),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch(temperatureTopic)
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
    }

    /// <summary>Tests that WhereTopicIsMatch matches single-level wildcard.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicIsMatch_MatchesSingleLevelWildcardAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp/room1", TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/humidity/room1", HumidityPayload),
            TestDataHelpers.CreateMessageReceivedArgs("devices/temp/room1", AlternateTemperaturePayload),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch("sensors/+/room1")
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
    }

    /// <summary>Tests that WhereTopicIsMatch matches multi-level wildcard.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicIsMatch_MatchesMultiLevelWildcardAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(LivingRoomTemperatureTopic, TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs(KitchenHumidityTopic, HumidityPayload),
            TestDataHelpers.CreateMessageReceivedArgs("office/temp", AdditionalTemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("home/bedroom/lights/brightness", "80"),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsMatch("home/#")
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedTripleResultCount);
    }

    /// <summary>Tests that WhereTopicMatchesAny matches multiple patterns.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicMatchesAny_MatchesMultiplePatternsAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(SensorTemperatureTopic, TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("devices/status", "online"),
            TestDataHelpers.CreateMessageReceivedArgs("alerts/fire", "true"),
            TestDataHelpers.CreateMessageReceivedArgs("other/topic", DataPayload),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicMatchesAny("sensors/#", "alerts/#")
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
    }

    /// <summary>Tests that WhereTopicIsNotMatch excludes matching topics.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicIsNotMatch_ExcludesMatchingTopicsAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(SensorTemperatureTopic, TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("debug/trace", DataPayload),
            TestDataHelpers.CreateMessageReceivedArgs(SensorHumidityTopic, HumidityPayload),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicIsNotMatch("debug/#")
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
    }

    /// <summary>Tests that ExtractTopicValues extracts values correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ExtractTopicValues_ExtractsValuesAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/temp123/readings/celsius", TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/hum456/readings/percent", HumidityPayload),
        };

        var results = new List<(MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)>();

        // Act
        using var subscription = messages.ToObservable()
            .ExtractTopicValues("sensors/{sensorId}/readings/{unit}")
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
        await Assert.That(results[FirstResultIndex].Values[SensorIdentifierKey]).IsEqualTo("temp123");
        await Assert.That(results[FirstResultIndex].Values["unit"]).IsEqualTo("celsius");
        await Assert.That(results[SecondResultIndex].Values[SensorIdentifierKey]).IsEqualTo("hum456");
    }

    /// <summary>Tests that WhereTopicLevelCount filters by level count.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task WhereTopicLevelCount_FiltersByLevelCountAsync()
    {
        // Arrange
        const int threeTopicLevels = 3;
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("a/b/c", DataPayload),
            TestDataHelpers.CreateMessageReceivedArgs("a/b", DataPayload),
            TestDataHelpers.CreateMessageReceivedArgs("a/b/c/d", DataPayload),
            TestDataHelpers.CreateMessageReceivedArgs("x/y/z", DataPayload),
        };

        var results = new List<MqttApplicationMessageReceivedEventArgs>();

        // Act
        using var subscription = messages.ToObservable()
            .WhereTopicLevelCount(threeTopicLevels)
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedDoubleResultCount);
    }

    /// <summary>Tests that SelectTopicLevel extracts specific level.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task SelectTopicLevel_ExtractsSpecificLevelAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(LivingRoomTemperatureTopic, TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs(KitchenHumidityTopic, HumidityPayload),
            TestDataHelpers.CreateMessageReceivedArgs("home/bedroom/lights", "on"),
        };

        var results = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .SelectTopicLevel(RoomTopicLevel) // Get the room names
            .Subscribe(results.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(results).Count().IsEqualTo(ExpectedTripleResultCount);
        await Assert.That(results).Contains("living");
        await Assert.That(results).Contains("kitchen");
        await Assert.That(results).Contains("bedroom");
    }

    /// <summary>Tests that GroupByTopic groups messages correctly.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GroupByTopic_GroupsMessagesCorrectlyAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs(FirstTopic, "a"),
            TestDataHelpers.CreateMessageReceivedArgs(SecondTopic, "b"),
            TestDataHelpers.CreateMessageReceivedArgs(FirstTopic, "c"),
            TestDataHelpers.CreateMessageReceivedArgs(SecondTopic, "d"),
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopic()
            .Select(static group => group.Key)
            .Subscribe(groupKeys.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(ExpectedDoubleResultCount);
        await Assert.That(groupKeys).Contains(FirstTopic);
        await Assert.That(groupKeys).Contains(SecondTopic);
    }

    /// <summary>Tests that GroupByTopicLevel groups by specific level.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GroupByTopicLevel_GroupsBySpecificLevelAsync()
    {
        // Arrange
        var messages = new[]
        {
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room1/temp", TemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room2/temp", AdditionalTemperaturePayload),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room1/humidity", HumidityPayload),
            TestDataHelpers.CreateMessageReceivedArgs("sensors/room2/humidity", "55"),
        };

        var groupKeys = new List<string>();

        // Act
        using var subscription = messages.ToObservable()
            .GroupByTopicLevel(RoomTopicLevel) // Group by room
            .Select(static group => group.Key)
            .Subscribe(groupKeys.Add);

        await Task.Delay(ObservableProcessingDelayMilliseconds);

        // Assert
        await Assert.That(groupKeys).Count().IsEqualTo(ExpectedDoubleResultCount);
        await Assert.That(groupKeys).Contains("room1");
        await Assert.That(groupKeys).Contains("room2");
    }
}
