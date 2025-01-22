// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Resilient Mqtt Client Options Builder.
/// </summary>
public class ResilientMqttClientOptionsBuilder
{
    private readonly ResilientMqttClientOptions _options = new();
    private MqttClientOptionsBuilder? _clientOptionsBuilder;

    /// <summary>
    /// Withes the maximum pending messages.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    public ResilientMqttClientOptionsBuilder WithMaxPendingMessages(int value)
    {
        _options.MaxPendingMessages = value;
        return this;
    }

    /// <summary>
    /// Withes the pending messages overflow strategy.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    public ResilientMqttClientOptionsBuilder WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy value)
    {
        _options.PendingMessagesOverflowStrategy = value;
        return this;
    }

    /// <summary>
    /// Withes the automatic reconnect delay.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    public ResilientMqttClientOptionsBuilder WithAutoReconnectDelay(in TimeSpan value)
    {
        _options.AutoReconnectDelay = value;
        return this;
    }

    /// <summary>
    /// Withes the storage.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    public ResilientMqttClientOptionsBuilder WithStorage(IResilientMqttClientStorage value)
    {
        _options.Storage = value;
        return this;
    }

    /// <summary>
    /// Withes the client options.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    /// <exception cref="InvalidOperationException">Cannot use client options builder and client options at the same time.</exception>
    /// <exception cref="ArgumentNullException">value.</exception>
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
    /// Withes the client options.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    /// <exception cref="InvalidOperationException">Cannot use client options builder and client options at the same time.</exception>
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
    /// Withes the client options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    /// <exception cref="ArgumentNullException">options.</exception>
    public ResilientMqttClientOptionsBuilder WithClientOptions(Action<MqttClientOptionsBuilder> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _clientOptionsBuilder ??= new MqttClientOptionsBuilder();

        options(_clientOptionsBuilder);
        return this;
    }

    /// <summary>
    /// Withes the maximum topic filters in subscribe unsubscribe packets.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>Resilient Mqtt Client Options Builder.</returns>
    public ResilientMqttClientOptionsBuilder WithMaxTopicFiltersInSubscribeUnsubscribePackets(int value)
    {
        _options.MaxTopicFiltersInSubscribeUnsubscribePackets = value;
        return this;
    }

    /// <summary>
    /// Builds this instance.
    /// </summary>
    /// <returns>Resilient Mqtt Client Options.</returns>
    /// <exception cref="InvalidOperationException">The Client Options cannot be null.</exception>
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
