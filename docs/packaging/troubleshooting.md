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

## Wrong Architecture Bridge

If the bridge binary architecture does not match the process architecture, diagnostics should report an architecture mismatch or invalid binary load failure.

Build a bridge for the same architecture as the published app:

- `osx-arm64` app -> `libxcanet_ossl_bridge.dylib` built for arm64
- `win-x64` app -> `xcanet_ossl_bridge.dll` built for x64
- `linux-x64` app -> `libxcanet_ossl_bridge.so` built for x64

## Missing Native Dependencies

If the bridge exists but `libssl` / `libcrypto` cannot be loaded, diagnostics should report a missing dependency failure. Managed mode still remains available.

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
