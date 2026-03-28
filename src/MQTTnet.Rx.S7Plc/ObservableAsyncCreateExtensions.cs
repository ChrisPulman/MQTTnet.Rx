// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client;
using ReactiveUI.Extensions.Async;
using S7PlcRx;

#pragma warning disable SA1600

namespace MQTTnet.Rx.S7Plc;

/// <summary>
/// Provides asynchronous observable counterparts for S7 PLC MQTT helpers.
/// </summary>
public static class ObservableAsyncCreateExtensions
{
    public static IObservableAsync<MqttClientPublishResult> PublishS7PlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishS7PlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeS7PlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeS7PlcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishS7PlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeS7PlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxS7> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeS7PlcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }
}
