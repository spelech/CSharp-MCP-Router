#!/usr/bin/env bash
# ==============================================================================
# CSharp-MCP-Router Automated Provisioning Script (JSON-RPC over /admin)
# ==============================================================================
# Usage:
#   ROUTER_URL="http://localhost:8080" ADMIN_KEY="mcp-global-admin-default-cli-key-99" ./automate-setup.sh
# ==============================================================================
set -euo pipefail

ROUTER_URL="${ROUTER_URL:-http://localhost:8080}"
ADMIN_KEY="${ADMIN_KEY:-mcp-global-admin-default-cli-key-99}"

echo "========================================================"
echo " Starting MCP Router Automated Provisioning"
echo " Target: ${ROUTER_URL}"
echo "========================================================"

call_admin_tool() {
  local tool_name="$1"
  local arguments_json="$2"

  local payload
  payload=$(cat <<EOF
{
  "jsonrpc": "2.0",
  "id": "$(date +%s%N)",
  "method": "tools/call",
  "params": {
    "name": "${tool_name}",
    "arguments": ${arguments_json}
  }
}
EOF
)

  curl -s -f -X POST "${ROUTER_URL}/admin/message" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer ${ADMIN_KEY}" \
    -d "${payload}"
}

# 1. Probe diagnostics
echo "[1/4] Probing gateway health & diagnostics..."
DIAG_RESP=$(call_admin_tool "manage_system" '{"action":"diagnostics"}')
echo "Diagnostics response: ${DIAG_RESP}"

# 2. Configure Authentik / Forward-Auth (example)
echo "[2/4] Configuring default Forward-Auth identity provider..."
AUTH_CONFIG=$(cat <<'EOF'
{
  "action": "save_auth",
  "providerName": "HeaderAuth",
  "displayName": "Authentik SSO",
  "userHeader": "Remote-User",
  "groupsHeader": "Remote-Groups",
  "isEnabled": true,
  "configJson": "{\"trustedProxies\":[\"127.0.0.1\",\"10.0.0.0/8\",\"172.16.0.0/12\",\"192.168.0.0/16\"]}"
}
EOF
)
AUTH_RESP=$(call_admin_tool "manage_providers" "${AUTH_CONFIG}")
echo "Auth provider response: ${AUTH_RESP}"

# 3. Create Group Mapping (Admin role)
echo "[3/4] Creating admin group mappings..."
MAPPING_CONFIG=$(cat <<'EOF'
{
  "action": "save",
  "externalId": "full_admin",
  "internalGroup": "full_admin"
}
EOF
)
MAPPING_RESP=$(call_admin_tool "manage_group_mappings" "${MAPPING_CONFIG}")
echo "Group mapping response: ${MAPPING_RESP}"

# 4. Provision Developer AppKey
echo "[4/4] Issuing developer AppKey..."
KEY_CONFIG=$(cat <<'EOF'
{
  "action": "create",
  "name": "DevOps Automation Key",
  "username": "devops-agent",
  "scopes": ["all"],
  "expiresInDays": 365
}
EOF
)
KEY_RESP=$(call_admin_tool "manage_appkeys" "${KEY_CONFIG}")
echo "AppKey response: ${KEY_RESP}"

echo "========================================================"
echo " MCP Router Automated Provisioning Completed Successfully"
echo "========================================================"
