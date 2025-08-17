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

### Binary payloads (byte[])
```csharp
using System.Reactive.Subjects;
using MQTTnet.Rx.Client;

// Publish byte[] payloads with raw client
var bytes = new Subject<(string topic, byte[] payload)>();
var pubBytes = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishMessage(bytes)
    .Subscribe();

bytes.OnNext(("images/frame", new byte[] { 0x01, 0x02, 0x03 }));

// Subscribe and access raw bytes (zero-copy helpers)
var subBytes = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("images/#")
    .Subscribe(m =>
    {
        // Avoid ToArray: use Payload() or PayloadUtf8()
        var payload = m.Payload(); // ReadOnlyMemory<byte>
        Console.WriteLine($"Got {payload.Length} bytes from {m.ApplicationMessage.Topic}");
    });

// Resilient client publish with byte[]
var pubBytesResilient = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithClientOptions(c => c.WithTcpServer("localhost", 1883)))
    .PublishMessage(bytes)
    .Subscribe();
```

### Retained messages (server-side and client behavior)
- When publishing with retain = true, the broker stores the last payload for the topic and delivers it to new subscribers.
- To clear a retained message, publish an empty payload with retain = true to that topic.
- Broker events for diagnostics are exposed via MQTTnet.Rx.Server.

```csharp
using MQTTnet.Protocol;
using MQTTnet.Rx.Server;

// Publish retained and later clear it
var msgsRetain = new Subject<(string topic, string payload)>();
var pubRetained = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishMessage(msgsRetain, qos: MqttQualityOfServiceLevel.AtLeastOnce, retain: true)
    .Subscribe();

msgsRetain.OnNext(("status/line1", "online"));
// Clear the retained message (per MQTT spec -> empty payload + retain)
msgsRetain.OnNext(("status/line1", string.Empty));

// Server-side: observe retained message lifecycle
var server = MQTTnet.Rx.Server.Create.MqttServer(b => b.WithDefaultEndpointPort(2883).WithDefaultEndpoint().Build())
    .Subscribe(sub =>
    {
        sub.Disposable.Add(sub.Server.RetainedMessageChanged().Subscribe(e => Console.WriteLine($"Retained changed: {e.ApplicationMessage.Topic}")));
        sub.Disposable.Add(sub.Server.RetainedMessagesCleared().Subscribe(_ => Console.WriteLine("All retained cleared")));
        sub.Disposable.Add(sub.Server.LoadingRetainedMessage().Subscribe(_ => Console.WriteLine("Loading retained at startup")));
    });
```

---

## 🔐 TLS/SSL configuration (server and client)
This section shows how to enable TLS on the MQTT broker and connect securely from clients using MQTTnet v5.

### 1) Prepare certificates
- Create or obtain a server certificate (PFX with private key). For development you can use a self-signed cert.
- Optionally, create a client certificate (PFX) if you want mutual TLS (client authentication).
- Ensure the issuing CA (or the self-signed cert) is trusted on the machines running the client(s).

### 2) Enable TLS on the broker
```csharp
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Rx.Server;

var serverCert = new X509Certificate2("server.pfx", "pfx-password");

var server = Create.MqttServer(b =>
        b.WithDefaultEndpoint(false)                // disable plain TCP endpoint
         .WithEncryptedEndpoint()                   // enable TLS endpoint
         .WithEncryptedEndpointPort(8883)          // standard MQTT over TLS port
         .WithEncryptionCertificate(serverCert)    // use your server certificate
         .WithEncryptionSslProtocol(SslProtocols.Tls12)
         .Build())
    .Subscribe(sub =>
    {
        sub.Disposable.Add(sub.Server.Started().Subscribe(_ => Console.WriteLine("Secure broker started on 8883")));
        // Optional: validate connecting clients, enforce client certs, etc.
        sub.Disposable.Add(sub.Server.ValidatingConnection().Subscribe(args =>
        {
            // Example: require TLS
            if (!args.IsSecureConnection)
            {
                args.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.ClientIdentifierNotValid;
            }
        }));
    });
```

Notes:
- WithDefaultEndpoint(false) disables the unsecured (tcp) listener; omit or set true to keep both.
- WithEncryptionSslProtocol can be set to Tls13 if available on your platform.

### 3) Connect a TLS client (server authentication)
```csharp
using System.Security.Authentication;
using MQTTnet.Rx.Client;

var client = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithClientOptions(c =>
        c.WithTcpServer("your-broker-host", 8883)
         .WithTlsOptions(tls =>
            tls.WithSslProtocols(SslProtocols.Tls12)
               .WithIgnoreCertificateChainErrors(false)
               .WithIgnoreCertificateRevocationErrors(false)))))
    .Subscribe();
```

### 4) Connect a TLS client with client certificate (mutual TLS)
```csharp
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Rx.Client;

var clientCert = new X509Certificate2("client.pfx", "pfx-password");

var client = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithClientOptions(c =>
        c.WithTcpServer("your-broker-host", 8883)
         .WithTlsOptions(tls =>
            tls.WithSslProtocols(SslProtocols.Tls12)
               .WithCertificates(new[] { clientCert })
               // For development only: accept untrusted/invalid chains
               .WithIgnoreCertificateChainErrors(true)
               .WithIgnoreCertificateRevocationErrors(true)))))
    .Subscribe();
```

Tips:
- On production, install and trust the CA certificate instead of ignoring validation.
- When using mutual TLS, validate the client certificate on the server (via ValidatingConnection or a custom connection validator).

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

## 🧰 New helper APIs
- PayloadExtensions
  - e.Payload(): ReadOnlyMemory<byte> – zero-copy access to payload
  - e.PayloadUtf8(): string – decode payload without ToArray
  - observable.ToUtf8String(): project stream to UTF8 strings
- ConnectionExtensions
  - WhenReady(): emits resilient client when it is connected, useful to gate pipelines
- SubscribeToTopics(params string[]): subscribe to multiple topics at once (raw and resilient)

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
Publish Modbus registers/coils/inputs as MQTT messages. You can provide either a preconfigured Modbus master or a factory.

- FromMaster(master): wrap an existing ModbusIpMaster
- FromFactory(() => new ModbusIpMaster(...)): wrap a factory creating a master

Raw client publish (IMqttClient) with QoS/retain:

```csharp
using MQTTnet.Rx.Modbus;
using MQTTnet.Protocol;

var modbusState = Create.FromFactory(() => /* create and configure ModbusIpMaster */);

// Input registers
var pubInRegs = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishInputRegisters(modbusState, topic: "modbus/inputregs", startAddress: 0, numberOfPoints: 8, interval: 250, qos: MqttQualityOfServiceLevel.AtLeastOnce, retain: false)
    .Subscribe();

// Holding registers
var pubHold = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishHoldingRegisters(modbusState, topic: "modbus/holdingregs", startAddress: 0, numberOfPoints: 8, interval: 500)
    .Subscribe();

// Inputs (discrete)
var pubInputs = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishInputs(modbusState, topic: "modbus/inputs", startAddress: 0, numberOfPoints: 16, interval: 250)
    .Subscribe();

// Coils
var pubCoils = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishCoils(modbusState, topic: "modbus/coils", startAddress: 0, numberOfPoints: 16, interval: 250)
    .Subscribe();
```

Resilient client publish (IResilientMqttClient):

```csharp
using MQTTnet.Rx.Modbus;
using MQTTnet.Protocol;

var modbusState = Create.FromMaster(preconfiguredMaster);

var resilient = Create.ResilientMqttClient()
    .WithResilientClientOptions(o => o.WithClientOptions(c => c.WithTcpServer("localhost", 1883)).WithAutoReconnectDelay(TimeSpan.FromSeconds(2)));

var p1 = resilient.PublishInputRegisters(modbusState, "modbus/inputregs", 0, 8, 250).Subscribe();
var p2 = resilient.PublishHoldingRegisters(modbusState, "modbus/holdingregs", 0, 8, 500).Subscribe();
var p3 = resilient.PublishInputs(modbusState, "modbus/inputs", 0, 16, 250).Subscribe();
var p4 = resilient.PublishCoils(modbusState, "modbus/coils", 0, 16, 250).Subscribe();
```

Custom payloads (string/byte[]) via factory:

```csharp
// JSON with additional metadata
var reader = modbusState.ReadHoldingRegisters(0, 8, 500)
    .Select(r => (connected: true, error: (Exception?)null, data: new { ts = DateTime.UtcNow, values = r.data } as object));

var pubCustom = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishModbus(reader, topic: "modbus/custom", payloadFactory: d => JsonConvert.SerializeObject(d))
    .Subscribe();

// Binary payload
var pubBin = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishModbus(reader, topic: "modbus/customBin", payloadFactory: d => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(d)))
    .Subscribe();
```

Write values from MQTT to Modbus (subscribe + write):

```csharp
// Shortcuts with default parsers
var subReg1 = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeWriteSingleRegister(modbusState, topic: "modbus/write/reg/10", address: 10, writer: (m, v) => m.WriteSingleRegister(10, v));

var subRegs = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeWriteMultipleRegisters(modbusState, topic: "modbus/write/regs", startAddress: 0, writer: (m, values) => m.WriteMultipleRegisters(0, values));

var subCoil1 = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeWriteSingleCoil(modbusState, topic: "modbus/write/coil/5", address: 5, writer: (m, v) => m.WriteSingleCoil(5, v));

var subCoils = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeWriteMultipleCoils(modbusState, topic: "modbus/write/coils", startAddress: 0, writer: (m, values) => m.WriteMultipleCoils(0, values));

// Or generic form mapping your own parser/writer
var generic = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .SubscribeWrite(modbusState, topic: "modbus/write/custom", parse: s => ushort.Parse(s), writer: (m, v) => m.WriteSingleRegister(20, v));
```

Notes:
- FromMaster and FromFactory help you pass your own configured master or a factory.
- All publish helpers accept qos/retain.
- For write operations, use SubscribeWrite* helpers or the generic SubscribeWrite/SubscribeWriteAsync to plug the correct Modbus API calls.

### Serial Port (MQTTnet.Rx.SerialPort)
Publish framed serial data to MQTT using CP.IO.Ports ISerialPortRx.

```csharp
using System;
using System.Reactive.Linq;
using CP.IO.Ports;
using MQTTnet.Rx.SerialPort;

// Create and configure an ISerialPortRx (COM port, baud, parity, etc.)
ISerialPortRx port = /* create and configure an ISerialPortRx */;

var serialPub = Create.MqttClient()
    .WithClientOptions(c => c.WithTcpServer("localhost", 1883))
    .PublishSerialPort(
        topic: "serial/data",
        serialPort: port,
        startsWith: Observable.Return('<'),
        endsWith: Observable.Return('>'),
        timeOut: 1000)
    .Subscribe();
```

### PLCs – S7, Allen-Bradley, TwinCAT
Each PLC package adds helpers to publish or subscribe PLC tags via MQTT using the respective Rx client libraries (S7PlcRx, ABPlcRx, CP.TwinCatRx). Example signatures include:

- MQTTnet.Rx.S7Plc: PublishS7PlcTag<T>(client, topic, plcVariable, configurePlc)
- MQTTnet.Rx.ABPlc: PublishABPlcTag<T>(client, topic, plcVariable, configurePlc)
- MQTTnet.Rx.TwinCAT: PublishTcPlcTag<T>(client, topic, plcVariable, configurePlc)

Refer to those libraries for creating and configuring connected PLC clients, then use the Publish* helpers to push tag changes to MQTT. You can also combine SubscribeToTopic with your PLC client to write incoming values back to tags.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing
Issues and PRs are welcome.

---

**MQTTnet.Rx** - Empowering Industrial Automation with Reactive Technology ⚡🏭
