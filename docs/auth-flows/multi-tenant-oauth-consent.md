# Multi-Tenant OAuth Consent Flow & Dynamic Client Registration (DCR)

To securely support dynamic, multi-tenant AI integrations (such as the **Slack MCP Server** or **Splunk MCP** integrations), Model Context Gateway (MCG) natively supports the standard OAuth 2.0 `authorization_code` and `refresh_token` flows. This allows external IDEs and agents to request fine-grained access to a user's isolated backend resources.

## 1. The Interactive Consent Architecture

Rather than relying purely on static API keys or upstream proxy headers for all client interactions, the router acts as an **OAuth 2.0 Authorization Server** using OpenIddict. 

### The React Consent Screen
When an external client (like Slack) initiates an authorization request, it redirects the user to the router's `/connect/authorize` endpoint. The backend processes the OpenIddict state and issues an HTTP 302 redirect to the React frontend at `/consent`.

The React application renders a secure, interactive consent screen:
* Displays the dynamically registered `client_name`.
* Summarizes the requested MCP access.
* Provides **Accept** and **Deny** form actions that `POST` directly back to `/connect/authorize`.

## 2. Inbound User Authentication Strategies

When a user is redirected to the router to view the consent screen, they must first be authenticated. The router handles this seamlessly across different network topologies:

### Option A: OIDC / Zero-Trust Proxy (e.g., GWS-MCP, Cloudflare Access)
In modern cloud deployments, the exposed router sits behind an identity-aware proxy. 
* The proxy intercepts the request and forces the user to log in (e.g., via Google Workspace).
* Once authenticated, the proxy forwards the request to the router with an `X-Forwarded-User` or `X-Amzn-Oidc-*` header.
* The router's `OidcHeaderAuthenticationHandler` instantly authenticates the user, bypassing any need for a local login screen, and renders the consent UI.

### Option B: Enterprise Internal Network (Windows Auth / AD)
If the router is deployed on an internal enterprise network (without an OIDC proxy) and configured with IIS/Kestrel:
* The user's browser negotiates silently with the router using **NTLM or Kerberos** (Windows Authentication).
* The ASP.NET Core pipeline populates `HttpContext.User` with a `WindowsPrincipal`.
* The router seamlessly reads the `WindowsPrincipal`, extracts the `DOMAIN\User` identity, and renders the consent screen without requiring manual credential entry.

## 3. The Code Exchange & Refresh Tokens

Once the user clicks **Accept**, the router issues a short-lived `authorization_code` and redirects the user back to the client's `redirect_uri` (e.g., Slack's backend).

1. **Access Tokens**: The client exchanges the code at `/connect/token` for a JWT Access Token. This token contains the authenticated user's identity bound as the `Subject` claim.
2. **Refresh Tokens**: To prevent the user from having to repeatedly consent every hour, the router issues a long-lived `refresh_token` (enabled via `AllowRefreshTokenFlow`). The client stores this securely and uses it to silently renew the access token when it expires.
3. **Execution**: The client then uses the Access Token as a Bearer token to execute MCP tools on the router. The router logs the execution and enforces Row-Level Security (RLS) under the context of the user who originally consented.

## Summary of Client Registration & Persistence Isolation

In MCG v5.1.0+, OAuth 2.0 / 2.1 client applications and RFC 7591 Dynamic Client Registrations are persisted in a dedicated, isolated **`OAuthClients`** table across SQLite, Microsoft SQL Server, and MySQL. This completely separates machine and interactive OAuth applications from static API keys (`AppKeys`):

* **SHA-256 Secret Hashing**: The `client_secret` is hashed using SHA-256 immediately upon generation. Plaintext secrets are **only returned once** during registration and are never persisted to disk or exposed in management APIs.
* **Privilege Decoupling**: Machine credentials registered via DCR or the management UI are created with `OwnerSid = ''`, ensuring machine applications do not inherit administrator SIDs or elevated privileges.
* **Multi-Dialect Persistence**: Backed by `sp_SaveOAuthClient`, `sp_GetOAuthClients`, `sp_GetOAuthClientById`, and `sp_DeleteOAuthClient` in MSSQL and MySQL, and atomic parameterized queries with upsert semantics in SQLite.

When a new client is provisioned (either via the UI, the `/api/register` DCR endpoint, or the `/api/clients` API), it is configured with:
* `grant_types`: `["authorization_code", "refresh_token", "client_credentials"]`
* `response_types`: `["code"]`
* `token_endpoint_auth_method`: `"client_secret_post"` or `"client_secret_basic"`
* `redirect_uris`: Whitelisted callback URLs registered dynamically or manually

## 4. Step-by-Step Setup Guide (Example: Slack MCP Integration)

Follow these steps to configure an external multi-tenant AI client (like Slack) to authenticate against Model Context Gateway (MCG) using the Interactive Consent Flow:

### Step 1: Register the Dynamic Client
Before Slack can redirect users to the router, it must be registered as an OAuth Client. 
You can do this via the router's UI (**App Keys & Security > Dynamic Client Registration**) or via a direct API call to `/api/register` (RFC 7591):
```json
// POST https://your-router-url.com/api/register
{
  "client_name": "Slack MCP Workspace App",
  "redirect_uris": ["https://slack.com/oauth/callback"],
  "grant_types": ["authorization_code", "refresh_token", "client_credentials"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "client_secret_post"
}
```
**Save the Output:** The router will return HTTP `201 Created` with a `client_id` and one-time plaintext `client_secret`. 

### Step 2: Configure the Client (Slack App)
In your Slack App configuration portal (or equivalent external platform):
1. **Client ID**: Paste the `client_id` generated in Step 1.
2. **Client Secret**: Paste the `client_secret`.
3. **Authorization URL**: Set this to `https://your-router-url.com/connect/authorize`
4. **Token URL**: Set this to `https://your-router-url.com/connect/token`
5. **Scopes**: Add the scope `api`.
6. **Redirect URI**: Note the callback URL provided by Slack (e.g., `https://slack.com/oauth/callback...`).

*(Note: The Model Context Gateway (MCG) currently accepts any valid redirect URI during the authorization request as long as the client is registered).*

### Step 3: Trigger the Flow
1. A user interacts with your Slack App and requests to use an MCP Tool.
2. Slack detects the user lacks a token and presents them with a "Sign in to Router" button.
3. The user clicks the button, which opens their browser and navigates to the router's **Authorization URL**.

### Step 4: User Authentication & Consent
1. **Authentication**: If the router is behind a Zero-Trust Proxy (like Cloudflare/GWS), the proxy intercepts the request, forces the user to log in via SSO, and forwards them to the router. Alternatively, on an internal corporate network, Windows Auth logs them in silently.
2. **Consent**: The router renders the React consent screen (`/consent`). The user sees: *"Slack MCP Workspace App is requesting access to your MCP isolated backend resources."*
3. The user clicks **Authorize**.

### Step 5: Token Exchange & Execution
1. The router redirects the user back to Slack's **Redirect URI** with an `authorization_code`.
2. Slack automatically calls the router's **Token URL** behind the scenes to exchange the code for an `access_token` and a `refresh_token`.
3. Slack executes MCP commands by attaching `Authorization: Bearer <access_token>` to its HTTP/SSE requests.
4. When the access token expires (typically after 1 hour), Slack seamlessly uses the `refresh_token` to retrieve a new access token without bothering the user.
