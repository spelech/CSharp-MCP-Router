# ID Mapping Blueprint

This blueprint maps the unannotated test domains to their respective requirement IDs.

- **Backend C# Tests** (`McpRouter.Tests/*.cs`):
  - Rate limiting tests -> `[Requirement("RATE-01", "GUARD", RequirementType.Negative, "Rate limiting restricts excessive requests")]`
  - Config validation tests -> `[Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]`
  - Audit logging tests -> `[Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]`
  - General auth/identity tests -> `AUTH-01` to `AUTH-03`
  - Transport/Streaming tests -> `TRANS-01` to `TRANS-03`

- **Frontend Vitest Tests** (`frontend/src/test/**/*.tsx`):
  - UI state and dashboard -> `@requirement UI-01`
  - Auth integration tests -> `@requirement AUTH-02`

- **Playwright E2E Tests** (`frontend/e2e/**/*.ts`):
  - Full end-to-end MCP routing -> `@requirement MCP-01`
  - Login/SSO flows -> `@requirement AUTH-01`