# Contributing to MCP Gateway Router

Thank you for your interest in contributing to the **CSharp-MCP-Router** project! We welcome contributions from the community.

Please take a moment to review this document to ensure a smooth, efficient development and review process.

---

## 📋 Table of Contents

1. [Code of Conduct](#-code-of-conduct)
2. [Development Environment Setup](#-development-environment-setup)
3. [Branching & Git Workflow](#-branching--git-workflow)
4. [Commit Conventions](#-commit-conventions)
5. [Mandatory Versioning Rule](#-mandatory-versioning-rule)
6. [CI Quality Gates & Verification](#-ci-quality-gates--verification)
7. [Screenshots & Documentation Standards](#-screenshots--documentation-standards)

---

## 🤝 Code of Conduct

We are committed to providing a welcoming, inclusive, and harassment-free experience for everyone. Be respectful, constructive, and open to feedback during code reviews and discussions.

---

## 💻 Development Environment Setup

Review the [**Developer Guide**](docs/developer-guide.md) for detailed environment setup instructions.

### Core Prerequisites
* **.NET 10.0 SDK** (`dotnet --version`)
* **Node.js 22 LTS & npm** (`node -v`, `npm -v`)
* **Git** (`git --version`)

```bash
# Clone the repository
git clone https://github.com/spelech/CSharp-MCP-Router.git
cd CSharp-MCP-Router

# Restore .NET dependencies
dotnet restore McpRouter.slnx

# Install frontend dependencies
cd frontend && npm install && cd ..
```

---

## 🌿 Branching & Git Workflow

1. Create a descriptive feature or bugfix branch from `main`:
   * Feature: `feat/issue-<number>-<short-description>`
   * Bug Fix: `fix/issue-<number>-<short-description>`
   * Documentation: `docs/issue-<number>-<short-description>`
   * Refactoring: `refactor/issue-<number>-<short-description>`

2. Keep branches focused and isolated. Avoid combining unrelated features in a single PR.

---

## 📝 Commit Conventions

We follow the [Conventional Commits](https://www.conventionalcommits.org/) standard. All commit messages must follow this structure:

```
<type>(<scope>): <short summary>

[optional body explaining motivation and architectural rationale]

[optional issue reference, e.g. Closes #58]
```

### Allowed Types
* `feat`: A new feature or capability.
* `fix`: A bug fix.
* `docs`: Documentation updates or additions only.
* `refactor`: Code restructuring without functional changes.
* `test`: Adding or correcting tests.
* `perf`: Performance improvements.
* `chore`: Build tooling, dependency updates, or repository maintenance.

### Atomic Commits Rule
* Organize changes into clean, logical atomic commits.
* Keep code modifications and documentation updates neatly structured and self-contained.

---

## 🏷️ Mandatory Versioning Rule

> [!IMPORTANT]
> **EVERY COMMIT OR MERGE TO `main` MUST BUMP THE VERSION NUMBER.**

* **Patch Bumps (e.g. `4.12.2` -> `4.12.3`)**: For bug fixes, performance optimizations, log refactoring, or minor UI tweaks.
* **Minor Bumps (e.g. `4.12.0` -> `4.13.0`)**: For new features, API endpoints, schema changes, or architectural additions.
* **Major Bumps (e.g. `4.0.0` -> `5.0.0`)**: For breaking protocol or architectural redesigns.

### Files That MUST Be Updated Simultaneously:
1. [`mcp-router.csproj`](mcp-router.csproj) (`<Version>`, `<AssemblyVersion>`, `<FileVersion>`).
2. [`frontend/src/stores/useUserStore.ts`](frontend/src/stores/useUserStore.ts) (React fallback version string).
3. [`CHANGELOG.md`](CHANGELOG.md) (Add a release entry to the Release Changelog table).
4. [`README.md`](README.md) (Update the top-5 release preview table).

---

## 🚦 CI Quality Gates & Verification

Before submitting a Pull Request, verify that all automated quality gates pass locally:

### 1. Backend Tests & Coverage
```bash
CI=true dotnet test McpRouter.slnx --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
```
* All 515+ tests must pass with 0 errors.

### 2. C# Formatting & Roslyn Analyzers
```bash
dotnet format McpRouter.slnx --verify-no-changes
```

### 3. Frontend Lint, Build, & Tests
```bash
cd frontend
npm run lint
npm run build
npm test
cd ..
```
* Zero ESLint warnings or TypeScript errors permitted.

For comprehensive details on pipeline jobs, CodeQL SAST scanning, and integration smoke tests, see [**CI Quality Gates & Security Workflows**](docs/ci-quality-gates.md).

---

## 📸 Screenshots & Documentation Standards

* **Real Screenshots Standard**: **AI-generated mockups or placeholder assets are strictly prohibited** in documentation. All assets in `docs/assets/` must be captured from the live application using the automated screenshot tool (`take_screenshots.js`).
* **Documentation Currency**: Every new feature or API change must include updated documentation in `docs/` and corresponding user guide updates.
