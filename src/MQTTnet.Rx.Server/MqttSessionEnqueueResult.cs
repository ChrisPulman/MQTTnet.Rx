// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Represents the result of trying to enqueue an application message.</summary>
/// <param name="IsEnqueued">Whether the message was enqueued.</param>
/// <param name="InjectResult">The MQTTnet injection result, when available.</param>
public sealed record MqttSessionEnqueueResult(
    bool IsEnqueued,
    InjectMqttApplicationMessageResult? InjectResult);
