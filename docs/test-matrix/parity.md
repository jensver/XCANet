# Parity Coverage

M7 expands parity from a single happy-path signing test into a compatibility matrix with fixture-backed evidence.

## Fixture Categories

- baseline CSR signing
- SAN-heavy and extension-rich CSR signing
- malformed CSR input
- managed PKCS#12 export inspected by OpenSSL CLI
- managed CRL generation inspected by OpenSSL CLI
- missing-bridge fallback scenarios
- managed-default routing with OpenSSL present

## Compatibility Matrix

| Operation | Status | Managed | OpenSSL | Fallback Behavior | Coverage |
| --- | --- | --- | --- | --- | --- |
| Key generation | managed-only | supported | not implemented | not applicable | `DotNetManagedCryptoTests.GenerateAsync_WithWeakRsaKeySize_ShouldFail` |
| Self-signed CA creation | managed-only | supported | not implemented | not applicable | `DotNetManagedCryptoTests.CreateSelfSignedCaAsync_ShouldProduceParseableCertificate` |
| CSR creation | managed-only | supported | not implemented | not applicable | `DotNetManagedCryptoTests.CreateAndParseCertificateSigningRequest_ShouldRoundTripSubjectAndSans` |
| CSR signing | openssl-supported | supported | supported | managed remains default, OpenSSL used only when requested/capable, managed fallback when unavailable | `OpenSslSigningParityTests.SignCertificateSigningRequest_ShouldPreserveCoreFieldsAcrossManagedAndOpenSslPaths`, `OpenSslSigningParityTests.SignCertificateSigningRequest_WithSanHeavyExtensionRichCsr_ShouldPreserveNormalizedExtensions`, `OpenSslIntegrationTests.*` |
| Certificate parsing | managed-only | supported | not implemented separately | OpenSSL output is normalized through managed parsing | covered indirectly by all signing parity tests |
| PKCS#12 / PFX export | managed-only | supported | not implemented | managed only | `OpenSslSigningParityTests.ManagedPkcs12Export_ShouldBeReadableByOpenSslCli` |
| PKCS#12 / PFX import | managed-only | supported | not implemented | managed only | `DotNetManagedCryptoTests.ImportAsync_WithMalformedPkcs12Bundle_ShouldFail`, `DotNetManagedCryptoTests.ImportAsync_WithWrongPkcs12Password_ShouldFail` |
| CRL generation | managed-only | supported | not implemented | managed only | `OpenSslSigningParityTests.ManagedGeneratedCrl_ShouldBeReadableByOpenSslCliAndContainRevokedSerials` |
| CRL parsing | managed-only | supported | not implemented separately | managed only | existing revocation integration tests plus OpenSSL CLI CRL inspection |

## Known Differences

- CSR signing is the only operation currently backed by both paths.
- Managed and OpenSSL certificate serials and validity timestamps are intentionally treated as non-deterministic and are normalized out of parity assertions.
- Extension-rich CSR signing currently preserves SAN, KU, EKU, issuer, and CA/leaf status across both paths for the tested fixtures.
- PKCS#12 and CRL operations are still managed-only, but the generated artifacts were verified as readable by the OpenSSL CLI in M7 tests.

## Candidate Evaluation Outcome

M7 evaluated three likely next OpenSSL candidates:

| Candidate | Evidence | Outcome |
| --- | --- | --- |
| PKCS#12 / PFX import/export | Managed-generated PFX bundles are readable by OpenSSL CLI; wrong-password and malformed bundle handling are deterministic | no new OpenSSL backend operation added |
| Advanced extension encoding | Extension-rich CSR signing parity passed for SAN, KU, and EKU in the tested fixtures | no new OpenSSL backend operation added |
| CRL generation fidelity | Managed-generated CRLs are readable by OpenSSL CLI and include revoked serials as expected | no new OpenSSL backend operation added |

No second OpenSSL-backed operation was added in M7 because the new parity evidence did not show a concrete managed-backend insufficiency.
