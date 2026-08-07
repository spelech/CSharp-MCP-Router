# CSharp-MCP-Router - Branch 5: Audit Trail Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-step. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement log buffer PII sanitization, structured admin audit logging across SQLite/MySQL/MSSQL, and verify with tests.

**Architecture:**
1. Update `LogBuffer.Add` to filter log messages and exceptions through `PiiSanitizer`.
2. Add `LogAdminActionAsync` to `IAuditLogger` and implement it in `AuditLogger.cs`.
3. Create `AuditLogs` and `AdminAuditLogs` SQLite tables during database seeding.
4. Update MySQL and MSSQL scripts to define the `AdminAuditLogs` table and its insertion stored procedure `sp_InsertAdminAuditLog`.
5. Call `LogAdminActionAsync` inside create/update/delete endpoints for servers, embedding settings, and secret/auth providers.
6. Write a test case checking `LogBuffer` sanitization.

---

### Task 1: Sanitize LogBuffer Entries

**Files:**
- Modify: `Services/InMemoryLogger.cs`

- [ ] **Step 1: Sanitize message and exceptions before enqueuing**
  In `/containers/dev/csharp-mcp-router/Services/InMemoryLogger.cs`, inside `LogBuffer.Add`:
  - Run message through `PiiSanitizer.SanitizePayload`.
  - If exception is not null, run its string representation through `PiiSanitizer.SanitizePayload`.

---

### Task 2: Implement Admin Audit Logging Interface & SQLite Tables

**Files:**
- Modify: `Core/Logging/AuditLogger.cs`
- Modify: `Services/DatabaseSeederService.cs`

- [ ] **Step 1: Declare LogAdminActionAsync in IAuditLogger & implement in AuditLogger**
  In `/containers/dev/csharp-mcp-router/Core/Logging/AuditLogger.cs`:
  - Define `LogAdminActionAsync` interface method.
  - Implement it to insert into `AdminAuditLogs` table (falling back to raw SQL for SQLite, calling `sp_InsertAdminAuditLog` for SQL Server/MySQL).

- [ ] **Step 2: Initialize AuditLogs and AdminAuditLogs tables for SQLite**
  In `/containers/dev/csharp-mcp-router/Services/DatabaseSeederService.cs`, run `CREATE TABLE IF NOT EXISTS AuditLogs` and `CREATE TABLE IF NOT EXISTS AdminAuditLogs` to initialize both tables in SQLite.

---

### Task 3: Support MySQL and MS SQL Server Audit Tables/Procedures

**Files:**
- Modify: `scripts/db/mysql/01_tables.sql`
- Modify: `scripts/db/mysql/02_procedures.sql`
- Modify: `scripts/db/mssql/01_tables.sql`
- Modify: `scripts/db/mssql/02_procedures.sql`

- [ ] **Step 1: Define MySQL table and stored procedure**
  - Add `AdminAuditLogs` table creation to `scripts/db/mysql/01_tables.sql`.
  - Add `sp_InsertAdminAuditLog` stored procedure to `scripts/db/mysql/02_procedures.sql`.

- [ ] **Step 2: Define MSSQL table and stored procedure**
  - Add `AdminAuditLogs` table creation to `scripts/db/mssql/01_tables.sql`.
  - Add `sp_InsertAdminAuditLog` stored procedure to `scripts/db/mssql/02_procedures.sql`.

---

### Task 4: Log Admin Actions in Controllers & Minimal APIs

**Files:**
- Modify: `Extensions/ApplicationBuilderExtensions.cs`
- Modify: `Controllers/ProvidersController.cs`

- [ ] **Step 1: Audit minimal API server/settings writes**
  In `/containers/dev/csharp-mcp-router/Extensions/ApplicationBuilderExtensions.cs`:
  - Log `UpdateServer` in `PUT /api/servers/{id}`.
  - Log `CreateServer` in `POST /api/servers`.
  - Log `DeleteServer` in `DELETE /api/servers/{id}`.
  - Log `UpdateSettings` in `POST /api/settings`.

- [ ] **Step 2: Audit controller dynamic provider writes**
  In `/containers/dev/csharp-mcp-router/Controllers/ProvidersController.cs`:
  - Log `SaveSecretProvider` in `POST /api/providers/secrets`.
  - Log `SaveAuthProvider` in `POST /api/providers/auth`.

---

### Task 5: Verify and Commit

- [ ] **Step 1: Add LogBuffer sanitization unit test**
  In `/containers/dev/csharp-mcp-router/McpRouter.Tests/PiiSanitizerTests.cs`, add `LogBuffer_Add_Sanitizes_PII_Payloads` to verify sanitization.

- [ ] **Step 2: Compile the project**
  Run: `dotnet build McpRouter.slnx --configuration Release`

- [ ] **Step 3: Run all tests**
  Run: `dotnet test McpRouter.slnx`

- [ ] **Step 4: Commit and bump version**
  Run: `./commit.sh "feat(security): implement LogBuffer PII sanitization and structured admin audit logging"`
  Expected: Version bumped to `3.5.0` (minor bump).
