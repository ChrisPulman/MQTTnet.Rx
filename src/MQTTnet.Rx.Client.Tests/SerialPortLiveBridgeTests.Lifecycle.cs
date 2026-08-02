// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
#if REACTIVE_SHIM
using IoT.Driver.Serial.Reactive;
#else
using IoT.Driver.Serial;
#endif
using MQTTnet.Protocol;
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using MQTTnet.Rx.SerialPort.Reactive;
#else
using MQTTnet.Rx.SerialPort;
#endif
using NSubstitute;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using ClientCreate = MQTTnet.Rx.Client.Reactive.Create;
#else
using ClientCreate = MQTTnet.Rx.Client.Create;
#endif
#if REACTIVE_SHIM
using SerialCreate = MQTTnet.Rx.SerialPort.Reactive.Create;
#else
using SerialCreate = MQTTnet.Rx.SerialPort.Create;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains serial bridge lifecycle tests and their local infrastructure.</summary>
public sealed partial class SerialPortLiveBridgeTests
{
    /// <summary>Verifies a finite serial frame publishes while the timer-backed bridge remains owned.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PublisherLifecycle_FiniteFrameRemainsOwnedUntilDisposedAsync()
    {
        const string topic = "tests/serial/lifecycle";
        using var client = new MockMqttClient();
        var completionPort = Substitute.For<ISerialPortRx>();
        _ = completionPort.DataReceived.Returns(Signal.FromEnumerable("<completed>"));
        var publishResult = new TaskCompletionSource<MqttClientPublishResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = false;
        using var lifecycleSubscription = SerialCreate.PublishSerialPort(
                Signal.FromEnumerable<IMqttClient>([client]),
                topic,
                completionPort,
                Signal.Emit('<'),
                Signal.Emit('>'),
                FrameTimeoutMilliseconds)
            .Subscribe(
                result => _ = publishResult.TrySetResult(result),
                exception => _ = publishResult.TrySetException(exception),
                () => completed = true);

        _ = await publishResult.Task.WaitAsync(OperationTimeout);
        await Assert.That(client.PublishedMessages).Count().IsEqualTo(1);
        await Assert.That(completed).IsFalse();
        lifecycleSubscription.Dispose();
    }

    /// <summary>Verifies a writer payload-factory exception reaches the MQTT receive operation.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WriterLifecycle_PayloadFactoryFaultPropagatesAsync()
    {
        const string topic = "tests/serial/lifecycle/fault";
        using var mockClient = new MockMqttClient();
        using var faultPair = new InMemoryPortRxPair();
        await OpenPairAsync(faultPair, "\n");
        using var faultingBridge = Signal.Emit<IMqttClient>(mockClient).SubscribeSerialPortWrite(
            topic,
            faultPair.First,
            (Func<string, string>)(static _ => throw new InvalidOperationException("payload rejected")));

        await Assert.That(() => mockClient.SimulateMessageReceivedAsync(topic, "bad"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Verifies disposing a writer removes serial ownership while MQTT delivery continues.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WriterLifecycle_DisposalStopsPairedSerialWritesAsync()
    {
        const string topic = "tests/serial/lifecycle/disposal";
        const string afterDisposePayload = "after-dispose";
        using var client = new MockMqttClient();
        using var disposalPair = new InMemoryPortRxPair();
        await OpenPairAsync(disposalPair, "\n");
        var receivedLines = new List<string>();
        var beforeDisposeReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lineCapture = disposalPair.Second.Lines.Subscribe(line =>
        {
            receivedLines.Add(line);
            if (line != "before-dispose")
            {
                return;
            }

            _ = beforeDisposeReceived.TrySetResult(true);
        });
        var bridge = Signal.Emit<IMqttClient>(client)
            .SubscribeSerialPortWriteLine(topic, disposalPair.First, static value => value);
        await client.SimulateMessageReceivedAsync(topic, "before-dispose");
        _ = await beforeDisposeReceived.Task.WaitAsync(OperationTimeout);
        bridge.Dispose();
        await client.SimulateMessageReceivedAsync(topic, afterDisposePayload);
        await Assert.That(receivedLines).DoesNotContain(afterDisposePayload);

        disposalPair.Dispose();
        await Assert.That(disposalPair.First.IsDisposed).IsTrue();
        await Assert.That(disposalPair.Second.IsDisposed).IsTrue();
    }

    /// <summary>Configures matching line separators and opens both deterministic serial endpoints.</summary>
    /// <param name="pair">The connected in-memory serial pair.</param>
    /// <param name="newLine">The line separator shared by both endpoints.</param>
    /// <returns>A task that completes when both endpoints are open.</returns>
    private static async Task OpenPairAsync(InMemoryPortRxPair pair, string newLine)
    {
        pair.First.NewLine = newLine;
        pair.Second.NewLine = newLine;
        await pair.First.OpenAsync();
        await pair.Second.OpenAsync();
    }

    /// <summary>Ensures a raw client has an acknowledged subscription for a test topic.</summary>
    /// <param name="client">The connected raw MQTT client.</param>
    /// <param name="topic">The exact topic to subscribe to.</param>
    /// <returns>A task that completes after the broker acknowledges the subscription.</returns>
    private static async Task EnsureRawSubscriptionAsync(IMqttClient client, string topic)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        var result = await client.SubscribeAsync(options, CancellationToken.None).WaitAsync(OperationTimeout);
        await Assert.That(result.Items).Count().IsEqualTo(1);
    }

    /// <summary>Publishes a test payload from the real probe client.</summary>
    /// <param name="client">The connected probe client.</param>
    /// <param name="topic">The destination topic.</param>
    /// <param name="payload">The UTF-8 payload.</param>
    /// <returns>A task that completes after the broker acknowledges the publish.</returns>
    private static async Task PublishFromProbeAsync(IMqttClient client, string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        var result = await client.PublishAsync(message, CancellationToken.None).WaitAsync(OperationTimeout);
        await Assert.That(result.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
    }

    /// <summary>Determines whether a captured batch is the ASCII representation of the expected text.</summary>
    /// <param name="batches">The captured serial receive batches.</param>
    /// <param name="expected">The expected ASCII text.</param>
    /// <returns><see langword="true"/> when an exact batch was captured.</returns>
    private static bool ContainsBatch(List<byte[]> batches, string expected)
    {
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        lock (batches)
        {
            return batches.Exists(batch => batch.AsSpan().SequenceEqual(expectedBytes));
        }
    }

    /// <summary>Invokes one private writer core with a deliberately missing write delegate.</summary>
    /// <typeparam name="TClient">The raw or resilient MQTT client interface.</typeparam>
    /// <param name="clients">The client observable supplied to the core.</param>
    /// <param name="serialPort">The deterministic serial endpoint supplied to the core.</param>
    /// <returns>The unwrapped defensive-validation exception.</returns>
    private static Exception? InvokeWriteCoreWithNullWrite<TClient>(
        IObservable<TClient> clients,
        ISerialPortRx serialPort)
    {
        foreach (var candidate in typeof(SerialPortMqttExtensions).GetMethods(
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        {
            if (candidate.Name != WriterCoreMethodName)
            {
                continue;
            }

            var firstParameter = candidate.GetParameters()[0].ParameterType;
            if (firstParameter.GetGenericArguments()[0] != typeof(TClient))
            {
                continue;
            }

            var method = candidate.MakeGenericMethod(typeof(string));
            try
            {
                _ = method.Invoke(
                    null,
                    [clients, ValidationTopic, serialPort, (Func<string, string>)(static value => value), null]);
            }
            catch (System.Reflection.TargetInvocationException exception)
            {
                return exception.InnerException;
            }
        }

        return new MissingMethodException(typeof(SerialPortMqttExtensions).FullName, WriterCoreMethodName);
    }

    /// <summary>Owns a real resilient client and its factory observable subscription.</summary>
    private sealed class LiveResilientLease : IAsyncDisposable
    {
        /// <summary>The subscription that owns the shared resilient client.</summary>
        private readonly IDisposable _sourceLease;

        /// <summary>Initializes a new instance of the <see cref="LiveResilientLease"/> class.</summary>
        /// <param name="source">The shared resilient-client observable.</param>
        /// <param name="client">The started resilient client.</param>
        /// <param name="sourceLease">The subscription that owns the client.</param>
        private LiveResilientLease(
            IObservable<IResilientMqttClient> source,
            IResilientMqttClient client,
            IDisposable sourceLease)
        {
            Source = source;
            Client = client;
            _sourceLease = sourceLease;
        }

        /// <summary>Gets the shared resilient-client source used by bridge methods.</summary>
        public IObservable<IResilientMqttClient> Source { get; }

        /// <summary>Gets the started real resilient client.</summary>
        private IResilientMqttClient Client { get; }

        /// <summary>Creates and connects a real resilient client to the live broker.</summary>
        /// <param name="port">The live broker's ephemeral loopback port.</param>
        /// <returns>An owned connected resilient-client lease.</returns>
        public static async Task<LiveResilientLease> StartAsync(int port)
        {
            var source = ClientCreate.ResilientMqttClient();
            var clientCompletion = new TaskCompletionSource<IResilientMqttClient>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sourceLease = source.Subscribe(
                client => _ = clientCompletion.TrySetResult(client),
                exception => _ = clientCompletion.TrySetException(exception),
                () => _ = clientCompletion.TrySetException(
                    new InvalidOperationException("The resilient source completed without a client.")));
            try
            {
                var client = await clientCompletion.Task.WaitAsync(OperationTimeout);
                var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var connectedSubscription = client.Connected.Subscribe(connectedArgs =>
                {
                    GC.KeepAlive(connectedArgs);
                    _ = connected.TrySetResult(true);
                });
                var options = new ResilientMqttClientOptionsBuilder()
                    .WithClientOptions(builder =>
                    {
                        _ = builder
                            .WithClientId($"serial-resilient-{Guid.NewGuid():N}")
                            .WithTcpServer(IPAddress.Loopback.ToString(), port);
                    })
                    .Build();
                await client.StartAsync(options).WaitAsync(OperationTimeout);
                if (!client.IsConnected)
                {
                    _ = await connected.Task.WaitAsync(OperationTimeout);
                }

                return new(source, client, sourceLease);
            }
            catch
            {
                sourceLease.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Client.IsStarted)
                {
                    await Client.StopAsync().WaitAsync(OperationTimeout);
                }
            }
            finally
            {
                _sourceLease.Dispose();
            }
        }
    }
}
