# xcanet_ossl_bridge

This directory contains the thin native OpenSSL bridge introduced in M6.

Current scope:

- version reporting
- capability reporting
- self-test
- CSR signing
- native output-buffer release

Build a local artifact with:

```bash
./build-bridge.sh <output-dir>
```

The bridge exposes a C ABI only and is intentionally small. Application and UI layers never talk to it directly.

For packaged app layouts introduced in M9, the expected runtime locations are:

- `<app>/native/`
- `<app>/bridges/`
- `<app>/runtimes/<rid>/native/`

You can also point the app at a specific bridge file through:

- `XCANET_OPENSSL_BRIDGE_PATH`
- `Crypto:OpenSslBridgePath`
