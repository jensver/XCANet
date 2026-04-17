# Threat Model

Milestone 2 establishes the initial local protection model:

- the database profile stores KDF parameters plus an encrypted verifier, not the password itself
- master keys are derived with `PBKDF2-SHA256` and used with `AES-256-GCM` for private-key payload encryption
- plaintext private-key bytes are encrypted before persistence and cleared as narrowly as practical in application flow
- audit events record lifecycle and key-storage actions without logging secrets or decrypted material

The model is intentionally local-only for now. OS-keystore wrapping, password rotation, auto-lock policy tuning, and export controls remain future work.
