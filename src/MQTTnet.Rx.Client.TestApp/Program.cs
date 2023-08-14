// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using ConsoleTools;

namespace MQTTnet.Rx.Client.TestApp
{
    internal static class Program
    {
        private static readonly Subject<(string topic, string payload)> _message = new();
        private static CompositeDisposable _disposables = new();

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            var publishMenu = new ConsoleMenu(args, level: 1)
                .Add("Publish Client", PublishClient)
                .Add("Publish Managed", PublishManagedClient)
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
                .Add("Subscribe Managed Client", SubscribeManagedClient)
                .Add("Discover Managed Client", DiscoverTopicsManagedClient)
                .Add("Close", ConsoleMenu.Close)
                .Configure(config =>
                {
                    config.Selector = "--> ";
                    config.EnableFilter = true;
                    config.Title = "Subscribe Submenu";
                    config.EnableBreadcrumb = true;
                    config.WriteBreadcrumbAction = titles => Console.WriteLine(string.Join(" / ", titles));
                });
            new ConsoleMenu(args, level: 0)
               .Add("Publish", publishMenu.Show)
               .Add("Subscribe", subscribeMenu.Show)
               .Add("Close", ConsoleMenu.Close)
               .Configure(config =>
               {
                   config.Title = "MQTTnet.Rx.Client Example";
                   config.EnableWriteTitle = true;
                   config.WriteHeaderAction = () => Console.WriteLine("Please select a mode:");
               })
               .Show();
        }

        private static void PublishManagedClient()
        {
            _disposables.Add(Create.ManagedMqttClient()
             .WithManagedClientOptions(a =>
                 a.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                     .WithClientOptions(c =>
                         c.WithTcpServer("localhost", 9000)))
             .PublishMessage(_message)
             .Subscribe(r => Console.WriteLine($"{r.ApplicationMessage.Id} [{r.ApplicationMessage.ApplicationMessage.Topic}] value : {r.ApplicationMessage.ApplicationMessage.ConvertPayloadToString()}")));
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

        private static void SubscribeManagedClient()
        {
            _disposables.Add(Create.ManagedMqttClient()
                .WithManagedClientOptions(a =>
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
            _disposables.Add(Create.ManagedMqttClient()
                .WithManagedClientOptions(a =>
                 a.WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                     .WithClientOptions(c =>
                         c.WithTcpServer("localhost", 9000)))
                .DiscoverTopics(TimeSpan.FromMinutes(5))
                .Subscribe(r =>
                {
                    Console.Clear();
                    foreach (var topic in r)
                    {
                        Console.WriteLine($"{topic.Topic} Last Seen: {topic.LastSeen}");
                    }
                }));
            WaitForExit();
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
            _disposables = new();
        }

        private static TObject DumpToConsole<TObject>(this TObject @object)
        {
            var output = "NULL";
            if (@object != null)
            {
                output = JsonSerializer.Serialize(@object, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
            return @object;
        }
    }
}
