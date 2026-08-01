// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Serial;
using ReactiveUI.Primitives.Async;
using SerialAsyncCreate = MQTTnet.Rx.SerialPort.ObservableAsyncCreateExtensions;
using SerialCreate = MQTTnet.Rx.SerialPort.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains serial bridge facade validation assertions.</summary>
public sealed partial class SerialPortLiveBridgeTests
{
    /// <summary>Asserts validation for raw observable publisher facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="starts">The valid start delimiter observable.</param>
    /// <param name="ends">The valid end delimiter observable.</param>
    /// <param name="raw">The valid raw client observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertRawPublisherValidationAsync(
        InMemoryPortRxPair pair,
        IObservable<char> starts,
        IObservable<char> ends,
        IObservable<IMqttClient> raw)
    {
        await Assert.That(() => SerialCreate.PublishSerialPort(
                (IObservable<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(raw, " ", pair.First, starts, ends, 1))
            .Throws<ArgumentException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(raw, ValidationTopic, null!, starts, ends, 1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(raw, ValidationTopic, pair.First, null!, ends, 1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(raw, ValidationTopic, pair.First, starts, null!, 1))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for resilient observable publisher facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="starts">The valid start delimiter observable.</param>
    /// <param name="ends">The valid end delimiter observable.</param>
    /// <param name="resilient">The valid resilient client observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertResilientPublisherValidationAsync(
        InMemoryPortRxPair pair,
        IObservable<char> starts,
        IObservable<char> ends,
        IObservable<IResilientMqttClient> resilient)
    {
        await Assert.That(() => SerialCreate.PublishSerialPort(
                (IObservable<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(
                resilient,
                string.Empty,
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                null!,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                pair.First,
                null!,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                pair.First,
                starts,
                null!,
                1))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for raw observable writer facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="raw">The valid raw client observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertRawWriterValidationAsync(InMemoryPortRxPair pair, IObservable<IMqttClient> raw)
    {
        await Assert.That(() => SerialCreate.SubscribeSerialPortWriteLine(
                (IObservable<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                raw,
                " ",
                pair.First,
                static value => value))
            .Throws<ArgumentException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                raw,
                ValidationTopic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                raw,
                ValidationTopic,
                pair.First,
                (Func<string, string>)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for resilient observable writer facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="resilient">The valid resilient client observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertResilientWriterValidationAsync(
        InMemoryPortRxPair pair,
        IObservable<IResilientMqttClient> resilient)
    {
        await Assert.That(() => SerialCreate.SubscribeSerialPortWriteLine(
                (IObservable<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                resilient,
                " ",
                pair.First,
                static value => value))
            .Throws<ArgumentException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                resilient,
                ValidationTopic,
                null!,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialCreate.SubscribeSerialPortWrite(
                resilient,
                ValidationTopic,
                pair.First,
                (Func<string, byte[]>)null!))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for async raw publisher facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="raw">The valid async raw client observable.</param>
    /// <param name="starts">The valid async start delimiter observable.</param>
    /// <param name="ends">The valid async end delimiter observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertAsyncRawPublisherValidationAsync(
        InMemoryPortRxPair pair,
        IObservableAsync<IMqttClient> raw,
        IObservableAsync<char> starts,
        IObservableAsync<char> ends)
    {
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                raw,
                ValidationTopic,
                pair.First,
                null!,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                raw,
                ValidationTopic,
                pair.First,
                starts,
                null!,
                1))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for async resilient publisher facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="resilient">The valid async resilient client observable.</param>
    /// <param name="starts">The valid async start delimiter observable.</param>
    /// <param name="ends">The valid async end delimiter observable.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertAsyncResilientPublisherValidationAsync(
        InMemoryPortRxPair pair,
        IObservableAsync<IResilientMqttClient> resilient,
        IObservableAsync<char> starts,
        IObservableAsync<char> ends)
    {
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                resilient,
                " ",
                pair.First,
                starts,
                ends,
                1))
            .Throws<ArgumentException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                null!,
                starts,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                pair.First,
                null!,
                ends,
                1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.PublishSerialPort(
                resilient,
                ValidationTopic,
                pair.First,
                starts,
                null!,
                1))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts validation for async writer facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertAsyncWriterValidationAsync(InMemoryPortRxPair pair)
    {
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWrite(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWrite(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static _ => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWriteLine(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWrite(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
    }

    /// <summary>Asserts bridge-specific validation for async writer facades.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertAsyncBridgeWriterValidationAsync(InMemoryPortRxPair pair)
    {
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWriteLine(
                (IObservableAsync<IMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static value => value))
            .Throws<ArgumentNullException>();
        await Assert.That(() => SerialAsyncCreate.SubscribeSerialPortWrite(
                (IObservableAsync<IResilientMqttClient>)null!,
                ValidationTopic,
                pair.First,
                static _ => Array.Empty<byte>()))
            .Throws<ArgumentNullException>();
    }
}
