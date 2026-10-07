// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies observed topic nodes preserve distinct levels and reuse exact topics.</summary>
public sealed class TopicNodeViewModelTests
{
    /// <summary>Stores the number of ordinally distinct topic names.</summary>
    private const int DistinctTopicCount = 2;

    /// <summary>Checks empty leading and interior levels preserve the full published topic.</summary>
    /// <param name="topic">The MQTT topic with empty levels.</param>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    [Arguments("/plant/value")]
    [Arguments("//plant")]
    [Arguments("plant//value")]
    public async Task GetOrAddPreservesLeadingAndConsecutiveEmptyLevelsAsync(string topic)
    {
        var root = new TopicNodeViewModel("topics", string.Empty, isRoot: true);
        var current = root;
        foreach (var part in topic.Split('/'))
        {
            current = current.GetOrAdd(part);
        }

        await Assert.That(current.FullTopic).IsEqualTo(topic);
        await Assert.That(root.FullTopic).IsEqualTo(string.Empty);
    }

    /// <summary>Checks child topics use ordinal names and display empty levels without discarding them.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task GetOrAddReusesExactTopicsAndPreservesEmptyLevelsAsync()
    {
        var root = new TopicNodeViewModel("topics", string.Empty, isRoot: true);
        var plant = root.GetOrAdd("plant");
        await Assert.That(root.GetOrAdd("plant")).IsSameReferenceAs(plant);
        var differentCase = root.GetOrAdd("Plant");
        await Assert.That(differentCase.FullTopic).IsEqualTo("Plant");
        await Assert.That(root.Children).Count().IsEqualTo(DistinctTopicCount);
        var empty = plant.GetOrAdd(string.Empty);
        await Assert.That(empty.Name).IsEqualTo("(empty)");
        await Assert.That(empty.FullTopic).IsEqualTo("plant/");
        await Assert.That(empty.GetOrAdd("value").FullTopic).IsEqualTo("plant//value");
        await Assert.That(plant.GetOrAdd(string.Empty)).IsSameReferenceAs(empty);
    }
}
