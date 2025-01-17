![License](https://img.shields.io/github/license/ChrisPulman/MQTTnet.Rx.svg)
[![Build](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml)

#### MQTTnet.Rx.Client
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.Client?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.Client.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.Client)

#### MQTTnet.Rx.ABPlc
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.ABPlc?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.ABPlc.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.ABPlc)

#### MQTTnet.Rx.Modbus
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.Modbus?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.Modbus.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.Modbus)

#### MQTTnet.Rx.S7Plc
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.S7Plc?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.S7Plc.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.S7Plc)

#### MQTTnet.Rx.SerialPort
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.SerialPort?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.SerialPort.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.SerialPort)

#### MQTTnet.Rx.TwinCAT
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.TwinCAT?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.TwinCAT.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.TwinCAT)


![Alt](https://repobeats.axiom.co/api/embed/c9b84f3200da41856350a888598eae1ca5613ec2.svg "Repobeats analytics image")

<p align="left">
  <a href="https://github.com/ChrisPulman/MQTTnet.Rx">
    <img alt="MQTTnet.Rx" src="https://github.com/ChrisPulman/MQTTnet.Rx/blob/main/Images/logo.png" width="200"/>
  </a>
</p>


# MQTTnet.Rx.Client
A Reactive Client for MQTTnet Broker

## NOTE: ManagedClient support has currently been removed from the MQTTnet.Rx.Client library. We will look into the possibility of adding this functionality in the future. This is due to the fact that the ManagedClient is no longer included in the MQTTnet V5 library.

## Create a Mqtt Client to Publish an Observable stream
```csharp
Create.MqttClient()
    .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
    .PublishMessage(_message)
    .Subscribe(r => Console.WriteLine($"{r.ReasonCode} [{r.PacketIdentifier}]"));
```

## Create a Mqtt Client to Subscribe to a Topic
```csharp
Create.MqttClient()
    .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
    .SubscribeToTopic("FromMilliseconds")
    .Subscribe(r => Console.WriteLine($"{r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"));
```

## MQTTnet.Rx.Server
A Reactive Server for MQTTnet Broker

## Create a Mqtt Server
```csharp
Create.MqttServer(builder =>
        builder
        .WithDefaultEndpointPort(2883)
        .WithDefaultEndpoint()
        .Build())
      .Subscribe();
```
