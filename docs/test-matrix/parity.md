# Parity Coverage

M6 parity coverage is intentionally limited to the first OpenSSL-backed operation:

- sign CSR into certificate

## Covered Assertions

The parity tests verify:

- managed signing succeeds
- OpenSSL signing succeeds when the bridge is present
- both outputs are parseable through the backend-neutral certificate parser
- subject is preserved
- issuer is preserved
- SAN entries are preserved
- CA/leaf status remains consistent
- backend-used diagnostics are visible in the result DTO

## Missing By Design

M6 does not yet attempt parity across:

- self-signed CA creation
- key generation
- PKCS#12 export
- CRL generation
- advanced extension editing

Those areas remain on the managed path until a larger OpenSSL surface is justified.
