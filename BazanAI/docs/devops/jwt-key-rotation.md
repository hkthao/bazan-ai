# JWT Key Rotation Strategy

This document outlines the strategy for rotating RSA key pairs used for JWT signing (RS256) in BazanAI.

## 1. Key Specifications
- **Algorithm:** RS256 (RSA with SHA-256)
- **Key Size:** 2048-bit
- **Format:** PKCS#8 (Private), SPKI (Public) in PEM files.

## 2. Rotation Frequency
- **Standard:** Quarterly (Every 90 days).
- **Emergency:** Immediate rotation if any private key is suspected of compromise.

## 3. Zero-Downtime Rotation Process (24h Dual-Key Period)

### Step 1: Generate New Key Pair (T-Minus 24h)
Generate a new RSA key pair but do **NOT** replace the existing one yet.
```bash
./infra/generate-jwt-keys.sh --new
```

### Step 2: Dual-Key Period (Active Rotation)
1. Identity Service starts accepting **both** the old and new public keys for verification.
2. Identity Service continues to use the **old** private key for signing new tokens.
3. This ensures tokens issued right before the switch remain valid.

### Step 3: Switch Signing Key (The Cutover)
1. Update Identity Service configuration to use the **new** private key for signing.
2. New tokens are now signed with the new key.
3. Old tokens (still valid) are verified using the old public key (still in JWKS).

### Step 4: Decommission Old Key (T-Plus 24h)
1. Once the maximum token TTL (e.g., 24h) has passed, remove the old public key from the JWKS.
2. Delete the old private key file.

## 4. Rollback Procedure
If the new key causes validation failures:
1. Revert Identity Service to use the old private key for signing.
2. Keep the new public key in JWKS until all tokens signed with it have expired.

## 5. Security Checklist
- [ ] Private key permissions set to `400`.
- [ ] Keys are **NEVER** committed to version control.
- [ ] Rotation is logged in the audit trail (excluding key material).
