# Design Specification: Security Hardening & Blocker Remediation

**Date:** 2026-08-07  
**Baseline Version:** `3.6.2`  
**Status:** Approved  

---

## 1. Overview & Objectives

This document specifies the architectural changes required to make the gateway fit for exposing external Model Context Protocol (MCP) servers to an internal agent/tool network. It addresses the blockers (P0) and production-readiness requirements (P1) from the remediation work order.

The core design principle is **Fail-Closed**: any validation, authentication, authorization, secret retrieval, or audit logging step that cannot guarantee a definitive *allow* must result in a *deny* or a fast startup termination.

---

## 2. P0 Blocker Specifications

### P0-1: Fail-Closed Audit Logging
*   **Mechanism:** Wrap all backend actions (`CallToolAsync`, `ReadResourceAsync`, `GetPromptAsync`) in a try-finally block.
*   **Audit-Before-Success Semantics:** 
    *   If `Audit:FailClosed` is `true` (default), the `AuditInvocationAsync` method will propagate any database write errors by throwing them.
    *   If the database write throws an exception, the client receives a `503 Service Unavailable` with a clear "Security audit record could not be persisted" error, masking any successful backend execution result.
    *   If `Audit:FailClosed` is `false`, failed writes are written to a local database outbox table `FailedAuditLogs` for background retry, incrementing a Prometheus metric `audit_write_failures_total`.

### P0-2: Cross-Platform Active Directory Integration
*   **ILdapService Abstraction:** Define an interface `ILdapService` with `ResolveUserSidsAsync(string username)` returning the user's `objectSid` and `tokenGroups` SIDs.
*   **System.DirectoryServices.Protocols:** Implement the interface using LDAP protocols to query Active Directory on Linux.
*   **SID-Based Authorization:**
    *   Add configuration `Admin:GroupSid` (defaults to the built-in Administrators SID `S-1-5-32-544`).
    *   The `AdminPolicy` and role mapping check will match the SIDs list resolved by `ILdapService` against `Admin:GroupSid`.
    *   Remove all raw string checks (e.g. `admin`, `system`, `Administrators`, `full_admin`).

### P0-3: Trusted Proxy Header Stripping
*   **Enforcement:** Set `RequireTrustedProxy = true` by default.
*   **IP Verification & Stripping:** Validate the immediate remote IP and forwarded chain. If not in the `Oidc:TrustedProxies` allowlist:
    *   Strip all `Remote-*` headers (e.g. `Remote-User`, `Remote-Groups`).
    *   Route `/api/me` and the AppKey handler through this same proxy gate to prevent spoofed header extraction.

### P0-4: Per-Server HashiCorp Vault Integration
*   **Database Schema & UI:** Add columns `SecretMount`, `SecretPath`, and `SecretField` to the `McpServer` table and display them in the frontend configuration modal.
*   **Fail-Closed Secrets:** If a server specifies `SecretProvider = Vault`, the retriever routes the query strictly to Vault. Any failure to retrieve the secret from Vault fails closed (throws an exception) and does not fall back to environmental variables or static API keys.
*   **HTTPS Scheme Guard:** Validate the Vault URL address during startup/configuration. If the scheme is `http://`, throw a validation exception and refuse to boot.
*   **Just-in-Time Token Renewal:** Before reading a secret, check if the cached Vault login token is expiring within 5 minutes. If so, execute a synchronous AppRole re-login.

### P0-5: Cryptography Hardening & AppKey Hashing
*   **Sourced Master Key:** Retrieve the symmetric encryption master key from Vault or an environment variable. If absent, throw a fast startup exception and refuse to boot.
*   **PBKDF2 Key Derivation:** Use PBKDF2 with 100,000 iterations and a stored salt to derive column keys from the master key.
*   **AES-GCM Authenticated Encryption:** Replace AES-CBC column encryption with AES-GCM to prevent padding oracle attacks.
*   **One-Time Startup Migration:** During database seeding, the gateway will locate any legacy AES-CBC-encrypted AppKeys, decrypt them, save their SHA-256 hash, and clear the reversible key columns.

### P0-6: Socket-Level SSRF Connect Protection
*   **ConnectCallback Interceptor:** Configure the gateway's HttpClient with a `SocketsHttpHandler` containing a custom `ConnectCallback`.
*   **SSRF Denylist:** After DNS resolution, validate the target IP address against loopback, link-local (`169.254.169.254`), CGNAT (`100.64.0.0/10`), private ranges, multicast, and IPv4-mapped IPv6 (`::ffff:a.b.c.d`).
*   **Configurable Allowlist:** Allow connection to private ranges only if the hostname or IP is explicitly allowlisted in `Security:AllowedIpRanges`.
*   **Docker Discovery Integration:** Apply the same IP checks to auto-discovered Docker containers.

### P0-7: Request-Scoped SSE Authorization
*   **Stateless Request Auth:** On the shared `/sse` stream, resolve the caller's identity and evaluate RBAC policies per-message rather than caching the handshake context, preventing session hijacking.

---

## 3. P1 & P2 Hardening Specifications

*   **P1-1: Opaque Session IDs:** Replace raw bearer tokens used as session keys with server-generated UUIDs bound to the principal.
*   **P1-2: PII Log Redaction:** Sanitize transport payloads, console logs, and request middleware to redact authorization headers, cookies, and tokens.
*   **P1-3: Consolidated RBAC Evaluator:** Unify the four divergent RBAC checks into a single `AccessEvaluator` class and add cross-dialect parity tests.
*   **P1-4: Audit API:** Add an admin-only authenticated API endpoint `GET /api/audit` for exporting and querying audit trails.
*   **P1-5: Persistent Certs:** In non-development environments, load persistent X.509 certificates and fail fast if missing (do not fall back to ephemeral).
*   **P1-6: Uniform Server-Id Canonicalization:** Reconcile URI host lowercasing to handle uppercase and underscored server IDs uniformly.

---

## 4. Test Verification Plan

*   **Fail-Closed Audit Test:** Verify that when the database is offline, `CallToolAsync` rejects requests and does not execute the backend.
*   **SSRF Protection Test:** Verify that attempts to connect to loopback or link-local addresses are terminated at the socket connection phase.
*   **AppKey Hashing Migration Test:** Verify that legacy encrypted keys are successfully migrated to SHA-256 hashes at startup.
