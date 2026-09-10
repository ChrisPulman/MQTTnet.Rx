// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Avalonia.Controls.Templates;
using MQTTnet.Rx.Toolkit.Controls;
using MQTTnet.Rx.Toolkit.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace MQTTnet.Rx.Toolkit.Views;

/// <summary>The broker workspace and its activation-scoped MVVM bindings.</summary>
internal sealed partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DashboardTilesItemsControl.ItemTemplate = new FuncDataTemplate<DashboardTileViewModel>(static (model, _) => new DashboardTileView { ViewModel = model });
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = this.WhenActivated(disposables =>
        {
            BindConnection(disposables);
            BindTransport(disposables);
            BindSecurity(disposables);
            BindWill(disposables);
            BindWorkspace(disposables);
            BindMessageOptions(disposables);
            BindCertificateSources(disposables);
            BindAdditionalEncodings(disposables);
            BindAuthenticationExchange(disposables);
            BindTwinCat(disposables);
        });
    }

    /// <summary>Binds certificate sources and validation policies.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindCertificateSources(MultipleDisposable disposables)
    {
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ClientCertificateSources, static view => view.CertificateSourceSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateSource, static view => view.CertificateSourceSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ClientCertificateStoreNames, static view => view.CertificateStoreSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateStoreName, static view => view.CertificateStoreSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ClientCertificateStoreLocations, static view => view.CertificateLocationSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateStoreLocation, static view => view.CertificateLocationSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ClientCertificateFindTypes, static view => view.CertificateFindTypeSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateFindType, static view => view.CertificateFindTypeSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.CertificateValidationModes, static view => view.CertificateValidationSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.CertificateValidationMode, static view => view.CertificateValidationSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.CertificateSelectionModes, static view => view.CertificateSelectionSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.CertificateSelectionMode, static view => view.CertificateSelectionSelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateFindValue, static view => view.CertificateFindValueEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificateAllowInvalid, static view => view.CertificateAllowInvalidToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.SelectedClientCertificateThumbprint, static view => view.CertificateThumbprintEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.PinnedServerCertificateThumbprint, static view => view.PinnedServerCertificateEditor.Text).DisposeWith(disposables);
    }

    /// <summary>Binds binary encodings and native WebSocket options.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindAdditionalEncodings(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketCookies, static view => view.WebSocketCookiesEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketCredentialUsername, static view => view.WebSocketCredentialUsernameEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketCredentialPassword, static view => view.WebSocketCredentialPasswordEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketCredentialDomain, static view => view.WebSocketCredentialDomainEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpUseLinger, static view => view.TcpUseLingerToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpLingerEnabled, static view => view.TcpLingerEnabledToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.UseWebSocketDeflate, static view => view.WebSocketDeflateToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketDeflateClientContextTakeover, static view => view.WebSocketClientTakeoverToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketDeflateServerContextTakeover, static view => view.WebSocketServerTakeoverToggle.IsChecked).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormats, static view => view.AuthenticationDataFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationDataFormat, static view => view.AuthenticationDataFormatSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormats, static view => view.WillPayloadFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillPayloadFormat, static view => view.WillPayloadFormatSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormats, static view => view.WillCorrelationFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillCorrelationDataFormat, static view => view.WillCorrelationFormatSelector.SelectedItem).DisposeWith(disposables);
    }

    /// <summary>Binds authentication response steps and the live exchange action.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindAuthenticationExchange(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationStepData, static view => view.AuthenticationStepDataEditor.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormats, static view => view.AuthenticationStepFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationStepDataFormat, static view => view.AuthenticationStepFormatSelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationStepReason, static view => view.AuthenticationStepReasonEditor.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.AuthenticationReasonCodes, static view => view.AuthenticationReasonCodeSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationStepReasonCode, static view => view.AuthenticationReasonCodeSelector.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationSteps, static view => view.AuthenticationStepsItemsControl.ItemsSource).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.AddEnhancedAuthenticationStepCommand, static view => view.AddAuthenticationStepButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.SendEnhancedAuthenticationExchangeCommand, static view => view.SendAuthenticationExchangeButton).DisposeWith(disposables);
    }

    /// <summary>Binds the connection controls for the current activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindConnection(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.Host, static view => view.ConnectionHostTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.TransportModes, static view => view.ConnectionTransportModesComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TransportMode, static view => view.ConnectionTransportModesComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ProtocolVersions, static view => view.ConnectionProtocolVersionsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ProtocolVersion, static view => view.ConnectionProtocolVersionsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ConnectionUri, static view => view.ConnectionConnectionUriTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientId, static view => view.ConnectionClientIdTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.Username, static view => view.ConnectionUsernameTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.Password, static view => view.ConnectionPasswordTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormats, static view => view.PasswordFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.PasswordFormat, static view => view.PasswordFormatSelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.UseCredentials, static view => view.UseEmptyCredentialsToggle.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.StartEmbeddedServer, static view => view.ConnectionStartEmbeddedServerCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.CleanStart, static view => view.ConnectionCleanStartCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TryPrivate, static view => view.ConnectionTryPrivateCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.RequestProblemInformation, static view => view.ConnectionRequestProblemInformationCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.RequestResponseInformation, static view => view.ConnectionRequestResponseInformationCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ValidateFeatures, static view => view.ConnectionValidateFeaturesCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.DisablePacketFragmentation, static view => view.ConnectionDisablePacketFragmentationCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationMethod, static view => view.ConnectionEnhancedAuthenticationMethodTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EnhancedAuthenticationData, static view => view.ConnectionEnhancedAuthenticationDataTextBox.Text).DisposeWith(disposables);
    }

    /// <summary>Binds the transport controls for the current activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindTransport(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketUri, static view => view.ConnectionWebSocketUriTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.AddressFamilies, static view => view.ConnectionAddressFamiliesComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpAddressFamily, static view => view.ConnectionAddressFamiliesComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.ProtocolTypes, static view => view.ConnectionProtocolTypesComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpProtocolType, static view => view.ConnectionProtocolTypesComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpNoDelay, static view => view.ConnectionTcpNoDelayCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpDualMode, static view => view.ConnectionTcpDualModeCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TcpLocalAddress, static view => view.ConnectionTcpLocalAddressTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketUseDefaultCredentials, static view => view.ConnectionWebSocketUseDefaultCredentialsCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketSubProtocols, static view => view.ConnectionWebSocketSubProtocolsTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketHeaders, static view => view.ConnectionWebSocketHeadersTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyAddress, static view => view.ConnectionWebSocketProxyAddressTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyUsername, static view => view.ConnectionWebSocketProxyUsernameTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyPassword, static view => view.ConnectionWebSocketProxyPasswordTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyDomain, static view => view.ConnectionWebSocketProxyDomainTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyBypassOnLocal, static view => view.ConnectionWebSocketProxyBypassOnLocalCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(
            ViewModel,
            static vm => vm.Connection.WebSocketProxyUseDefaultCredentials,
            static view => view.ConnectionWebSocketProxyUseDefaultCredentialsCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WebSocketProxyBypassList, static view => view.ConnectionWebSocketProxyBypassListTextBox.Text).DisposeWith(disposables);
    }

    /// <summary>Binds the security controls for the current activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindSecurity(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.TrustChainCertificatePaths, static view => view.TrustChainEditor.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TlsApplicationProtocols, static view => view.ApplicationProtocolsEditor.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.EncryptionPolicies, static view => view.EncryptionPolicySelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.EncryptionPolicy, static view => view.EncryptionPolicySelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.UseTls, static view => view.ConnectionUseTlsCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.TlsTargetHost, static view => view.ConnectionTlsTargetHostTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.SslProtocolOptions, static view => view.ConnectionSslProtocolOptionsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.SslProtocols, static view => view.ConnectionSslProtocolOptionsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.RevocationModes, static view => view.ConnectionRevocationModesComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.RevocationMode, static view => view.ConnectionRevocationModesComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.AllowUntrustedCertificates, static view => view.ConnectionAllowUntrustedCertificatesCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.IgnoreCertificateChainErrors, static view => view.ConnectionIgnoreCertificateChainErrorsCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(
                ViewModel,
                static vm => vm.Connection.IgnoreCertificateRevocationErrors,
                static view => view.ConnectionIgnoreCertificateRevocationErrorsCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.AllowTlsRenegotiation, static view => view.ConnectionAllowTlsRenegotiationCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificatePath, static view => view.ConnectionClientCertificatePathTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.ClientCertificatePassword, static view => view.ConnectionClientCertificatePasswordTextBox.Text).DisposeWith(disposables);
    }

    /// <summary>Binds the will controls for the current activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindWill(MultipleDisposable disposables)
    {
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillTopic, static view => view.ConnectionWillTopicTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillPayload, static view => view.ConnectionWillPayloadTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.QualityOfServiceLevels, static view => view.ConnectionQualityOfServiceLevelsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillQualityOfService, static view => view.ConnectionQualityOfServiceLevelsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Connection.PayloadFormatIndicators, static view => view.ConnectionPayloadFormatIndicatorsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillPayloadFormatIndicator, static view => view.ConnectionPayloadFormatIndicatorsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillContentType, static view => view.ConnectionWillContentTypeTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillResponseTopic, static view => view.ConnectionWillResponseTopicTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillCorrelationData, static view => view.ConnectionWillCorrelationDataTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Connection.WillRetain, static view => view.ConnectionWillRetainCheckBox.IsChecked).DisposeWith(disposables);
    }

    /// <summary>Binds the workspace controls for the current activation.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindWorkspace(MultipleDisposable disposables)
    {
        _ = this.OneWayBind(ViewModel, static vm => vm.Status, static view => view.StatusTextBlock.Text).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.ConnectCommand, static view => view.ConnectCommandButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.DisconnectCommand, static view => view.DisconnectCommandButton).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Subscription.TopicFilter, static view => view.SubscriptionTopicFilterTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Subscription.QualityOfServiceLevels, static view => view.SubscriptionQualityOfServiceLevelsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Subscription.QualityOfService, static view => view.SubscriptionQualityOfServiceLevelsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Subscription.RetainHandlingOptions, static view => view.SubscriptionRetainHandlingOptionsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Subscription.RetainHandling, static view => view.SubscriptionRetainHandlingOptionsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Subscription.NoLocal, static view => view.SubscriptionNoLocalCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Subscription.RetainAsPublished, static view => view.SubscriptionRetainAsPublishedCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.SubscribeCommand, static view => view.SubscribeCommandButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.UnsubscribeCommand, static view => view.UnsubscribeCommandButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.UseSelectedTopicCommand, static view => view.UseSelectedTopicCommandButton).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Topics, static view => view.TopicsTreeView.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.SelectedTopicNode, static view => view.TopicsTreeView.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.MessageSearch, static view => view.MessageSearchSearchBox.Text).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.UseSelectedTopicCommand, static view => view.UseSelectedTopicCommandButton2).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.ClearMessagesCommand, static view => view.ClearMessagesCommandButton).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Messages, static view => view.MessagesDataGrid.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.SelectedMessage, static view => view.MessagesDataGrid.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.Topic, static view => view.PublisherTopicTextBox.Text).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Publisher.PayloadFormats, static view => view.PublisherPayloadFormatsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.PayloadFormat, static view => view.PublisherPayloadFormatsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Publisher.QualityOfServiceLevels, static view => view.PublisherQualityOfServiceLevelsComboBox.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.QualityOfService, static view => view.PublisherQualityOfServiceLevelsComboBox.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.Retain, static view => view.PublisherRetainCheckBox.IsChecked).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.Payload, static view => view.PublisherPayloadTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.ContentType, static view => view.PublisherContentTypeTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.ResponseTopic, static view => view.PublisherResponseTopicTextBox.Text).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.CorrelationData, static view => view.PublisherCorrelationDataTextBox.Text).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.PublishCommand, static view => view.PublishCommandButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.AddSelectedTopicDashboardTileCommand, static view => view.AddSelectedTopicDashboardTileCommandButton).DisposeWith(disposables);
        _ = this.BindCommand(ViewModel, static vm => vm.SaveDashboardLayoutCommand, static view => view.SaveDashboardLayoutCommandButton).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.DashboardTiles, static view => view.DashboardTilesItemsControl.ItemsSource).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.TopicIssues, static view => view.TopicIssuesDataGrid.ItemsSource).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.LogEntries, static view => view.LogEntriesDataGrid.ItemsSource).DisposeWith(disposables);
    }

    /// <summary>Binds optional MQTT 5 message metadata editors.</summary>
    /// <param name="disposables">The activation lifetime.</param>
    private void BindMessageOptions(MultipleDisposable disposables)
    {
        _ = this.OneWayBind(ViewModel, static vm => vm.Publisher.PayloadFormats, static view => view.CorrelationFormatSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.CorrelationDataFormat, static view => view.CorrelationFormatSelector.SelectedItem).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.UsePayloadFormatIndicator, static view => view.IncludePayloadIndicatorToggle.IsChecked).DisposeWith(disposables);
        _ = this.OneWayBind(ViewModel, static vm => vm.Publisher.PayloadFormatIndicators, static view => view.PayloadIndicatorSelector.ItemsSource).DisposeWith(disposables);
        _ = this.Bind(ViewModel, static vm => vm.Publisher.PayloadFormatIndicator, static view => view.PayloadIndicatorSelector.SelectedItem).DisposeWith(disposables);
    }
}
