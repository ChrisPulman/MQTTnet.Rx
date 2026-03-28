// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ABPlcRx;
using MQTTnet.Rx.Client;
using ReactiveUI.Extensions.Async;

#pragma warning disable SA1600

namespace MQTTnet.Rx.ABPlc;

/// <summary>
/// Provides asynchronous observable counterparts for Allen-Bradley PLC MQTT helpers.
/// </summary>
public static class ObservableAsyncCreateExtensions
{
    public static IObservableAsync<MqttClientPublishResult> PublishABPlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishABPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeABPlcTag<T>(this IObservableAsync<IMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeABPlcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }

    public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc)
    {
        ArgumentNullException.ThrowIfNull(client);
        return Create.PublishABPlcTag<T>(client.ToObservable(), topic, plcVariable, configurePlc).ToObservableAsync();
    }

    public static void SubscribeABPlcTag<T>(this IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, Action<IABPlcRx> configurePlc, Func<string, T> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        Create.SubscribeABPlcTag(client.ToObservable(), topic, plcVariable, configurePlc, payloadFactory);
    }
}
