# Crypto Backends

XCANet now supports two crypto backends behind the existing application-facing contracts:

- `DotNetCryptoBackend`
- `OpenSslCryptoBackend`

## Managed Backend

The managed backend remains the default and implements the full workflow surface currently used by the application:

- key generation
- self-signed CA creation
- CSR creation
- certificate parsing
- import/export
- CRL generation/parsing

This keeps the application functional even when no native bridge is present.

## OpenSSL Backend

The OpenSSL backend remains intentionally narrow in M7.

It currently implements exactly one real operation through the native bridge:

- sign CSR into certificate

All other certificate-service operations continue through the managed backend.

## Backend Roles

- managed backend:
  - default for all workflows
  - full application coverage
  - primary parser and normalizer for certificate and CRL details
- OpenSSL backend:
  - compatibility-focused
  - invoked only for CSR signing when explicitly requested and the bridge reports support
  - not used at startup unless a specific operation routes into it

## Routing Policy

Backend routing is centralized in `XcaNet.Crypto.OpenSsl`:

- managed remains the default
- OpenSSL can be preferred through configuration or explicit request DTO preference
- managed fallback occurs when OpenSSL is unavailable or the bridge does not report the needed capability
- the actual backend used is surfaced in `SignedCertificateResult` and `StoredCertificateResult`

This keeps backend selection explicit and testable without leaking native concerns into the application layer.

M7 hardened the routing expectations with test coverage for:

- managed default even when the bridge is present
- OpenSSL preference when the bridge is present and capable
- fallback to managed when OpenSSL is unavailable
- no implicit managed fallback for `OpenSslOnly`

## Known Differences

### Extension Handling

- The current parity fixtures show equivalent preservation of SAN, KU, and EKU for CSR signing across managed and OpenSSL paths.
- OpenSSL signing copies CSR extensions directly through the bridge, then XcaNet normalizes the result via managed parsing.
- More exotic extension encoding is not yet routed through OpenSSL because M7 did not produce evidence that the managed path is insufficient for the current supported set.

### Encoding Differences

- Serial numbers and validity timestamps are treated as non-deterministic across backends and normalized out of parity comparisons.
- Managed parsing remains the normalization layer even for OpenSSL-produced certificates, which keeps higher layers backend-neutral.

### PKCS#12 Compatibility

- PKCS#12 import/export remains on the managed backend.
- M7 parity checks showed that managed-generated PFX bundles are readable by the OpenSSL CLI.
- Wrong-password and malformed-bundle handling are deterministic on the managed path.

### CRL Behavior

- CRL generation and parsing remain managed-only.
- M7 compatibility checks showed that managed-generated CRLs are readable by the OpenSSL CLI and contain the expected revoked serials.
- There is not yet evidence that CRL fidelity requires an OpenSSL-backed generation path.

### Edge-Case Parsing

- Malformed CSR input fails deterministically in both managed and OpenSSL signing paths.
- The application still remains resilient when the bridge is absent because OpenSSL availability is treated as capability, not as a startup requirement.

## When To Use Each Backend

- Use the managed backend for normal application workflows and as the default path.
- Prefer OpenSSL only when compatibility testing or targeted routing explicitly requests CSR signing through the bridge.
- Do not expand additional OpenSSL-backed operations until parity tests show a concrete interoperability gap.

## Isolation Rationale

OpenSSL is isolated behind an adapter for three reasons:

1. Native handles and OpenSSL-specific types never cross into Application, App, Storage, or Contracts.
2. Startup stays resilient because the app does not require OpenSSL to be present.
3. New OpenSSL-backed operations can be added incrementally under parity tests instead of replacing the managed path wholesale.
