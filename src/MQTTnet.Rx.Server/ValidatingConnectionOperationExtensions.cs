// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Server;
using MQTTnet.Server.EnhancedAuthentication;

#if REACTIVE_SHIM
namespace MQTTnet.Rx.Server.Reactive;
#else
namespace MQTTnet.Rx.Server;
#endif

/// <summary>Provides reactive enhanced-authentication operations during connection validation.</summary>
public static class ValidatingConnectionOperationExtensions
{
    /// <summary>Provides enhanced-authentication operations.</summary>
    /// <param name="eventArgs">The connection-validation event arguments.</param>
    extension(ValidatingConnectionEventArgs eventArgs)
    {
        /// <summary>Exchanges enhanced-authentication data when subscribed.</summary>
        /// <param name="options">The authentication exchange options.</param>
        /// <returns>A cold authentication exchange.</returns>
        public IObservable<ExchangeEnhancedAuthenticationResult> ExchangeEnhancedAuthentication(
            ExchangeEnhancedAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTask<ExchangeEnhancedAuthenticationResult>(cancellationToken =>
                eventArgs.ExchangeEnhancedAuthenticationAsync(options, cancellationToken));
        }

        /// <summary>Exchanges enhanced-authentication data through an asynchronous observable.</summary>
        /// <param name="options">The authentication exchange options.</param>
        /// <returns>A cold asynchronous authentication exchange.</returns>
        public IObservableAsync<ExchangeEnhancedAuthenticationResult> ObserveExchangeEnhancedAuthentication(
            ExchangeEnhancedAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return CreateObservable.FromTaskSignal<ExchangeEnhancedAuthenticationResult>(cancellationToken =>
                eventArgs.ExchangeEnhancedAuthenticationAsync(options, cancellationToken));
        }
    }
}
