![License](https://img.shields.io/github/license/ChrisPulman/MQTTnet.Rx.svg)
[![Build](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml)

#### MQTTnet.Rx.Client
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.Client?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.Client.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.Client)

#### MQTTnet.Rx.Server
![Nuget](https://img.shields.io/nuget/dt/MQTTnet.Rx.Server?color=pink&style=plastic)
[![NuGet](https://img.shields.io/nuget/v/MQTTnet.Rx.Server.svg?style=plastic)](https://www.nuget.org/packages/MQTTnet.Rx.Server)

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

# MQTTnet.Rx
Reactive extensions and helpers for MQTTnet (v5) that make it simple to build event-driven MQTT clients and servers using IObservable streams.

- Targets .NET 8 and .NET 9
- Based on MQTTnet 5.x and System.Reactive
- Client and Server wrappers with rich observable APIs
- Auto-reconnect Resilient client (replacement for ManagedClient)
- Topic discovery and JSON helpers
- Optional integration packages (Modbus, SerialPort, S7 PLC, Allen-Bradley PLC, TwinCAT)

Note on ManagedClient: Support for ManagedClient is removed because MQTTnet v5 no longer includes it. Use the Resilient client in MQTTnet.Rx.Client instead.

## Packages
- MQTTnet.Rx.Client – Reactive MQTT client helpers (raw and resilient)
- MQTTnet.Rx.Server – Reactive MQTT server helpers
- MQTTnet.Rx.Modbus – Publish Modbus values via MQTT using ModbusRx.Reactive
- MQTTnet.Rx.SerialPort – Publish/consume serial data via MQTT with CP.IO.Ports
- MQTTnet.Rx.S7Plc – Publish/subscribe S7 PLC tags via MQTT with S7PlcRx
- MQTTnet.Rx.ABPlc – Publish/subscribe Allen-Bradley PLC tags via MQTT with ABPlcRx
- MQTTnet.Rx.TwinCAT – Publish/subscribe TwinCAT tags via MQTT with CP.TwinCatRx

## Install
```bash
# Pick what you need
 dotnet add package MQTTnet.Rx.Client
 dotnet add package MQTTnet.Rx.Server
 dotnet add package MQTTnet.Rx.Modbus
 dotnet add package MQTTnet.Rx.SerialPort
 dotnet add package MQTTnet.Rx.S7Plc
 dotnet add package MQTTnet.Rx.ABPlc
 dotnet add package MQTTnet.Rx.TwinCAT
```

---

## Quick start – Client (raw)
Publish an observable stream and subscribe to a topic.

```csharp
using System.Reactive.Subjects;
using MQTTnet.Rx.Client;

var messages = new Subject<(string topic, string payload)>();

// Connect and publish
var publishSub = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishMessage(messages)
    .Subscribe(r => Console.WriteLine($"PUB: {r.ReasonCode} [{r.PacketIdentifier}]"));

// Subscribe to a topic (supports wildcards + and #)
var subscribeSub = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("sensors/+/temp")
    .Subscribe(m => Console.WriteLine($"SUB: {m.ApplicationMessage.Topic} => {m.ApplicationMessage.ConvertPayloadToString()}"));

// Emit some messages
messages.OnNext(("sensors/kitchen/temp", "21.5"));
messages.OnNext(("sensors/lab/temp", "19.9"));
```

## Quick start – Resilient client (auto-reconnect)
The resilient client stays connected and queues outbound messages while reconnecting.

```csharp
using System.Reactive.Subjects;
using MQTTnet.Rx.Client;

var messages = new Subject<(string topic, string payload)>();

var resilientPubSub = Create.ResilientMqttClient()
    .WithResilientClientOptions(o =>
        o.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
         .WithClientOptions(c => c.WithTcpServer("localhost", 1883).WithClientId("app-1")))
    .PublishMessage(messages)
    .Subscribe(e => Console.WriteLine($"PUB: {e.ApplicationMessage.Id} [{e.ApplicationMessage.ApplicationMessage?.Topic}]"));

var resilientSub = Create.ResilientMqttClient()
    .WithResilientClientOptions(o =>
        o.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
         .WithClientOptions(c => c.WithTcpServer("localhost", 1883).WithClientId("app-2")))
    .SubscribeToTopic("devices/#")
    .Subscribe(m => Console.WriteLine($"SUB: {m.ApplicationMessage.Topic} => {m.ApplicationMessage.ConvertPayloadToString()}"));

// Emit messages
messages.OnNext(("devices/alpha", "hello"));
```

### Resilient client event streams
```csharp
var client = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithClientOptions(c => c.WithTcpServer("localhost", 1883)));

var events = client.Subscribe(c =>
{
    var d1 = c.Connected.Subscribe(_ => Console.WriteLine("Connected"));
    var d2 = c.Disconnected.Subscribe(_ => Console.WriteLine("Disconnected"));
    var d3 = c.ApplicationMessageReceived.Subscribe(m => Console.WriteLine($"RX: {m.ApplicationMessage.Topic}"));
});
```

---

## JSON helpers and topic discovery
Use ToDictionary to parse JSON payloads and Observe to extract keys as typed streams.

```csharp
using MQTTnet.Rx.Client;

var d = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("telemetry/#")
    .ToDictionary()
    .Subscribe(dict =>
    {
        if (dict != null)
        {
            Console.WriteLine(string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}")));
        }
    });

// Observe specific keys with conversion helpers
var temperature = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("telemetry/room1")
    .ToDictionary()
    .Observe("temperature")
    .ToDouble()
    .Subscribe(t => Console.WriteLine($"Temp: {t:0.0}"));
```

Discover topics seen by the client (with optional expiry window):

```csharp
var discovery = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .DiscoverTopics(TimeSpan.FromMinutes(5))
    .Subscribe(topics =>
    {
        Console.WriteLine("Known topics:");
        foreach (var (topic, lastSeen) in topics)
        {
            Console.WriteLine($" - {topic} (last seen: {lastSeen:O})");
        }
    });
```

---

## 📚 Advanced publish options
All publish helpers accept QoS and retain flags, with overloads for string or byte[] payloads.

```csharp
using MQTTnet.Protocol;

var msgs = new Subject<(string topic, string payload)>();

var pub = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishMessage(msgs, qos: MqttQualityOfServiceLevel.AtLeastOnce, retain: false)
    .Subscribe(r => Console.WriteLine($"Sent: {r.ReasonCode}"));

// Custom message builder (add user properties etc.)
var pub2 = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishMessage(
        msgs,
        messageBuilder: b => b.WithUserProperty("app", "demo"))
    .Subscribe();
```

---

## Server – Reactive MQTT broker
Spin up an in-process MQTT broker and subscribe to server-side events with IObservable.

```csharp
using MQTTnet.Rx.Server;

var server = Create.MqttServer(builder =>
        builder.WithDefaultEndpointPort(2883)
               .WithDefaultEndpoint()
               .Build())
    .Subscribe(async sub =>
    {
        // Subscribe to server events
        sub.Disposable.Add(sub.Server.ClientConnected().Subscribe(e => Console.WriteLine($"SERVER: Connected {e.ClientId}")));
        sub.Disposable.Add(sub.Server.ClientDisconnected().Subscribe(e => Console.WriteLine($"SERVER: Disconnected {e.ClientId}")));
        sub.Disposable.Add(sub.Server.InterceptingPublish().Subscribe(e => Console.WriteLine($"SERVER: Publish {e.ApplicationMessage.Topic}")));
    });
```

---

## 🏭 Diagnostics
- Topic matching: SubscribeToTopic supports wildcards (+ and #). Internally, topics are matched using MqttTopicFilterComparer.
- Duplicate subscriptions: SubscribeToTopic reference-counts identical topic filters per client and unsubscribes when the last observer disposes.
- Packet inspection (raw client):

```csharp
var c = Create.MqttClient().WithClientOptions(o => o.WithTcpServer("localhost", 1883));
var sniff = c.Subscribe(cli => cli.InspectPackage().Subscribe(p => Console.WriteLine($"{p.Direction} {p.Packet}") ));
```

---

## Integration packages
These packages provide focused helpers to bridge other systems to MQTT using observables. Refer to the respective package documentation for how to create and configure the underlying connections/clients.

### Modbus (MQTTnet.Rx.Modbus)
Publish Modbus registers/coils/inputs as MQTT messages. The helpers extend an IObservable<(bool connected, Exception? error, ModbusIpMaster? master)> from ModbusRx.Reactive:

```csharp
using MQTTnet.Rx.Modbus;

// modbusMaster is an observable from ModbusRx.Reactive that yields connection state and a ModbusIpMaster
IObservable<(bool connected, Exception? error, ModbusRx.Device.ModbusIpMaster? master)> modbusMaster = /* create with ModbusRx.Reactive */;

var pub1 = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishInputRegisters(modbusMaster, topic: "modbus/inputregs", startAddress: 0, numberOfPoints: 8, interval: 250)
    .Subscribe();

var pub2 = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishHoldingRegisters(modbusMaster, topic: "modbus/holdingregs", startAddress: 0, numberOfPoints: 8, interval: 500)
    .Subscribe();
```

### Serial Port (MQTTnet.Rx.SerialPort)
Publish framed serial data to MQTT using CP.IO.Ports ISerialPortRx.

```csharp
using CP.IO.Ports;
using MQTTnet.Rx.SerialPort;

ISerialPortRx port = /* create and configure an ISerialPortRx (e.g., COM port, baud, parity) */;

var serialPub = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishSerialPort(topic: "serial/data", serialPort: port, startsWith: Observable.Return('<'), endsWith: Observable.Return('>'), timeOut: 1000)
    .Subscribe();
```

### PLCs – S7, Allen-Bradley, TwinCAT
Each PLC package adds helpers to publish or subscribe PLC tags via MQTT using the respective Rx client libraries (S7PlcRx, ABPlcRx, CP.TwinCatRx). Example signatures include:

- MQTTnet.Rx.S7Plc: PublishS7PlcTag<T>(client, topic, plcVariable, configurePlc)
- MQTTnet.Rx.ABPlc: PublishABPlcTag<T>(client, topic, plcVariable, configurePlc)
- MQTTnet.Rx.TwinCAT: PublishTcPlcTag<T>(client, topic, plcVariable, configurePlc)

Refer to those libraries for creating and configuring connected PLC clients, then use the Publish* helpers to push tag changes to MQTT. You can also combine SubscribeToTopic with your PLC client to write incoming values back to tags.

---

## End-to-end sample (server + resilient clients)
```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Server;

var serverPort = 2883;

// Start server
var server = Create.MqttServer(b => b.WithDefaultEndpointPort(serverPort).WithDefaultEndpoint().Build())
    .Subscribe(sub =>
    {
        sub.Disposable.Add(sub.Server.ClientConnected().Subscribe(e => Console.WriteLine($"SERVER: Client connected {e.ClientId}")));
        sub.Disposable.Add(sub.Server.ClientDisconnected().Subscribe(e => Console.WriteLine($"SERVER: Client disconnected {e.ClientId}")));
    });

// Client 1 – subscriber
var c1 = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithAutoReconnectDelay(TimeSpan.FromSeconds(2))
                                      .WithClientOptions(c => c.WithTcpServer("localhost", serverPort)));

var s1 = c1.SubscribeToTopic("demo/#")
    .Subscribe(m => Console.WriteLine($"C1: {m.ApplicationMessage.Topic} => {m.ApplicationMessage.ConvertPayloadToString()}"));

// Client 2 – publisher
var messages = new Subject<(string topic, string payload)>();
var c2 = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithAutoReconnectDelay(TimeSpan.FromSeconds(2))
                                      .WithClientOptions(c => c.WithTcpServer("localhost", serverPort).WithClientId("publisher")));

var p2 = c2.PublishMessage(messages).Subscribe();

messages.OnNext(("demo/FromMilliseconds", "{" + $"payload:{Environment.TickCount}" + "}"));
```

---

## 📖 API reference (high-level)
- Create.MqttClient(): IObservable<IMqttClient>
- Create.ResilientMqttClient(): IObservable<IResilientMqttClient>
- WithClientOptions(Action<MqttClientOptionsBuilder>) ensures connected client and emits it
- WithResilientClientOptions(Action<ResilientMqttClientOptionsBuilder>) starts the resilient client and emits it
- PublishMessage(...) overloads for IMqttClient and IResilientMqttClient
- SubscribeToTopic(string topic) for IMqttClient and IResilientMqttClient
- DiscoverTopics(TimeSpan? expiry)
- JSON: ToDictionary(), Observe(key), ToBool/ToInt16/ToInt32/ToInt64/ToSingle/ToDouble/ToString
- Server: Create.MqttServer(Func<MqttServerOptionsBuilder, MqttServerOptions>) and many server event streams (ClientConnected, ClientDisconnected, InterceptingPublish, etc.)

---

## Notes
- All observable helpers use Retry() where appropriate to keep streams alive.
- Dispose subscriptions returned from Subscribe(...) to cleanly unsubscribe and allow helper logic to ref-count subscriptions and perform Unsubscribe at zero subscribers.
- QoS defaults to ExactlyOnce and retain defaults to true; override as needed.

## Contributing
Issues and PRs are welcome.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


---

**MQTTnet.Rx** - Empowering Industrial Automation with Reactive Technology ⚡🏭
