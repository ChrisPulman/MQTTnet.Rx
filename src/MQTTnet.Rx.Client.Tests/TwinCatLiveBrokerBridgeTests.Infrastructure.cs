// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Globalization;
#if REACTIVE_SHIM
using CP.Collections.Reactive;
#else
using CP.Collections;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Reactive;
#else
using IoT.Driver.TwinCATRx;
#endif
#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Core.Reactive;
#else
using IoT.Driver.TwinCATRx.Core;
#endif
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.Reactive.TwinCatRxExtensions;
#else
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.TwinCatRxExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains broker and in-memory-device infrastructure for the TwinCAT live-broker bridge fixture.</summary>
public sealed partial class TwinCatLiveBrokerBridgeTests
{
    /// <summary>Starts the loopback broker and connects both fixture clients.</summary>
    /// <returns>The connected broker fixture.</returns>
    private static async Task<LiveMqttBroker> StartConnectedBrokerAsync()
    {
        var broker = await LiveMqttBroker.StartAsync();
        try
        {
            _ = await broker.ConnectClientsAsync();
            return broker;
        }
        catch
        {
            await broker.DisposeAsync();
            throw;
        }
    }

    /// <summary>Creates a connected in-memory ADS client with one readable and writable symbol.</summary>
    /// <returns>The connected deterministic ADS client.</returns>
    private static InMemoryAdsClient CreateAdsClient()
    {
        var ads = new InMemoryAdsClient();
        var settings = new Settings
        {
            AdsAddress = "in-memory",
            Port = TwinCat3Port,
            SettingsId = "mqtt-live-bridge",
        };
        TwinCatCoreExtensions.AddNotification(settings, AdsVariable);
        TwinCatCoreExtensions.AddWriteVariable(settings, AdsVariable);
        _ = ads.RegisterSymbol(AdsVariable, 0);
        ads.Connect(settings);
        return ads;
    }

    /// <summary>Creates a real reactive hash-table seam with one mutable value.</summary>
    /// <returns>The populated reactive hash table.</returns>
    private static HashTableRx CreateHashTable()
    {
        var table = new HashTableRx(useUpperCase: false);
        table.Add(HashVariable, 0);
        return table;
    }

    /// <summary>Creates a resilient facade that forwards operations to the fixture's real MQTT client.</summary>
    /// <param name="internalClient">The connected real MQTT client.</param>
    /// <param name="processed">The processed-message stream used by resilient publishers.</param>
    /// <returns>The composed resilient test facade.</returns>
    private static IResilientMqttClient CreateLiveResilientClient(
        IMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed)
    {
        var receivedAsync = internalClient.ObserveApplicationMessageReceived();
        var received = receivedAsync.ToObservable();
        var client = Substitute.For<IResilientMqttClient>();
        _ = client.InternalClient.Returns(internalClient);
        _ = client.IsConnected.Returns(_ => internalClient.IsConnected);
        _ = client.IsStarted.Returns(true);
        _ = client.ApplicationMessageProcessed.Returns(processed);
        _ = client.ApplicationMessageProcessedAsyncObservable.Returns(processed.ToSignal());
        _ = client.ApplicationMessageReceived.Returns(received);
        _ = client.ApplicationMessageReceivedAsyncObservable.Returns(receivedAsync);
        _ = client.EnqueueAsync(Arg.Any<MqttApplicationMessage>()).Returns(call =>
            PublishResilientMessageAsync(
                internalClient,
                processed,
                call.Arg<MqttApplicationMessage>() ?? throw new InvalidOperationException(
                    "The resilient facade requires an application message.")));
        _ = client.EnqueueAsync(Arg.Any<ResilientMqttApplicationMessage>()).Returns(call =>
            PublishResilientMessageAsync(
                internalClient,
                processed,
                call.Arg<ResilientMqttApplicationMessage>() ?? throw new InvalidOperationException(
                    "The resilient facade requires a managed application message.")));
        _ = client.SubscribeAsync(Arg.Any<IEnumerable<MqttTopicFilter>>()).Returns(async call =>
        {
            var builder = new MqttClientSubscribeOptionsBuilder();
            var filters = call.Arg<IEnumerable<MqttTopicFilter>>() ?? throw new InvalidOperationException(
                "The resilient facade requires MQTT topic filters.");
            foreach (var filter in filters)
            {
                _ = builder.WithTopicFilter(filter);
            }

            _ = await internalClient.SubscribeAsync(builder.Build(), CancellationToken.None);
        });
        _ = client.UnsubscribeAsync(Arg.Any<IEnumerable<string>>()).Returns(async call =>
        {
            var builder = new MqttClientUnsubscribeOptionsBuilder();
            var topics = call.Arg<IEnumerable<string>>() ?? throw new InvalidOperationException(
                "The resilient facade requires MQTT topics.");
            foreach (var topic in topics)
            {
                _ = builder.WithTopicFilter(topic);
            }

            _ = await internalClient.UnsubscribeAsync(builder.Build(), CancellationToken.None);
        });
        return client;
    }

    /// <summary>Publishes one managed message through a real client and emits resilient completion evidence.</summary>
    /// <param name="internalClient">The connected real MQTT client.</param>
    /// <param name="processed">The processed-message result stream.</param>
    /// <param name="message">The raw application message to publish.</param>
    /// <returns>A task representing network publication and result emission.</returns>
    private static async Task PublishResilientMessageAsync(
        IMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed,
        MqttApplicationMessage message)
    {
        var managed = new ResilientMqttApplicationMessage { ApplicationMessage = message };
        await PublishResilientMessageAsync(internalClient, processed, managed);
    }

    /// <summary>Publishes one managed message through a real client and emits resilient completion evidence.</summary>
    /// <param name="internalClient">The connected real MQTT client.</param>
    /// <param name="processed">The processed-message result stream.</param>
    /// <param name="managed">The resilient application message to publish.</param>
    /// <returns>A task representing network publication and result emission.</returns>
    private static async Task PublishResilientMessageAsync(
        IMqttClient internalClient,
        TestSignal<ApplicationMessageProcessedEventArgs> processed,
        ResilientMqttApplicationMessage managed)
    {
        Exception? failure = null;
        try
        {
            var applicationMessage = managed.ApplicationMessage ?? throw new InvalidOperationException(
                "The resilient message requires an application message.");
            _ = await internalClient.PublishAsync(applicationMessage, CancellationToken.None);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        processed.OnNext(new(managed, failure));
    }

    /// <summary>Publishes an invariant integer payload from the real probe client.</summary>
    /// <param name="broker">The connected real loopback broker.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="value">The invariant integer payload.</param>
    /// <returns>A task representing the probe publication.</returns>
    private static async Task PublishFromProbeAsync(LiveMqttBroker broker, string topic, int value)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(value.ToString(CultureInfo.InvariantCulture))
            .Build();
        _ = await broker.ProbeClient.PublishAsync(message, CancellationToken.None);
    }

    /// <summary>Parses one invariant integer MQTT payload.</summary>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The parsed integer.</returns>
    private static int ParsePayload(string payload) => int.Parse(payload, CultureInfo.InvariantCulture);
}
#endif
