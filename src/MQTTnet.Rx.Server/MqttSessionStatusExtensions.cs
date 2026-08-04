// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides reactive operations, properties, and fluent configuration for MQTT session statuses.</summary>
public static class MqttSessionStatusExtensions
{
    /// <summary>Provides reactive session-status features.</summary>
    /// <param name="session">The MQTT session status.</param>
    extension(MqttSessionStatus session)
    {
        /// <summary>Captures every public session property immediately.</summary>
        /// <returns>The current session-property snapshot.</returns>
        public MqttSessionStatusProperties Properties() => new(
            session.CreatedTimestamp,
            session.DisconnectedTimestamp,
            session.ExpiryInterval,
            session.Id,
            MqttPropertySnapshot.Copy(session.Items),
            session.PendingApplicationMessagesCount);

        /// <summary>Reads an arbitrary session property once per subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold property projection.</returns>
        public IObservable<T> Property<T>(Func<MqttSessionStatus, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTask(_ => Task.FromResult(selector(session)));
        }

        /// <summary>Reads an arbitrary session property once per asynchronous subscription.</summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="selector">Selects the property value.</param>
        /// <returns>A cold asynchronous property projection.</returns>
        public IObservableAsync<T> ObserveProperty<T>(Func<MqttSessionStatus, T> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return CreateObservable.FromTaskSignal(_ => Task.FromResult(selector(session)));
        }

        /// <summary>Captures every public session property once per subscription.</summary>
        /// <returns>A cold session-property snapshot.</returns>
        public IObservable<MqttSessionStatusProperties> PropertySnapshots() =>
            session.Property(static value => value.Properties());

        /// <summary>Captures every public session property once per asynchronous subscription.</summary>
        /// <returns>A cold asynchronous session-property snapshot.</returns>
        public IObservableAsync<MqttSessionStatusProperties> ObservePropertySnapshots() =>
            session.ObserveProperty(static value => value.Properties());

        /// <summary>Adds or replaces one session item.</summary>
        /// <param name="key">The session-item key.</param>
        /// <param name="value">The session-item value.</param>
        /// <returns>The original session status.</returns>
        public MqttSessionStatus WithSessionItem(object key, object value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            session.Items[key] = value;
            return session;
        }

        /// <summary>Removes one session item.</summary>
        /// <param name="key">The session-item key.</param>
        /// <returns>The original session status.</returns>
        public MqttSessionStatus WithoutSessionItem(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            session.Items.Remove(key);
            return session;
        }

        /// <summary>Clears every session item.</summary>
        /// <returns>The original session status.</returns>
        public MqttSessionStatus ClearSessionItems()
        {
            session.Items.Clear();
            return session;
        }

        /// <summary>Clears queued application messages when subscribed.</summary>
        /// <returns>A cold clear operation.</returns>
        public IObservable<RxVoid> ClearApplicationMessagesQueue() =>
            CreateObservable.FromTask(cancellationToken =>
            {
                var operation = InvokeClearApplicationMessagesQueue(session);
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Clears queued application messages through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous clear operation.</returns>
        public IObservableAsync<RxVoid> ObserveClearApplicationMessagesQueue() =>
            CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = InvokeClearApplicationMessagesQueue(session);
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Deletes the session when subscribed.</summary>
        /// <returns>A cold delete operation.</returns>
        public IObservable<RxVoid> Delete() =>
            CreateObservable.FromTask(cancellationToken =>
            {
                var operation = session.DeleteAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Deletes the session through an asynchronous observable.</summary>
        /// <returns>A cold asynchronous delete operation.</returns>
        public IObservableAsync<RxVoid> ObserveDelete() =>
            CreateObservable.FromTaskSignal(cancellationToken =>
            {
                var operation = session.DeleteAsync();
                return operation.WaitAsync(cancellationToken);
            });

        /// <summary>Delivers an application message immediately when subscribed.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold delivery operation.</returns>
        public IObservable<InjectMqttApplicationMessageResult> DeliverApplicationMessage(
            MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTask<InjectMqttApplicationMessageResult>(cancellationToken =>
            {
                var operation = session.DeliverApplicationMessageAsync(message);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Delivers an application message immediately through an asynchronous observable.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold asynchronous delivery operation.</returns>
        public IObservableAsync<InjectMqttApplicationMessageResult> ObserveDeliverApplicationMessage(
            MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTaskSignal<InjectMqttApplicationMessageResult>(cancellationToken =>
            {
                var operation = session.DeliverApplicationMessageAsync(message);
                return operation.WaitAsync(cancellationToken);
            });
        }

        /// <summary>Attempts to enqueue an application message when subscribed.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold enqueue attempt.</returns>
        public IObservable<MqttSessionEnqueueResult> TryEnqueueApplicationMessage(MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTask(_ =>
            {
                var isEnqueued = session.TryEnqueueApplicationMessage(message, out var result);
                return Task.FromResult(new MqttSessionEnqueueResult(isEnqueued, result));
            });
        }

        /// <summary>Attempts to enqueue an application message through an asynchronous observable.</summary>
        /// <param name="message">The application message.</param>
        /// <returns>A cold asynchronous enqueue attempt.</returns>
        public IObservableAsync<MqttSessionEnqueueResult> ObserveTryEnqueueApplicationMessage(
            MqttApplicationMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateObservable.FromTaskSignal(_ =>
            {
                var isEnqueued = session.TryEnqueueApplicationMessage(message, out var result);
                return Task.FromResult(new MqttSessionEnqueueResult(isEnqueued, result));
            });
        }

    }

    /// <summary>Normalizes MQTTnet implementations that throw before returning their operation task.</summary>
    /// <param name="session">The session whose queue should be cleared.</param>
    /// <returns>The clear operation or a faulted task.</returns>
    private static Task InvokeClearApplicationMessagesQueue(MqttSessionStatus session)
    {
        try
        {
            return session.ClearApplicationMessagesQueueAsync();
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
