// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Provides a builder for configuring and creating instances of ResilientMqttClientOptions with support for advanced
/// MQTT client settings and resilience features.
/// </summary>
/// <remarks>This builder enables fluent configuration of various MQTT client options, including message
/// buffering, reconnect behavior, and storage. It supports both direct assignment of MqttClientOptions and
/// configuration via MqttClientOptionsBuilder, but only one approach can be used per instance. Attempting to use both
/// will result in an exception. The builder pattern allows for chaining configuration methods before calling Build to
/// create a finalized ResilientMqttClientOptions instance.</remarks>
public class ResilientMqttClientOptionsBuilder
{
    private readonly ResilientMqttClientOptions _options = new();
    private MqttClientOptionsBuilder? _clientOptionsBuilder;

    /// <summary>
    /// Sets the maximum number of messages that can be pending for delivery before new messages are rejected.
    /// </summary>
    /// <remarks>If the number of pending messages reaches the specified limit, additional messages may be
    /// rejected or dropped until space becomes available. This setting can be used to control memory usage and
    /// backpressure in high-throughput scenarios.</remarks>
    /// <param name="value">The maximum number of pending messages allowed. Must be a non-negative integer.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public ResilientMqttClientOptionsBuilder WithMaxPendingMessages(int value)
    {
        _options.MaxPendingMessages = value;
        return this;
    }

    /// <summary>
    /// Sets the strategy to use when the pending messages queue exceeds its capacity.
    /// </summary>
    /// <remarks>Use this method to control how the client handles situations where the number of pending
    /// messages exceeds the configured limit. The chosen strategy determines whether new messages are dropped, oldest
    /// messages are removed, or another policy is applied.</remarks>
    /// <param name="value">The overflow strategy to apply when the pending messages queue is full.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public ResilientMqttClientOptionsBuilder WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy value)
    {
        _options.PendingMessagesOverflowStrategy = value;
        return this;
    }

    /// <summary>
    /// Sets the delay interval to wait before attempting to automatically reconnect after a disconnection.
    /// </summary>
    /// <remarks>Use this method to configure the reconnection backoff period for the MQTT client. Setting an
    /// appropriate delay can help avoid rapid reconnection attempts in unstable network conditions.</remarks>
    /// <param name="value">The time interval to wait before each automatic reconnection attempt. Must be a non-negative duration.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public ResilientMqttClientOptionsBuilder WithAutoReconnectDelay(in TimeSpan value)
    {
        _options.AutoReconnectDelay = value;
        return this;
    }

    /// <summary>
    /// Sets the storage provider to be used for persisting client state and messages.
    /// </summary>
    /// <remarks>Use this method to specify a custom storage mechanism for message persistence and state
    /// recovery. Providing a storage implementation is required for features such as reliable message delivery and
    /// session recovery after disconnections.</remarks>
    /// <param name="value">The storage implementation that handles persistence for the MQTT client. Cannot be null.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public ResilientMqttClientOptionsBuilder WithStorage(IResilientMqttClientStorage value)
    {
        _options.Storage = value;
        return this;
    }

    /// <summary>
    /// Sets the MQTT client options to use for the connection.
    /// </summary>
    /// <remarks>This method cannot be used in combination with a client options builder. Only one approach to
    /// configuring client options can be used per builder instance.</remarks>
    /// <param name="value">The client options to apply to the MQTT connection. Cannot be null.</param>
    /// <returns>The current builder instance with the specified client options applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown if client options have already been set using a client options builder.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public ResilientMqttClientOptionsBuilder WithClientOptions(MqttClientOptions value)
    {
        if (_clientOptionsBuilder != null)
        {
            throw new InvalidOperationException("Cannot use client options builder and client options at the same time.");
        }

        _options.ClientOptions = value ?? throw new ArgumentNullException(nameof(value));

        return this;
    }

    /// <summary>
    /// Configures the MQTT client options using the specified builder.
    /// </summary>
    /// <param name="builder">The builder used to configure MQTT client options. Cannot be null.</param>
    /// <returns>The current instance of <see cref="ResilientMqttClientOptionsBuilder"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if client options have already been set directly, as both a client options builder and direct client
    /// options cannot be used simultaneously.</exception>
    public ResilientMqttClientOptionsBuilder WithClientOptions(MqttClientOptionsBuilder builder)
    {
        if (_options.ClientOptions != null)
        {
            throw new InvalidOperationException("Cannot use client options builder and client options at the same time.");
        }

        _clientOptionsBuilder = builder;
        return this;
    }

    /// <summary>
    /// Configures the underlying MQTT client options using the specified configuration action.
    /// </summary>
    /// <remarks>This method allows customization of MQTT client options before building the resilient client.
    /// Multiple calls will apply additional configuration to the same options builder instance.</remarks>
    /// <param name="options">An action that receives an <see cref="MqttClientOptionsBuilder"/> to configure client options. Cannot be null.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance for method chaining.</returns>
    public ResilientMqttClientOptionsBuilder WithClientOptions(Action<MqttClientOptionsBuilder> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _clientOptionsBuilder ??= new MqttClientOptionsBuilder();

        options(_clientOptionsBuilder);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of topic filters allowed in SUBSCRIBE and UNSUBSCRIBE packets for the MQTT client
    /// configuration.
    /// </summary>
    /// <remarks>Use this method to control the maximum number of topic filters that can be included in a
    /// single SUBSCRIBE or UNSUBSCRIBE packet sent by the client. This setting may be constrained by the MQTT broker's
    /// capabilities or protocol version.</remarks>
    /// <param name="value">The maximum number of topic filters permitted in a single SUBSCRIBE or UNSUBSCRIBE packet. Must be a positive
    /// integer.</param>
    /// <returns>The current <see cref="ResilientMqttClientOptionsBuilder"/> instance with the updated setting.</returns>
    public ResilientMqttClientOptionsBuilder WithMaxTopicFiltersInSubscribeUnsubscribePackets(int value)
    {
        _options.MaxTopicFiltersInSubscribeUnsubscribePackets = value;
        return this;
    }

    /// <summary>
    /// Builds and returns a configured instance of the resilient MQTT client options.
    /// </summary>
    /// <returns>A <see cref="ResilientMqttClientOptions"/> instance containing the configured client options.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the client options are not set or are null.</exception>
    public ResilientMqttClientOptions Build()
    {
        if (_clientOptionsBuilder != null)
        {
            _options.ClientOptions = _clientOptionsBuilder.Build();
        }

        if (_options.ClientOptions == null)
        {
            throw new InvalidOperationException("The Client Options cannot be null.");
        }

        return _options;
    }
}
