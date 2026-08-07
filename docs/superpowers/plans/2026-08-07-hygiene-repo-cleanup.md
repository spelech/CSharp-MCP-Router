# CSharp-MCP-Router - Branch 1: Hygiene & Repo Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up build/runtime artifacts, resolve package vulnerabilities, and add dynamic frontend building to the Dockerfile.

**Architecture:** We will update Git ignore rules, purge committed log/DB files, introduce a `.dockerignore` file, and update the `Dockerfile` to build the React application dynamically inside a Node.js stage rather than copying pre-compiled artifacts.

**Tech Stack:** Git, Docker, .NET 10.0, Node.js 20, Vite.

## Global Constraints
- Target Framework: `net10.0`
- No warnings or errors during production build.
- Do not commit mockups or logs.

---

### Task 1: Clean Up Git Tracking & Ignore Patterns

**Files:**
- Modify: `.gitignore`
- Create: `.dockerignore`
- Delete from Git: `mcp_server.log`, `data/router.db`, `McpRouter.Tests/TestResults/*`, `wwwroot/assets/*`
- Move: `take_screenshots.js` ➡️ `scripts/take_screenshots.js`

- [ ] **Step 1: Create a new git branch**
  Run: `git checkout -b hygiene/v3.1.2-repo-cleanup`

- [ ] **Step 2: Update `.gitignore`**
  Add the following lines to the end of `.gitignore`:
  ```text
  # Build and runtime outputs
  mcp_server.log
  data/router.db*
  data/*.key
  **/TestResults/
  wwwroot/
  wwwroot/assets/
  ```

- [ ] **Step 3: Create `.dockerignore`**
  Create `/containers/dev/csharp-mcp-router/.dockerignore` with the following contents:
  ```text
  .git
  **/bin
  **/obj
  *.log
  data/*.db*
  data/*.key
  McpRouter.Tests
  TestResults
  node_modules
  wwwroot
  ```

- [ ] **Step 4: Untrack generated and runtime files from git index**
  Run:
  ```bash
  git rm --cached mcp_server.log
  git rm --cached data/router.db
  git rm -r --cached McpRouter.Tests/TestResults 2>/dev/null || true
  git rm -r --cached wwwroot/assets 2>/dev/null || true
  git rm --cached wwwroot/index.html 2>/dev/null || true
  ```

- [ ] **Step 5: Relocate the screenshot helper script**
  Run:
  ```bash
  git mv take_screenshots.js scripts/take_screenshots.js
  ```

- [ ] **Step 6: Commit intermediate cleanup**
  Run:
  ```bash
  git add .gitignore .dockerignore
  git commit -m "chore(hygiene): update gitignore, dockerignore, and remove runtime files from index"
  ```

---

### Task 2: Resolve NuGet Vulnerabilities & CA Warnings

**Files:**
- Modify: `mcp-router.csproj`
- Modify: `McpRouter.Tests/McpRouter.Tests.csproj`

- [ ] **Step 1: Remove `<NoWarn>NU1903</NoWarn>` suppression**
  Target: `mcp-router.csproj`
  Remove the line containing `<NoWarn>$(NoWarn);NU1903</NoWarn>`.

- [ ] **Step 2: Run vulnerability scan**
  Run: `dotnet list package --vulnerable`
  Identify vulnerable packages (e.g., `Azure.Identity` 1.10.3 and `Microsoft.Identity.Client` 4.56.0).

- [ ] **Step 3: Update vulnerable packages**
  Run:
  ```bash
  dotnet add package Azure.Identity --version 1.14.0
  dotnet add package Microsoft.Identity.Client --version 4.61.3
  ```
  *(Update `McpRouter.Tests` package references similarly if required)*

- [ ] **Step 4: Resolve Active Directory platform warnings**
  Add platform exclusions or compile guards for `CA1416` in `ActiveDirectoryIdentityProvider.cs` using:
  `#pragma warning disable CA1416` or check `OperatingSystem.IsWindows()` before accessing SIDs.

- [ ] **Step 5: Verify build compile succeeds without errors**
  Run: `dotnet build McpRouter.slnx --configuration Release`
  Expected: Success, 0 errors, no vulnerability audit failures.

- [ ] **Step 6: Commit changes**
  Run:
  ```bash
  git add mcp-router.csproj McpRouter.Tests/McpRouter.Tests.csproj
  git commit -m "fix(security): resolve NuGet package vulnerabilities and remove warning suppression"
  ```

---

### Task 3: Update Dockerfile for Dynamic Frontend Building

**Files:**
- Modify: `Dockerfile`

- [ ] **Step 1: Add Node compilation stages to Dockerfile**
  Modify `Dockerfile` to match:
  ```dockerfile
  # Stage 1: Build the React frontend SPA
  FROM node:20-alpine AS frontend-build
  WORKDIR /frontend
  COPY frontend/package*.json ./
  RUN npm ci
  COPY frontend/ ./
  RUN npm run build

  # Stage 2: Build the C# application
  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /source
  COPY mcp-router.csproj ./
  RUN dotnet restore mcp-router.csproj
  COPY . ./
  # Copy dynamically compiled frontend over to wwwroot
  COPY --from=frontend-build /wwwroot ./wwwroot
  RUN dotnet publish mcp-router.csproj -c Release -o /app

  # Stage 3: Final runtime image
  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
  WORKDIR /app
  RUN apt-get update && apt-get install -y --no-install-recommends \
      libsqlite3-dev \
      && rm -rf /var/lib/apt/lists/*
  COPY --from=build /app .
  EXPOSE 8080
  ENV ASPNETCORE_URLS=http://+:8080 \
      ASPNETCORE_ENVIRONMENT=Production
  ENTRYPOINT ["dotnet", "mcp-router.dll"]
  ```

- [ ] **Step 2: Build the local Docker image to verify build workflow**
  Run: `docker build -t csharp-mcp-router-test .`
  Expected: Clean compilation of React SPA followed by .NET app publish.

- [ ] **Step 3: Commit and run the version bump commit wrapper**
  Run: `./commit.sh "feat(hygiene): integrate dynamic frontend building into Dockerfile"`
  Expected: Version bumped to `3.1.2` and commit created successfully.
