# Operator Quick Start

This guide covers the shortest path from a fresh checkout to usable certificate workflows.

## 1. Start the app

```bash
dotnet run --project src/XcaNet.App.Desktop/XcaNet.App.Desktop.csproj
```

## 2. Create and unlock a database

In `Settings / Security`:

1. Choose a database path.
2. Set a display name.
3. Enter a strong master password.
4. Click `Create`.
5. If reopening an existing database, click `Open`, then `Unlock`.

Private keys are encrypted at rest. Key creation, signing, revocation, and export require an unlocked database.

## 3. Common workflows

### Create a local CA

1. Go to `Private Keys`.
2. Generate a key.
3. Use `Create Self-Signed CA`.

### Create a CSR

1. Go to `Private Keys`.
2. Select or generate a key.
3. Use `Create CSR`.

### Sign a CSR

1. Go to `CSRs`.
2. Select the CSR.
3. Choose an issuer CA certificate and matching issuer key.
4. Click `Sign CSR`.

### Revoke and generate a CRL

1. Go to `Certificates`.
2. Select the certificate to revoke.
3. Choose a reason and date.
4. Type `REVOKE` and confirm.
5. Select the CA certificate and generate a CRL.

## 4. Import and export

Supported import/export material includes:

- PEM
- DER / CER
- PKCS#8
- PKCS#12 / PFX
- PKCS#10 CSR
- CRL

Use the native file dialogs for normal operator workflows. Drag-and-drop import is also supported from the main window.

## 5. Diagnostics

Use `Settings / Security` for read-only diagnostics:

- managed backend status
- OpenSSL backend status
- version and capabilities
- routing summary
- app/schema version

If the optional OpenSSL bridge is missing or invalid, the managed backend remains usable.
