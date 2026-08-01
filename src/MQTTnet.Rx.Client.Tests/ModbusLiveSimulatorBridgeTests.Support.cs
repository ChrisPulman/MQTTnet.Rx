// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using IoT.Driver.ModbusRx.Data;
using IoT.Driver.ModbusRx.Device;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Advanced;
using ClientCreate = MQTTnet.Rx.Client.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Provides the live Modbus bridge test support types.</summary>
public sealed partial class ModbusLiveSimulatorBridgeTests
{
    /// <summary>Owns one filtered simulator data-store write notification.</summary>
    private sealed class DataStoreWriteSignal : IDisposable
    {
        /// <summary>The simulator data store that owns the event.</summary>
        private readonly DataStore _dataStore;

        /// <summary>The handler detached during disposal.</summary>
        private readonly EventHandler<DataStoreEventArgs> _handler;

        /// <summary>Initializes a new instance of the <see cref="DataStoreWriteSignal"/> class.</summary>
        /// <param name="dataStore">The simulator data store.</param>
        /// <param name="dataType">The expected Modbus data area.</param>
        /// <param name="address">The expected zero-based address.</param>
        internal DataStoreWriteSignal(DataStore dataStore, ModbusDataType dataType, ushort address)
        {
            _dataStore = dataStore;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Completion = completion.Task;
            _handler = (_, args) =>
            {
                if (args.ModbusDataType != dataType || args.StartAddress != address)
                {
                    return;
                }

                _ = completion.TrySetResult();
            };
            dataStore.DataStoreWrittenTo += _handler;
        }

        /// <summary>Gets the task completed by the matching data-store write.</summary>
        internal Task Completion { get; }

        /// <inheritdoc/>
        public void Dispose() => _dataStore.DataStoreWrittenTo -= _handler;
    }

    /// <summary>Records tasks started by synchronous convenience-writer callbacks.</summary>
    private sealed class ModbusWriteRecorder
    {
        /// <summary>Signals the register write started by a forwarded MQTT command.</summary>
        private readonly TaskCompletionSource<Task> _registerWrite =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Signals the coil write started by a forwarded MQTT command.</summary>
        private readonly TaskCompletionSource<Task> _coilWrite =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets the forwarded coil-write task.</summary>
        internal Task<Task> CoilWrite => _coilWrite.Task;

        /// <summary>Gets the forwarded register-write task.</summary>
        internal Task<Task> RegisterWrite => _registerWrite.Task;

        /// <summary>Starts a single-register protocol write.</summary>
        /// <param name="master">The current simulator master.</param>
        /// <param name="address">The forwarded register address.</param>
        /// <param name="value">The parsed register value.</param>
        internal void WriteRegister(ModbusIpMaster master, ushort address, ushort value) =>
            _ = _registerWrite.TrySetResult(master.WriteSingleRegisterAsync(UnitId, address, value));

        /// <summary>Starts a multiple-register protocol write.</summary>
        /// <param name="master">The current simulator master.</param>
        /// <param name="address">The forwarded starting address.</param>
        /// <param name="values">The parsed register values.</param>
        internal void WriteRegisters(ModbusIpMaster master, ushort address, ushort[] values) =>
            _ = _registerWrite.TrySetResult(master.WriteMultipleRegistersAsync(UnitId, address, values));

        /// <summary>Starts a multiple-coil protocol write.</summary>
        /// <param name="master">The current simulator master.</param>
        /// <param name="address">The forwarded starting address.</param>
        /// <param name="values">The parsed coil values.</param>
        internal void WriteCoils(ModbusIpMaster master, ushort address, bool[] values) =>
            _ = _coilWrite.TrySetResult(master.WriteMultipleCoilsAsync(UnitId, address, values));
    }

    /// <summary>Owns a real resilient MQTT client connected to the test broker.</summary>
    private sealed class LiveResilientSource : IAsyncDisposable
    {
        /// <summary>The interval used while awaiting a resilient connection.</summary>
        private const int ConnectionPollingMilliseconds = 10;

        /// <summary>The subscription that owns the resilient factory sequence.</summary>
        private readonly IDisposable _owner;

        /// <summary>Initializes a new instance of the <see cref="LiveResilientSource"/> class.</summary>
        /// <param name="source">The observable resilient client source.</param>
        /// <param name="client">The connected resilient client.</param>
        /// <param name="owner">The subscription owning the client factory.</param>
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

        /// <summary>Gets the resilient client sequence consumed by bridge methods.</summary>
        public IObservable<IResilientMqttClient> Source { get; }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await Client.StopAsync();
            _owner.Dispose();
        }

        /// <summary>Starts a real resilient client against the supplied broker.</summary>
        /// <param name="broker">The running live MQTT broker.</param>
        /// <returns>The connected and owned resilient source.</returns>
        internal static async Task<LiveResilientSource> StartAsync(LiveMqttBroker broker)
        {
            var source = ClientCreate.ResilientMqttClient();
            IResilientMqttClient? client = null;
            var owner = source.Subscribe(Witness.Create<IResilientMqttClient>(value => client = value));
            var options = new ResilientMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.Zero)
                .WithClientOptions(builder => builder
                    .WithClientId($"modbus-resilient-{Guid.NewGuid():N}")
                    .WithTcpServer(IPAddress.Loopback.ToString(), broker.Port))
                .Build();
            options.ConnectionCheckInterval = TimeSpan.FromMilliseconds(ConnectionPollingMilliseconds);
            await client!.StartAsync(options);
            await WaitUntilAsync(() => client.IsConnected);
            return new(source, client, owner);
        }

        /// <summary>Waits for a bounded condition without arbitrary sleeps.</summary>
        /// <param name="condition">The condition that completes the wait.</param>
        /// <returns>A task representing the bounded wait.</returns>
        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            using var cancellation = new CancellationTokenSource(Timeout);
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(ConnectionPollingMilliseconds));
            while (!condition())
            {
                _ = await timer.WaitForNextTickAsync(cancellation.Token);
            }
        }
    }
}
