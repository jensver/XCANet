using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;

namespace XcaNet.Crypto.Abstractions;

public interface ICertificateSigningRequestService
{
    Task<OperationResult<CertificateSigningRequestResult>> CreateAsync(CreateCertificateSigningRequestRequest request, CancellationToken cancellationToken);

    Task<OperationResult<CertificateSigningRequestDetails>> ParseAsync(CertificateSigningRequestParseRequest request, CancellationToken cancellationToken);
}
