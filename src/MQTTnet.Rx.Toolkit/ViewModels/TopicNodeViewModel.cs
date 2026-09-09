// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Represents one node in the observed MQTT topic tree.</summary>
/// <param name="name">The display name for this topic node.</param>
/// <param name="fullTopic">The complete topic represented by this node.</param>
internal sealed partial class TopicNodeViewModel(string name, string fullTopic) : ViewModelBase
{
    /// <summary>Stores the display name for this topic level.</summary>
    [Reactive]
    private string _name = name;

    /// <summary>Stores the full topic represented by this node.</summary>
    [Reactive]
    private string _fullTopic = fullTopic;

    /// <summary>Stores the last payload observed for this topic.</summary>
    [Reactive]
    private string _lastPayload = string.Empty;

    /// <summary>Stores the last timestamp observed for this topic.</summary>
    [Reactive]
    private DateTimeOffset? _lastSeen;

    /// <summary>Stores the number of messages observed for this topic.</summary>
    [Reactive]
    private long _messageCount;

    /// <summary>Gets the child topic levels for this node.</summary>
    public ObservableCollection<TopicNodeViewModel> Children { get; } = [];

    /// <summary>Gets an existing child node or creates one for the supplied topic segment.</summary>
    /// <param name="childSegment">The child topic segment, including an empty segment when MQTT uses an empty level.</param>
    /// <returns>The existing or newly created child node.</returns>
    internal TopicNodeViewModel GetOrAdd(string childSegment)
    {
        var childTopic = string.IsNullOrEmpty(FullTopic) ? childSegment : $"{FullTopic}/{childSegment}";
        foreach (var child in Children)
        {
            if (string.Equals(child.FullTopic, childTopic, StringComparison.Ordinal))
            {
                return child;
            }
        }

        var displayName = childSegment.Length == 0 ? "(empty)" : childSegment;
        var created = new TopicNodeViewModel(displayName, childTopic);
        Children.Add(created);
        return created;
    }
}
