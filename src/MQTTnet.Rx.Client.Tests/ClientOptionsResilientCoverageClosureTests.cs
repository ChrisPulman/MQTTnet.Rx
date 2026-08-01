// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Disposables;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes public client-option and resilient-client behavior coverage.</summary>
[NotInParallel]
public sealed class ClientOptionsResilientCoverageClosureTests
{
    /// <summary>Defines the expected connection count after the initial connection and one reconnect.</summary>
    private const int ExpectedConnectionCount = 2;

    /// <summary>Defines the expected connection count before any reconnect attempt.</summary>
    private const int InitialConnectionCount = 1;

    /// <summary>Defines the loopback host used to create reconnectable client options.</summary>
    private const string LoopbackHost = "localhost";

    /// <summary>Defines the number of failed reconnect attempts used by retry-policy tests.</summary>
    private const int ReconnectFailureCount = 2;

    /// <summary>Defines the total connections after bounded retries exhaust their limit.</summary>
    private const int ExpectedBoundedConnectionCount = 3;

    /// <summary>Defines the total connections after unlimited retries recover.</summary>
    private const int ExpectedUnlimitedConnectionCount = 4;

    /// <summary>Uses a bounded wait for observable callbacks and loopback MQTT operations.</summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Defines the short connection-maintenance polling interval used by loopback tests.</summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>Defines the short delay used to schedule deterministic reconnection.</summary>
    private static readonly TimeSpan ReconnectDelay = TimeSpan.Zero;

    /// <summary>Defines a delay long enough to exercise an in-flight reconnect cancellation.</summary>
    private static readonly TimeSpan DelayedReconnectDelay = TimeSpan.FromSeconds(1);

    /// <summary>Ensures automatic reconnection emits the recovered client.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_ReconnectsAfterDisconnectionAsync()
    {
        using var client = new MockMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(LoopbackHost).Build());

        var clients = new TestClientObservable();
        var reconnectingClients = clients
            .WithAutoReconnect(ReconnectDelay, 1);
        var observed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = reconnectingClients.Subscribe(new TestObserver<IMqttClient>(
            _ => observed.TrySetResult(true)));
        clients.Emit(client);
        await observed.Task.WaitAsync(OperationTimeout);
        await Assert.That(client.DisconnectedHandlerCount).IsEqualTo(1);

        await client.SimulateDisconnectedAsync();
        await WaitUntilAsync(() => client.ConnectCount == ExpectedConnectionCount);
        await Assert.That(client.ConnectCount).IsEqualTo(ExpectedConnectionCount);
    }

    /// <summary>Ensures a configured reconnect bound terminates after the exact number of failed attempts.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_ReportsErrorAfterTheConfiguredFailedAttemptLimitAsync()
    {
        using var client = new MockMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(LoopbackHost).Build());
        client.ReconnectFailuresRemaining = ReconnectFailureCount;
        var clients = new TestClientObservable();
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = clients.WithAutoReconnect(ReconnectDelay, ReconnectFailureCount).Subscribe(
            new TestObserver<IMqttClient>(static _ => { }, exception => error.TrySetResult(exception)));
        clients.Emit(client);

        await client.SimulateDisconnectedAsync();
        var exception = await error.Task.WaitAsync(OperationTimeout);

        await Assert.That(client.ConnectCount).IsEqualTo(ExpectedBoundedConnectionCount);
        await Assert.That(exception).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Ensures an unlimited reconnect policy continues through failures until it succeeds.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_UnlimitedPolicyContinuesUntilRecoveryAsync()
    {
        using var client = new MockMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(LoopbackHost).Build());
        client.ReconnectFailuresRemaining = ReconnectFailureCount;
        var clients = new TestClientObservable();
        using var subscription = clients.WithAutoReconnect(ReconnectDelay, 0).Subscribe(
            new TestObserver<IMqttClient>(static _ => { }));
        clients.Emit(client);

        await client.SimulateDisconnectedAsync();
        await WaitUntilAsync(() => client.ConnectCount == ExpectedUnlimitedConnectionCount);

        await Assert.That(client.IsConnected).IsTrue();
    }

    /// <summary>Ensures duplicate disconnects do not overlap and disposal cancels a pending reconnect.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_IgnoresOverlappingDisconnectsAndCancelsPendingReconnectAsync()
    {
        using var client = new MockMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(LoopbackHost).Build());
        var clients = new TestClientObservable();
        var subscription = clients.WithAutoReconnect(DelayedReconnectDelay, 0).Subscribe(
            new TestObserver<IMqttClient>(static _ => { }));
        try
        {
            clients.Emit(client);
            await client.SimulateDisconnectedAsync();
            await client.SimulateDisconnectedAsync();
            subscription.Dispose();
            await Task.Delay(DelayedReconnectDelay);

            await Assert.That(client.ConnectCount).IsEqualTo(InitialConnectionCount);
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>Ensures source errors are forwarded to the subscription observer.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_ForwardsSourceErrorsAsync()
    {
        var clients = new TestClientObservable();
        var expected = new InvalidOperationException("source failure");
        var observed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = clients.WithAutoReconnect().Subscribe(
            new TestObserver<IMqttClient>(static _ => { }, exception => observed.TrySetResult(exception)));

        clients.Fail(expected);

        await Assert.That(await observed.Task.WaitAsync(OperationTimeout)).IsSameReferenceAs(expected);
    }

    /// <summary>Ensures source completion is forwarded to the subscription observer.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithAutoReconnect_ForwardsSourceCompletionAsync()
    {
        var clients = new TestClientObservable();
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = clients.WithAutoReconnect().Subscribe(
            new TestObserver<IMqttClient>(static _ => { }, onCompleted: () => completed.TrySetResult(true)));

        clients.Complete();

        await completed.Task.WaitAsync(OperationTimeout);
    }

    /// <summary>Ensures direct client options reject a null MQTT options instance.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ResilientOptionsBuilder_RejectsNullDirectClientOptionsAsync()
    {
        var builder = new ResilientMqttClientOptionsBuilder();

        await Assert.That(() => builder.WithClientOptions((MqttClientOptions)null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Ensures the trust-all TLS validation callback accepts an untrusted certificate.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task WithTlsTrustAllCertificates_AcceptsASelfSignedTlsPeerAsync()
    {
        using var certificate = CreateLoopbackCertificate();
        using var chain = new X509Chain();
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId($"tls-coverage-{Guid.NewGuid():N}")
            .WithTcpServer("localhost")
            .WithTlsTrustAllCertificates()
            .Build();
        var tlsOptions = clientOptions.ChannelOptions.TlsOptions;
        MqttClientCertificateValidationEventArgs validationArguments = new(
            certificate,
            chain,
            SslPolicyErrors.RemoteCertificateChainErrors,
            clientOptions.ChannelOptions);
        var certificateValidationHandler = tlsOptions.CertificateValidationHandler;
        ArgumentNullException.ThrowIfNull(certificateValidationHandler);
        var validationResult = certificateValidationHandler(validationArguments);

        await Assert.That(validationResult).IsTrue();
    }

    /// <summary>Ensures skipped-message notifications are raised for both standard and awaited registrations.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ResilientClient_ReportsSkippedMessagesToBothPublicEventSurfacesAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        var source = Create.ResilientMqttClient();
        IResilientMqttClient? client = null;
        using var owner = source.Subscribe(value => client = value);
        var resilientClient = client ?? throw new InvalidOperationException("The factory did not produce a client.");

        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var skippedByHandler = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var skippedByEvent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var connectedRegistration = resilientClient.RegisterConnectedHandler((_, _) =>
        {
            _ = connected.TrySetResult(true);
            return ValueTask.CompletedTask;
        });
        using var skippedRegistration = resilientClient.RegisterApplicationMessageSkippedHandler((_, _) =>
        {
            _ = skippedByHandler.TrySetResult(true);
            return ValueTask.CompletedTask;
        });

        await StartResilientClientAsync(
            resilientClient,
            broker,
            $"resilient-coverage-{Guid.NewGuid():N}",
            TimeSpan.Zero,
            0);
        await connected.Task.WaitAsync(OperationTimeout);

        await resilientClient.EnqueueAsync(new MqttApplicationMessage { Topic = "coverage/skipped/awaited" });
        await skippedByHandler.Task.WaitAsync(OperationTimeout);

        EventHandler<ApplicationMessageSkippedEventArgs> eventHandler =
            (_, _) => _ = skippedByEvent.TrySetResult(true);
        resilientClient.ApplicationMessageSkippedEvent += eventHandler;
        try
        {
            await resilientClient.EnqueueAsync(new MqttApplicationMessage { Topic = "coverage/skipped/event" });
            await skippedByEvent.Task.WaitAsync(OperationTimeout);
        }
        finally
        {
            resilientClient.ApplicationMessageSkippedEvent -= eventHandler;
            await resilientClient.StopAsync(cleanDisconnect: false);
        }

        await Assert.That(resilientClient.PendingApplicationMessagesCount).IsEqualTo(0);
    }

    /// <summary>Ensures disposal cancels and joins a running resilient connection-maintenance loop.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ResilientClient_DisposeCancelsAnActiveMaintenanceLoopAsync()
    {
        await using var broker = await LiveMqttBroker.StartAsync();
        var source = Create.ResilientMqttClient();
        IResilientMqttClient? client = null;
        using var owner = source.Subscribe(value => client = value);
        var resilientClient = client ?? throw new InvalidOperationException("The factory did not produce a client.");
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var connectedRegistration = resilientClient.RegisterConnectedHandler((_, _) =>
        {
            _ = connected.TrySetResult(true);
            return ValueTask.CompletedTask;
        });

        await resilientClient.StartAsync(new ResilientMqttClientOptions
        {
            ClientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"resilient-dispose-{Guid.NewGuid():N}")
                .WithTcpServer("127.0.0.1", broker.Port)
                .Build(),
            ConnectionCheckInterval = PollingInterval,
        });
        await connected.Task.WaitAsync(OperationTimeout);
        resilientClient.Dispose();

        await Assert.That(resilientClient.IsStarted).IsFalse();
    }

    /// <summary>Creates a temporary self-signed certificate for a loopback TLS handshake.</summary>
    /// <returns>The certificate presented by the loopback TLS peer.</returns>
    private static X509Certificate2 CreateLoopbackCertificate()
    {
        using var key = RSA.Create();
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        return request.CreateSelfSigned(now, now.AddMinutes(1));
    }

    /// <summary>Starts a resilient client against the live test broker.</summary>
    /// <param name="client">The resilient client to start.</param>
    /// <param name="broker">The live broker that accepts the connection.</param>
    /// <param name="clientId">The unique client identifier to configure.</param>
    /// <param name="autoReconnectDelay">The configured automatic reconnect delay.</param>
    /// <param name="maxPendingMessages">The maximum number of pending messages.</param>
    /// <returns>A task that completes when the client has been started.</returns>
    private static Task StartResilientClientAsync(
        IResilientMqttClient client,
        LiveMqttBroker broker,
        string clientId,
        TimeSpan autoReconnectDelay,
        int maxPendingMessages) =>
        client.StartAsync(new ResilientMqttClientOptions
        {
            ClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer("127.0.0.1", broker.Port)
                .Build(),
            AutoReconnectDelay = autoReconnectDelay,
            ConnectionCheckInterval = PollingInterval,
            MaxPendingMessages = maxPendingMessages,
        });

    /// <summary>Waits until a deterministic client state transition has occurred.</summary>
    /// <param name="condition">The state condition to poll.</param>
    /// <returns>A task that completes when the condition becomes true.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(OperationTimeout);
        using var timer = new PeriodicTimer(PollingInterval);
        while (!condition())
        {
            _ = await timer.WaitForNextTickAsync(timeout.Token);
        }
    }

    /// <summary>Provides a controllable client sequence without synchronously emitting during subscription.</summary>
    private sealed class TestClientObservable : IObservable<IMqttClient>
    {
        /// <summary>Stores the subscribed observers.</summary>
        private readonly List<IObserver<IMqttClient>> _observers = [];

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<IMqttClient> observer)
        {
            _observers.Add(observer);
            return Scope.Create(
                (Observers: _observers, Observer: observer),
                static state => state.Observers.Remove(state.Observer));
        }

        /// <summary>Emits a client to all active observers.</summary>
        /// <param name="client">The client to emit.</param>
        public void Emit(IMqttClient client)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(client);
            }
        }

        /// <summary>Terminates all active observers with an error.</summary>
        /// <param name="exception">The terminal error to emit.</param>
        public void Fail(Exception exception)
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnError(exception);
            }
        }

        /// <summary>Completes all active observers.</summary>
        public void Complete()
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnCompleted();
            }
        }
    }

    /// <summary>Adapts a test callback to the standard observable observer contract.</summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    /// <param name="onNext">The callback for observed values.</param>
    /// <param name="onError">The optional callback for terminal errors.</param>
    /// <param name="onCompleted">The optional callback for successful completion.</param>
    private sealed class TestObserver<T>(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted() => onCompleted?.Invoke();

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            if (onError is null)
            {
                return;
            }

            onError(error);
        }

        /// <inheritdoc/>
        public void OnNext(T value) => onNext(value);
    }
}
