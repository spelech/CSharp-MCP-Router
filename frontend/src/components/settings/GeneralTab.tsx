import React, { useState } from 'react';
import { EmbeddingSettings } from '../../shared/types';

export interface GeneralTabProps {
  settings: EmbeddingSettings | null;
  saveEmbeddingSettings: (settings: EmbeddingSettings) => Promise<boolean>;
}

export const GeneralTab: React.FC<GeneralTabProps> = ({ settings, saveEmbeddingSettings }) => {
  return <GeneralTabForm key={JSON.stringify(settings)} settings={settings} saveEmbeddingSettings={saveEmbeddingSettings} />;
};

const GeneralTabForm: React.FC<GeneralTabProps> = ({ settings, saveEmbeddingSettings }) => {
  const [dashboardTitle, setDashboardTitle] = useState(settings?.dashboardTitle ?? 'MCP Gateway');
  const [dashboardIcon, setDashboardIcon] = useState(settings?.dashboardIcon ?? 'fa-solid fa-network-wired');
  const [embProvider, setEmbProvider] = useState(settings?.embeddingProvider ?? 'local');
  const [embModelDir, setEmbModelDir] = useState(settings?.embeddingModelDir ?? 'data/models');
  const [embApiUrl, setEmbApiUrl] = useState(settings?.embeddingApiUrl ?? 'http://litellm:4000/v1/embeddings');
  const [embApiModel, setEmbApiModel] = useState(settings?.embeddingApiModel ?? 'all-MiniLM-L6-v2');
  const [embApiKey, setEmbApiKey] = useState(settings?.embeddingApiKey ?? '');
  const [allowOpenDCR, setAllowOpenDCR] = useState(settings?.allowOpenClientRegistration ?? true);
  const [userMaxKeys, setUserMaxKeys] = useState<number>(settings?.userMaxKeys ?? 5);
  const [globalMaxKeys, setGlobalMaxKeys] = useState<number>(settings?.globalMaxKeys ?? 100);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  const handleSaveSearchSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaveStatus('saving');
    const success = await saveEmbeddingSettings({
      dashboardTitle,
      dashboardIcon,
      embeddingProvider: embProvider,
      embeddingModelDir: embModelDir,
      embeddingApiUrl: embApiUrl,
      embeddingApiModel: embApiModel,
      embeddingApiKey: embApiKey,
      allowOpenClientRegistration: allowOpenDCR,
      userMaxKeys: Number(userMaxKeys),
      globalMaxKeys: Number(globalMaxKeys),
    });
    setSaveStatus(success ? 'saved' : 'error');
    setTimeout(() => setSaveStatus('idle'), 2500);
  };

  return (
    <div id="subview-search" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '600px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-gear"></i> General Settings
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Configure branding and semantic search settings. Changes are saved securely to the database.
        </p>
        <form id="settings-form" onSubmit={handleSaveSearchSettings}>
          
          <div className="form-group">
            <h3 style={{ fontSize: '14px', marginBottom: '10px', marginTop: '10px', color: 'var(--primary)' }}>Dashboard Branding</h3>
          </div>
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="settings-dashboard-title">Dashboard Title</label>
              <input
                type="text"
                id="settings-dashboard-title"
                placeholder="MCP Gateway"
                value={dashboardTitle}
                onChange={(e) => setDashboardTitle(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label htmlFor="settings-dashboard-icon">Header Icon (FontAwesome class)</label>
              <input
                type="text"
                id="settings-dashboard-icon"
                placeholder="fa-solid fa-network-wired"
                value={dashboardIcon}
                onChange={(e) => setDashboardIcon(e.target.value)}
              />
            </div>
          </div>
          
          <div className="form-group">
            <h3 style={{ fontSize: '14px', marginBottom: '10px', marginTop: '20px', color: 'var(--primary)' }}>Semantic Search</h3>
          </div>
          <div className="form-group">
            <label htmlFor="settings-provider">Embedding Provider</label>
            <select
              id="settings-provider"
              value={embProvider}
              onChange={(e) => setEmbProvider(e.target.value)}
              required
            >
              <option value="local">Local ONNX Model (In-Process, CPU-Friendly)</option>
              <option value="api">External Embedding API (LiteLLM, Open WebUI, OpenAI)</option>
            </select>
          </div>

          {embProvider === 'local' ? (
            <div id="settings-local-group">
              <div className="form-group">
                <label htmlFor="settings-model-dir">Local Model Directory (inside volume)</label>
                <input
                  type="text"
                  id="settings-model-dir"
                  placeholder="data/models"
                  value={embModelDir}
                  onChange={(e) => setEmbModelDir(e.target.value)}
                />
                <small style={{ color: 'var(--text-muted)' }}>
                  The <code>all-MiniLM-L6-v2</code> ONNX files will be downloaded automatically to this directory on first run.
                </small>
              </div>
            </div>
          ) : (
            <div id="settings-api-group">
              <div className="form-group">
                <label htmlFor="settings-api-url">Embedding API URL</label>
                <input
                  type="text"
                  id="settings-api-url"
                  placeholder="http://litellm:4000/v1/embeddings"
                  value={embApiUrl}
                  onChange={(e) => setEmbApiUrl(e.target.value)}
                />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="settings-api-model">API Model Name</label>
                  <input
                    type="text"
                    id="settings-api-model"
                    placeholder="all-MiniLM-L6-v2"
                    value={embApiModel}
                    onChange={(e) => setEmbApiModel(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label htmlFor="settings-api-key">API Key (Optional)</label>
                  <input
                    type="password"
                    id="settings-api-key"
                    placeholder="API password / auth token"
                    value={embApiKey}
                    onChange={(e) => setEmbApiKey(e.target.value)}
                  />
                </div>
              </div>
            </div>
          )}
          
          <div className="form-group">
            <h3 style={{ fontSize: '14px', marginBottom: '10px', marginTop: '20px', color: 'var(--primary)' }}><i className="fa-solid fa-shield-halved"></i> Security Defaults</h3>
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="settings-user-max-keys">Default User Quota (UserMaxKeys)</label>
              <input
                type="number"
                id="settings-user-max-keys"
                min="0"
                placeholder="5"
                value={userMaxKeys}
                onChange={(e) => setUserMaxKeys(parseInt(e.target.value, 10) || 0)}
              />
              <small style={{ color: 'var(--text-muted)' }}>
                Default maximum personal App Keys per user (0 = unlimited).
              </small>
            </div>
            <div className="form-group">
              <label htmlFor="settings-global-max-keys">Global Max Keys</label>
              <input
                type="number"
                id="settings-global-max-keys"
                min="0"
                placeholder="100"
                value={globalMaxKeys}
                onChange={(e) => setGlobalMaxKeys(parseInt(e.target.value, 10) || 0)}
              />
              <small style={{ color: 'var(--text-muted)' }}>
                Maximum total active App Keys allowed across all users (0 = unlimited).
              </small>
            </div>
          </div>
          
          <div className="form-group" style={{ backgroundColor: 'rgba(255,255,255,0.02)', padding: '15px', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.05)' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer', margin: 0 }}>
              <input
                type="checkbox"
                checked={allowOpenDCR}
                onChange={(e) => setAllowOpenDCR(e.target.checked)}
                style={{ width: '18px', height: '18px', margin: 0, cursor: 'pointer' }}
              />
              <strong style={{ fontSize: '14px', color: '#e2e8f0' }}>Allow Open Dynamic Client Registration (RFC 7591)</strong>
            </label>
            <p style={{ margin: '8px 0 0 28px', fontSize: '12px', color: 'var(--text-muted)' }}>
              If enabled, third-party clients (like Gemini Spark) can programmatically register themselves via <code>/api/register</code> without requiring an Admin AppKey.
            </p>
          </div>

          <div className="settings-actions" style={{ marginTop: '25px', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <button
              type="submit"
              className="btn btn-primary"
              id="btn-save-settings"
              disabled={saveStatus === 'saving'}
              style={{
                backgroundColor: saveStatus === 'saved' ? '#10b981' : saveStatus === 'error' ? '#ef4444' : '',
              }}
            >
              {saveStatus === 'saving' && (
                <>
                  <i className="fa-solid fa-spinner fa-spin"></i> Saving...
                </>
              )}
              {saveStatus === 'saved' && (
                <>
                  <i className="fa-solid fa-check"></i> Saved!
                </>
              )}
              {saveStatus === 'error' && (
                <>
                  <i className="fa-solid fa-triangle-exclamation"></i> Error
                </>
              )}
              {saveStatus === 'idle' && (
                <>
                  <i className="fa-solid fa-floppy-disk"></i> Save Settings
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
