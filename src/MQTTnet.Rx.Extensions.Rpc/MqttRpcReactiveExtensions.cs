// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using MQTTnet.Extensions.Rpc;
using MQTTnet.Protocol;
using PrimitivesResult = ReactiveUI.Primitives.Result;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Extensions.Rpc.Reactive;
#else
namespace MQTTnet.Rx.Extensions.Rpc;
#endif

/// <summary>Provides ReactiveUI.Primitives wrappers for MQTTnet RPC clients.</summary>
public static class MqttRpcReactiveExtensions
{
    /// <summary>Provides MQTT RPC client factory extensions.</summary>
    /// <param name="mqttClient">The MQTT client used for request and response messages.</param>
    extension(IMqttClient mqttClient)
    {
        /// <summary>Creates an RPC client over an existing MQTT client.</summary>
        /// <returns>An MQTT RPC client.</returns>
        public IMqttRpcClient CreateRpcClient()
        {
            ArgumentNullException.ThrowIfNull(mqttClient);
            return new MqttClientFactory().CreateMqttRpcClient(mqttClient);
        }

        /// <summary>Creates an RPC client over an existing MQTT client.</summary>
        /// <param name="configure">Configures RPC client options.</param>
        /// <returns>An MQTT RPC client.</returns>
        public IMqttRpcClient CreateRpcClient(Action<MqttRpcClientOptionsBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(mqttClient);
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MqttRpcClientOptionsBuilder();
            configure(builder);
            return new MqttClientFactory().CreateMqttRpcClient(mqttClient, builder.Build());
        }
    }

    /// <summary>Provides reactive MQTT RPC operation extensions.</summary>
    /// <param name="client">The MQTT RPC client.</param>
    extension(IMqttRpcClient client)
    {
        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(TimeSpan timeout, string methodName, byte[] payload) =>
            client.Execute(timeout, methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(
            TimeSpan timeout,
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.Execute(timeout, methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(
            TimeSpan timeout,
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(methodName);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(parameters);
            return Signal.FromAsync(cancellationToken => ExecuteWithTimeoutAsync(
                client,
                timeout,
                methodName,
                payload,
                qualityOfServiceLevel,
                parameters,
                cancellationToken));
        }

        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(TimeSpan timeout, string methodName, string payload) =>
            client.Execute(timeout, methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(
            TimeSpan timeout,
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.Execute(timeout, methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request as a cold observable operation.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold observable that emits the binary response payload.</returns>
        public IObservable<byte[]> Execute(
            TimeSpan timeout,
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return client.Execute(timeout, methodName, Encoding.UTF8.GetBytes(payload), qualityOfServiceLevel, parameters);
        }

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(string methodName, byte[] payload) =>
            client.ObserveExecute(methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.ObserveExecute(methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The binary request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(
            string methodName,
            byte[] payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(methodName);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(parameters);
            return FromAsyncTask(cancellationToken => client.ExecuteAsync(
                methodName,
                payload,
                qualityOfServiceLevel,
                parameters,
                cancellationToken));
        }

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(string methodName, string payload) =>
            client.ObserveExecute(methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.ObserveExecute(methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request as a cold asynchronous observable operation.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold asynchronous observable that emits the binary response payload.</returns>
        public IObservableAsync<byte[]> ObserveExecute(
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return client.ObserveExecute(methodName, Encoding.UTF8.GetBytes(payload), qualityOfServiceLevel, parameters);
        }

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <returns>A cold observable that emits the UTF-8 response payload.</returns>
        public IObservable<string> ExecuteString(TimeSpan timeout, string methodName, string payload) =>
            client.ExecuteString(timeout, methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold observable that emits the UTF-8 response payload.</returns>
        public IObservable<string> ExecuteString(
            TimeSpan timeout,
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.ExecuteString(timeout, methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="timeout">The request timeout.</param>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold observable that emits the UTF-8 response payload.</returns>
        public IObservable<string> ExecuteString(
            TimeSpan timeout,
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters) =>
            client.Execute(timeout, methodName, payload, qualityOfServiceLevel, parameters).Select(DecodeUtf8);

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <returns>A cold asynchronous observable that emits the UTF-8 response payload.</returns>
        public IObservableAsync<string> ObserveExecuteString(string methodName, string payload) =>
            client.ObserveExecuteString(methodName, payload, MqttQualityOfServiceLevel.AtMostOnce, EmptyParameters());

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <returns>A cold asynchronous observable that emits the UTF-8 response payload.</returns>
        public IObservableAsync<string> ObserveExecuteString(
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel) =>
            client.ObserveExecuteString(methodName, payload, qualityOfServiceLevel, EmptyParameters());

        /// <summary>Executes an RPC request and decodes the response payload as UTF-8.</summary>
        /// <param name="methodName">The RPC method name.</param>
        /// <param name="payload">The UTF-8 request payload.</param>
        /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
        /// <param name="parameters">The topic generation parameters.</param>
        /// <returns>A cold asynchronous observable that emits the UTF-8 response payload.</returns>
        public IObservableAsync<string> ObserveExecuteString(
            string methodName,
            string payload,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            IDictionary<string, object> parameters)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(methodName);
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(parameters);
            var requestPayload = Encoding.UTF8.GetBytes(payload);
            return FromAsyncTask(async cancellationToken =>
            {
                var response = await client.ExecuteAsync(
                        methodName,
                        requestPayload,
                        qualityOfServiceLevel,
                        parameters,
                        cancellationToken)
                    .ConfigureAwait(false);
                return DecodeUtf8(response);
            });
        }
    }

    /// <summary>Provides fluent RPC options builder extensions.</summary>
    /// <param name="builder">The RPC options builder.</param>
    extension(MqttRpcClientOptionsBuilder builder)
    {
        /// <summary>Configures an MQTT RPC options builder with a topic generation strategy.</summary>
        /// <param name="strategy">The topic generation strategy.</param>
        /// <returns>The configured builder.</returns>
        public MqttRpcClientOptionsBuilder UseTopicGenerationStrategy(
            IMqttRpcClientTopicGenerationStrategy strategy)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(strategy);
            return builder.WithTopicGenerationStrategy(strategy);
        }
    }

    /// <summary>Creates an empty RPC parameter dictionary.</summary>
    /// <returns>An empty RPC parameter dictionary.</returns>
    public static IDictionary<string, object> EmptyParameters() => new Dictionary<string, object>();

    /// <summary>Decodes bytes as a UTF-8 string.</summary>
    /// <param name="response">The response bytes.</param>
    /// <returns>The decoded string.</returns>
    private static string DecodeUtf8(byte[] response) => Encoding.UTF8.GetString(response);

    /// <summary>Executes an RPC request with timeout and subscription cancellation support.</summary>
    /// <param name="client">The MQTT RPC client.</param>
    /// <param name="timeout">The request timeout.</param>
    /// <param name="methodName">The RPC method name.</param>
    /// <param name="payload">The binary request payload.</param>
    /// <param name="qualityOfServiceLevel">The MQTT quality of service level.</param>
    /// <param name="parameters">The topic generation parameters.</param>
    /// <param name="cancellationToken">The subscription cancellation token.</param>
    /// <returns>A task that produces the binary response payload.</returns>
    private static async Task<byte[]> ExecuteWithTimeoutAsync(
        IMqttRpcClient client,
        TimeSpan timeout,
        string methodName,
        byte[] payload,
        MqttQualityOfServiceLevel qualityOfServiceLevel,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        return await client.ExecuteAsync(
                methodName,
                payload,
                qualityOfServiceLevel,
                parameters,
                timeoutSource.Token)
            .ConfigureAwait(false);
    }

    /// <summary>Wraps a task result as a cold asynchronous observable operation.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The task factory.</param>
    /// <returns>A cold asynchronous observable operation.</returns>
    private static IObservableAsync<T> FromAsyncTask<T>(Func<CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return SignalAsync.Create<T>(async (observer, cancellationToken) =>
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await observer.OnNextAsync(result, cancellationToken).ConfigureAwait(false);
            await observer.OnCompletedAsync(PrimitivesResult.Success).ConfigureAwait(false);
            return DisposableAsync.Empty;
        });
    }
}
