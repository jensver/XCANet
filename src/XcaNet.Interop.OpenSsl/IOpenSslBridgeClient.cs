using XcaNet.Contracts.Results;

namespace XcaNet.Interop.OpenSsl;

public interface IOpenSslBridgeClient
{
    OpenSslDiagnosticsSnapshot Diagnostics { get; }

    OperationResult<OpenSslDiagnosticsSnapshot> Probe();

    OperationResult SelfTest();

    OperationResult<byte[]> SignCertificateSigningRequest(OpenSslSignCertificateSigningRequestRequest request);
}
