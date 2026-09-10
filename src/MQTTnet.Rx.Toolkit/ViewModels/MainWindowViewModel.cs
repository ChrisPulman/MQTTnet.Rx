// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Threading;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Coordinates MQTT Toolkit state, commands, topic discovery, messages, logs, and dashboard tiles.</summary>
internal sealed partial class MainWindowViewModel : ViewModelBase, IDisposable, IAsyncDisposable
{
    /// <summary>Stores the dashboard log source label.</summary>
    private const string DashboardSource = "Dashboard";

    /// <summary>Stores the error log level.</summary>
    private const string ErrorLevel = "Error";

    /// <summary>Stores the maximum retained log entries.</summary>
    private const int MaximumLogEntries = 800;

    /// <summary>Stores the maximum retained messages and issues.</summary>
    private const int MaximumMessages = 500;

    /// <summary>Stores the default MQTT operation timeout in seconds.</summary>
    private const int OperationTimeoutSeconds = 30;

    /// <summary>Stores the publish log source label.</summary>
    private const string PublishSource = "Publish";

    /// <summary>Stores the warning log level.</summary>
    private const string WarningLevel = "Warning";

    /// <summary>Stores JSON serialization settings for dashboard layout persistence.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Stores dashboard tiles keyed by exact MQTT topic.</summary>
    private readonly Dictionary<string, DashboardTileViewModel> _dashboardByTopic = new(StringComparer.Ordinal);

    /// <summary>Stores the dashboard layout persistence path.</summary>
    private readonly string _dashboardLayoutPath;

    /// <summary>Posts state updates to the UI thread.</summary>
    private readonly Action<Action> _postToUi;

    /// <summary>Stores the MQTT Toolkit session service.</summary>
    private readonly MqttToolkitSessionService _session;

    /// <summary>Provides deterministic timestamps for logs created by this view model.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>Tracks whether a command operation is already running.</summary>
    private int _operationRunning;

    /// <summary>Tracks whether this view model has been disposed.</summary>
    private bool _disposed;

    /// <summary>Stores the editable MQTT connection options.</summary>
    [Reactive]
    private ConnectionOptionsViewModel _connection = new();

    /// <summary>Stores the command busy state.</summary>
    [Reactive]
    private bool _isBusy;

    /// <summary>Stores the MQTT connection state.</summary>
    [Reactive]
    private bool _isConnected;

    /// <summary>Stores the current message search text.</summary>
    [Reactive]
    private string _messageSearch = string.Empty;

    /// <summary>Stores the editable MQTT publish message options.</summary>
    [Reactive]
    private PublishMessageViewModel _publisher = new();

    /// <summary>Stores the selected dashboard tile.</summary>
    [Reactive]
    private DashboardTileViewModel? _selectedDashboardTile;

    /// <summary>Stores the selected received MQTT message.</summary>
    private ReceivedMqttMessage? _selectedMessage;

    /// <summary>Stores the selected or manually entered MQTT topic.</summary>
    [Reactive]
    private string _selectedTopic = string.Empty;

    /// <summary>Stores the selected topic tree node.</summary>
    private TopicNodeViewModel? _selectedTopicNode;

    /// <summary>Stores the current status text.</summary>
    [Reactive]
    private string _status = "Disconnected";

    /// <summary>Stores the editable MQTT subscription options.</summary>
    [Reactive]
    private SubscriptionViewModel _subscription = new();

    /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel"/> class.</summary>
    /// <param name="session">The MQTT Toolkit session service.</param>
    public MainWindowViewModel(MqttToolkitSessionService session)
        : this(session, TimeProvider.System, PostToAvaloniaThread, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MainWindowViewModel"/> class.</summary>
    /// <param name="session">The MQTT Toolkit session service.</param>
    /// <param name="timeProvider">The timestamp provider used for deterministic logs.</param>
    /// <param name="postToUi">The callback used to marshal state updates.</param>
    /// <param name="dashboardLayoutPath">The optional dashboard layout persistence path.</param>
    internal MainWindowViewModel(
        MqttToolkitSessionService session,
        TimeProvider timeProvider,
        Action<Action> postToUi,
        string? dashboardLayoutPath)
    {
        _session = session;
        _timeProvider = timeProvider;
        _postToUi = postToUi;
        _dashboardLayoutPath = dashboardLayoutPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MQTTnet.Rx.Toolkit",
            "dashboard-layout.json");
        _session.MessageReceived += OnMessageReceived;
        _session.TopicIssueDetected += OnTopicIssueDetected;
        _session.LogReceived += OnLogReceived;
        _session.ConnectionChanged += OnConnectionChanged;
        LoadDashboardLayout();
        LogEntries.Add(new(
            _timeProvider.GetLocalNow(),
            "Info",
            "Toolkit",
            "Ready. Topic discovery is based on observed traffic and broker subscription activity; MQTT does not expose a complete topic catalog."));
    }

    /// <summary>Gets dynamic dashboard tiles.</summary>
    public ObservableCollection<DashboardTileViewModel> DashboardTiles { get; } = [];

    /// <summary>Gets retained Toolkit log entries.</summary>
    public ObservableCollection<MqttLogEntry> LogEntries { get; } = [];

    /// <summary>Gets retained received MQTT messages.</summary>
    public ObservableCollection<ReceivedMqttMessage> Messages { get; } = [];

    /// <summary>Gets quality of service values for UI selectors.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

    /// <summary>Gets or sets the selected received MQTT message.</summary>
    public ReceivedMqttMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            var changed = !EqualityComparer<ReceivedMqttMessage?>.Default.Equals(_selectedMessage, value);
            _ = this.RaiseAndSetIfChanged(ref _selectedMessage, value);
            if (changed && value is not null)
            {
                SelectedTopicNode = null;
            }
        }
    }

    /// <summary>Gets or sets the selected topic tree node.</summary>
    public TopicNodeViewModel? SelectedTopicNode
    {
        get => _selectedTopicNode;
        set
        {
            var changed = !EqualityComparer<TopicNodeViewModel?>.Default.Equals(_selectedTopicNode, value);
            _ = this.RaiseAndSetIfChanged(ref _selectedTopicNode, value);
            if (changed && value is not null)
            {
                SelectedMessage = null;
            }
        }
    }

    /// <summary>Gets the observed MQTT topic tree.</summary>
    public ObservableCollection<TopicNodeViewModel> Topics { get; } =
    [
        new("topics", string.Empty),
    ];

    /// <summary>Gets retained topic and payload issues.</summary>
    public ObservableCollection<TopicIssue> TopicIssues { get; } = [];

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.MessageReceived -= OnMessageReceived;
        _session.TopicIssueDetected -= OnTopicIssueDetected;
        _session.LogReceived -= OnLogReceived;
        _session.ConnectionChanged -= OnConnectionChanged;
        _dashboardByTopic.Clear();
        DashboardTiles.Clear();
        try
        {
            await _session.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _connection.Dispose();
        }
    }

    /// <summary>Posts an action to the Avalonia UI thread.</summary>
    /// <param name="action">The action to run.</param>
    private static void PostToAvaloniaThread(Action action) => Dispatcher.UIThread.Post(action);

    /// <summary>Trims an observable collection to a maximum retained size.</summary>
    /// <typeparam name="T">The collection item type.</typeparam>
    /// <param name="collection">The collection to trim.</param>
    /// <param name="maximum">The maximum number of retained items.</param>
    private static void Trim<T>(ObservableCollection<T> collection, int maximum)
    {
        while (collection.Count > maximum)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }

    /// <summary>Adds an editable MQTT connection user property row.</summary>
    [ReactiveCommand]
    private void AddConnectionUserProperty() => Connection.UserProperties.Add(new());

    /// <summary>Adds an editable MQTT publish user property row.</summary>
    [ReactiveCommand]
    private void AddPublishUserProperty() => Publisher.UserProperties.Add(new());

    /// <summary>Adds a scripted enhanced-authentication exchange step.</summary>
    [ReactiveCommand]
    private void AddEnhancedAuthenticationStep() =>
        Connection.EnhancedAuthenticationSteps.Add(CreateEnhancedAuthenticationStep());

    /// <summary>Sends an enhanced-authentication step during an active exchange.</summary>
    /// <returns>A task representing the asynchronous send operation.</returns>
    [ReactiveCommand]
    private async Task SendEnhancedAuthenticationExchange()
    {
        var step = CreateEnhancedAuthenticationStep();
        await RunOperationAsync(
            "Send enhanced authentication",
            cancellationToken => _session.SendEnhancedAuthenticationExchangeDataAsync(step, cancellationToken));
    }

    /// <summary>Adds a dashboard tile for the selected or manually entered topic.</summary>
    /// <returns>A task that completes when the dashboard tile has been added and subscribed.</returns>
    [ReactiveCommand]
    private async Task AddSelectedTopicDashboardTile()
    {
        var topic = SelectedTopicNode?.FullTopic;
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = SelectedMessage?.Topic;
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = Publisher.Topic;
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            AddLog(WarningLevel, DashboardSource, "Select or enter a concrete topic before adding a dashboard tile.");
            return;
        }

        _ = AddDashboardTile(topic);
        SaveDashboardLayout();
        if (IsConnected)
        {
            await RunOperationAsync(
                "Subscribe dashboard",
                cancellationToken => SubscribeExactTopicAsync(topic, cancellationToken));
        }
    }

    /// <summary>Adds an editable MQTT subscribe user property row.</summary>
    [ReactiveCommand]
    private void AddSubscribeUserProperty() => Subscription.UserProperties.Add(new());

    /// <summary>Creates the enhanced-authentication step represented by the current editor fields.</summary>
    /// <returns>The enhanced-authentication step.</returns>
    private EnhancedAuthenticationStepViewModel CreateEnhancedAuthenticationStep() =>
        new()
        {
            Data = Connection.EnhancedAuthenticationStepData,
            DataFormat = Connection.EnhancedAuthenticationStepDataFormat,
            Reason = Connection.EnhancedAuthenticationStepReason,
            ReasonCode = Connection.EnhancedAuthenticationStepReasonCode,
        };

    /// <summary>Adds an editable MQTT will user property row.</summary>
    [ReactiveCommand]
    private void AddWillUserProperty() => Connection.WillUserProperties.Add(new());

    /// <summary>Clears retained messages, issues, and observed topic nodes.</summary>
    [ReactiveCommand]
    private void ClearMessages()
    {
        Messages.Clear();
        TopicIssues.Clear();
        Topics[0].Children.Clear();
        SelectedMessage = null;
        SelectedTopicNode = null;
    }

    /// <summary>Connects the MQTT client and starts the embedded broker when requested.</summary>
    /// <returns>A task that completes when the command has finished.</returns>
    [ReactiveCommand]
    private Task ConnectAsync() =>
        RunOperationAsync("Connect", async cancellationToken =>
        {
            if (Connection.StartEmbeddedServer)
            {
                await _session.StartEmbeddedServerAsync(Connection.EmbeddedServerPort, cancellationToken).ConfigureAwait(false);
            }

            await _session.ConnectAsync(Connection.BuildClientOptions(), cancellationToken).ConfigureAwait(false);
            await _session.SubscribeAsync(Subscription, cancellationToken).ConfigureAwait(false);
            await SubscribeDashboardTilesAsync(cancellationToken).ConfigureAwait(false);
        });

    /// <summary>Disconnects the MQTT client and stops the embedded broker.</summary>
    /// <returns>A task that completes when the command has finished.</returns>
    [ReactiveCommand]
    private Task DisconnectAsync() =>
        RunOperationAsync("Disconnect", async cancellationToken =>
        {
            await _session.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            await _session.StopEmbeddedServerAsync(cancellationToken).ConfigureAwait(false);
        });

    /// <summary>Publishes the current message builder contents.</summary>
    /// <returns>A task that completes when the command has finished.</returns>
    [ReactiveCommand]
    private async Task PublishAsync()
    {
        var validation = Publisher.Validate();
        if (!string.IsNullOrEmpty(validation))
        {
            AddLog(ErrorLevel, PublishSource, validation);
            return;
        }

        await RunOperationAsync(
            PublishSource,
            cancellationToken => _session.PublishAsync(Publisher.BuildMessage(), cancellationToken));
    }

    /// <summary>Removes an MQTT connection user property row.</summary>
    /// <param name="property">The row to remove.</param>
    [ReactiveCommand]
    private void RemoveConnectionUserProperty(UserPropertyViewModel property) =>
        _ = Connection.UserProperties.Remove(property);

    /// <summary>Removes an MQTT publish user property row.</summary>
    /// <param name="property">The row to remove.</param>
    [ReactiveCommand]
    private void RemovePublishUserProperty(UserPropertyViewModel property) =>
        _ = Publisher.UserProperties.Remove(property);

    /// <summary>Removes a scripted enhanced-authentication exchange step.</summary>
    /// <param name="step">The step to remove.</param>
    [ReactiveCommand]
    private void RemoveEnhancedAuthenticationStep(EnhancedAuthenticationStepViewModel step) =>
        _ = Connection.EnhancedAuthenticationSteps.Remove(step);

    /// <summary>Removes an MQTT subscribe user property row.</summary>
    /// <param name="property">The row to remove.</param>
    [ReactiveCommand]
    private void RemoveSubscribeUserProperty(UserPropertyViewModel property) =>
        _ = Subscription.UserProperties.Remove(property);

    /// <summary>Removes an MQTT will user property row.</summary>
    /// <param name="property">The row to remove.</param>
    [ReactiveCommand]
    private void RemoveWillUserProperty(UserPropertyViewModel property) =>
        _ = Connection.WillUserProperties.Remove(property);

    /// <summary>Saves the dashboard layout to disk.</summary>
    [ReactiveCommand]
    private void SaveDashboardLayout() => SaveDashboardLayoutFile();

    /// <summary>Subscribes with the current subscription options.</summary>
    /// <returns>A task that completes when the command has finished.</returns>
    [ReactiveCommand]
    private Task SubscribeAsync() =>
        RunOperationAsync(
            "Subscribe",
            cancellationToken => _session.SubscribeAsync(Subscription, cancellationToken));

    /// <summary>Unsubscribes the current topic filter.</summary>
    /// <returns>A task that completes when the command has finished.</returns>
    [ReactiveCommand]
    private Task UnsubscribeAsync() =>
        RunOperationAsync(
            "Unsubscribe",
            cancellationToken => _session.UnsubscribeAsync(Subscription.TopicFilter, cancellationToken));

    /// <summary>Copies the selected topic into publish and subscribe editors.</summary>
    [ReactiveCommand]
    private void UseSelectedTopic()
    {
        var topic = SelectedTopicNode?.FullTopic;
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = SelectedMessage?.Topic;
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            return;
        }

        SelectedTopic = topic;
        Publisher.Topic = topic;
        Subscription.TopicFilter = topic;
    }

    /// <summary>Adds a dashboard tile for a topic or selects the existing tile.</summary>
    /// <param name="topic">The exact MQTT topic represented by the tile.</param>
    /// <returns>The existing or newly created dashboard tile.</returns>
    private DashboardTileViewModel AddDashboardTile(string topic)
    {
        if (_dashboardByTopic.TryGetValue(topic, out var existing))
        {
            SelectedDashboardTile = existing;
            return existing;
        }

        var tile = new DashboardTileViewModel(topic, PublishTileAsync, RemoveDashboardTile, MoveDashboardTile);
        _dashboardByTopic.Add(topic, tile);
        DashboardTiles.Add(tile);
        SelectedDashboardTile = tile;
        return tile;
    }

    /// <summary>Adds a log entry using the view-model clock.</summary>
    /// <param name="level">The log level text.</param>
    /// <param name="source">The log source text.</param>
    /// <param name="message">The log message text.</param>
    private void AddLog(string level, string source, string message) =>
        AddLog(new(_timeProvider.GetLocalNow(), level, source, message));

    /// <summary>Adds a log entry to the retained log collection.</summary>
    /// <param name="entry">The log entry to add.</param>
    private void AddLog(MqttLogEntry entry)
    {
        LogEntries.Insert(0, entry);
        Trim(LogEntries, MaximumLogEntries);
    }

    /// <summary>Loads persisted dashboard layout from disk.</summary>
    private void LoadDashboardLayout()
    {
        if (!File.Exists(_dashboardLayoutPath))
        {
            return;
        }

        try
        {
            var layouts = JsonSerializer.Deserialize<List<DashboardTileLayout>>(File.ReadAllText(_dashboardLayoutPath), JsonOptions) ?? [];
            foreach (var layout in layouts)
            {
                if (string.IsNullOrWhiteSpace(layout.Topic))
                {
                    continue;
                }

                var tile = AddDashboardTile(layout.Topic);
                tile.ApplyLayout(layout);
            }
        }
        catch (Exception exception)
        {
            AddLog(WarningLevel, DashboardSource, $"Dashboard layout was not loaded: {exception.Message}");
        }
    }

    /// <summary>Determines whether a message matches the current search text.</summary>
    /// <param name="message">The received MQTT message.</param>
    /// <returns><see langword="true"/> when the message should be shown.</returns>
    private bool MatchesSearch(ReceivedMqttMessage message) =>
        string.IsNullOrWhiteSpace(MessageSearch)
        || message.Topic.Contains(MessageSearch, StringComparison.OrdinalIgnoreCase)
        || message.Payload.Contains(MessageSearch, StringComparison.OrdinalIgnoreCase);

    /// <summary>Moves a dashboard tile by a signed offset.</summary>
    /// <param name="tile">The tile to move.</param>
    /// <param name="offset">The signed tile index offset.</param>
    private void MoveDashboardTile(DashboardTileViewModel tile, int offset)
    {
        var oldIndex = DashboardTiles.IndexOf(tile);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= DashboardTiles.Count)
        {
            return;
        }

        DashboardTiles.Move(oldIndex, newIndex);
        SaveDashboardLayout();
    }

    /// <summary>Handles connection state changes observed by the session service.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="connected">A value indicating whether the client is connected.</param>
    private void OnConnectionChanged(object? sender, bool connected) =>
        _postToUi(() =>
        {
            IsConnected = connected;
            Status = connected ? "Connected" : "Disconnected";
        });

    /// <summary>Handles log entries observed by the session service.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="entry">The received log entry.</param>
    private void OnLogReceived(object? sender, MqttLogEntry entry) =>
        _postToUi(() => AddLog(entry));

    /// <summary>Handles messages observed by the session service.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="message">The received MQTT message.</param>
    private void OnMessageReceived(object? sender, ReceivedMqttMessage message) =>
        _postToUi(() =>
        {
            UpdateTopicModel(message);
            UpdateDashboard(message);
            if (!MatchesSearch(message))
            {
                return;
            }

            Messages.Insert(0, message);
            Trim(Messages, MaximumMessages);
            SelectedMessage ??= message;
        });

    /// <summary>Handles topic issues observed by the session service.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="issue">The detected topic issue.</param>
    private void OnTopicIssueDetected(object? sender, TopicIssue issue) =>
        _postToUi(() =>
        {
            TopicIssues.Insert(0, issue);
            Trim(TopicIssues, MaximumMessages);
        });

    /// <summary>Publishes the editable value from a dashboard tile.</summary>
    /// <param name="tile">The tile to publish from.</param>
    /// <returns>A task that completes when the publish command has finished.</returns>
    private async Task PublishTileAsync(DashboardTileViewModel tile)
    {
        Publisher.Topic = tile.Topic;
        Publisher.Payload = tile.EditableValue;
        Publisher.Retain = tile.Retain;
        Publisher.QualityOfService = tile.QualityOfService;
        Publisher.PayloadFormat = tile.VisualKind switch
        {
            DashboardVisualKind.Toggle => PayloadFormat.Boolean,
            DashboardVisualKind.Gauge => PayloadFormat.Number,
            DashboardVisualKind.Json => PayloadFormat.Json,
            DashboardVisualKind.Binary => PayloadFormat.Hex,
            _ => PayloadFormat.Utf8Text,
        };

        await PublishAsync();
    }

    /// <summary>Removes a dashboard tile.</summary>
    /// <param name="tile">The tile to remove.</param>
    private void RemoveDashboardTile(DashboardTileViewModel tile)
    {
        _ = DashboardTiles.Remove(tile);
        _ = _dashboardByTopic.Remove(tile.Topic);
        if (ReferenceEquals(SelectedDashboardTile, tile))
        {
            SelectedDashboardTile = null;
        }

        SaveDashboardLayout();
    }

    /// <summary>Runs an asynchronous MQTT command with busy state and logging.</summary>
    /// <param name="operation">The operation display name.</param>
    /// <param name="action">The cancellable operation body.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunOperationAsync(string operation, Func<CancellationToken, Task> action)
    {
        if (Interlocked.Exchange(ref _operationRunning, 1) != 0)
        {
            AddLog(WarningLevel, operation, "Another MQTT operation is already running.");
            return;
        }

        var finalStatus = $"{operation} running";
        SetOperationState(true, finalStatus);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(OperationTimeoutSeconds));
        try
        {
            await action(cancellation.Token).ConfigureAwait(false);
            finalStatus = $"{operation} complete";
            SetOperationState(true, finalStatus);
        }
        catch (Exception exception)
        {
            finalStatus = $"{operation} failed";
            SetOperationState(true, finalStatus);
            AddLog(ErrorLevel, operation, exception.Message);
        }
        finally
        {
            _ = Interlocked.Exchange(ref _operationRunning, 0);
            SetOperationState(false, finalStatus);
        }
    }

    /// <summary>Saves persisted dashboard layout to disk.</summary>
    private void SaveDashboardLayoutFile()
    {
        try
        {
            _ = Directory.CreateDirectory(Path.GetDirectoryName(_dashboardLayoutPath)!);
            var layouts = new DashboardTileLayout[DashboardTiles.Count];
            for (var index = 0; index < layouts.Length; index++)
            {
                layouts[index] = DashboardTiles[index].ToLayout();
            }

            File.WriteAllText(_dashboardLayoutPath, JsonSerializer.Serialize(layouts, JsonOptions));
            AddLog("Info", DashboardSource, $"Saved {layouts.Length} dashboard tiles.");
        }
        catch (Exception exception)
        {
            AddLog(ErrorLevel, DashboardSource, $"Dashboard layout was not saved: {exception.Message}");
        }
    }

    /// <summary>Sets busy and status state through the UI dispatcher.</summary>
    /// <param name="isBusy">A value indicating whether an operation is running.</param>
    /// <param name="status">The status text to display.</param>
    private void SetOperationState(bool isBusy, string status) => _postToUi(() =>
        {
            IsBusy = isBusy;
            Status = status;
        });

    /// <summary>Subscribes all exact dashboard tile topics after a new connection.</summary>
    /// <param name="cancellationToken">Cancels the subscribe operations.</param>
    /// <returns>A task that completes when all tile topics have been subscribed.</returns>
    private async Task SubscribeDashboardTilesAsync(CancellationToken cancellationToken)
    {
        foreach (var tile in DashboardTiles)
        {
            await SubscribeExactTopicAsync(tile.Topic, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Subscribes one exact dashboard tile topic using current subscription defaults.</summary>
    /// <param name="topic">The exact MQTT topic to subscribe to.</param>
    /// <param name="cancellationToken">Cancels the subscribe operation.</param>
    /// <returns>A task that completes when the topic has been subscribed.</returns>
    private async Task SubscribeExactTopicAsync(string topic, CancellationToken cancellationToken)
    {
        var subscription = new SubscriptionViewModel
        {
            TopicFilter = topic,
            QualityOfService = Subscription.QualityOfService,
            NoLocal = Subscription.NoLocal,
            RetainAsPublished = Subscription.RetainAsPublished,
            RetainHandling = Subscription.RetainHandling,
        };
        await _session.SubscribeAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates any dashboard tile matching the received topic.</summary>
    /// <param name="message">The received MQTT message.</param>
    private void UpdateDashboard(ReceivedMqttMessage message)
    {
        if (_dashboardByTopic.TryGetValue(message.Topic, out var tile))
        {
            tile.Apply(message);
        }
    }

    /// <summary>Updates the observed topic tree for a received message.</summary>
    /// <param name="message">The received MQTT message.</param>
    private void UpdateTopicModel(ReceivedMqttMessage message)
    {
        var current = Topics[0];
        foreach (var part in message.Topic.Split('/'))
        {
            current = current.GetOrAdd(part);
        }

        current.LastPayload = message.Payload;
        current.LastSeen = message.Timestamp;
        current.MessageCount++;
    }
}
