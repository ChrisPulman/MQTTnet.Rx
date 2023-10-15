// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet;
using MQTTnet.Rx.Client;
using MQTTnet.Server;

var serverPort = 2883;
var mqttServerOptions = new MqttServerOptionsBuilder()
                        .WithDefaultEndpointPort(serverPort)
                        .WithDefaultEndpoint().Build();

var mqttFactory = new MqttFactory();
var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);
mqttServer.ClientConnectedAsync += (args) =>
{
    Console.WriteLine($"SERVER: ClientConnectedAsync => clientId:{args.ClientId}");
    return Task.CompletedTask;
};
mqttServer.ClientDisconnectedAsync += (args) =>
{
    Console.WriteLine($"SERVER: ClientDisconnectedAsync => clientId:{args.ClientId}");
    return Task.CompletedTask;
};
Console.WriteLine("Starting MQTT server...");
await mqttServer.StartAsync();
Console.WriteLine("MQTT server is started.");
Console.WriteLine("------------------------------------\n");

var obsClient = Create.ManagedMqttClient()
                      .WithManagedClientOptions(options =>
                        options.WithClientOptions(c =>
                                    c.WithTcpServer("localhost", serverPort))
                                ////.WithClientId("Client01"))
                               .WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));

obsClient.Subscribe(i =>
{
    i.Connected().Subscribe((_) =>
        Console.WriteLine($"{DateTime.Now.Dump()}\t {_.ConnectResult.AssignedClientIdentifier} CLIENT: Connected with server."));
    i.Disconnected().Subscribe((_) =>
        Console.WriteLine($"{DateTime.Now.Dump()}\t CLIENT: Disconnected with server."));
});
var subClient = obsClient.Publish().RefCount();
var s1 = subClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S1: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

var s2 = obsClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S2: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

var s3 = obsClient.SubscribeToTopic("FromMilliseconds")
         .Subscribe(r => Console.WriteLine($"\tCLIENT S3: {r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));

Subject<(string topic, string payload)> message = new();

Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(i => message.OnNext(("FromMilliseconds", "{" + $"payload: {i}" + "}")));

obsClient.PublishMessage(message).Subscribe(r =>
{
    // Console.WriteLine($"Publish result [{r.Exception == null}], Id:{r.ApplicationMessage.Id}, Topic:{r.ApplicationMessage.ApplicationMessage.Topic}");
});

await Task.Delay(3000);
s2.Dispose();
Console.WriteLine("Dispose S2 ---------------");
await Task.Delay(5000);
s1.Dispose();
Console.WriteLine("Dispose S1 ---------------");
Console.Read();
