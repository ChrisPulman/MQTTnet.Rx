// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CP.Collections;
using CP.TwinCatRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.TwinCAT;

/// <summary>
/// Provides asynchronous observable counterparts for TwinCAT MQTT helpers.
/// </summary>
public static class ObservableAsyncCreateExtensions
{
    public static IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishTcPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeTcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeTcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }

    public static IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishTcPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishTcPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeTcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IRxTcAdsClient> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeTcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IHashTableRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishTcPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }
}
