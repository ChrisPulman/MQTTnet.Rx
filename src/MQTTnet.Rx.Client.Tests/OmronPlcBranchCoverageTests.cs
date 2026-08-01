// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using IoT.Driver.Core;
#if REACTIVE_SHIM
using IoT.Driver.OmronPlcRx.Reactive;
#else
using IoT.Driver.OmronPlcRx;
#endif
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif
#if REACTIVE_SHIM
using OmronAsyncCreate = MQTTnet.Rx.OmronPlc.Reactive.ObservableAsyncCreateExtensions;
#else
using OmronAsyncCreate = MQTTnet.Rx.OmronPlc.ObservableAsyncCreateExtensions;
#endif
#if REACTIVE_SHIM
using OmronCreate = MQTTnet.Rx.OmronPlc.Reactive.OmronPlcCreateExtensions;
#else
using OmronCreate = MQTTnet.Rx.OmronPlc.OmronPlcCreateExtensions;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises Omron bridge validation, formatting, and terminal observer branches.</summary>
public sealed class OmronPlcBranchCoverageTests
{
    /// <summary>The valid topic used while isolating dependency validation.</summary>
    private const string Topic = "omron/coverage/validation";

    /// <summary>The logical tag name used by branch tests.</summary>
    private const string TagName = "CoverageTag";

    /// <summary>The decimal value used to prove invariant formatting.</summary>
    private const decimal InvariantFormattingValue = 1234.5M;

    /// <summary>Gets an empty raw-client stream.</summary>
    private static readonly IObservable<IMqttClient> RawClients = Signal.None<IMqttClient>();

    /// <summary>Gets an empty resilient-client stream.</summary>
    private static readonly IObservable<IResilientMqttClient> ResilientClients = Signal.None<IResilientMqttClient>();

    /// <summary>Gets an empty asynchronous raw-client stream.</summary>
    private static readonly IObservableAsync<IMqttClient> AsyncRawClients = SignalAsync.None<IMqttClient>();

    /// <summary>Gets an empty asynchronous resilient-client stream.</summary>
    private static readonly IObservableAsync<IResilientMqttClient> AsyncResilientClients =
        SignalAsync.None<IResilientMqttClient>();

    /// <summary>Exercises every synchronous common-argument guard.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SynchronousHelpers_ValidateClientTopicTagPlcAndPayloadFactoryAsync()
    {
        using var simulator = CreateSimulator();
        var tag = new LogicalTagKey<int>(TagName);

        await Assert.That(() => OmronCreate.PublishOmronPlcTag((IObservable<IMqttClient>)null!, Topic, tag, simulator))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag(RawClients, null!, tag, simulator))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag(RawClients, string.Empty, tag, simulator))
            .Throws<ArgumentException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag(RawClients, " ", tag, simulator))
            .Throws<ArgumentException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag<int>(RawClients, Topic, null!, simulator))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag(RawClients, Topic, tag, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.SubscribeOmronPlcTag(
            RawClients,
            Topic,
            tag,
            simulator,
            null!)).Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.PublishOmronPlcTag(
            (IObservable<IResilientMqttClient>)null!,
            Topic,
            tag,
            simulator)).Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.SubscribeOmronPlcTag(
            ResilientClients,
            Topic,
            tag,
            simulator,
            null!)).Throws<ArgumentNullException>();
        await Assert.That(() => OmronCreate.SubscribeOmronPlcTag(
            (IObservable<IResilientMqttClient>)null!,
            Topic,
            tag,
            simulator,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture))).Throws<ArgumentNullException>();
    }

    /// <summary>Exercises null-client guards and delegated validation in all asynchronous bridge wrappers.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AsyncHelpers_ValidateBothClientKindsAndDelegateArgumentsAsync()
    {
        using var simulator = CreateSimulator();
        var tag = new LogicalTagKey<int>(TagName);

        await Assert.That(() => OmronAsyncCreate.PublishOmronPlcTag(
            (IObservableAsync<IMqttClient>)null!,
            Topic,
            tag,
            simulator)).Throws<ArgumentNullException>();
        await Assert.That(() => OmronAsyncCreate.SubscribeOmronPlcTag(
            (IObservableAsync<IMqttClient>)null!,
            Topic,
            tag,
            simulator,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture))).Throws<ArgumentNullException>();
        await Assert.That(() => OmronAsyncCreate.PublishOmronPlcTag(
            (IObservableAsync<IResilientMqttClient>)null!,
            Topic,
            tag,
            simulator)).Throws<ArgumentNullException>();
        await Assert.That(() => OmronAsyncCreate.SubscribeOmronPlcTag(
            (IObservableAsync<IResilientMqttClient>)null!,
            Topic,
            tag,
            simulator,
            static payload => int.Parse(payload, CultureInfo.InvariantCulture))).Throws<ArgumentNullException>();
        await Assert.That(() => OmronAsyncCreate.PublishOmronPlcTag(
            AsyncRawClients,
            " ",
            tag,
            simulator)).Throws<ArgumentException>();
        await Assert.That(() => OmronAsyncCreate.SubscribeOmronPlcTag(
            AsyncResilientClients,
            Topic,
            tag,
            simulator,
            null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Exercises invariant and null-result payload formatting branches.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task PayloadFormatting_UsesInvariantCultureAndFallsBackForNullConvertibleResultAsync()
    {
        var invariantPayload = InvokePayloadFormatter(InvariantFormattingValue);
        var nullFormatter = DispatchProxy.Create<IConvertible, NullConvertibleProxy>();
        var emptyPayload = InvokePayloadFormatter(nullFormatter);

        await Assert.That(invariantPayload).IsEqualTo("1234.5");
        await Assert.That(emptyPayload).IsEmpty();
    }

    /// <summary>Exercises the write observer's completion and formatted error branches.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task WriteObserver_HandlesCompletionAndWritesFormattedTraceErrorAsync()
    {
        using var simulator = CreateSimulator();
        var observer = CreateWriteObserver(
            simulator,
            new(TagName),
            static payload => int.Parse(payload, CultureInfo.InvariantCulture));
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var listener = new TextWriterTraceListener(output);
        _ = Trace.Listeners.Add(listener);
        try
        {
            observer.OnCompleted();
            observer.OnError(new InvalidOperationException("observer branch failure"));
            Trace.Flush();
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        await Assert.That(simulator.Operations).IsEmpty();
        await Assert.That(output.ToString()).Contains("Omron MQTT subscription failed");
        await Assert.That(output.ToString()).Contains("observer branch failure");
    }

    /// <summary>Creates a simulator with the branch-test tag registered.</summary>
    /// <returns>The configured deterministic simulator.</returns>
    private static OmronPlcSimulator CreateSimulator()
    {
        var simulator = new OmronPlcSimulator();
        simulator.Seed(new(TagName, "D200"), 0);
        return simulator;
    }

    /// <summary>Invokes the private generic payload formatter for one value.</summary>
    /// <typeparam name="T">The value type supplied to the formatter.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatter result.</returns>
    private static string InvokePayloadFormatter<T>(T value)
    {
        var formatter = typeof(OmronCreate).GetMethod("ToPayload", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(typeof(OmronCreate).FullName, "ToPayload");
        var result = formatter.MakeGenericMethod(typeof(T)).Invoke(null, [value]);
        return result is string payload
            ? payload
            : throw new InvalidOperationException("The Omron payload formatter did not return a string.");
    }

    /// <summary>Creates the private write observer through its interface boundary.</summary>
    /// <typeparam name="T">The registered tag type.</typeparam>
    /// <param name="simulator">The simulator that receives writes.</param>
    /// <param name="tag">The typed destination key.</param>
    /// <param name="payloadFactory">The MQTT payload converter.</param>
    /// <returns>The constructed write observer.</returns>
    private static IObserver<MqttApplicationMessageReceivedEventArgs> CreateWriteObserver<T>(
        OmronPlcSimulator simulator,
        LogicalTagKey<T> tag,
        Func<string, T> payloadFactory)
    {
        var genericObserver = typeof(OmronCreate).GetNestedType("OmronWriteObserver`1", BindingFlags.NonPublic)
            ?? throw new MissingMemberException(typeof(OmronCreate).FullName, "OmronWriteObserver<T>");
        var observerType = genericObserver.MakeGenericType(typeof(T));
        var instance = Activator.CreateInstance(
            observerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [simulator, tag, payloadFactory],
            CultureInfo.InvariantCulture);
        return instance is IObserver<MqttApplicationMessageReceivedEventArgs> observer
            ? observer
            : throw new InvalidOperationException("The Omron write observer could not be constructed.");
    }

    /// <summary>Returns null from the conversion contract without null-state suppression.</summary>
    public class NullConvertibleProxy : DispatchProxy
    {
        /// <inheritdoc/>
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            string.Equals(targetMethod?.Name, nameof(IConvertible.ToString), StringComparison.Ordinal)
                ? null
                : throw new NotSupportedException(targetMethod?.Name);
    }
}
