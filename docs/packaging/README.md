# Packaging

This folder documents the M9 packaging and release-hardening workflow.

## Output Layout

The packaging scripts use a predictable artifact layout:

- `artifacts/native/<rid>/`
- `artifacts/publish/<rid>/<Configuration>/app/`
- `artifacts/packages/<rid>/<Configuration>/`

The desktop publish directory is the runtime payload. Optional native bridge artifacts are copied into `app/native/`.

Each packaged output also includes `artifacts/packages/<rid>/<Configuration>/manifest.txt` with:

- target RID
- build configuration
- release version
- publish directory
- bridge path
- bridge mode (`managed-only` or `optional-openssl-present`)
- packaging timestamp

## Scripts

- `packaging/build-native-bridge.sh <rid> [output-root]`
- `packaging/package-app.sh <rid> [configuration] [output-root] [bridge-path]`
- `packaging/verify-layout.sh <rid> <configuration> [output-root]`

## Typical Release Flow

1. Build and test the solution.
2. Build the optional OpenSSL bridge if you want OpenSSL-enhanced CSR signing available in the package.
3. Publish the desktop app for a target RID.
4. Verify the publish layout.
5. Distribute the contents of `artifacts/publish/<rid>/Release/app/`.

For a release-candidate verification pass, also validate:

1. managed-only packaging
2. OpenSSL-enhanced packaging when a bridge artifact is available
3. startup diagnostics and Settings / Security diagnostics after launch

## Managed-only vs OpenSSL-enhanced Packages

- Managed-only package:
  - do not ship a bridge file
  - the app remains fully usable on the managed backend
- OpenSSL-enhanced package:
  - copy the bridge into `app/native/`
  - ensure system OpenSSL dependencies are available on the target machine

See the platform and troubleshooting notes in this folder for details.
