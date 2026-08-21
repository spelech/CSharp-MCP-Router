# MCP Request Auth End-to-End Flow

This diagram illustrates the complete, end-to-end authentication lifecycle of an MCP request passing from a Client, through the Router, to a backend MCP Server.

```mermaid
sequenceDiagram
    participant C as Client (IDE/LLM)
    participant R as Router Gateway
    participant DB as Router Database
    participant IDP as CompositeIdentityProvider
    participant SEC as CompositeSecretRetriever
    participant B as Backend MCP Server

    Note over C, B: Phase 1: Client to Router Authentication
    
    alt Uses AppKey (API Token)
        C->>R: HTTP Request (Header: Authorization / X-App-Key)
        R->>DB: Lookup AppKey by Prefix
        DB-->>R: AppKey Hash & Scopes
        R->>R: Validate SHA-256 Hash
    else Uses SSO/Proxy Header
        C->>R: HTTP Request (Headers: Remote-User, Remote-Groups)
        R->>IDP: Extract Identity from Headers
        IDP-->>R: Username & SIDs
    end
    
    R->>R: Establish Client Session & Claims (Username, SID, Scopes)
    
    Note over C, B: Phase 2: Router to Backend Authentication
    
    C->>R: Send MCP Protocol Message (e.g., tools/call)
    R->>DB: Fetch Backend Server Config
    DB-->>R: Server Details (SecretProvider, AuthShape)
    
    alt SecretProvider == UserProvided
        R->>DB: Fetch UserCredentialDto for (Username, ServerId)
        DB-->>R: Encrypted User Secret
        R->>R: Decrypt User Secret
    else SecretProvider == Vault / Environment / WindowsRegistry
        R->>SEC: GetSecretAsync(SecretPath, SecretKey)
        SEC-->>R: Global Target Secret
        R->>R: Cache Secret (5 mins)
    else SecretProvider == None
        R->>R: Use configured ApiKey (if any) or proceed without auth
    end
    
    Note over C, B: Phase 3: Proxying the Request
    
    R->>R: Format Secret using AuthShape (Bearer, Basic, Custom Header, etc.)
    R->>B: Forward HTTP/SSE Request with Injected Auth
    B-->>R: MCP Response
    R-->>C: Forward Response back to Client
```

### Auth & Setup Matrix Reference
This flow highlights how the different matrices combine:
1. **Client Identity**: The router knows *who* is making the request (Username from SSO, or Username/OwnerSid from an AppKey).
2. **Server Auth Requirement**: The backend server expects a specific credential format (`AuthShape`).
3. **Secret Origin**: The router fulfills the backend's requirement by fetching a global secret (`Vault`, `Env`) OR a per-user secret (`UserProvided`), bridging the gap between the authenticated client and the target server transparently.

> [!WARNING]
> **Dynamic Token Limitation:** The gateway does not support *retrieving* dynamic tokens (like short-lived JWTs) from internal Auth endpoints on behalf of the user. All configured `SecretProviders` (including `UserProvided`) yield **static** credentials.
>
> If a backend requires a dynamic JWT with claims or timestamp checks, the client (IDE) must fetch the token itself and send it to the router using the `X-Target-Auth` header (provided `AllowPassThroughAuth` is enabled on the server config). The router will then format this token according to the backend's `AuthShape` (e.g., standard Bearer header) before forwarding the request.
