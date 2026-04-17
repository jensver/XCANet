# XCANet

XcaNet is a clean-architecture rewrite of a cross-platform PKI manager in `.NET 10`, following the structure defined in `spec/SPEC.md`.

## Solution Structure

- `src/XcaNet.App` contains the shared Avalonia UI shell, views, and view models.
- `src/XcaNet.App.Desktop` contains the desktop entry point, DI composition root, configuration, and logging bootstrap.
- `src/XcaNet.Application` contains application orchestration placeholders and DI registration.
- `src/XcaNet.Core` contains domain-level primitives with no external dependencies.
- `src/XcaNet.Contracts` contains cross-layer contracts.
- `src/XcaNet.Storage`, `src/XcaNet.Security`, `src/XcaNet.Crypto.*`, `src/XcaNet.ImportExport`, `src/XcaNet.Localization`, and `src/XcaNet.Diagnostics` are scaffolded as visible infrastructure boundaries.
- `tests/` contains scaffolded test projects aligned to the target architecture.

Milestone 1 intentionally stops at a buildable shell. No crypto, storage, OpenSSL interop, or business workflows are implemented yet.
