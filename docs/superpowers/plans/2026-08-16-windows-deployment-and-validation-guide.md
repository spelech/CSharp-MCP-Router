# Windows Deployment, IIS Handoff Guide & Environment Validation Toolkit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a complete, production-ready Windows deployment guide, IIS configuration assets, PowerShell automation scripts, and an environment validation toolkit for testing native Windows capabilities (Active Directory WindowsIdentity, Registry, DPAPI, and STDIO).

**Architecture:** We add a dedicated `scripts/windows/` folder containing production IIS `web.config.example`, PowerShell deployment and registry management scripts (`Deploy-IIS.ps1`, `Setup-WindowsService.ps1`, `Set-RegistrySecrets.ps1`, `Test-WindowsEnvironment.ps1`), and a detailed, end-to-end handoff guide in `docs/windows-deployment-and-validation-guide.md`.

**Tech Stack:** ASP.NET Core 10, IIS (ANCM v2), PowerShell 7 / Windows PowerShell 5.1, Windows DPAPI / Registry APIs, xUnit, Roslyn Catalog Generator.

## Global Constraints
- Target Version: `v4.17.0` (Minor bump for new deployment assets, scripts, and validation toolset).
- Synchronized Files: `mcp-router.csproj`, `frontend/package.json`, `frontend/src/shared/stores/useUserStore.ts`, `CHANGELOG.md`, `README.md`.
- SSE Streaming Requirement: IIS `web.config` must set `responseBufferLimit="0"` to disable response buffering for real-time Model Context Protocol SSE events.
- Zero-drift Requirements Catalog: `dotnet run --project scripts/CatalogGenerator -- --verify-only`.

---

### Task 1: Create IIS `web.config.example` & PowerShell Setup Scripts

**Files:**
- Create: `scripts/windows/web.config.example`
- Create: `scripts/windows/Deploy-IIS.ps1`
- Create: `scripts/windows/Setup-WindowsService.ps1`
- Create: `scripts/windows/Set-RegistrySecrets.ps1`

- [ ] **Step 1: Create `scripts/windows/web.config.example`**
  - Configure `aspNetCore` handler with `processPath="dotnet"`, `arguments=".\mcp-router.dll"`, `stdoutLogEnabled="false"`, `stdoutLogFile=".\logs\stdout"`, `hostingModel="inprocess"`, and `responseBufferLimit="0"`.
  - Add Windows Authentication and URL rewrite / security headers if needed.

- [ ] **Step 2: Create `scripts/windows/Deploy-IIS.ps1`**
  - Automate building backend (`dotnet publish -c Release -o <publishDir>`), building frontend (`npm run build`), creating/updating IIS AppPool with `.NET CLR Version: No Managed Code`, binding website, copying `web.config`, and verifying `/health`.

- [ ] **Step 3: Create `scripts/windows/Setup-WindowsService.ps1`**
  - Automate publishing self-contained/framework-dependent Windows binary and registering as a Windows Service with auto-recovery triggers.

- [ ] **Step 4: Create `scripts/windows/Set-RegistrySecrets.ps1`**
  - Helper script to write plaintext strings or DPAPI machine-encrypted byte arrays (`[System.Security.Cryptography.ProtectedData]::Protect(...)`) into `HKLM\SOFTWARE\McpRouter\Secrets`.

- [ ] **Step 5: Commit Task 1**
  ```bash
  git add scripts/windows/
  git commit -m "feat(windows): add IIS deployment, service setup, and registry secret automation scripts"
  ```

---

### Task 2: Create Diagnostic Validation Script (`Test-WindowsEnvironment.ps1`)

**Files:**
- Create: `scripts/windows/Test-WindowsEnvironment.ps1`

- [ ] **Step 1: Implement `Test-WindowsEnvironment.ps1`**
  - Check .NET 10 SDK / Runtime installation.
  - Verify Windows Registry `HKLM\SOFTWARE\McpRouter\Secrets` access.
  - Verify DPAPI `Protect` / `Unprotect` functionality.
  - Test Windows Identity extraction.
  - Run `dotnet test McpRouter.slnx` and output test summary.
  - Run `dotnet run --project scripts/CatalogGenerator -- --verify-only` for catalog verification.
  - Output structured Pass/Fail report.

- [ ] **Step 2: Commit Task 2**
  ```bash
  git add scripts/windows/Test-WindowsEnvironment.ps1
  git commit -m "feat(windows): add automated Windows environment diagnostic and validation runner"
  ```

---

### Task 3: Create Comprehensive Windows Deployment & Handoff Guide

**Files:**
- Create: `docs/windows-deployment-and-validation-guide.md`
- Modify: `README.md`
- Modify: `docs/runbook.md`

- [ ] **Step 1: Write `docs/windows-deployment-and-validation-guide.md`**
  - Document complete Windows deployment architectures (IIS In-Process, Windows Service, Kestrel Standalone).
  - Provide step-by-step instructions for the 4 validation scenarios (Windows Auth / Kerberos, Registry Secrets, STDIO Process execution, Test suite execution).
  - Document common troubleshooting scenarios (SSE response buffering in IIS, DPAPI scopes, Active Directory Kerberos SPN delegation).

- [ ] **Step 2: Link guide in `README.md` and `docs/runbook.md`**

- [ ] **Step 3: Commit Task 3**
  ```bash
  git add docs/windows-deployment-and-validation-guide.md README.md docs/runbook.md
  git commit -m "docs(windows): create comprehensive Windows deployment and validation handoff guide"
  ```

---

### Task 4: Release Bump to `v4.17.0` & Living Catalog Synchronization

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `frontend/package.json`
- Modify: `frontend/src/shared/stores/useUserStore.ts`
- Modify: `CHANGELOG.md`
- Modify: `README.md`
- Modify: `docs/requirements-catalog.json`
- Modify: `docs/software-requirements-and-test-catalog.md`

- [ ] **Step 1: Synchronize version to `v4.17.0` across all 5 mandatory files**
- [ ] **Step 2: Run catalog generator and verify zero drift**
- [ ] **Step 3: Run full verification test suite**
- [ ] **Step 4: Commit Task 4**
  ```bash
  git add -A
  git commit -m "release: bump version to v4.17.0 and establish Windows deployment and validation toolkit"
  ```
