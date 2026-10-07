// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI.Primitives;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies editor commands and selected-topic state transitions.</summary>
public sealed partial class MainWindowViewModelTests
{
    /// <summary>Stores the isolated layout filename.</summary>
    private const string LayoutFileName = "layout.json";

    /// <summary>Stores the primary observed telemetry topic.</summary>
    private const string ObservedTopic = "plant/value";

    /// <summary>Stores the alternate message selection topic.</summary>
    private const string OtherObservedTopic = "other/value";

    /// <summary>Checks row commands update the editor collections and preserve authentication editor values.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task EditorCommandsAddAndRemoveRowsAsync()
    {
        await using var model = CreateModel();
        await model.AddConnectionUserPropertyCommand.Execute().FirstAsync();
        await model.AddWillUserPropertyCommand.Execute().FirstAsync();
        await model.AddPublishUserPropertyCommand.Execute().FirstAsync();
        await model.AddSubscribeUserPropertyCommand.Execute().FirstAsync();
        model.Connection.EnhancedAuthenticationStepData = "hello";
        model.Connection.EnhancedAuthenticationStepDataFormat = PayloadFormat.Utf8Text;
        model.Connection.EnhancedAuthenticationStepReason = "challenge";
        await model.AddEnhancedAuthenticationStepCommand.Execute().FirstAsync();

        await Assert.That(model.Connection.UserProperties).Count().IsEqualTo(1);
        await Assert.That(model.Connection.WillUserProperties).Count().IsEqualTo(1);
        await Assert.That(model.Publisher.UserProperties).Count().IsEqualTo(1);
        await Assert.That(model.Subscription.UserProperties).Count().IsEqualTo(1);
        var step = model.Connection.EnhancedAuthenticationSteps[0];
        await Assert.That(step.Data).IsEqualTo("hello");
        await Assert.That(step.DataFormat).IsEqualTo(PayloadFormat.Utf8Text);
        await Assert.That(step.Reason).IsEqualTo("challenge");

        await model.RemoveConnectionUserPropertyCommand.Execute(model.Connection.UserProperties[0]).FirstAsync();
        await model.RemoveWillUserPropertyCommand.Execute(model.Connection.WillUserProperties[0]).FirstAsync();
        await model.RemovePublishUserPropertyCommand.Execute(model.Publisher.UserProperties[0]).FirstAsync();
        await model.RemoveSubscribeUserPropertyCommand.Execute(model.Subscription.UserProperties[0]).FirstAsync();
        await model.RemoveEnhancedAuthenticationStepCommand.Execute(step).FirstAsync();
        await Assert.That(model.Connection.UserProperties).IsEmpty();
        await Assert.That(model.Connection.WillUserProperties).IsEmpty();
        await Assert.That(model.Publisher.UserProperties).IsEmpty();
        await Assert.That(model.Subscription.UserProperties).IsEmpty();
        await Assert.That(model.Connection.EnhancedAuthenticationSteps).IsEmpty();
    }

    /// <summary>Checks message and tree selection remain mutually exclusive and copy into both editors.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task TopicSelectionCopiesTopicAndClearsOtherSelectionAsync()
    {
        await using var model = CreateModel();
        var node = model.Topics[0].GetOrAdd("plant").GetOrAdd("value");
        var message = CreateMessage(OtherObservedTopic, "42", PayloadFormat.Number);
        model.SelectedMessage = message;
        model.SelectedMessage = message;
        model.SelectedTopicNode = node;
        model.SelectedTopicNode = node;
        await Assert.That(model.SelectedMessage).IsNull();
        await model.UseSelectedTopicCommand.Execute().FirstAsync();
        await Assert.That(model.Publisher.Topic).IsEqualTo(ObservedTopic);
        await Assert.That(model.Subscription.TopicFilter).IsEqualTo(ObservedTopic);
        await Assert.That(model.SelectedTopic).IsEqualTo(ObservedTopic);
        model.SelectedMessage = message;
        await Assert.That(model.SelectedTopicNode).IsNull();
        await model.UseSelectedTopicCommand.Execute().FirstAsync();
        await Assert.That(model.Publisher.Topic).IsEqualTo(OtherObservedTopic);
        model.SelectedMessage = null;
        model.SelectedTopicNode = model.Topics[0];
        await model.UseSelectedTopicCommand.Execute().FirstAsync();
        await Assert.That(model.Publisher.Topic).IsEqualTo(OtherObservedTopic);
        model.Messages.Add(message);
        await model.ClearMessagesCommand.Execute().FirstAsync();
        await Assert.That(model.Messages).IsEmpty();
        await Assert.That(model.Topics[0].Children).IsEmpty();
        await Assert.That(model.SelectedMessage).IsNull();
        await Assert.That(model.SelectedTopicNode).IsNull();
    }

    /// <summary>Checks invalid publish input reports validation without starting an MQTT operation.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task PublishValidationDoesNotStartOperationAsync()
    {
        await using var model = CreateModel();
        model.Publisher.Topic = "plant/#";
        await model.PublishCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Disconnected");
        await Assert.That(model.IsBusy).IsFalse();
        await Assert.That(model.LogEntries[0].Source).IsEqualTo("Publish");
        await Assert.That(model.LogEntries[0].Message).IsEqualTo("Publish topics cannot contain MQTT wildcards.");
        await model.SubscribeCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Subscribe failed");
        await model.UnsubscribeCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Unsubscribe failed");
        await model.StartTwinCatBridgeCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Subscribe structure failed");
        await model.StopTwinCatBridgeCommand.Execute().FirstAsync();
        await Assert.That(model.Status).IsEqualTo("Stop structure complete");
    }

    /// <summary>Creates an isolated model with immediate UI dispatch and a missing layout file.</summary>
    /// <returns>The model.</returns>
    private static MainWindowViewModel CreateModel() => new(
        new(TimeProvider.System),
        TimeProvider.System,
        static action => action(),
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), LayoutFileName));

    /// <summary>Creates an inbound message for editor and dashboard behavior.</summary>
    /// <param name="topic">The message topic.</param>
    /// <param name="payload">The displayed payload.</param>
    /// <param name="format">The detected payload format.</param>
    /// <returns>The received message.</returns>
    private static ReceivedMqttMessage CreateMessage(string topic, string payload, PayloadFormat format) => new(
        DateTimeOffset.UnixEpoch,
        "Client received",
        topic,
        payload,
        format,
        MqttQualityOfServiceLevel.AtLeastOnce,
        false,
        payload.Length,
        System.Text.Encoding.UTF8.GetBytes(payload),
        null,
        MqttPayloadFormatIndicator.CharacterData,
        null,
        null,
        0,
        [],
        0,
        false,
        []);
}
