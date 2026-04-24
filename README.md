# XCANet

XcaNet is a clean-architecture rewrite of a cross-platform PKI manager in `.NET 10`, following the structure defined in `spec/SPEC.md`.

The current app has a strong PKI foundation, but it is **not yet XCA parity**. XCA parity now means matching XCA's core functionality, workflow model, and screen layout for the primary operator surfaces, not just shipping equivalent backend capabilities. See `docs/parity/xca-parity-charter.md`.

## What It Does

XcaNet is a desktop PKI workbench for operators who need to:

- create and store private keys securely in SQLite
- create self-signed CA certificates
- create and sign CSRs
- revoke certificates and generate CRLs
- import and export common certificate, key, CSR, PFX, and CRL formats
- apply reusable issuance templates

The managed `.NET` crypto backend is the default for all workflows. An optional OpenSSL bridge can be added for compatibility-sensitive CSR signing without becoming a runtime requirement.

## v0.1.0 Release Candidate Scope

The current release-candidate target is `v0.1.0`.

`v0.1.0` is a foundation release candidate, not an XCA parity milestone.

Included:

- secure local database workflow
- managed crypto workflows for keys, certificates, CSRs, revocation, and CRLs
- optional OpenSSL-enhanced CSR signing
- template-assisted issuance defaults
- desktop import/export, diagnostics, and packaging support

Not included:

- OpenSSL as a general-purpose backend replacement
- advanced template policy engine behavior
- CRL publishing/distribution infrastructure
- certificate issuance beyond the currently implemented managed/OpenSSL-routed paths

Draft release notes are in `docs/release/v0.1.0.md`.
The XCA parity contract and roadmap reset are in `docs/parity/xca-parity-charter.md`.

## Quick Start

Build and test:

```bash
dotnet build XcaNet.sln
dotnet test XcaNet.sln --no-build
```

Run the desktop app:

```bash
dotnet run --project src/XcaNet.App.Desktop/XcaNet.App.Desktop.csproj
```

Typical first-run flow:

1. Open `Settings / Security`.
2. Create a database and unlock it.
3. Generate a private key.
4. Create a self-signed CA or create a CSR.
5. Export/import material as needed.

Operator guides:

- `docs/operator/quickstart.md`
- `docs/operator/templates.md`
- `docs/packaging/README.md`
- `docs/parity/xca-parity-charter.md`

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

## Managed vs OpenSSL

- Managed backend:
  - default for all workflows
  - fully supported for normal operation
  - required for zero-native-dependency installs
- OpenSSL backend:
  - optional
  - currently used only when routing policy selects the OpenSSL CSR-signing path
  - should be treated as a compatibility enhancement, not a startup requirement

If the bridge is missing, invalid, or built for the wrong architecture, XcaNet stays usable in managed mode and exposes that failure through diagnostics.

## UX Completion And Operator Workflows

Milestone 8 focuses on desktop usability rather than backend expansion:

- native file-based import and export now sit on top of the existing application-layer import/export contracts
- supported file workflows cover PEM, DER, PKCS#8, PKCS#12/PFX, PKCS#10 CSR, and CRL material
- drag-and-drop import routes through the same application import path as the native picker flow
- certificates, keys, CSRs, CRLs, templates, and settings now expose clearer empty states and more consistent workflow actions
- Settings / Security now shows read-only backend diagnostics for managed availability, OpenSSL availability/version/capabilities, routing summary, and schema/app version data

The file picker layer only gathers paths. Parsing, classification, preview, persistence, and export generation remain below the UI layer. Diagnostics are informational only: there is still no backend picker UI, and the managed backend remains the default path.

## Template System And Policy Refinement

Milestone 10 turns templates into real workflow helpers instead of browse-only placeholders:

- templates are durable editable entities with name, description, enabled/favorite state, intended usage, subject defaults, SAN defaults, key defaults, validity defaults, and extension defaults
- operators can create, edit, clone, delete, enable/disable, and favorite templates from the Templates page
- templates can pre-populate self-signed CA, CSR, and CSR-signing workflows without creating parallel crypto implementations
- template validation catches obvious conflicts such as CA/basic-constraints mismatches, invalid algorithm defaults, unsupported KU/EKU values, and disabled or incompatible template use
- CSR templates now carry SAN, key-usage, EKU, and CA/basic-constraints defaults into the managed issuance path so signed certificates reflect those defaults when the CSR is used for issuance

Template usage is intentionally lightweight rather than policy-engine heavy. The existing issuance flows still work without templates, and the managed backend remains the default routing path.

## Release And Packaging

Milestone 9 adds a repeatable packaging lane without moving packaging concerns into the core app layers:

- `packaging/build-native-bridge.sh` builds the optional native bridge into `artifacts/native/<rid>/`
- `packaging/package-app.sh` publishes the desktop app into `artifacts/publish/<rid>/<Configuration>/app/`
- `packaging/verify-layout.sh` validates the expected publish layout
- `.github/workflows/ci.yml` exercises build, test, publish, and layout verification in CI

The application still runs correctly without any OpenSSL bridge present. If the bridge is missing, invalid, or built for the wrong architecture, startup diagnostics and the Settings / Security page report the failure while managed mode remains available.

Desktop startup now also writes support logs under the local application-data log directory:

- `startup.log`
- `startup-failure-<timestamp>.log`

Those logs are intended for operator troubleshooting and packaging verification. They do not change backend routing or introduce backend selection UI.

For repeatable release packaging and troubleshooting:

- `docs/packaging/README.md`
- `docs/packaging/platform-builds.md`
- `docs/packaging/troubleshooting.md`

## Theme-Safe Desktop UI

Milestone 11 hardens the desktop UI for both light and dark mode without changing backend behavior:

- shared theme resources now live in `src/XcaNet.App/Styles/ThemeResources.axaml`
- semantic surfaces such as notifications, empty states, validation panels, diagnostics panels, and navigation states use reusable theme-safe styles instead of per-view hard-coded brushes
- the key operator pages and inspectors were updated to stay readable in both theme variants

For extension guidance, see `docs/ui/theming.md`.

## Optional MCP Developer Tooling

This repo now includes optional developer guidance for:

- Microsoft Learn MCP
- Avalonia Build MCP

These integrations are developer tooling only. They are not required for app runtime, `dotnet build`, `dotnet test`, or packaging.

- docs: `docs/developer/mcp.md`
- example workspace config: `tooling/mcp/workspace.mcp.example.json`

Use Microsoft Learn MCP for trusted .NET and Microsoft platform reference work, and Avalonia Build MCP for Avalonia documentation and UI implementation guidance.

## Diagnostics And Troubleshooting

The `Settings / Security` page exposes read-only diagnostics for:

- app version
- schema version
- managed backend availability
- OpenSSL backend availability/version/capabilities
- routing summary

Startup logs are written to the platform-specific app-data log directory. Those logs are the first place to check when:

- the app fails to start
- the OpenSSL bridge does not load
- a packaged app behaves differently from a development run

## Related Docs

- `docs/operator/quickstart.md`
- `docs/operator/templates.md`
- `docs/packaging/README.md`
- `docs/packaging/troubleshooting.md`
- `docs/architecture/crypto-backends.md`
- `docs/architecture/interop-openssl.md`
