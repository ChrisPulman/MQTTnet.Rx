// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Reactive.Signals;

namespace TestSingleClient;

/// <summary>Publishes two retained observable message streams to a local MQTT broker.</summary>
internal static class Program
{
    /// <summary>Gets the local MQTT broker port.</summary>
    private const int BrokerPort = 2883;

    /// <summary>Starts the sample and keeps its observable subscriptions alive until a key is pressed.</summary>
    internal static void Main()
    {
        using var disposables = new MultipleDisposable();
        var client = Create.ResilientMqttClient()
            .WithResilientClientOptions(static options =>
                options.WithClientOptions(static settings =>
                    settings.WithTcpServer("localhost", BrokerPort)));
        var message = new ReplaySignal<(string Topic, string Payload)>(0);
        var message1 = new ReplaySignal<(string Topic, string Payload)>(0);

        disposables.Add(Signal.Every(TimeSpan.FromSeconds(1)).Subscribe(index =>
            message.OnNext(("FromMilliseconds/1/xyz/abc", $"{{payload: {index}}}"))));
        disposables.Add(client.PublishMessage(message).Subscribe());
        disposables.Add(Signal.Every(TimeSpan.FromSeconds(1)).Subscribe(index =>
            message1.OnNext(("FromMilliseconds/2/zyx/abc", $"{{payload: {index}}}"))));
        disposables.Add(client.PublishMessage(message1).Subscribe());

        var _ = Console.ReadKey(intercept: true);
    }
}
