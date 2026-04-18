# XCANet

XcaNet is a clean-architecture rewrite of a cross-platform PKI manager in `.NET 10`, following the structure defined in `spec/SPEC.md`.

## Solution Structure

- `src/XcaNet.App` contains the shared Avalonia shell, page views, navigation state, list/detail view models, and workflow wiring.
- `src/XcaNet.App.Desktop` contains the desktop entry point, DI composition root, configuration, and logging bootstrap.
- `src/XcaNet.Application` contains application orchestration for database lifecycle, lock/unlock state, secure private-key storage, and the managed crypto workflows.
- `src/XcaNet.Core` contains domain-level primitives with no external dependencies.
- `src/XcaNet.Contracts` contains cross-layer contracts.
- `src/XcaNet.Storage` contains the EF Core SQLite context, repositories, and migrations.
- `src/XcaNet.Security` contains master-password protection and encrypted private-key blob handling.
- `src/XcaNet.Crypto.Abstractions` defines backend-neutral crypto contracts.
- `src/XcaNet.Crypto.DotNet` implements the managed RSA/ECDSA, certificate, CSR, import/export, and CRL path.
- `src/XcaNet.Crypto.OpenSsl` adds the routed OpenSSL-backed certificate-signing path through a thin native bridge.
- `src/XcaNet.Interop.OpenSsl` owns bridge loading, probing, capability reporting, and safe marshalling.
- `src/XcaNet.ImportExport`, `src/XcaNet.Localization`, and `src/XcaNet.Diagnostics` remain partial or scaffolded for later milestones.
- `tests/` contains unit and integration coverage for the storage, security, and managed-crypto milestones.

## Storage And Security

Milestone 2 adds:

- SQLite persistence through EF Core with checked-in migrations in `src/XcaNet.Storage/Persistence/Migrations`
- database create, open, unlock, and lock flows in the application layer
- encrypted PKCS#8 private-key storage using PBKDF2-SHA256 derived master keys and AES-256-GCM payload encryption
- audit event recording for database lifecycle and private-key storage actions
- minimal shell UI controls for create, open, unlock, lock, and status display

The current shell defaults the database path to the local app-data directory. The database is created or opened through the UI, then unlocked with the master password. No plaintext private keys are stored at rest, and master passwords are never logged.

## Managed Crypto

Milestone 3 adds the managed backend on top of the M2 storage model:

- RSA key generation with a `3072` bit minimum
- ECDSA key generation for `P-256` and `P-384`
- self-signed CA certificate creation
- PKCS#10 CSR creation and signing
- certificate parsing for subject, issuer, serial, validity, thumbprints, key algorithm, basic constraints, key usage, EKU, and SAN
- import/export for PEM, DER, PKCS#8, PKCS#12/PFX, and PKCS#10 CSR in the common managed path
- application workflows and minimal shell UI actions for generate key, create CA, create CSR, sign CSR, import/export, and certificate-details inspection

Generated and imported private keys continue to be stored only as encrypted PKCS#8 blobs inside SQLite. Managed crypto workflows decrypt the private key material only for the shortest necessary in-memory window, then persist certificates and CSRs as raw DER plus normalized searchable metadata.

## Core UI Workflows

Milestone 4 turns the shell into a usable desktop structure:

- primary navigation for dashboard, certificates, private keys, CSRs, CRLs, templates, and settings/security
- certificate, private-key, CSR, CRL, and template list views backed by application-layer browse contracts
- a certificate inspector with metadata, SAN, key usage, EKU, revocation state, and relationship navigation to issuer, child certificates, and related private key
- selection-based workflows for key generation, self-signed CA creation, CSR creation, CSR signing, import, and export
- certificate search and filter support for display name, subject, issuer, serial, thumbprints, validity state, and CA vs leaf
- busy-state feedback, notifications, and command enablement tied to database session state

The UI still talks only to `XcaNet.Application` contracts. EF Core and crypto implementations remain behind the application and storage layers.

## Revocation And CRLs

Milestone 5 adds the first revocation workflow and a cleanup pass on the UI/application boundary:

- page view models now stay focused on selection, filter state, busy state, and user intent while application services own workflow execution
- navigation between certificates, keys, CSRs, and CRLs uses a single `NavigationTarget` model
- certificate browsing uses a dedicated `CertificateFilterState` and the inspector binds to a stable `CertificateInspectorData` DTO instead of storage models
- certificates can now be revoked with a stored reason and revocation timestamp
- managed CRL generation persists issuer metadata, CRL number, update timestamps, and revoked entries
- certificate and CRL list/detail views now reflect revocation status and generated CRLs
- audit events now cover certificate revocation and CRL generation

Import and export remain transport-oriented in the application layer, so the current paste-based UI can be replaced later without changing the workflow contracts.

## Optional OpenSSL Backend

Milestone 6 adds an optional OpenSSL integration without making native code part of the application architecture:

- the app still starts and works with the managed backend when no OpenSSL bridge is present
- OpenSSL is loaded only through `XcaNet.Interop.OpenSsl`
- backend routing is centralized and explicit
- the first OpenSSL-backed operation is CSR signing
- parity tests cover managed vs OpenSSL output for that operation
- managed remains the default even when an OpenSSL bridge is present

M7 hardens the compatibility story rather than expanding features:

- richer parity fixtures now cover SAN-heavy signing, extension-rich signing, malformed input, PKCS#12 inspection, and CRL inspection
- managed PKCS#12 and CRL artifacts were validated against the OpenSSL CLI
- no second OpenSSL-backed operation was added because the new evidence did not justify it

Build the native bridge locally with:

```bash
native/xcanet_ossl_bridge/build-bridge.sh <output-dir>
```

To use a specific bridge artifact, set `XCANET_OPENSSL_BRIDGE_PATH` or configure `Crypto:OpenSslBridgePath`. If no bridge is configured, the managed backend remains active.
