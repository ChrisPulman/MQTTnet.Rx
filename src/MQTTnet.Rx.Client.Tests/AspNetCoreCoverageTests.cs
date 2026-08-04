// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using MQTTnet.AspNetCore;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Packets;
#if REACTIVE_SHIM
using MQTTnet.Rx.AspNetCore.Reactive;
#else
using MQTTnet.Rx.AspNetCore;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies the ASP.NET Core fluent and reactive package surface.</summary>
[NotInParallel]
public sealed class AspNetCoreCoverageTests
{
    /// <summary>The initial MQTT packet writer buffer size.</summary>
    private const int InitialBufferSize = 4_096;

    /// <summary>The maximum MQTT packet writer buffer size.</summary>
    private const int MaximumBufferSize = 65_535;

    /// <summary>The maximum time allowed for observable operations.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>A serialized MQTT ping request.</summary>
    private static readonly byte[] PingRequestBytes = [0xC0, 0x00];

    /// <summary>A serialized MQTT ping response.</summary>
    private static readonly byte[] PingResponseBytes = [0xD0, 0x00];

    /// <summary>Verifies the hosted-server registration overloads preserve services and options.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServiceCollectionExtensions_RegisterHostedServerOverloadsFluentlyAsync()
    {
        var defaultServices = CreateServices();
        var defaultOptions = new MqttServerFactory().CreateServerOptionsBuilder().Build();
        _ = defaultServices.AddSingleton(defaultOptions);
        await Assert.That(defaultServices.WithHostedMqttServer()).IsSameReferenceAs(defaultServices);
        await using var defaultProvider = defaultServices.BuildServiceProvider();
        await Assert.That(defaultProvider.GetRequiredService<MqttHostedServer>()).IsNotNull();

        var factory = new MqttServerFactory();
        var explicitOptions = factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build();
        var optionsServices = CreateServices();
        await Assert.That(optionsServices.WithHostedMqttServer(explicitOptions)).IsSameReferenceAs(optionsServices);
        await using var optionsProvider = optionsServices.BuildServiceProvider();
        await Assert.That(optionsProvider.GetRequiredService<MqttServerOptions>()).IsSameReferenceAs(explicitOptions);

        var callbackInvoked = false;
        var callbackServices = CreateServices();
        await Assert.That(callbackServices.WithHostedMqttServer(
            builder =>
            {
                callbackInvoked = true;
                _ = builder.WithoutDefaultEndpoint();
            })).IsSameReferenceAs(callbackServices);
        await using var callbackProvider = callbackServices.BuildServiceProvider();
        _ = callbackProvider.GetRequiredService<MqttServerOptions>();
        await Assert.That(callbackInvoked).IsTrue();
    }

    /// <summary>Verifies service-aware hosted-server registration exposes application services.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServiceCollectionExtensions_RegisterServiceAwareHostedServerFluentlyAsync()
    {
        var marker = new object();
        var callbackInvoked = false;
        var services = CreateServices();
        _ = services.AddSingleton(marker);
        await Assert.That(services.WithHostedMqttServerServices(
            builder =>
            {
                callbackInvoked = ReferenceEquals(builder.ServiceProvider.GetRequiredService<object>(), marker);
                _ = builder.WithoutDefaultEndpoint();
            })).IsSameReferenceAs(services);

        await using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<MqttServerOptions>();
        await Assert.That(callbackInvoked).IsTrue();
    }

    /// <summary>Verifies adapters, connections, logging, and composite server registrations.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServiceCollectionExtensions_RegisterAdaptersAndCompositeServerFluentlyAsync()
    {
        var connectionServices = CreateServices();
        await Assert.That(connectionServices.WithMqttConnections()).IsSameReferenceAs(connectionServices);
        await Assert.That(connectionServices.WithMqttConnectionHandler()).IsSameReferenceAs(connectionServices);
        await using var connectionProvider = connectionServices.BuildServiceProvider();
        await Assert.That(connectionProvider.GetRequiredService<MqttConnectionHandler>()).IsNotNull();
        await Assert.That(GetOnlyAdapter(connectionProvider)).IsTypeOf<MqttConnectionHandler>();

        var loggerServices = CreateServices();
        await Assert.That(loggerServices.WithMqttLogger(MqttNetNullLogger.Instance)).IsSameReferenceAs(loggerServices);
        await using var loggerProvider = loggerServices.BuildServiceProvider();
        await Assert.That(loggerProvider.GetRequiredService<IMqttNetLogger>())
            .IsSameReferenceAs(MqttNetNullLogger.Instance);

        var serverServices = CreateServices();
        await Assert.That(serverServices.WithMqttServer()).IsSameReferenceAs(serverServices);
        await using var serverProvider = serverServices.BuildServiceProvider();
        await Assert.That(serverProvider.GetRequiredService<MqttConnectionHandler>()).IsNotNull();
    }

    /// <summary>Verifies configured, TCP, and WebSocket adapter registrations.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ServiceCollectionExtensions_RegisterConfiguredAndTransportAdaptersFluentlyAsync()
    {
        var callbackInvoked = false;
        var serverServices = CreateServices();
        await Assert.That(serverServices.WithMqttServer(
            builder =>
            {
                callbackInvoked = true;
                _ = builder.WithoutDefaultEndpoint();
            })).IsSameReferenceAs(serverServices);
        await using var serverProvider = serverServices.BuildServiceProvider();
        _ = serverProvider.GetRequiredService<MqttServerOptions>();
        await Assert.That(callbackInvoked).IsTrue();

        var tcpServices = CreateServices();
        await Assert.That(tcpServices.WithMqttTcpServerAdapter()).IsSameReferenceAs(tcpServices);
        await using var tcpProvider = tcpServices.BuildServiceProvider();
        await Assert.That(GetOnlyAdapter(tcpProvider)).IsTypeOf<MqttTcpServerAdapter>();

        var webSocketServices = CreateServices();
        await Assert.That(webSocketServices.WithMqttWebSocketServerAdapter()).IsSameReferenceAs(webSocketServices);
        await using var webSocketProvider = webSocketServices.BuildServiceProvider();
        await Assert.That(webSocketProvider.GetRequiredService<MqttWebSocketServerAdapter>()).IsNotNull();
    }

    /// <summary>Verifies endpoint, connection, and application hosting helpers preserve their receivers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task HostingExtensions_PreserveConfiguredBuildersAsync()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRouting();
        _ = services.AddConnections();
        _ = services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();
        _ = services.AddSingleton<MqttConnectionHandler>();
        await using var provider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(provider);
        var endpoints = new TestEndpointRouteBuilder(app);
        await Assert.That(endpoints.MapMqttEndpoint("/mqtt")).IsSameReferenceAs(endpoints);

        var connectionBuilder = new ConnectionBuilder(provider);
        await Assert.That(connectionBuilder.UseMqttConnectionHandler()).IsSameReferenceAs(connectionBuilder);

        var mqttFactory = new MqttServerFactory();
        using var server = mqttFactory.CreateMqttServer(
            mqttFactory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build());
        var applicationServices = new ServiceCollection();
        _ = applicationServices.AddSingleton(server);
        await using var applicationProvider = applicationServices.BuildServiceProvider();
        var mqttApplication = new ApplicationBuilder(applicationProvider);
        var configured = false;
        await Assert.That(mqttApplication.ConfigureMqttServer(
            resolvedServer => configured = ReferenceEquals(resolvedServer, server)))
            .IsSameReferenceAs(mqttApplication);
        await Assert.That(configured).IsTrue();
    }

    /// <summary>Verifies hosted-server configuration and both lifecycle state projections.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task HostedServerExtensions_ConfigureAndObserveStartedStateAsync()
    {
        var factory = new MqttServerFactory();
        using var lifetime = new TestHostApplicationLifetime();
        using var server = new MqttHostedServer(
            lifetime,
            factory,
            factory.CreateServerOptionsBuilder().WithoutDefaultEndpoint().Build(),
            [],
            MqttNetNullLogger.Instance);

        await Assert.That(server.WithAcceptNewConnections(false)).IsSameReferenceAs(server);
        await Assert.That(server.AcceptNewConnections).IsFalse();
        await Assert.That(server.WithServerSessionItem("key", "value")).IsSameReferenceAs(server);
        await Assert.That(server.ServerSessionItems["key"]).IsEqualTo("value");

        var configured = false;
        await Assert.That(server.ConfigureHostedServer(_ => configured = true)).IsSameReferenceAs(server);
        await Assert.That(configured).IsTrue();
        await Assert.That(() => server.ConfigureHostedServer(null!)).Throws<ArgumentNullException>();

        var states = new List<bool>();
        var asyncStates = new List<bool>();
        using var subscription = server.IsStartedChanges().Subscribe(states.Add);
        await using var asyncSubscription = await server.ObserveIsStarted().SubscribeAsync(
            (value, cancellationToken) =>
            {
                _ = cancellationToken;
                asyncStates.Add(value);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        await server.StartAsync();
        await server.StopAsync(factory.CreateMqttServerStopOptionsBuilder().Build());

        await Assert.That(states).IsEquivalentTo([false, true, false]);
        await Assert.That(asyncStates).IsEquivalentTo([false, true, false]);
    }

    /// <summary>Verifies property snapshots and every cold synchronous/asynchronous connection operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ConnectionContextExtensions_ExposePropertiesAndOperationsAsync()
    {
        var formatter = new MqttPacketFormatterAdapter(
            MqttProtocolVersion.V500,
            new MqttBufferWriter(InitialBufferSize, MaximumBufferSize));
        var pipes = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        var connectionContext = new DefaultConnectionContext { Transport = pipes.Transport };
        using var connection = new MqttConnectionContext(formatter, connectionContext);

        await AssertInitialPropertiesAsync(connection.Properties(), formatter);
        _ = await connection.Connect().FirstAsync(Timeout);
        _ = await connection.ConnectSignal().FirstAsync(Timeout);

        _ = await connection.SendPacket(new MqttPingReqPacket()).FirstAsync(Timeout);
        await DrainSentPacketAsync(pipes.Application);
        _ = await connection.SendPacketSignal(new MqttPingRespPacket()).FirstAsync(Timeout);
        await DrainSentPacketAsync(pipes.Application);

        await pipes.Application.Output.WriteAsync(PingRequestBytes);
        await Assert.That(await connection.ReceivePacket().FirstAsync(Timeout)).IsTypeOf<MqttPingReqPacket>();
        await pipes.Application.Output.WriteAsync(PingResponseBytes);
        await Assert.That(await connection.ReceivePacketSignal().FirstAsync(Timeout)).IsTypeOf<MqttPingRespPacket>();

        await AssertPopulatedAndResetPropertiesAsync(connection);
        await Assert.That(() => connection.SendPacket(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => connection.SendPacketSignal(null!)).Throws<ArgumentNullException>();
        _ = await connection.Disconnect().FirstAsync(Timeout);
        _ = await connection.DisconnectSignal().FirstAsync(Timeout);
    }

    /// <summary>Verifies an initial connection-property snapshot.</summary>
    /// <param name="properties">The captured properties.</param>
    /// <param name="formatter">The expected formatter.</param>
    /// <returns>A task that represents the asynchronous assertions.</returns>
    private static async Task AssertInitialPropertiesAsync(
        MqttConnectionProperties properties,
        MqttPacketFormatterAdapter formatter)
    {
        await Assert.That(properties.BytesReceived).IsEqualTo(0);
        await Assert.That(properties.BytesSent).IsEqualTo(0);
        await Assert.That(properties.ClientCertificate).IsNull();
        await Assert.That(properties.IsSecureConnection).IsFalse();
        await Assert.That(properties.LocalEndPoint).IsNull();
        await Assert.That(properties.RemoteEndPoint).IsNull();
        await Assert.That(properties.PacketFormatterAdapter).IsSameReferenceAs(formatter);
    }

    /// <summary>Verifies populated counters and their fluent reset.</summary>
    /// <param name="connection">The connection under test.</param>
    /// <returns>A task that represents the asynchronous assertions.</returns>
    private static async Task AssertPopulatedAndResetPropertiesAsync(MqttConnectionContext connection)
    {
        var populated = connection.Properties();
        await Assert.That(populated.BytesReceived).IsGreaterThan(0);
        await Assert.That(populated.BytesSent).IsGreaterThan(0);
        await Assert.That(connection.ResetConnectionStatistics()).IsSameReferenceAs(connection);
        await Assert.That(connection.Properties().BytesReceived).IsEqualTo(0);
        await Assert.That(connection.Properties().BytesSent).IsEqualTo(0);
    }

    /// <summary>Drains one encoded packet from a paired pipe.</summary>
    /// <param name="pipe">The paired pipe endpoint.</param>
    /// <returns>A task that represents the drain operation.</returns>
    private static async Task DrainSentPacketAsync(IDuplexPipe pipe)
    {
        var result = await pipe.Input.ReadAsync();
        pipe.Input.AdvanceTo(result.Buffer.End);
    }

    /// <summary>Returns the only registered MQTT server adapter.</summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The registered adapter.</returns>
    private static IMqttServerAdapter GetOnlyAdapter(IServiceProvider provider)
    {
        IMqttServerAdapter? found = null;
        foreach (var adapter in provider.GetServices<IMqttServerAdapter>())
        {
            if (found is not null)
            {
                throw new InvalidOperationException("Expected only one MQTT server adapter.");
            }

            found = adapter;
        }

        return found ?? throw new InvalidOperationException("Expected one MQTT server adapter.");
    }

    /// <summary>Creates services required to construct a hosted MQTT server.</summary>
    /// <returns>The configured service collection.</returns>
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        _ = services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();
        return services;
    }

    /// <summary>Provides a public endpoint-route builder for exercising endpoint extensions.</summary>
    /// <param name="applicationBuilder">The application builder.</param>
    private sealed class TestEndpointRouteBuilder(IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        /// <inheritdoc/>
        public ICollection<EndpointDataSource> DataSources { get; } = [];

        /// <inheritdoc/>
        public IServiceProvider ServiceProvider => applicationBuilder.ApplicationServices;

        /// <inheritdoc/>
        public IApplicationBuilder CreateApplicationBuilder() => applicationBuilder.New();
    }

    /// <summary>Provides the host-lifetime contract required by the hosted server.</summary>
    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        /// <summary>Signals application start.</summary>
        private readonly CancellationTokenSource _started = new();

        /// <summary>Signals application stop completion.</summary>
        private readonly CancellationTokenSource _stopped = new();

        /// <summary>Signals application shutdown.</summary>
        private readonly CancellationTokenSource _stopping = new();

        /// <inheritdoc/>
        public CancellationToken ApplicationStarted => _started.Token;

        /// <inheritdoc/>
        public CancellationToken ApplicationStopped => _stopped.Token;

        /// <inheritdoc/>
        public CancellationToken ApplicationStopping => _stopping.Token;

        /// <inheritdoc/>
        public void StopApplication() => _stopping.Cancel();

        /// <inheritdoc/>
        public void Dispose()
        {
            _started.Dispose();
            _stopped.Dispose();
            _stopping.Dispose();
        }
    }
}
