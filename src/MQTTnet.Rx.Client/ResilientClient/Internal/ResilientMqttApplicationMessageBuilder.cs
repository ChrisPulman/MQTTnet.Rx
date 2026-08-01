// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>Builds a resilient MQTT application message.</summary>
/// <remarks>This builder enables the step-by-step construction of a resilient MQTT application message by
/// allowing the caller to specify a unique identifier and configure the underlying MQTT message. Use the fluent methods
/// to set the desired properties before calling Build to create the final message instance.</remarks>
internal sealed class ResilientMqttApplicationMessageBuilder
{
    /// <summary>Stores the identifier assigned to the message being built.</summary>
    private Guid _id = Guid.NewGuid();

    /// <summary>Stores the MQTT application message being built.</summary>
    private MqttApplicationMessage? _applicationMessage;

    /// <summary>Sets the identifier for the MQTT application message being built.</summary>
    /// <param name="id">The unique identifier to assign to the message.</param>
    /// <returns>The current <see cref="ResilientMqttApplicationMessageBuilder"/> instance with the specified identifier
    /// set.</returns>
    internal ResilientMqttApplicationMessageBuilder WithId(in Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>Sets the MQTT application message to be published by the builder.</summary>
    /// <param name="applicationMessage">The MQTT application message to use for publishing. Cannot be null.</param>
    /// <returns>The current instance of <see cref="ResilientMqttApplicationMessageBuilder"/> to allow method
    /// chaining.</returns>
    internal ResilientMqttApplicationMessageBuilder WithApplicationMessage(
        MqttApplicationMessage applicationMessage)
    {
        _applicationMessage = applicationMessage;
        return this;
    }

    /// <summary>Configures the MQTT application message to be published using the specified builder action.</summary>
    /// <remarks>Use this method to set the topic, payload, QoS, and other properties of the MQTT application
    /// message before publishing. This method supports a fluent configuration style.</remarks>
    /// <param name="builder">An action that receives an instance of <see cref="MqttApplicationMessageBuilder"/> to
    /// configure the application
    /// message properties.</param>
    /// <returns>The current <see cref="ResilientMqttApplicationMessageBuilder"/> instance for method
    /// chaining.</returns>
    internal ResilientMqttApplicationMessageBuilder WithApplicationMessage(
        Action<MqttApplicationMessageBuilder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var internalBuilder = new MqttApplicationMessageBuilder();
        builder(internalBuilder);

        _applicationMessage = internalBuilder.Build();
        return this;
    }

    /// <summary>Builds the configured resilient application message.</summary>
    /// <returns>A ResilientMqttApplicationMessage initialized with the current configuration settings.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the application message has not been set before calling
    /// this method.</exception>
    internal ResilientMqttApplicationMessage Build()
    {
        if (_applicationMessage is null)
        {
            throw new InvalidOperationException("The ApplicationMessage cannot be null.");
        }

        return new() { Id = _id, ApplicationMessage = _applicationMessage };
    }
}
