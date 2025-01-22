// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client.ResilientClient.Internal;

/// <summary>
/// Resilient Mqtt Application Message Builder.
/// </summary>
internal class ResilientMqttApplicationMessageBuilder
{
    private Guid _id = Guid.NewGuid();
    private MqttApplicationMessage? _applicationMessage;

    /// <summary>
    /// Withes the identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>Resilient Mqtt Application Message Builder.</returns>
    public ResilientMqttApplicationMessageBuilder WithId(in Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Withes the application message.
    /// </summary>
    /// <param name="applicationMessage">The application message.</param>
    /// <returns>Resilient Mqtt Application Message Builder.</returns>
    public ResilientMqttApplicationMessageBuilder WithApplicationMessage(MqttApplicationMessage applicationMessage)
    {
        _applicationMessage = applicationMessage;
        return this;
    }

    /// <summary>
    /// Withes the application message.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>Resilient Mqtt Application Message Builder.</returns>
    /// <exception cref="ArgumentNullException">builder.</exception>
    public ResilientMqttApplicationMessageBuilder WithApplicationMessage(Action<MqttApplicationMessageBuilder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var internalBuilder = new MqttApplicationMessageBuilder();
        builder(internalBuilder);

        _applicationMessage = internalBuilder.Build();
        return this;
    }

    /// <summary>
    /// Builds this instance.
    /// </summary>
    /// <returns>Resilient Mqtt Application Message.</returns>
    /// <exception cref="InvalidOperationException">The ApplicationMessage cannot be null.</exception>
    public ResilientMqttApplicationMessage Build()
    {
        if (_applicationMessage == null)
        {
            throw new InvalidOperationException("The ApplicationMessage cannot be null.");
        }

        return new()
        {
            Id = _id,
            ApplicationMessage = _applicationMessage
        };
    }
}
