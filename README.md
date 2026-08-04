# MQTTnet.Rx

[![License](https://img.shields.io/github/license/ChrisPulman/MQTTnet.Rx.svg)](LICENSE)
[![Build](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml/badge.svg)](https://github.com/ChrisPulman/MQTTnet.Rx/actions/workflows/BuildOnly.yml)
[![MQTTnet.Rx.Client](https://img.shields.io/nuget/v/MQTTnet.Rx.Client.svg?style=flat-square&label=client)](https://www.nuget.org/packages/MQTTnet.Rx.Client)
[![MQTTnet.Rx.Server](https://img.shields.io/nuget/v/MQTTnet.Rx.Server.svg?style=flat-square&label=server)](https://www.nuget.org/packages/MQTTnet.Rx.Server)
[![MQTTnet.Rx.AspNetCore](https://img.shields.io/nuget/v/MQTTnet.Rx.AspNetCore.svg?style=flat-square&label=aspnetcore)](https://www.nuget.org/packages/MQTTnet.Rx.AspNetCore)

<p align="left">
  <a href="https://github.com/ChrisPulman/MQTTnet.Rx">
    <img alt="MQTTnet.Rx" src="https://github.com/ChrisPulman/MQTTnet.Rx/blob/main/Images/logo.png" width="200" />
  </a>
</p>

MQTTnet.Rx adds reactive client, broker, resilience, payload, topic, and industrial-device APIs to MQTTnet 5. It supports ordinary `IObservable<T>` pipelines and cancellation-aware `IObservableAsync<T>` pipelines without making application code own MQTT event-handler plumbing.

The package family provides:

- shared, reference-counted MQTT client and server lifetimes;
- observable publish, subscribe, discovery, connection, packet, and broker-event APIs;
- a resilient MQTT client with reconnect, queueing, storage, and subscription synchronization;
- topic filtering, named topic-value extraction, JSON conversion, and payload helpers;
- low-allocation pooled-payload, batching, throttling, sampling, and back-pressure helpers;
- TLS, WebSocket, Azure IoT Hub/Event Grid, session, connection, and Last Will helpers;
- fluent ASP.NET Core dependency-injection, endpoint, connection, hosted-server, and reactive connection APIs;
- MQTT bridges for Allen-Bradley, Mitsubishi, Modbus, Omron, Siemens S7, serial ports, and TwinCAT.

MQTTnet 5 removed `ManagedClient`. Use the `IResilientMqttClient` implementation supplied by `MQTTnet.Rx.Client` when an application needs automatic reconnection and an outbound queue.

## Contents

- [Packages and compatibility](#packages-and-compatibility)
- [Install](#install)
- [Core concepts](#core-concepts)
- [MQTT client](#mqtt-client)
- [Resilient client](#resilient-client)
- [Payloads, JSON, and topics](#payloads-json-and-topics)
- [Connection configuration and Last Will](#connection-configuration-and-last-will)
- [Low-allocation APIs](#low-allocation-apis)
- [MQTT server](#mqtt-server)
- [ASP.NET Core hosting](#aspnet-core-hosting)
- [Industrial bridges](#industrial-bridges)
- [Complete public API](#complete-public-api)
  - [`MQTTnet.Rx.Client`](#mqttnetrxclient-api)
  - [`MQTTnet.Rx.Server`](#mqttnetrxserver-api)
  - [`MQTTnet.Rx.AspNetCore`](#mqttnetrxaspnetcore-api)
  - [Industrial packages](#industrial-package-api)
- [Building the repository](#building-the-repository)
- [Contributing](#contributing)
- [License](#license)

## Packages and compatibility

### Package matrix

Choose one column for an application. A `.Reactive` package compiles the same source as its lean sibling and changes the public namespace and reactive dependency aliases. Do not install both variants of the same component unless a deliberate interop boundary requires them.

| Capability | Lean package | System.Reactive-compatible package | Lean namespace | `.Reactive` namespace |
| --- | --- | --- | --- | --- |
| MQTT client, resilience, payloads, topics | `MQTTnet.Rx.Client` | `MQTTnet.Rx.Client.Reactive` | `MQTTnet.Rx.Client` | `MQTTnet.Rx.Client.Reactive` |
| MQTT broker/server | `MQTTnet.Rx.Server` | `MQTTnet.Rx.Server.Reactive` | `MQTTnet.Rx.Server` | `MQTTnet.Rx.Server.Reactive` |
| ASP.NET Core hosting | `MQTTnet.Rx.AspNetCore` | `MQTTnet.Rx.AspNetCore.Reactive` | `MQTTnet.Rx.AspNetCore` | `MQTTnet.Rx.AspNetCore.Reactive` |
| Allen-Bradley | `MQTTnet.Rx.ABPlc` | `MQTTnet.Rx.ABPlc.Reactive` | `MQTTnet.Rx.ABPlc` | `MQTTnet.Rx.ABPlc.Reactive` |
| Mitsubishi | `MQTTnet.Rx.Mitsubishi` | `MQTTnet.Rx.Mitsubishi.Reactive` | `MQTTnet.Rx.Mitsubishi` | `MQTTnet.Rx.Mitsubishi.Reactive` |
| Modbus | `MQTTnet.Rx.Modbus` | `MQTTnet.Rx.Modbus.Reactive` | `MQTTnet.Rx.Modbus` | `MQTTnet.Rx.Modbus.Reactive` |
| Omron | `MQTTnet.Rx.OmronPlc` | `MQTTnet.Rx.OmronPlc.Reactive` | `MQTTnet.Rx.OmronPlc` | `MQTTnet.Rx.OmronPlc.Reactive` |
| Siemens S7 | `MQTTnet.Rx.S7Plc` | `MQTTnet.Rx.S7Plc.Reactive` | `MQTTnet.Rx.S7Plc` | `MQTTnet.Rx.S7Plc.Reactive` |
| Serial port | `MQTTnet.Rx.SerialPort` | `MQTTnet.SerialPort.Reactive` | `MQTTnet.Rx.SerialPort` | `MQTTnet.Rx.SerialPort.Reactive` |
| TwinCAT | `MQTTnet.Rx.TwinCAT` | `MQTTnet.TwinCATRx.Reactive` | `MQTTnet.Rx.TwinCAT` | `MQTTnet.Rx.TwinCAT.Reactive` |

The SerialPort and TwinCAT reactive package IDs retain their historical names; their namespaces follow the consistent `MQTTnet.Rx.*.Reactive` pattern.

All packages target .NET 8, .NET 9, .NET 10, and .NET 11. TwinCAT targets the Windows-specific `net8.0-windows10.0.19041` through `net11.0-windows10.0.19041` frameworks.

Industrial packages bring in the matching `IoT-Driver.*` package and `MQTTnet.Rx.Client` transitively. Their `.Reactive` siblings bring in the matching `IoT-Driver.*.Reactive` and client `.Reactive` packages.

The ASP.NET Core packages bring in the matching `MQTTnet.Rx.Server` family transitively. Import both the ASP.NET Core and Server namespace when configuring the base `MqttServer` supplied to `UseMqttServer` callbacks.

### Lean or `.Reactive`?

Both families use BCL `System.IObservable<T>`. The differences are the implementation package and the types used for completion values and scheduling:

| Concern | Lean family | `.Reactive` family |
| --- | --- | --- |
| Core package | `ReactiveUI.Primitives` | `ReactiveUI.Primitives.Reactive` |
| Async package | `ReactiveUI.Primitives.Async` | `ReactiveUI.Primitives.Async.Reactive` |
| Unit value | `ReactiveUI.Primitives.RxVoid` | `System.Reactive.Unit` |
| Timed scheduler | `ReactiveUI.Primitives.Concurrency.ISequencer` | `System.Reactive.Concurrency.IScheduler` |
| Grouped sequence | `MQTTnet.Rx.Client.Linq.IGroupedObservable<TKey,T>` | `System.Reactive.Linq.IGroupedObservable<TKey,T>` |

Use the lean package in a Primitives-first application. Use `.Reactive` in an application already based on System.Reactive. Most examples below use the lean family; for `.Reactive`, change the package and the `MQTTnet.Rx.*` namespace to its `.Reactive` counterpart, then import System.Reactive operators as usual.

Industrial `.Reactive` packages also use the matching reactive driver namespace: `IoT.Driver.ABPlcRx.Reactive`, `IoT.Driver.MitsubishiRx.Reactive`, `IoT.Driver.ModbusRx.Reactive`, `IoT.Driver.OmronPlcRx.Reactive`, `IoT.Driver.S7PlcRx.Reactive`, `IoT.Driver.Serial.Reactive`, or `IoT.Driver.TwinCATRx.Reactive`. Keep `IoT.Driver.Core` imports unchanged.

This is the System.Reactive counterpart of the basic connected-client pipeline:

```csharp
using MQTTnet.Rx.Client.Reactive;
using System.Reactive.Linq;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var status = clients.ConnectionStatus().Subscribe(
    connected => Console.WriteLine($"Connected: {connected}"),
    error => Console.Error.WriteLine(error));
```

The public conversion boundary is available in both families:

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

IObservable<string> classic = GetClassicMessages();
IObservableAsync<string> asynchronous = classic.ToSignal();
IObservable<string> roundTrip = asynchronous.ToObservable();

static IObservable<string> GetClassicMessages() =>
    ReactiveUI.Primitives.Signals.Signal.Return("ready");
```

`ToSignal` and `ToObservable` preserve notification order and dispose the underlying subscription when the converted subscription ends.

## Install

Install the smallest top-level package that supplies the required feature. NuGet restores its MQTTnet, Primitives, client, and driver dependencies.

```bash
dotnet add package MQTTnet.Rx.Client
dotnet add package MQTTnet.Rx.Server
dotnet add package MQTTnet.Rx.AspNetCore

dotnet add package MQTTnet.Rx.ABPlc
dotnet add package MQTTnet.Rx.Mitsubishi
dotnet add package MQTTnet.Rx.Modbus
dotnet add package MQTTnet.Rx.OmronPlc
dotnet add package MQTTnet.Rx.S7Plc
dotnet add package MQTTnet.Rx.SerialPort
dotnet add package MQTTnet.Rx.TwinCAT
```

For a System.Reactive application, install the corresponding package from the `.Reactive` column, for example:

```bash
dotnet add package MQTTnet.Rx.Client.Reactive
dotnet add package MQTTnet.Rx.AspNetCore.Reactive
dotnet add package MQTTnet.Rx.Modbus.Reactive
dotnet add package MQTTnet.SerialPort.Reactive
dotnet add package MQTTnet.TwinCATRx.Reactive
```

The MQTT client examples expect an MQTT broker on `localhost:1883`. Run the [in-process server example](#start-a-broker-and-observe-events) in a separate process, host that server in the same application, or change the endpoint to an existing MQTT 5 broker.

## Core concepts

### Pipelines are lazy

Factory and operation observables do work when subscribed. Retain and dispose the returned `IDisposable`, or `await using` the returned `IAsyncDisposable` for `IObservableAsync<T>`. Disposing the final subscription releases event handlers, broker subscriptions, and the shared client or server.

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883));

using var status = clients.ConnectionStatus().Subscribe(
    connected => Console.WriteLine($"Connected: {connected}"),
    error => Console.Error.WriteLine(error));
```

### Factory lifetime and safe sharing

One call to `Create.MqttClient()` or `Create.ResilientMqttClient()` captures one client. Overlapping subscribers to that returned sequence receive the same instance, and the final subscription disposes it. After that final disposal, do not resubscribe to the old sequence: it still refers to the disposed client. Build a new factory pipeline when a later application lifetime needs a new client.

A server factory has different restart behavior. Overlapping `MqttServerSession` values share one running server; releasing the last session stops and disposes it. A later subscription to the same server sequence creates and starts a new server.

Do not directly dispose a client emitted by a factory sequence. Dispose its owning subscription. `MqttServerSession` is the explicit server-lifetime handle and may own additional resources through `Add`.

`WithClientOptions` and `WithResilientClientOptions` configure on subscription; they do not themselves multicast the connect/start operation. When several downstream pipelines use one configured client, multicast that configured sequence with `.Publish().RefCount()` and keep at least one owner subscription active until all dependent work is disposed. This serializes the initial connect/start subscription and prevents concurrent downstream subscriptions from racing it.

### Synchronous and asynchronous streams

- APIs returning `IObservable<T>` use ordinary reactive subscriptions and `IDisposable`.
- APIs returning `IObservableAsync<T>` await observers, accept cancellation, and return `IAsyncDisposable` from `SubscribeAsync`.
- Methods named `Observe...` normally expose `IObservableAsync<T>`; the corresponding non-`Observe` event method normally exposes `IObservable<T>`.
- Exceptions from MQTT operations flow through `OnError`/`OnErrorAsync`. Always install an error handler in long-lived production pipelines.

### Important defaults

| API | Default behavior |
| --- | --- |
| `Publish(topic, payload)` | QoS 0 (`AtMostOnce`), not retained |
| stream `PublishMessage(messages)` | QoS 2 (`ExactlyOnce`), retained |
| `PingPeriodically()` | 30-second interval |
| raw `WithAutoReconnect()` | 5-second delay, unlimited attempts |
| resilient `AutoReconnectDelay` | 5 seconds |
| resilient `ConnectionCheckInterval` | 1 second |
| resilient queue limit | `int.MaxValue` |
| resilient overflow | `DropNewMessage` |
| topic discovery expiry | 1 hour |
| back-pressure queue | 1,000 messages |

## MQTT client

### Create, connect, receive, and publish

Reuse the same client sequence for related operations. Topic filters support MQTT `+` and `#` wildcards.

```csharp
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

var clients = Create.MqttClient()
    .WithClientOptions(options => options
        .WithTcpServer("localhost", 1883)
        .WithClientId("sample-client")
        .WithSessionOptions())
    .Publish()
    .RefCount();

using var received = clients
    .SubscribeToTopic("sensors/+/temperature")
    .Subscribe(
        message => Console.WriteLine(
            $"{message.ApplicationMessage.Topic}: {message.PayloadUtf8()}"),
        error => Console.Error.WriteLine($"Receive failed: {error}"));

var outgoing = new ReplaySignal<(string Topic, string Payload)>(0);
using var published = clients
    .PublishMessage(
        outgoing,
        MqttQualityOfServiceLevel.AtLeastOnce,
        retain: false)
    .Subscribe(
        result => Console.WriteLine($"Publish result: {result.ReasonCode}"),
        error => Console.Error.WriteLine($"Publish failed: {error}"));

outgoing.OnNext(("sensors/lab/temperature", "21.4"));
```

The stream publisher accepts `(Topic, string Payload)` and `(Topic, byte[] Payload)` sequences. Raw-client overloads emit `MqttClientPublishResult`; resilient overloads emit `ApplicationMessageProcessedEventArgs`. Raw overloads also accept QoS, retain, and message-builder customization where shown in the complete API.

### Async-observable client

Use `MqttClientSignal` when observers must be awaited or cancellation should stop delivery.

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

using var cancellation = new CancellationTokenSource();

var messages = Create.MqttClientSignal()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("alerts/#")
    .ToUtf8String();

await using var subscription = await messages.SubscribeAsync(
    async (payload, cancellationToken) =>
    {
        await Console.Out.WriteLineAsync(payload.AsMemory(), cancellationToken);
    },
    cancellation.Token);
```

`Create.MqttClientSignal`, `ResilientMqttClientSignal`, `MqttServerSignal`, and the `ObservableAsync...` integration APIs are the async-observable entry points. The method families and overload intent mirror the synchronous APIs.

### Single MQTT operations

`ReactiveClientOperationsExtensions` supplies fluent operations on `IObservable<IMqttClient>` and `IObservableAsync<IMqttClient>`. `ReactiveClientOperations` exposes the same overloads as static forwarding methods when extension syntax is inconvenient. A directly owned `IMqttClient` also has paired ordinary/async-observable methods through `MqttClientOperationExtensions`: `Connect`/`ObserveConnect`, `Disconnect`/`ObserveDisconnect`, `Ping`/`ObservePing`, `Publish`/`ObservePublish`, `Reconnect`/`ObserveReconnect`, enhanced-authentication exchange, subscribe, try-disconnect, try-ping, and unsubscribe. Builder callbacks preserve fluent configuration without hiding the complete MQTTnet options objects.

```csharp
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var keepAlive = clients
    .PingPeriodically(TimeSpan.FromSeconds(30))
    .Subscribe(_ => Console.WriteLine("Keep-alive completed"));
using var ping = clients.Ping().Subscribe(_ => Console.WriteLine("Pong"));

using var subscribed = clients
    .Subscribe(
        ["telemetry/#", "alarms/+"],
        MqttQualityOfServiceLevel.AtLeastOnce)
    .Subscribe(result => Console.WriteLine($"Filters: {result.Items.Count}"));

using var customSubscription = clients
    .Subscribe(filter => filter
        .WithTopic("commands/#")
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce))
    .Subscribe();

using var publishText = clients
    .Publish("status/app", "online", MqttQualityOfServiceLevel.AtLeastOnce, retain: true)
    .Subscribe(result => Console.WriteLine(result.ReasonCode));

using var publishBytes = clients
    .Publish("binary/frame", [0x01, 0x02, 0x03])
    .Subscribe();

using var publishBuilt = clients
    .Publish(builder => builder
        .WithTopic("events/custom")
        .WithPayload("created")
        .WithContentType("text/plain")
        .WithUserProperty("source", "sample"))
    .Subscribe();

using var options = clients.GetOptions().Subscribe(Console.WriteLine);
using var connected = clients.ConnectionStatus().Subscribe(Console.WriteLine);
using var ready = clients.WaitForConnection(TimeSpan.FromSeconds(10)).Subscribe();
using var unsubscribe = clients.Unsubscribe("telemetry/#", "alarms/+").Subscribe();
using var disconnect = clients
    .Disconnect(MqttClientDisconnectOptionsReason.NormalDisconnection)
    .Subscribe();
```

`Reconnect()` reconnects with the underlying client's previous options. `PublishMany` accepts an observable (or async-observable) of complete `MqttApplicationMessage` values and emits one publish result per message. `Properties()`, `PropertySnapshots()`/`ObservePropertySnapshots()`, `IsConnectedValue()`/`ObserveIsConnected()`, and `OptionsSnapshot()`/`ObserveOptionsSnapshot()` expose every public raw-client property. `WithAutoReconnect` is available for async-observable client sequences with configurable delay and retry count.

For a caller-owned `IMqttClient`, each direct operation is cold: constructing the observable does no network work, and subscribing performs exactly one MQTT operation. Complete MQTTnet options objects and fluent builder callbacks are both supported.

| Operation family | Ordinary observable | Async-observable | Result |
| --- | --- | --- | --- |
| connect | `Connect(options/configure)` | `ObserveConnect(options/configure)` | `MqttClientConnectResult` |
| disconnect | `Disconnect(options/configure)` | `ObserveDisconnect(options/configure)` | completion value |
| ping / safe ping | `Ping()`, `TryPing()` | `ObservePing()`, `ObserveTryPing()` | completion value / `bool` |
| publish | `Publish(message/configure)` | `ObservePublish(message/configure)` | `MqttClientPublishResult` |
| binary / sequence / string publish | `PublishBinary`, `PublishSequence`, `PublishString` | corresponding `Observe...` method | `MqttClientPublishResult` |
| reconnect | `Reconnect()` | `ObserveReconnect()` | completion value |
| enhanced authentication | `SendEnhancedAuthenticationExchangeData` | `ObserveSendEnhancedAuthenticationExchangeData` | completion value |
| subscribe / unsubscribe | `Subscribe`, `Unsubscribe` | `ObserveSubscribe`, `ObserveUnsubscribe` | MQTTnet result object |
| safe disconnect | `TryDisconnect` | `ObserveTryDisconnect` | `bool` |

```csharp
using System.Buffers;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var factory = new MqttClientFactory();
using var client = factory.CreateMqttClient();

IObservable<MqttClientConnectResult> connect = client.Connect(options => options
    .WithTcpServer("localhost", 1883)
    .WithClientId("direct-client"));

IObservable<MqttClientPublishResult> publish = client.PublishSequence(
    "telemetry/frame",
    new ReadOnlySequence<byte>(new byte[] { 0x01, 0x02, 0x03 }),
    MqttQualityOfServiceLevel.AtLeastOnce,
    retain: false);

using var state = client.PropertySnapshots().Subscribe(snapshot =>
    Console.WriteLine($"Connected={snapshot.IsConnected}; options={snapshot.Options?.ClientId}"));

// Subscribing starts the operation. Compose connect and publish with the
// application's preferred reactive operators when strict ordering is required.
using var connection = connect.Subscribe(result => Console.WriteLine(result.ResultCode));
```

### Raw client event streams

An emitted `IMqttClient` exposes synchronous and asynchronous bridges for all client events:

| Synchronous | Async-observable | Value |
| --- | --- | --- |
| `ApplicationMessageReceived()` | `ObserveApplicationMessageReceived()` | `MqttApplicationMessageReceivedEventArgs` |
| `Connected()` | `ObserveConnected()` | `MqttClientConnectedEventArgs` |
| `Connecting()` | `ObserveConnecting()` | `MqttClientConnectingEventArgs` |
| `Disconnected()` | `ObserveDisconnected()` | `MqttClientDisconnectedEventArgs` |
| `InspectPacket()` | `ObserveInspectPacket()` | `InspectMqttPacketEventArgs` |

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883));

using var events = clients.Subscribe(client =>
{
    using var connected = client.Connected().Subscribe(_ => Console.WriteLine("Connected"));
    using var disconnected = client.Disconnected().Subscribe(
        value => Console.WriteLine(value.Reason));
    using var packets = client.InspectPacket().Subscribe(
        value => Console.WriteLine($"{value.Direction}: {value.Packet}"));

    Console.ReadLine(); // Keeps the nested subscriptions alive for this sample.
});
```

Subscribing installs the corresponding MQTTnet async event handler; disposing removes it.

### Shared topic subscriptions and discovery

`SubscribeToTopic` and `SubscribeToTopics` manage the broker subscription as a shared hub per client/topic set. The first observer subscribes at the broker, concurrent observers share that subscription, late observers receive the latest message, and disposal of the last observer unsubscribes. Always dispose topic subscriptions.

`DiscoverTopics` subscribes to `#` and periodically publishes the distinct topic names and their last-seen UTC times. Supply an expiry to remove stale topics and a `TimeProvider` for deterministic hosting or tests.

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883));

using var topics = clients
    .DiscoverTopics(TimeSpan.FromMinutes(10), TimeProvider.System)
    .Subscribe(snapshot =>
    {
        foreach (var (topic, lastSeen) in snapshot)
        {
            Console.WriteLine($"{topic} last seen {lastSeen:O}");
        }
    });
```

## Resilient client

The resilient client owns an `IMqttClient`, reconnects after failures, restores subscriptions, and queues outbound messages. Create it with the observable factory for reactive composition or directly with `ResilientMqttClientFactory.Create` when an application already owns an `IMqttClient` and `IMqttNetLogger`.

### Configure and use

```csharp
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

var clients = Create.ResilientMqttClient()
    .WithResilientClientOptions(options => options
        .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
        .WithMaxPendingMessages(10_000)
        .WithPendingMessagesOverflowStrategy(
            MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
        .WithMaxTopicFiltersInSubscribeUnsubscribePackets(100)
        .WithClientOptions(client => client
            .WithTcpServer("localhost", 1883)
            .WithClientId("resilient-sample")))
    .Publish()
    .RefCount();

using var ready = clients.WhenReady().Subscribe(
    client => Console.WriteLine($"Ready; queued={client.PendingApplicationMessagesCount}"));

using var incoming = clients
    .SubscribeToTopic("commands/#")
    .Subscribe(message => Console.WriteLine(message.PayloadUtf8()));

var outgoing = new ReplaySignal<(string Topic, string Payload)>(0);
using var publishing = clients
    .PublishMessage(
        outgoing,
        MqttQualityOfServiceLevel.AtLeastOnce,
        retain: false)
    .Subscribe(result =>
    {
        Console.WriteLine($"Processed {result.ApplicationMessage.Id}");
        if (result.Exception is not null)
        {
            Console.Error.WriteLine(result.Exception);
        }
    });

outgoing.OnNext(("telemetry/device-01", "42"));
```

`WhenReady` immediately emits an already-connected client, then emits it after later successful connections. It is a gate for work that must not begin before a connection exists.

### Options and queue storage

`ResilientMqttClientOptions` exposes `ClientOptions`, `AutoReconnectDelay`, `ConnectionCheckInterval`, `Storage`, `MaxPendingMessages`, `PendingMessagesOverflowStrategy`, and `MaxTopicFiltersInSubscribeUnsubscribePackets`. The builder validates the options and requires MQTT client options.

Implement `IResilientMqttClientStorage` to persist the queue. The contract intentionally deals in complete `ResilientMqttApplicationMessage` objects so IDs survive process restarts.

```csharp
using System.Text.Json;
using MQTTnet.Rx.Client;

var storage = new JsonQueueStorage("mqtt-outbox.json");
var clients = Create.ResilientMqttClient()
    .WithResilientClientOptions(options => options
        .WithStorage(storage)
        .WithClientOptions(client => client.WithTcpServer("localhost", 1883)));

public sealed class JsonQueueStorage(string fileName) : IResilientMqttClientStorage
{
    public async Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync()
    {
        if (!File.Exists(fileName))
        {
            return [];
        }

        await using var stream = File.OpenRead(fileName);
        return await JsonSerializer.DeserializeAsync<List<ResilientMqttApplicationMessage>>(stream)
            ?? [];
    }

    public async Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages)
    {
        await using var stream = File.Create(fileName);
        await JsonSerializer.SerializeAsync(stream, messages);
    }
}
```

Coordinate file access if more than one process may use the same path. Production storage should also use atomic replacement to avoid a partial file after a crash.

### Direct resilient API and event surfaces

`IResilientMqttClient` implements `IDisposable` and exposes:

- lifecycle/state: `InternalClient`, `IsConnected`, `IsStarted`, `Options`, `PendingApplicationMessagesCount`, `StartAsync`, `StopAsync`, and `PingAsync`;
- queueing: `EnqueueAsync(MqttApplicationMessage)` and `EnqueueAsync(ResilientMqttApplicationMessage)`;
- subscription synchronization: `SubscribeAsync(IEnumerable<MqttTopicFilter>)` and `UnsubscribeAsync(IEnumerable<string>)`;
- ordinary .NET events, `IObservable<T>` properties, `IObservableAsync<T>` properties, and awaited handler registration methods.

Every direct task operation also has a cold ordinary/async-observable pair: `Enqueue`/`ObserveEnqueue`, `Ping`/`ObservePing`, `Start`/`ObserveStart`, `Stop`/`ObserveStop`, `Subscribe`/`ObserveSubscribe`, and `Unsubscribe`/`ObserveUnsubscribe`. `Start` accepts either `ResilientMqttClientOptions` or an `Action<ResilientMqttClientOptionsBuilder>`; `Stop` accepts an optional clean-disconnect flag. `Properties()` snapshots `InternalClient`, `IsConnected`, `IsStarted`, `Options`, and `PendingApplicationMessagesCount`, while `Property`/`ObserveProperty` select arbitrary state.

```csharp
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var factory = new MqttClientFactory();
using var client = ResilientMqttClientFactory.Create(
    factory.CreateMqttClient(),
    factory.DefaultLogger);

using var state = client.PropertySnapshots().Subscribe(snapshot =>
    Console.WriteLine($"Started={snapshot.IsStarted}; queued={snapshot.PendingApplicationMessagesCount}"));

var start = client.Start(options => options.WithClientOptions(mqtt => mqtt
    .WithTcpServer("localhost", 1883)
    .WithClientId("direct-resilient")));

var enqueue = client.Enqueue(new MqttApplicationMessageBuilder()
    .WithTopic("telemetry/device-01")
    .WithPayload("42")
    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
    .Build());

using var lifetime = start.Subscribe(_ => Console.WriteLine("Resilient client started"));
// Subscribe to enqueue after the start operation completes, or compose both
// operations in the application's pipeline. ObserveStart/ObserveEnqueue provide
// cancellation-aware async-observable equivalents.
```

| Event category | .NET event | `IObservable<T>` | `IObservableAsync<T>` / helper |
| --- | --- | --- | --- |
| message processed | `ApplicationMessageProcessedEvent` | `ApplicationMessageProcessed` | `ApplicationMessageProcessedAsyncObservable` / `ObserveApplicationMessageProcessed()` |
| message received | `ApplicationMessageReceivedEvent` | `ApplicationMessageReceived` | `ApplicationMessageReceivedAsyncObservable` / `ObserveApplicationMessageReceived()` |
| message skipped | `ApplicationMessageSkippedEvent` | `ApplicationMessageSkipped` | `ApplicationMessageSkippedAsyncObservable` / `ObserveApplicationMessageSkipped()` |
| connected | `ConnectedEvent` | `Connected` | `ConnectedAsyncObservable` / `ObserveConnected()` |
| connecting failed | `ConnectingFailedEvent` | `ConnectingFailed` | `ConnectingFailedAsyncObservable` / `ObserveConnectingFailed()` |
| state changed | `ConnectionStateChangedEvent` | `ConnectionStateChanged` | `ConnectionStateChangedAsyncObservable` / `ObserveConnectionStateChanged()` |
| disconnected | `DisconnectedEvent` | `Disconnected` | `DisconnectedAsyncObservable` / `ObserveDisconnected()` |
| synchronization failed | `SynchronizingSubscriptionsFailedEvent` | `SynchronizingSubscriptionsFailed` | `SynchronizingSubscriptionsFailedAsyncObservable` / `ObserveSynchronizingSubscriptionsFailed()` |
| subscriptions changed | `SubscriptionsChangedEvent` | `SubscriptionsChanged()` | `ObserveSubscriptionsChanged()` |

Each `Register...Handler` method accepts `Func<TEventArgs, CancellationToken, ValueTask>` and returns an `IDisposable` registration.

The supporting public models are:

- `ResilientMqttApplicationMessage`: queue `Id` and `ApplicationMessage`;
- `ApplicationMessageProcessedEventArgs`: message and optional exception;
- `ApplicationMessageSkippedEventArgs`: skipped message;
- `ConnectingFailedEventArgs`: optional connect result and exception;
- `InterceptingPublishMessageEventArgs`: message and mutable `AcceptPublish` flag;
- `ResilientProcessFailedEventArgs`: exception plus added/removed topic filters;
- `SubscriptionsChangedEventArgs`: subscribe and unsubscribe results;
- `MqttPendingMessagesOverflowStrategy`: `DropOldestQueuedMessage` or `DropNewMessage`;
- `ReconnectionResult`: `StillConnected`, `Reconnected`, `Recovered`, or `NotConnected`.

## Payloads, JSON, and topics

### Payload access

`Payload()` returns the MQTTnet `ReadOnlySequence<byte>` without forcing a new array. `PayloadUtf8()` decodes one event. `ToUtf8String()` projects an entire message sequence.

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var source = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("data/#");

using var raw = source.Subscribe(message =>
{
    var payload = message.Payload();
    Console.WriteLine($"{payload.Length} bytes: {message.PayloadUtf8()}");
});

using var text = source.ToUtf8String().Subscribe(Console.WriteLine);
```

### JSON dictionaries and typed models

The library uses `System.Text.Json`; no Newtonsoft.Json dependency is required.

```csharp
using System.Text.Json;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var source = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("sensors/+/reading");

using var dictionaries = source
    .ToDictionary()
    .Subscribe(values =>
    {
        if (values is not null)
        {
            Console.WriteLine(values["temperature"]);
        }
    });

using var temperatures = source
    .ToDictionary()
    .Where(values => values is not null)
    .Select(values => values!.ToDictionary(
        static pair => pair.Key,
        static pair => pair.Value!))
    .Observe("temperature")
    .ToDouble()
    .Subscribe(value => Console.WriteLine($"{value:F1} °C"));

using var readings = source
    .ToObject(static json => JsonSerializer.Deserialize<SensorReading>(json))
    .Subscribe(reading => Console.WriteLine(reading));

public sealed record SensorReading(
    string SensorId,
    double Temperature,
    DateTimeOffset Timestamp);
```

`ToObject<T>` also accepts `JsonTypeInfo<T>` for source-generated serialization. `Observe` replays the most recently observed dictionary value for its key. The explicit normalization above bridges the current nullable `ToDictionary` result to `Observe`'s non-null dictionary receiver. The conversion family is `ToBool`, `ToByte`, `ToInt16`, `ToInt32`, `ToInt64`, `ToSingle`, `ToDouble`, and `ToString`. Invalid JSON or conversions terminate the pipeline with an error unless the application handles it upstream.

### Topic filtering and extraction

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var all = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("#");

using var selected = all
    .WhereTopicMatchesAny("sensors/+/temperature", "alarms/#")
    .WhereTopicIsNotMatch("alarms/debug/#")
    .WhereTopicLevelCount(3)
    .Subscribe(message => Console.WriteLine(message.ApplicationMessage.Topic));

using var values = all
    .ExtractTopicValues("sites/{site}/devices/{device}/status")
    .Subscribe(item =>
    {
        Console.WriteLine($"Site={item.Values["site"]}; device={item.Values["device"]}");
        Console.WriteLine(item.Message.PayloadUtf8());
    });

using var deviceIds = all
    .SelectTopicLevel(2)
    .Subscribe(Console.WriteLine);

using var groups = all
    .GroupByTopicLevel(1)
    .Subscribe(group =>
    {
        Console.WriteLine($"New group: {group.Key}");
        group.Subscribe(message => Console.WriteLine(message.PayloadUtf8()));
    });
```

`WhereTopicIsMatch` is available with the subscribe/JSON helpers. `TopicFilterExtensions` adds `WhereTopicMatchesAny`, `WhereTopicIsNotMatch`, `ExtractTopicValues`, `WhereTopicLevelCount`, `SelectTopicLevel`, `GroupByTopic`, and `GroupByTopicLevel`. Async-observable equivalents return `IObservableAsync<T>`.

An empty `WhereTopicMatchesAny()` filter list produces an empty sequence. `SelectTopicLevel` ignores messages without the requested zero-based level. Topic patterns use MQTT wildcard matching; extraction patterns use `{name}` placeholders.

## Connection configuration and Last Will

### Connection, session, credentials, WebSocket, and cloud helpers

```csharp
using MQTTnet.Rx.Client;

var clients = Create.MqttClient()
    .WithClientOptions(options => options
        .WithTcpServer("broker.example.com", 1883)
        .WithUserCredentials("device-01", "secret")
        .WithSessionOptions(cleanStart: false, sessionExpiryInterval: 3_600)
        .WithConnectionSettings(
            keepAlivePeriod: TimeSpan.FromSeconds(30),
            timeout: TimeSpan.FromSeconds(10)));
```

`WithUserCredentials` accepts string or byte-array passwords. `WithWebSocketUri` configures WebSocket transport. `ForAzureIotHub` configures hostname, device ID, and SAS token; `ForAzureEventGrid` configures hostname, client ID, authentication name, and an X.509 certificate.

### TLS

```csharp
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Rx.Client;

var clientCertificate = X509CertificateLoader.LoadPkcs12FromFile(
    "device.pfx",
    "pfx-password");

var clients = Create.MqttClient()
    .WithClientOptions(options => options
        .WithTcpServer("broker.example.com", 8883)
        .WithTlsEnabled()
        .WithTlsProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)
        .WithTlsClientCertificate(clientCertificate)
        .WithTlsCertificateValidation(context => context.SslPolicyErrors == 0));
```

Use `WithTlsClientCertificates` for a collection. `WithTlsTrustAllCertificates` disables certificate validation and is intended only for controlled development environments; never use it for production endpoints.

### Last Will and Testament

```csharp
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;

var clients = Create.MqttClient()
    .WithClientOptions(options => options
        .WithTcpServer("localhost", 1883)
        .WithClientId("device-01")
        .WithLastWill(
            "presence/device-01",
            "offline",
            MqttQualityOfServiceLevel.AtLeastOnce,
            retain: true)
        .WithLastWillMetadata(
            "presence/device-01",
            "offline",
            "text/plain",
            correlationData: null,
            MqttQualityOfServiceLevel.AtLeastOnce,
            retain: true));
```

The Last Will family covers:

- `WithLastWill` for string or byte payloads, QoS, and retain;
- `WithLastWillJson<T>` with QoS, retain, and optional `JsonSerializerOptions`;
- `WithPresenceLastWill` and `WithPresenceLastWillJson` convenience payloads;
- `WithDelayedLastWill` using an MQTT 5 will-delay interval;
- `WithLastWillMetadata` for content type and correlation data;
- `WithLastWillUserProperties` for string, `ArraySegment<byte>`, or `ReadOnlyMemory<byte>` dictionaries.

### Raw-client reconnect helper

`WithAutoReconnect()` monitors a configured ordinary client. It retries after disconnection, prevents overlapping reconnect attempts, and emits the client after a successful reconnect. A maximum of `0` means unlimited attempts; reaching a positive limit terminates the sequence with the final error. Disposal cancels pending retries.

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

var clients = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .WithAutoReconnect(TimeSpan.FromSeconds(5), maxReconnectAttempts: 10);

using var lifetime = clients.Subscribe(
    _ => Console.WriteLine("Connected or reconnected"),
    error => Console.Error.WriteLine($"Reconnect limit reached: {error}"));
```

The same policy is available for `IObservableAsync<IMqttClient>` and preserves cancellation through reconnect delays and attempts:

```csharp
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives.Async;

using var cancellation = new CancellationTokenSource();
var clients = Create.MqttClientSignal()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .WithAutoReconnect(TimeSpan.FromSeconds(5), maxReconnectAttempts: 10);

await using var lifetime = await clients.SubscribeAsync(
    (client, _) =>
    {
        Console.WriteLine($"Connected={client.IsConnected}");
        return ValueTask.CompletedTask;
    },
    cancellation.Token);
```

This helper does not add an outbound queue. Use the resilient client when queued delivery or subscription synchronization is required.

## Low-allocation APIs

Import `MQTTnet.Rx.Client.MemoryEfficient` (or the `.Reactive.MemoryEfficient` namespace). The same family exists for `IObservable<T>` and `IObservableAsync<T>`.

### Pooled payloads

```csharp
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Client.MemoryEfficient;
using ReactiveUI.Primitives;

var source = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("binary/#");

using var pooled = source.ToPooledPayload().Subscribe(item =>
{
    try
    {
        Process(item.Buffer.AsSpan(0, item.Length));
    }
    finally
    {
        item.ReturnBuffer(); // Required exactly once.
    }
});

static void Process(ReadOnlySpan<byte> payload) =>
    Console.WriteLine($"Received {payload.Length} bytes");
```

Do not retain the buffer after calling `ReturnBuffer`. Use `ToPayloadArray` when data must outlive the callback. `GetPayloadLength` avoids copying, and `ToUtf8StringLowAlloc(maxStackSize)` uses stack decoding for suitably small payloads.

### Batching, rate control, filtering, and back-pressure

```csharp
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Client.MemoryEfficient;
using ReactiveUI.Primitives;

var source = Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .SubscribeToTopic("telemetry/#")
    .WhereTopicStartsWith("telemetry/line-")
    .WhereTopicEndsWith("/value");

using var batches = source
    .BatchProcess(
        count: 100,
        batch => batch.Sum(message => message.ApplicationMessage.Payload.Length))
    .Subscribe(totalBytes => Console.WriteLine($"Batch bytes: {totalBytes}"));

using var sampled = source
    .SampleMessages(TimeSpan.FromSeconds(1))
    .ObserveOnThreadPool()
    .Subscribe(message => Console.WriteLine(message.ApplicationMessage.Topic));

using var dropped = source
    .WithBackPressureDrop(message =>
        Console.Error.WriteLine($"Dropped {message.ApplicationMessage.Topic}"))
    .Subscribe(ProcessSlowly);

using var queued = source
    .WithBackPressureQueue(
        maxQueueSize: 500,
        onOverflow: message =>
            Console.Error.WriteLine($"Queue full: {message.ApplicationMessage.Topic}"))
    .Subscribe(ProcessSlowly);

static void ProcessSlowly(MQTTnet.Client.MqttApplicationMessageReceivedEventArgs message) =>
    Console.WriteLine(message.ApplicationMessage.Topic);
```

`BatchProcess` batches by time (optionally with a scheduler/sequencer) or by count. `ThrottleMessages`, `SampleMessages`, `GroupByTopic`, `WhereTopicStartsWith`, `WhereTopicEndsWith`, and `ObserveOnThreadPool` cover the other common high-throughput shapes. Drop mode suppresses an item while the observer is busy. Queue mode drops a new item when the bounded queue is full and invokes the optional overflow callback. Keep callbacks fast.

### Buffer utilities

- `BufferPool.DefaultBufferSize`, `Rent`, `Return`, `RentScope`, `ToArray`, and `CopyToRented` expose the shared `ArrayPool<byte>` policy.
- `BufferScope` rents in its constructors, exposes `Buffer`, `Span`, and `Memory`, and returns the array on `Dispose`.
- `SpanParser<T>` is the public `ReadOnlySpan<byte>` parsing delegate used by allocation-sensitive consumers.

```csharp
using MQTTnet.Rx.Client.MemoryEfficient;

using var buffer = BufferPool.RentScope(4_096);
Span<byte> writable = buffer.Span;
writable[0] = 0x2A;
```

## MQTT server

`MQTTnet.Rx.Server` creates a shared in-process MQTTnet broker and exposes the complete MQTTnet server-event surface as synchronous and asynchronous observables.

### Start a broker and observe events

```csharp
using MQTTnet.Rx.Server;
using ReactiveUI.Primitives;

var servers = Create.MqttServer(builder => builder
    .WithDefaultEndpoint()
    .WithDefaultEndpointPort(1883)
    .Build());

using var broker = servers.Subscribe(session =>
{
    session.Disposable.Add(session.Server.ClientConnected().Subscribe(
        value => Console.WriteLine($"Connected: {value.ClientId}")));

    session.Disposable.Add(session.Server.ClientDisconnected().Subscribe(
        value => Console.WriteLine($"Disconnected: {value.ClientId}")));

    session.Disposable.Add(session.Server.InterceptingPublish().Subscribe(
        value => Console.WriteLine($"Publish: {value.ApplicationMessage.Topic}")));
});
```

`Create.MqttServerSignal` is the async-observable factory. Both factories retry server startup up to three times. One factory sequence shares one server between its subscribers and stops it after the final `MqttServerSession` is disposed.

`MqttServerSession` exposes `Server`, `IsDisposed`, `Add(IDisposable)`, `Dispose`, and `DisposeAsync`. It has no public constructor; consume the instance emitted by the factory.

### Persistent retained messages

`MqttServerWithRetainedMessages` and `MqttServerWithRetainedMessagesSignal` persist retained messages in `RetainedMessages.json`. Pass a directory to control the storage location; omitting it uses the system temporary directory.

```csharp
using MQTTnet.Rx.Server;
using ReactiveUI.Primitives;

var servers = Create.MqttServerWithRetainedMessages(
    builder => builder
        .WithDefaultEndpoint()
        .WithDefaultEndpointPort(1883)
        .Build(),
    retainedMessageDirectory: Path.Combine(AppContext.BaseDirectory, "mqtt-state"));

using var broker = servers.Subscribe(session =>
{
    session.Disposable.Add(session.Server.RetainedMessageChanged().Subscribe(
        value => Console.WriteLine(value.ApplicationMessage.Topic)));
    session.Disposable.Add(session.Server.RetainedMessagesCleared().Subscribe(
        _ => Console.WriteLine("Retained messages cleared")));
});
```

`IMqttRetainedMessageModel` and `MqttRetainedMessageModel` round-trip MQTT retained messages. Their public contract includes `Topic`, `Payload`, `QualityOfServiceLevel`, `ContentType`, `ResponseTopic`, `CorrelationData`, `PayloadFormatIndicator`, and `UserProperties`, plus `Create(MqttApplicationMessage)` and `ToApplicationMessage()`.

### Fluent server operations, properties, and configuration

Every task-returning `MqttServer` operation has an ordinary observable and an async-observable form. This includes start/stop, client disconnect and broker-side subscribe/unsubscribe, retained-message query/update/delete, client/session queries, and injected application messages. The injection pair is named `InjectApplicationMessageOperation`/`ObserveInjectApplicationMessage` because MQTTnet already owns the instance name `InjectApplicationMessage`.

For MQTTnet methods that do not accept a cancellation token, cancelling an async-observable subscription stops waiting for and emitting the result; it cannot cancel broker work already running inside MQTTnet. Injected-message and enhanced-authentication operations pass cancellation into MQTTnet directly.

`Create.MqttServer(...)` emits an already-started, reference-count-owned server. Do not call the direct `Start` or `Stop` wrappers on that emitted instance; dispose the factory subscription/session and let it own the lifecycle. Use `Start`/`ObserveStart` and `Stop`/`ObserveStop` only with an independently created `MqttServer`.

`Properties()` captures `AcceptNewConnections`, `IsStarted`, and a safe copy of `ServerSessionItems`. Generic `Property`/`ObserveProperty`, snapshot streams, `AcceptNewConnectionsValue`/`ObserveAcceptNewConnections`, session-item snapshots, and distinct lifecycle streams expose all public server state. `WithAcceptNewConnections`, server-session item methods, and `ConfigureServer` preserve the receiver for fluent composition; the same configuration method is available on ordinary and async-observable server sequences. `ConfigureOptions` on MQTTnet's server, stop, and client-disconnect builders provides receiver-preserving access to the complete options objects.

Values returned by `GetClients` and `GetSessions` remain reactive. The query result types are `IList<MqttClientStatus>`, `MqttApplicationMessage`, `IList<MqttApplicationMessage>`, `MqttSessionStatus`, and `IList<MqttSessionStatus>`. `MqttClientStatus` exposes complete property snapshots plus disconnect/`ObserveDisconnect`, `ResetStatisticsOperation`/`ObserveResetStatistics`, and `WithSession`. `MqttSessionStatus` exposes complete snapshots, fluent session-item changes, and paired queue-clear, delete, deliver, and enqueue operations. `TryEnqueueApplicationMessage` returns `MqttSessionEnqueueResult`, retaining both the queue decision and optional `InjectMqttApplicationMessageResult`. Connection validation exposes `ExchangeEnhancedAuthentication`/`ObserveExchangeEnhancedAuthentication` and returns `ExchangeEnhancedAuthenticationResult`.

| Configuration/operation | Supported forms |
| --- | --- |
| disconnect client | `DisconnectClient` / `ObserveDisconnectClient` with options or builder callback |
| stop independently owned server | parameterless, `MqttServerStopOptions`, or builder callback |
| broker-side subscribe | topic-filter collection or `MqttTopicFilterBuilder` callback |
| broker-side unsubscribe | client ID plus topic-name collection |
| options mutation | `ConfigureOptions` on server, stop, and client-disconnect builders |
| server sequence configuration | `ConfigureServer` on `IObservable<MqttServer>` and `IObservableAsync<MqttServer>` |

```csharp
using MQTTnet;
using MQTTnet.Rx.Server;
using ReactiveUI.Primitives;

using var broker = Create.MqttServer(builder => builder.Build()).Subscribe(session =>
{
    session.Server
        .WithAcceptNewConnections(true)
        .WithServerSessionItem("environment", "production");

    session.Disposable.Add(session.Server.IsStartedChanges().Subscribe(Console.WriteLine));
    session.Disposable.Add(session.Server.GetClients().Subscribe(clients =>
    {
        foreach (var client in clients)
        {
            var clientState = client.Properties();
            var sessionState = client.Session.Properties();
            Console.WriteLine($"{clientState.Id}: {clientState.BytesReceived} bytes; " +
                              $"queued={sessionState.PendingApplicationMessagesCount}");
        }
    }));
    session.Disposable.Add(session.Server
        .UpdateRetainedMessage(new MqttApplicationMessageBuilder()
            .WithTopic("status/broker")
            .WithPayload("online")
            .WithRetainFlag()
            .Build())
        .Subscribe());
});
```

### Complete server event list

Every event below has an ordinary observable method and an `Observe...` async-observable method.

| Ordinary method | Async-observable method |
| --- | --- |
| `ApplicationMessageEnqueuedOrDropped` | `ObserveApplicationMessageEnqueuedOrDropped` |
| `ApplicationMessageNotConsumed` | `ObserveApplicationMessageNotConsumed` |
| `ClientAcknowledgedPublishPacket` | `ObserveClientAcknowledgedPublishPacket` |
| `ClientConnected` | `ObserveClientConnected` |
| `ClientDisconnected` | `ObserveClientDisconnected` |
| `ClientSubscribedTopic` | `ObserveClientSubscribedTopic` |
| `ClientUnsubscribedTopic` | `ObserveClientUnsubscribedTopic` |
| `InterceptingClientEnqueue` | `ObserveInterceptingClientEnqueue` |
| `InterceptingInboundPacket` | `ObserveInterceptingInboundPacket` |
| `InterceptingOutboundPacket` | `ObserveInterceptingOutboundPacket` |
| `InterceptingPublish` | `ObserveInterceptingPublish` |
| `InterceptingSubscription` | `ObserveInterceptingSubscription` |
| `InterceptingUnsubscription` | `ObserveInterceptingUnsubscription` |
| `LoadingRetainedMessage` | `ObserveLoadingRetainedMessage` |
| `PreparingSession` | `ObservePreparingSession` |
| `QueuedApplicationMessageOverwritten` | `ObserveQueuedApplicationMessageOverwritten` |
| `RetainedMessageChanged` | `ObserveRetainedMessageChanged` |
| `RetainedMessagesCleared` | `ObserveRetainedMessagesCleared` |
| `SessionDeleted` | `ObserveSessionDeleted` |
| `Started` | `ObserveStarted` |
| `Stopped` | `ObserveStopped` |
| `ValidatingConnection` | `ObserveValidatingConnection` |

## ASP.NET Core hosting

`MQTTnet.Rx.AspNetCore` wraps every non-obsolete `MQTTnet.AspNetCore` registration and hosting entry point with receiver-preserving fluent methods. It also projects hosted-server state and connection operations into ordinary and cancellation-aware asynchronous observables. The matching `MQTTnet.Rx.Server` package is transitive, so hosted brokers also receive the complete server event, operation, property, and configuration surface.

```csharp
using MQTTnet.Rx.AspNetCore;
using MQTTnet.Rx.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .WithMqttConnections()
    .WithMqttServer(options => options
        .WithoutDefaultEndpoint());

var app = builder.Build();
app.MapMqttEndpoint("/mqtt");

app.ConfigureMqttServer(server => server
    .WithAcceptNewConnections(true)
    .WithServerSessionItem("environment", app.Environment.EnvironmentName));

await app.RunAsync();
```

The fluent service surface includes hosted-server overloads for prebuilt options, options-builder callbacks, and service-aware `AspNetMqttServerOptionsBuilder` callbacks, plus MQTT logging, connection-handler, connection infrastructure, TCP adapter, and WebSocket adapter registration. Endpoint and pipeline methods cover `MapMqtt`, `UseMqtt`, and `UseMqttServer`. MQTTnet's obsolete `UseMqttEndpoint` is intentionally not wrapped; use `MapMqttEndpoint` with endpoint routing.

`MqttHostedServer` exposes `WithAcceptNewConnections`, server-session item configuration, `IsStartedChanges()`, and `ObserveIsStarted()`. `MqttConnectionContext` exposes a complete `MqttConnectionProperties` snapshot, statistics reset, and reactive connect, disconnect, send, and receive operations. Low-level MQTTnet buffer, pipe, and socket implementation types remain available from `MQTTnet.AspNetCore` but are not hosting configuration. The transport adapters' `ClientHandler` delegates are also intentionally not replaceable: MQTTnet owns them while the broker is running, and overriding them would bypass broker session processing.

The `.Reactive` package compiles the same source in `MQTTnet.Rx.AspNetCore.Reactive`; import `MQTTnet.Rx.Server.Reactive` for base-server extensions and use the corresponding System.Reactive operators and `Unit` values.

## Industrial bridges

The industrial packages bridge device values to MQTT and MQTT payloads back to devices. The application remains responsible for creating and configuring the driver object; the examples use `GetConfigured...` placeholders for that application-specific work.

All bridges provide ordinary raw-client and resilient-client forms. Async-observable forms use `IObservableAsync<IMqttClient>` or `IObservableAsync<IResilientMqttClient>` and return `IObservableAsync<T>` for publications. Static `Create` methods are compatibility forwarders; extension methods are normally clearer and, for S7/TwinCAT subscriptions, preserve the returned lifetime handle.

The client model changes the publication result but not the bridge's device arguments:

| MQTT client sequence | Publication result |
| --- | --- |
| `IObservable<IMqttClient>` | `IObservable<MqttClientPublishResult>` |
| `IObservable<IResilientMqttClient>` | `IObservable<ApplicationMessageProcessedEventArgs>` |
| `IObservableAsync<IMqttClient>` | `IObservableAsync<MqttClientPublishResult>` |
| `IObservableAsync<IResilientMqttClient>` | `IObservableAsync<ApplicationMessageProcessedEventArgs>` |

For example, the same Modbus master can feed resilient and async-observable clients. The other industrial packages follow the same receiver/result pattern with their device-specific arguments:

```csharp
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Async;

ModbusIpMaster master = GetConfiguredModbusMaster();
var modbus = MQTTnet.Rx.Modbus.Create.FromMaster(master);

var resilientClients = MQTTnet.Rx.Client.Create.ResilientMqttClient()
    .WithResilientClientOptions(options => options.WithClientOptions(mqtt => mqtt
        .WithTcpServer("localhost", 1883)));

using var resilientPublishing = resilientClients
    .PublishInputRegisters(modbus, "plant/input", 0, 8)
    .Subscribe(result => Console.WriteLine(result.Exception));

var asyncClients = MQTTnet.Rx.Client.Create.MqttClientSignal()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883));
var asyncModbus = MQTTnet.Rx.Modbus.ObservableAsyncCreateExtensions.FromMasterAsync(master);
var asyncPublishing = asyncClients.PublishInputRegisters(asyncModbus, "plant/input-async", 0, 8);

await using var asyncLifetime = await asyncPublishing.SubscribeAsync(
    (result, _) =>
    {
        Console.WriteLine(result.ReasonCode);
        return ValueTask.CompletedTask;
    });

static ModbusIpMaster GetConfiguredModbusMaster() => throw new NotImplementedException();
```

Keep the returned `IDisposable` or `IAsyncDisposable` for as long as the device-to-MQTT bridge should run. An async resilient bridge uses `ResilientMqttClientSignal()` with the same call and emits `ApplicationMessageProcessedEventArgs`.

### Allen-Bradley

`PublishABPlcTag<T>` observes a PLC variable and publishes its values. `SubscribeABPlcTag<T>` parses MQTT text and writes it to the PLC. The `params T[] typeWitness` parameter on publication exists for generic inference; explicit `<T>` is usually clearer.

```csharp
using IoT.Driver.ABPlcRx;
using MQTTnet.Rx.ABPlc;
using MQTTnet.Rx.Client;
using ReactiveUI.Primitives;

IABPlcRx plc = GetConfiguredAllenBradleyClient();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishABPlcTag<int>("plc/ab/line-speed", "LineSpeed", plc)
    .Subscribe();

using var write = clients.SubscribeABPlcTag(
    "plc/ab/line-speed/set",
    "LineSpeed",
    plc,
    static payload => int.Parse(payload, System.Globalization.CultureInfo.InvariantCulture));

static IABPlcRx GetConfiguredAllenBradleyClient() => throw new NotImplementedException();
```

### Mitsubishi

Mitsubishi bridges use a typed `LogicalTagKey<T>` and `MitsubishiLogicalTagClient`. Publication accepts a `Func<T,string>` formatter. Subscription accepts a `Func<string,T>` parser, a required-but-nullable error callback (pass `null` to omit it), and a cancellation token; writes are serialized and disposal cancels pending work.

```csharp
using IoT.Driver.Core;
using IoT.Driver.MitsubishiRx;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Mitsubishi;
using ReactiveUI.Primitives;

MitsubishiLogicalTagClient plc = GetConfiguredMitsubishiClient();
LogicalTagKey<int> speed = GetMitsubishiSpeedTag();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishMitsubishiTag(
        "plc/mitsubishi/speed",
        speed,
        plc,
        static value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))
    .Subscribe();

using var write = clients.SubscribeMitsubishiTag(
    "plc/mitsubishi/speed/set",
    speed,
    plc,
    static payload => int.Parse(payload, System.Globalization.CultureInfo.InvariantCulture),
    onError: Console.Error.WriteLine,
    cancellationToken: CancellationToken.None);

static MitsubishiLogicalTagClient GetConfiguredMitsubishiClient() => throw new NotImplementedException();
static LogicalTagKey<int> GetMitsubishiSpeedTag() => throw new NotImplementedException();
```

### Omron

`PublishOmronPlcTag<T>` and `SubscribeOmronPlcTag<T>` use `IOmronPlcRx` and `LogicalTagKey<T>`. Source-sequence failures are sent to the supplied trace callback. Exceptions thrown while parsing an MQTT payload or performing the synchronous PLC write propagate from the notification; handle them in the surrounding pipeline or in the parser/writer implementation.

```csharp
using IoT.Driver.Core;
using IoT.Driver.OmronPlcRx;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.OmronPlc;
using ReactiveUI.Primitives;

IOmronPlcRx plc = GetConfiguredOmronClient();
LogicalTagKey<bool> running = GetOmronRunningTag();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishOmronPlcTag("plc/omron/running", running, plc)
    .Subscribe();

using var write = clients.SubscribeOmronPlcTag(
    "plc/omron/running/set",
    running,
    plc,
    bool.Parse);

static IOmronPlcRx GetConfiguredOmronClient() => throw new NotImplementedException();
static LogicalTagKey<bool> GetOmronRunningTag() => throw new NotImplementedException();
```

### Siemens S7

Preferred extension APIs use `LogicalTagKey<T>` and return `IDisposable` for MQTT-to-PLC writes. Static `Create.SubscribeS7PlcTag` compatibility methods accept string variable names and return `void`; use the extension form when deterministic disposal matters.

```csharp
using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.S7Plc;
using ReactiveUI.Primitives;

IRxS7 plc = GetConfiguredS7Client();
LogicalTagKey<double> pressure = GetS7PressureTag();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishS7PlcTag("plc/s7/pressure", pressure, plc)
    .Subscribe();

using var write = clients.SubscribeS7PlcTag(
    "plc/s7/pressure/set",
    pressure,
    plc,
    static payload => double.Parse(
        payload,
        System.Globalization.CultureInfo.InvariantCulture));

static IRxS7 GetConfiguredS7Client() => throw new NotImplementedException();
static LogicalTagKey<double> GetS7PressureTag() => throw new NotImplementedException();
```

### Serial port

`PublishSerialPort` buffers data between observable start and end delimiters and publishes complete frames. `SubscribeSerialPortWriteLine` appends the driver's line ending. `SubscribeSerialPortWrite` writes either a transformed string or byte array. The `timeOut` argument is expressed in milliseconds.

```csharp
using IoT.Driver.Serial;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.SerialPort;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

ISerialPortRx port = GetConfiguredSerialPort();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishSerialPort(
        "serial/frames",
        port,
        startsWith: Signal.Return('<'),
        endsWith: Signal.Return('>'),
        timeOut: 1_000)
    .Subscribe();

using var writeLine = clients.SubscribeSerialPortWriteLine(
    "serial/write-line",
    port,
    static payload => payload);

using var writeBytes = clients.SubscribeSerialPortWrite(
    "serial/write-bytes",
    port,
    static payload => System.Text.Encoding.ASCII.GetBytes(payload));

static ISerialPortRx GetConfiguredSerialPort() => throw new NotImplementedException();
```

### TwinCAT

TwinCAT packages are Windows-only. Publication supports both `IRxTcAdsClient` and `IHashTableRx`. MQTT-to-tag extension methods return `IDisposable`. The static compatibility `Create.SubscribeTcTag` methods return `void`, so prefer extension syntax for lifecycle ownership.

```csharp
using CP.Collections;
using IoT.Driver.TwinCATRx;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.TwinCAT;
using ReactiveUI.Primitives;

IRxTcAdsClient ads = GetConfiguredAdsClient();
IHashTableRx symbols = GetConfiguredSymbolTable();
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var publish = clients
    .PublishTcPlcTag<double>("plc/twincat/temperature", "MAIN.Temperature", ads)
    .Subscribe();

using var publishFromTable = clients
    .PublishTcPlcTag<double>("plc/twincat/pressure", "MAIN.Pressure", symbols)
    .Subscribe();

using var write = clients.SubscribeTcTag(
    "plc/twincat/temperature/set",
    "MAIN.Temperature",
    ads,
    static payload => double.Parse(
        payload,
        System.Globalization.CultureInfo.InvariantCulture));

static IRxTcAdsClient GetConfiguredAdsClient() => throw new NotImplementedException();
static IHashTableRx GetConfiguredSymbolTable() => throw new NotImplementedException();
```

The `IHashTableRx` overload family is available for publication in synchronous and asynchronous extension APIs. See the complete API for the precise current overload set.

### Modbus

Modbus has the broadest bridge surface. `Create.FromMaster` wraps an existing `ModbusIpMaster`; `Create.FromFactory` creates one per subscription. Their state sequence reports connection status, an optional error, and the active master. Async code can use the public `FromMasterAsync` and `FromFactoryAsync` delegates.

#### Poll and publish

```csharp
using IoT.Driver.ModbusRx;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives;

ModbusIpMaster master = GetConfiguredModbusMaster();
var modbus = MQTTnet.Rx.Modbus.Create.FromMaster(master);
var scopedModbus = MQTTnet.Rx.Modbus.Create.FromFactory(GetConfiguredModbusMaster);
var asyncScopedModbus = MQTTnet.Rx.Modbus.ObservableAsyncCreateExtensions
    .FromFactoryAsync(GetConfiguredModbusMaster);
var clients = MQTTnet.Rx.Client.Create.MqttClient()
    .WithClientOptions(options => options.WithTcpServer("localhost", 1883))
    .Publish()
    .RefCount();

using var inputRegisters = clients.PublishInputRegisters(
    modbus,
    topic: "modbus/input-registers",
    startAddress: 0,
    numberOfPoints: 8,
    interval: 250,
    qos: MqttQualityOfServiceLevel.AtLeastOnce,
    retain: false).Subscribe();

using var holdingRegisters = clients.PublishHoldingRegisters(
    scopedModbus,
    "modbus/holding-registers",
    startAddress: 0,
    numberOfPoints: 8,
    interval: 500).Subscribe();

using var discreteInputs = clients.PublishInputs(
    modbus,
    "modbus/inputs",
    startAddress: 0,
    numberOfPoints: 16,
    interval: 250).Subscribe();

using var coils = clients.PublishCoils(
    modbus,
    "modbus/coils",
    startAddress: 0,
    numberOfPoints: 16,
    interval: 250).Subscribe();

static ModbusIpMaster GetConfiguredModbusMaster() => throw new NotImplementedException();
```

Every read family has raw/resilient and synchronous/async-observable forms, with overloads for default point counts/intervals and explicit QoS/retain values. Raw forms emit `MqttClientPublishResult`; resilient forms emit `ApplicationMessageProcessedEventArgs`.

#### Custom payloads

`PublishModbus<TPayload>` accepts a reader sequence containing `(Connected, Error, Data)` and a payload factory. `TPayload` is constrained to `notnull`; strings and byte arrays are published directly.

```csharp
using System.Text.Json;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Modbus;
using ReactiveUI.Primitives;

var reader = modbus
    .ReadHoldingRegisters(0, 8, 500)
    .Select(result => (
        result.Connected,
        result.Error,
        Data: (object?)new { Timestamp = DateTimeOffset.UtcNow, result.Data }));

using var custom = clients
    .PublishModbus(
        reader,
        "modbus/custom",
        static data => JsonSerializer.Serialize(data))
    .Subscribe();
```

#### MQTT-to-Modbus writes

```csharp
using MQTTnet.Rx.Modbus;

using var singleRegister = clients.SubscribeWriteSingleRegister(
    modbus,
    "modbus/write/register/10",
    address: 10,
    static (activeMaster, address, value) =>
        activeMaster.WriteSingleRegisterAsync(1, address, value).GetAwaiter().GetResult());

using var multipleRegisters = clients.SubscribeWriteMultipleRegisters(
    modbus,
    "modbus/write/registers",
    startAddress: 0,
    static (activeMaster, address, values) =>
        activeMaster.WriteMultipleRegistersAsync(1, address, values).GetAwaiter().GetResult());

using var singleCoil = clients.SubscribeWriteSingleCoil(
    modbus,
    "modbus/write/coil/5",
    address: 5,
    static (activeMaster, address, value) =>
        activeMaster.WriteSingleCoilAsync(1, address, value).GetAwaiter().GetResult());

using var multipleCoils = clients.SubscribeWriteMultipleCoils(
    modbus,
    "modbus/write/coils",
    startAddress: 0,
    static (activeMaster, address, values) =>
        activeMaster.WriteMultipleCoilsAsync(1, address, values).GetAwaiter().GetResult());

using var customWrite = clients.SubscribeWrite(
    modbus,
    "modbus/write/custom",
    static payload => ushort.Parse(payload),
    static (activeMaster, value) =>
        activeMaster.WriteSingleRegisterAsync(1, 20, value));
```

`SubscribeWrite<T>` accepts synchronous `Action<ModbusIpMaster,T>` and asynchronous `Func<ModbusIpMaster,T,Task>` writers. The single/multiple register and coil helpers provide typed parsing and address forwarding. `Serialize` and `DeSerialize<T>` are available as static compatibility methods and extension methods, implemented with `System.Text.Json`.

## Complete public API

The reference below is synchronized with every public source declaration in the ten lean projects. It includes all public types, enum values, constructors, properties, events, methods, extension receivers, overloads, default values, and generic constraints. Static compatibility forwarders are included even when an equivalent extension form exists.

The collapsed blocks use compact signature notation rather than complete compilation units. In particular, C# 14 extension blocks are shown as `extension(receiver) { member; }`, and implementation bodies are omitted. Use the feature examples above for copy/paste programs.

The ten `.Reactive` projects compile these same files with `REACTIVE_SHIM`; therefore every listed API is also present in the matching `.Reactive` namespace. Apply these substitutions when reading a signature:

- namespace `MQTTnet.Rx.<component>` becomes `MQTTnet.Rx.<component>.Reactive`;
- `RxVoid`/`RxUnit` completion values become `System.Reactive.Unit`;
- `ISequencer`/scheduler parameters become `System.Reactive.Concurrency.IScheduler`;
- grouped streams use `System.Reactive.Linq.IGroupedObservable<TKey,TElement>`.

<!-- PUBLIC_API_START -->

<details>
<summary>Type index (86 exported types)</summary>

- **MQTTnet.Rx.Client:** [`ApplicationMessageProcessedEventArgs`](#api-mqttnet-rx-client-applicationmessageprocessedeventargs), [`ApplicationMessageSkippedEventArgs`](#api-mqttnet-rx-client-applicationmessageskippedeventargs), [`ClientOptionsExtensions`](#api-mqttnet-rx-client-clientoptionsextensions), [`ConnectingFailedEventArgs`](#api-mqttnet-rx-client-connectingfailedeventargs), [`ConnectionExtensions`](#api-mqttnet-rx-client-connectionextensions), [`Create`](#api-mqttnet-rx-client-create), [`CreateExtensions`](#api-mqttnet-rx-client-createextensions), [`IResilientMqttClient`](#api-mqttnet-rx-client-iresilientmqttclient), [`IResilientMqttClientStorage`](#api-mqttnet-rx-client-iresilientmqttclientstorage), [`InterceptingPublishMessageEventArgs`](#api-mqttnet-rx-client-interceptingpublishmessageeventargs), [`LastWillExtensions`](#api-mqttnet-rx-client-lastwillextensions), [`Linq.IGroupedObservable`](#api-mqttnet-rx-client-linq-igroupedobservable), [`MemoryEfficient.BufferPool`](#api-mqttnet-rx-client-memoryefficient-bufferpool), [`MemoryEfficient.BufferScope`](#api-mqttnet-rx-client-memoryefficient-bufferscope), [`MemoryEfficient.LowAllocExtensions`](#api-mqttnet-rx-client-memoryefficient-lowallocextensions), [`MemoryEfficient.ObservableAsyncBridgeExtensions`](#api-mqttnet-rx-client-memoryefficient-observableasyncbridgeextensions), [`MqttClientAsyncAutoReconnectExtensions`](#api-mqttnet-rx-client-mqttclientasyncautoreconnectextensions), [`MqttClientExtensions`](#api-mqttnet-rx-client-mqttclientextensions), [`MqttClientOperationExtensions`](#api-mqttnet-rx-client-mqttclientoperationextensions), [`MqttClientProperties`](#api-mqttnet-rx-client-mqttclientproperties), [`MqttClientPropertyExtensions`](#api-mqttnet-rx-client-mqttclientpropertyextensions), [`MqttClientSequenceOperationExtensions`](#api-mqttnet-rx-client-mqttclientsequenceoperationextensions), [`MqttPendingMessagesOverflowStrategy`](#api-mqttnet-rx-client-mqttpendingmessagesoverflowstrategy), [`MqttdPublishExtensions`](#api-mqttnet-rx-client-mqttdpublishextensions), [`MqttdSubscribeExtensions`](#api-mqttnet-rx-client-mqttdsubscribeextensions), [`ObservableAsyncBridgeExtensions`](#api-mqttnet-rx-client-observableasyncbridgeextensions), [`ObservableBridgeCompatibilityExtensions`](#api-mqttnet-rx-client-observablebridgecompatibilityextensions), [`PayloadExtensions`](#api-mqttnet-rx-client-payloadextensions), [`ReactiveClientOperations`](#api-mqttnet-rx-client-reactiveclientoperations), [`ReactiveClientOperationsExtensions`](#api-mqttnet-rx-client-reactiveclientoperationsextensions), [`ReconnectionResult`](#api-mqttnet-rx-client-reconnectionresult), [`ResilientMqttApplicationMessage`](#api-mqttnet-rx-client-resilientmqttapplicationmessage), [`ResilientMqttClientFactory`](#api-mqttnet-rx-client-resilientmqttclientfactory), [`ResilientMqttClientOperationExtensions`](#api-mqttnet-rx-client-resilientmqttclientoperationextensions), [`ResilientMqttClientOptions`](#api-mqttnet-rx-client-resilientmqttclientoptions), [`ResilientMqttClientOptionsBuilder`](#api-mqttnet-rx-client-resilientmqttclientoptionsbuilder), [`ResilientMqttClientProperties`](#api-mqttnet-rx-client-resilientmqttclientproperties), [`ResilientMqttClientPropertyExtensions`](#api-mqttnet-rx-client-resilientmqttclientpropertyextensions), [`ResilientProcessFailedEventArgs`](#api-mqttnet-rx-client-resilientprocessfailedeventargs), [`SubscriptionsChangedEventArgs`](#api-mqttnet-rx-client-subscriptionschangedeventargs), [`TopicFilterExtensions`](#api-mqttnet-rx-client-topicfilterextensions), [`MemoryEfficient.SpanParser`](#api-mqttnet-rx-client-memoryefficient-spanparser)
- **MQTTnet.Rx.Server:** [`Create`](#api-mqttnet-rx-server-create), [`IMqttRetainedMessageModel`](#api-mqttnet-rx-server-imqttretainedmessagemodel), [`MqttClientStatusExtensions`](#api-mqttnet-rx-server-mqttclientstatusextensions), [`MqttClientStatusProperties`](#api-mqttnet-rx-server-mqttclientstatusproperties), [`MqttRetainedMessageModel`](#api-mqttnet-rx-server-mqttretainedmessagemodel), [`MqttServerConfigurationExtensions`](#api-mqttnet-rx-server-mqttserverconfigurationextensions), [`MqttServerExtensions`](#api-mqttnet-rx-server-mqttserverextensions), [`MqttServerOperationExtensions`](#api-mqttnet-rx-server-mqttserveroperationextensions), [`MqttServerOptionsConfigurationExtensions`](#api-mqttnet-rx-server-mqttserveroptionsconfigurationextensions), [`MqttServerProperties`](#api-mqttnet-rx-server-mqttserverproperties), [`MqttServerPropertyExtensions`](#api-mqttnet-rx-server-mqttserverpropertyextensions), [`MqttServerSequenceConfigurationExtensions`](#api-mqttnet-rx-server-mqttserversequenceconfigurationextensions), [`MqttServerSession`](#api-mqttnet-rx-server-mqttserversession), [`MqttSessionEnqueueResult`](#api-mqttnet-rx-server-mqttsessionenqueueresult), [`MqttSessionStatusExtensions`](#api-mqttnet-rx-server-mqttsessionstatusextensions), [`MqttSessionStatusProperties`](#api-mqttnet-rx-server-mqttsessionstatusproperties), [`ValidatingConnectionOperationExtensions`](#api-mqttnet-rx-server-validatingconnectionoperationextensions)
- **MQTTnet.Rx.AspNetCore:** [`MqttAspNetCoreHostingExtensions`](#api-mqttnet-rx-aspnetcore-mqttaspnetcorehostingextensions), [`MqttAspNetCoreServiceCollectionExtensions`](#api-mqttnet-rx-aspnetcore-mqttaspnetcoreservicecollectionextensions), [`MqttConnectionContextExtensions`](#api-mqttnet-rx-aspnetcore-mqttconnectioncontextextensions), [`MqttConnectionProperties`](#api-mqttnet-rx-aspnetcore-mqttconnectionproperties), [`MqttHostedServerExtensions`](#api-mqttnet-rx-aspnetcore-mqtthostedserverextensions)
- **MQTTnet.Rx.ABPlc:** [`Create`](#api-mqttnet-rx-abplc-create), [`CreateExtensions`](#api-mqttnet-rx-abplc-createextensions), [`ObservableAsyncCreateExtensionMixins`](#api-mqttnet-rx-abplc-observableasynccreateextensionmixins), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-abplc-observableasynccreateextensions)
- **MQTTnet.Rx.Mitsubishi:** [`MitsubishiMqttExtensions`](#api-mqttnet-rx-mitsubishi-mitsubishimqttextensions), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-mitsubishi-observableasynccreateextensions)
- **MQTTnet.Rx.Modbus:** [`Create`](#api-mqttnet-rx-modbus-create), [`CreateExtensions`](#api-mqttnet-rx-modbus-createextensions), [`ObservableAsyncCreateExtensionMixins`](#api-mqttnet-rx-modbus-observableasynccreateextensionmixins), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-modbus-observableasynccreateextensions), [`SerializationExtensions`](#api-mqttnet-rx-modbus-serializationextensions)
- **MQTTnet.Rx.OmronPlc:** [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-omronplc-observableasynccreateextensions), [`OmronPlcCreateExtensions`](#api-mqttnet-rx-omronplc-omronplccreateextensions)
- **MQTTnet.Rx.S7Plc:** [`Create`](#api-mqttnet-rx-s7plc-create), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-s7plc-observableasynccreateextensions), [`S7PlcExtensions`](#api-mqttnet-rx-s7plc-s7plcextensions)
- **MQTTnet.Rx.SerialPort:** [`Create`](#api-mqttnet-rx-serialport-create), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-serialport-observableasynccreateextensions), [`SerialPortMqttExtensions`](#api-mqttnet-rx-serialport-serialportmqttextensions)
- **MQTTnet.Rx.TwinCAT:** [`Create`](#api-mqttnet-rx-twincat-create), [`CreateExtensions`](#api-mqttnet-rx-twincat-createextensions), [`ObservableAsyncCreateExtensions`](#api-mqttnet-rx-twincat-observableasynccreateextensions)

</details>

<a id="mqttnetrxclient-api"></a>
### `MQTTnet.Rx.Client`

<a id="api-mqttnet-rx-client-applicationmessageprocessedeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ApplicationMessageProcessedEventArgs</code></summary>

```text
public sealed class ApplicationMessageProcessedEventArgs( ResilientMqttApplicationMessage applicationMessage, Exception? exception) : EventArgs
public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
public Exception? Exception { get; } = exception;
```
</details>

<a id="api-mqttnet-rx-client-applicationmessageskippedeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ApplicationMessageSkippedEventArgs</code></summary>

```text
public sealed class ApplicationMessageSkippedEventArgs( ResilientMqttApplicationMessage applicationMessage) : EventArgs
public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
```
</details>

<a id="api-mqttnet-rx-client-clientoptionsextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ClientOptionsExtensions</code></summary>

```text
public static class ClientOptionsExtensions
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WithAutoReconnect(); }
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WithAutoReconnect(TimeSpan? reconnectDelay); }
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WithAutoReconnect( TimeSpan? reconnectDelay, int maxReconnectAttempts); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsEnabled(); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsClientCertificate(X509Certificate2 certificate); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsClientCertificates( X509Certificate2Collection certificates); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsCertificateValidation( Func<MqttClientCertificateValidationEventArgs, bool> certificateValidationHandler); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsProtocols(SslProtocols sslProtocols); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithTlsTrustAllCertificates(); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithWebSocketUri(string uri); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithUserCredentials(string username, string password); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithUserCredentials(string username, byte[] password); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithSessionOptions(); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithSessionOptions(bool cleanStart); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithSessionOptions( bool cleanStart, uint sessionExpiryInterval); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithConnectionSettings(); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithConnectionSettings(TimeSpan? keepAlivePeriod); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithConnectionSettings( TimeSpan? keepAlivePeriod, TimeSpan? timeout); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder ForAzureIotHub( string iotHubHostname, string deviceId, string sasToken); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder ForAzureEventGrid( string hostname, string clientId, string authenticationName, X509Certificate2 certificate); }
```
</details>

<a id="api-mqttnet-rx-client-connectingfailedeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ConnectingFailedEventArgs</code></summary>

```text
public sealed class ConnectingFailedEventArgs( MqttClientConnectResult? connectResult, Exception exception) : EventArgs
public MqttClientConnectResult? ConnectResult { get; } = connectResult;
public Exception Exception { get; } = exception;
```
</details>

<a id="api-mqttnet-rx-client-connectionextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ConnectionExtensions</code></summary>

```text
public static class ConnectionExtensions
extension(IObservable<IResilientMqttClient> client) { public IObservable<IResilientMqttClient> WhenReady(); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<IResilientMqttClient> WhenReady(); }
extension(IResilientMqttClient client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> ObserveApplicationMessageProcessed(); }
extension(IResilientMqttClient client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveApplicationMessageReceived(); }
extension(IResilientMqttClient client) { public IObservableAsync<ApplicationMessageSkippedEventArgs> ObserveApplicationMessageSkipped(); }
extension(IResilientMqttClient client) { public IObservableAsync<MqttClientConnectedEventArgs> ObserveConnected(); }
extension(IResilientMqttClient client) { public IObservableAsync<ConnectingFailedEventArgs> ObserveConnectingFailed(); }
extension(IResilientMqttClient client) { public IObservableAsync<EventArgs> ObserveConnectionStateChanged(); }
extension(IResilientMqttClient client) { public IObservableAsync<MqttClientDisconnectedEventArgs> ObserveDisconnected(); }
extension(IResilientMqttClient client) { public IObservableAsync<ResilientProcessFailedEventArgs> ObserveSynchronizingSubscriptionsFailed(); }
extension(IResilientMqttClient client) { public IObservableAsync<SubscriptionsChangedEventArgs> ObserveSubscriptionsChanged(); }
```
</details>

<a id="api-mqttnet-rx-client-create"></a>
<details>
<summary><code>MQTTnet.Rx.Client.Create</code></summary>

```text
public static class Create
public static MqttClientFactory MqttFactory { get; }
public static void NewMqttFactory(MqttClientFactory mqttFactory)
public static IObservable<IMqttClient> MqttClient()
public static IObservableAsync<IMqttClient> MqttClientSignal()
public static IObservable<IResilientMqttClient> ResilientMqttClient()
public static IObservableAsync<IResilientMqttClient> ResilientMqttClientSignal()
public static IObservable<IMqttClient> WithClientOptions( IObservable<IMqttClient> client, Action<MqttClientOptionsBuilder> optionsBuilder)
public static IObservableAsync<IMqttClient> WithClientOptions( IObservableAsync<IMqttClient> client, Action<MqttClientOptionsBuilder> optionsBuilder)
public static ResilientMqttClientOptionsBuilder WithClientOptions( ResilientMqttClientOptionsBuilder builder, Action<MqttClientOptionsBuilder> clientBuilder)
public static IObservable<IResilientMqttClient> WithResilientClientOptions( IObservable<IResilientMqttClient> client, Action<ResilientMqttClientOptionsBuilder> optionsBuilder)
public static IObservableAsync<IResilientMqttClient> WithResilientClientOptions( IObservableAsync<IResilientMqttClient> client, Action<ResilientMqttClientOptionsBuilder> optionsBuilder)
public static ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder( MqttClientFactory factory)
```
</details>

<a id="api-mqttnet-rx-client-createextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.CreateExtensions</code></summary>

```text
public static class CreateExtensions
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WithClientOptions( Action<MqttClientOptionsBuilder> optionsBuilder); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<IResilientMqttClient> WithResilientClientOptions( Action<ResilientMqttClientOptionsBuilder> optionsBuilder); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IMqttClient> WithClientOptions( Action<MqttClientOptionsBuilder> optionsBuilder); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<IResilientMqttClient> WithResilientClientOptions( Action<ResilientMqttClientOptionsBuilder> optionsBuilder); }
extension(MqttClientFactory factory) { public ResilientMqttClientOptionsBuilder CreateResilientClientOptionsBuilder(); }
extension(ResilientMqttClientOptionsBuilder builder) { public ResilientMqttClientOptionsBuilder WithClientOptions( Action<MqttClientOptionsBuilder> clientBuilder); }
```
</details>

<a id="api-mqttnet-rx-client-iresilientmqttclient"></a>
<details>
<summary><code>MQTTnet.Rx.Client.IResilientMqttClient</code></summary>

```text
public interface IResilientMqttClient : IDisposable
event EventHandler<ApplicationMessageProcessedEventArgs>? ApplicationMessageProcessedEvent;
event EventHandler<MqttApplicationMessageReceivedEventArgs>? ApplicationMessageReceivedEvent;
event EventHandler<ApplicationMessageSkippedEventArgs>? ApplicationMessageSkippedEvent;
event EventHandler<MqttClientConnectedEventArgs>? ConnectedEvent;
event EventHandler<ConnectingFailedEventArgs>? ConnectingFailedEvent;
event EventHandler<EventArgs>? ConnectionStateChangedEvent;
event EventHandler<MqttClientDisconnectedEventArgs>? DisconnectedEvent;
event EventHandler<ResilientProcessFailedEventArgs>? SynchronizingSubscriptionsFailedEvent;
event EventHandler<SubscriptionsChangedEventArgs>? SubscriptionsChangedEvent;
IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed { get; }
IObservableAsync<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedAsyncObservable { get; }
IObservable<MqttClientConnectedEventArgs> Connected { get; }
IObservableAsync<MqttClientConnectedEventArgs> ConnectedAsyncObservable { get; }
IObservable<MqttClientDisconnectedEventArgs> Disconnected { get; }
IObservableAsync<MqttClientDisconnectedEventArgs> DisconnectedAsyncObservable { get; }
IObservable<ConnectingFailedEventArgs> ConnectingFailed { get; }
IObservableAsync<ConnectingFailedEventArgs> ConnectingFailedAsyncObservable { get; }
IObservable<EventArgs> ConnectionStateChanged { get; }
IObservableAsync<EventArgs> ConnectionStateChangedAsyncObservable { get; }
IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }
IObservableAsync<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailedAsyncObservable { get; }
IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped { get; }
IObservableAsync<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedAsyncObservable { get; }
IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; }
IObservableAsync<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedAsyncObservable { get; }
IMqttClient InternalClient { get; }
bool IsConnected { get; }
bool IsStarted { get; }
ResilientMqttClientOptions? Options { get; }
int PendingApplicationMessagesCount { get; }
IDisposable RegisterApplicationMessageProcessedHandler( Func<ApplicationMessageProcessedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterApplicationMessageReceivedHandler( Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterApplicationMessageSkippedHandler( Func<ApplicationMessageSkippedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterConnectedHandler( Func<MqttClientConnectedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterConnectingFailedHandler( Func<ConnectingFailedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterConnectionStateChangedHandler( Func<EventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterDisconnectedHandler( Func<MqttClientDisconnectedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterSynchronizingSubscriptionsFailedHandler( Func<ResilientProcessFailedEventArgs, CancellationToken, ValueTask> handler);
IDisposable RegisterSubscriptionsChangedHandler( Func<SubscriptionsChangedEventArgs, CancellationToken, ValueTask> handler);
Task EnqueueAsync(MqttApplicationMessage applicationMessage);
Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage);
Task PingAsync()
Task PingAsync(CancellationToken cancellationToken);
Task StartAsync(ResilientMqttClientOptions options);
Task StopAsync()
Task StopAsync(bool cleanDisconnect);
Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters);
Task UnsubscribeAsync(IEnumerable<string> topics);
```
</details>

<a id="api-mqttnet-rx-client-iresilientmqttclientstorage"></a>
<details>
<summary><code>MQTTnet.Rx.Client.IResilientMqttClientStorage</code></summary>

```text
public interface IResilientMqttClientStorage
Task SaveQueuedMessagesAsync(IList<ResilientMqttApplicationMessage> messages);
Task<IList<ResilientMqttApplicationMessage>> LoadQueuedMessagesAsync();
```
</details>

<a id="api-mqttnet-rx-client-interceptingpublishmessageeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.InterceptingPublishMessageEventArgs</code></summary>

```text
public sealed class InterceptingPublishMessageEventArgs( ResilientMqttApplicationMessage applicationMessage) : EventArgs
public ResilientMqttApplicationMessage ApplicationMessage { get; } = applicationMessage ?? throw new ArgumentNullException(nameof(applicationMessage));
public bool AcceptPublish { get; set; } = true;
```
</details>

<a id="api-mqttnet-rx-client-lastwillextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.LastWillExtensions</code></summary>

```text
public static class LastWillExtensions
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill(string topic, string payload); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill( string topic, string payload, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill(string topic, byte[] payload); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill( string topic, byte[] payload, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillJson<T>(string topic, T payload); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillJson<T>( string topic, T payload, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillJson<T>( string topic, T payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWill(string statusTopic); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWill( string statusTopic, string offlineMessage); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWillJson( string statusTopic, string clientId); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWillJson( string statusTopic, string clientId, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithDelayedLastWill( string topic, string payload, in TimeSpan delay); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithDelayedLastWill( string topic, string payload, in TimeSpan delay, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillMetadata( string topic, string payload, string contentType); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillMetadata( string topic, string payload, string contentType, byte[]? correlationData); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillMetadata( string topic, string payload, string contentType, byte[]? correlationData, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, string> userProperties); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, string> userProperties, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ArraySegment<byte>> userProperties); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ArraySegment<byte>> userProperties, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ReadOnlyMemory<byte>> userProperties); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ReadOnlyMemory<byte>> userProperties, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill( string topic, string payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWill( string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillJson<T>( string topic, T payload, MqttQualityOfServiceLevel qos, bool retain, JsonSerializerOptions? options); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWill( string statusTopic, string offlineMessage, MqttQualityOfServiceLevel qos); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithPresenceLastWillJson( string statusTopic, string clientId, MqttQualityOfServiceLevel qos, TimeProvider timeProvider); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithDelayedLastWill( string topic, string payload, in TimeSpan delay, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillMetadata( string topic, string payload, string contentType, byte[]? correlationData, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, string> userProperties, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ArraySegment<byte>> userProperties, MqttQualityOfServiceLevel qos, bool retain); }
extension(MqttClientOptionsBuilder builder) { public MqttClientOptionsBuilder WithLastWillUserProperties( string topic, string payload, IDictionary<string, ReadOnlyMemory<byte>> userProperties, MqttQualityOfServiceLevel qos, bool retain); }
```
</details>

<a id="api-mqttnet-rx-client-linq-igroupedobservable"></a>
<details>
<summary><code>MQTTnet.Rx.Client.Linq.IGroupedObservable</code></summary>

```text
public interface IGroupedObservable<out TKey, out TElement> : IObservable<TElement>
TKey Key { get; }
```
</details>

<a id="api-mqttnet-rx-client-memoryefficient-bufferpool"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MemoryEfficient.BufferPool</code></summary>

```text
public static class BufferPool
public static int DefaultBufferSize { get; }
public static byte[] Rent()
public static byte[] Rent(int minimumLength)
public static void Return(byte[]? array)
public static void Return(byte[]? array, bool clearArray)
public static BufferScope RentScope()
public static BufferScope RentScope(int minimumLength)
public static byte[] ToArray(in ReadOnlySequence<byte> sequence)
public static byte[] CopyToRented(in ReadOnlySequence<byte> sequence, out int bytesWritten)
```
</details>

<a id="api-mqttnet-rx-client-memoryefficient-bufferscope"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MemoryEfficient.BufferScope</code></summary>

```text
public readonly record struct BufferScope : IDisposable
public BufferScope()
public BufferScope(int minimumLength)
public byte[] Buffer { get; }
public Span<byte> Span { get; }
public Memory<byte> Memory { get; }
public void Dispose()
```
</details>

<a id="api-mqttnet-rx-client-memoryefficient-lowallocextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MemoryEfficient.LowAllocExtensions</code></summary>

```text
public static class LowAllocExtensions
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<int> GetPayloadLength(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<byte[]> ToPayloadArray(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<string> ToUtf8StringLowAlloc(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<string> ToUtf8StringLowAlloc(int maxStackSize); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<TResult> BatchProcess<TResult>( TimeSpan timeSpan, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<TResult> BatchProcess<TResult>( TimeSpan timeSpan, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor, IScheduler? scheduler); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<TResult> BatchProcess<TResult>( int count, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> ThrottleMessages( TimeSpan dueTime); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> ThrottleMessages( TimeSpan dueTime, IScheduler? scheduler); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> SampleMessages( TimeSpan interval); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> SampleMessages( TimeSpan interval, IScheduler? scheduler); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopic(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith( string topicPrefix); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith( string topicSuffix); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop( Action<MqttApplicationMessageReceivedEventArgs>? onDrop); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue( int maxQueueSize); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue( int maxQueueSize, Action<MqttApplicationMessageReceivedEventArgs>? onOverflow); }
```
</details>

<a id="api-mqttnet-rx-client-memoryefficient-observableasyncbridgeextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MemoryEfficient.ObservableAsyncBridgeExtensions</code></summary>

```text
public static class ObservableAsyncBridgeExtensions
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<(byte[] Buffer, int Length, Action ReturnBuffer)> ToPooledPayload(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<int> GetPayloadLength(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<byte[]> ToPayloadArray(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<string> ToUtf8StringLowAlloc(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<string> ToUtf8StringLowAlloc(int maxStackSize); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<TResult> BatchProcess<TResult>( TimeSpan timeSpan, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<TResult> BatchProcess<TResult>( TimeSpan timeSpan, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor, IScheduler? scheduler); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<TResult> BatchProcess<TResult>( int count, Func<IList<MqttApplicationMessageReceivedEventArgs>, TResult> batchProcessor); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ThrottleMessages( TimeSpan dueTime); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ThrottleMessages( TimeSpan dueTime, IScheduler? scheduler); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SampleMessages( TimeSpan interval); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SampleMessages( TimeSpan interval, IScheduler? scheduler); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopic(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicStartsWith( string topicPrefix); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicEndsWith( string topicSuffix); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveOnThreadPool(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureDrop( Action<MqttApplicationMessageReceivedEventArgs>? onDrop); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue( int maxQueueSize); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue( Action<MqttApplicationMessageReceivedEventArgs>? onOverflow); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WithBackPressureQueue( int maxQueueSize, Action<MqttApplicationMessageReceivedEventArgs>? onOverflow); }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientasyncautoreconnectextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientAsyncAutoReconnectExtensions</code></summary>

```text
public static class MqttClientAsyncAutoReconnectExtensions
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<IMqttClient> WithAutoReconnect(); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<IMqttClient> WithAutoReconnect(TimeSpan? reconnectDelay); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<IMqttClient> WithAutoReconnect(TimeSpan? reconnectDelay, int maxReconnectAttempts); }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientExtensions</code></summary>

```text
public static class MqttClientExtensions
extension(IMqttClient client) { public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived(); }
extension(IMqttClient client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ObserveApplicationMessageReceived(); }
extension(IMqttClient client) { public IObservable<MqttClientConnectedEventArgs> Connected(); }
extension(IMqttClient client) { public IObservableAsync<MqttClientConnectedEventArgs> ObserveConnected(); }
extension(IMqttClient client) { public IObservable<MqttClientConnectingEventArgs> Connecting(); }
extension(IMqttClient client) { public IObservableAsync<MqttClientConnectingEventArgs> ObserveConnecting(); }
extension(IMqttClient client) { public IObservable<MqttClientDisconnectedEventArgs> Disconnected(); }
extension(IMqttClient client) { public IObservableAsync<MqttClientDisconnectedEventArgs> ObserveDisconnected(); }
extension(IMqttClient client) { public IObservable<InspectMqttPacketEventArgs> InspectPacket(); }
extension(IMqttClient client) { public IObservableAsync<InspectMqttPacketEventArgs> ObserveInspectPacket(); }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientoperationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientOperationExtensions</code></summary>

```text
public static class MqttClientOperationExtensions
extension(IMqttClient client) { public IObservable<MqttClientConnectResult> Connect(MqttClientOptions options); }
extension(IMqttClient client) { public IObservable<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservableAsync<MqttClientConnectResult> ObserveConnect(MqttClientOptions options); }
extension(IMqttClient client) { public IObservableAsync<MqttClientConnectResult> ObserveConnect(Action<MqttClientOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptions options); }
extension(IMqttClient client) { public IObservable<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservableAsync<RxUnit> ObserveDisconnect(MqttClientDisconnectOptions options); }
extension(IMqttClient client) { public IObservableAsync<RxUnit> ObserveDisconnect(Action<MqttClientDisconnectOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservable<RxUnit> Ping(); }
extension(IMqttClient client) { public IObservableAsync<RxUnit> ObservePing(); }
extension(IMqttClient client) { public IObservable<MqttClientPublishResult> Publish(MqttApplicationMessage message); }
extension(IMqttClient client) { public IObservable<MqttClientPublishResult> Publish(Action<MqttApplicationMessageBuilder> configure); }
extension(IMqttClient client) { public IObservableAsync<MqttClientPublishResult> ObservePublish(MqttApplicationMessage message); }
extension(IMqttClient client) { public IObservableAsync<MqttClientPublishResult> ObservePublish(Action<MqttApplicationMessageBuilder> configure); }
extension(IMqttClient client) { public IObservable<MqttClientPublishResult> PublishBinary(string topic, IEnumerable<byte>? payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservableAsync<MqttClientPublishResult> ObservePublishBinary(string topic, IEnumerable<byte>? payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservable<MqttClientPublishResult> PublishSequence(string topic, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservableAsync<MqttClientPublishResult> ObservePublishSequence(string topic, ReadOnlySequence<byte> payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservable<MqttClientPublishResult> PublishString(string topic, string? payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservableAsync<MqttClientPublishResult> ObservePublishString(string topic, string? payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain); }
extension(IMqttClient client) { public IObservable<RxUnit> Reconnect(); }
extension(IMqttClient client) { public IObservableAsync<RxUnit> ObserveReconnect(); }
extension(IMqttClient client) { public IObservable<RxUnit> SendEnhancedAuthenticationExchangeData(MqttEnhancedAuthenticationExchangeData data); }
extension(IMqttClient client) { public IObservableAsync<RxUnit> ObserveSendEnhancedAuthenticationExchangeData(MqttEnhancedAuthenticationExchangeData data); }
extension(IMqttClient client) { public IObservable<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options); }
extension(IMqttClient client) { public IObservable<MqttClientSubscribeResult> Subscribe(Action<MqttClientSubscribeOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribe(MqttClientSubscribeOptions options); }
extension(IMqttClient client) { public IObservableAsync<MqttClientSubscribeResult> ObserveSubscribe(Action<MqttClientSubscribeOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservable<bool> TryDisconnect(); }
extension(IMqttClient client) { public IObservable<bool> TryDisconnect(MqttClientDisconnectOptionsReason reason, string? reasonString); }
extension(IMqttClient client) { public IObservableAsync<bool> ObserveTryDisconnect(); }
extension(IMqttClient client) { public IObservableAsync<bool> ObserveTryDisconnect(MqttClientDisconnectOptionsReason reason, string? reasonString); }
extension(IMqttClient client) { public IObservable<bool> TryPing(); }
extension(IMqttClient client) { public IObservableAsync<bool> ObserveTryPing(); }
extension(IMqttClient client) { public IObservable<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options); }
extension(IMqttClient client) { public IObservable<MqttClientUnsubscribeResult> Unsubscribe(Action<MqttClientUnsubscribeOptionsBuilder> configure); }
extension(IMqttClient client) { public IObservableAsync<MqttClientUnsubscribeResult> ObserveUnsubscribe(MqttClientUnsubscribeOptions options); }
extension(IMqttClient client) { public IObservableAsync<MqttClientUnsubscribeResult> ObserveUnsubscribe(Action<MqttClientUnsubscribeOptionsBuilder> configure); }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientproperties"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientProperties</code></summary>

```text
public sealed record MqttClientProperties(bool IsConnected, MqttClientOptions? Options)
public bool IsConnected { get; init; }
public MqttClientOptions? Options { get; init; }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientpropertyextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientPropertyExtensions</code></summary>

```text
public static class MqttClientPropertyExtensions
extension(IMqttClient client) { public MqttClientProperties Properties(); }
extension(IMqttClient client) { public IObservable<T> Property<T>(Func<IMqttClient, T> selector); }
extension(IMqttClient client) { public IObservableAsync<T> ObserveProperty<T>(Func<IMqttClient, T> selector); }
extension(IMqttClient client) { public IObservable<MqttClientProperties> PropertySnapshots(); }
extension(IMqttClient client) { public IObservableAsync<MqttClientProperties> ObservePropertySnapshots(); }
extension(IMqttClient client) { public IObservable<bool> IsConnectedValue(); }
extension(IMqttClient client) { public IObservableAsync<bool> ObserveIsConnected(); }
extension(IMqttClient client) { public IObservable<MqttClientOptions?> OptionsSnapshot(); }
extension(IMqttClient client) { public IObservableAsync<MqttClientOptions?> ObserveOptionsSnapshot(); }
```
</details>

<a id="api-mqttnet-rx-client-mqttclientsequenceoperationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttClientSequenceOperationExtensions</code></summary>

```text
public static class MqttClientSequenceOperationExtensions
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientConnectResult> Connect(MqttClientOptions options); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure); }
extension(IObservable<IMqttClient> clients) { public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptions options); }
extension(IObservable<IMqttClient> clients) { public IObservable<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientPublishResult> Publish(MqttApplicationMessage message); }
extension(IObservable<IMqttClient> clients) { public IObservable<RxUnit> SendEnhancedAuthenticationExchangeData(MqttEnhancedAuthenticationExchangeData data); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientSubscribeResult> Subscribe(Action<MqttClientSubscribeOptionsBuilder> configure); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options); }
extension(IObservable<IMqttClient> clients) { public IObservable<MqttClientUnsubscribeResult> Unsubscribe(Action<MqttClientUnsubscribeOptionsBuilder> configure); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientConnectResult> Connect(MqttClientOptions options); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientConnectResult> Connect(Action<MqttClientOptionsBuilder> configure); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<RxUnit> Disconnect(MqttClientDisconnectOptions options); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<RxUnit> Disconnect(Action<MqttClientDisconnectOptionsBuilder> configure); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientPublishResult> Publish(MqttApplicationMessage message); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<RxUnit> SendEnhancedAuthenticationExchangeData(MqttEnhancedAuthenticationExchangeData data); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientSubscribeResult> Subscribe(MqttClientSubscribeOptions options); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientSubscribeResult> Subscribe(Action<MqttClientSubscribeOptionsBuilder> configure); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(MqttClientUnsubscribeOptions options); }
extension(IObservableAsync<IMqttClient> clients) { public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(Action<MqttClientUnsubscribeOptionsBuilder> configure); }
```
</details>

<a id="api-mqttnet-rx-client-mqttpendingmessagesoverflowstrategy"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttPendingMessagesOverflowStrategy</code></summary>

```text
public enum MqttPendingMessagesOverflowStrategy
DropOldestQueuedMessage
DropNewMessage
```
</details>

<a id="api-mqttnet-rx-client-mqttdpublishextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttdPublishExtensions</code></summary>

```text
public static class MqttdPublishExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, string Payload)> message); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, byte[] Payload)> message); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMessage( IObservable<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
```
</details>

<a id="api-mqttnet-rx-client-mqttdsubscribeextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MqttdSubscribeExtensions</code></summary>

```text
public static partial class MqttdSubscribeExtensions
extension(IObservable<Dictionary<string, object>> dictionary) { public IObservable<object?> Observe(string key); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics( params string[] topics); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic( string topic); }
extension(IObservable<IMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(); }
extension(IObservable<IMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry); }
extension(IObservable<IMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry, TimeProvider timeProvider); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics( params string[] topics); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic( string topic); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry, TimeProvider timeProvider); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> message) { public IObservable<Dictionary<string, object?>?> ToDictionary(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> message) { public IObservable<T?> ToObject<T>(JsonTypeInfo<T> jsonTypeInfo); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> message) { public IObservable<T?> ToObject<T>(Func<string, T?> deserialize); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> message) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch( string topic); }
extension(IObservable<object?> observable) { public IObservable<bool> ToBool(); }
extension(IObservable<object?> observable) { public IObservable<byte> ToByte(); }
extension(IObservable<object?> observable) { public IObservable<short> ToInt16(); }
extension(IObservable<object?> observable) { public IObservable<int> ToInt32(); }
extension(IObservable<object?> observable) { public IObservable<long> ToInt64(); }
extension(IObservable<object?> observable) { public IObservable<float> ToSingle(); }
extension(IObservable<object?> observable) { public IObservable<double> ToDouble(); }
extension(IObservable<object?> observable) { public IObservable<string?> ToString(); }
```
</details>

<a id="api-mqttnet-rx-client-observableasyncbridgeextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions</code></summary>

```text
public static partial class ObservableAsyncBridgeExtensions
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<string> ToUtf8String(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsMatch( string topic); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny( params string[] topicFilters); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch( string topicFilter); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<( MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)> ExtractTopicValues(string topicPattern); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount( int levelCount); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<string> SelectTopicLevel(int levelIndex); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopic(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopicLevel(int levelIndex); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<Dictionary<string, object?>?> ToDictionary(); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<T?> ToObject<T>(JsonTypeInfo<T> jsonTypeInfo); }
extension(IObservableAsync<MqttApplicationMessageReceivedEventArgs> source) { public IObservableAsync<T?> ToObject<T>(Func<string, T?> deserialize); }
extension(IObservableAsync<Dictionary<string, object>> dictionary) { public IObservableAsync<object?> Observe(string key); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<bool> ToBool(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<byte> ToByte(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<short> ToInt16(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<int> ToInt32(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<long> ToInt64(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<float> ToSingle(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<double> ToDouble(); }
extension(IObservableAsync<object?> observable) { public IObservableAsync<string?> ToString(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, Action<MqttApplicationMessageBuilder> messageBuilder, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics( params string[] topics); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic( string topic); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry, TimeProvider timeProvider); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, string Payload)> message); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, string Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMessage( IObservableAsync<(string Topic, byte[] Payload)> message, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopics( params string[] topics); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<MqttApplicationMessageReceivedEventArgs> SubscribeToTopic( string topic); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics(); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<IEnumerable<(string Topic, DateTime LastSeen)>> DiscoverTopics( TimeSpan? topicExpiry, TimeProvider timeProvider); }
```
</details>

<a id="api-mqttnet-rx-client-observablebridgecompatibilityextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions</code></summary>

```text
public static class ObservableBridgeCompatibilityExtensions
extension<T>(IObservable<T> source) { public IObservableAsync<T> ToSignal(); }
extension<T>(IObservableAsync<T> source) { public IObservable<T> ToObservable(); }
```
</details>

<a id="api-mqttnet-rx-client-payloadextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.PayloadExtensions</code></summary>

```text
public static class PayloadExtensions
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<string> ToUtf8String(); }
extension(MqttApplicationMessageReceivedEventArgs e) { public ReadOnlySequence<byte> Payload(); }
extension(MqttApplicationMessageReceivedEventArgs e) { public string PayloadUtf8(); }
```
</details>

<a id="api-mqttnet-rx-client-reactiveclientoperations"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ReactiveClientOperations</code></summary>

```text
public static class ReactiveClientOperations
public static IObservable<RxUnit> Ping(IObservable<IMqttClient> client)
public static IObservableAsync<RxUnit> Ping(IObservableAsync<IMqttClient> client)
public static IObservable<RxUnit> PingPeriodically(IObservable<IMqttClient> client)
public static IObservable<RxUnit> PingPeriodically( IObservable<IMqttClient> client, TimeSpan? interval)
public static IObservableAsync<RxUnit> PingPeriodically(IObservableAsync<IMqttClient> client)
public static IObservableAsync<RxUnit> PingPeriodically( IObservableAsync<IMqttClient> client, TimeSpan? interval)
public static IObservable<MqttClientSubscribeResult> Subscribe( IObservable<IMqttClient> client, string[] topics)
public static IObservable<MqttClientSubscribeResult> Subscribe( IObservable<IMqttClient> client, string[] topics, MqttQualityOfServiceLevel qualityOfServiceLevel)
public static IObservable<MqttClientSubscribeResult> Subscribe( IObservable<IMqttClient> client, Action<MqttTopicFilterBuilder> topicFilterBuilder)
public static IObservable<MqttClientSubscribeResult> Subscribe( IObservable<IMqttClient> client, params MqttTopicFilter[] topicFilters)
public static IObservableAsync<MqttClientSubscribeResult> Subscribe( IObservableAsync<IMqttClient> client, string[] topics)
public static IObservableAsync<MqttClientSubscribeResult> Subscribe( IObservableAsync<IMqttClient> client, string[] topics, MqttQualityOfServiceLevel qualityOfServiceLevel)
public static IObservableAsync<MqttClientSubscribeResult> Subscribe( IObservableAsync<IMqttClient> client, Action<MqttTopicFilterBuilder> topicFilterBuilder)
public static IObservableAsync<MqttClientSubscribeResult> Subscribe( IObservableAsync<IMqttClient> client, params MqttTopicFilter[] topicFilters)
public static IObservable<MqttClientUnsubscribeResult> Unsubscribe( IObservable<IMqttClient> client, params string[] topics)
public static IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe( IObservableAsync<IMqttClient> client, params string[] topics)
public static IObservable<RxUnit> Disconnect(IObservable<IMqttClient> client)
public static IObservable<RxUnit> Disconnect( IObservable<IMqttClient> client, MqttClientDisconnectOptionsReason reason)
public static IObservableAsync<RxUnit> Disconnect(IObservableAsync<IMqttClient> client)
public static IObservableAsync<RxUnit> Disconnect( IObservableAsync<IMqttClient> client, MqttClientDisconnectOptionsReason reason)
public static IObservable<RxUnit> Reconnect(IObservable<IMqttClient> client)
public static IObservableAsync<RxUnit> Reconnect(IObservableAsync<IMqttClient> client)
public static IObservable<bool> ConnectionStatus(IObservable<IMqttClient> client)
public static IObservableAsync<bool> ConnectionStatus(IObservableAsync<IMqttClient> client)
public static IObservable<IMqttClient> WaitForConnection(IObservable<IMqttClient> client)
public static IObservable<IMqttClient> WaitForConnection( IObservable<IMqttClient> client, TimeSpan? timeout)
public static IObservableAsync<IMqttClient> WaitForConnection( IObservableAsync<IMqttClient> client)
public static IObservableAsync<IMqttClient> WaitForConnection( IObservableAsync<IMqttClient> client, TimeSpan? timeout)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, string payload)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, string payload, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, string payload, MqttQualityOfServiceLevel qos, bool retain)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, byte[] payload)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, byte[] payload, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain)
public static IObservable<MqttClientPublishResult> Publish( IObservable<IMqttClient> client, Action<MqttApplicationMessageBuilder> messageBuilder)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, string payload)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, string payload, MqttQualityOfServiceLevel qos)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, string payload, MqttQualityOfServiceLevel qos, bool retain)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, byte[] payload)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, byte[] payload, MqttQualityOfServiceLevel qos)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain)
public static IObservableAsync<MqttClientPublishResult> Publish( IObservableAsync<IMqttClient> client, Action<MqttApplicationMessageBuilder> messageBuilder)
public static IObservable<MqttClientPublishResult> PublishMany( IObservable<IMqttClient> client, IObservable<MqttApplicationMessage> messages)
public static IObservableAsync<MqttClientPublishResult> PublishMany( IObservableAsync<IMqttClient> client, IObservableAsync<MqttApplicationMessage> messages)
public static IObservable<MqttClientOptions?> GetOptions(IObservable<IMqttClient> client)
public static IObservableAsync<MqttClientOptions?> GetOptions( IObservableAsync<IMqttClient> client)
```
</details>

<a id="api-mqttnet-rx-client-reactiveclientoperationsextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ReactiveClientOperationsExtensions</code></summary>

```text
public static class ReactiveClientOperationsExtensions
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> Ping(); }
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> PingPeriodically(); }
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> PingPeriodically(TimeSpan? interval); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientSubscribeResult> Subscribe(string[] topics); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientSubscribeResult> Subscribe( string[] topics, MqttQualityOfServiceLevel qualityOfServiceLevel); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientSubscribeResult> Subscribe( Action<MqttTopicFilterBuilder> topicFilterBuilder); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientSubscribeResult> Subscribe( params MqttTopicFilter[] topicFilters); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientUnsubscribeResult> Unsubscribe(params string[] topics); }
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> Disconnect(); }
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> Disconnect(MqttClientDisconnectOptionsReason reason); }
extension(IObservable<IMqttClient> client) { public IObservable<RxUnit> Reconnect(); }
extension(IObservable<IMqttClient> client) { public IObservable<bool> ConnectionStatus(); }
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WaitForConnection(); }
extension(IObservable<IMqttClient> client) { public IObservable<IMqttClient> WaitForConnection(TimeSpan? timeout); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish(string topic, string payload); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish( string topic, string payload, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish( string topic, string payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish(string topic, byte[] payload); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish( string topic, byte[] payload, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish( string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> Publish( Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMany( IObservable<MqttApplicationMessage> messages); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientOptions?> GetOptions(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> Ping(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> PingPeriodically(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> PingPeriodically(TimeSpan? interval); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientSubscribeResult> Subscribe(string[] topics); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientSubscribeResult> Subscribe( string[] topics, MqttQualityOfServiceLevel qualityOfServiceLevel); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientSubscribeResult> Subscribe( Action<MqttTopicFilterBuilder> topicFilterBuilder); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientSubscribeResult> Subscribe( params MqttTopicFilter[] topicFilters); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientUnsubscribeResult> Unsubscribe(params string[] topics); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> Disconnect(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> Disconnect(MqttClientDisconnectOptionsReason reason); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<RxUnit> Reconnect(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<bool> ConnectionStatus(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IMqttClient> WaitForConnection(); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<IMqttClient> WaitForConnection(TimeSpan? timeout); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish(string topic, string payload); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish( string topic, string payload, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish( string topic, string payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish(string topic, byte[] payload); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish( string topic, byte[] payload, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish( string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> Publish( Action<MqttApplicationMessageBuilder> messageBuilder); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMany( IObservableAsync<MqttApplicationMessage> messages); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientOptions?> GetOptions(); }
```
</details>

<a id="api-mqttnet-rx-client-reconnectionresult"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ReconnectionResult</code></summary>

```text
public enum ReconnectionResult
StillConnected
Reconnected
Recovered
NotConnected
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttapplicationmessage"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttApplicationMessage</code></summary>

```text
public class ResilientMqttApplicationMessage
public Guid Id { get; set; } = Guid.NewGuid();
public MqttApplicationMessage? ApplicationMessage { get; set; }
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientfactory"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientFactory</code></summary>

```text
public static class ResilientMqttClientFactory
public static IResilientMqttClient Create(IMqttClient mqttClient, IMqttNetLogger logger)
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientoperationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientOperationExtensions</code></summary>

```text
public static class ResilientMqttClientOperationExtensions
extension(IResilientMqttClient client) { public IObservable<RxUnit> Enqueue(MqttApplicationMessage message); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Enqueue(ResilientMqttApplicationMessage message); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveEnqueue(MqttApplicationMessage message); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveEnqueue(ResilientMqttApplicationMessage message); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Ping(); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObservePing(); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Start(ResilientMqttClientOptions options); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Start(Action<ResilientMqttClientOptionsBuilder> configure); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveStart(ResilientMqttClientOptions options); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveStart(Action<ResilientMqttClientOptionsBuilder> configure); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Stop(); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Stop(bool cleanDisconnect); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveStop(); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveStop(bool cleanDisconnect); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Subscribe(IEnumerable<MqttTopicFilter> topicFilters); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveSubscribe(IEnumerable<MqttTopicFilter> topicFilters); }
extension(IResilientMqttClient client) { public IObservable<RxUnit> Unsubscribe(IEnumerable<string> topics); }
extension(IResilientMqttClient client) { public IObservableAsync<RxUnit> ObserveUnsubscribe(IEnumerable<string> topics); }
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientoptions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientOptions</code></summary>

```text
public sealed class ResilientMqttClientOptions
public MqttClientOptions? ClientOptions { get; set; }
public TimeSpan AutoReconnectDelay { get; set; } = DefaultAutoReconnectDelay;
public TimeSpan ConnectionCheckInterval { get; set; } = TimeSpan.FromSeconds(1);
public IResilientMqttClientStorage? Storage { get; set; }
public int MaxPendingMessages { get; set; } = int.MaxValue;
public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropNewMessage;
public int MaxTopicFiltersInSubscribeUnsubscribePackets { get; set; } = int.MaxValue;
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientoptionsbuilder"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientOptionsBuilder</code></summary>

```text
public class ResilientMqttClientOptionsBuilder
public ResilientMqttClientOptionsBuilder WithMaxPendingMessages(int value)
public ResilientMqttClientOptionsBuilder WithPendingMessagesOverflowStrategy( MqttPendingMessagesOverflowStrategy value)
public ResilientMqttClientOptionsBuilder WithAutoReconnectDelay(in TimeSpan value)
public ResilientMqttClientOptionsBuilder WithStorage(IResilientMqttClientStorage value)
public ResilientMqttClientOptionsBuilder WithClientOptions(MqttClientOptions value)
public ResilientMqttClientOptionsBuilder WithClientOptions(MqttClientOptionsBuilder builder)
public ResilientMqttClientOptionsBuilder WithClientOptions( Action<MqttClientOptionsBuilder> options)
public ResilientMqttClientOptionsBuilder WithMaxTopicFiltersInSubscribeUnsubscribePackets( int value)
public ResilientMqttClientOptions Build()
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientproperties"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientProperties</code></summary>

```text
public sealed record ResilientMqttClientProperties(IMqttClient InternalClient, bool IsConnected, bool IsStarted, ResilientMqttClientOptions? Options, int PendingApplicationMessagesCount)
public IMqttClient InternalClient { get; init; }
public bool IsConnected { get; init; }
public bool IsStarted { get; init; }
public ResilientMqttClientOptions? Options { get; init; }
public int PendingApplicationMessagesCount { get; init; }
```
</details>

<a id="api-mqttnet-rx-client-resilientmqttclientpropertyextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientMqttClientPropertyExtensions</code></summary>

```text
public static class ResilientMqttClientPropertyExtensions
extension(IResilientMqttClient client) { public ResilientMqttClientProperties Properties(); }
extension(IResilientMqttClient client) { public IObservable<T> Property<T>(Func<IResilientMqttClient, T> selector); }
extension(IResilientMqttClient client) { public IObservableAsync<T> ObserveProperty<T>(Func<IResilientMqttClient, T> selector); }
extension(IResilientMqttClient client) { public IObservable<ResilientMqttClientProperties> PropertySnapshots(); }
extension(IResilientMqttClient client) { public IObservableAsync<ResilientMqttClientProperties> ObservePropertySnapshots(); }
extension(IResilientMqttClient client) { public IObservable<SubscriptionsChangedEventArgs> SubscriptionsChanged(); }
```
</details>

<a id="api-mqttnet-rx-client-resilientprocessfailedeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.ResilientProcessFailedEventArgs</code></summary>

```text
public class ResilientProcessFailedEventArgs : EventArgs
public ResilientProcessFailedEventArgs( Exception exception, List<MqttTopicFilter>? addedSubscriptions, List<string>? removedSubscriptions)
public Exception Exception { get; }
public List<string> AddedSubscriptions { get; }
public List<string> RemovedSubscriptions { get; }
```
</details>

<a id="api-mqttnet-rx-client-subscriptionschangedeventargs"></a>
<details>
<summary><code>MQTTnet.Rx.Client.SubscriptionsChangedEventArgs</code></summary>

```text
public sealed class SubscriptionsChangedEventArgs( List<MqttClientSubscribeResult> subscribeResult, List<MqttClientUnsubscribeResult> unsubscribeResult) : EventArgs
public List<MqttClientSubscribeResult> SubscribeResult { get; } = subscribeResult ?? throw new ArgumentNullException(nameof(subscribeResult));
public List<MqttClientUnsubscribeResult> UnsubscribeResult { get; } = unsubscribeResult ?? throw new ArgumentNullException(nameof(unsubscribeResult));
```
</details>

<a id="api-mqttnet-rx-client-topicfilterextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Client.TopicFilterExtensions</code></summary>

```text
public static class TopicFilterExtensions
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicMatchesAny( params string[] topicFilters); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicIsNotMatch( string topicFilter); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<( MqttApplicationMessageReceivedEventArgs Message, Dictionary<string, string> Values)> ExtractTopicValues(string topicPattern); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<MqttApplicationMessageReceivedEventArgs> WhereTopicLevelCount( int levelCount); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<string> SelectTopicLevel(int levelIndex); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopic(); }
extension(IObservable<MqttApplicationMessageReceivedEventArgs> source) { public IObservable<RxLinq.IGroupedObservable< string, MqttApplicationMessageReceivedEventArgs >> GroupByTopicLevel(int levelIndex); }
```
</details>

<a id="api-mqttnet-rx-client-memoryefficient-spanparser"></a>
<details>
<summary><code>MQTTnet.Rx.Client.MemoryEfficient.SpanParser</code></summary>

```text
public delegate T SpanParser<out T>(ReadOnlySpan<byte> data);
```
</details>

<a id="mqttnetrxserver-api"></a>
### `MQTTnet.Rx.Server`

<a id="api-mqttnet-rx-server-create"></a>
<details>
<summary><code>MQTTnet.Rx.Server.Create</code></summary>

```text
public static class Create
public static MqttServerFactory MqttFactory { get; private set; } = new();
public static void NewMqttFactory(MqttServerFactory mqttFactory)
public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServer( Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerSignal( Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages( Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
public static IObservable<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessages( Func<MqttServerOptionsBuilder, MqttServerOptions> builder, string? retainedMessageDirectory)
public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessagesSignal( Func<MqttServerOptionsBuilder, MqttServerOptions> builder)
public static IObservableAsync<(MqttServer Server, MqttServerSession Disposable)> MqttServerWithRetainedMessagesSignal( Func<MqttServerOptionsBuilder, MqttServerOptions> builder, string? retainedMessageDirectory)
```
</details>

<a id="api-mqttnet-rx-server-imqttretainedmessagemodel"></a>
<details>
<summary><code>MQTTnet.Rx.Server.IMqttRetainedMessageModel</code></summary>

```text
public interface IMqttRetainedMessageModel
string? ContentType { get; set; }
byte[]? CorrelationData { get; init; }
byte[]? Payload { get; init; }
MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
string? ResponseTopic { get; set; }
string? Topic { get; set; }
List<MqttUserProperty>? UserProperties { get; init; }
static abstract MqttRetainedMessageModel Create(MqttApplicationMessage message);
MqttApplicationMessage ToApplicationMessage();
```
</details>

<a id="api-mqttnet-rx-server-mqttclientstatusextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttClientStatusExtensions</code></summary>

```text
public static class MqttClientStatusExtensions
extension(MqttClientStatus client) { public MqttClientStatusProperties Properties(); }
extension(MqttClientStatus client) { public IObservable<T> Property<T>(Func<MqttClientStatus, T> selector); }
extension(MqttClientStatus client) { public IObservableAsync<T> ObserveProperty<T>(Func<MqttClientStatus, T> selector); }
extension(MqttClientStatus client) { public IObservable<MqttClientStatusProperties> PropertySnapshots(); }
extension(MqttClientStatus client) { public IObservableAsync<MqttClientStatusProperties> ObservePropertySnapshots(); }
extension(MqttClientStatus client) { public IObservable<RxVoid> Disconnect(MqttServerClientDisconnectOptions options); }
extension(MqttClientStatus client) { public IObservable<RxVoid> Disconnect(Action<MqttServerClientDisconnectOptionsBuilder> configure); }
extension(MqttClientStatus client) { public IObservableAsync<RxVoid> ObserveDisconnect(MqttServerClientDisconnectOptions options); }
extension(MqttClientStatus client) { public IObservableAsync<RxVoid> ObserveDisconnect(Action<MqttServerClientDisconnectOptionsBuilder> configure); }
extension(MqttClientStatus client) { public IObservable<RxVoid> ResetStatisticsOperation(); }
extension(MqttClientStatus client) { public IObservableAsync<RxVoid> ObserveResetStatistics(); }
extension(MqttClientStatus client) { public MqttClientStatus WithSession(MqttSessionStatus session); }
```
</details>

<a id="api-mqttnet-rx-server-mqttclientstatusproperties"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttClientStatusProperties</code></summary>

```text
public sealed record MqttClientStatusProperties(long BytesReceived, long BytesSent, DateTimeOffset ConnectedTimestamp, EndPoint? RemoteEndPoint, string? Endpoint, string Id, DateTimeOffset LastNonKeepAlivePacketReceivedTimestamp, DateTimeOffset LastPacketReceivedTimestamp, DateTimeOffset LastPacketSentTimestamp, MqttProtocolVersion ProtocolVersion, long ReceivedApplicationMessagesCount, long ReceivedPacketsCount, long SentApplicationMessagesCount, long SentPacketsCount, MqttSessionStatus Session)
```
</details>

<a id="api-mqttnet-rx-server-mqttretainedmessagemodel"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttRetainedMessageModel</code></summary>

```text
public sealed class MqttRetainedMessageModel : IMqttRetainedMessageModel
public string? ContentType { get; set; }
public byte[]? CorrelationData { get; init; }
public byte[]? Payload { get; init; }
public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
public string? ResponseTopic { get; set; }
public string? Topic { get; set; }
public List<MqttUserProperty>? UserProperties { get; init; }
public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
public MqttApplicationMessage ToApplicationMessage()
```
</details>

<a id="api-mqttnet-rx-server-mqttserverconfigurationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerConfigurationExtensions</code></summary>

```text
public static class MqttServerConfigurationExtensions
extension(MqttServer server) { public MqttServer WithAcceptNewConnections(bool value); }
extension(MqttServer server) { public MqttServer WithServerSessionItem(object key, object value); }
extension(MqttServer server) { public MqttServer WithoutServerSessionItem(object key); }
extension(MqttServer server) { public MqttServer ClearServerSessionItems(); }
extension(MqttServer server) { public MqttServer ConfigureServer(Action<MqttServer> configure); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserverextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerExtensions</code></summary>

```text
public static class MqttServerExtensions
extension(MqttServer server) { public IObservable<ApplicationMessageEnqueuedEventArgs> ApplicationMessageEnqueuedOrDropped(); }
extension(MqttServer server) { public IObservableAsync<ApplicationMessageEnqueuedEventArgs> ObserveApplicationMessageEnqueuedOrDropped(); }
extension(MqttServer server) { public IObservable<ApplicationMessageNotConsumedEventArgs> ApplicationMessageNotConsumed(); }
extension(MqttServer server) { public IObservableAsync<ApplicationMessageNotConsumedEventArgs> ObserveApplicationMessageNotConsumed(); }
extension(MqttServer server) { public IObservable<ClientAcknowledgedPublishPacketEventArgs> ClientAcknowledgedPublishPacket(); }
extension(MqttServer server) { public IObservableAsync<ClientAcknowledgedPublishPacketEventArgs> ObserveClientAcknowledgedPublishPacket(); }
extension(MqttServer server) { public IObservable<ClientConnectedEventArgs> ClientConnected(); }
extension(MqttServer server) { public IObservableAsync<ClientConnectedEventArgs> ObserveClientConnected(); }
extension(MqttServer server) { public IObservable<ClientDisconnectedEventArgs> ClientDisconnected(); }
extension(MqttServer server) { public IObservableAsync<ClientDisconnectedEventArgs> ObserveClientDisconnected(); }
extension(MqttServer server) { public IObservable<ClientSubscribedTopicEventArgs> ClientSubscribedTopic(); }
extension(MqttServer server) { public IObservableAsync<ClientSubscribedTopicEventArgs> ObserveClientSubscribedTopic(); }
extension(MqttServer server) { public IObservable<ClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic(); }
extension(MqttServer server) { public IObservableAsync<ClientUnsubscribedTopicEventArgs> ObserveClientUnsubscribedTopic(); }
extension(MqttServer server) { public IObservable<InterceptingClientApplicationMessageEnqueueEventArgs> InterceptingClientEnqueue(); }
extension(MqttServer server) { public IObservableAsync<InterceptingClientApplicationMessageEnqueueEventArgs> ObserveInterceptingClientEnqueue(); }
extension(MqttServer server) { public IObservable<InterceptingPacketEventArgs> InterceptingInboundPacket(); }
extension(MqttServer server) { public IObservableAsync<InterceptingPacketEventArgs> ObserveInterceptingInboundPacket(); }
extension(MqttServer server) { public IObservable<InterceptingPacketEventArgs> InterceptingOutboundPacket(); }
extension(MqttServer server) { public IObservableAsync<InterceptingPacketEventArgs> ObserveInterceptingOutboundPacket(); }
extension(MqttServer server) { public IObservable<InterceptingPublishEventArgs> InterceptingPublish(); }
extension(MqttServer server) { public IObservableAsync<InterceptingPublishEventArgs> ObserveInterceptingPublish(); }
extension(MqttServer server) { public IObservable<InterceptingSubscriptionEventArgs> InterceptingSubscription(); }
extension(MqttServer server) { public IObservableAsync<InterceptingSubscriptionEventArgs> ObserveInterceptingSubscription(); }
extension(MqttServer server) { public IObservable<InterceptingUnsubscriptionEventArgs> InterceptingUnsubscription(); }
extension(MqttServer server) { public IObservableAsync<InterceptingUnsubscriptionEventArgs> ObserveInterceptingUnsubscription(); }
extension(MqttServer server) { public IObservable<LoadingRetainedMessagesEventArgs> LoadingRetainedMessage(); }
extension(MqttServer server) { public IObservableAsync<LoadingRetainedMessagesEventArgs> ObserveLoadingRetainedMessage(); }
extension(MqttServer server) { public IObservable<EventArgs> PreparingSession(); }
extension(MqttServer server) { public IObservableAsync<EventArgs> ObservePreparingSession(); }
extension(MqttServer server) { public IObservable<QueueMessageOverwrittenEventArgs> QueuedApplicationMessageOverwritten(); }
extension(MqttServer server) { public IObservableAsync<QueueMessageOverwrittenEventArgs> ObserveQueuedApplicationMessageOverwritten(); }
extension(MqttServer server) { public IObservable<RetainedMessageChangedEventArgs> RetainedMessageChanged(); }
extension(MqttServer server) { public IObservableAsync<RetainedMessageChangedEventArgs> ObserveRetainedMessageChanged(); }
extension(MqttServer server) { public IObservable<EventArgs> RetainedMessagesCleared(); }
extension(MqttServer server) { public IObservableAsync<EventArgs> ObserveRetainedMessagesCleared(); }
extension(MqttServer server) { public IObservable<SessionDeletedEventArgs> SessionDeleted(); }
extension(MqttServer server) { public IObservableAsync<SessionDeletedEventArgs> ObserveSessionDeleted(); }
extension(MqttServer server) { public IObservable<EventArgs> Started(); }
extension(MqttServer server) { public IObservableAsync<EventArgs> ObserveStarted(); }
extension(MqttServer server) { public IObservable<EventArgs> Stopped(); }
extension(MqttServer server) { public IObservableAsync<EventArgs> ObserveStopped(); }
extension(MqttServer server) { public IObservable<ValidatingConnectionEventArgs> ValidatingConnection(); }
extension(MqttServer server) { public IObservableAsync<ValidatingConnectionEventArgs> ObserveValidatingConnection(); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserveroperationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerOperationExtensions</code></summary>

```text
public static class MqttServerOperationExtensions
extension(MqttServer server) { public IObservable<RxVoid> DeleteRetainedMessages(); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveDeleteRetainedMessages(); }
extension(MqttServer server) { public IObservable<RxVoid> DisconnectClient(string clientId, MqttServerClientDisconnectOptions options); }
extension(MqttServer server) { public IObservable<RxVoid> DisconnectClient(string clientId, Action<MqttServerClientDisconnectOptionsBuilder> configure); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveDisconnectClient(string clientId, MqttServerClientDisconnectOptions options); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveDisconnectClient(string clientId, Action<MqttServerClientDisconnectOptionsBuilder> configure); }
extension(MqttServer server) { public IObservable<IList<MqttClientStatus>> GetClients(); }
extension(MqttServer server) { public IObservableAsync<IList<MqttClientStatus>> ObserveClients(); }
extension(MqttServer server) { public IObservable<MqttApplicationMessage> GetRetainedMessage(string topic); }
extension(MqttServer server) { public IObservableAsync<MqttApplicationMessage> ObserveRetainedMessage(string topic); }
extension(MqttServer server) { public IObservable<IList<MqttApplicationMessage>> GetRetainedMessages(); }
extension(MqttServer server) { public IObservableAsync<IList<MqttApplicationMessage>> ObserveRetainedMessages(); }
extension(MqttServer server) { public IObservable<MqttSessionStatus> GetSession(string clientId); }
extension(MqttServer server) { public IObservableAsync<MqttSessionStatus> ObserveSession(string clientId); }
extension(MqttServer server) { public IObservable<IList<MqttSessionStatus>> GetSessions(); }
extension(MqttServer server) { public IObservableAsync<IList<MqttSessionStatus>> ObserveSessions(); }
extension(MqttServer server) { public IObservable<RxVoid> InjectApplicationMessageOperation(InjectedMqttApplicationMessage message); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveInjectApplicationMessage(InjectedMqttApplicationMessage message); }
extension(MqttServer server) { public IObservable<RxVoid> Start(); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveStart(); }
extension(MqttServer server) { public IObservable<RxVoid> Stop(); }
extension(MqttServer server) { public IObservable<RxVoid> Stop(MqttServerStopOptions options); }
extension(MqttServer server) { public IObservable<RxVoid> Stop(Action<MqttServerStopOptionsBuilder> configure); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveStop(); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveStop(MqttServerStopOptions options); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveStop(Action<MqttServerStopOptionsBuilder> configure); }
extension(MqttServer server) { public IObservable<RxVoid> SubscribeClient(string clientId, ICollection<MqttTopicFilter> topicFilters); }
extension(MqttServer server) { public IObservable<RxVoid> SubscribeClient(string clientId, Action<MqttTopicFilterBuilder> configure); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveSubscribeClient(string clientId, ICollection<MqttTopicFilter> topicFilters); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveSubscribeClient(string clientId, Action<MqttTopicFilterBuilder> configure); }
extension(MqttServer server) { public IObservable<RxVoid> UnsubscribeClient(string clientId, ICollection<string> topicFilters); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveUnsubscribeClient(string clientId, ICollection<string> topicFilters); }
extension(MqttServer server) { public IObservable<RxVoid> UpdateRetainedMessage(MqttApplicationMessage message); }
extension(MqttServer server) { public IObservableAsync<RxVoid> ObserveUpdateRetainedMessage(MqttApplicationMessage message); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserveroptionsconfigurationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerOptionsConfigurationExtensions</code></summary>

```text
public static class MqttServerOptionsConfigurationExtensions
extension(MqttServerClientDisconnectOptionsBuilder builder) { public MqttServerClientDisconnectOptionsBuilder ConfigureOptions(Action<MqttServerClientDisconnectOptions> configure); }
extension(MqttServerOptionsBuilder builder) { public MqttServerOptionsBuilder ConfigureOptions(Action<MqttServerOptions> configure); }
extension(MqttServerStopOptionsBuilder builder) { public MqttServerStopOptionsBuilder ConfigureOptions(Action<MqttServerStopOptions> configure); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserverproperties"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerProperties</code></summary>

```text
public sealed record MqttServerProperties(bool AcceptNewConnections, bool IsStarted, IReadOnlyDictionary<object, object?> ServerSessionItems)
```
</details>

<a id="api-mqttnet-rx-server-mqttserverpropertyextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerPropertyExtensions</code></summary>

```text
public static class MqttServerPropertyExtensions
extension(MqttServer server) { public MqttServerProperties Properties(); }
extension(MqttServer server) { public IObservable<T> Property<T>(Func<MqttServer, T> selector); }
extension(MqttServer server) { public IObservableAsync<T> ObserveProperty<T>(Func<MqttServer, T> selector); }
extension(MqttServer server) { public IObservable<MqttServerProperties> PropertySnapshots(); }
extension(MqttServer server) { public IObservableAsync<MqttServerProperties> ObservePropertySnapshots(); }
extension(MqttServer server) { public IObservable<bool> AcceptNewConnectionsValue(); }
extension(MqttServer server) { public IObservableAsync<bool> ObserveAcceptNewConnections(); }
extension(MqttServer server) { public IObservable<IReadOnlyDictionary<object, object?>> ServerSessionItemsSnapshot(); }
extension(MqttServer server) { public IObservableAsync<IReadOnlyDictionary<object, object?>> ObserveServerSessionItemsSnapshot(); }
extension(MqttServer server) { public IObservable<bool> IsStartedChanges(); }
extension(MqttServer server) { public IObservableAsync<bool> ObserveIsStartedChanges(); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserversequenceconfigurationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerSequenceConfigurationExtensions</code></summary>

```text
public static class MqttServerSequenceConfigurationExtensions
extension(IObservable<MqttServer> servers) { public IObservable<MqttServer> ConfigureServer(Action<MqttServer> configure); }
extension(IObservableAsync<MqttServer> servers) { public IObservableAsync<MqttServer> ConfigureServer(Action<MqttServer> configure); }
```
</details>

<a id="api-mqttnet-rx-server-mqttserversession"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttServerSession</code></summary>

```text
public sealed class MqttServerSession : IDisposable, IAsyncDisposable
public bool IsDisposed { get; }
public MqttServer Server { get; }
public void Add(IDisposable resource)
public void Dispose()
public async ValueTask DisposeAsync()
```
</details>

<a id="api-mqttnet-rx-server-mqttsessionenqueueresult"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttSessionEnqueueResult</code></summary>

```text
public sealed record MqttSessionEnqueueResult(bool IsEnqueued, InjectMqttApplicationMessageResult? InjectResult)
```
</details>

<a id="api-mqttnet-rx-server-mqttsessionstatusextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttSessionStatusExtensions</code></summary>

```text
public static class MqttSessionStatusExtensions
extension(MqttSessionStatus session) { public MqttSessionStatusProperties Properties(); }
extension(MqttSessionStatus session) { public IObservable<T> Property<T>(Func<MqttSessionStatus, T> selector); }
extension(MqttSessionStatus session) { public IObservableAsync<T> ObserveProperty<T>(Func<MqttSessionStatus, T> selector); }
extension(MqttSessionStatus session) { public IObservable<MqttSessionStatusProperties> PropertySnapshots(); }
extension(MqttSessionStatus session) { public IObservableAsync<MqttSessionStatusProperties> ObservePropertySnapshots(); }
extension(MqttSessionStatus session) { public MqttSessionStatus WithSessionItem(object key, object value); }
extension(MqttSessionStatus session) { public MqttSessionStatus WithoutSessionItem(object key); }
extension(MqttSessionStatus session) { public MqttSessionStatus ClearSessionItems(); }
extension(MqttSessionStatus session) { public IObservable<RxVoid> ClearApplicationMessagesQueue(); }
extension(MqttSessionStatus session) { public IObservableAsync<RxVoid> ObserveClearApplicationMessagesQueue(); }
extension(MqttSessionStatus session) { public IObservable<RxVoid> Delete(); }
extension(MqttSessionStatus session) { public IObservableAsync<RxVoid> ObserveDelete(); }
extension(MqttSessionStatus session) { public IObservable<InjectMqttApplicationMessageResult> DeliverApplicationMessage(MqttApplicationMessage message); }
extension(MqttSessionStatus session) { public IObservableAsync<InjectMqttApplicationMessageResult> ObserveDeliverApplicationMessage(MqttApplicationMessage message); }
extension(MqttSessionStatus session) { public IObservable<MqttSessionEnqueueResult> TryEnqueueApplicationMessage(MqttApplicationMessage message); }
extension(MqttSessionStatus session) { public IObservableAsync<MqttSessionEnqueueResult> ObserveTryEnqueueApplicationMessage(MqttApplicationMessage message); }
```
</details>

<a id="api-mqttnet-rx-server-mqttsessionstatusproperties"></a>
<details>
<summary><code>MQTTnet.Rx.Server.MqttSessionStatusProperties</code></summary>

```text
public sealed record MqttSessionStatusProperties(DateTimeOffset CreatedTimestamp, DateTimeOffset? DisconnectedTimestamp, uint ExpiryInterval, string Id, IReadOnlyDictionary<object, object?> Items, long PendingApplicationMessagesCount)
```
</details>

<a id="api-mqttnet-rx-server-validatingconnectionoperationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Server.ValidatingConnectionOperationExtensions</code></summary>

```text
public static class ValidatingConnectionOperationExtensions
extension(ValidatingConnectionEventArgs eventArgs) { public IObservable<ExchangeEnhancedAuthenticationResult> ExchangeEnhancedAuthentication(ExchangeEnhancedAuthenticationOptions options); }
extension(ValidatingConnectionEventArgs eventArgs) { public IObservableAsync<ExchangeEnhancedAuthenticationResult> ObserveExchangeEnhancedAuthentication(ExchangeEnhancedAuthenticationOptions options); }
```
</details>

<a id="mqttnetrxaspnetcore-api"></a>
### `MQTTnet.Rx.AspNetCore`

<a id="api-mqttnet-rx-aspnetcore-mqttaspnetcorehostingextensions"></a>
<details>
<summary><code>MQTTnet.Rx.AspNetCore.MqttAspNetCoreHostingExtensions</code></summary>

```text
public static class MqttAspNetCoreHostingExtensions
extension(IApplicationBuilder app) { public IApplicationBuilder ConfigureMqttServer(Action<MqttServer> configure); }
extension(IConnectionBuilder builder) { public IConnectionBuilder UseMqttConnectionHandler(); }
extension(IEndpointRouteBuilder endpoints) { public IEndpointRouteBuilder MapMqttEndpoint(string pattern); }
```
</details>

<a id="api-mqttnet-rx-aspnetcore-mqttaspnetcoreservicecollectionextensions"></a>
<details>
<summary><code>MQTTnet.Rx.AspNetCore.MqttAspNetCoreServiceCollectionExtensions</code></summary>

```text
public static class MqttAspNetCoreServiceCollectionExtensions
extension(IServiceCollection services) { public IServiceCollection WithHostedMqttServer(); }
extension(IServiceCollection services) { public IServiceCollection WithHostedMqttServer(MqttServerOptions options); }
extension(IServiceCollection services) { public IServiceCollection WithHostedMqttServer(Action<MqttServerOptionsBuilder>? configure); }
extension(IServiceCollection services) { public IServiceCollection WithHostedMqttServerServices(Action<AspNetMqttServerOptionsBuilder> configure); }
extension(IServiceCollection services) { public IServiceCollection WithMqttConnectionHandler(); }
extension(IServiceCollection services) { public IServiceCollection WithMqttConnections(); }
extension(IServiceCollection services) { public IServiceCollection WithMqttLogger(IMqttNetLogger logger); }
extension(IServiceCollection services) { public IServiceCollection WithMqttServer(); }
extension(IServiceCollection services) { public IServiceCollection WithMqttServer(Action<MqttServerOptionsBuilder>? configure); }
extension(IServiceCollection services) { public IServiceCollection WithMqttTcpServerAdapter(); }
extension(IServiceCollection services) { public IServiceCollection WithMqttWebSocketServerAdapter(); }
```
</details>

<a id="api-mqttnet-rx-aspnetcore-mqttconnectioncontextextensions"></a>
<details>
<summary><code>MQTTnet.Rx.AspNetCore.MqttConnectionContextExtensions</code></summary>

```text
public static class MqttConnectionContextExtensions
extension(MqttConnectionContext connection) { public MqttConnectionProperties Properties(); }
extension(MqttConnectionContext connection) { public MqttConnectionContext ResetConnectionStatistics(); }
extension(MqttConnectionContext connection) { public IObservable<RxVoid> Connect(); }
extension(MqttConnectionContext connection) { public IObservableAsync<RxVoid> ConnectSignal(); }
extension(MqttConnectionContext connection) { public IObservable<RxVoid> Disconnect(); }
extension(MqttConnectionContext connection) { public IObservableAsync<RxVoid> DisconnectSignal(); }
extension(MqttConnectionContext connection) { public IObservable<MqttPacket> ReceivePacket(); }
extension(MqttConnectionContext connection) { public IObservableAsync<MqttPacket> ReceivePacketSignal(); }
extension(MqttConnectionContext connection) { public IObservable<RxVoid> SendPacket(MqttPacket packet); }
extension(MqttConnectionContext connection) { public IObservableAsync<RxVoid> SendPacketSignal(MqttPacket packet); }
```
</details>

<a id="api-mqttnet-rx-aspnetcore-mqttconnectionproperties"></a>
<details>
<summary><code>MQTTnet.Rx.AspNetCore.MqttConnectionProperties</code></summary>

```text
public sealed record MqttConnectionProperties( long BytesReceived, long BytesSent, X509Certificate2? ClientCertificate, bool IsSecureConnection, EndPoint? LocalEndPoint, EndPoint? RemoteEndPoint, MqttPacketFormatterAdapter PacketFormatterAdapter)
public long BytesReceived { get; init; }
public long BytesSent { get; init; }
public X509Certificate2? ClientCertificate { get; init; }
public bool IsSecureConnection { get; init; }
public EndPoint? LocalEndPoint { get; init; }
public EndPoint? RemoteEndPoint { get; init; }
public MqttPacketFormatterAdapter PacketFormatterAdapter { get; init; }
```
</details>

<a id="api-mqttnet-rx-aspnetcore-mqtthostedserverextensions"></a>
<details>
<summary><code>MQTTnet.Rx.AspNetCore.MqttHostedServerExtensions</code></summary>

```text
public static class MqttHostedServerExtensions
extension(MqttHostedServer server) { public MqttHostedServer WithAcceptNewConnections(bool acceptNewConnections); }
extension(MqttHostedServer server) { public MqttHostedServer WithServerSessionItem(object key, object value); }
extension(MqttHostedServer server) { public MqttHostedServer ConfigureHostedServer(Action<MqttHostedServer> configure); }
extension(MqttHostedServer server) { public IObservable<bool> IsStartedChanges(); }
extension(MqttHostedServer server) { public IObservableAsync<bool> ObserveIsStarted(); }
```
</details>

<a id="industrial-package-api"></a>
### `MQTTnet.Rx.ABPlc`

<a id="api-mqttnet-rx-abplc-create"></a>
<details>
<summary><code>MQTTnet.Rx.ABPlc.Create</code></summary>

```text
public static class Create
public static IObservable<MqttClientPublishResult> PublishABPlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness)
public static IDisposable SubscribeABPlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory)
public static IDisposable SubscribeABPlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory)
```
</details>

<a id="api-mqttnet-rx-abplc-createextensions"></a>
<details>
<summary><code>MQTTnet.Rx.ABPlc.CreateExtensions</code></summary>

```text
public static class CreateExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-abplc-observableasynccreateextensionmixins"></a>
<details>
<summary><code>MQTTnet.Rx.ABPlc.ObservableAsyncCreateExtensionMixins</code></summary>

```text
public static class ObservableAsyncCreateExtensionMixins
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeABPlcTag<T>( string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-abplc-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.ABPlc.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
public static IObservableAsync<MqttClientPublishResult> PublishABPlcTag<T>( IObservableAsync<IMqttClient> client, string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness)
public static IObservableAsync<ApplicationMessageProcessedEventArgs> PublishABPlcTag<T>( IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, IABPlcRx plc, params T[] typeWitness)
public static IDisposable SubscribeABPlcTag<T>( IObservableAsync<IMqttClient> client, string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory)
public static IDisposable SubscribeABPlcTag<T>( IObservableAsync<IResilientMqttClient> client, string topic, string plcVariable, IABPlcRx plc, Func<string, T> payloadFactory)
```
</details>

### `MQTTnet.Rx.Mitsubishi`

<a id="api-mqttnet-rx-mitsubishi-mitsubishimqttextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Mitsubishi.MitsubishiMqttExtensions</code></summary>

```text
public static class MitsubishiMqttExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<T, string> payloadFormatter); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<string, T> payloadParser, Action<Exception>? onError, CancellationToken cancellationToken); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<T, string> payloadFormatter); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<string, T> payloadParser, Action<Exception>? onError, CancellationToken cancellationToken); }
```
</details>

<a id="api-mqttnet-rx-mitsubishi-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Mitsubishi.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<T, string> payloadFormatter); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<string, T> payloadParser, Action<Exception>? onError, CancellationToken cancellationToken); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<T, string> payloadFormatter); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeMitsubishiTag<T>( string topic, LogicalTagKey<T> tag, MitsubishiLogicalTagClient logicalTags, Func<string, T> payloadParser, Action<Exception>? onError, CancellationToken cancellationToken); }
```
</details>

### `MQTTnet.Rx.Modbus`

<a id="api-mqttnet-rx-modbus-create"></a>
<details>
<summary><code>MQTTnet.Rx.Modbus.Create</code></summary>

```text
public static class Create
public static IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromMaster( ModbusIpMaster master)
public static IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> FromFactory( Func<ModbusIpMaster> factory)
public static IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> PublishInputs( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<MqttClientPublishResult> PublishInputs( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<MqttClientPublishResult> PublishInputs( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> PublishCoils( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<MqttClientPublishResult> PublishCoils( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<MqttClientPublishResult> PublishCoils( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos)
public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull
public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull
public static IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull
public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull
public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull
public static IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull
public static IDisposable SubscribeWrite<T>( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer)
public static IDisposable SubscribeWrite<T>( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync)
public static IDisposable SubscribeWrite<T>( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer)
public static IDisposable SubscribeWrite<T>( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync)
public static IDisposable SubscribeWriteSingleRegister( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, ushort> writer)
public static IDisposable SubscribeWriteSingleRegister( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, ushort> writer)
public static IDisposable SubscribeWriteMultipleRegisters( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, ushort[]> writer)
public static IDisposable SubscribeWriteMultipleRegisters( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, ushort[]> writer)
public static IDisposable SubscribeWriteSingleCoil( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, bool> writer)
public static IDisposable SubscribeWriteSingleCoil( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, bool> writer)
public static IDisposable SubscribeWriteMultipleCoils( IObservable<IMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, bool[]> writer)
public static IDisposable SubscribeWriteMultipleCoils( IObservable<IResilientMqttClient> client, IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, bool[]> writer)
public static string Serialize(object? value)
public static T? DeSerialize<T>(string value, params T[] typeWitness)
```
</details>

<a id="api-mqttnet-rx-modbus-createextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Modbus.CreateExtensions</code></summary>

```text
public static partial class CreateExtensions
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull; }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull; }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull; }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWrite<T>( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWrite<T>( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWriteSingleRegister( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, ushort> writer); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWriteMultipleRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, ushort[]> writer); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWriteSingleCoil( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, bool> writer); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeWriteMultipleCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, bool[]> writer); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishHoldingRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishInputs( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull; }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull; }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishModbus<TPayload>( IObservable<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull; }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWrite<T>( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Action<ModbusIpMaster, T> writer); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWrite<T>( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, Func<string, T> parse, Func<ModbusIpMaster, T, Task> writerAsync); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWriteSingleRegister( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, ushort> writer); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWriteMultipleRegisters( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, ushort[]> writer); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWriteSingleCoil( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort address, Action<ModbusIpMaster, ushort, bool> writer); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeWriteMultipleCoils( IObservable<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, Action<ModbusIpMaster, ushort, bool[]> writer); }
```
</details>

<a id="api-mqttnet-rx-modbus-observableasynccreateextensionmixins"></a>
<details>
<summary><code>MQTTnet.Rx.Modbus.ObservableAsyncCreateExtensionMixins</code></summary>

```text
public static partial class ObservableAsyncCreateExtensionMixins
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull; }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull; }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull; }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishHoldingRegisters( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishInputs( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishCoils( IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)> modbus, string topic, ushort startAddress, ushort numberOfPoints, double interval, MqttQualityOfServiceLevel qos, bool retain); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory) where TPayload : notnull; }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos) where TPayload : notnull; }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishModbus<TPayload>( IObservableAsync<(bool Connected, Exception? Error, object? Data)> reader, string topic, Func<object, TPayload> payloadFactory, MqttQualityOfServiceLevel qos, bool retain) where TPayload : notnull; }
```
</details>

<a id="api-mqttnet-rx-modbus-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Modbus.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
public static Func< ModbusIpMaster, IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)>> FromMasterAsync { get; } = FromMaster;
public static Func< Func<ModbusIpMaster>, IObservableAsync<(bool Connected, Exception? Error, ModbusIpMaster? Master)>> FromFactoryAsync { get; } = FromFactory;
```
</details>

<a id="api-mqttnet-rx-modbus-serializationextensions"></a>
<details>
<summary><code>MQTTnet.Rx.Modbus.SerializationExtensions</code></summary>

```text
public static class SerializationExtensions
extension(object? value) { public string Serialize(); }
extension(string value) { public T? DeSerialize<T>(params T[] typeWitness); }
```
</details>

### `MQTTnet.Rx.OmronPlc`

<a id="api-mqttnet-rx-omronplc-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.OmronPlc.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc, Func<string, T> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc, Func<string, T> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-omronplc-omronplccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.OmronPlc.OmronPlcCreateExtensions</code></summary>

```text
public static class OmronPlcCreateExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc, Func<string, T> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeOmronPlcTag<T>( string topic, LogicalTagKey<T> tag, IOmronPlcRx plc, Func<string, T> payloadFactory); }
```
</details>

### `MQTTnet.Rx.S7Plc`

<a id="api-mqttnet-rx-s7plc-create"></a>
<details>
<summary><code>MQTTnet.Rx.S7Plc.Create</code></summary>

```text
public static class Create
public static IObservable<MqttClientPublishResult> PublishS7PlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IRxS7 plc, params T[] typeWitness)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IRxS7 plc, params T[] typeWitness)
public static void SubscribeS7PlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IRxS7 plc, Func<string, T> payloadFactory)
public static void SubscribeS7PlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IRxS7 plc, Func<string, T> payloadFactory)
```
</details>

<a id="api-mqttnet-rx-s7plc-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.S7Plc.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc, Func<string, T> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc, Func<string, T> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-s7plc-s7plcextensions"></a>
<details>
<summary><code>MQTTnet.Rx.S7Plc.S7PlcExtensions</code></summary>

```text
public static class S7PlcExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc, Func<string, T> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeS7PlcTag<T>( string topic, LogicalTagKey<T> tag, IRxS7 plc, Func<string, T> payloadFactory); }
```
</details>

### `MQTTnet.Rx.SerialPort`

<a id="api-mqttnet-rx-serialport-create"></a>
<details>
<summary><code>MQTTnet.Rx.SerialPort.Create</code></summary>

```text
public static class Create
public static IObservable<MqttClientPublishResult> PublishSerialPort( IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort( IObservable<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut)
public static IDisposable SubscribeSerialPortWriteLine( IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory)
public static IDisposable SubscribeSerialPortWriteLine( IObservable<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory)
public static IDisposable SubscribeSerialPortWrite( IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory)
public static IDisposable SubscribeSerialPortWrite( IObservable<IMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory)
public static IDisposable SubscribeSerialPortWrite( IObservable<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory)
public static IDisposable SubscribeSerialPortWrite( IObservable<IResilientMqttClient> client, string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory)
```
</details>

<a id="api-mqttnet-rx-serialport-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.SerialPort.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishSerialPort( string topic, ISerialPortRx serialPort, IObservableAsync<char> startsWith, IObservableAsync<char> endsWith, int timeOut); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeSerialPortWriteLine( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishSerialPort( string topic, ISerialPortRx serialPort, IObservableAsync<char> startsWith, IObservableAsync<char> endsWith, int timeOut); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWriteLine( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-serialport-serialportmqttextensions"></a>
<details>
<summary><code>MQTTnet.Rx.SerialPort.SerialPortMqttExtensions</code></summary>

```text
public static class SerialPortMqttExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishSerialPort( string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeSerialPortWriteLine( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishSerialPort( string topic, ISerialPortRx serialPort, IObservable<char> startsWith, IObservable<char> endsWith, int timeOut); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWriteLine( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, string> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeSerialPortWrite( string topic, ISerialPortRx serialPort, Func<string, byte[]> payloadFactory); }
```
</details>

### `MQTTnet.Rx.TwinCAT`

<a id="api-mqttnet-rx-twincat-create"></a>
<details>
<summary><code>MQTTnet.Rx.TwinCAT.Create</code></summary>

```text
public static class Create
public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness)
public static IObservable<MqttClientPublishResult> PublishTcPlcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness)
public static IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness)
public static void SubscribeTcTag<T>( IObservable<IMqttClient> client, string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory)
public static void SubscribeTcTag<T>( IObservable<IResilientMqttClient> client, string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory)
```
</details>

<a id="api-mqttnet-rx-twincat-createextensions"></a>
<details>
<summary><code>MQTTnet.Rx.TwinCAT.CreateExtensions</code></summary>

```text
public static class CreateExtensions
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness); }
extension(IObservable<IMqttClient> client) { public IObservable<MqttClientPublishResult> PublishTcPlcTag<T>( string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness); }
extension(IObservable<IMqttClient> client) { public IDisposable SubscribeTcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness); }
extension(IObservable<IResilientMqttClient> client) { public IObservable<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness); }
extension(IObservable<IResilientMqttClient> client) { public IDisposable SubscribeTcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory); }
```
</details>

<a id="api-mqttnet-rx-twincat-observableasynccreateextensions"></a>
<details>
<summary><code>MQTTnet.Rx.TwinCAT.ObservableAsyncCreateExtensions</code></summary>

```text
public static class ObservableAsyncCreateExtensions
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness); }
extension(IObservableAsync<IMqttClient> client) { public IObservableAsync<MqttClientPublishResult> PublishTcPlcTag<T>( string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness); }
extension(IObservableAsync<IMqttClient> client) { public IDisposable SubscribeTcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, params T[] typeWitness); }
extension(IObservableAsync<IResilientMqttClient> client) { public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcPlcTag<T>( string topic, string plcVariable, IHashTableRx plc, params T[] typeWitness); }
extension(IObservableAsync<IResilientMqttClient> client) { public IDisposable SubscribeTcTag<T>( string topic, string plcVariable, IRxTcAdsClient plc, Func<string, T> payloadFactory); }
```
</details>

<!-- PUBLIC_API_END -->

## Building the repository

The repository is pinned by `global.json` to .NET SDK `11.0.100-preview.6.26359.118`. On Windows, use a Visual Studio version that supports that SDK and the .NET 11 targets.

If the exact SDK is not installed machine-wide, bootstrap the repository-local SDK before opening the solution:

```powershell
.\build.ps1
```

On macOS or Linux:

```bash
./build.sh
```

Restart Visual Studio after bootstrapping, then open `src/MQTTnet.Rx.slnx`.

## Contributing

Issues and pull requests are welcome. Keep public API additions documented in this README and include tests for behavior changes.

## License

MQTTnet.Rx is licensed under the [MIT License](LICENSE).

---

**MQTTnet.Rx** - Empowering Industrial Automation with Reactive Technology ⚡🏭
