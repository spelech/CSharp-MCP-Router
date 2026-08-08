# 03. RBAC, Security & Approvals

The MCP Gateway Router implements multi-tenant, enterprise-grade security including Role-Based Access Control (RBAC), Identity Provider group mappings, and manual tool execution approval queues.

---

## 🛡️ Fine-Grained RBAC Policies

Access to backend servers and individual tools can be restricted by Active Directory / OIDC group membership:

1. Click **`Policy`** on any server card.
2. Define access rules:
   - **Allowed Groups**: Comma-separated list of group SIDs or group names (e.g. `full_admin, house_member`).
   - **Denied Groups**: Comma-separated list of explicitly forbidden groups.
   - **Default Behavior**: Allow or Deny by default when no specific policy rule matches.

---

## 👥 Group Mappings & Identity Resolution

The router integrates with 3 Identity Provider modes via `IIdentityProvider`:

1. **OIDC / Reverse Proxy Headers (`OidcHeader`)**:
   - Reads incoming HTTP headers passed by identity proxies like Authelia, TinyAuth, or PocketID:
     - `Remote-User`: User UPN / username (e.g. `steve`).
     - `Remote-Groups`: Comma-separated group claims (e.g. `full_admin, house_member`).
     - `Remote-Email`: User email address.
     - `Remote-Name`: User full name.

2. **Active Directory / Windows SID (`ActiveDirectory`)**:
   - Resolves Windows Kerberos / NTLM Security Identifiers (SIDs) and Active Directory group memberships.
   - Maps Windows Domain Groups (e.g. `S-1-5-32-544` / Domain Admins) to router permissions.

3. **AppKey Identity (`AppKey`)**:
   - Maps AppKeys issued to external clients/agents to assigned role scopes (`read:tools`, `execute:admin`, `full_access`).

---

## 🛑 Manual Tool Execution Approval Queue

For destructive or sensitive tools (e.g. `docker__remove_container` or `homeassistant__unlock_door`), the router supports **Manual Approval Mode**:

1. **Enabling Manual Approval**:
   - Go to **Settings View** -> Toggle `Require Manual Approval for Destructive Tools` ON.
   - Or set `RequireManualApproval = 1` in Settings.

2. **Approval Flow**:
   - When an AI agent calls a sensitive tool, the router intercepts the invocation and places the request into the **Pending Approvals Queue**.
   - The AI agent receives a pending notification response (`{"status": "pending_approval", "request_id": "req-12345"}`).
   - An administrator views the **Approvals Card** on the Dashboard and clicks **Approve** or **Reject**.
   - Upon approval, the tool execution resumes automatically and returns the result payload to the AI agent session.
