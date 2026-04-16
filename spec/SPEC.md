# XcaNet Rewrite Specification

## 1. Purpose

Build a new cross-platform desktop PKI manager in **.NET 10** named **XcaNet**.

This project is **inspired by XCA** but is **not** a literal source port. The target is a **clean long-term architecture** with a modern UI, strong local security, and a replaceable crypto backend.

The app must:
- run on Windows, macOS, and Linux
- use one shared UI codebase
- manage certificates, private keys, CSRs, CRLs, and templates
- store data locally and securely
- support interoperability with OpenSSL-generated artifacts
- be designed so Codex can work with minimal further clarification

---

## 2. Product Direction

### 2.1 What this is
A desktop certificate and key management tool that covers the practical workflows users expect from XCA:
- generate keys
- create self-signed CAs
- create and sign CSRs
- issue certificates
- renew certificates
- revoke certificates
- generate CRLs
- inspect X.509 fields and extensions
- import and export common PKI formats
- organize templates and authorities

### 2.2 What this is not
Do **not**:
- wrap the existing XCA executable
- embed Qt UI inside the new app
- mechanically translate C++ classes into C# classes
- make OpenSSL the application's architecture
- let native types leak into the domain model or UI

---

## 3. Core Technical Decisions

### 3.1 Runtime and language
- **.NET 10**
- **C#** with nullable reference types enabled

### 3.2 UI
- **Avalonia UI**
- **MVVM** architecture
- XAML-based views
- no business logic in code-behind

### 3.3 Storage
- **SQLite**
- **EF Core** with migrations from day one

### 3.4 Security
- encrypted private keys at rest
- master-password-based local database protection
- structured audit events

### 3.5 Crypto strategy
Implement a **pluggable crypto backend**:
- `DotNetCryptoBackend`
- `OpenSslCryptoBackend`

Use the managed backend first for standard operations.
Use the OpenSSL backend only where compatibility, feature completeness, or extension fidelity requires it.

### 3.6 Native interop strategy
If OpenSSL is used, it must be behind a **thin native bridge** with a **C ABI**.
No direct OpenSSL calls from UI or application layers.

---

## 4. Non-Functional Requirements

The implementation must prioritize:
- maintainability
- clean layering
- testability
- security
- predictable cross-platform behavior
- low coupling to native code
- suitability for AI-assisted development

It must not optimize for the shortest possible porting path at the expense of architecture.

---

## 5. High-Level Architecture

Use this layered model:

1. **App/UI Layer**
   - Avalonia views and view models
   - user workflows only

2. **Application Layer**
   - use cases and orchestration
   - commands, queries, DTOs, validation flow

3. **Domain/Core Layer**
   - entities, value objects, domain rules
   - no UI, no storage, no native dependencies

4. **Infrastructure Layers**
   - storage
   - security
   - crypto backend implementations
   - import/export
   - diagnostics

5. **Native Bridge Layer**
   - optional OpenSSL bridge only
   - isolated from all higher layers

---

## 6. Required Solution Structure

```text
XcaNet.sln

src/
  XcaNet.App/
  XcaNet.App.Desktop/
  XcaNet.Core/
  XcaNet.Application/
  XcaNet.Contracts/
  XcaNet.Storage/
  XcaNet.Security/
  XcaNet.Crypto.Abstractions/
  XcaNet.Crypto.DotNet/
  XcaNet.Crypto.OpenSsl/
  XcaNet.Interop.OpenSsl/
  XcaNet.ImportExport/
  XcaNet.Localization/
  XcaNet.Diagnostics/
  XcaNet.Packaging/

native/
  xcanet_ossl_bridge/

tests/
  XcaNet.Core.Tests/
  XcaNet.Application.Tests/
  XcaNet.Storage.Tests/
  XcaNet.Security.Tests/
  XcaNet.Crypto.DotNet.Tests/
  XcaNet.Crypto.OpenSsl.Tests/
  XcaNet.ImportExport.Tests/
  XcaNet.Integration.Tests/
  XcaNet.Interop.Tests/
  XcaNet.Parity.Tests/

docs/
  adr/
  architecture/
  threat-model/
  test-matrix/
```

---

## 7. Layering Rules

These rules are strict:

- `XcaNet.App` may depend on `Application`, `Contracts`, `Localization`, `Diagnostics`
- `Application` may depend on `Core`, `Contracts`, `Crypto.Abstractions`, `Storage`, `Security`
- `Core` depends only on the base class library
- `Crypto.Abstractions` must not depend on any concrete backend
- `Crypto.DotNet` and `Crypto.OpenSsl` implement `Crypto.Abstractions`
- `Interop.OpenSsl` is only used by `Crypto.OpenSsl`
- `Storage` must not reference UI or native interop
- `Security` must not depend on UI
- View models must never call native interop directly
- No domain entity may contain native handles, pointers, or OpenSSL-specific types

---

## 8. Functional Scope for v1

### 8.1 Supported object types
- Private keys
- Certificates
- Certificate signing requests (CSRs)
- Certificate revocation lists (CRLs)
- Templates
- Authorities / issuer relationships
- Tags
- Audit events

### 8.2 Key algorithms
Required for v1:
- RSA
- ECDSA

Optional for later:
- Ed25519
- Ed448

### 8.3 Certificate operations
Required:
- Create self-signed root CA
- Create intermediate CA
- Create end-entity certificate
- Create CSR
- Sign CSR with CA
- Renew certificate
- Revoke certificate
- Generate CRL
- Inspect issuer/subject relationships

### 8.4 X.509 extension support
Required in v1:
- Basic Constraints
- Key Usage
- Extended Key Usage
- Subject Alternative Name
- Subject Key Identifier
- Authority Key Identifier
- CRL Distribution Points
- Authority Information Access

Planned after v1:
- Name Constraints
- custom OIDs
- `otherName`
- UPN-like identities
- certificate policy extensions

### 8.5 Import/export formats
Required:
- PEM
- DER
- PKCS#8
- PKCS#12 / PFX
- PKCS#10 CSR
- JWK export

---

## 9. UI Scope for v1

Build these screens first:

1. Database create/open/unlock
2. Dashboard
3. Certificates list
4. Private keys list
5. CSRs list
6. CRLs list
7. Templates list
8. Object detail inspector
9. Import wizard
10. Export wizard
11. Create CA wizard
12. Create leaf certificate wizard
13. Sign CSR wizard
14. Revoke certificate dialog
15. Settings / security page

### 9.1 Required UI behaviors
- global search
- filtering by status
- filtering by issuer/template/tag
- hierarchy or issuer tree
- raw extension view
- copy serial/thumbprint/subject values
- export from details page
- open linked issuer or linked private key
- cancel long-running operations
- centralized notifications and error presentation

### 9.2 UI constraints
- no business logic in code-behind
- async IO and storage operations
- cancellable workflows
- state kept in view models and application services

---

## 10. Domain Model

### 10.1 Core entities
- `CertificateRecord`
- `PrivateKeyRecord`
- `CertificateRequestRecord`
- `CertificateRevocationListRecord`
- `TemplateRecord`
- `AuthorityRecord`
- `TagRecord`
- `AuditEvent`
- `AppSetting`
- `DatabaseProfile`

### 10.2 Suggested value objects
- `DistinguishedName`
- `SerialNumber`
- `Fingerprint`
- `KeyAlgorithm`
- `SignatureAlgorithm`
- `ValidityPeriod`
- `SubjectAlternativeName`
- `ExtensionValue`
- `ObjectIdentifier`
- `RevocationStatus`

### 10.3 Domain guidance
- Use normalized domain concepts, not raw backend types
- Prefer immutable request/result models where practical
- Use domain validation before backend invocation where possible

---

## 11. Database Design

### 11.1 Required tables
- `PrivateKeys`
- `Certificates`
- `CertificateRequests`
- `CertificateRevocationLists`
- `Templates`
- `Authorities`
- `Tags`
- `CertificateTags`
- `AuditEvents`
- `AppSettings`
- `DatabaseProfiles`

### 11.2 Required indexes
At minimum index:
- certificate serial
- SHA-1 thumbprint
- SHA-256 thumbprint
- subject string
- issuer string
- not before / not after
- revocation state
- issuer certificate id
- private key public fingerprint
- template name

### 11.3 Storage rules
- private keys must never be stored in plaintext
- private keys should be stored as encrypted PKCS#8 blobs
- certificate blobs may be stored as DER or PEM plus parsed metadata
- searchable fields should be normalized, not buried in JSON
- use migrations from the start

---

## 12. Security Architecture

### 12.1 Master secret model
Support:
- create database password
- unlock database
- explicit lock
- auto-lock after inactivity
- change master password
- optional OS-keystore-assisted wrapping in a later phase

### 12.2 Encryption at rest
Requirements:
- derive a database master key from the user secret using a strong KDF
- use per-record random salt where appropriate
- use authenticated encryption for encrypted blobs
- include a migration path if KDF settings are upgraded later

Preferred:
- Argon2id if acceptable dependency-wise
- PBKDF2 as fallback
- AES-GCM for encrypted blob storage

### 12.3 Sensitive data handling
- never log plaintext private keys
- never log master passwords or export passwords
- minimize plaintext key lifetime in memory where practical
- provide optional clipboard expiry for exported or copied sensitive values
- require explicit confirmation before exporting unencrypted private keys

### 12.4 Audit events
Record at minimum:
- database created/opened/locked/unlocked
- key generated/imported/exported/deleted
- cert created/imported/exported/revoked/renewed
- CSR created/imported/signed
- CRL generated/exported
- master password changed
- backend used for each crypto operation

---

## 13. Crypto Abstractions

Define contracts similar to these:

```csharp
public interface IKeyService
{
    Task<Result<KeyGenerationResult>> GenerateAsync(KeyGenerationRequest request, CancellationToken ct);
    Task<Result<ParsedPrivateKey>> ImportPrivateKeyAsync(PrivateKeyImportRequest request, CancellationToken ct);
    Task<Result<PrivateKeyExportResult>> ExportPrivateKeyAsync(PrivateKeyExportRequest request, CancellationToken ct);
}

public interface ICertificateService
{
    Task<Result<CertificateIssueResult>> CreateSelfSignedAsync(SelfSignedCertificateRequest request, CancellationToken ct);
    Task<Result<CertificateIssueResult>> SignCsrAsync(SignCsrRequest request, CancellationToken ct);
    Task<Result<CertificateRenewalResult>> RenewAsync(CertificateRenewalRequest request, CancellationToken ct);
    Task<Result<ParsedCertificate>> ParseAsync(CertificateParseRequest request, CancellationToken ct);
}

public interface ICsrService
{
    Task<Result<CsrCreationResult>> CreateAsync(CsrCreationRequest request, CancellationToken ct);
    Task<Result<ParsedCsr>> ParseAsync(CsrParseRequest request, CancellationToken ct);
}

public interface ICrlService
{
    Task<Result<CrlGenerationResult>> GenerateAsync(CrlGenerationRequest request, CancellationToken ct);
    Task<Result<ParsedCrl>> ParseAsync(CrlParseRequest request, CancellationToken ct);
}

public interface IImportExportService
{
    Task<Result<ImportResult>> ImportAsync(ImportRequest request, CancellationToken ct);
    Task<Result<ExportResult>> ExportAsync(ExportRequest request, CancellationToken ct);
}
```

### 13.1 Contract rules
- contracts must be backend-agnostic
- return warnings as well as failures
- return structured machine-readable error codes
- do not leak native exceptions outside the backend boundary

---

## 14. Managed Backend Requirements

`XcaNet.Crypto.DotNet` must implement:
- RSA key generation
- ECDSA key generation
- self-signed certificate creation
- standard CSR creation
- standard PEM/DER/PFX import/export
- parsing for standard certificate fields and common extensions

Use the managed backend first for:
- standard happy-path operations
- simple CA and leaf issuance
- conventional PEM/PFX handling
- certificate inspection
- standard SAN/KU/EKU scenarios

---

## 15. OpenSSL Backend Requirements

### 15.1 Purpose
The OpenSSL backend exists for:
- compatibility-sensitive operations
- uncommon or tricky extension encoding
- difficult PKCS#12 interoperability cases
- parity with external OpenSSL-generated artifacts

### 15.2 Native bridge requirements
Create a small native bridge in:

```text
native/xcanet_ossl_bridge/
```

Rules:
- expose a **C ABI**
- no exposed C++ classes
- no OpenSSL pointers exposed to .NET
- use explicit buffer ownership rules
- map native errors to stable result codes and messages

### 15.3 Example exported functions
```c
int xcanet_ossl_get_version(char* buffer, int buffer_len);
int xcanet_ossl_generate_key(const xcanet_keygen_request* req, xcanet_buffer* out_key, xcanet_error* err);
int xcanet_ossl_create_csr(const xcanet_csr_request* req, xcanet_buffer* out_csr, xcanet_error* err);
int xcanet_ossl_sign_certificate(const xcanet_sign_request* req, xcanet_buffer* out_cert, xcanet_error* err);
int xcanet_ossl_generate_crl(const xcanet_crl_request* req, xcanet_buffer* out_crl, xcanet_error* err);
int xcanet_ossl_parse_certificate(const xcanet_buffer* cert, xcanet_buffer* out_json, xcanet_error* err);
void xcanet_ossl_free_buffer(xcanet_buffer* buffer);
```

### 15.4 Packaging expectations
The main app must be able to load a platform-appropriate native bridge on:
- Windows
- macOS
- Linux

If the bridge is missing, the app must degrade gracefully and clearly report backend availability.

---

## 16. Import / Export Requirements

### 16.1 Import flow
The app must be able to inspect and classify imported material:
- PEM cert
- PEM key
- PEM CSR
- DER cert
- DER CSR
- PKCS#8
- PKCS#12 / PFX

Import UX must:
- detect object type
- preview parsed content
- report warnings
- allow user to choose storage destination and behavior
- detect missing or mismatched private keys where possible

### 16.2 Export flow
Allow export of:
- certificate only
- full chain
- private key only
- cert + key bundle
- PFX
- CSR
- JWK public
- JWK private only when explicitly allowed

### 16.3 Export safety rules
- encrypted private-key export should be default
- unencrypted key export requires explicit override
- UI must clearly state when secret material leaves protected storage

---

## 17. Template System

Templates are first-class objects.

### 17.1 Template fields
- name
- description
- subject defaults
- SAN defaults
- key algorithm defaults
- key size / curve defaults
- signature algorithm default
- validity period default
- EKU defaults
- KU defaults
- basic constraints
- AIA/CDP defaults
- custom extension presets
- tags / categories

### 17.2 Template behaviors
- preview before issue
- clone template
- favorite template
- disable template
- validate issuer compatibility
- detect conflicting settings

---

## 18. Validation Rules

Perform validation before issuance, renewal, revocation, and export.

Examples:
- CA certificate must have CA-capable constraints
- issuing CA validity must cover child validity
- duplicate serial under same issuer must warn or fail by policy
- TLS server templates without SAN should warn
- weak key sizes or weak hashes should warn or fail by policy
- invalid renewal relationships should fail
- invalid KU/EKU combinations should warn

Operation results must include:
- success/failure
- warnings
- backend used
- machine-readable result code
- human-readable message

---

## 19. Search and Filtering

Support search and filtering by:
- display name
- subject
- issuer
- serial
- SHA-1 / SHA-256 thumbprint
- SAN values
- template
- tag
- revoked status
- expiring soon
- expired
- missing private key
- CA vs leaf

Implement efficient indexed queries rather than large in-memory scans.

---

## 20. Logging and Diagnostics

### 20.1 Logging
Use structured logging.

Levels:
- Trace
- Debug
- Information
- Warning
- Error

### 20.2 Never log
- private key plaintext
- master password
- export password
- decrypted secret-derived key material

### 20.3 Diagnostic surface
Expose in-app or in logs:
- app version
- schema version
- platform info
- active crypto backend
- backend capability matrix
- native backend version if loaded

---

## 21. Testing Strategy

Testing is mandatory from the beginning.

### 21.1 Unit tests
Cover:
- domain validation
- template validation
- security policy decisions
- parser helpers
- repository behaviors
- result mapping

### 21.2 Integration tests
Cover:
- generate key -> issue cert -> store -> retrieve -> export
- import PEM/PFX -> parse -> store
- revoke -> generate CRL
- open/unlock/lock flows
- schema migration upgrades

### 21.3 Parity tests
Create fixture-based tests using external artifacts, especially OpenSSL-generated files:
- certificates
- CSRs
- PFX bundles
- SAN/KU/EKU edge cases
- CA constraint edge cases
- malformed inputs

Validate:
- parse fidelity
- roundtrip fidelity
- extension preservation
- signature verification
- chain behavior

### 21.4 Native interop tests
Cover:
- invalid inputs
- missing backend library
- memory cleanup
- repeated load/unload
- error mapping
- version detection

### 21.5 UI tests
At minimum automate:
- create database
- unlock database
- import cert
- create CA
- sign CSR / issue cert
- export cert
- revoke cert

---

## 22. Acceptance Criteria

The project is acceptable when:

1. It builds on Windows, macOS, and Linux.
2. The app launches from one Avalonia UI codebase.
3. Local database create/open/lock/unlock works.
4. RSA and ECDSA keys can be generated and stored encrypted.
5. Self-signed CA creation works.
6. CSR creation works.
7. Signing a CSR into a certificate works.
8. Import/export of PEM, DER, PKCS#8, and PKCS#12 works for common cases.
9. The certificate detail inspector correctly shows:
   - subject
   - issuer
   - serial
   - validity
   - SAN
   - KU/EKU
   - CA status
   - fingerprints
10. Revocation and CRL generation work.
11. Integration tests pass.
12. Parity tests pass for agreed fixtures.
13. No UI layer depends directly on native interop.
14. No plaintext private keys are stored at rest.

---

## 23. Packaging Requirements

### 23.1 Windows
Produce:
- a distributable packaged build
- a portable build if practical

### 23.2 macOS
Produce:
- an app bundle
- documented signing/notarization steps

### 23.3 Linux
Produce:
- at least one portable package format
- documented runtime dependency expectations

---

## 24. Documentation Requirements

Create and maintain:
- `README.md`
- `docs/architecture/overview.md`
- `docs/architecture/layers.md`
- `docs/architecture/crypto-backends.md`
- `docs/architecture/storage.md`
- `docs/threat-model/summary.md`
- `docs/test-matrix/parity.md`
- `docs/adr/0001-architecture.md`
- `docs/adr/0002-crypto-backend-strategy.md`
- `docs/adr/0003-private-key-storage.md`

Each ADR must include:
- context
- decision
- alternatives considered
- consequences

---

## 25. Suggested Package Set

Prefer a conservative dependency set.
Likely packages:
- Avalonia
- Avalonia.Desktop
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Sqlite
- FluentValidation

Avoid unnecessary package sprawl.

---

## 26. Coding Standards

### 26.1 General
- nullable reference types enabled
- warnings as errors for core/app projects
- file-scoped namespaces
- analyzers enabled
- cancellation tokens on IO/crypto/storage methods
- async suffix on async methods

### 26.2 Error handling
- no silent catches
- no exceptions for normal validation flow
- use typed result objects for expected failures
- reserve exceptions for unexpected failures

### 26.3 Design style
- prefer small focused classes
- avoid god services
- prefer immutable DTOs where practical
- no static mutable global state

---

## 27. Milestone Plan

### Milestone 1: solution skeleton
- create solution and projects
- set up dependency graph
- configure DI, logging, config
- create base domain models
- create Avalonia shell app

### Milestone 2: storage and security
- EF Core schema
- migrations
- database create/open/unlock
- encrypted private-key storage
- audit event plumbing

### Milestone 3: managed crypto backend
- RSA/ECDSA key generation
- self-signed cert creation
- CSR creation
- standard import/export
- certificate parser

### Milestone 4: primary UI workflows
- lists and detail inspectors
- import/export wizards
- create CA
- create leaf cert
- sign CSR

### Milestone 5: revocation
- revoke cert
- CRL generation
- CRL UI

### Milestone 6: OpenSSL bridge
- native bridge skeleton
- capability detection
- routing/fallback logic
- interop tests

### Milestone 7: parity hardening
- fixture suite
- extension edge cases
- packaging docs
- feature status matrix

---

## 28. Backend Routing Policy

Implement capability-based routing.

Default behavior:
- use managed backend first for standard operations
- route to OpenSSL backend for:
  - unsupported or inconsistent extension combinations
  - compatibility mode templates
  - difficult PKCS#12 cases
  - explicit user preference
  - parity-test-specific scenarios

Expose backend choice for advanced users, but keep sane defaults.

---

## 29. Minimal First Release Scope

The first release is considered feature-complete enough when it can:
- create/open/unlock a database
- generate RSA/ECDSA keys
- create a self-signed CA
- create a CSR
- sign a CSR into a certificate
- import/export PEM/DER/PFX
- inspect common certificate fields and extensions
- revoke certificates
- generate CRLs
- document platform packaging steps

---

## 30. Explicit Build Order for Codex

Codex should implement in this order:

1. Create the solution and all projects.
2. Establish the dependency graph and references.
3. Implement the base domain models and contracts.
4. Implement storage and encrypted-key-at-rest handling.
5. Implement the managed crypto backend.
6. Implement minimal Avalonia UI workflows.
7. Implement revocation and CRLs.
8. Add the OpenSSL bridge only behind a narrow abstraction.
9. Add parity tests with external fixtures.
10. Add docs and packaging instructions.

---

## 31. GitHub Project Management Guidance

This repository should use GitHub-native planning instead of Jira initially.

Required repo setup:
- Issues enabled
- Projects enabled
- Milestones enabled
- Pull requests enabled
- Issue templates enabled
- Labels enabled

Recommended labels:
- `type:feature`
- `type:bug`
- `type:task`
- `type:spike`
- `area:ui`
- `area:storage`
- `area:security`
- `area:crypto`
- `area:interop`
- `area:tests`
- `area:docs`
- `priority:high`
- `priority:medium`
- `priority:low`
- `backend:dotnet`
- `backend:openssl`

Recommended milestones:
- `M1 - Skeleton`
- `M2 - Storage and Security`
- `M3 - Managed Crypto`
- `M4 - Core UI`
- `M5 - Revocation`
- `M6 - OpenSSL Bridge`
- `M7 - Parity Hardening`
- `v0.1.0`

Recommended project views:
- Backlog
- Next Up
- In Progress
- Blocked
- In Review
- Done

Use GitHub Projects as the primary Jira-style board.

---

## 32. Branching Guidance

Recommended branches:
- `main`
- `develop` (optional; only if really useful)
- short-lived feature branches: `feat/...`, `fix/...`, `chore/...`

Prefer a simple workflow:
- protect `main`
- use pull requests for all non-trivial work
- require passing tests before merge

---

## 33. Repository Bootstrap Expectations

On initial bootstrap, include:
- `.editorconfig`
- `.gitattributes`
- `.gitignore`
- `README.md`
- `LICENSE` placeholder or actual chosen license
- `CONTRIBUTING.md`
- `SECURITY.md`
- issue templates
- pull request template
- CODEOWNERS if useful later
- CI workflow skeleton

---

## 34. CI Expectations

At minimum set up CI to:
- restore dependencies
- build the solution
- run unit tests
- run integration tests where practical
- publish test results
- verify formatting / analyzers

Later add:
- cross-platform matrix
- packaging jobs
- native bridge build jobs

---

## 35. Codex Prompt Block

Use this prompt if needed when delegating to Codex:

```text
Build a new cross-platform desktop PKI manager in .NET 10 named XcaNet.

This is a clean-architecture rewrite inspired by XCA, not a literal source port.
Use Avalonia for the UI, MVVM for presentation, SQLite + EF Core for persistence, and a pluggable crypto backend.

Required architecture:
- XcaNet.App
- XcaNet.App.Desktop
- XcaNet.Core
- XcaNet.Application
- XcaNet.Contracts
- XcaNet.Storage
- XcaNet.Security
- XcaNet.Crypto.Abstractions
- XcaNet.Crypto.DotNet
- XcaNet.Crypto.OpenSsl
- XcaNet.Interop.OpenSsl
- XcaNet.ImportExport
- XcaNet.Localization
- XcaNet.Diagnostics
- tests projects

Rules:
- UI depends only on application-facing abstractions
- no direct P/Invoke from UI or view models
- domain model must not contain native/OpenSSL types
- private keys must be encrypted at rest
- managed crypto backend first
- OpenSSL only through a thin bridge with a C ABI
- tests and docs from day one

Implement in this order:
1. solution skeleton
2. domain + contracts
3. storage + security
4. managed crypto backend
5. minimal Avalonia UI
6. revocation + CRLs
7. OpenSSL bridge
8. parity tests
9. packaging + docs
```

---

## 36. Final Design Intent

The long-term success criterion is not perfect short-term parity with every corner of the current XCA implementation.
The long-term success criterion is:
- clean architecture
- secure local storage
- maintainable codebase
- predictable cross-platform UI
- replaceable crypto backends
- strong interoperability backed by tests

