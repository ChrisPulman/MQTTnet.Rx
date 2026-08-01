// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Serial;

namespace MQTTnet.Rx.Client.TestApp;

/// <summary>Demonstrates the ReactiveUI.Primitives serial receive surface.</summary>
internal static class Program
{
    /// <summary>Gets the serial connection bit rate.</summary>
    private const int BaudRate = 9600;

    /// <summary>Gets the finite serial operation timeout in milliseconds.</summary>
    private const int TimeoutMilliseconds = 1000;

    /// <summary>Opens COM1 and writes received characters to standard output.</summary>
    /// <returns>A task representing the application's lifetime.</returns>
    internal static async Task Main()
    {
        using var port = new SerialPortRx("COM1", BaudRate)
        {
            ReadTimeout = TimeoutMilliseconds,
            WriteTimeout = TimeoutMilliseconds,
            EnableAutoDataReceive = true,
        };
        using var errors = port.ErrorReceived.Subscribe(Console.Error.WriteLine);
        using var received = port.DataReceived.Subscribe(Console.Out.Write);

        await port.OpenAsync().ConfigureAwait(false);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(false);
        }
        finally
        {
            port.Close();
        }
    }
}
