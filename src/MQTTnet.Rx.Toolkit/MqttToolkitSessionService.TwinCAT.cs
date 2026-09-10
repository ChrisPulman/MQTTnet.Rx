// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if WINDOWS
using IoT.Driver.TwinCATRx;
#endif
using MQTTnet.Rx.Toolkit.ViewModels;
#if WINDOWS
using MQTTnet.Rx.TwinCAT;
using ReactiveUI.Primitives.Signals;
#endif

namespace MQTTnet.Rx.Toolkit;

/// <summary>Owns the lifetime of the Toolkit's configured PLC structure publisher.</summary>
internal sealed partial class MqttToolkitSessionService
{
    /// <summary>Stores the log source used for structure bridge events.</summary>
    private const string TwinCatSource = "TwinCAT";

    /// <summary>Stores the active structure publication and its owned ADS connection.</summary>
    private IDisposable? _twinCatBridge;

#if WINDOWS
    /// <summary>Gets or sets the ADS factory used by the structure bridge.</summary>
    internal Func<IRxTcAdsClient> TwinCatClientFactory { get; set; } = static () => new RxTcAdsClient();
#endif

    /// <summary>Starts the configured structure publisher against the connected MQTT broker.</summary>
    /// <param name="configuration">The editable structure configuration.</param>
    /// <param name="cancellationToken">Cancels waiting for the session lifecycle gate.</param>
    /// <returns>The asynchronous startup operation.</returns>
    internal async Task StartTwinCatBridgeAsync(TwinCatBridgeViewModel configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ThrowIfDisposed();
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
#if WINDOWS
            var client = RequireClient();
            if (!client.IsConnected)
            {
                throw new InvalidOperationException("Connect to the MQTT broker before subscribing to a PLC structure.");
            }

            var options = configuration.BuildOptions();
            options.ErrorHandler = exception => Log("Error", TwinCatSource, exception.Message);
            options.StructureLinked = _ => Log("Info", TwinCatSource, $"Structure {options.PlcVariable} linked; publishing members under {options.TopicPrefix}.");
            StopTwinCatBridgeCore();
            _twinCatBridge = Signal.Emit(client).PublishTcStructure(options, TwinCatClientFactory);
            Log("Info", TwinCatSource, "Structure subscription started. Waiting for ADS data; connection progress and errors appear in this log.");
#else
            throw new PlatformNotSupportedException("TwinCAT ADS requires the Windows Toolkit build.");
#endif
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Stops publishing structure values and releases the owned ADS connection.</summary>
    /// <param name="cancellationToken">Cancels waiting for the session lifecycle gate.</param>
    /// <returns>The asynchronous stop operation.</returns>
    internal async Task StopTwinCatBridgeAsync(CancellationToken cancellationToken)
    {
        using var operationCancellation = CreateOperationCancellation(cancellationToken);
        await _lifecycleGate.WaitAsync(operationCancellation.Token).ConfigureAwait(false);
        try
        {
            StopTwinCatBridgeCore();
        }
        finally
        {
            _ = _lifecycleGate.Release();
        }
    }

    /// <summary>Stops the bridge while the caller owns the session lifecycle gate.</summary>
    private void StopTwinCatBridgeCore()
    {
        var bridge = _twinCatBridge;
        _twinCatBridge = null;
        bridge?.Dispose();
        if (bridge is not null)
        {
            Log("Info", TwinCatSource, "Structure subscription stopped.");
        }
    }
}
