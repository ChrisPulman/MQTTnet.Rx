// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using IoT.Driver.Core;
using IoT.Driver.S7PlcRx;
using IoT.Driver.S7PlcRx.Enums;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Advanced;
using ReactiveUI.Primitives.Reactive.Signals;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains the in-memory PLC and resilient MQTT infrastructure for S7 bridge tests.</summary>
public sealed partial class S7PlcLiveBridgeTests
{
    /// <summary>Provides the narrow in-memory S7 seam needed by MQTT bridge tests.</summary>
    private sealed class RecordingS7 : IRxS7
    {
        /// <summary>Stores observed values keyed by logical name and CLR type.</summary>
        private readonly Dictionary<(string Name, Type Type), object?> _observedValues = [];

        /// <summary>Stores PLC writes in arrival order.</summary>
        private readonly List<(string? Variable, object? Value)> _writes = [];

        /// <summary>Signals that another PLC write arrived.</summary>
        private TaskCompletionSource _writeArrived = NewWriteSignal();

        /// <inheritdoc/>
        public string IP => IPAddress.Loopback.ToString();

        /// <inheritdoc/>
        public IObservable<bool> IsConnected => Signal.Emit(true);

        /// <inheritdoc/>
        public bool IsConnectedValue => true;

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public IObservable<string> LastError => Signal.None<string>();

        /// <inheritdoc/>
        public IObservable<ErrorCode> LastErrorCode => Signal.None<ErrorCode>();

        /// <inheritdoc/>
        public IObservable<Tag?> ObserveAll => Signal.None<Tag?>();

        /// <inheritdoc/>
        public CpuType PLCType => CpuType.S71500;

        /// <inheritdoc/>
        public short Rack => 0;

        /// <inheritdoc/>
        public short Slot => 1;

        /// <inheritdoc/>
        public IObservable<bool> IsPaused => Signal.Emit(false);

        /// <inheritdoc/>
        public IObservable<string> Status => Signal.None<string>();

        /// <inheritdoc/>
        public Tags TagList { get; } = [];

        /// <inheritdoc/>
        public bool ShowWatchDogWriting { get; set; }

        /// <inheritdoc/>
        public string? WatchDogAddress => null;

        /// <inheritdoc/>
        public ushort WatchDogValueToWrite { get; set; }

        /// <inheritdoc/>
        public int WatchDogWritingTime => 0;

        /// <inheritdoc/>
        public IObservable<long> ReadTime => Signal.None<long>();

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;

        /// <inheritdoc/>
        public IObservable<T?> Observe<T>(LogicalTagKey<T> tag) =>
            _observedValues.TryGetValue((tag.Name, typeof(T)), out var value) && value is T typed
                ? Signal.Emit<T?>(typed)
                : Signal.None<T?>();

        /// <inheritdoc/>
        public Task<T?> ReadAsync<T>(LogicalTagKey<T> tag) => Task.FromResult(default(T));

        /// <inheritdoc/>
        public Task<T?> ReadAsync<T>(LogicalTagKey<T> tag, CancellationToken cancellationToken) =>
            Task.FromResult(default(T));

        /// <inheritdoc/>
        public void Value<T>(string? variable, T? value)
        {
            TaskCompletionSource writeArrived;
            lock (_writes)
            {
                _writes.Add((variable, value));
                writeArrived = _writeArrived;
                _writeArrived = NewWriteSignal();
            }

            _ = writeArrived.TrySetResult();
        }

        /// <inheritdoc/>
        public IObservable<string[]> GetCpuInfo() => Signal.Emit<string[]>([]);

        /// <summary>Sets the value returned by the typed observation seam.</summary>
        /// <typeparam name="T">The observed PLC value type.</typeparam>
        /// <param name="tag">The typed logical tag.</param>
        /// <param name="value">The value returned to observers.</param>
        internal void SetObserved<T>(LogicalTagKey<T> tag, T value) =>
            _observedValues[(tag.Name, typeof(T))] = value;

        /// <summary>Returns the requested write once it arrives.</summary>
        /// <param name="index">The zero-based write index.</param>
        /// <returns>A task containing the recorded variable and value.</returns>
        internal async Task<(string? Variable, object? Value)> WaitForWriteAsync(int index)
        {
            while (true)
            {
                Task waitTask;
                lock (_writes)
                {
                    if (_writes.Count > index)
                    {
                        return _writes[index];
                    }

                    waitTask = _writeArrived.Task;
                }

                await waitTask.ConfigureAwait(false);
            }
        }

        /// <summary>Creates an asynchronously continued write notification.</summary>
        /// <returns>A new write notification source.</returns>
        private static TaskCompletionSource NewWriteSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Owns a real resilient MQTT client connected to the live broker.</summary>
    private sealed class LiveResilientSource : IAsyncDisposable
    {
        /// <summary>The interval used while awaiting a resilient connection.</summary>
        private const int PollingIntervalMilliseconds = 10;

        /// <summary>The source subscription that keeps the resilient client alive.</summary>
        private readonly IDisposable _owner;

        /// <summary>Initializes a new instance of the <see cref="LiveResilientSource"/> class.</summary>
        /// <param name="source">The observable resilient-client source.</param>
        /// <param name="client">The real resilient client.</param>
        /// <param name="owner">The subscription keeping the client alive.</param>
        private LiveResilientSource(
            IObservable<IResilientMqttClient> source,
            IResilientMqttClient client,
            IDisposable owner)
        {
            Source = source;
            Client = client;
            _owner = owner;
        }

        /// <summary>Gets the connected resilient client.</summary>
        public IResilientMqttClient Client { get; }

        /// <summary>Gets the observable source consumed by synchronous bridges.</summary>
        public IObservable<IResilientMqttClient> Source { get; }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await Client.StopAsync();
            _owner.Dispose();
        }

        /// <summary>Creates and connects a resilient MQTT source to the live broker.</summary>
        /// <param name="broker">The live MQTT broker fixture.</param>
        /// <returns>A connected, owned resilient source.</returns>
        internal static async Task<LiveResilientSource> StartAsync(LiveMqttBroker broker)
        {
            var source = MQTTnet.Rx.Client.Create.ResilientMqttClient();
            IResilientMqttClient? client = null;
            var owner = source.Subscribe(Witness.Create<IResilientMqttClient>(value => client = value));
            var options = new ResilientMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.Zero)
                .WithClientOptions(builder => builder
                    .WithClientId($"s7-resilient-{Guid.NewGuid():N}")
                    .WithTcpServer(IPAddress.Loopback.ToString(), broker.Port))
                .Build();
            options.ConnectionCheckInterval = TimeSpan.FromMilliseconds(PollingIntervalMilliseconds);
            var startedClient = client ?? throw new InvalidOperationException(
                "The resilient MQTT source did not produce a client.");
            await startedClient.StartAsync(options);
            await WaitUntilAsync(() => startedClient.IsConnected);
            return new(source, startedClient, owner);
        }

        /// <summary>Registers an exact-topic observer before a bridge starts its broker subscription.</summary>
        /// <param name="topic">The exact topic whose granted SUBACK completes readiness.</param>
        /// <param name="readiness">A task completed by the matching subscription result.</param>
        /// <returns>The handler registration.</returns>
        internal IDisposable RegisterSubscriptionReadiness(string topic, out Task readiness)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            readiness = completion.Task;
            return Client.RegisterSubscriptionsChangedHandler((eventArgs, cancellationToken) =>
            {
                foreach (var result in eventArgs.SubscribeResult)
                {
                    foreach (var item in result.Items)
                    {
                        if (!string.Equals(item.TopicFilter.Topic, topic, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        _ = item.ResultCode is MqttClientSubscribeResultCode.GrantedQoS0
                            or MqttClientSubscribeResultCode.GrantedQoS1
                            or MqttClientSubscribeResultCode.GrantedQoS2
                            ? completion.TrySetResult()
                            : completion.TrySetException(
                                new InvalidOperationException(
                                    $"The live broker rejected the S7 bridge subscription to '{topic}'."));
                    }
                }

                return ValueTask.CompletedTask;
            });
        }

        /// <summary>Waits for a condition using a bounded periodic timer.</summary>
        /// <param name="condition">The condition that indicates completion.</param>
        /// <returns>A task that completes when the condition becomes true.</returns>
        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            using var cancellation = new CancellationTokenSource(Timeout);
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingIntervalMilliseconds));
            while (!condition())
            {
                _ = await timer.WaitForNextTickAsync(cancellation.Token);
            }
        }
    }
}
