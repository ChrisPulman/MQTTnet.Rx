// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet;
using MQTTnet.Rx.Client;

CompositeDisposable? disposable = default;
var serverPort = 2883;

var server =
Create.MqttServer(builder => builder.WithDefaultEndpointPort(serverPort).WithDefaultEndpoint().Build())
      .Subscribe(sub =>
      {
          disposable = sub.Disposable;
          sub.Disposable.Add(sub.Server.ClientConnected().Subscribe(args => Console.WriteLine($"SERVER: ClientConnectedAsync => clientId:{args.ClientId}")));
          sub.Disposable.Add(sub.Server.ClientDisconnected().Subscribe(args => Console.WriteLine($"SERVER: ClientDisconnectedAsync => clientId:{args.ClientId}")));
      });

var obsClient = Create.ManagedMqttClient()
                      .WithManagedClientOptions(options =>
                        options.WithClientOptions(c =>
                                    c.WithTcpServer("localhost", serverPort))
                               ////.WithClientId("Client01"))
                               .WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));

var obsClient1 = Create.ManagedMqttClient()
                      .WithManagedClientOptions(options =>
                        options.WithClientOptions(c =>
                                    c.WithTcpServer("localhost", serverPort))
                               ////.WithClientId("Client01"))
                               .WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));
disposable!.Add(
obsClient.Subscribe(i =>
{
    disposable.Add(i.Connected().Subscribe((_) =>
        Console.WriteLine($"{DateTime.Now.Dump()}\t CLIENT: Connected with server.")));
    disposable.Add(i.Disconnected().Subscribe((_) =>
        Console.WriteLine($"{DateTime.Now.Dump()}\t CLIENT: Disconnected with server.")));
}));

var s1 = obsClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S1: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

var s2 = obsClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S2: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

var s3 = obsClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S3: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

var s4 = obsClient1.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S4: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

Subject<(string topic, string payload)> message = new();

disposable.Add(Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(i => message.OnNext(("FromMilliseconds", "{" + $"payload: {i}" + "}"))));
disposable.Add(obsClient.PublishMessage(message).Subscribe());

await Task.Delay(3000);
s2.Dispose();
Console.WriteLine("Dispose S2 ---------------");
await Task.Delay(5000);
s1.Dispose();
Console.WriteLine("Dispose S1 ---------------");
await Task.Delay(5000);
s4.Dispose();
Console.WriteLine("Dispose S4 ---------------");

Console.Read();
s3.Dispose();
Console.WriteLine("Dispose S3 ---------------");

Console.WriteLine("Press 'Escape' or 'Q' to exit.");

while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.Q))
{
    Thread.Sleep(1);
}

Console.WriteLine("Disposing All ---------------");
disposable.Dispose();
Console.WriteLine("Disposed All ---------------");
server.Dispose();
