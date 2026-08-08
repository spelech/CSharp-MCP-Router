# 06. System Settings & Vector Embeddings

The **Settings View** (`Settings` in header) controls system-wide vector search configurations, approval thresholds, authentication modes, and server default settings.

---

## 🧠 Vector Embedding Engine Settings

The router uses vector embeddings to power Meta-Mode `search_tools`. You can choose between local CPU inference or external API providers:

### 1. Local ONNX Engine (Default)
- Model: `All-MiniLM-L6-v2` ONNX model.
- Requires no external API keys or external network calls.
- In-process execution with fast CPU vector calculations.

### 2. OpenAI / Ollama API Provider
- **Embedding Provider**: Select `OpenAI` or `Ollama`.
- **API URL**: Base endpoint (e.g. `https://api.openai.com/v1` or `http://localhost:11434`).
- **API Key**: API key for authentication.
- **Model Name**: Model name (e.g. `text-embedding-3-small` or `nomic-embed-text`).

---

## 🛡️ Security & Approval Parameters

- **Require Manual Approval**: Toggle ON to mandate human admin approval on destructive tools.
- **Global Max App Keys**: Maximum number of active AppKeys allowed globally (default `100`).
- **User Max App Keys**: Maximum AppKeys per user principal (default `5`).
- **OpenIddict Certificate Path**: Persistent X.509 certificate path for production OAuth2 signing.

---

## 💾 Saving Settings

Click **Save Settings** at the bottom of the form to apply changes instantly across all active client sessions.
