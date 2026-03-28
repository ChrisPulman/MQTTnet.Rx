// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Packets;
using ReactiveUI.Extensions.Async;

namespace MQTTnet.Rx.Client.Tests.Helpers;

/// <summary>
/// Mock implementation of <see cref="IResilientMqttClient"/> for testing async observable extensions.
/// </summary>
public sealed class MockResilientMqttClient : IResilientMqttClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MockResilientMqttClient"/> class.
    /// </summary>
    public MockResilientMqttClient()
    {
        ApplicationMessageProcessed = CreateObservable<ApplicationMessageProcessedEventArgs>(
            handler => ApplicationMessageProcessedAsync += handler,
            handler => ApplicationMessageProcessedAsync -= handler);
        ApplicationMessageProcessedAsyncObservable = ApplicationMessageProcessed.ToObservableAsync();
        ApplicationMessageReceived = CreateObservable<MqttApplicationMessageReceivedEventArgs>(
            handler => ApplicationMessageReceivedAsync += handler,
            handler => ApplicationMessageReceivedAsync -= handler);
        ApplicationMessageReceivedAsyncObservable = ApplicationMessageReceived.ToObservableAsync();
        ApplicationMessageSkipped = CreateObservable<ApplicationMessageSkippedEventArgs>(
            handler => ApplicationMessageSkippedAsync += handler,
            handler => ApplicationMessageSkippedAsync -= handler);
        ApplicationMessageSkippedAsyncObservable = ApplicationMessageSkipped.ToObservableAsync();
        Connected = CreateObservable<MqttClientConnectedEventArgs>(
            handler => ConnectedAsync += handler,
            handler => ConnectedAsync -= handler);
        ConnectedAsyncObservable = Connected.ToObservableAsync();
        ConnectingFailed = CreateObservable<ConnectingFailedEventArgs>(
            handler => ConnectingFailedAsync += handler,
            handler => ConnectingFailedAsync -= handler);
        ConnectingFailedAsyncObservable = ConnectingFailed.ToObservableAsync();
        ConnectionStateChanged = CreateObservable<EventArgs>(
            handler => ConnectionStateChangedAsync += handler,
            handler => ConnectionStateChangedAsync -= handler);
        ConnectionStateChangedAsyncObservable = ConnectionStateChanged.ToObservableAsync();
        Disconnected = CreateObservable<MqttClientDisconnectedEventArgs>(
            handler => DisconnectedAsync += handler,
            handler => DisconnectedAsync -= handler);
        DisconnectedAsyncObservable = Disconnected.ToObservableAsync();
        SynchronizingSubscriptionsFailed = CreateObservable<ResilientProcessFailedEventArgs>(
            handler => SynchronizingSubscriptionsFailedAsync += handler,
            handler => SynchronizingSubscriptionsFailedAsync -= handler);
        SynchronizingSubscriptionsFailedAsyncObservable = SynchronizingSubscriptionsFailed.ToObservableAsync();
    }

    /// <inheritdoc/>
    public event Func<ApplicationMessageProcessedEventArgs, Task>? ApplicationMessageProcessedAsync;

    /// <inheritdoc/>
    public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;

    /// <inheritdoc/>
    public event Func<ApplicationMessageSkippedEventArgs, Task>? ApplicationMessageSkippedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;

    /// <inheritdoc/>
    public event Func<ConnectingFailedEventArgs, Task>? ConnectingFailedAsync;

    /// <inheritdoc/>
    public event Func<EventArgs, Task>? ConnectionStateChangedAsync;

    /// <inheritdoc/>
    public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;

    /// <inheritdoc/>
    public event Func<ResilientProcessFailedEventArgs, Task>? SynchronizingSubscriptionsFailedAsync;

    /// <inheritdoc/>
    public event Func<SubscriptionsChangedEventArgs, Task>? SubscriptionsChangedAsync;

    /// <inheritdoc/>
    public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttClientConnectedEventArgs> Connected { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttClientConnectedEventArgs> ConnectedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttClientDisconnectedEventArgs> Disconnected { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttClientDisconnectedEventArgs> DisconnectedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ConnectingFailedEventArgs> ConnectingFailed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ConnectingFailedEventArgs> ConnectingFailedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<EventArgs> ConnectionStateChanged { get; }

    /// <inheritdoc/>
    public IObservableAsync<EventArgs> ConnectionStateChangedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }

    /// <inheritdoc/>
    public IObservableAsync<ResilientProcessFailedEventArgs> SynchronizingSubscriptionsFailedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped { get; }

    /// <inheritdoc/>
    public IObservableAsync<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedAsyncObservable { get; }

    /// <inheritdoc/>
    public IObservable<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived { get; }

    /// <inheritdoc/>
    public IObservableAsync<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedAsyncObservable { get; }

    /// <inheritdoc/>
    public IMqttClient InternalClient { get; } = new MockMqttClient();

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public bool IsStarted { get; private set; }

    /// <inheritdoc/>
    public ResilientMqttClientOptions? Options { get; private set; }

    /// <inheritdoc/>
    public int PendingApplicationMessagesCount => 0;

    /// <inheritdoc/>
    public Task EnqueueAsync(MqttApplicationMessage applicationMessage) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnqueueAsync(ResilientMqttApplicationMessage applicationMessage) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(ResilientMqttClientOptions options)
    {
        Options = options;
        IsStarted = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(bool cleanDisconnect = true)
    {
        IsStarted = false;
        IsConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task UnsubscribeAsync(IEnumerable<string> topics) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <summary>
    /// Raises the connected event.
    /// </summary>
    public async Task SimulateConnectedAsync()
    {
        IsConnected = true;
        if (ConnectedAsync is not null)
        {
            await ConnectedAsync(new MqttClientConnectedEventArgs(new MqttClientConnectResult())).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Raises the application message processed event.
    /// </summary>
    public async Task SimulateApplicationMessageProcessedAsync()
    {
        if (ApplicationMessageProcessedAsync is null)
        {
            return;
        }

        var args = new ApplicationMessageProcessedEventArgs(
            new ResilientMqttApplicationMessage
            {
                ApplicationMessage = new MqttApplicationMessage { Topic = "processed/topic" }
            },
            null);

        await ApplicationMessageProcessedAsync(args).ConfigureAwait(false);
    }

    private static IObservable<T> CreateObservable<T>(Action<Func<T, Task>> addHandler, Action<Func<T, Task>> removeHandler) =>
        Observable.Create<T>(observer =>
            {
                Task Handler(T args)
                {
                    observer.OnNext(args);
                    return Task.CompletedTask;
                }

                addHandler(Handler);
                return Disposable.Create(() => removeHandler(Handler));
            })
            .Publish()
            .RefCount();
}
