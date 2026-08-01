// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using IoT.Driver.Serial;
using ReactiveUI.Primitives.Async;
using ReactiveUI.Primitives.Reactive.Signals;
using SerialAsyncCreate = MQTTnet.Rx.SerialPort.ObservableAsyncCreateExtensions;
using SerialCreate = MQTTnet.Rx.SerialPort.Create;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Contains serial bridge facade construction and validation tests.</summary>
public sealed partial class SerialPortLiveBridgeTests
{
    /// <summary>Executes all remaining public facades without hardware so overload forwarding stays covered.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RemainingFacades_AreComposableWithoutHardwareAsync()
    {
        using var pair = new InMemoryPortRxPair();
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();
        var rawAsync = raw.ToSignal();
        var resilientAsync = resilient.ToSignal();
        var starts = Signal.Emit('!');
        var ends = Signal.Emit(';');
        var startsAsync = starts.ToSignal();
        var endsAsync = ends.ToSignal();

        var publisherSources = new PublisherFacadeSources
        {
            Raw = raw,
            Resilient = resilient,
            RawAsync = rawAsync,
            ResilientAsync = resilientAsync,
            Starts = starts,
            Ends = ends,
            StartsAsync = startsAsync,
            EndsAsync = endsAsync,
        };

        await AssertPublisherFacadesAsync(pair, publisherSources);
        ExerciseSyncWriterFacades(pair, raw, resilient);
        ExerciseAsyncWriterFacades(pair, rawAsync, resilientAsync);

        await Assert.That(pair.First.IsOpen).IsFalse();
    }

    /// <summary>Verifies synchronous bridge entry points reject every invalid required argument.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SyncBridgeFacades_InvalidArgumentsThrowAsync()
    {
        using var pair = new InMemoryPortRxPair();
        var starts = Signal.Emit('<');
        var ends = Signal.Emit('>');
        var raw = Signal.None<IMqttClient>();
        var resilient = Signal.None<IResilientMqttClient>();

        await AssertRawPublisherValidationAsync(pair, starts, ends, raw);
        await AssertResilientPublisherValidationAsync(pair, starts, ends, resilient);
        await AssertRawWriterValidationAsync(pair, raw);
        await AssertResilientWriterValidationAsync(pair, resilient);
    }

    /// <summary>Verifies async-observable bridge entry points reject every invalid required argument.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncBridgeFacades_InvalidArgumentsThrowAsync()
    {
        using var pair = new InMemoryPortRxPair();
        var rawAsync = Signal.None<IMqttClient>().ToSignal();
        var resilientAsync = Signal.None<IResilientMqttClient>().ToSignal();
        var startsAsync = Signal.Emit('<').ToSignal();
        var endsAsync = Signal.Emit('>').ToSignal();

        await AssertAsyncRawPublisherValidationAsync(pair, rawAsync, startsAsync, endsAsync);
        await AssertAsyncResilientPublisherValidationAsync(pair, resilientAsync, startsAsync, endsAsync);
        await AssertAsyncBridgeWriterValidationAsync(pair);
    }

    /// <summary>Verifies every async writer overload checks its client before forwarding.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncWriterFacades_NullClientsThrowAsync()
    {
        using var pair = new InMemoryPortRxPair();
        await AssertAsyncWriterValidationAsync(pair);
    }

    /// <summary>Verifies both private writer cores retain their defensive null-write guard.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WriterCores_NullWriteDelegateThrowsAsync()
    {
        using var pair = new InMemoryPortRxPair();
        var rawError = InvokeWriteCoreWithNullWrite(Signal.None<IMqttClient>(), pair.First);
        var resilientError = InvokeWriteCoreWithNullWrite(
            Signal.None<IResilientMqttClient>(),
            pair.First);

        await Assert.That(rawError).IsTypeOf<ArgumentNullException>();
        await Assert.That(resilientError).IsTypeOf<ArgumentNullException>();
    }

    /// <summary>Asserts the publisher facade overloads can be composed with empty client sources.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="sources">The composed synchronous and async publisher sources.</param>
    /// <returns>A task that represents the assertions.</returns>
    private static async Task AssertPublisherFacadesAsync(
        InMemoryPortRxPair pair,
        PublisherFacadeSources sources)
    {
        await Assert.That(SerialCreate.PublishSerialPort(
                sources.Raw,
                "serial/facade/raw",
                pair.First,
                sources.Starts,
                sources.Ends,
                FrameTimeoutMilliseconds))
            .IsNotNull();
        await Assert.That(SerialCreate.PublishSerialPort(
                sources.Resilient,
                "serial/facade/resilient",
                pair.First,
                sources.Starts,
                sources.Ends,
                FrameTimeoutMilliseconds))
            .IsNotNull();
        await Assert.That(SerialAsyncCreate.PublishSerialPort(
                sources.RawAsync,
                "serial/facade/raw-async",
                pair.First,
                sources.StartsAsync,
                sources.EndsAsync,
                FrameTimeoutMilliseconds))
            .IsNotNull();
        await Assert.That(SerialAsyncCreate.PublishSerialPort(
                sources.ResilientAsync,
                "serial/facade/resilient-async",
                pair.First,
                sources.StartsAsync,
                sources.EndsAsync,
                FrameTimeoutMilliseconds))
            .IsNotNull();
    }

    /// <summary>Constructs and disposes the synchronous writer facade overloads.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="raw">The raw client observable.</param>
    /// <param name="resilient">The resilient client observable.</param>
    private static void ExerciseSyncWriterFacades(
        InMemoryPortRxPair pair,
        IObservable<IMqttClient> raw,
        IObservable<IResilientMqttClient> resilient)
    {
        using var rawLine = SerialCreate.SubscribeSerialPortWriteLine(
            raw,
            "serial/facade/raw-line",
            pair.First,
            static value => value);
        using var rawText = SerialCreate.SubscribeSerialPortWrite(
            raw,
            "serial/facade/raw-text",
            pair.First,
            static value => value);
        using var rawBytes = SerialCreate.SubscribeSerialPortWrite(
            raw,
            "serial/facade/raw-bytes",
            pair.First,
            static _ => Array.Empty<byte>());
        using var resilientLine = SerialCreate.SubscribeSerialPortWriteLine(
            resilient,
            "serial/facade/resilient-line",
            pair.First,
            static value => value);
        using var resilientText = SerialCreate.SubscribeSerialPortWrite(
            resilient,
            "serial/facade/resilient-text",
            pair.First,
            static value => value);
        using var resilientBytes = SerialCreate.SubscribeSerialPortWrite(
            resilient,
            "serial/facade/resilient-bytes",
            pair.First,
            static _ => Array.Empty<byte>());
    }

    /// <summary>Constructs and disposes the async writer facade overloads.</summary>
    /// <param name="pair">The serial pair supplied to each facade.</param>
    /// <param name="rawAsync">The async raw client observable.</param>
    /// <param name="resilientAsync">The async resilient client observable.</param>
    private static void ExerciseAsyncWriterFacades(
        InMemoryPortRxPair pair,
        IObservableAsync<IMqttClient> rawAsync,
        IObservableAsync<IResilientMqttClient> resilientAsync)
    {
        using var rawAsyncLine = SerialAsyncCreate.SubscribeSerialPortWriteLine(
            rawAsync,
            "serial/facade/raw-async-line",
            pair.First,
            static value => value);
        using var rawAsyncText = SerialAsyncCreate.SubscribeSerialPortWrite(
            rawAsync,
            "serial/facade/raw-async-text",
            pair.First,
            static value => value);
        using var rawAsyncBytes = SerialAsyncCreate.SubscribeSerialPortWrite(
            rawAsync,
            "serial/facade/raw-async-bytes",
            pair.First,
            static _ => Array.Empty<byte>());
        using var resilientAsyncLine = SerialAsyncCreate.SubscribeSerialPortWriteLine(
            resilientAsync,
            "serial/facade/resilient-async-line",
            pair.First,
            static value => value);
        using var resilientAsyncText = SerialAsyncCreate.SubscribeSerialPortWrite(
            resilientAsync,
            "serial/facade/resilient-async-text",
            pair.First,
            static value => value);
        using var resilientAsyncBytes = SerialAsyncCreate.SubscribeSerialPortWrite(
            resilientAsync,
            "serial/facade/resilient-async-bytes",
            pair.First,
            static _ => Array.Empty<byte>());
    }

    /// <summary>Composes the synchronous and async sources accepted by publisher facade overloads.</summary>
    private sealed class PublisherFacadeSources
    {
        public required IObservable<IMqttClient> Raw { get; init; }

        public required IObservable<IResilientMqttClient> Resilient { get; init; }

        public required IObservableAsync<IMqttClient> RawAsync { get; init; }

        public required IObservableAsync<IResilientMqttClient> ResilientAsync { get; init; }

        public required IObservable<char> Starts { get; init; }

        public required IObservable<char> Ends { get; init; }

        public required IObservableAsync<char> StartsAsync { get; init; }

        public required IObservableAsync<char> EndsAsync { get; init; }
    }
}
