# Platform Build Notes

## Supported Runtime Identifiers

- Windows: `win-x64`
- macOS Intel: `osx-x64`
- macOS Apple Silicon: `osx-arm64`
- Linux x64: `linux-x64`

## Common Commands

Build and test:

```bash
dotnet build XcaNet.sln
dotnet test XcaNet.sln --no-build
```

Package a managed-only app:

```bash
packaging/package-app.sh osx-arm64 Release artifacts
packaging/verify-layout.sh osx-arm64 Release artifacts
```

Package with the optional OpenSSL bridge:

```bash
packaging/build-native-bridge.sh osx-arm64 artifacts
packaging/package-app.sh osx-arm64 Release artifacts
packaging/verify-layout.sh osx-arm64 Release artifacts
```

## Windows Notes

- `dotnet publish` works for `win-x64` from the normal .NET SDK flow.
- The native bridge build script uses a C toolchain and OpenSSL development headers/libraries. On Windows, use a compatible environment such as MSYS2/MinGW or another toolchain that can produce `xcanet_ossl_bridge.dll`.
- Managed-only packaging remains valid even if no native bridge is built.

## macOS Notes

- The native bridge build script defaults to Homebrew OpenSSL (`openssl@3`) when available.
- If a packaged app is launched in a non-GUI or constrained environment, Avalonia may fail before the UI is created. M9 writes a startup failure log in `~/Library/Application Support/XcaNet/logs/`.

## Linux Notes

- Ensure `libssl` and `libcrypto` are available to the packaged bridge on the target system.
- Managed-only mode remains the fallback when the bridge is not present or cannot load.
