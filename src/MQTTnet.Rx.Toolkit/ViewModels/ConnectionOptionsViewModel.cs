// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet.Channel;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Rx.Client;
using MQTTnet.Rx.Toolkit.Models;
using ReactiveUI.SourceGenerators;

namespace MQTTnet.Rx.Toolkit.ViewModels;

/// <summary>Captures editable MQTT client connection options for the Toolkit.</summary>
internal sealed partial class ConnectionOptionsViewModel : ViewModelBase, IDisposable
{
    /// <summary>Stores the default certificate source choices exposed without injected providers.</summary>
    private static readonly ClientCertificateSource[] DefaultClientCertificateSources =
    [
        ClientCertificateSource.None,
        ClientCertificateSource.File,
        ClientCertificateSource.Store,
    ];

    /// <summary>Stores the certificate source choices exposed when an injected provider is registered.</summary>
    private static readonly ClientCertificateSource[] ProviderClientCertificateSources =
    [
        ClientCertificateSource.None,
        ClientCertificateSource.File,
        ClientCertificateSource.Store,
        ClientCertificateSource.Provider,
    ];

    /// <summary>Stores the default certificate validation choices exposed without injected callbacks.</summary>
    private static readonly CertificateValidationMode[] DefaultCertificateValidationModes =
    [
        CertificateValidationMode.System,
        CertificateValidationMode.PinnedThumbprint,
        CertificateValidationMode.AllowAll,
        CertificateValidationMode.RejectAll,
    ];

    /// <summary>Stores the certificate validation choices exposed when an injected callback is registered.</summary>
    private static readonly CertificateValidationMode[] CallbackCertificateValidationModes =
    [
        CertificateValidationMode.System,
        CertificateValidationMode.PinnedThumbprint,
        CertificateValidationMode.AllowAll,
        CertificateValidationMode.RejectAll,
        CertificateValidationMode.Callback,
    ];

    /// <summary>Stores the default certificate selection choices exposed without injected callbacks.</summary>
    private static readonly CertificateSelectionMode[] DefaultCertificateSelectionModes =
    [
        CertificateSelectionMode.Automatic,
        CertificateSelectionMode.First,
        CertificateSelectionMode.Thumbprint,
    ];

    /// <summary>Stores the certificate selection choices exposed when an injected callback is registered.</summary>
    private static readonly CertificateSelectionMode[] CallbackCertificateSelectionModes =
    [
        CertificateSelectionMode.Automatic,
        CertificateSelectionMode.First,
        CertificateSelectionMode.Thumbprint,
        CertificateSelectionMode.Callback,
    ];

    /// <summary>Stores a modern TLS protocol combination not returned by enum value enumeration.</summary>
    private static readonly SslProtocols ModernTlsProtocolCombination = Enum.Parse<SslProtocols>("Tls12, Tls13");

    /// <summary>Stores loaded client certificates owned by this options view model.</summary>
    private readonly List<X509Certificate2> _ownedCertificates = [];

    /// <summary>Stores the MQTT client identifier.</summary>
    [Reactive]
    private string _clientId = $"mqttnet-rx-toolkit-{Environment.MachineName}";

    /// <summary>Stores the selected MQTT transport mode.</summary>
    [Reactive]
    private MqttTransportMode _transportMode;

    /// <summary>Stores the TCP MQTT host name.</summary>
    [Reactive]
    private string _host = "localhost";

    /// <summary>Stores the TCP MQTT port.</summary>
    [Reactive]
    private int _port = 1883;

    /// <summary>Stores the MQTTnet connection URI.</summary>
    [Reactive]
    private string _connectionUri = "mqtt://localhost:1883";

    /// <summary>Stores the WebSocket MQTT URI.</summary>
    [Reactive]
    private string _webSocketUri = "ws://localhost:8083/mqtt";

    /// <summary>Stores the MQTT protocol version.</summary>
    [Reactive]
    private MqttProtocolVersion _protocolVersion = MqttProtocolVersion.V500;

    /// <summary>Stores the MQTT username.</summary>
    [Reactive]
    private string _username = string.Empty;

    /// <summary>Stores the MQTT password.</summary>
    [Reactive]
    private string _password = string.Empty;

    /// <summary>Stores the encoding of the MQTT binary password field.</summary>
    [Reactive]
    private PayloadFormat _passwordFormat;

    /// <summary>Stores whether empty MQTT credentials should be explicitly included.</summary>
    [Reactive]
    private bool _useCredentials;

    /// <summary>Stores whether the MQTT connection should start a clean session.</summary>
    [Reactive]
    private bool _cleanStart = true;

    /// <summary>Stores MQTT 5 session expiry seconds.</summary>
    [Reactive]
    private uint _sessionExpirySeconds;

    /// <summary>Stores MQTT keep alive seconds.</summary>
    [Reactive]
    private int _keepAliveSeconds = 30;

    /// <summary>Stores MQTT operation timeout seconds.</summary>
    [Reactive]
    private int _timeoutSeconds = 10;

    /// <summary>Stores whether TLS should be enabled.</summary>
    [Reactive]
    private bool _useTls;

    /// <summary>Stores the TLS target host override.</summary>
    [Reactive]
    private string _tlsTargetHost = string.Empty;

    /// <summary>Stores whether untrusted server certificates are allowed.</summary>
    [Reactive]
    private bool _allowUntrustedCertificates;

    /// <summary>Stores whether certificate chain errors are ignored.</summary>
    [Reactive]
    private bool _ignoreCertificateChainErrors;

    /// <summary>Stores whether certificate revocation errors are ignored.</summary>
    [Reactive]
    private bool _ignoreCertificateRevocationErrors;

    /// <summary>Stores whether TLS renegotiation is allowed.</summary>
    [Reactive]
    private bool _allowTlsRenegotiation;

    /// <summary>Stores the allowed TLS protocol versions.</summary>
    [Reactive]
    private SslProtocols _sslProtocols;

    /// <summary>Stores the X509 revocation mode.</summary>
    [Reactive]
    private X509RevocationMode _revocationMode = X509RevocationMode.Online;

    /// <summary>Stores the client certificate PKCS12 path.</summary>
    [Reactive]
    private string _clientCertificatePath = string.Empty;

    /// <summary>Stores the client certificate password.</summary>
    [Reactive]
    private string _clientCertificatePassword = string.Empty;

    /// <summary>Stores how client certificates are loaded.</summary>
    [Reactive]
    private ClientCertificateSource _clientCertificateSource;

    /// <summary>Stores how server certificate validation is handled.</summary>
    [Reactive]
    private CertificateValidationMode _certificateValidationMode;

    /// <summary>Stores how client certificate selection is handled.</summary>
    [Reactive]
    private CertificateSelectionMode _certificateSelectionMode;

    /// <summary>Stores the certificate store name used for client certificates.</summary>
    [Reactive]
    private StoreName _clientCertificateStoreName = StoreName.My;

    /// <summary>Stores the certificate store location used for client certificates.</summary>
    [Reactive]
    private StoreLocation _clientCertificateStoreLocation = StoreLocation.CurrentUser;

    /// <summary>Stores the X509 find type used for store certificate lookup.</summary>
    [Reactive]
    private X509FindType _clientCertificateFindType;

    /// <summary>Stores the X509 find value used for store certificate lookup.</summary>
    [Reactive]
    private string _clientCertificateFindValue = string.Empty;

    /// <summary>Stores whether invalid store certificates can be returned.</summary>
    [Reactive]
    private bool _clientCertificateAllowInvalid;

    /// <summary>Stores the selected client certificate thumbprint.</summary>
    [Reactive]
    private string _selectedClientCertificateThumbprint = string.Empty;

    /// <summary>Stores the pinned server certificate thumbprint.</summary>
    [Reactive]
    private string _pinnedServerCertificateThumbprint = string.Empty;

    /// <summary>Stores semicolon-separated custom trust-chain certificate paths.</summary>
    [Reactive]
    private string _trustChainCertificatePaths = string.Empty;

    /// <summary>Stores semicolon-separated TLS application protocols.</summary>
    [Reactive]
    private string _tlsApplicationProtocols = "mqtt";

    /// <summary>Stores the TLS encryption policy.</summary>
    [Reactive]
    private EncryptionPolicy _encryptionPolicy;

    /// <summary>Stores the MQTT 5 receive maximum.</summary>
    [Reactive]
    private ushort _receiveMaximum = 100;

    /// <summary>Stores the MQTT 5 topic alias maximum.</summary>
    [Reactive]
    private ushort _topicAliasMaximum = 32;

    /// <summary>Stores the MQTT 5 maximum packet size.</summary>
    [Reactive]
    private uint _maximumPacketSize = 1_048_576;

    /// <summary>Stores whether problem information should be requested.</summary>
    [Reactive]
    private bool _requestProblemInformation = true;

    /// <summary>Stores whether response information should be requested.</summary>
    [Reactive]
    private bool _requestResponseInformation;

    /// <summary>Stores whether MQTTnet private extensions should be attempted.</summary>
    [Reactive]
    private bool _tryPrivate;

    /// <summary>Stores whether packet fragmentation should be disabled.</summary>
    [Reactive]
    private bool _disablePacketFragmentation;

    /// <summary>Stores whether MQTTnet should validate negotiated feature usage.</summary>
    [Reactive]
    private bool _validateFeatures = true;

    /// <summary>Stores the MQTTnet writer buffer size.</summary>
    [Reactive]
    private int _writerBufferSize = 4096;

    /// <summary>Stores the maximum MQTTnet writer buffer size.</summary>
    [Reactive]
    private int _writerBufferSizeMax = 65_536;

    /// <summary>Stores the enhanced authentication method name.</summary>
    [Reactive]
    private string _enhancedAuthenticationMethod = string.Empty;

    /// <summary>Stores the enhanced authentication data text.</summary>
    [Reactive]
    private string _enhancedAuthenticationData = string.Empty;

    /// <summary>Stores the enhanced authentication data encoding format.</summary>
    [Reactive]
    private PayloadFormat _enhancedAuthenticationDataFormat;

    /// <summary>Stores the enhanced authentication step data text.</summary>
    [Reactive]
    private string _enhancedAuthenticationStepData = string.Empty;

    /// <summary>Stores the enhanced authentication step data encoding format.</summary>
    [Reactive]
    private PayloadFormat _enhancedAuthenticationStepDataFormat;

    /// <summary>Stores the enhanced authentication step reason text.</summary>
    [Reactive]
    private string _enhancedAuthenticationStepReason = string.Empty;

    /// <summary>Stores the enhanced authentication step reason code.</summary>
    [Reactive]
    private MqttAuthenticateReasonCode _enhancedAuthenticationStepReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication;

    /// <summary>Stores the TCP address family.</summary>
    [Reactive]
    private AddressFamily _tcpAddressFamily;

    /// <summary>Stores the TCP protocol type.</summary>
    [Reactive]
    private ProtocolType _tcpProtocolType = ProtocolType.Tcp;

    /// <summary>Stores whether TCP no-delay is enabled.</summary>
    [Reactive]
    private bool _tcpNoDelay = true;

    /// <summary>Stores whether a TCP linger state should be configured.</summary>
    [Reactive]
    private bool _tcpUseLinger;

    /// <summary>Stores whether the configured TCP linger state is enabled.</summary>
    [Reactive]
    private bool _tcpLingerEnabled;

    /// <summary>Stores the configured TCP linger time in seconds.</summary>
    [Reactive]
    private int _tcpLingerTimeSeconds;

    /// <summary>Stores whether dual-mode sockets are enabled.</summary>
    [Reactive]
    private bool _tcpDualMode;

    /// <summary>Stores the local TCP bind address.</summary>
    [Reactive]
    private string _tcpLocalAddress = string.Empty;

    /// <summary>Stores the local TCP bind port.</summary>
    [Reactive]
    private int _tcpLocalPort;

    /// <summary>Stores the TCP socket buffer size.</summary>
    [Reactive]
    private int _tcpBufferSize = 8192;

    /// <summary>Stores the WebSocket keep-alive interval in seconds.</summary>
    [Reactive]
    private int _webSocketKeepAliveSeconds = 30;

    /// <summary>Stores whether WebSocket default credentials should be used.</summary>
    [Reactive]
    private bool _webSocketUseDefaultCredentials;

    /// <summary>Stores the WebSocket credential username.</summary>
    [Reactive]
    private string _webSocketCredentialUsername = string.Empty;

    /// <summary>Stores the WebSocket credential password.</summary>
    [Reactive]
    private string _webSocketCredentialPassword = string.Empty;

    /// <summary>Stores the WebSocket credential domain.</summary>
    [Reactive]
    private string _webSocketCredentialDomain = string.Empty;

    /// <summary>Stores WebSocket subprotocols separated by commas or semicolons.</summary>
    [Reactive]
    private string _webSocketSubProtocols = "mqtt";

    /// <summary>Stores WebSocket request headers in key-colon-value lines.</summary>
    [Reactive]
    private string _webSocketHeaders = string.Empty;

    /// <summary>Stores the WebSocket proxy address.</summary>
    [Reactive]
    private string _webSocketProxyAddress = string.Empty;

    /// <summary>Stores the WebSocket proxy username.</summary>
    [Reactive]
    private string _webSocketProxyUsername = string.Empty;

    /// <summary>Stores the WebSocket proxy password.</summary>
    [Reactive]
    private string _webSocketProxyPassword = string.Empty;

    /// <summary>Stores the WebSocket proxy domain.</summary>
    [Reactive]
    private string _webSocketProxyDomain = string.Empty;

    /// <summary>Stores whether the WebSocket proxy should bypass local addresses.</summary>
    [Reactive]
    private bool _webSocketProxyBypassOnLocal = true;

    /// <summary>Stores whether the WebSocket proxy should use default credentials.</summary>
    [Reactive]
    private bool _webSocketProxyUseDefaultCredentials;

    /// <summary>Stores the WebSocket proxy bypass list.</summary>
    [Reactive]
    private string _webSocketProxyBypassList = string.Empty;

    /// <summary>Stores WebSocket cookies as name=value lines.</summary>
    [Reactive]
    private string _webSocketCookies = string.Empty;

    /// <summary>Stores whether WebSocket deflate options are enabled.</summary>
    [Reactive]
    private bool _useWebSocketDeflate;

    /// <summary>Stores WebSocket deflate client maximum window bits.</summary>
    [Reactive]
    private int _webSocketDeflateClientMaxWindowBits = 15;

    /// <summary>Stores WebSocket deflate server maximum window bits.</summary>
    [Reactive]
    private int _webSocketDeflateServerMaxWindowBits = 15;

    /// <summary>Stores whether WebSocket client context takeover is enabled.</summary>
    [Reactive]
    private bool _webSocketDeflateClientContextTakeover = true;

    /// <summary>Stores whether WebSocket server context takeover is enabled.</summary>
    [Reactive]
    private bool _webSocketDeflateServerContextTakeover = true;

    /// <summary>Stores whether the embedded broker should start with the client connection.</summary>
    [Reactive]
    private bool _startEmbeddedServer = true;

    /// <summary>Stores the embedded broker TCP port.</summary>
    [Reactive]
    private int _embeddedServerPort = 1883;

    /// <summary>Stores the MQTT will topic.</summary>
    [Reactive]
    private string _willTopic = string.Empty;

    /// <summary>Stores the MQTT will payload text.</summary>
    [Reactive]
    private string _willPayload = string.Empty;

    /// <summary>Stores the MQTT will quality of service.</summary>
    [Reactive]
    private MqttQualityOfServiceLevel _willQualityOfService = MqttQualityOfServiceLevel.AtLeastOnce;

    /// <summary>Stores the MQTT will content type.</summary>
    [Reactive]
    private string _willContentType = "text/plain";

    /// <summary>Stores the MQTT will response topic.</summary>
    [Reactive]
    private string _willResponseTopic = string.Empty;

    /// <summary>Stores the MQTT will correlation data text.</summary>
    [Reactive]
    private string _willCorrelationData = string.Empty;

    /// <summary>Stores the MQTT will correlation data encoding format.</summary>
    [Reactive]
    private PayloadFormat _willCorrelationDataFormat;

    /// <summary>Stores the MQTT will payload encoding format.</summary>
    [Reactive]
    private PayloadFormat _willPayloadFormat;

    /// <summary>Stores the MQTT will message expiry seconds.</summary>
    [Reactive]
    private uint _willMessageExpirySeconds;

    /// <summary>Stores the MQTT will payload format indicator.</summary>
    [Reactive]
    private MqttPayloadFormatIndicator _willPayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData;

    /// <summary>Stores whether the MQTT will should be retained.</summary>
    [Reactive]
    private bool _willRetain;

    /// <summary>Stores the MQTT will delay seconds.</summary>
    [Reactive]
    private uint _willDelaySeconds;

    /// <summary>Gets editable MQTT user properties for the connection.</summary>
    public ObservableCollection<UserPropertyViewModel> UserProperties { get; } = [];

    /// <summary>Gets editable MQTT user properties for the will message.</summary>
    public ObservableCollection<UserPropertyViewModel> WillUserProperties { get; } = [];

    /// <summary>Gets scripted enhanced-authentication exchange steps.</summary>
    public ObservableCollection<EnhancedAuthenticationStepViewModel> EnhancedAuthenticationSteps { get; } = [];

    /// <summary>Gets selectable transport modes.</summary>
    public IReadOnlyList<MqttTransportMode> TransportModes { get; } =
        Enum.GetValues<MqttTransportMode>();

    /// <summary>Gets selectable MQTT protocol versions.</summary>
    public IReadOnlyList<MqttProtocolVersion> ProtocolVersions { get; } =
        Enum.GetValues<MqttProtocolVersion>();

    /// <summary>Gets selectable TCP address families.</summary>
    public IReadOnlyList<AddressFamily> AddressFamilies { get; } =
    [
        AddressFamily.Unspecified,
        AddressFamily.InterNetwork,
        AddressFamily.InterNetworkV6,
    ];

    /// <summary>Gets selectable TCP protocol types.</summary>
    public IReadOnlyList<ProtocolType> ProtocolTypes { get; } =
    [
        ProtocolType.Tcp,
    ];

    /// <summary>Gets selectable TLS protocol combinations.</summary>
    public IReadOnlyList<SslProtocols> SslProtocolOptions { get; } =
    [
        SslProtocols.None,
        ModernTlsProtocolCombination,
    ];

    /// <summary>Gets selectable certificate revocation modes.</summary>
    public IReadOnlyList<X509RevocationMode> RevocationModes { get; } =
        Enum.GetValues<X509RevocationMode>();

    /// <summary>Gets selectable TLS encryption policies.</summary>
    public IReadOnlyList<EncryptionPolicy> EncryptionPolicies { get; } =
        Enum.GetValues<EncryptionPolicy>();

    /// <summary>Gets selectable quality of service levels.</summary>
    public IReadOnlyList<MqttQualityOfServiceLevel> QualityOfServiceLevels { get; } =
        Enum.GetValues<MqttQualityOfServiceLevel>();

    /// <summary>Gets selectable payload format indicators.</summary>
    public IReadOnlyList<MqttPayloadFormatIndicator> PayloadFormatIndicators { get; } =
        Enum.GetValues<MqttPayloadFormatIndicator>();

    /// <summary>Gets selectable enhanced-authentication reason codes.</summary>
    public IReadOnlyList<MqttAuthenticateReasonCode> AuthenticationReasonCodes { get; } =
        Enum.GetValues<MqttAuthenticateReasonCode>();

    /// <summary>Gets selectable payload formats.</summary>
    public IReadOnlyList<PayloadFormat> PayloadFormats { get; } =
        Enum.GetValues<PayloadFormat>();

    /// <summary>Gets selectable client certificate sources.</summary>
    public IReadOnlyList<ClientCertificateSource> ClientCertificateSources =>
        ClientCertificateProvider is null ? DefaultClientCertificateSources : ProviderClientCertificateSources;

    /// <summary>Gets selectable certificate validation modes.</summary>
    public IReadOnlyList<CertificateValidationMode> CertificateValidationModes =>
        CertificateValidationHandler is null ? DefaultCertificateValidationModes : CallbackCertificateValidationModes;

    /// <summary>Gets selectable certificate selection modes.</summary>
    public IReadOnlyList<CertificateSelectionMode> CertificateSelectionModes =>
        CertificateSelectionHandler is null ? DefaultCertificateSelectionModes : CallbackCertificateSelectionModes;

    /// <summary>Gets selectable certificate store names.</summary>
    public IReadOnlyList<StoreName> ClientCertificateStoreNames { get; } =
        Enum.GetValues<StoreName>();

    /// <summary>Gets selectable certificate store locations.</summary>
    public IReadOnlyList<StoreLocation> ClientCertificateStoreLocations { get; } =
        Enum.GetValues<StoreLocation>();

    /// <summary>Gets selectable X509 certificate find types.</summary>
    public IReadOnlyList<X509FindType> ClientCertificateFindTypes { get; } =
        Enum.GetValues<X509FindType>();

    /// <summary>Gets MQTTnet callback and provider options that are now mapped through composition properties.</summary>
    public IReadOnlyList<string> CallbackOnlyOptions { get; } =
    [
        nameof(ClientOptionsConfigurator),
        nameof(TlsOptionsConfigurator),
        nameof(WebSocketOptionsConfigurator),
        nameof(StreamProvider),
        nameof(ClientCertificateProvider),
        nameof(CertificateValidationHandler),
        nameof(CertificateSelectionHandler),
        nameof(EnhancedAuthenticationHandler),
    ];

    /// <summary>Gets or sets an optional client-options composition callback.</summary>
    internal Action<MqttClientOptionsBuilder>? ClientOptionsConfigurator { get; set; }

    /// <summary>Gets or sets an optional MQTT client stream provider composition hook.</summary>
    internal IMqttClientStreamProvider? StreamProvider { get; set; }

    /// <summary>Gets or sets an optional TLS-options composition callback.</summary>
    internal Action<MqttClientTlsOptionsBuilder>? TlsOptionsConfigurator { get; set; }

    /// <summary>Gets or sets an optional WebSocket-options composition callback.</summary>
    internal Action<MqttClientWebSocketOptionsBuilder>? WebSocketOptionsConfigurator { get; set; }

    /// <summary>Gets or sets an optional client-certificate provider callback.</summary>
    internal Func<X509CertificateCollection>? ClientCertificateProvider { get; set; }

    /// <summary>Gets or sets an optional certificate validation callback.</summary>
    internal Func<MqttClientCertificateValidationEventArgs, bool>? CertificateValidationHandler { get; set; }

    /// <summary>Gets or sets an optional certificate selection callback.</summary>
    internal Func<MqttClientCertificateSelectionEventArgs, X509Certificate>? CertificateSelectionHandler { get; set; }

    /// <summary>Gets or sets an optional enhanced-authentication handler.</summary>
    internal IMqttEnhancedAuthenticationHandler? EnhancedAuthenticationHandler { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var certificate in _ownedCertificates)
        {
            certificate.Dispose();
        }

        _ownedCertificates.Clear();
    }

    /// <summary>Builds the MQTTnet client options represented by this view model.</summary>
    /// <returns>The MQTTnet client options.</returns>
    internal MqttClientOptions BuildClientOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(ClientId)
            .WithProtocolVersion(ProtocolVersion)
            .WithCleanStart(CleanStart)
            .WithSessionExpiryInterval(SessionExpirySeconds)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(Math.Max(0, KeepAliveSeconds)))
            .WithTimeout(TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds)))
            .WithReceiveMaximum(ReceiveMaximum)
            .WithTopicAliasMaximum(TopicAliasMaximum)
            .WithMaximumPacketSize(MaximumPacketSize)
            .WithRequestProblemInformation(RequestProblemInformation)
            .WithRequestResponseInformation(RequestResponseInformation)
            .WithTryPrivate(TryPrivate);

        if (DisablePacketFragmentation)
        {
            _ = builder.WithoutPacketFragmentation();
        }

        ConfigureStreamProvider(builder);
        ConfigureTransport(builder);
        ConfigureCredentials(builder);
        ConfigureEnhancedAuthentication(builder);
        ConfigureTls(builder);
        ConfigureWill(builder);
        ConfigureUserProperties(builder);
        ClientOptionsConfigurator?.Invoke(builder);
        var options = builder.Build();
        ConfigureBuiltTcpOptions(options);
        options.ValidateFeatures = ValidateFeatures;
        options.WriterBufferSize = WriterBufferSize;
        options.WriterBufferSizeMax = WriterBufferSizeMax;
        return options;
    }

    /// <summary>Parses key-colon-value header text into request headers.</summary>
    /// <param name="value">The UI header text to parse.</param>
    /// <returns>The parsed request headers.</returns>
    private static Dictionary<string, string> ParseHeaders(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return result;
    }

    /// <summary>Splits semicolon, comma, or newline separated UI text.</summary>
    /// <param name="value">The UI text to split.</param>
    /// <returns>The non-empty trimmed values.</returns>
    private static List<string> SplitList(string value)
    {
        var result = new List<string>();
        foreach (var item in value.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result.Add(item);
        }

        return result;
    }

    /// <summary>Normalizes an X509 thumbprint for comparison.</summary>
    /// <param name="thumbprint">The thumbprint to normalize.</param>
    /// <returns>The normalized thumbprint.</returns>
    private static string NormalizeThumbprint(string? thumbprint) =>
        string.IsNullOrWhiteSpace(thumbprint)
            ? string.Empty
            : thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal);

    /// <summary>Parses name-value WebSocket cookies using the WebSocket endpoint URI.</summary>
    /// <param name="value">The cookie text.</param>
    /// <param name="uri">The WebSocket URI.</param>
    /// <returns>The parsed cookie container.</returns>
    private static CookieContainer ParseCookies(string value, Uri uri)
    {
        var container = new CookieContainer();
        foreach (var line in value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            container.Add(uri, new Cookie(line[..separator].Trim(), line[(separator + 1)..].Trim()));
        }

        return container;
    }

    /// <summary>Configures a custom MQTT stream provider when one is supplied through composition.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureStreamProvider(MqttClientOptionsBuilder builder)
    {
        if (StreamProvider is not null)
        {
            _ = builder.WithStreamProvider(StreamProvider);
        }
    }

    /// <summary>Configures transport-specific MQTT client options.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureTransport(MqttClientOptionsBuilder builder)
    {
        if (TransportMode == MqttTransportMode.Tcp)
        {
            ConfigureTcpTransport(builder);
            return;
        }

        if (TransportMode == MqttTransportMode.WebSocket)
        {
            ConfigureWebSocketTransport(builder);
            return;
        }

        if (TransportMode == MqttTransportMode.Uri)
        {
            _ = builder.WithConnectionUri(new Uri(ConnectionUri));
            return;
        }

        _ = builder.WithTcpServer(Host, Port);
    }

    /// <summary>Configures MQTT username and password credentials.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureCredentials(MqttClientOptionsBuilder builder)
    {
        if (UseCredentials || Username.Length != 0 || Password.Length != 0)
        {
            _ = builder.WithCredentials(Username, MqttPayloadEncoding.BuildBytes(Password, PasswordFormat));
        }
    }

    /// <summary>Configures enhanced authentication data.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureEnhancedAuthentication(MqttClientOptionsBuilder builder)
    {
        if (EnhancedAuthenticationHandler is not null)
        {
            _ = builder.WithEnhancedAuthenticationHandler(EnhancedAuthenticationHandler);
            return;
        }

        if (EnhancedAuthenticationSteps.Count > 0)
        {
            _ = builder.WithEnhancedAuthenticationHandler(new ScriptedEnhancedAuthenticationHandler(EnhancedAuthenticationSteps));
            return;
        }

        if (!string.IsNullOrWhiteSpace(EnhancedAuthenticationMethod))
        {
            _ = builder.WithEnhancedAuthentication(
                EnhancedAuthenticationMethod,
                MqttPayloadEncoding.BuildBytes(EnhancedAuthenticationData, EnhancedAuthenticationDataFormat));
        }
    }

    /// <summary>Configures TCP transport options.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureTcpTransport(MqttClientOptionsBuilder builder)
    {
        _ = builder.WithTcpServer(Host, Port);
    }

    /// <summary>Configures advanced TCP channel options after the MQTTnet builder creates the endpoint.</summary>
    /// <param name="options">The built MQTT client options to adjust.</param>
    private void ConfigureBuiltTcpOptions(MqttClientOptions options)
    {
        if (TransportMode != MqttTransportMode.Tcp || options.ChannelOptions is not MqttClientTcpOptions tcpOptions)
        {
            return;
        }

        if (IPAddress.TryParse(Host, out var address))
        {
            tcpOptions.RemoteEndpoint = new IPEndPoint(address, Port);
            tcpOptions.AddressFamily = address.AddressFamily;
        }
        else
        {
            var addressFamily = GetTcpAddressFamily();
            tcpOptions.RemoteEndpoint = new DnsEndPoint(Host, Port, addressFamily);
            tcpOptions.AddressFamily = addressFamily;
        }

        if (TcpAddressFamily != AddressFamily.Unspecified)
        {
            tcpOptions.AddressFamily = TcpAddressFamily;
        }

        tcpOptions.ProtocolType = TcpProtocolType;
        tcpOptions.NoDelay = TcpNoDelay;
        tcpOptions.BufferSize = TcpBufferSize;
        if (TcpUseLinger)
        {
            tcpOptions.LingerState = new(TcpLingerEnabled, TcpLingerTimeSeconds);
        }

        if (tcpOptions.AddressFamily == AddressFamily.InterNetworkV6)
        {
            tcpOptions.DualMode = TcpDualMode;
        }

        if (!string.IsNullOrWhiteSpace(TcpLocalAddress))
        {
            tcpOptions.LocalEndpoint = new IPEndPoint(IPAddress.Parse(TcpLocalAddress), TcpLocalPort);
        }
    }

    /// <summary>Gets a concrete TCP address family for DNS endpoints.</summary>
    /// <returns>The configured TCP address family, or IPv4 when no explicit family is selected.</returns>
    private AddressFamily GetTcpAddressFamily() =>
        TcpAddressFamily == AddressFamily.Unspecified ? AddressFamily.InterNetwork : TcpAddressFamily;

    /// <summary>Configures TLS transport options.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureTls(MqttClientOptionsBuilder builder)
    {
        if (!UseTls)
        {
            return;
        }

        _ = builder.WithTlsOptions(options =>
        {
            _ = options
                .UseTls()
                .WithSslProtocols(SslProtocols)
                .WithAllowUntrustedCertificates(AllowUntrustedCertificates)
                .WithIgnoreCertificateChainErrors(IgnoreCertificateChainErrors)
                .WithIgnoreCertificateRevocationErrors(IgnoreCertificateRevocationErrors)
                .WithAllowRenegotiation(AllowTlsRenegotiation)
                .WithRevocationMode(RevocationMode);

            _ = options.WithCipherSuitesPolicy(EncryptionPolicy);
            AddApplicationProtocols(options);
            AddTrustChain(options);
            ConfigureCertificateValidation(options);
            ConfigureCertificateSelection(options);
            ConfigureClientCertificates(options);
            TlsOptionsConfigurator?.Invoke(options);

            if (!string.IsNullOrWhiteSpace(TlsTargetHost))
            {
                _ = options.WithTargetHost(TlsTargetHost);
            }
        });
    }

    /// <summary>Configures the MQTT will message options.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureWill(MqttClientOptionsBuilder builder)
    {
        if (WillTopic.Length == 0)
        {
            return;
        }

        _ = builder
            .WithWillTopic(WillTopic)
            .WithWillPayload(MqttPayloadEncoding.BuildBytes(WillPayload, WillPayloadFormat))
            .WithWillRetain(WillRetain)
            .WithWillDelayInterval(WillDelaySeconds)
            .WithWillQualityOfServiceLevel(WillQualityOfService)
            .WithWillPayloadFormatIndicator(WillPayloadFormatIndicator)
            .WithWillMessageExpiryInterval(WillMessageExpirySeconds);

        if (!string.IsNullOrWhiteSpace(WillContentType))
        {
            _ = builder.WithWillContentType(WillContentType);
        }

        if (!string.IsNullOrWhiteSpace(WillResponseTopic))
        {
            _ = builder.WithWillResponseTopic(WillResponseTopic);
        }

        if (!string.IsNullOrWhiteSpace(WillCorrelationData))
        {
            _ = builder.WithWillCorrelationData(MqttPayloadEncoding.BuildBytes(WillCorrelationData, WillCorrelationDataFormat));
        }

        foreach (var property in WillUserProperties)
        {
            if (property.IsValid)
            {
                _ = builder.WithWillUserProperty(property.Name, Encoding.UTF8.GetBytes(property.Value).AsMemory());
            }
        }
    }

    /// <summary>Configures MQTT user properties for the connection.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureUserProperties(MqttClientOptionsBuilder builder)
    {
        foreach (var property in UserProperties)
        {
            if (property.IsValid)
            {
                _ = builder.WithUserProperty(property.Name, Encoding.UTF8.GetBytes(property.Value).AsMemory());
            }
        }
    }

    /// <summary>Configures TLS certificate selection behavior.</summary>
    /// <param name="options">The TLS options to configure.</param>
    private void ConfigureCertificateSelection(MqttClientTlsOptionsBuilder options)
    {
        if (CertificateSelectionMode == CertificateSelectionMode.Callback && CertificateSelectionHandler is not null)
        {
            _ = options.WithCertificateSelectionHandler(CertificateSelectionHandler);
            return;
        }

        if (CertificateSelectionMode == CertificateSelectionMode.First)
        {
            _ = options.WithCertificateSelectionHandler(static args => ConnectionOptionHelpers.SelectFirstCertificate(args.LocalCertificates));
            return;
        }

        if (CertificateSelectionMode == CertificateSelectionMode.Thumbprint)
        {
            if (string.IsNullOrWhiteSpace(SelectedClientCertificateThumbprint))
            {
                throw new InvalidOperationException("Client certificate thumbprint selection requires a thumbprint.");
            }

            _ = options.WithCertificateSelectionHandler(args => SelectCertificateByThumbprint(args.LocalCertificates));
        }
    }

    /// <summary>Configures TLS certificate validation behavior.</summary>
    /// <param name="options">The TLS options to configure.</param>
    private void ConfigureCertificateValidation(MqttClientTlsOptionsBuilder options)
    {
        if (CertificateValidationMode == CertificateValidationMode.AllowAll)
        {
            _ = options.WithCertificateValidationHandler(static _ => true);
            return;
        }

        if (CertificateValidationMode == CertificateValidationMode.PinnedThumbprint)
        {
            if (string.IsNullOrWhiteSpace(PinnedServerCertificateThumbprint))
            {
                throw new InvalidOperationException("Pinned server certificate validation requires a thumbprint.");
            }

            _ = options.WithCertificateValidationHandler(IsPinnedServerCertificate);
            return;
        }

        if (CertificateValidationMode == CertificateValidationMode.RejectAll)
        {
            _ = options.WithCertificateValidationHandler(static _ => false);
            return;
        }

        if (CertificateValidationMode == CertificateValidationMode.Callback && CertificateValidationHandler is not null)
        {
            _ = options.WithCertificateValidationHandler(CertificateValidationHandler);
        }
    }

    /// <summary>Configures TLS client certificate loading.</summary>
    /// <param name="options">The TLS options to configure.</param>
    private void ConfigureClientCertificates(MqttClientTlsOptionsBuilder options)
    {
        if (ClientCertificateSource == ClientCertificateSource.Provider && ClientCertificateProvider is not null)
        {
            _ = options.WithClientCertificatesProvider(new DelegateMqttClientCertificatesProvider(ClientCertificateProvider));
            return;
        }

        var certificates = ClientCertificateSource switch
        {
            ClientCertificateSource.File => LoadFileCertificates(),
            ClientCertificateSource.Store => LoadStoreCertificates(),
            _ => [],
        };
        if (certificates.Count > 0)
        {
            _ = options.WithClientCertificates(certificates);
        }
    }

    /// <summary>Loads configured PKCS12 client certificate files.</summary>
    /// <returns>The loaded client certificates.</returns>
    private X509Certificate2Collection LoadFileCertificates()
    {
        var certificates = new X509Certificate2Collection();
        if (string.IsNullOrWhiteSpace(ClientCertificatePath))
        {
            return certificates;
        }

        var certificate = string.IsNullOrEmpty(ClientCertificatePassword)
            ? X509CertificateLoader.LoadPkcs12FromFile(ClientCertificatePath, null)
            : X509CertificateLoader.LoadPkcs12FromFile(ClientCertificatePath, ClientCertificatePassword);
        _ownedCertificates.Add(certificate);
        _ = certificates.Add(certificate);
        return certificates;
    }

    /// <summary>Loads configured client certificates from an operating-system store.</summary>
    /// <returns>The loaded client certificates.</returns>
    private X509Certificate2Collection LoadStoreCertificates()
    {
        using var store = new X509Store(ClientCertificateStoreName, ClientCertificateStoreLocation);
        store.Open(OpenFlags.ReadOnly);
        var certificates = store.Certificates.Find(ClientCertificateFindType, ClientCertificateFindValue, !ClientCertificateAllowInvalid);
        foreach (var certificate in certificates)
        {
            _ownedCertificates.Add(certificate);
        }

        return certificates;
    }

    /// <summary>Adds TLS application protocols to the TLS options.</summary>
    /// <param name="options">The TLS options to configure.</param>
    private void AddApplicationProtocols(MqttClientTlsOptionsBuilder options)
    {
        var protocols = new List<SslApplicationProtocol>();
        foreach (var protocol in SplitList(TlsApplicationProtocols))
        {
            protocols.Add(new(protocol));
        }

        if (protocols.Count > 0)
        {
            _ = options.WithApplicationProtocols(protocols);
        }
    }

    /// <summary>Adds custom trust chain certificates to the TLS options.</summary>
    /// <param name="options">The TLS options to configure.</param>
    private void AddTrustChain(MqttClientTlsOptionsBuilder options)
    {
        var certificatePaths = SplitList(TrustChainCertificatePaths);
        if (certificatePaths.Count == 0)
        {
            return;
        }

        var certificates = new X509Certificate2Collection();
        foreach (var path in certificatePaths)
        {
            _ = certificates.Add(X509CertificateLoader.LoadCertificateFromFile(path));
        }

        _ = options.WithTrustChain(certificates);
    }

    /// <summary>Validates a server certificate against the pinned thumbprint.</summary>
    /// <param name="args">The certificate validation event arguments.</param>
    /// <returns><see langword="true"/> when the server certificate matches the configured pin.</returns>
    private bool IsPinnedServerCertificate(MqttClientCertificateValidationEventArgs args) =>
        args.Certificate is X509Certificate2 certificate &&
        string.Equals(
            NormalizeThumbprint(certificate.Thumbprint),
            NormalizeThumbprint(PinnedServerCertificateThumbprint),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Selects a local certificate by configured thumbprint.</summary>
    /// <param name="certificates">The available local certificates.</param>
    /// <returns>The selected certificate.</returns>
    private X509Certificate SelectCertificateByThumbprint(X509CertificateCollection certificates)
    {
        foreach (var certificate in certificates)
        {
            if (certificate is X509Certificate2 certificate2 &&
                string.Equals(
                    NormalizeThumbprint(certificate2.Thumbprint),
                    NormalizeThumbprint(SelectedClientCertificateThumbprint),
                    StringComparison.OrdinalIgnoreCase))
            {
                return certificate;
            }
        }

        throw new InvalidOperationException("No local client certificate matched the selected thumbprint.");
    }

    /// <summary>Configures WebSocket transport options.</summary>
    /// <param name="builder">The MQTT client options builder to configure.</param>
    private void ConfigureWebSocketTransport(MqttClientOptionsBuilder builder)
    {
        _ = builder.WithWebSocketServer(options =>
        {
            _ = options
                .WithUri(WebSocketUri)
                .WithKeepAliveInterval(TimeSpan.FromSeconds(Math.Max(0, WebSocketKeepAliveSeconds)))
                .WithUseDefaultCredentials(WebSocketUseDefaultCredentials);

            ConfigureWebSocketCredentials(options);
            ConfigureWebSocketMetadata(options);
            ConfigureWebSocketProxy(options);
            ConfigureWebSocketCookies(options);
            ConfigureWebSocketDeflate(options);
            WebSocketOptionsConfigurator?.Invoke(options);
        });
    }

    /// <summary>Configures WebSocket endpoint credentials.</summary>
    /// <param name="options">The WebSocket options to configure.</param>
    private void ConfigureWebSocketCredentials(MqttClientWebSocketOptionsBuilder options)
    {
        if (string.IsNullOrWhiteSpace(WebSocketCredentialUsername))
        {
            return;
        }

        _ = options.WithCookieContainer(new NetworkCredential(
            WebSocketCredentialUsername,
            WebSocketCredentialPassword,
            WebSocketCredentialDomain));
    }

    /// <summary>Configures WebSocket headers and subprotocol metadata.</summary>
    /// <param name="options">The WebSocket options to configure.</param>
    private void ConfigureWebSocketMetadata(MqttClientWebSocketOptionsBuilder options)
    {
        var subProtocols = SplitList(WebSocketSubProtocols);
        if (subProtocols.Count > 0)
        {
            _ = options.WithSubProtocols(subProtocols);
        }

        var headers = ParseHeaders(WebSocketHeaders);
        if (headers.Count > 0)
        {
            _ = options.WithRequestHeaders(headers);
        }
    }

    /// <summary>Configures WebSocket cookies.</summary>
    /// <param name="options">The WebSocket options to configure.</param>
    private void ConfigureWebSocketCookies(MqttClientWebSocketOptionsBuilder options)
    {
        var cookies = ParseCookies(WebSocketCookies, new(WebSocketUri));
        if (cookies.Count > 0)
        {
            _ = options.WithCookieContainer(cookies);
        }
    }

    /// <summary>Configures WebSocket deflate options.</summary>
    /// <param name="options">The WebSocket options to configure.</param>
    private void ConfigureWebSocketDeflate(MqttClientWebSocketOptionsBuilder options)
    {
        if (!UseWebSocketDeflate)
        {
            return;
        }

        ConnectionOptionHelpers.ValidateWebSocketDeflateWindowBits(WebSocketDeflateClientMaxWindowBits, nameof(WebSocketDeflateClientMaxWindowBits));
        ConnectionOptionHelpers.ValidateWebSocketDeflateWindowBits(WebSocketDeflateServerMaxWindowBits, nameof(WebSocketDeflateServerMaxWindowBits));
        _ = options.WithDangerousDeflateOptions(new()
        {
            ClientContextTakeover = WebSocketDeflateClientContextTakeover,
            ClientMaxWindowBits = WebSocketDeflateClientMaxWindowBits,
            ServerContextTakeover = WebSocketDeflateServerContextTakeover,
            ServerMaxWindowBits = WebSocketDeflateServerMaxWindowBits,
        });
    }

    /// <summary>Configures WebSocket proxy options.</summary>
    /// <param name="options">The WebSocket options to configure.</param>
    private void ConfigureWebSocketProxy(MqttClientWebSocketOptionsBuilder options)
    {
        if (string.IsNullOrWhiteSpace(WebSocketProxyAddress))
        {
            return;
        }

        _ = options.WithProxyOptions(proxy =>
        {
            _ = proxy
                .WithAddress(WebSocketProxyAddress)
                .WithUsername(WebSocketProxyUsername)
                .WithPassword(WebSocketProxyPassword)
                .WithDomain(WebSocketProxyDomain)
                .WithBypassOnLocal(WebSocketProxyBypassOnLocal)
                .WithUseDefaultCredentials(WebSocketProxyUseDefaultCredentials);

            var bypassList = SplitList(WebSocketProxyBypassList);
            if (bypassList.Count > 0)
            {
                _ = proxy.WithBypassList(bypassList);
            }
        });
    }
}
