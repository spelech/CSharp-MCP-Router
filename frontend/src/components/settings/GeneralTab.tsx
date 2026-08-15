import React, { useState } from 'react';
import { EmbeddingSettings } from '../../shared/types';

export interface GeneralTabProps {
  settings: EmbeddingSettings | null;
  saveEmbeddingSettings: (settings: EmbeddingSettings) => Promise<boolean>;
}

export const GeneralTab: React.FC<GeneralTabProps> = ({ settings, saveEmbeddingSettings }) => {
  const [embProvider, setEmbProvider] = useState(settings?.embeddingProvider || 'local');
  const [embModelDir, setEmbModelDir] = useState(settings?.embeddingModelDir || 'data/models');
  const [embApiUrl, setEmbApiUrl] = useState(settings?.embeddingApiUrl || 'http://litellm:4000/v1/embeddings');
  const [embApiModel, setEmbApiModel] = useState(settings?.embeddingApiModel || 'all-MiniLM-L6-v2');
  const [embApiKey, setEmbApiKey] = useState(settings?.embeddingApiKey || '');
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  const handleSaveSearchSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaveStatus('saving');
    const success = await saveEmbeddingSettings({
      embeddingProvider: embProvider,
      embeddingModelDir: embModelDir,
      embeddingApiUrl: embApiUrl,
      embeddingApiModel: embApiModel,
      embeddingApiKey: embApiKey,
    });
    setSaveStatus(success ? 'saved' : 'error');
    setTimeout(() => setSaveStatus('idle'), 2500);
  };

  return (
    <div id="subview-search" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '600px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-gear"></i> Semantic Search Settings
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Configure the embedding model used for matching query intents to tools. Changes are saved securely to the database.
        </p>
        <form id="settings-form" onSubmit={handleSaveSearchSettings}>
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
