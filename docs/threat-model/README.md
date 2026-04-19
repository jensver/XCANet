# Threat Model

Milestone 2 establishes the initial local protection model:

- the database profile stores KDF parameters plus an encrypted verifier, not the password itself
- master keys are derived with `PBKDF2-SHA256` and used with `AES-256-GCM` for private-key payload encryption
- plaintext private-key bytes are encrypted before persistence and cleared as narrowly as practical in application flow
- audit events record lifecycle and key-storage actions without logging secrets or decrypted material

Milestone 3 extends that model to the managed crypto path:

- RSA and ECDSA keys are generated in-memory through the managed backend, then stored only as encrypted PKCS#8 blobs
- certificate issuance, CSR creation, CSR signing, and PKCS#12 export require an unlocked database session before private-key use
- export of private key material supports encrypted PKCS#8 output through an explicit export password
- imported PFX bundles are unpacked into normalized certificate and encrypted private-key records rather than stored as opaque bundle blobs

The model is still intentionally local-only. OS-keystore wrapping, password rotation, auto-lock policy tuning, advanced issuance policy, and stricter import/export UX guardrails remain future work.
