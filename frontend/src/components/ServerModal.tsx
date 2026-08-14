import React, { useState } from 'react';
import { useServerStore } from '../stores/useServerStore';

interface ServerPayload {
  id?: string;
  displayName: string;
  type: string;
  categories: string[];
  url: string;
  secretProvider: string;
  secretItemKey: string;
  authShape: string;
  customHeaderName: string;
  apiKey?: string;
  enabled: boolean;
  hidden: boolean;
}

const ServerModalDialog: React.FC = () => {
  const { editingServer, saveServer, closeAddEditModal } = useServerStore();

  const [displayName, setDisplayName] = useState(editingServer?.displayName || '');
  const [type, setType] = useState(editingServer?.type || 'sse');
  const [category, setCategory] = useState(
    editingServer?.categories ? editingServer.categories.join(', ') : (editingServer ? 'default' : 'infrastructure')
  );
  const [url, setUrl] = useState(editingServer?.url || '');
  const [secretProvider, setSecretProvider] = useState(editingServer?.secretProvider || 'None');
  const [secretKey, setSecretKey] = useState(editingServer?.secretItemKey || '');
  const [authShape, setAuthShape] = useState(editingServer?.authShape || 'bearer');
  const [customHeaderName, setCustomHeaderName] = useState(editingServer?.customHeaderName || '');
  const [apiKey, setApiKey] = useState('');
  const [enabled, setEnabled] = useState(editingServer ? editingServer.enabled : true);
  const [hidden, setHidden] = useState(editingServer ? editingServer.hidden : false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    console.error('Submitting ServerModal form...');
    const serverPayload: ServerPayload = {
      displayName,
      type,
      categories: category.split(',').map((s) => s.trim()).filter(Boolean),
      url,
      secretProvider,
      secretItemKey: secretKey,
      authShape,
      customHeaderName,
      enabled,
      hidden,
    };
    if (editingServer) {
      serverPayload.id = editingServer.id;
    }
    if (apiKey) {
      serverPayload.apiKey = apiKey;
    }

    try {
      await saveServer(serverPayload);
    } catch {
      // Error is handled upstream or ignored
    }
  };

  const showCustomHeaderName = authShape === 'custom-header' || authShape === 'query';

  return (
    <div className="modal-backdrop" id="server-modal" style={{ display: 'flex' }}>
      <div className="glass-card modal-card">
        <div className="modal-header">
          <h2>
            <i className="fa-solid fa-server"></i> {editingServer ? 'Edit MCP Server' : 'Add MCP Server'}
          </h2>
          <button className="btn-close" onClick={closeAddEditModal}>
            &times;
          </button>
        </div>
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="server-name">Display Name</label>
            <input
              type="text"
              id="server-name"
              placeholder="e.g. Notes RAG"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              required
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="server-type">Transport Type</label>
              <select
                id="server-type"
                value={type}
                onChange={(e) => setType(e.target.value)}
                required
              >
                <option value="sse">SSE</option>
                <option value="http">HTTP</option>
                <option value="streamable">Streamable HTTP</option>
                <option value="stdio">STDIO</option>
              </select>
            </div>
            <div className="form-group">
              <label htmlFor="server-category">Category</label>
              <input
                type="text"
                id="server-category"
                placeholder="e.g. infrastructure"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="server-url">Connection URL</label>
            <input
              type="text"
              id="server-url"
              placeholder="e.g. http://notes-rag-mcp:3000/sse"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              required
            />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="server-secret-provider">Secret Provider</label>
              <select
                id="server-secret-provider"
                value={secretProvider}
                onChange={(e) => setSecretProvider(e.target.value)}
              >
                <option value="None">None (Static API Token)</option>
                <option value="Vault">HashiCorp Vault (KV v2)</option>
                <option value="WindowsRegistry">Windows Registry (DPAPI)</option>
                <option value="Environment">Environment Variables</option>
              </select>
            </div>
            <div className="form-group">
              <label htmlFor="server-secret-key">Secret Key / Item Name</label>
              <input
                type="text"
                id="server-secret-key"
                placeholder="e.g. slack/bot-token or HOMEASSISTANT_TOKEN"
                value={secretKey}
                onChange={(e) => setSecretKey(e.target.value)}
              />
            </div>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="server-auth-shape">Auth Token Format / Shape</label>
              <select
                id="server-auth-shape"
                value={authShape}
                onChange={(e) => setAuthShape(e.target.value)}
              >
                <option value="bearer">Bearer Token (Authorization: Bearer &lt;token&gt;)</option>
                <option value="basic">Basic Auth (Authorization: Basic &lt;token&gt;)</option>
                <option value="raw">Raw Auth Header (Authorization: &lt;token&gt;)</option>
                <option value="x-api-key">X-API-Key Header (X-API-Key: &lt;token&gt;)</option>
                <option value="custom-header">Custom Header Name (e.g. Slack-Token: &lt;token&gt;)</option>
                <option value="query">URL Query Parameter (e.g. ?token=&lt;token&gt;)</option>
              </select>
            </div>
            {showCustomHeaderName && (
              <div className="form-group" id="group-custom-header-name">
                <label htmlFor="server-custom-header-name">Custom Header / Query Name</label>
                <input
                  type="text"
                  id="server-custom-header-name"
                  placeholder="e.g. Slack-Bot-Token or token"
                  value={customHeaderName}
                  onChange={(e) => setCustomHeaderName(e.target.value)}
                />
              </div>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="server-key">Static API Token / Secret (Fallback)</label>
            <input
              type="password"
              id="server-key"
              placeholder="Fallback API token if secret provider is not used"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
            />
          </div>

          <div className="form-row checkbox-row">
            <div className="checkbox-group">
              <label className="switch">
                <input
                  type="checkbox"
                  id="server-enabled"
                  checked={enabled}
                  onChange={(e) => setEnabled(e.target.checked)}
                />
                <span className="slider"></span>
              </label>
              <span className="checkbox-label">Enabled</span>
            </div>
            <div className="checkbox-group">
              <label className="switch">
                <input
                  type="checkbox"
                  id="server-hidden"
                  checked={hidden}
                  onChange={(e) => setHidden(e.target.checked)}
                />
                <span className="slider"></span>
              </label>
              <span className="checkbox-label">Hidden</span>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn btn-secondary" onClick={closeAddEditModal}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" id="btn-save">
              Save Server
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export const ServerModal: React.FC = () => {
  const { isAddEditOpen, editingServer } = useServerStore();

  if (!isAddEditOpen) return null;

  return <ServerModalDialog key={editingServer?.id || 'new'} />;
};
