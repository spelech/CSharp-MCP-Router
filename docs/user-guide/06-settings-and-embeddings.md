# 06. System Settings & Vector Embeddings

The **Settings View** (`Settings` tab) provides a comprehensive configuration plane for adjusting vector embedding search engines, security approval policies, authentication providers, enterprise secret retrievers, custom JSON specification files, and access control matrices.

---

## 🧭 Settings Sub-Navigation Tabs

The Settings interface is organized into 6 modular domain tabs:

```
+---------------------------------------------------------------------------------------------+
| ⚙️ System Settings                                                                          |
+---------------------------------------------------------------------------------------------+
| [🧠 Vector & Search] [🛡️ Security & Approvals] [🪪 Identity & Auth] [🔐 Secret Providers]   |
| [📂 Prompts & Resources] [👥 Access Control]                                                |
+---------------------------------------------------------------------------------------------+
```

---

## 🧠 Tab 1: Vector & Search Engine (`GeneralTab`)

The vector embedding engine powers the **Meta-Mode** dynamic discovery pipeline (`search_tools`), matching natural language agent queries against the tool catalog.

```
+-------------------------------------------------------------------------------+
| 🧠 Vector Embedding & Semantic Search Configuration                           |
+-------------------------------------------------------------------------------+
| Embedding Engine:     (•) Local ONNX In-Process   ( ) External API Provider   |
|                                                                               |
| [Local ONNX Engine Parameters]                                                |
| Model Architecture:   All-MiniLM-L6-v2 (384-dimensional dense vectors)        |
| Model Cache Path:     [ /data/models                                       ]  |
| Execution Provider:   CPU Multi-Threaded In-Process (Microsoft.ML.OnnxRuntime) |
|                                                                               |
| [External API Provider Parameters]                                            |
| Provider Type:        [ OpenAI / Compatible (LiteLLM, Ollama) ▾ ]             |
| Base API Endpoint:    [ https://api.openai.com/v1                          ]  |
| API Secret Key:       [ sk-proj-********************************           ]  |
| Model Identifier:     [ text-embedding-3-small                             ]  |
|                                                                               |
| [ Save Vector Settings ]                                                      |
+-------------------------------------------------------------------------------+
```

### 1. Local ONNX Engine (Recommended / Default)
* **Model**: Embedded `All-MiniLM-L6-v2` transformer model.
* **Zero External Dependencies**: Runs entirely in-process on CPU using `Microsoft.ML.OnnxRuntime` and `Microsoft.ML.Tokenizers`.
* **Privacy & Air-Gapped**: Queries and tool descriptions never leave the local container.
* **Auto-Initialization**: Model weights and tokenizers are automatically verified and cached in the persistent models directory (`/data/models`).

### 2. External API Provider (OpenAI / Ollama / LiteLLM)
* **Usage**: Ideal when standardizing across external embedding models or connecting to a shared GPU inference cluster.
* **Supported Backends**: OpenAI, Azure OpenAI, Ollama, LiteLLM, Open WebUI, and vLLM.
* **Secure Storage**: External API keys are encrypted at rest in the database using authenticated AES-256-GCM envelope encryption.

---

## 🛡️ Tab 2: Security & Approvals (`SecurityTab`)

Controls global execution policies and administrative barriers:

* **Require Manual Approval for Destructive Tools**:
  * When enabled, tool invocations matching destructive patterns (e.g. `rm`, `delete`, `drop`, `restart`, `unlock`) are paused in the approval queue until confirmed by an administrator.
* **Global Max AppKeys**: Limits the total number of active AppKeys that can be generated across the system (default: `100`).
* **User Max AppKeys**: Restricts the maximum AppKeys any single user principal can issue (default: `5`).
* **Dynamic Configuration Reload**: Automatically reloads downstream server definitions when underlying database records or secret providers change.

---

## 🪪 Tab 3: Identity & Auth Providers (`IdentityAuthTab`)

Manages incoming authentication and user identity resolution:

### 1. Active Directory / Windows SID Provider
* Enable or disable Windows Kerberos/NTLM authentication.
* Configure Domain Controller endpoints, Base DN, and service account credentials.

### 2. OIDC / Reverse Proxy Headers Provider
* Enable or disable header-based identity inspection from reverse proxies (TinyAuth, PocketID, Authelia).
* Custom header field names: `Remote-User`, `Remote-Groups`, `Remote-Email`, `Remote-Name`.

### 3. OpenIddict OAuth 2.0 Authorization Server
* Configure OAuth 2.0 token lifetimes (Access Token TTL, Refresh Token TTL).
* X.509 signing certificate paths and encryption keys for distributed microservice trust.

---

## 🔐 Tab 4: Secret Providers (`SecretProvidersTab`)

Manage centralized configurations for external secret stores:

```
+-------------------------------------------------------------------------------+
| 🔐 Enterprise Secret Providers                                                |
+-------------------------------------------------------------------------------+
| [ HashiCorp Vault KV v2 ]                         [ Status: 🟢 Connected ]   |
| Vault Address:       [ http://vault:8200                                   ]  |
| Mount Point:         [ secret                                              ]  |
| Authentication Mode: [ AppRole ▾ ]                                            |
| Role ID:             [ 4a3e21b0-****-****-****-****************            ]  |
| Secret ID:           [ ************************************                ]  |
| JIT Renewal Window:  [ 300 ] seconds (Auto-renews if TTL < 5 min)             |
|                                                                               |
| [ Windows Registry (DPAPI) ]                      [ Status: 🟢 Active    ]   |
| Registry Hive:       [ LocalMachine (HKLM) ▾ ]                                |
| Base Subkey Path:    [ SOFTWARE\Homelab\McpSecrets                         ]  |
|                                                                               |
| [ Save Provider Settings ]                                                    |
+-------------------------------------------------------------------------------+
```

---

## 📂 Tab 5: Prompts & Resources File Manager (`CustomFilesTab`)

Create and manage custom JSON files that define virtual tools, prompt templates, and virtual resource endpoints:

* **File Catalog Grid**: Lists all registered specification files with file name, type (`Tools`, `Prompts`, `Resources`), and last updated timestamp.
* **Interactive Editor**: Built-in JSON editor with validation before persistence.
* **Hot Reload**: Instantly updates catalog caches and vector embeddings upon saving.

---

## 👥 Tab 6: Access Control & Group Mappings (`AccessControlTab`)

Fine-tune enterprise permissions across servers and external groups:

* **Group Mappings Table**: Define mappings that translate external Identity Provider groups (e.g. `CN=IT-Admins,OU=Groups,DC=corp`) to simplified internal roles (`full_admin`).
* **Server Policies Table**: Complete matrix of server-level access rules:
  * Server Identifier
  * Allowed Groups (e.g. `full_admin, house_member`)
  * Denied Groups (e.g. `contractors`)
  * Default Allow / Deny policy flag
