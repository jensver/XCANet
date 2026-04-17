# XCANet

XcaNet is a clean-architecture rewrite of a cross-platform PKI manager in `.NET 10`, following the structure defined in `spec/SPEC.md`.

## Solution Structure

- `src/XcaNet.App` contains the shared Avalonia UI shell, views, and view models.
- `src/XcaNet.App.Desktop` contains the desktop entry point, DI composition root, configuration, and logging bootstrap.
- `src/XcaNet.Application` contains application orchestration for database lifecycle, lock/unlock state, and secure private-key storage.
- `src/XcaNet.Core` contains domain-level primitives with no external dependencies.
- `src/XcaNet.Contracts` contains cross-layer contracts.
- `src/XcaNet.Storage` contains the EF Core SQLite context, repositories, and migrations.
- `src/XcaNet.Security` contains master-password protection and encrypted private-key blob handling.
- `src/XcaNet.Crypto.*`, `src/XcaNet.ImportExport`, `src/XcaNet.Localization`, and `src/XcaNet.Diagnostics` remain scaffolded for later milestones.
- `tests/` contains unit and integration coverage for the storage/security milestone.

## Storage And Security

Milestone 2 adds:

- SQLite persistence through EF Core with checked-in migrations in `src/XcaNet.Storage/Persistence/Migrations`
- database create, open, unlock, and lock flows in the application layer
- encrypted PKCS#8 private-key storage using PBKDF2-SHA256 derived master keys and AES-256-GCM payload encryption
- audit event recording for database lifecycle and private-key storage actions
- minimal shell UI controls for create, open, unlock, lock, and status display

The current shell defaults the database path to the local app-data directory. The database is created or opened through the UI, then unlocked with the master password. No plaintext private keys are stored at rest, and master passwords are never logged.
