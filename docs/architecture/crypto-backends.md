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

The OpenSSL backend is intentionally narrow in M6.

It currently implements exactly one real operation through the native bridge:

- sign CSR into certificate

All other certificate-service operations continue through the managed backend.

## Routing Policy

Backend routing is centralized in `XcaNet.Crypto.OpenSsl`:

- managed remains the default
- OpenSSL can be preferred through configuration or explicit request DTO preference
- managed fallback occurs when OpenSSL is unavailable or the bridge does not report the needed capability
- the actual backend used is surfaced in `SignedCertificateResult` and `StoredCertificateResult`

This keeps backend selection explicit and testable without leaking native concerns into the application layer.

## Isolation Rationale

OpenSSL is isolated behind an adapter for three reasons:

1. Native handles and OpenSSL-specific types never cross into Application, App, Storage, or Contracts.
2. Startup stays resilient because the app does not require OpenSSL to be present.
3. New OpenSSL-backed operations can be added incrementally under parity tests instead of replacing the managed path wholesale.
