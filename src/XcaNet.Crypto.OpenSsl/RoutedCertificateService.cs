using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;
using XcaNet.Crypto.Abstractions;
using XcaNet.Crypto.DotNet;

namespace XcaNet.Crypto.OpenSsl;

public sealed class RoutedCertificateService : ICertificateService
{
    private readonly DotNetCryptoBackend _managedBackend;
    private readonly OpenSslCryptoBackend _openSslBackend;
    private readonly CryptoBackendRoutingOptions _options;

    public RoutedCertificateService(DotNetCryptoBackend managedBackend, OpenSslCryptoBackend openSslBackend, CryptoBackendRoutingOptions options)
    {
        _managedBackend = managedBackend;
        _openSslBackend = openSslBackend;
        _options = options;
    }

    public Task<OperationResult<SignedCertificateResult>> CreateSelfSignedCaAsync(SelfSignedCaCertificateRequest request, CancellationToken cancellationToken)
        => _managedBackend.CreateSelfSignedCaAsync(request, cancellationToken);

    public async Task<OperationResult<SignedCertificateResult>> SignCertificateSigningRequestAsync(SignCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        var decision = new CryptoBackendRoutingPolicy(_options, _openSslBackend.Diagnostics)
            .SelectCertificateSigningBackend(request.PreferredBackend);

        if (decision.BackendToUse == CryptoBackendKind.OpenSsl)
        {
            var openSslResult = await _openSslBackend.SignCertificateSigningRequestAsync(request, cancellationToken);
            if (openSslResult.IsSuccess)
            {
                return openSslResult;
            }

            if (decision.Preference == CryptoBackendPreference.OpenSslOnly)
            {
                return openSslResult;
            }
        }

        return await _managedBackend.SignCertificateSigningRequestAsync(
            request with { PreferredBackend = CryptoBackendPreference.PreferManaged },
            cancellationToken);
    }

    public Task<OperationResult<CertificateDetails>> ParseCertificateAsync(CertificateParseRequest request, CancellationToken cancellationToken)
        => _managedBackend.ParseCertificateAsync(request, cancellationToken);

    public Task<OperationResult<CertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListRequest request, CancellationToken cancellationToken)
        => _managedBackend.GenerateCertificateRevocationListAsync(request, cancellationToken);

    public Task<OperationResult<CertificateRevocationListDetails>> ParseCertificateRevocationListAsync(CertificateRevocationListParseRequest request, CancellationToken cancellationToken)
        => _managedBackend.ParseCertificateRevocationListAsync(request, cancellationToken);
}
