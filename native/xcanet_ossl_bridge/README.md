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
