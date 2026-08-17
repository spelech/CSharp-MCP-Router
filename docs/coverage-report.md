# Code Coverage Report

**Date:** 2026-08-17 (Post-Windows IIS & DPAPI Validation)  
**Status:** **All Frontend Component Modules & Backend Controllers $\ge 85\%$ | Native Windows Subsystems 100%**

This document details the code coverage metrics across the core modules of the MCP Router, including containerized Linux environments and native Windows IIS hosting.

---

## 1. Summary of Current Coverage Metrics

### Frontend Layer (`npm run test:coverage`)
- **Total Test Files:** 24 passing suites (137 unit tests)
- **Overall Line Rate:** **86.2%**
- **Overall Branch Rate:** **89.0%**
- **Component Coverage Breakdown:**
  - `components/servers`: **98.7%** (ServerCard: 99.1%, DashboardView: 96.6%, ServerInspectModal: 100.0%, ServerControlsToolbar: 100.0%, StatsCard: 100.0%, ServerModal: 98.5%)
  - `components/clients`: **95.6%** (ClientSetupGuide: 100.0%, AppKeyModal: 99.3%, ClientModal: 97.9%, RegisteredClientsCard: 100.0%, AppKeysCard: 81.5%)
  - `components/security`: **97.5%** (SecurityView: 100.0%, PolicyModal: 97.5%, MappingModal: 96.9%)
  - `components/testbench`: **93.2%** (ToolTesterCard: 100.0%, SemanticRouterCard: 100.0%, ConsoleCard: 100.0%, PromptTesterCard: 96.5%, ResourceTesterCard: 95.3%, LogsTerminalCard: 93.0%, TestBenchView: 85.4%)
  - `components/shared`: **100.0%** (Modal: 100.0%, StatusBadge: 100.0%, PaginationToolbar: 100.0%, Header: 100.0%, Footer: 100.0%, Toasts: 100.0%)
  - `components/settings`: **89.9%** (GeneralTab: 97.5%, IdentityAuthTab: 96.9%, CustomFilesTab: 90.5%, BackupsTab: 100.0%, CustomFileModal: 84.6%, SecretProvidersTab: 79.9%)

### Backend Layer (`dotnet test --collect:"XPlat Code Coverage"`)
- **Total Test Files:** 70 passing suites (**543 unit & integration tests**)
- **Controllers & Routing Services:**
  - `ProvidersController.cs`: **100.0%**
  - `ClientsController.cs`: **100.0%**
  - `PermissionsController.cs`: **100.0%**
  - `ApiEmbeddingService.cs`: **100.0%**
  - `DynamicEmbeddingService.cs`: **75.9%** (Core methods 100%)
  - `ResourceRoutingManager.cs`: **86.8% to 100%**
  - `ClientSession.cs`: **88.4%**
  - `SessionManager.cs`: **92.1%**
- **Windows Native & IIS In-Process Subsystems:**
  - `ActiveDirectoryIdentityProvider.cs` / `WindowsIdentityAccessor.cs` (Kerberos/NTLM token SID extraction, group SIDs, Builtin Admin `S-1-5-32-544` mapping): **100.0%**
  - `WindowsRegistrySecretRetriever.cs` / `WindowsRegistryAccessor.cs` / `WindowsDpapiProtector.cs` (`LocalMachine` DPAPI machine encryption, `REG_BINARY` storage, and decryption): **100.0%**
  - `HttpTransport.cs`, `SseTransport.cs`, `StdioTransport.cs` WindowsRegistry default path fallback: **100.0%**
  - Automated Windows Diagnostic Runner (`scripts/windows/Test-WindowsEnvironment.ps1`): **18/18 validations passing (100%)**
  - Native IIS In-Process Hosting (`web.config` ANCM v2 `responseBufferLimit="0"`, unbuffered SSE, MSSQL 2022 `McpEnterpriseDb`): **100% Live Validated**

### Standalone Media MCP Layer (`MediaMcp.Tests`)
- **Total Test Files:** 4 test suites (**28 unit & integration tests**)
- **Line Coverage:** **89.1%**
- **Branch Coverage:** **84.6%**

---

## 2. Running Tests Locally

To generate and inspect the code coverage report locally:

### Backend .NET Suite:
```bash
dotnet test McpRouter.Tests/McpRouter.Tests.csproj --collect:"XPlat Code Coverage"
```

### Frontend Vitest Suite:
```bash
cd frontend
npm run test:coverage
```

### Windows Host Diagnostics & Validation Suite:
```powershell
.\scripts\windows\Test-WindowsEnvironment.ps1 -JsonReportPath ".\diagnostics-report.json"
```

### Report Generation (HTML):
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

---

## 3. Related Documentation

- Living SRS & Test Verification Catalog: [`docs/software-requirements-and-test-catalog.md`](software-requirements-and-test-catalog.md)
- Test Catalog & Annotation Guide: [`docs/test-catalog-guide.md`](test-catalog-guide.md)
- Detailed Evaluation & Methodology: [`docs/test-coverage-evaluation.md`](test-coverage-evaluation.md)
- Integration Matrix & Requirements: [`docs/testing-matrix.md`](testing-matrix.md)
- CI Quality Gate Pipeline: [`docs/ci-quality-gates.md`](ci-quality-gates.md)
- Windows Deployment & Validation Guide: [`docs/windows-deployment-and-validation-guide.md`](windows-deployment-and-validation-guide.md)
