// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using ConsoleTools;
using CP.IO.Ports;
using MQTTnet.Rx.SerialPort;

namespace MQTTnet.Rx.Client.TestApp;

internal static class Program
{
    private static readonly Subject<(string topic, string payload)> _message = new();
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private static CompositeDisposable _disposables = [];

    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    /// <param name="args">The arguments.</param>
    private static void Main(string[] args)
    {
        var publishMenu = new ConsoleMenu(args, level: 1)
            .Add("Publish Client", PublishClient)
            .Add("Publish Resilient", PublishResilientClient)
            .Add("Close", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = true;
                config.Title = "Publish Submenu";
                config.EnableBreadcrumb = true;
                config.WriteBreadcrumbAction = titles => Console.WriteLine(string.Join(" / ", titles));
            });
        var subscribeMenu = new ConsoleMenu(args, level: 1)
            .Add("Subscribe Client", SubscribeClient)
            .Add("Subscribe Resilient Client", SubscribeResilientClient)
            .Add("Discover Resilient Client", DiscoverTopicsManagedClient)
            .Add("Close", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = true;
                config.Title = "Subscribe Submenu";
                config.EnableBreadcrumb = true;
                config.WriteBreadcrumbAction = titles => Console.WriteLine(string.Join(" / ", titles));
            });
        var serialPortMenu = new ConsoleMenu(args, level: 1)
            .Add("Start Temperature Controller Simulator (COM2)", StartTemperatureControllerSimulator)
            .Add("Serial to MQTT Bridge (COM1)", SerialToMqttBridge)
            .Add("MQTT to Serial Bridge (COM1)", MqttToSerialBridge)
            .Add("Full Demo (requires both COMs)", SerialPortFullDemo)
            .Add("Close", ConsoleMenu.Close)
            .Configure(config =>
            {
                config.Selector = "--> ";
                config.EnableFilter = true;
                config.Title = "Serial Port Submenu (COM1 <-> COM2 via com0com)";
                config.EnableBreadcrumb = true;
                config.WriteBreadcrumbAction = titles => Console.WriteLine(string.Join(" / ", titles));
            });
        new ConsoleMenu(args, level: 0)
           .Add("Publish", publishMenu.Show)
           .Add("Subscribe", subscribeMenu.Show)
           .Add("Serial Port", serialPortMenu.Show)
           .Add("Close", ConsoleMenu.Close)
           .Configure(config =>
           {
               config.Title = "MQTTnet.Rx.Client Example";
               config.EnableWriteTitle = true;
               config.WriteHeaderAction = () => Console.WriteLine("Please select a mode:");
           })
           .Show();
    }

    private static void PublishResilientClient()
    {
        _disposables.Add(Create.ResilientMqttClient()
         .WithResilientClientOptions(a =>
             a.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                 .WithClientOptions(c =>
                     c.WithTcpServer("localhost", 9000)))
         .PublishMessage(_message)
         .Subscribe(r => Console.WriteLine($"{r.ApplicationMessage.Id} [{r.ApplicationMessage.ApplicationMessage?.Topic}] value : {r.ApplicationMessage.ApplicationMessage.ConvertPayloadToString()}")));
        StartMessages("managed/");
        WaitForExit();
    }

    private static void PublishClient()
    {
        _disposables.Add(Create.MqttClient()
            .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
            .PublishMessage(_message)
            .Subscribe(r => Console.WriteLine($"{r.ReasonCode} [{r.PacketIdentifier}]")));
        StartMessages("unmanaged/");
        WaitForExit();
    }

    private static void SubscribeClient()
    {
        _disposables.Add(Create.MqttClient().WithClientOptions(a => a.WithTcpServer("localhost", 9000))
            .SubscribeToTopic("unmanaged/FromMilliseconds")
            .Do(r => Console.WriteLine($"{r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"))
            .ToDictionary()
            .Subscribe(dict =>
            {
                foreach (var item in dict!)
                {
                    Console.WriteLine($"key: {item.Key} value: {item.Value}");
                }
            }));
        WaitForExit();
    }

    private static void SubscribeResilientClient()
    {
        _disposables.Add(Create.ResilientMqttClient()
            .WithResilientClientOptions(a =>
             a.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                 .WithClientOptions(c =>
                     c.WithTcpServer("localhost", 9000)))
            .SubscribeToTopic("+/FromMilliseconds")
            .Do(r => Console.WriteLine($"{r.ReasonCode} [{r.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ConvertPayloadToString()}"))
            .ToDictionary()
            .Subscribe(dict =>
            {
                foreach (var item in dict!)
                {
                    Console.WriteLine($"key: {item.Key} value: {item.Value}");
                }
            }));
        WaitForExit();
    }

    private static void DiscoverTopicsManagedClient()
    {
        _disposables.Add(Create.MqttClient()
            .WithClientOptions(a =>
             a.WithTcpServer("localhost", 9000))
            .DiscoverTopics(TimeSpan.FromMinutes(5))
            .Subscribe(r =>
            {
                Console.Clear();
                foreach (var (topic, lastSeen) in r)
                {
                    Console.WriteLine($"{topic} Last Seen: {lastSeen}");
                }
            }));
        WaitForExit();
    }

    /// <summary>
    /// Starts a simulated multi-value temperature controller on COM2.
    /// The controller responds to commands and periodically sends temperature readings.
    /// Protocol: Messages are framed with STX (0x02) at start and ETX (0x03) at end.
    /// Commands: "GET_TEMP", "GET_SETPOINT", "SET_SETPOINT:xx.x", "GET_STATUS".
    /// </summary>
    private static void StartTemperatureControllerSimulator()
    {
        Console.Clear();
        Console.WriteLine("=== Temperature Controller Simulator (COM2) ===");
        Console.WriteLine("Protocol: STX (0x02) + Message + ETX (0x03)");
        Console.WriteLine("Commands: GET_TEMP, GET_SETPOINT, SET_SETPOINT:xx.x, GET_STATUS");
        Console.WriteLine();

        var random = new Random();
        var currentTemp = 22.5;
        var setpoint = 25.0;
        var heatingOn = false;

        try
        {
            var serialPort = new SerialPortRx
            {
                PortName = "COM2",
                BaudRate = 9600
            };
            serialPort.Open();
            _disposables.Add(serialPort);

            Console.WriteLine($"Opened {serialPort.PortName} at {serialPort.BaudRate} baud");
            Console.WriteLine("Simulator running - sending temperature updates every 2 seconds...");
            Console.WriteLine();

            // Buffer for receiving commands (STX to ETX)
            var buffer = new StringBuilder();
            var inMessage = false;

            // Handle incoming commands - DataReceived emits one char at a time
            _disposables.Add(serialPort.DataReceived
                .Subscribe(c =>
                {
                    // STX - Start of message
                    if (c == 0x02)
                    {
                        buffer.Clear();
                        inMessage = true;
                    }
                    else if (c == 0x03 && inMessage)
                    {
                        // ETX - End of message
                        inMessage = false;
                        var command = buffer.ToString().Trim().ToUpperInvariant();
                        var response = ProcessCommand(command, ref currentTemp, ref setpoint, ref heatingOn);
                        var framedResponse = $"\x02{response}\x03";
                        serialPort.Write(framedResponse);
                        Console.WriteLine($"[RX] {command} -> [TX] {response}");
                    }
                    else if (inMessage)
                    {
                        buffer.Append(c);
                    }
                }));

            // Simulate temperature changes and send periodic updates
            _disposables.Add(Observable.Interval(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    // Simulate temperature changes based on heating state
                    if (heatingOn && currentTemp < setpoint)
                    {
                        currentTemp += random.NextDouble() * 0.5;
                    }
                    else if (!heatingOn && currentTemp > 20.0)
                    {
                        currentTemp -= random.NextDouble() * 0.3;
                    }

                    // Auto control heating
                    heatingOn = currentTemp < setpoint - 0.5;

                    // Add some noise
                    currentTemp += (random.NextDouble() - 0.5) * 0.1;

                    // Send periodic temperature update
                    var update = $"TEMP_UPDATE:{currentTemp:F1}";
                    var framedUpdate = $"\x02{update}\x03";
                    serialPort.Write(framedUpdate);
                    Console.WriteLine($"[AUTO] {update} | Setpoint: {setpoint:F1} | Heating: {(heatingOn ? "ON" : "OFF")}");
                }));

            WaitForExit("Temperature Controller Simulator running on COM2...", clear: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure COM2 is available (com0com virtual port pair)");
            WaitForExit(clear: false);
        }
    }

    private static string ProcessCommand(string command, ref double currentTemp, ref double setpoint, ref bool heatingOn)
    {
        if (command == "GET_TEMP")
        {
            return $"TEMP:{currentTemp:F1}";
        }

        if (command == "GET_SETPOINT")
        {
            return $"SETPOINT:{setpoint:F1}";
        }

        if (command.StartsWith("SET_SETPOINT:"))
        {
            var valueStr = command["SET_SETPOINT:".Length..];
            if (double.TryParse(valueStr, out var newSetpoint) && newSetpoint >= 10 && newSetpoint <= 40)
            {
                setpoint = newSetpoint;
                return $"OK:SETPOINT={setpoint:F1}";
            }

            return "ERROR:INVALID_SETPOINT";
        }

        if (command == "GET_STATUS")
        {
            return $"STATUS:TEMP={currentTemp:F1},SETPOINT={setpoint:F1},HEATING={(heatingOn ? "ON" : "OFF")}";
        }

        return "ERROR:UNKNOWN_COMMAND";
    }

    /// <summary>
    /// Reads data from the serial port (COM1) and publishes it to MQTT.
    /// Uses STX/ETX framing to detect complete messages.
    /// </summary>
    private static void SerialToMqttBridge()
    {
        Console.Clear();
        Console.WriteLine("=== Serial to MQTT Bridge (COM1 -> MQTT) ===");
        Console.WriteLine("Reading from COM1 and publishing to MQTT topic 'serial/temperature/#'");
        Console.WriteLine();

        try
        {
            var serialPort = new SerialPortRx
            {
                PortName = "COM1",
                BaudRate = 9600
            };
            serialPort.Open();
            _disposables.Add(serialPort);

            Console.WriteLine($"Opened {serialPort.PortName} at {serialPort.BaudRate} baud");

            // Publish serial data to MQTT using STX (0x02) / ETX (0x03) framing
            _disposables.Add(Create.MqttClient()
                .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
                .PublishSerialPort(
                    topic: "serial/temperature/readings",
                    serialPort: serialPort,
                    startsWith: Observable.Return('\x02'),
                    endsWith: Observable.Return('\x03'),
                    timeOut: 5000)
                .Subscribe(
                    result => Console.WriteLine($"[MQTT TX] Published: {result.ReasonCode}"),
                    error => Console.WriteLine($"[ERROR] {error.Message}")));

            // Also display the raw data received - DataReceived emits one char at a time
            var buffer = new StringBuilder();
            var inMessage = false;
            _disposables.Add(serialPort.DataReceived
                .Subscribe(c =>
                {
                    if (c == 0x02)
                    {
                        buffer.Clear();
                        inMessage = true;
                    }
                    else if (c == 0x03 && inMessage)
                    {
                        inMessage = false;
                        Console.WriteLine($"[SERIAL RX] {buffer}");
                    }
                    else if (inMessage)
                    {
                        buffer.Append(c);
                    }
                }));

            WaitForExit("Serial to MQTT Bridge running (COM1 -> MQTT)...", clear: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure COM1 is available and MQTT broker is running on localhost:9000");
            WaitForExit(clear: false);
        }
    }

    /// <summary>
    /// Subscribes to MQTT topics and writes commands to the serial port (COM1).
    /// Allows sending commands to the temperature controller through MQTT.
    /// </summary>
    private static void MqttToSerialBridge()
    {
        Console.Clear();
        Console.WriteLine("=== MQTT to Serial Bridge (MQTT -> COM1) ===");
        Console.WriteLine("Subscribe to 'serial/temperature/commands' to send commands to COM1");
        Console.WriteLine();
        Console.WriteLine("Example MQTT commands to publish:");
        Console.WriteLine("  Topic: serial/temperature/commands");
        Console.WriteLine("  Payloads: GET_TEMP, GET_SETPOINT, SET_SETPOINT:28.0, GET_STATUS");
        Console.WriteLine();

        try
        {
            var serialPort = new SerialPortRx
            {
                PortName = "COM1",
                BaudRate = 9600
            };
            serialPort.Open();
            _disposables.Add(serialPort);

            Console.WriteLine($"Opened {serialPort.PortName} at {serialPort.BaudRate} baud");

            // Subscribe to MQTT and write to serial port
            _disposables.Add(Create.MqttClient()
                .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
                .SubscribeToTopic("serial/temperature/commands")
                .Subscribe(
                    message =>
                    {
                        var payload = message.ApplicationMessage.ConvertPayloadToString();

                        // Frame the command with STX/ETX
                        var framedCommand = $"\x02{payload}\x03";
                        serialPort.Write(framedCommand);
                        Console.WriteLine($"[MQTT RX -> SERIAL TX] {payload}");
                    },
                    error => Console.WriteLine($"[ERROR] {error.Message}")));

            // Display responses from the serial port - DataReceived emits one char at a time
            var buffer = new StringBuilder();
            var inMessage = false;
            _disposables.Add(serialPort.DataReceived
                .Subscribe(c =>
                {
                    if (c == 0x02)
                    {
                        buffer.Clear();
                        inMessage = true;
                    }
                    else if (c == 0x03 && inMessage)
                    {
                        inMessage = false;
                        Console.WriteLine($"[SERIAL RX] Response: {buffer}");
                    }
                    else if (inMessage)
                    {
                        buffer.Append(c);
                    }
                }));

            WaitForExit("MQTT to Serial Bridge running (MQTT -> COM1)...", clear: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure COM1 is available and MQTT broker is running on localhost:9000");
            WaitForExit(clear: false);
        }
    }

    /// <summary>
    /// Full demo that combines serial port communication with MQTT.
    /// Runs the temperature controller simulator in background and bridges serial to MQTT.
    /// Also provides interactive command sending capability.
    /// </summary>
    private static void SerialPortFullDemo()
    {
        Console.Clear();
        Console.WriteLine("=== Full Serial Port + MQTT Demo ===");
        Console.WriteLine("This demo requires:");
        Console.WriteLine("  1. com0com virtual COM port pair (COM1 <-> COM2)");
        Console.WriteLine("  2. MQTT broker running on localhost:9000");
        Console.WriteLine();
        Console.WriteLine("The demo will:");
        Console.WriteLine("  - Simulate a temperature controller on COM2");
        Console.WriteLine("  - Bridge COM1 to MQTT (bidirectional)");
        Console.WriteLine("  - Publish temperature readings to 'serial/temperature/readings'");
        Console.WriteLine("  - Accept commands from 'serial/temperature/commands'");
        Console.WriteLine();

        var random = new Random();
        var currentTemp = 22.5;
        var setpoint = 25.0;
        var heatingOn = false;

        try
        {
            // === COM2: Temperature Controller Simulator ===
            var com2 = new SerialPortRx
            {
                PortName = "COM2",
                BaudRate = 9600
            };
            com2.Open();
            _disposables.Add(com2);
            Console.WriteLine("[COM2] Temperature Controller Simulator started");

            var com2Buffer = new StringBuilder();
            var com2InMessage = false;

            // Handle commands on COM2 - DataReceived emits one char at a time
            _disposables.Add(com2.DataReceived
                .Subscribe(c =>
                {
                    if (c == 0x02)
                    {
                        com2Buffer.Clear();
                        com2InMessage = true;
                    }
                    else if (c == 0x03 && com2InMessage)
                    {
                        com2InMessage = false;
                        var command = com2Buffer.ToString().Trim().ToUpperInvariant();
                        var response = ProcessCommand(command, ref currentTemp, ref setpoint, ref heatingOn);
                        com2.Write($"\x02{response}\x03");
                    }
                    else if (com2InMessage)
                    {
                        com2Buffer.Append(c);
                    }
                }));

            // Simulate temperature changes on COM2
            _disposables.Add(Observable.Interval(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    if (heatingOn && currentTemp < setpoint)
                    {
                        currentTemp += random.NextDouble() * 0.5;
                    }
                    else if (!heatingOn && currentTemp > 20.0)
                    {
                        currentTemp -= random.NextDouble() * 0.3;
                    }

                    heatingOn = currentTemp < setpoint - 0.5;
                    currentTemp += (random.NextDouble() - 0.5) * 0.1;

                    com2.Write($"\x02TEMP_UPDATE:{currentTemp:F1}\x03");
                }));

            // === COM1: MQTT Bridge ===
            var com1 = new SerialPortRx
            {
                PortName = "COM1",
                BaudRate = 9600
            };
            com1.Open();
            _disposables.Add(com1);
            Console.WriteLine("[COM1] MQTT Bridge started");

            // Publish serial data to MQTT
            _disposables.Add(Create.MqttClient()
                .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
                .PublishSerialPort(
                    topic: "serial/temperature/readings",
                    serialPort: com1,
                    startsWith: Observable.Return('\x02'),
                    endsWith: Observable.Return('\x03'),
                    timeOut: 5000)
                .Subscribe(_ => { })); // Silent publish

            // Subscribe to MQTT commands and forward to serial
            _disposables.Add(Create.MqttClient()
                .WithClientOptions(a => a.WithTcpServer("localhost", 9000))
                .SubscribeToTopic("serial/temperature/commands")
                .Subscribe(message =>
                {
                    var payload = message.ApplicationMessage.ConvertPayloadToString();
                    com1.Write($"\x02{payload}\x03");
                    Console.WriteLine($"[CMD] Sent: {payload}");
                }));

            // Display received data - DataReceived emits one char at a time
            var com1Buffer = new StringBuilder();
            var com1InMessage = false;
            _disposables.Add(com1.DataReceived
                .Subscribe(c =>
                {
                    if (c == 0x02)
                    {
                        com1Buffer.Clear();
                        com1InMessage = true;
                    }
                    else if (c == 0x03 && com1InMessage)
                    {
                        com1InMessage = false;
                        var msg = com1Buffer.ToString();
                        if (msg.StartsWith("TEMP_UPDATE:"))
                        {
                            Console.WriteLine($"[TEMP] {msg} | Setpoint: {setpoint:F1} | Heating: {(heatingOn ? "ON" : "OFF")}");
                        }
                        else
                        {
                            Console.WriteLine($"[RESP] {msg}");
                        }
                    }
                    else if (com1InMessage)
                    {
                        com1Buffer.Append(c);
                    }
                }));

            Console.WriteLine();
            Console.WriteLine("Demo running! Temperature updates will appear below.");
            Console.WriteLine("To send commands, publish to MQTT topic 'serial/temperature/commands':");
            Console.WriteLine("  mosquitto_pub -t serial/temperature/commands -m \"GET_STATUS\"");
            Console.WriteLine("  mosquitto_pub -t serial/temperature/commands -m \"SET_SETPOINT:30.0\"");
            Console.WriteLine();

            // Interactive command input
            _disposables.Add(Observable.Create<string>(observer =>
                {
                    var cts = new CancellationTokenSource();
                    _ = Task.Run(
                        () =>
                        {
                            while (!cts.Token.IsCancellationRequested)
                            {
                                if (Console.KeyAvailable)
                                {
                                    var key = Console.ReadKey(true);
                                    if (key.Key == ConsoleKey.T)
                                    {
                                        observer.OnNext("GET_TEMP");
                                    }
                                    else if (key.Key == ConsoleKey.S)
                                    {
                                        observer.OnNext("GET_STATUS");
                                    }
                                    else if (key.Key == ConsoleKey.P)
                                    {
                                        observer.OnNext("GET_SETPOINT");
                                    }
                                    else if (key.Key == ConsoleKey.U)
                                    {
                                        observer.OnNext("SET_SETPOINT:30.0");
                                    }
                                    else if (key.Key == ConsoleKey.D)
                                    {
                                        observer.OnNext("SET_SETPOINT:20.0");
                                    }
                                }

                                Thread.Sleep(100);
                            }
                        },
                        cts.Token);
                    return () => cts.Cancel();
                })
                .Subscribe(cmd =>
                {
                    com1.Write($"\x02{cmd}\x03");
                    Console.WriteLine($"[KEY] Sent: {cmd}");
                }));

            Console.WriteLine("Keyboard shortcuts: [T]emp, [S]tatus, [P]oint, [U]p setpoint, [D]own setpoint");
            Console.WriteLine();

            WaitForExit(clear: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Troubleshooting:");
            Console.WriteLine("1. Install com0com and create a virtual COM port pair (COM1 <-> COM2)");
            Console.WriteLine("2. Make sure no other application is using the COM ports");
            Console.WriteLine("3. Ensure MQTT broker is running on localhost:9000");
            WaitForExit(clear: false);
        }
    }

    private static void StartMessages(string baseTopic = "") =>
        _disposables.Add(Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Subscribe(i => _message.OnNext(($"{baseTopic}FromMilliseconds", "{" + $"payload: {i}" + "}"))));

    private static void WaitForExit(string? message = null, bool clear = true)
    {
        if (clear)
        {
            Console.Clear();
        }

        if (message != null)
        {
            Console.WriteLine(message);
        }

        Console.WriteLine("Press 'Escape' or 'E' to exit.");
        Console.WriteLine();

        while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.E))
        {
            Thread.Sleep(1);
        }

        _disposables.Dispose();
        _disposables = [];
    }

    private static TObject DumpToConsole<TObject>(this TObject @object)
    {
        var output = "NULL";
        if (@object != null)
        {
            output = JsonSerializer.Serialize(@object, _jsonOptions);
        }

        Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
        return @object;
    }
}
