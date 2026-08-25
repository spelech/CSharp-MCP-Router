#!/usr/bin/env bash
# ==============================================================================
# Release & Quality Verification Script (Issue #59)
# Model Context Gateway (MCG)
# ==============================================================================
set -euo pipefail

# Resolve script directory and repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

cd "${REPO_ROOT}"

# Execute python release verification engine with all passed arguments
exec python3 "${SCRIPT_DIR}/verify_release.py" "$@"
