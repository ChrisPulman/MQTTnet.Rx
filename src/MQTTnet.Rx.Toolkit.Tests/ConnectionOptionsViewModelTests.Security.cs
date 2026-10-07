// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Rx.Toolkit.Models;
using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies TLS policies, certificate loading and certificate ownership.</summary>
public sealed partial class ConnectionOptionsViewModelTests
{
    /// <summary>Stores the certificate target host used by policy tests.</summary>
    private const string CertificateTargetHost = "tls.example";

    /// <summary>Stores the expected number of provider invocations.</summary>
    private const int ExpectedCertificateProviderCalls = 2;

    /// <summary>Stores the test certificate RSA key size.</summary>
    private const int TestCertificateKeySize = 2048;

    /// <summary>Stores the entropy size for ephemeral PKCS12 passwords.</summary>
    private const int TestCertificatePasswordBytes = 32;

    /// <summary>Verifies configured TLS policy and composition callbacks reach MQTTnet.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MapsTlsPolicyAndCompositionAsync()
    {
        var selectedProtocol = Enum.Parse<SslProtocols>("Tls13");
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            TlsTargetHost = CertificateTargetHost,
            SslProtocols = selectedProtocol,
            RevocationMode = X509RevocationMode.Offline,
            AllowTlsRenegotiation = true,
            AllowUntrustedCertificates = true,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            TlsApplicationProtocols = " mqtt; custom, mqttv5\r\n",
            TlsOptionsConfigurator = static builder => builder.WithRevocationMode(X509RevocationMode.NoCheck),
        };
        var tls = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions;
        await Assert.That(tls.UseTls).IsTrue();
        await Assert.That(tls.TargetHost).IsEqualTo(CertificateTargetHost);
        await Assert.That(tls.SslProtocol).IsEqualTo(selectedProtocol);
        await Assert.That(tls.RevocationMode).IsEqualTo(X509RevocationMode.NoCheck);
        await Assert.That(tls.AllowRenegotiation).IsTrue();
        await Assert.That(tls.AllowUntrustedCertificates).IsTrue();
        await Assert.That(tls.IgnoreCertificateChainErrors).IsTrue();
        await Assert.That(tls.IgnoreCertificateRevocationErrors).IsTrue();
        await Assert.That(tls.ApplicationProtocols).IsEquivalentTo(new[] { new SslApplicationProtocol("mqtt"), new SslApplicationProtocol("custom"), new SslApplicationProtocol("mqttv5") });
    }

    /// <summary>Verifies explicit validation modes accept or reject the supplied server certificate.</summary>
    /// <param name="expected">The validation decision.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task BuildClientOptions_ExplicitCertificatePolicyDecidesAsync(bool expected)
    {
        var mode = expected ? CertificateValidationMode.AllowAll : CertificateValidationMode.RejectAll;
        using var connection = new ConnectionOptionsViewModel { UseTls = true, CertificateValidationMode = mode };
        using var certificate = CreateOptionsCertificate();
        using var chain = new X509Chain();
        var tcp = (MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!;
        var args = new MqttClientCertificateValidationEventArgs(certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors, tcp);
        await Assert.That(tcp.TlsOptions.CertificateValidationHandler(args)).IsEqualTo(expected);
    }

    /// <summary>Verifies thumbprint pinning tolerates presentation spaces and case while rejecting another certificate.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_PinnedCertificateMatchesOnlyConfiguredThumbprintAsync()
    {
        using var certificate = CreateOptionsCertificate();
        using var otherCertificate = CreateOptionsCertificate();
        using var chain = new X509Chain();
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateValidationMode = CertificateValidationMode.PinnedThumbprint,
            PinnedServerCertificateThumbprint = $" {certificate.Thumbprint.ToLowerInvariant()} ",
        };
        var tcp = (MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!;
        var validate = tcp.TlsOptions.CertificateValidationHandler;
        await Assert.That(validate(new(certificate, chain, SslPolicyErrors.None, tcp))).IsTrue();
        await Assert.That(validate(new(otherCertificate, chain, SslPolicyErrors.None, tcp))).IsFalse();
        await Assert.That(validate(new(null!, chain, SslPolicyErrors.RemoteCertificateNotAvailable, tcp))).IsFalse();
        connection.PinnedServerCertificateThumbprint = " ";
        await Assert.That(validate(new(certificate, chain, SslPolicyErrors.None, tcp))).IsFalse();
    }

    /// <summary>Verifies injected validation and selection callbacks are preserved and invoked.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_PreservesCertificateCallbacksAsync()
    {
        using var certificate = CreateOptionsCertificate();
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateValidationMode = CertificateValidationMode.Callback,
            CertificateValidationHandler = static args => args.SslPolicyErrors == SslPolicyErrors.None,
            CertificateSelectionMode = CertificateSelectionMode.Callback,
            CertificateSelectionHandler = args => certificate,
        };
        var tcp = (MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!;
        using var chain = new X509Chain();
        await Assert.That(tcp.TlsOptions.CertificateValidationHandler).IsSameReferenceAs(connection.CertificateValidationHandler);
        await Assert.That(tcp.TlsOptions.CertificateValidationHandler(new(certificate, chain, SslPolicyErrors.None, tcp))).IsTrue();
        await Assert.That(tcp.TlsOptions.CertificateValidationHandler(new(certificate, chain, SslPolicyErrors.RemoteCertificateNameMismatch, tcp))).IsFalse();
        await Assert.That(tcp.TlsOptions.CertificateSelectionHandler).IsSameReferenceAs(connection.CertificateSelectionHandler);
        await Assert.That(tcp.TlsOptions.CertificateSelectionHandler(new(CertificateTargetHost, [], certificate, [], tcp))).IsSameReferenceAs(certificate);
    }

    /// <summary>Verifies unavailable callback modes retain system validation and automatic selection.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_UnavailableCertificateCallbacksUseSystemDefaultsAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateValidationMode = CertificateValidationMode.Callback,
            CertificateSelectionMode = CertificateSelectionMode.Callback,
            ClientCertificateSource = ClientCertificateSource.Provider,
            TlsApplicationProtocols = " ;, ",
        };
        var tls = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions;
        await Assert.That(tls.CertificateValidationHandler).IsNull();
        await Assert.That(tls.CertificateSelectionHandler).IsNull();
        await Assert.That(tls.ClientCertificatesProvider).IsNull();
        await Assert.That(tls.ApplicationProtocols).IsNull();
    }

    /// <summary>Verifies client certificate selection returns the first or pinned certificate and fails when unavailable.</summary>
    /// <param name="selectFirst">Whether to select the first certificate.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task BuildClientOptions_SelectsClientCertificateOrReportsMissingMatchAsync(bool selectFirst)
    {
        var mode = selectFirst ? CertificateSelectionMode.First : CertificateSelectionMode.Thumbprint;
        using var first = CreateOptionsCertificate();
        using var selected = CreateOptionsCertificate();
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            CertificateSelectionMode = mode,
            SelectedClientCertificateThumbprint = $" {selected.Thumbprint.ToLowerInvariant()} ",
        };
        var tcp = (MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!;
        var select = tcp.TlsOptions.CertificateSelectionHandler;
        var expected = mode == CertificateSelectionMode.First ? first : selected;
        await Assert.That(select(new(CertificateTargetHost, [first, selected], first, [], tcp))).IsSameReferenceAs(expected);
        await Assert.That(() => select(new(CertificateTargetHost, [], first, [], tcp))).Throws<InvalidOperationException>();
        if (mode == CertificateSelectionMode.Thumbprint)
        {
            await Assert.That(() => select(new(CertificateTargetHost, [first], first, [], tcp))).Throws<InvalidOperationException>();
        }
    }

    /// <summary>Verifies certificate providers remain lazy and may supply fresh certificates for each connection.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_CertificateProviderIsLazyAndReloadedAsync()
    {
        using var certificate = CreateOptionsCertificate();
        var calls = 0;
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            ClientCertificateSource = ClientCertificateSource.Provider,
            ClientCertificateProvider = () =>
            {
                calls++;
                return calls == 1 ? [] : [certificate];
            },
        };
        var provider = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.ClientCertificatesProvider;
        await Assert.That(calls).IsEqualTo(0);
        await Assert.That(provider.GetCertificates().Count).IsEqualTo(0);
        await Assert.That(provider.GetCertificates()[0]).IsSameReferenceAs(certificate);
        await Assert.That(calls).IsEqualTo(ExpectedCertificateProviderCalls);
    }

    /// <summary>Verifies PKCS12 files are loaded with either an empty or explicit password and owned certificates are disposed.</summary>
    /// <param name="usePassword">Whether to protect the PKCS12 file with a password.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task BuildClientOptions_LoadsAndOwnsPkcs12CertificatesAsync(bool usePassword)
    {
        var password = usePassword ? Convert.ToHexString(RandomNumberGenerator.GetBytes(TestCertificatePasswordBytes)) : string.Empty;
        using var certificate = CreateOptionsCertificate();
        var path = Path.Combine(Path.GetTempPath(), $"mqtt-options-{Guid.NewGuid():N}.pfx");
        try
        {
            await File.WriteAllBytesAsync(path, certificate.Export(X509ContentType.Pkcs12, password));
            using var connection = new ConnectionOptionsViewModel
            {
                UseTls = true,
                ClientCertificateSource = ClientCertificateSource.File,
                ClientCertificatePath = path,
                ClientCertificatePassword = password,
            };
            var provider = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.ClientCertificatesProvider;
            var loaded = await Assert.That(provider.GetCertificates()[0]).IsTypeOf<X509Certificate2>() ?? throw new InvalidOperationException("Loaded certificate missing.");
            await Assert.That(loaded.Thumbprint).IsEqualTo(certificate.Thumbprint);
            await Assert.That(loaded.HasPrivateKey).IsTrue();
            connection.Dispose();
            await Assert.That(loaded.Handle).IsEqualTo(IntPtr.Zero);
            connection.Dispose();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Verifies an empty certificate file selection yields no client certificate.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_EmptyCertificateFilePathHasNoCertificatesAsync()
    {
        using var connection = new ConnectionOptionsViewModel { UseTls = true, ClientCertificateSource = ClientCertificateSource.File };
        var provider = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.ClientCertificatesProvider;
        await Assert.That(provider).IsNull();
    }

    /// <summary>Verifies a store query with no match does not create a client certificate provider.</summary>
    /// <param name="allowInvalid">Whether expired certificates may match the query.</param>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task BuildClientOptions_UnmatchedReadOnlyStoreQueryHasNoCertificatesAsync(bool allowInvalid)
    {
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            ClientCertificateSource = ClientCertificateSource.Store,
            ClientCertificateStoreLocation = StoreLocation.CurrentUser,
            ClientCertificateStoreName = StoreName.My,
            ClientCertificateFindType = X509FindType.FindBySubjectName,
            ClientCertificateFindValue = $"mqtt-options-missing-{Guid.NewGuid():N}",
            ClientCertificateAllowInvalid = allowInvalid,
        };
        var provider = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.ClientCertificatesProvider;
        await Assert.That(provider).IsNull();
    }

    /// <summary>Verifies incorrect PKCS12 passwords are reported instead of using an unusable certificate.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_RejectsIncorrectPkcs12PasswordAsync()
    {
        using var certificate = CreateOptionsCertificate();
        var path = Path.Combine(Path.GetTempPath(), $"mqtt-options-{Guid.NewGuid():N}.pfx");
        try
        {
            var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(TestCertificatePasswordBytes));
            await File.WriteAllBytesAsync(path, certificate.Export(X509ContentType.Pkcs12, password));
            using var connection = new ConnectionOptionsViewModel
            {
                UseTls = true,
                ClientCertificateSource = ClientCertificateSource.File,
                ClientCertificatePath = path,
                ClientCertificatePassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(TestCertificatePasswordBytes)),
            };
            await Assert.That(connection.BuildClientOptions).Throws<CryptographicException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Verifies missing certificate files are reported to the caller.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_MissingCertificateFileFailsAsync()
    {
        using var connection = new ConnectionOptionsViewModel
        {
            UseTls = true,
            ClientCertificateSource = ClientCertificateSource.File,
            ClientCertificatePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pfx"),
        };
        await Assert.That(connection.BuildClientOptions).Throws<CryptographicException>();
    }

    /// <summary>Verifies configured public certificate files become the custom trust chain.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task BuildClientOptions_LoadsCustomTrustChainAsync()
    {
        using var certificate = CreateOptionsCertificate();
        var path = Path.Combine(Path.GetTempPath(), $"mqtt-options-{Guid.NewGuid():N}.cer");
        try
        {
            await File.WriteAllBytesAsync(path, certificate.Export(X509ContentType.Cert));
            using var connection = new ConnectionOptionsViewModel { UseTls = true, TrustChainCertificatePaths = path };
            var chain = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.TrustChain;
            var secondChain = ((MqttClientTcpOptions)connection.BuildClientOptions().ChannelOptions!).TlsOptions.TrustChain;
            await Assert.That(chain.Count).IsEqualTo(1);
            await Assert.That(chain[0].Thumbprint).IsEqualTo(certificate.Thumbprint);
            await Assert.That(chain[0].HasPrivateKey).IsFalse();
            await Assert.That(secondChain[0].Thumbprint).IsEqualTo(certificate.Thumbprint);
            await Assert.That(secondChain[0]).IsNotSameReferenceAs(chain[0]);
            connection.Dispose();
            await Assert.That(chain[0].Handle).IsEqualTo(IntPtr.Zero);
            await Assert.That(secondChain[0].Handle).IsEqualTo(IntPtr.Zero);
            connection.Dispose();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Creates a temporary self-signed certificate without writing to the certificate store.</summary>
    /// <returns>The caller-owned certificate.</returns>
    private static X509Certificate2 CreateOptionsCertificate()
    {
        using var rsa = RSA.Create(TestCertificateKeySize);
        var request = new CertificateRequest("CN=mqtt-options-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        return request.CreateSelfSigned(now.AddMinutes(-1), now.AddHours(1));
    }
}
