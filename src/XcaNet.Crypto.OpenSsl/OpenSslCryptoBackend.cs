using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;
using XcaNet.Crypto.Abstractions;
using XcaNet.Crypto.DotNet;
using XcaNet.Interop.OpenSsl;

namespace XcaNet.Crypto.OpenSsl;

public sealed class OpenSslCryptoBackend : ICertificateService
{
    private readonly DotNetCryptoBackend _managedBackend;
    private readonly IOpenSslBridgeClient _bridgeClient;

    public OpenSslCryptoBackend(DotNetCryptoBackend managedBackend, IOpenSslBridgeClient bridgeClient)
    {
        _managedBackend = managedBackend;
        _bridgeClient = bridgeClient;
    }

    public string Name => "OpenSSL";

    public OpenSslDiagnosticsSnapshot Diagnostics => _bridgeClient.Diagnostics;

    public Task<OperationResult<SignedCertificateResult>> CreateSelfSignedCaAsync(SelfSignedCaCertificateRequest request, CancellationToken cancellationToken)
        => _managedBackend.CreateSelfSignedCaAsync(request, cancellationToken);

    public async Task<OperationResult<SignedCertificateResult>> SignCertificateSigningRequestAsync(SignCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Diagnostics.IsAvailable)
        {
            return OperationResult<SignedCertificateResult>.Failure(OperationErrorCode.ValidationFailed, Diagnostics.LastLoadError ?? "OpenSSL backend is unavailable.");
        }

        if (!Diagnostics.Capabilities.HasFlag(OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning))
        {
            return OperationResult<SignedCertificateResult>.Failure(OperationErrorCode.ValidationFailed, "OpenSSL backend does not support CSR signing.");
        }

        var signResult = _bridgeClient.SignCertificateSigningRequest(
            new OpenSslSignCertificateSigningRequestRequest(
                request.CertificateSigningRequestDer,
                request.IssuerCertificateDer,
                request.IssuerPrivateKeyPkcs8,
                request.ValidityDays));

        if (!signResult.IsSuccess || signResult.Value is null)
        {
            return OperationResult<SignedCertificateResult>.Failure(signResult.ErrorCode, signResult.Message);
        }

        var parseResult = await _managedBackend.ParseCertificateAsync(new CertificateParseRequest(signResult.Value, CryptoDataFormat.Der), cancellationToken);
        if (!parseResult.IsSuccess || parseResult.Value is null)
        {
            return OperationResult<SignedCertificateResult>.Failure(parseResult.ErrorCode, parseResult.Message);
        }

        return OperationResult<SignedCertificateResult>.Success(
            new SignedCertificateResult(signResult.Value, parseResult.Value, CryptoBackendKind.OpenSsl),
            "Certificate signing request issued through OpenSSL.");
    }

    public Task<OperationResult<CertificateDetails>> ParseCertificateAsync(CertificateParseRequest request, CancellationToken cancellationToken)
        => _managedBackend.ParseCertificateAsync(request, cancellationToken);

    public Task<OperationResult<CertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListRequest request, CancellationToken cancellationToken)
        => _managedBackend.GenerateCertificateRevocationListAsync(request, cancellationToken);

    public Task<OperationResult<CertificateRevocationListDetails>> ParseCertificateRevocationListAsync(CertificateRevocationListParseRequest request, CancellationToken cancellationToken)
        => _managedBackend.ParseCertificateRevocationListAsync(request, cancellationToken);
}
