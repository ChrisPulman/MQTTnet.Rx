// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Server;

IDisposable? disposable = default;
var serverPort = 2883;

disposable =
MQTTnet.Rx.Server.Create.MqttServer(builder => builder.WithDefaultEndpointPort(serverPort).WithDefaultEndpoint().Build())
      .Subscribe(async sub =>
      {
          sub.Disposable.Add(sub.Server.ClientConnected().Subscribe(args => Console.WriteLine($"SERVER: ClientConnectedAsync => clientId:{args.ClientId}")));
          sub.Disposable.Add(sub.Server.ClientDisconnected().Subscribe(args => Console.WriteLine($"SERVER: ClientDisconnectedAsync => clientId:{args.ClientId}")));

          var obsClient1 = MQTTnet.Rx.Client.Create.ResilientMqttClient()
                          .WithResilientClientOptions(options =>
                            options.WithClientOptions(c =>
                                        c.WithTcpServer("localhost", serverPort))
                                   .WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));

          var obsClient2 = MQTTnet.Rx.Client.Create.ResilientMqttClient()
                                .WithResilientClientOptions(options =>
                                  options.WithClientOptions(c =>
                                              c.WithTcpServer("localhost", serverPort)
                                               .WithClientId("Client02"))
                                         .WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));
          sub.Disposable.Add(
          obsClient1.Subscribe(i =>
          {
              sub.Disposable.Add(i.Connected.Subscribe((_) =>
                  Console.WriteLine($"{DateTime.Now.Dump()}\t CLIENT: Connected with server.")));
              sub.Disposable.Add(i.Disconnected.Subscribe((_) =>
                  Console.WriteLine($"{DateTime.Now.Dump()}\t CLIENT: Disconnected with server.")));
          }));

          var s1 = obsClient1.SubscribeToTopic("FromMilliseconds/#")
                   .Subscribe(r => Console.WriteLine($"\tCLIENT S1 #: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

          var s2 = obsClient1.SubscribeToTopic("FromMilliseconds/+/+/abc")
                   .Subscribe(r => Console.WriteLine($"\tCLIENT S2 /+/+/: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

          var s3 = obsClient1.SubscribeToTopic("FromMilliseconds/+")
                   .Subscribe(r => Console.WriteLine($"\tCLIENT S3 +: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

          var s4 = obsClient2.SubscribeToTopic("FromMilliseconds/1/+/abc")
                   .Subscribe(r => Console.WriteLine($"\tCLIENT S4 /+/: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

          var s5 = obsClient1.SubscribeToTopic("FromMilliseconds/2/+/abc")
                   .Subscribe(r => Console.WriteLine($"\tCLIENT S5 /+/: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

          Subject<(string topic, string payload)> message = new();

          sub.Disposable.Add(Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(i => message.OnNext(("FromMilliseconds/1/xyz/abc", "{" + $"payload: {i}" + "}"))));
          sub.Disposable.Add(obsClient1.PublishMessage(message).Subscribe());

          Subject<(string topic, string payload)> message1 = new();

          sub.Disposable.Add(Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(i => message.OnNext(("FromMilliseconds/2/zyx/abc", "{" + $"payload: {i}" + "}"))));
          sub.Disposable.Add(obsClient1.PublishMessage(message1).Subscribe());

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
          Console.Read();
          s5.Dispose();
          Console.WriteLine("Dispose S5 ---------------");
      });
Console.WriteLine("Press 'Escape' or 'Q' to exit.");

while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.Q))
{
    Thread.Sleep(1);
}

Console.WriteLine("Disposing Server ---------------");
disposable?.Dispose();
Console.WriteLine("Disposed Server ---------------");
