# Packaging And Runtime Troubleshooting

## Bridge Not Loaded

The app does not require OpenSSL to start. If the bridge is missing or invalid:

- managed mode remains the default
- startup diagnostics report the load failure
- the Settings / Security page shows read-only backend diagnostics

Check:

- `XCANET_OPENSSL_BRIDGE_PATH`
- `Crypto:OpenSslBridgePath`
- `app/native/`
- `app/bridges/`
- `app/runtimes/<rid>/native/`

Also confirm the packaged manifest at `artifacts/packages/<rid>/<Configuration>/manifest.txt` matches the bridge mode you expected to ship.

## Wrong Architecture Bridge

If the bridge binary architecture does not match the process architecture, diagnostics should report an architecture mismatch or invalid binary load failure.

Build a bridge for the same architecture as the published app:

- `osx-arm64` app -> `libxcanet_ossl_bridge.dylib` built for arm64
- `win-x64` app -> `xcanet_ossl_bridge.dll` built for x64
- `linux-x64` app -> `libxcanet_ossl_bridge.so` built for x64

## Missing Native Dependencies

If the bridge exists but `libssl` / `libcrypto` cannot be loaded, diagnostics should report a missing dependency failure. Managed mode still remains available.

## Import Failures

Common import failures include:

- unsupported extension
- empty file
- malformed DER/CER payload
- incorrect password for PKCS#12 or encrypted key material

XcaNet should fail these imports with a clear validation or storage message rather than crashing.

If an operator reports import trouble, confirm:

- the file type is one of the supported formats
- the file is non-empty
- the password is correct when required
- the payload is really a certificate, CSR, CRL, key, or PFX rather than mislabeled content

## Export Failures

Common export failures include:

- invalid destination path
- directory selected instead of file path
- insufficient filesystem permissions
- locked database or missing selection

The application should report export failures clearly and keep the rest of the session usable.

## Startup Failure Logs

On startup failure, the desktop launcher writes logs under the local application-data log directory:

- macOS: `~/Library/Application Support/XcaNet/logs/`
- Windows: `%LocalAppData%\XcaNet\logs\`
- Linux: `${XDG_DATA_HOME:-~/.local/share}/XcaNet/logs/` when `LocalApplicationData` maps there

Files:

- `startup.log`
- `startup-failure-<timestamp>.log`

## Headless Or Constrained GUI Environment

If Avalonia cannot start the native render timer or graphics subsystem, this is an environment/runtime problem rather than an OpenSSL requirement. The startup failure log will include that guidance.
