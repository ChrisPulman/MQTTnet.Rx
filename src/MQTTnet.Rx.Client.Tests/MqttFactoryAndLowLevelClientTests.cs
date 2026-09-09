// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Server;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using ClientCreate = MQTTnet.Rx.Client.Reactive.Create;
using ServerCreate = MQTTnet.Rx.Server.Reactive.Create;
using ServerSession = MQTTnet.Rx.Server.Reactive.MqttServerSession;
#else
using ClientCreate = MQTTnet.Rx.Client.Create;
using ServerCreate = MQTTnet.Rx.Server.Create;
using ServerSession = MQTTnet.Rx.Server.MqttServerSession;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies MQTTnet factory overloads and low-level client reactive operations.</summary>
[NotInParallel]
public sealed class MqttFactoryAndLowLevelClientTests
{
    /// <summary>The number of times paired classic and asynchronous operations are invoked.</summary>
    private const int PairedOperationCount = 2;

    /// <summary>The maximum wait for cold observable operations.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies every low-level MQTT client operation has paired reactive wrappers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task LowLevelMqttClientOperations_ExposeColdObservablePairsAsync()
    {
        using var client = new RecordingLowLevelMqttClient();
        var options = new MqttClientOptionsBuilder().WithClientId("low-level").WithTcpServer("localhost").Build();
        var packet = new MqttPingReqPacket();
        var inspected = new TaskCompletionSource<InspectMqttPacketEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<InspectMqttPacketEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await Assert.That(client.Properties().IsConnected).IsFalse();
        await Assert.That(await client.Property(static value => value.IsConnected).FirstAsync(Timeout)).IsFalse();
        await Assert.That(await client.ObserveProperty(static value => value.IsConnected).FirstAsync(Timeout)).IsFalse();
        await Assert.That(await client.PropertySnapshots().FirstAsync(Timeout)).IsEqualTo(new(false));
        await Assert.That(await client.ObservePropertySnapshots().FirstAsync(Timeout)).IsEqualTo(new(false));
        await Assert.That(await client.IsConnectedValue().FirstAsync(Timeout)).IsFalse();
        await Assert.That(await client.ObserveIsConnected().FirstAsync(Timeout)).IsFalse();

        using var inspectedSubscription = client.InspectPacket().Subscribe(value =>
        {
            _ = inspected.TrySetResult(value);
        });
        await using var observedSubscription = await client.ObserveInspectPacket().SubscribeAsync(
            (value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = observed.TrySetResult(value);
                return ValueTask.CompletedTask;
            });

        _ = await client.Connect(options).FirstAsync(Timeout);
        _ = await client.ObserveConnect(options).FirstAsync(Timeout);
        _ = await client.Send(packet).FirstAsync(Timeout);
        _ = await client.ObserveSend(packet).FirstAsync(Timeout);
        await Assert.That((await inspected.Task.WaitAsync(Timeout)).Direction)
            .IsEqualTo(MqttPacketFlowDirection.Outbound);
        await Assert.That((await observed.Task.WaitAsync(Timeout)).Direction)
            .IsEqualTo(MqttPacketFlowDirection.Outbound);
        await Assert.That(await client.Receive().FirstAsync(Timeout)).IsTypeOf<MqttPingRespPacket>();
        await Assert.That(await client.ObserveReceive().FirstAsync(Timeout)).IsTypeOf<MqttPingRespPacket>();
        _ = await client.Disconnect().FirstAsync(Timeout);
        _ = await client.ObserveDisconnect().FirstAsync(Timeout);

        await Assert.That(client.ConnectCount).IsEqualTo(PairedOperationCount);
        await Assert.That(client.DisconnectCount).IsEqualTo(PairedOperationCount);
        await Assert.That(client.ReceiveCount).IsEqualTo(PairedOperationCount);
        MqttPacket[] expectedSentPackets = [packet, packet];
        await Assert.That(client.SentPackets).IsEquivalentTo(expectedSentPackets);
    }

    /// <summary>Verifies client factory overloads emit shared MQTT and low-level client instances.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ClientFactoryOverloads_ExposeLoggerAndAdapterVariantsAsync()
    {
        var logger = MqttNetNullLogger.Instance;
        IMqttClientAdapterFactory adapterFactory = new MqttClientAdapterFactory();

        await Assert.That(await ClientCreate.MqttClient(logger).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.MqttClient(adapterFactory).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.MqttClient(logger, adapterFactory).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.MqttClientSignal(logger).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.MqttClientSignal(adapterFactory).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.MqttClientSignal(logger, adapterFactory).FirstAsync(Timeout)).IsNotNull();

        await Assert.That(await ClientCreate.LowLevelMqttClient().FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClient(logger).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClient(adapterFactory).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClient(logger, adapterFactory).FirstAsync(Timeout))
            .IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClientSignal().FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClientSignal(logger).FirstAsync(Timeout)).IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClientSignal(adapterFactory).FirstAsync(Timeout))
            .IsNotNull();
        await Assert.That(await ClientCreate.LowLevelMqttClientSignal(logger, adapterFactory).FirstAsync(Timeout))
            .IsNotNull();
    }

    /// <summary>Verifies server factory overloads expose MQTTnet logger and adapter variants.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServerFactoryOverloads_ExposeLoggerAdapterAndRetainedVariantsAsync()
    {
        var originalFactory = ServerCreate.MqttFactory;
        var logger = MqttNetNullLogger.Instance;
        IMqttServerAdapter[] adapters = [];
        var retainedDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(retainedDirectory);

        try
        {
            await Assert.That(static () => ServerCreate.NewMqttFactory(null!)).Throws<ArgumentNullException>();
            ServerCreate.NewMqttFactory(new());

            await UseServerAsync(ServerCreate.MqttServer(BuildNoEndpoint, logger));
            await UseServerAsync(ServerCreate.MqttServer(BuildNoEndpoint, adapters));
            await UseServerAsync(ServerCreate.MqttServer(BuildNoEndpoint, adapters, logger));
            await UseServerAsync(ServerCreate.MqttServerSignal(BuildNoEndpoint, logger));
            await UseServerAsync(ServerCreate.MqttServerSignal(BuildNoEndpoint, adapters));
            await UseServerAsync(ServerCreate.MqttServerSignal(BuildNoEndpoint, adapters, logger));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(BuildNoEndpoint, logger));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(
                BuildNoEndpoint,
                logger,
                retainedDirectory));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(BuildNoEndpoint, adapters));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(
                BuildNoEndpoint,
                adapters,
                retainedDirectory));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(BuildNoEndpoint, adapters, logger));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessages(
                BuildNoEndpoint,
                adapters,
                logger,
                retainedDirectory));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(BuildNoEndpoint, logger));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(
                BuildNoEndpoint,
                logger,
                retainedDirectory));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(BuildNoEndpoint, adapters));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(
                BuildNoEndpoint,
                adapters,
                retainedDirectory));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(BuildNoEndpoint, adapters, logger));
            await UseServerAsync(ServerCreate.MqttServerWithRetainedMessagesSignal(
                BuildNoEndpoint,
                adapters,
                logger,
                retainedDirectory));
        }
        finally
        {
            ServerCreate.NewMqttFactory(originalFactory);
            Directory.Delete(retainedDirectory, true);
        }
    }

    /// <summary>Builds no-endpoint server options.</summary>
    /// <param name="builder">The MQTT server options builder.</param>
    /// <returns>The configured server options.</returns>
    private static MqttServerOptions BuildNoEndpoint(MqttServerOptionsBuilder builder) =>
        builder.WithoutDefaultEndpoint().WithoutEncryptedEndpoint().Build();

    /// <summary>Starts and disposes a server emitted by a classic observable.</summary>
    /// <param name="source">The server source.</param>
    /// <returns>A task that represents the asynchronous test helper.</returns>
    private static async Task UseServerAsync(
        IObservable<(MqttServer Server, ServerSession Disposable)> source)
    {
        var received = new TaskCompletionSource<(MqttServer Server, ServerSession Disposable)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = source.Subscribe(
            value => _ = received.TrySetResult(value),
            error => _ = received.TrySetException(error));
        var value = await received.Task.WaitAsync(Timeout);
        await Assert.That(value.Server.IsStarted).IsTrue();
        await value.Disposable.DisposeAsync();
        await Assert.That(value.Server.IsStarted).IsFalse();
    }

    /// <summary>Starts and disposes a server emitted by an asynchronous observable.</summary>
    /// <param name="source">The server source.</param>
    /// <returns>A task that represents the asynchronous test helper.</returns>
    private static async Task UseServerAsync(
        IObservableAsync<(MqttServer Server, ServerSession Disposable)> source)
    {
        var received = new TaskCompletionSource<(MqttServer Server, ServerSession Disposable)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var subscription = await source.SubscribeAsync(
            (value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = received.TrySetResult(value);
                return ValueTask.CompletedTask;
            });
        var value = await received.Task.WaitAsync(Timeout);
        await Assert.That(value.Server.IsStarted).IsTrue();
        await value.Disposable.DisposeAsync();
        await Assert.That(value.Server.IsStarted).IsFalse();
    }

    /// <summary>Records low-level MQTT client calls.</summary>
    private sealed class RecordingLowLevelMqttClient : ILowLevelMqttClient
    {
        /// <summary>Stores packet inspection subscribers.</summary>
        private Func<InspectMqttPacketEventArgs, Task>? _inspectPacketAsync;

        /// <inheritdoc/>
        public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync
        {
            add => _inspectPacketAsync += value;
            remove => _inspectPacketAsync -= value;
        }

        /// <summary>Gets the number of connect operations.</summary>
        public int ConnectCount { get; private set; }

        /// <summary>Gets the number of disconnect operations.</summary>
        public int DisconnectCount { get; private set; }

        /// <summary>Gets the number of receive operations.</summary>
        public int ReceiveCount { get; private set; }

        /// <summary>Gets the sent MQTT packets.</summary>
        public List<MqttPacket> SentPackets { get; } = [];

        /// <summary>Gets a value indicating whether the client is connected.</summary>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(options);
            cancellationToken.ThrowIfCancellationRequested();
            ConnectCount++;
            IsConnected = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DisconnectCount++;
            IsConnected = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task<MqttPacket> ReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceiveCount++;
            return Task.FromResult<MqttPacket>(new MqttPingRespPacket());
        }

        /// <inheritdoc/>
        public Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SentPackets.Add(packet);
            return _inspectPacketAsync?.Invoke(new(MqttPacketFlowDirection.Outbound, [])) ?? Task.CompletedTask;
        }
    }
}
