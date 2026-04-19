# OpenSSL Interop

## Scope

The native bridge lives under `native/xcanet_ossl_bridge` and exposes a small C ABI.

M6 intentionally keeps the bridge surface small:

- version retrieval
- capability reporting
- self-test
- CSR signing
- native buffer release

## ABI Design

The bridge exports flat C functions only.

Key structs:

- `xcanet_error`
- `xcanet_capabilities`
- `xcanet_buffer`

The managed layer never sees OpenSSL pointers or C++ classes.

## Memory Ownership

Ownership is explicit:

- input buffers are owned by managed code
- output buffers allocated by native code are returned through `xcanet_buffer`
- managed code must call `xcanet_ossl_free_buffer` deterministically after copying data

## Error Model

Native failures use a stable error shape:

- numeric code
- short message
- optional detail string

The interop client converts these into managed `OperationResult` failures before the crypto backend sees them.

## Platform Loading

`XcaNet.Interop.OpenSsl` resolves the bridge from:

- an explicit configured path
- `XCANET_OPENSSL_BRIDGE_PATH`
- `<app>/`
- `<app>/native/`
- `<app>/bridges/`
- `<app>/runtimes/<rid>/native/`
- common local repository output locations used during development

If loading fails:

- diagnostics report `IsAvailable = false`
- the app still starts
- routing falls back to the managed backend unless OpenSSL-only execution was explicitly requested
- load diagnostics include attempted paths, architecture/runtime hints, and dependency guidance

## Native Build

Use `native/xcanet_ossl_bridge/build-bridge.sh <output-dir>` to produce a local bridge artifact.

M9 adds explicit packaging conventions for the native artifact. The bridge remains optional and can be omitted entirely when only the managed backend is desired.
