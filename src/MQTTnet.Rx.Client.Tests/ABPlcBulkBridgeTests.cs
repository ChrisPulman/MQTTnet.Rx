// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
#if REACTIVE_SHIM
using IoT.Driver.ABPlcRx.Reactive;
#else
using IoT.Driver.ABPlcRx;
#endif
using IoT.Driver.Core;
using MQTTnet.Protocol;
using NSubstitute;
using ReactiveUI.Primitives.Async;
#if REACTIVE_SHIM
using MQTTnet.Rx.ABPlc.Reactive;
#else
using MQTTnet.Rx.ABPlc;
#endif
using MQTTnet.Rx.Client.Tests.Helpers;
#if REACTIVE_SHIM
using Signal = ReactiveUI.Primitives.Reactive.Signals.Signal;
#else
using Signal = ReactiveUI.Primitives.Signals.Signal;
#endif

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Exercises wave-8 industrial adapter bulk MQTT surfaces.</summary>
public sealed class ABPlcBulkBridgeTests
{
    /// <summary>The source error used to verify subscription cleanup.</summary>
    private const string SourceFailureMessage = "source failed";

    /// <summary>The first logical variable name.</summary>
    private const string FirstVariable = "BulkFirst";

    /// <summary>The second logical variable name.</summary>
    private const string SecondVariable = "BulkSecond";

    /// <summary>The first physical simulator tag.</summary>
    private const string FirstPhysicalTag = "Program:Bulk.First";

    /// <summary>The second physical simulator tag.</summary>
    private const string SecondPhysicalTag = "Program:Bulk.Second";

    /// <summary>The AB test group name.</summary>
    private const string Group = "BulkBridge";

    /// <summary>The validation topic.</summary>
    private const string ValidationTopic = "topic";

    /// <summary>The ignored MQTT payload.</summary>
    private const string IgnoredPayload = "ignored";

    /// <summary>The logical Int32 type name.</summary>
    private const string Int32TypeName = "Int32";

    /// <summary>The AB bulk publish topic.</summary>
    private const string ABPlcBulkPublishTopic = "tests/ab/bulk/publish";

    /// <summary>The AB bulk error topic.</summary>
    private const string ABPlcBulkErrorTopic = "tests/ab/bulk/error";

    /// <summary>The AB resilient bulk subscribe topic.</summary>
    private const string ABResilientBulkSubscribeTopic = "tests/ab/resilient/bulk/subscribe";

    /// <summary>The AB resilient logical publish topic.</summary>
    private const string ABResilientLogicalPublishTopic = "tests/ab/resilient/logical/publish";

    /// <summary>The AB async bulk subscribe topic.</summary>
    private const string ABAsyncBulkSubscribeTopic = "tests/ab/async/bulk/subscribe";

    /// <summary>The AB async logical subscribe topic.</summary>
    private const string ABAsyncLogicalSubscribeTopic = "tests/ab/async/logical/subscribe";

    /// <summary>The AB async logical publish topic.</summary>
    private const string ABAsyncLogicalPublishTopic = "tests/ab/async/logical/publish";

    /// <summary>The AB logical subscribe topic.</summary>
    private const string ABLogicalSubscribeTopic = "tests/ab/logical/subscribe";

    /// <summary>The AB callback error topic prefix.</summary>
    private const string ABCallbackErrorTopic = "tests/ab/callback/error";

    /// <summary>The initial first tag value.</summary>
    private const int InitialFirstValue = 11;

    /// <summary>The initial second tag value.</summary>
    private const int InitialSecondValue = 22;

    /// <summary>The written first tag value.</summary>
    private const int WrittenFirstValue = 33;

    /// <summary>The written second tag value.</summary>
    private const int WrittenSecondValue = 44;

    /// <summary>The observed first tag value.</summary>
    private const int ObservedFirstValue = 55;

    /// <summary>The observed second tag value.</summary>
    private const int ObservedSecondValue = 66;

    /// <summary>The maximum duration allowed for asynchronous assertions.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    /// <summary>Publishes Allen-Bradley bulk observe values as one MQTT payload.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABPlcBulkPublisher_UsesObserveManyAndCustomFormatterAsync()
    {
        using var simulator = CreateSimulator();
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);

        using var publisher = clients
            .PublishABPlcTags(
                ABPlcBulkPublishTopic,
                simulator,
                static values => $"{values[FirstVariable]},{values[SecondVariable]}",
                FirstVariable,
                SecondVariable)
            .Subscribe();
        await Task.Yield();
        simulator.SetTagValue(FirstPhysicalTag, ObservedFirstValue);
        simulator.SetTagValue(SecondPhysicalTag, ObservedSecondValue);
        _ = simulator.Read(FirstVariable);
        _ = simulator.Read(SecondVariable);
        await WaitUntilAsync(() => client.PublishedMessages.Count >= 2);

        await Assert.That(client.PublishedMessages[0].Topic).IsEqualTo(ABPlcBulkPublishTopic);
        await Assert.That(client.PublishedMessages[0].ConvertPayloadToString())
            .IsEqualTo("55,22");
        await Assert.That(client.PublishedMessages[1].Topic).IsEqualTo(ABPlcBulkPublishTopic);
        await Assert.That(client.PublishedMessages[1].ConvertPayloadToString())
            .IsEqualTo("55,66");
    }

    /// <summary>Writes an Allen-Bradley bulk MQTT payload through the driver's bulk write API.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABPlcBulkSubscriber_UsesWriteManyAsync()
    {
        using var simulator = CreateSimulator();
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);
        using var subscription = clients.SubscribeABPlcTags(
            "tests/ab/bulk/subscribe",
            simulator,
            static _ => new Dictionary<string, object?>
            {
                [FirstVariable] = WrittenFirstValue,
                [SecondVariable] = WrittenSecondValue,
            });

        await client.SimulateMessageReceivedAsync("tests/ab/bulk/subscribe", IgnoredPayload);
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == WrittenFirstValue
            && simulator.GetTagValue(SecondPhysicalTag, 0) == WrittenSecondValue);

        await Assert.That(simulator.OperationMetrics.WriteOperations).IsGreaterThan(0L);
        await Assert.That(simulator.GetTagValue(FirstPhysicalTag, 0)).IsEqualTo(WrittenFirstValue);
        await Assert.That(simulator.GetTagValue(SecondPhysicalTag, 0)).IsEqualTo(WrittenSecondValue);
    }

    /// <summary>Covers Allen-Bradley default, resilient, and async bulk publish wrappers.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABPlcBulkPublishWrappers_UseObserveManyAsync()
    {
        var values = new Dictionary<string, object?> { [FirstVariable] = 1, [SecondVariable] = null };
        var plc = Substitute.For<IABPlcRx>();
        _ = plc.ObserveMany(FirstVariable, SecondVariable).Returns(Signal.Emit<IReadOnlyDictionary<string, object?>>(values));
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var resilientResult = Signal.Emit<IResilientMqttClient>(resilientClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable, SecondVariable)
            .FirstAsync(Timeout);

        var rawResult = await Signal.Emit<IMqttClient>(rawClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable, SecondVariable)
            .FirstAsync(Timeout);
        await Task.Yield();
        await resilientClient.SimulateApplicationMessageProcessedAsync();
        var processed = await resilientResult;
        _ = SignalAsync.Return<IMqttClient>(rawClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable)
            .ToObservable();
        _ = SignalAsync.Return<IMqttClient>(rawClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, static _ => "async", FirstVariable)
            .ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable)
            .ToObservable();
        _ = SignalAsync.Return<IResilientMqttClient>(resilientClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, plc, static _ => "async", FirstVariable)
            .ToObservable();

        await Assert.That(rawResult.ReasonCode).IsEqualTo(MqttClientPublishReasonCode.Success);
        await Assert.That(processed.Exception).IsNull();
        await Assert.That(rawClient.PublishedMessages[0].ConvertPayloadToString())
            .IsEqualTo("{\"BulkFirst\":1,\"BulkSecond\":null}");
    }

    /// <summary>Covers default publish validation branches for Allen-Bradley publish wrappers.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABDefaultPublishWrappers_ValidateSuccessAndFailureBranches()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);

        using var rawBulkPublisher = Signal.Emit<IMqttClient>(rawClient)
            .PublishABPlcTags(ABPlcBulkPublishTopic, simulator, FirstVariable)
            .Subscribe();
        using var resilientLogicalPublisher = Signal.Emit<IResilientMqttClient>(resilientClient)
            .PublishABLogicalTag(ABResilientLogicalPublishTopic, logicalTags, FirstVariable)
            .Subscribe();
        IObservable<IMqttClient> nullRawClients = null!;
        IObservable<IResilientMqttClient> nullResilientClients = null!;
        await Assert.That(() => Signal.Emit<IMqttClient>(rawClient).PublishABPlcTags(" ", simulator, FirstVariable))
            .Throws<ArgumentException>();
        await Assert.That(() => Signal.Emit<IResilientMqttClient>(resilientClient).PublishABLogicalTag(" ", logicalTags, FirstVariable))
            .Throws<ArgumentException>();
        await Assert.That(() => nullRawClients.PublishABPlcTags(ABPlcBulkPublishTopic, simulator, FirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => nullResilientClients.PublishABLogicalTag(ABResilientLogicalPublishTopic, logicalTags, FirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit<IMqttClient>(rawClient).PublishABPlcTags(
                ABPlcBulkPublishTopic,
                null!,
                FirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit<IMqttClient>(rawClient).PublishABPlcTags(
                ABPlcBulkPublishTopic,
                simulator,
                (string[])null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit<IResilientMqttClient>(resilientClient).PublishABLogicalTag(
                ABResilientLogicalPublishTopic,
                null!,
                FirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => Signal.Emit<IResilientMqttClient>(resilientClient).PublishABLogicalTag(
                ABResilientLogicalPublishTopic,
                logicalTags,
                " "))
            .Throws<ArgumentException>();
    }

    /// <summary>Covers Allen-Bradley logical tag publish and subscribe wrappers.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABLogicalTagBridge_PublishesAndWritesRegisteredTagsAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var client = new MockMqttClient();
        var clients = Signal.Emit<IMqttClient>(client);
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);
        using var publisher = clients.PublishABLogicalTag(ABPlcBulkPublishTopic, logicalTags, FirstVariable).Subscribe();
        using var emptyBulkPublisher = Signal.None<IMqttClient>()
            .PublishABPlcTags(ABPlcBulkPublishTopic, simulator, FirstVariable)
            .Subscribe();
        simulator.SetTagValue(FirstPhysicalTag, ObservedFirstValue);
        _ = simulator.Read(FirstVariable);
        await WaitUntilAsync(() => client.PublishedMessages.Count >= 2);
        using var subscription = clients.SubscribeABLogicalTags(
            ABLogicalSubscribeTopic,
            logicalTags,
            static _ => [new LogicalTagValue(FirstVariable, WrittenFirstValue, DateTimeOffset.UnixEpoch)]);

        await client.SimulateMessageReceivedAsync(ABLogicalSubscribeTopic, IgnoredPayload);
        var logicalObserver = (IObserver<MqttApplicationMessageReceivedEventArgs>)subscription;
        logicalObserver.OnNext(TestDataHelpers.CreateMessageReceivedArgs(ABLogicalSubscribeTopic, IgnoredPayload));
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == WrittenFirstValue);
        _ = Signal.Emit<IResilientMqttClient>(new MockResilientMqttClient())
            .SubscribeABLogicalTags(ValidationTopic, logicalTags, static _ => []);
        _ = SignalAsync.Return<IMqttClient>(client)
            .SubscribeABLogicalTags(ValidationTopic, logicalTags, static _ => []);
        _ = SignalAsync.Return<IResilientMqttClient>(new MockResilientMqttClient())
            .SubscribeABLogicalTags(ValidationTopic, logicalTags, static _ => []);

        await Assert.That(client.PublishedMessages[0].ConvertPayloadToString()).IsEqualTo("11");
        await Assert.That(client.PublishedMessages[1].ConvertPayloadToString()).IsEqualTo("55");
        await Assert.That(simulator.OperationMetrics.WriteOperations).IsGreaterThan(0L);
    }

    /// <summary>Covers resilient Allen-Bradley bulk and logical bridge wrappers.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABResilientWrappers_PublishAndSubscribeThroughMockClientAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var client = new MockResilientMqttClient();
        var clients = Signal.Emit<IResilientMqttClient>(client);
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);
        using var bulkPublisher = clients.PublishABPlcTags(
            ABPlcBulkPublishTopic,
            simulator,
            FirstVariable).Subscribe();
        using var publisher = clients.PublishABLogicalTag(
            ABResilientLogicalPublishTopic,
            logicalTags,
            FirstVariable).Subscribe();
        var processed = clients.PublishABLogicalTag(
            ABResilientLogicalPublishTopic,
            logicalTags,
            FirstVariable,
            static value => $"{value.TagName}:{value.Value}").FirstAsync(Timeout);
        using var subscription = clients.SubscribeABPlcTags(
            ABResilientBulkSubscribeTopic,
            simulator,
            static _ => CreateBulkWriteValues(WrittenFirstValue, WrittenSecondValue));

        await client.SimulateApplicationMessageProcessedAsync();
        await client.SimulateMessageReceivedAsync(ABResilientBulkSubscribeTopic, IgnoredPayload);
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == WrittenFirstValue);
        var result = await processed;

        await Assert.That(result.Exception).IsNull();
        await Assert.That(simulator.GetTagValue(SecondPhysicalTag, 0)).IsEqualTo(WrittenSecondValue);
    }

    /// <summary>Covers asynchronous Allen-Bradley bulk and logical bridge wrappers.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABAsyncWrappers_ForwardEverySubscribeAndLogicalPublishSurfaceAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);
        var rawClients = SignalAsync.Return<IMqttClient>(rawClient);
        var resilientClients = SignalAsync.Return<IResilientMqttClient>(resilientClient);
        using var rawPublisher = rawClients.PublishABLogicalTag(
            ABAsyncLogicalPublishTopic,
            logicalTags,
            FirstVariable).ToObservable().Subscribe();
        _ = rawClients.PublishABLogicalTag(
            ABAsyncLogicalPublishTopic,
            logicalTags,
            FirstVariable,
            static value => Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        using var rawBulk = rawClients.SubscribeABPlcTags(
            ABAsyncBulkSubscribeTopic,
            simulator,
            static _ => CreateBulkWriteValues(WrittenFirstValue, WrittenSecondValue));
        using var rawLogical = rawClients.SubscribeABLogicalTags(
            ABAsyncLogicalSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalWriteValues(WrittenSecondValue));
        _ = resilientClients.PublishABPlcTags(ABPlcBulkPublishTopic, simulator, FirstVariable);
        _ = resilientClients.PublishABLogicalTag(ABAsyncLogicalPublishTopic, logicalTags, FirstVariable);
        _ = resilientClients.PublishABLogicalTag(
            ABAsyncLogicalPublishTopic,
            logicalTags,
            FirstVariable,
            static value => $"{value.TagName}:{value.Value}");
        using var resilientBulk = resilientClients.SubscribeABPlcTags(
            ABAsyncBulkSubscribeTopic,
            simulator,
            static _ => CreateBulkWriteValues(ObservedFirstValue, ObservedSecondValue));
        using var resilientLogical = resilientClients.SubscribeABLogicalTags(
            ABAsyncLogicalSubscribeTopic,
            logicalTags,
            static _ => CreateLogicalWriteValues(ObservedFirstValue));

        var formattedNull = InvokeLogicalFormatter(new(FirstVariable, null, DateTimeOffset.UnixEpoch));
        var formattedValue = InvokeLogicalFormatter(new(FirstVariable, WrittenFirstValue, DateTimeOffset.UnixEpoch));
        _ = simulator.Read(FirstVariable);
        await WaitUntilAsync(() => rawClient.PublishedMessages.Count > 0);
        await ExerciseAsyncWriteSubscriptionsAsync(simulator, rawClient, resilientClient);

        await Assert.That(rawClient.PublishedMessages[0].ConvertPayloadToString()).IsEqualTo("11");
        await Assert.That(formattedNull).IsEmpty();
        await Assert.That(formattedValue).IsEqualTo("33");
    }

    /// <summary>Verifies every Allen-Bradley subscription family surfaces parser failures.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABSubscribeWrappers_ForwardParserFailuresToOnErrorAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var rawClient = new MockMqttClient();
        using var resilientClient = new MockResilientMqttClient();
        var errors = new List<Exception>();
        var throwingCallbackCount = 0;
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);
        using var rawBulk = SignalAsync.Return<IMqttClient>(rawClient).SubscribeABPlcTags(
            $"{ABCallbackErrorTopic}/raw-bulk",
            simulator,
            ThrowBulkPayload,
            errors.Add);
        using var rawLogical = SignalAsync.Return<IMqttClient>(rawClient).SubscribeABLogicalTags(
            $"{ABCallbackErrorTopic}/raw-logical",
            logicalTags,
            ThrowLogicalPayload,
            errors.Add);
        using var resilientBulk = SignalAsync.Return<IResilientMqttClient>(resilientClient).SubscribeABPlcTags(
            $"{ABCallbackErrorTopic}/resilient-bulk",
            simulator,
            ThrowBulkPayload,
            errors.Add);
        using var resilientLogical = SignalAsync.Return<IResilientMqttClient>(resilientClient).SubscribeABLogicalTags(
            $"{ABCallbackErrorTopic}/resilient-logical",
            logicalTags,
            ThrowLogicalPayload,
            errors.Add);
        using var throwingCallback = Signal.Emit<IMqttClient>(rawClient).SubscribeABPlcTags(
            $"{ABCallbackErrorTopic}/throwing-callback",
            simulator,
            ThrowBulkPayload,
            _ =>
            {
                throwingCallbackCount++;
                throw new InvalidOperationException("callback failed");
            });

        await rawClient.SimulateMessageReceivedAsync($"{ABCallbackErrorTopic}/raw-bulk", "bad");
        await rawClient.SimulateMessageReceivedAsync($"{ABCallbackErrorTopic}/raw-logical", "bad");
        await resilientClient.SimulateMessageReceivedAsync($"{ABCallbackErrorTopic}/resilient-bulk", "bad");
        await resilientClient.SimulateMessageReceivedAsync($"{ABCallbackErrorTopic}/resilient-logical", "bad");
        await rawClient.SimulateMessageReceivedAsync($"{ABCallbackErrorTopic}/throwing-callback", "bad");
        await WaitUntilAsync(() => errors.Count == 4 && throwingCallbackCount == 1);

        foreach (var error in errors)
        {
            await Assert.That(error).IsTypeOf<FormatException>();
        }
    }

    /// <summary>Verifies subscriptions without error callbacks still detach after a source failure.</summary>
    /// <returns>The asynchronous assertions.</returns>
    [Test]
    public async Task DefaultSubscriptionsDetachAfterSourceFailureAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        using var client = new MockMqttClient();
        var parsedCount = 0;
        using var bulk = Signal.Emit<IMqttClient>(client).SubscribeABPlcTags(
            ABPlcBulkErrorTopic,
            simulator,
            _ =>
            {
                parsedCount++;
                return new Dictionary<string, object?>();
            });
        using var logical = Signal.Emit<IMqttClient>(client).SubscribeABLogicalTags(
            ABLogicalSubscribeTopic,
            logicalTags,
            _ =>
            {
                parsedCount++;
                return [];
            });
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)bulk).OnError(new InvalidOperationException(SourceFailureMessage));
        ((IObserver<MqttApplicationMessageReceivedEventArgs>)logical).OnError(new InvalidOperationException(SourceFailureMessage));
        await client.SimulateMessageReceivedAsync(ABPlcBulkErrorTopic, IgnoredPayload);
        await client.SimulateMessageReceivedAsync(ABLogicalSubscribeTopic, IgnoredPayload);
        await Assert.That(parsedCount).IsEqualTo(0);
    }

    /// <summary>Covers Allen-Bradley validation and ordered observer lifecycle branches.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABPlcBulkObservers_SurfaceErrorsAndHandleDisposalAsync()
    {
        var plc = Substitute.For<IABPlcRx>();
        var client = new MockMqttClient();
        var releaseWrite = new TaskCompletionSource<IReadOnlyList<PlcTagResult>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var errors = new List<Exception>();
        _ = plc.WriteManyAsync(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(_ => releaseWrite.Task);
        var subscription = Signal.Emit<IMqttClient>(client).SubscribeABPlcTags(
            ABPlcBulkErrorTopic,
            plc,
            static payload => new Dictionary<string, object?> { [FirstVariable] = int.Parse(payload) },
            errors.Add);
        var observer = (IObserver<MqttApplicationMessageReceivedEventArgs>)subscription;

        Exception? invalidDefaultBulkTopicError = null;
        try
        {
            _ = Signal.Emit<IMqttClient>(client).PublishABPlcTags(" ", plc, FirstVariable);
        }
        catch (Exception error)
        {
            invalidDefaultBulkTopicError = error;
        }

        await Assert.That(invalidDefaultBulkTopicError).IsTypeOf<ArgumentException>();
        await Assert.That(() => Signal.Emit<IMqttClient>(client).PublishABPlcTags(
                ABPlcBulkPublishTopic,
                plc,
                static _ => string.Empty))
            .Throws<ArgumentException>();
        await Assert.That(() => Signal.Emit<IMqttClient>(client).PublishABPlcTags(ABPlcBulkPublishTopic, plc))
            .Throws<ArgumentException>();
        IObservable<IMqttClient> missingClients = null!;
        await Assert.That(() => missingClients.PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable))
            .Throws<ArgumentNullException>();
        IObservable<IResilientMqttClient> missingBulkResilientClients = null!;
        Exception? missingBulkResilientError = null;
        try
        {
            _ = missingBulkResilientClients.PublishABPlcTags(ABPlcBulkPublishTopic, plc, FirstVariable);
        }
        catch (Exception error)
        {
            missingBulkResilientError = error;
        }

        await Assert.That(missingBulkResilientError).IsTypeOf<ArgumentNullException>();
        observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(ABPlcBulkErrorTopic, "1"));
        observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(ABPlcBulkErrorTopic, "2"));
        subscription.Dispose();
        observer.OnNext(TestDataHelpers.CreateMessageReceivedArgs(ABPlcBulkErrorTopic, "3"));
        observer.OnError(new InvalidOperationException(SourceFailureMessage));
        InvokeObserverAttach(subscription, new TestDisposable());
        _ = releaseWrite.TrySetResult([]);

        await Task.Yield();
        await Assert.That(errors).Count().IsEqualTo(1);
        await Assert.That(errors[0].Message).IsEqualTo(SourceFailureMessage);
        client.Dispose();
    }

    /// <summary>Verifies logical-tag bridge validation for missing dependencies.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ABLogicalTagBridge_RejectsMissingDependenciesAsync()
    {
        using var simulator = CreateSimulator();
        using var logicalTags = simulator.CreateLogicalTagClient();
        var clients = Signal.None<IMqttClient>();
        _ = logicalTags.CreateTag(FirstVariable, FirstPhysicalTag, Int32TypeName);

        using var emptyLogicalPublisher = Signal.None<IResilientMqttClient>()
            .PublishABLogicalTag(ValidationTopic, logicalTags, FirstVariable)
            .Subscribe();
        IObservable<IMqttClient> missingLogicalClients = null!;
        Exception? missingLogicalError = null;
        try
        {
            _ = missingLogicalClients.PublishABLogicalTag(ValidationTopic, logicalTags, FirstVariable);
        }
        catch (Exception error)
        {
            missingLogicalError = error;
        }

        await Assert.That(missingLogicalError).IsTypeOf<ArgumentNullException>();
        await Assert.That(() => clients.PublishABLogicalTag(ValidationTopic, null!, FirstVariable))
            .Throws<ArgumentNullException>();
        await Assert.That(() => clients.PublishABLogicalTag(ValidationTopic, logicalTags, " "))
            .Throws<ArgumentException>();
        IObservable<IResilientMqttClient> missingResilientClients = null!;
        Exception? invalidDefaultResilientLogicalTopicError = null;
        try
        {
            _ = Signal.None<IResilientMqttClient>().PublishABLogicalTag(" ", logicalTags, FirstVariable);
        }
        catch (Exception error)
        {
            invalidDefaultResilientLogicalTopicError = error;
        }

        await Assert.That(invalidDefaultResilientLogicalTopicError).IsTypeOf<ArgumentException>();
        Exception? missingResilientLogicalError = null;
        try
        {
            _ = missingResilientClients.PublishABLogicalTag(ValidationTopic, logicalTags, FirstVariable);
        }
        catch (Exception error)
        {
            missingResilientLogicalError = error;
        }

        await Assert.That(missingResilientLogicalError).IsTypeOf<ArgumentNullException>();
        await Assert.That(() => clients.PublishABLogicalTag(" ", logicalTags, FirstVariable))
            .Throws<ArgumentException>();
        await Assert.That(() => clients.SubscribeABLogicalTags(
                ValidationTopic,
                logicalTags,
                null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => clients.SubscribeABLogicalTags(
                ValidationTopic,
                logicalTags,
                static _ => [new LogicalTagValue(FirstVariable, 1, DateTimeOffset.UnixEpoch)]))
            .ThrowsNothing();
    }

    /// <summary>Waits for each independent subscription write before starting the next one.</summary>
    /// <param name="simulator">The shared PLC simulator.</param>
    /// <param name="rawClient">The ordinary MQTT client.</param>
    /// <param name="resilientClient">The resilient MQTT client.</param>
    /// <returns>The asynchronous write verification.</returns>
    private static async Task ExerciseAsyncWriteSubscriptionsAsync(
        ABPlcSimulator simulator,
        MockMqttClient rawClient,
        MockResilientMqttClient resilientClient)
    {
        await rawClient.SimulateMessageReceivedAsync(ABAsyncBulkSubscribeTopic, "bulk");
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == WrittenFirstValue
            && simulator.GetTagValue(SecondPhysicalTag, 0) == WrittenSecondValue);
        await rawClient.SimulateMessageReceivedAsync(ABAsyncLogicalSubscribeTopic, "logical");
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == WrittenSecondValue);
        await resilientClient.SimulateMessageReceivedAsync(ABAsyncLogicalSubscribeTopic, "logical");
        await WaitUntilAsync(() => simulator.GetTagValue(FirstPhysicalTag, 0) == ObservedFirstValue);
    }

    /// <summary>Creates a deterministic AB simulator with two registered variables.</summary>
    /// <returns>The configured simulator.</returns>
    private static ABPlcSimulator CreateSimulator()
    {
        var simulator = new ABPlcSimulator(PlcType.LGX);
        simulator.ScanEnabled = false;
        simulator.AddUpdateTagItem(FirstVariable, FirstPhysicalTag, Group, 0);
        simulator.AddUpdateTagItem(SecondVariable, SecondPhysicalTag, Group, 0);
        simulator.SetTagValue(FirstPhysicalTag, InitialFirstValue);
        simulator.SetTagValue(SecondPhysicalTag, InitialSecondValue);
        _ = simulator.Read(FirstVariable);
        _ = simulator.Read(SecondVariable);
        return simulator;
    }

    /// <summary>Creates bulk write values.</summary>
    /// <param name="firstValue">The first value.</param>
    /// <param name="secondValue">The second value.</param>
    /// <returns>The bulk write values.</returns>
    private static Dictionary<string, object?> CreateBulkWriteValues(int firstValue, int secondValue) =>
        new()
        {
            [FirstVariable] = firstValue,
            [SecondVariable] = secondValue,
        };

    /// <summary>Creates logical write values.</summary>
    /// <param name="value">The logical value.</param>
    /// <returns>The logical write values.</returns>
    private static IReadOnlyCollection<LogicalTagValue> CreateLogicalWriteValues(int value) =>
        [new(FirstVariable, value, DateTimeOffset.UnixEpoch)];

    /// <summary>Invokes the default logical formatter for an edge-case logical value.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted payload.</returns>
    private static string InvokeLogicalFormatter(LogicalTagValue value)
    {
        var method = typeof(ABPlcBulkMqttExtensions).GetMethod(
            "FormatLogicalTagValue",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(ABPlcBulkMqttExtensions).FullName, "FormatLogicalTagValue");
        return (string)(method.Invoke(null, [value]) ?? string.Empty);
    }

    /// <summary>Throws for bulk payload parser tests.</summary>
    /// <param name="payload">The ignored payload.</param>
    /// <returns>No value because the method always throws.</returns>
    private static IReadOnlyDictionary<string, object?> ThrowBulkPayload(string payload) =>
        throw new FormatException(payload);

    /// <summary>Throws for logical payload parser tests.</summary>
    /// <param name="payload">The ignored payload.</param>
    /// <returns>No value because the method always throws.</returns>
    private static IReadOnlyCollection<LogicalTagValue> ThrowLogicalPayload(string payload) =>
        throw new FormatException(payload);

    /// <summary>Waits until a simulator condition is true.</summary>
    /// <param name="condition">The condition to await.</param>
    /// <returns>A task representing the wait.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        using var cancellation = new CancellationTokenSource(Timeout);
        while (!condition())
        {
            cancellation.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }

    /// <summary>Invokes the ordered observer attach hook after disposal.</summary>
    /// <param name="subscription">The subscription returned by the bridge.</param>
    /// <param name="disposable">The disposable to attach.</param>
    private static void InvokeObserverAttach(IDisposable subscription, IDisposable disposable)
    {
        var method = subscription.GetType().GetMethod(
            "Attach",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new MissingMethodException(subscription.GetType().FullName, "Attach");
        _ = method.Invoke(subscription, [disposable]);
    }

    /// <summary>Records whether a disposable was disposed.</summary>
    private sealed class TestDisposable : IDisposable
    {
        /// <summary>Gets a value indicating whether this instance has been disposed.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public void Dispose() => IsDisposed = true;
    }
}
