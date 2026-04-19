using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;

namespace XcaNet.Crypto.Abstractions;

public interface ICertificateService
{
    Task<OperationResult<SignedCertificateResult>> CreateSelfSignedCaAsync(SelfSignedCaCertificateRequest request, CancellationToken cancellationToken);

    Task<OperationResult<SignedCertificateResult>> SignCertificateSigningRequestAsync(SignCertificateSigningRequestRequest request, CancellationToken cancellationToken);

    Task<OperationResult<CertificateDetails>> ParseCertificateAsync(CertificateParseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<CertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListRequest request, CancellationToken cancellationToken);

    Task<OperationResult<CertificateRevocationListDetails>> ParseCertificateRevocationListAsync(CertificateRevocationListParseRequest request, CancellationToken cancellationToken);
}
