// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using MQTTnet.Protocol;

#if REACTIVE_SHIM
using IoT.Driver.TwinCATRx.Core.Reactive;
using ReactiveUI.Primitives.Reactive.Signals;
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.Reactive.TwinCatRxExtensions;
using TwinCatRuntimeExtensions = IoT.Driver.TwinCATRx.Reactive.TwinCatRxExtensions;
#else
using IoT.Driver.TwinCATRx.Core;
using ReactiveUI.Primitives.Signals;
using TwinCatCoreExtensions = IoT.Driver.TwinCATRx.Core.TwinCatRxExtensions;
using TwinCatRuntimeExtensions = IoT.Driver.TwinCATRx.TwinCatRxExtensions;
#endif

#if REACTIVE_SHIM
namespace MQTTnet.Rx.TwinCAT.Reactive;
#else
namespace MQTTnet.Rx.TwinCAT;
#endif

/// <summary>Provides automatic MQTT publication for TwinCAT structure leaves.</summary>
public static class TwinCatStructureBridgeExtensions
{
    /// <summary>Serializes non-scalar TwinCAT member values while preserving public ADS fields.</summary>
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        IncludeFields = true,
    };

    /// <summary>Creates MQTT messages for every current and changed TwinCAT structure member.</summary>
    /// <param name="structure">The linked TwinCAT structure table.</param>
    /// <param name="options">The bridge options.</param>
    /// <returns>The MQTT topic/payload stream.</returns>
    public static IObservable<(string Topic, string Payload)> ObserveTcStructureMessages(
        HashTableRx structure,
        TwinCatStructureOptions options)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(options);
        ValidateLowLevelOptions(options);

        return Signal.Create<(string Topic, string Payload)>(observer =>
        {
            var state = new TwinCatStructurePublicationState(observer);
            var useUpperCase = structure.UseUpperCase;
            var changes = structure.ObserveAll.Subscribe(Witness.Create<(string key, object? value)>(
                change => PublishValueSynchronized(
                    state,
                    options,
                    NormalizeMemberName(change.key, useUpperCase),
                    change.value,
                    force: false)));
            PublishSnapshot(state, structure, options, useUpperCase, force: false);

            var interval = options.RepublishInterval is { } republishInterval
                ? new Timer(_ => PublishSnapshot(state, structure, options, useUpperCase, force: true), null, republishInterval, republishInterval)
                : null;

            return new TwinCatStructureSubscription(state, changes, interval);
        });
    }

    /// <summary>Provides structure publication helpers for MQTT client sequences.</summary>
    /// <param name="client">The MQTT client sequence.</param>
    extension(IObservable<IMqttClient> client)
    {
        /// <summary>Publishes every current and changed TwinCAT structure member through an MQTT client.</summary>
        /// <param name="structure">The linked TwinCAT structure table.</param>
        /// <param name="options">The bridge options.</param>
        /// <returns>The MQTT publish results.</returns>
        public IObservable<MqttClientPublishResult> PublishTcStructure(
            HashTableRx structure,
            TwinCatStructureOptions options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(options);

            var messages = ObserveTcStructureMessages(structure, options);
            return client
                .Select(mqttClient => messages.SelectMany(message =>
                    mqttClient.Publish(CreateApplicationMessage(message, options))))
                .Switch();
        }

        /// <summary>Publishes an owned TwinCAT structure connection through an MQTT client.</summary>
        /// <param name="options">The bridge options.</param>
        /// <param name="adsClientFactory">The ADS client factory.</param>
        /// <returns>A disposable that stops the bridge and disconnects the ADS client.</returns>
        [RequiresUnreferencedCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        [RequiresDynamicCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        public IDisposable PublishTcStructure(
            TwinCatStructureOptions options,
            Func<IRxTcAdsClient> adsClientFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(adsClientFactory);
            ValidateOwnedOptions(options);

            return StartOwnedBridge(
                options,
                adsClientFactory,
                structure => client.PublishTcStructure(structure, options).Subscribe(
                    new ErrorRoutedObserver<MqttClientPublishResult>(options)));
        }
    }

    /// <summary>Provides structure publication helpers for resilient MQTT client sequences.</summary>
    /// <param name="client">The resilient MQTT client sequence.</param>
    extension(IObservable<IResilientMqttClient> client)
    {
        /// <summary>Publishes every current and changed TwinCAT structure member through a resilient MQTT client.</summary>
        /// <param name="structure">The linked TwinCAT structure table.</param>
        /// <param name="options">The bridge options.</param>
        /// <returns>The resilient processed-message results.</returns>
        public IObservable<ApplicationMessageProcessedEventArgs> PublishTcStructure(
            HashTableRx structure,
            TwinCatStructureOptions options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(options);

            var messages = ObserveTcStructureMessages(structure, options);
            return client
                .Select(mqttClient => mqttClient.ApplicationMessageProcessed.Merge(
                    messages.SelectMany(message =>
                        mqttClient.Enqueue(CreateApplicationMessage(message, options))
                            .SelectMany(static _ => Signal.Empty<ApplicationMessageProcessedEventArgs>()))))
                .Switch();
        }

        /// <summary>Publishes an owned TwinCAT structure connection through a resilient MQTT client.</summary>
        /// <param name="options">The bridge options.</param>
        /// <param name="adsClientFactory">The ADS client factory.</param>
        /// <returns>A disposable that stops the bridge and disconnects the ADS client.</returns>
        [RequiresUnreferencedCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        [RequiresDynamicCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        public IDisposable PublishTcStructure(
            TwinCatStructureOptions options,
            Func<IRxTcAdsClient> adsClientFactory)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(adsClientFactory);
            ValidateOwnedOptions(options);

            return StartOwnedBridge(
                options,
                adsClientFactory,
                structure => client.PublishTcStructure(structure, options).Subscribe(
                    new ErrorRoutedObserver<ApplicationMessageProcessedEventArgs>(options)));
        }
    }

    /// <summary>Provides structure publication helpers for asynchronous MQTT client sequences.</summary>
    /// <param name="client">The async MQTT client sequence.</param>
    extension(IObservableAsync<IMqttClient> client)
    {
        /// <summary>Publishes every current and changed TwinCAT structure member through an async MQTT client stream.</summary>
        /// <param name="structure">The linked TwinCAT structure table.</param>
        /// <param name="options">The bridge options.</param>
        /// <returns>The asynchronous MQTT publish results.</returns>
        public IObservableAsync<MqttClientPublishResult> PublishTcStructure(
            HashTableRx structure,
            TwinCatStructureOptions options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(options);

            return client.ToObservable().PublishTcStructure(structure, options).ToMqttAsyncSignal();
        }
    }

    /// <summary>Provides structure publication helpers for asynchronous resilient MQTT client sequences.</summary>
    /// <param name="client">The async resilient MQTT client sequence.</param>
    extension(IObservableAsync<IResilientMqttClient> client)
    {
        /// <summary>Publishes every current and changed TwinCAT structure member through an async resilient MQTT client stream.</summary>
        /// <param name="structure">The linked TwinCAT structure table.</param>
        /// <param name="options">The bridge options.</param>
        /// <returns>The asynchronous resilient processed-message results.</returns>
        public IObservableAsync<ApplicationMessageProcessedEventArgs> PublishTcStructure(
            HashTableRx structure,
            TwinCatStructureOptions options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(structure);
            ArgumentNullException.ThrowIfNull(options);

            return client.ToObservable().PublishTcStructure(structure, options).ToMqttAsyncSignal();
        }
    }

    /// <summary>Determines whether a TwinCAT structure key refers to a requested member.</summary>
    /// <param name="key">The key emitted by the structure table.</param>
    /// <param name="memberName">The member name to match.</param>
    /// <returns><c>true</c> when the key refers to the member.</returns>
    public static bool IsTcStructureMember(string key, string memberName)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(memberName);

        return string.Equals(key, memberName, StringComparison.OrdinalIgnoreCase) ||
            (key.Length > memberName.Length &&
                key[key.Length - memberName.Length - 1] == '.' &&
                key.EndsWith(memberName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Builds an MQTT message from a structure member using the configured delivery settings.</summary>
    /// <param name="message">The member topic and payload.</param>
    /// <param name="options">The publication settings.</param>
    /// <returns>The MQTT application message.</returns>
    private static MqttApplicationMessage CreateApplicationMessage(
        (string Topic, string Payload) message,
        TwinCatStructureOptions options) =>
        new MqttApplicationMessageBuilder()
            .WithTopic(message.Topic)
            .WithPayload(message.Payload)
            .WithQualityOfServiceLevel(options.QualityOfService)
            .WithRetainFlag(options.Retain)
            .Build();

    /// <summary>Publishes all currently known table values.</summary>
    /// <param name="state">The publication state.</param>
    /// <param name="structure">The source table.</param>
    /// <param name="options">The bridge options.</param>
    /// <param name="useUpperCase">A value indicating whether member paths are normalized to upper case.</param>
    /// <param name="force">A value indicating whether unchanged values should be republished.</param>
    private static void PublishSnapshot(
        TwinCatStructurePublicationState state,
        HashTableRx structure,
        TwinCatStructureOptions options,
        bool useUpperCase,
        bool force)
    {
        var visited = new HashSet<HashTableRx>(ReferenceEqualityComparer.Instance);
        PublishSnapshot(state, structure, options, parentPath: null, useUpperCase, force, visited);
    }

    /// <summary>Publishes all currently known table values under one table branch.</summary>
    /// <param name="state">The publication state.</param>
    /// <param name="structure">The source table.</param>
    /// <param name="options">The bridge options.</param>
    /// <param name="parentPath">The parent dotted path, if this table is nested.</param>
    /// <param name="useUpperCase">A value indicating whether member paths are normalized to upper case.</param>
    /// <param name="force">A value indicating whether unchanged values should be republished.</param>
    /// <param name="visited">The already visited table instances.</param>
    private static void PublishSnapshot(
        TwinCatStructurePublicationState state,
        HashTableRx structure,
        TwinCatStructureOptions options,
        string? parentPath,
        bool useUpperCase,
        bool force,
        HashSet<HashTableRx> visited)
    {
        if (!visited.Add(structure))
        {
            return;
        }

        foreach (var key in structure.Keys)
        {
            var memberName = CreateSnapshotMemberName(parentPath, key, useUpperCase);

            // Reflected keys retain their original casing even when dotted-path lookups normalize to uppercase.
            var value = structure[(object)key];
            if (value is HashTableRx child)
            {
                PublishSnapshot(state, child, options, memberName, useUpperCase, force, visited);
                continue;
            }

            PublishValueSynchronized(state, options, memberName, value, force);
        }
    }

    /// <summary>Creates a normalized snapshot member path from a parent path and current key.</summary>
    /// <param name="parentPath">The parent dotted path, if this table is nested.</param>
    /// <param name="key">The current table key.</param>
    /// <param name="useUpperCase">A value indicating whether member paths are normalized to upper case.</param>
    /// <returns>The normalized member path.</returns>
    private static string CreateSnapshotMemberName(string? parentPath, string key, bool useUpperCase)
    {
        var memberName = string.IsNullOrWhiteSpace(parentPath) || key.Contains('.', StringComparison.Ordinal)
            ? key
            : $"{parentPath}.{key}";
        return NormalizeMemberName(memberName, useUpperCase);
    }

    /// <summary>Normalizes member paths to the root table casing mode.</summary>
    /// <param name="memberName">The member path.</param>
    /// <param name="useUpperCase">A value indicating whether paths are normalized to upper case.</param>
    /// <returns>The normalized member path.</returns>
    private static string NormalizeMemberName(string memberName, bool useUpperCase) =>
        useUpperCase ? memberName.ToUpperInvariant() : memberName;

    /// <summary>Publishes one value under a serialized observer lock.</summary>
    /// <param name="state">The publication state.</param>
    /// <param name="options">The bridge options.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="value">The member value.</param>
    /// <param name="force">A value indicating whether unchanged values should be republished.</param>
    private static void PublishValueSynchronized(
        TwinCatStructurePublicationState state,
        TwinCatStructureOptions options,
        string memberName,
        object? value,
        bool force)
    {
        state.Synchronize(() =>
        {
            try
            {
                PublishValue(state, options, memberName, value, force);
            }
            catch (Exception error)
            {
                state.TryError(error);
            }
        });
    }

    /// <summary>Publishes one value when it is allowed by the bridge options.</summary>
    /// <param name="state">The publication state.</param>
    /// <param name="options">The bridge options.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="value">The member value.</param>
    /// <param name="force">A value indicating whether unchanged values should be republished.</param>
    private static void PublishValue(
        TwinCatStructurePublicationState state,
        TwinCatStructureOptions options,
        string memberName,
        object? value,
        bool force)
    {
        if (value is IHashTableRx || ShouldSkipMember(options, memberName))
        {
            return;
        }

        var topic = CreateTopic(options, memberName);
        var model = new TwinCatStructureValue(memberName, topic, value);
        var payload = FormatPayload(options, model);
        state.TryPublish(memberName, topic, payload, force);
    }

    /// <summary>Determines whether a member is rejected by the optional filter.</summary>
    /// <param name="options">The bridge options.</param>
    /// <param name="memberName">The member name.</param>
    /// <returns><c>true</c> when the member should not be published.</returns>
    private static bool ShouldSkipMember(TwinCatStructureOptions options, string memberName) =>
        options.MemberFilter is { } filter && !filter(memberName);

    /// <summary>Creates the MQTT topic for a structure member.</summary>
    /// <param name="options">The bridge options.</param>
    /// <param name="memberName">The member name.</param>
    /// <returns>The MQTT topic.</returns>
    private static string CreateTopic(TwinCatStructureOptions options, string memberName) =>
        options.TopicFactory is { } factory
            ? factory(options.TopicPrefix, memberName)
            : $"{options.TopicPrefix.TrimEnd('/')}/{TrimMemberPrefix(options.PlcVariable, memberName).Replace('.', '/')}";

    /// <summary>Removes the configured PLC variable prefix from a structure member path.</summary>
    /// <param name="plcVariable">The PLC structure symbol.</param>
    /// <param name="memberName">The member path emitted by the structure table.</param>
    /// <returns>The MQTT leaf path.</returns>
    private static string TrimMemberPrefix(string plcVariable, string memberName)
    {
        var trimmed = memberName.TrimStart('.');
        var variable = plcVariable.Trim('.');
        return variable.Length > 0 && trimmed.StartsWith($"{variable}.", StringComparison.OrdinalIgnoreCase)
            ? trimmed[(variable.Length + 1)..]
            : trimmed;
    }

    /// <summary>Formats one MQTT payload using either caller options or default scalar/JSON formatting.</summary>
    /// <param name="options">The bridge options.</param>
    /// <param name="value">The structure value.</param>
    /// <returns>The MQTT payload.</returns>
    private static string FormatPayload(TwinCatStructureOptions options, TwinCatStructureValue value) =>
        options.PayloadFormatter is { } formatter ? formatter(value) : FormatPayload(value.Value);

    /// <summary>Formats one MQTT payload using default scalar/JSON formatting.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The MQTT payload.</returns>
    private static string FormatPayload(object? value) =>
        value switch
        {
            null => "null",
            string text => text,
            bool boolean => boolean.ToString(),
            char character => character.ToString(),
            IFormattable formattable when value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal =>
                formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => JsonSerializer.Serialize(value, PayloadSerializerOptions),
        };

    /// <summary>Starts an owned ADS bridge and returns its lifetime handle.</summary>
    /// <param name="options">The bridge options.</param>
    /// <param name="adsClientFactory">The ADS client factory.</param>
    /// <param name="publishFactory">The MQTT publication factory.</param>
    /// <returns>The owned bridge lifetime.</returns>
    [RequiresUnreferencedCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
    [RequiresDynamicCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
    private static TwinCatStructureOwnedBridge StartOwnedBridge(
        TwinCatStructureOptions options,
        Func<IRxTcAdsClient> adsClientFactory,
        Func<HashTableRx, IDisposable> publishFactory)
    {
        var bridge = new TwinCatStructureOwnedBridge(options, adsClientFactory, publishFactory);
        bridge.Start();
        return bridge;
    }

    /// <summary>Validates options required by table-only structure publication.</summary>
    /// <param name="options">The bridge options.</param>
    private static void ValidateLowLevelOptions(TwinCatStructureOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.TopicPrefix);
        if (options.RepublishInterval is { } interval && interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), interval, "Republish interval must be positive.");
        }
    }

    /// <summary>Validates options required by owned ADS structure publication.</summary>
    /// <param name="options">The bridge options.</param>
    private static void ValidateOwnedOptions(TwinCatStructureOptions options)
    {
        ValidateLowLevelOptions(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AmsNetId);
        if (options.AdsPort <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.AdsPort, "ADS port must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.PlcVariable);
    }

    /// <summary>Tracks per-subscription state for serialized structure publication.</summary>
    private sealed class TwinCatStructurePublicationState
    {
        /// <summary>The observer receiving MQTT messages.</summary>
        private readonly IObserver<(string Topic, string Payload)> _observer;

#if NET9_0_OR_GREATER
        /// <summary>Serializes observer and deduplication access.</summary>
        private readonly Lock _gate = new();
#else
        /// <summary>Serializes observer and deduplication access.</summary>
        private readonly object _gate = new();
#endif

        /// <summary>Stores the last payload published for each structure member and MQTT topic.</summary>
        private readonly Dictionary<(string MemberName, string Topic), string> _lastPayloads = [];

        /// <summary>Tracks whether this state has reached a terminal condition.</summary>
        private bool _terminated;

        /// <summary>Initializes a new instance of the <see cref="TwinCatStructurePublicationState"/> class.</summary>
        /// <param name="observer">The observer receiving MQTT messages.</param>
        public TwinCatStructurePublicationState(IObserver<(string Topic, string Payload)> observer) =>
            _observer = observer;

        /// <summary>Runs one publication action under the state lock.</summary>
        /// <param name="action">The action to run.</param>
        public void Synchronize(Action action)
        {
            lock (_gate)
            {
                if (_terminated)
                {
                    return;
                }

                action();
            }
        }

        /// <summary>Publishes a topic/payload pair when allowed by the terminal and deduplication state.</summary>
        /// <param name="memberName">The structure member name.</param>
        /// <param name="topic">The MQTT topic.</param>
        /// <param name="payload">The MQTT payload.</param>
        /// <param name="force">A value indicating whether unchanged values should be republished.</param>
        public void TryPublish(string memberName, string topic, string payload, bool force)
        {
            var publicationKey = (memberName, topic);
            if (!force &&
                _lastPayloads.TryGetValue(publicationKey, out var lastPayload) &&
                string.Equals(lastPayload, payload, StringComparison.Ordinal))
            {
                return;
            }

            _lastPayloads[publicationKey] = payload;
            _observer.OnNext((topic, payload));
        }

        /// <summary>Signals an error once and prevents later publications.</summary>
        /// <param name="error">The terminal error.</param>
        public void TryError(Exception error)
        {
            if (_terminated)
            {
                return;
            }

            _terminated = true;
            _observer.OnError(error);
        }

        /// <summary>Prevents any later publications.</summary>
        public void Terminate()
        {
            lock (_gate)
            {
                _terminated = true;
            }
        }
    }

    /// <summary>Owns a structure-message subscription and optional snapshot republish timer.</summary>
    /// <param name="state">The terminal publication state.</param>
    /// <param name="changes">The change subscription.</param>
    /// <param name="interval">The optional timer.</param>
    private sealed class TwinCatStructureSubscription(TwinCatStructurePublicationState state, IDisposable changes, Timer? interval) : IDisposable
    {
        /// <summary>Tracks whether this subscription has been disposed.</summary>
        private int _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            state.Terminate();
            interval?.Dispose();
            changes.Dispose();
        }
    }

    /// <summary>Owns the ADS client, linked structure table, and MQTT publication lifetime.</summary>
    private sealed class TwinCatStructureOwnedBridge : IDisposable
    {
        /// <summary>The ADS client cancellation source.</summary>
        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>The cancellation token captured before the source can be disposed.</summary>
        private readonly CancellationToken _cancellationToken;

#if NET9_0_OR_GREATER
        /// <summary>Serializes lifetime state changes.</summary>
        private readonly Lock _gate = new();
#else
        /// <summary>Serializes lifetime state changes.</summary>
        private readonly object _gate = new();
#endif

        /// <summary>The bridge options.</summary>
        private readonly TwinCatStructureOptions _options;

        /// <summary>The ADS client factory.</summary>
        private readonly Func<IRxTcAdsClient> _adsClientFactory;

        /// <summary>The MQTT publication factory.</summary>
        private readonly Func<HashTableRx, IDisposable> _publishFactory;

        /// <summary>The ADS error subscription.</summary>
        private IDisposable? _adsErrorSubscription;

        /// <summary>The structure-ready subscription.</summary>
        private IDisposable? _structureReadySubscription;

        /// <summary>The MQTT publication subscription.</summary>
        private IDisposable? _publication;

        /// <summary>The owned ADS client.</summary>
        private IRxTcAdsClient? _adsClient;

        /// <summary>The linked TwinCAT structure table.</summary>
        private HashTableRx? _structure;

        /// <summary>Tracks whether a synchronous ADS connect call is active.</summary>
        private int _connecting;

        /// <summary>Tracks whether this bridge has been disposed.</summary>
        private int _disposed;

        /// <summary>Initializes a new instance of the <see cref="TwinCatStructureOwnedBridge"/> class.</summary>
        /// <param name="options">The bridge options.</param>
        /// <param name="adsClientFactory">The ADS client factory.</param>
        /// <param name="publishFactory">The MQTT publication factory.</param>
        public TwinCatStructureOwnedBridge(
            TwinCatStructureOptions options,
            Func<IRxTcAdsClient> adsClientFactory,
            Func<HashTableRx, IDisposable> publishFactory)
        {
            _options = options;
            _adsClientFactory = adsClientFactory;
            _publishFactory = publishFactory;
            _cancellationToken = _cancellation.Token;
        }

        /// <summary>Starts ADS setup on a background thread.</summary>
        [RequiresUnreferencedCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        [RequiresDynamicCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        public void Start() =>
            _ = Task.Run(ConnectAndPublish, CancellationToken.None);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cancellation.Cancel();
            DisposeCurrentState();
            _cancellation.Dispose();
        }

        /// <summary>Creates the TwinCAT settings used by the owned ADS client.</summary>
        /// <returns>The TwinCAT settings.</returns>
        private Settings CreateSettings() =>
            new()
            {
                AdsAddress = _options.AmsNetId,
                Port = _options.AdsPort,
                SettingsId = _options.PlcVariable,
            };

        /// <summary>Connects ADS, links the structure, and starts MQTT publication.</summary>
        [RequiresUnreferencedCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        [RequiresDynamicCode("Connect/CreateStruct use TwinCAT runtime reflection and dynamic code generation.")]
        private void ConnectAndPublish()
        {
            IRxTcAdsClient? adsClient = null;
            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                adsClient = CreateAndConnectAdsClient();
                _cancellationToken.ThrowIfCancellationRequested();
                LinkStructure(adsClient);
            }
            catch (OperationCanceledException)
            {
                adsClient?.Dispose();
            }
            catch (Exception error)
            {
                _options.ErrorHandler?.Invoke(error);
            }
        }

        /// <summary>Creates, subscribes, configures, stores, and connects the ADS client.</summary>
        /// <returns>The connected ADS client.</returns>
        private IRxTcAdsClient CreateAndConnectAdsClient()
        {
            var adsClient = _adsClientFactory();
            var errorSubscription = adsClient.ErrorReceived.Subscribe(
                Witness.Create<Exception>(error => _options.ErrorHandler?.Invoke(error)));
            var settings = CreateSettings();
            TwinCatCoreExtensions.AddNotification(settings, _options.PlcVariable);
            StoreAdsClient(adsClient, errorSubscription);
            _ = Interlocked.Exchange(ref _connecting, 1);
            try
            {
                adsClient.Connect(settings);
            }
            finally
            {
                _ = Interlocked.Exchange(ref _connecting, 0);
            }

            return adsClient;
        }

        /// <summary>Stores the ADS client unless disposal has already been requested.</summary>
        /// <param name="adsClient">The ADS client.</param>
        /// <param name="errorSubscription">The ADS error subscription.</param>
        private void StoreAdsClient(IRxTcAdsClient adsClient, IDisposable errorSubscription)
        {
            lock (_gate)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    errorSubscription.Dispose();
                    adsClient.Dispose();
                    throw new OperationCanceledException(_cancellationToken);
                }

                _adsClient = adsClient;
                _adsErrorSubscription = errorSubscription;
            }
        }

        /// <summary>Creates the structure table and requests an explicit initial ADS read.</summary>
        /// <param name="adsClient">The connected ADS client.</param>
        [RequiresUnreferencedCode("CreateStruct uses TwinCAT runtime reflection and dynamic code generation.")]
        [RequiresDynamicCode("CreateStruct uses TwinCAT runtime reflection and dynamic code generation.")]
        private void LinkStructure(IRxTcAdsClient adsClient)
        {
            var structure = TwinCatRuntimeExtensions.CreateStruct(adsClient, _options.PlcVariable)
                ?? throw new InvalidOperationException($"TwinCAT structure '{_options.PlcVariable}' could not be created.");
            var ready = TwinCatRuntimeExtensions.StructureReady(structure).Take(1).Subscribe(
                Witness.Create<HashTableRx>(PublishLinkedStructure));

            if (!StoreStructure(structure, ready))
            {
                return;
            }

            adsClient.Read(_options.PlcVariable);
        }

        /// <summary>Stores the linked structure resources unless disposal has already been requested.</summary>
        /// <param name="structure">The structure table.</param>
        /// <param name="ready">The readiness subscription.</param>
        /// <returns><c>true</c> when the structure was stored.</returns>
        private bool StoreStructure(HashTableRx structure, IDisposable ready)
        {
            lock (_gate)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    ready.Dispose();
                    structure.Dispose();
                    return false;
                }

                _structure = structure;
                _structureReadySubscription = ready;
                return true;
            }
        }

        /// <summary>Starts MQTT publication once the TwinCAT structure table has linked.</summary>
        /// <param name="linked">The linked structure table.</param>
        private void PublishLinkedStructure(HashTableRx linked)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            _options.StructureLinked?.Invoke(linked);
            var publication = _publishFactory(linked);
            lock (_gate)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    publication.Dispose();
                    return;
                }

                _publication = publication;
            }
        }

        /// <summary>Disposes all currently attached owned resources.</summary>
        private void DisposeCurrentState()
        {
            IDisposable? publication;
            IDisposable? readiness;
            IDisposable? errors;
            HashTableRx? structure;
            IRxTcAdsClient? adsClient;
            lock (_gate)
            {
                publication = _publication;
                readiness = _structureReadySubscription;
                errors = _adsErrorSubscription;
                structure = _structure;
                adsClient = _adsClient;
            }

            // Subscription disposal may wait for callbacks which must be free to acquire the bridge lock.
            publication?.Dispose();
            readiness?.Dispose();
            errors?.Dispose();
            structure?.Dispose();
            if (Volatile.Read(ref _connecting) != 0)
            {
                return;
            }

            adsClient?.Disconnect();
            adsClient?.Dispose();
        }
    }

    /// <summary>Routes owned publication errors to the bridge error handler.</summary>
    /// <typeparam name="T">The ignored publication result type.</typeparam>
    /// <param name="options">The bridge options.</param>
    private sealed class ErrorRoutedObserver<T>(TwinCatStructureOptions options) : IObserver<T>
    {
        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => options.ErrorHandler?.Invoke(error);

        /// <inheritdoc/>
        public void OnNext(T value)
        {
        }
    }
}
