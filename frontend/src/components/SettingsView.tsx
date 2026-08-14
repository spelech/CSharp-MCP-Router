import React, { useEffect, useState } from 'react';
import { useSettingsStore, EmbeddingSettings, AuthProviderConfig, SecretProviderConfig } from '../stores/useSettingsStore';

interface VectorSearchTabProps {
  settings: EmbeddingSettings | null;
  saveEmbeddingSettings: (settings: EmbeddingSettings) => Promise<boolean>;
}

const VectorSearchTab: React.FC<VectorSearchTabProps> = ({ settings, saveEmbeddingSettings }) => {
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
      requireManualApproval: settings?.requireManualApproval ?? false,
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

interface SecurityApprovalsTabProps {
  requireApproval: boolean;
  onToggleApproval: (checked: boolean) => void;
}

const SecurityApprovalsTab: React.FC<SecurityApprovalsTabProps> = ({ requireApproval, onToggleApproval }) => {
  return (
    <div id="subview-security" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '600px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-shield-halved"></i> Security &amp; Safety Controls
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Enforce human-in-the-loop validation for executing destructive or sensitive tools.
        </p>
        <div className="form-group">
          <div className="checkbox-group" style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <label className="switch">
              <input
                type="checkbox"
                id="settings-require-approval"
                checked={requireApproval}
                onChange={(e) => onToggleApproval(e.target.checked)}
              />
              <span className="slider"></span>
            </label>
            <span className="checkbox-label" style={{ fontWeight: 500 }}>
              Require Manual Approval for Dangerous Tools
            </span>
          </div>
          <small style={{ color: 'var(--text-muted)', display: 'block', marginTop: '5px', marginLeft: '50px' }}>
            When enabled, tool execution requests targeting databases, docker containers, files, or smart devices will prompt the user in the UI before running.
          </small>
        </div>
      </div>
    </div>
  );
};

interface IdentityAuthTabProps {
  providers: AuthProviderConfig[];
  saveAuthProvider: (provider: AuthProviderConfig) => Promise<void>;
}

const IdentityAuthTab: React.FC<IdentityAuthTabProps> = ({ providers, saveAuthProvider }) => {
  const ad = providers.find((p) => p.providerName === 'ActiveDirectory');
  const oidc = providers.find((p) => p.providerName === 'PocketID_TinyAuth');

  const [authAdEnabled, setAuthAdEnabled] = useState(ad ? ad.isEnabled : false);
  const [authOidcEnabled, setAuthOidcEnabled] = useState(oidc ? oidc.isEnabled : true);
  const [authUserHeader, setAuthUserHeader] = useState(oidc?.userHeader || 'Remote-User');
  const [authGroupsHeader, setAuthGroupsHeader] = useState(oidc?.groupsHeader || 'Remote-Groups');

  const handleSaveAuthProviders = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await saveAuthProvider({
        providerName: 'ActiveDirectory',
        displayName: 'Active Directory',
        isEnabled: authAdEnabled,
      });
      await saveAuthProvider({
        providerName: 'PocketID_TinyAuth',
        displayName: 'PocketID / TinyAuth OIDC',
        userHeader: authUserHeader,
        groupsHeader: authGroupsHeader,
        isEnabled: authOidcEnabled,
      });
      alert('Auth Provider configurations saved successfully!');
    } catch {
      alert('Failed to save Auth Providers');
    }
  };

  return (
    <div id="subview-identity" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '800px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-id-card"></i> Identity &amp; Auth Providers
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Enable authentication providers for user identity context and group policy authorization.
        </p>
        <form id="auth-providers-form" onSubmit={handleSaveAuthProviders}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
            <div style={{ padding: '15px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                <h4 style={{ margin: 0 }}>
                  <i className="fa-solid fa-brands fa-windows"></i> Active Directory
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    id="auth-ad-enabled"
                    checked={authAdEnabled}
                    onChange={(e) => setAuthAdEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Integrate Kerberos/NTLM Windows SIDs for enterprise group policies.</p>
            </div>

            <div style={{ padding: '15px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '10px' }}>
                <h4 style={{ margin: 0 }}>
                  <i className="fa-solid fa-key"></i> PocketID / TinyAuth OIDC
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    id="auth-oidc-enabled"
                    checked={authOidcEnabled}
                    onChange={(e) => setAuthOidcEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Parse Remote-User and Remote-Groups HTTP headers from reverse proxies.</p>
              <div className="form-group" style={{ marginTop: '10px' }}>
                <label htmlFor="auth-user-header" style={{ fontSize: '11px' }}>
                  User Header Name
                </label>
                <input
                  type="text"
                  id="auth-user-header"
                  value={authUserHeader}
                  onChange={(e) => setAuthUserHeader(e.target.value)}
                  style={{ fontSize: '12px' }}
                />
              </div>
              <div className="form-group" style={{ marginTop: '5px' }}>
                <label htmlFor="auth-groups-header" style={{ fontSize: '11px' }}>
                  Groups Header Name
                </label>
                <input
                  type="text"
                  id="auth-groups-header"
                  value={authGroupsHeader}
                  onChange={(e) => setAuthGroupsHeader(e.target.value)}
                  style={{ fontSize: '12px' }}
                />
              </div>
            </div>
          </div>
          <div style={{ marginTop: '15px', textAlign: 'right' }}>
            <button type="submit" className="btn btn-primary btn-sm">
              <i className="fa-solid fa-floppy-disk"></i> Save Auth Config
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

interface SecretProvidersTabProps {
  providers: SecretProviderConfig[];
  saveSecretProvider: (provider: SecretProviderConfig) => Promise<void>;
}

const SecretProvidersTab: React.FC<SecretProvidersTabProps> = ({ providers, saveSecretProvider }) => {
  const vault = providers.find((p) => p.providerName === 'Vault');
  const winreg = providers.find((p) => p.providerName === 'WindowsRegistry');
  const env = providers.find((p) => p.providerName === 'Environment');

  const parsedVault = vault?.configJson
    ? (() => {
        try {
          return JSON.parse(vault.configJson);
        } catch {
          return {};
        }
      })()
    : {};

  const parsedWinreg = winreg?.configJson
    ? (() => {
        try {
          return JSON.parse(winreg.configJson);
        } catch {
          return {};
        }
      })()
    : {};

  const parsedEnv = env?.configJson
    ? (() => {
        try {
          return JSON.parse(env.configJson);
        } catch {
          return {};
        }
      })()
    : {};

  const [secVaultEnabled, setSecVaultEnabled] = useState(vault ? vault.isEnabled : false);
  const [secVaultAddress, setSecVaultAddress] = useState(parsedVault.address || '');
  const [secVaultToken, setSecVaultToken] = useState(parsedVault.token || '');
  const [secVaultPath, setSecVaultPath] = useState(parsedVault.mountPath || '');

  const [secWinregEnabled, setSecWinregEnabled] = useState(winreg ? winreg.isEnabled : false);
  const [secWinregKey, setSecWinregKey] = useState(parsedWinreg.keyPath || '');

  const [secEnvEnabled, setSecEnvEnabled] = useState(env ? env.isEnabled : false);
  const [secEnvPrefix, setSecEnvPrefix] = useState(parsedEnv.prefix || '');

  const handleSaveSecretProviders = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const vaultConfig = {
        address: secVaultAddress,
        token: secVaultToken,
        mountPath: secVaultPath,
      };

      const winregConfig = {
        keyPath: secWinregKey,
      };

      const envConfig = {
        prefix: secEnvPrefix,
      };

      await saveSecretProvider({
        providerName: 'Vault',
        displayName: 'HashiCorp Vault (KV v2)',
        configJson: JSON.stringify(vaultConfig),
        isEnabled: secVaultEnabled,
      });
      await saveSecretProvider({
        providerName: 'WindowsRegistry',
        displayName: 'Windows Registry (DPAPI)',
        configJson: JSON.stringify(winregConfig),
        isEnabled: secWinregEnabled,
      });
      await saveSecretProvider({
        providerName: 'Environment',
        displayName: 'Container Environment',
        configJson: JSON.stringify(envConfig),
        isEnabled: secEnvEnabled,
      });
      alert('Secret Provider configurations saved successfully!');
    } catch {
      alert('Failed to save Secret Providers');
    }
  };

  return (
    <div id="subview-secrets" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '800px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-vault"></i> Secret Providers
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Configure external vault and registry secret providers for resolving downstream MCP tokens.
        </p>
        <form id="secret-providers-form" onSubmit={handleSaveSecretProviders}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '15px' }}>
            {/* Vault */}
            <div style={{ padding: '12px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <h4 style={{ margin: 0, fontSize: '13px' }}>
                  <i className="fa-solid fa-lock"></i> Vault (KV v2)
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    checked={secVaultEnabled}
                    onChange={(e) => setSecVaultEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <span className="badge badge-secondary" style={{ fontSize: '10px', marginBottom: '10px', display: 'inline-block' }}>
                HashiCorp Vault
              </span>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', marginTop: '8px' }}>
                <input
                  type="text"
                  placeholder="http://vault:8200"
                  value={secVaultAddress}
                  onChange={(e) => setSecVaultAddress(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />
                <input
                  type="password"
                  placeholder="Vault Token (optional)"
                  value={secVaultToken}
                  onChange={(e) => setSecVaultToken(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />
                <input
                  type="text"
                  placeholder="Mount Path (secret/data/)"
                  value={secVaultPath}
                  onChange={(e) => setSecVaultPath(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />
              </div>
            </div>

            {/* Registry */}
            <div style={{ padding: '12px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <h4 style={{ margin: 0, fontSize: '13px' }}>
                  <i className="fa-solid fa-database"></i> Win Registry
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    checked={secWinregEnabled}
                    onChange={(e) => setSecWinregEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <span className="badge badge-secondary" style={{ fontSize: '10px', marginBottom: '10px', display: 'inline-block' }}>
                DPAPI Encrypted
              </span>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', marginTop: '8px' }}>
                <input
                  type="text"
                  placeholder="HKCU\Software\McpRouter\Secrets"
                  value={secWinregKey}
                  onChange={(e) => setSecWinregKey(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />
              </div>
            </div>

            {/* Env */}
            <div style={{ padding: '12px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <h4 style={{ margin: 0, fontSize: '13px' }}>
                  <i className="fa-solid fa-terminal"></i> Environment
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    checked={secEnvEnabled}
                    onChange={(e) => setSecEnvEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <span className="badge badge-secondary" style={{ fontSize: '10px', marginBottom: '10px', display: 'inline-block' }}>
                Container Env
              </span>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', marginTop: '8px' }}>
                <input
                  type="text"
                  placeholder="Prefix (MCP_SECRET_)"
                  value={secEnvPrefix}
                  onChange={(e) => setSecEnvPrefix(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />
              </div>
            </div>
          </div>
          <div style={{ marginTop: '15px', textAlign: 'right' }}>
            <button type="submit" className="btn btn-primary btn-sm">
              <i className="fa-solid fa-floppy-disk"></i> Save Secret Config
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export const SettingsView: React.FC = () => {
  const [activeSubview, setActiveSubview] = useState<'search' | 'security' | 'identity' | 'secrets' | 'files' | 'permissions'>('search');

  const {
    embeddingSettings,
    authProviders,
    secretProviders,
    customFiles,
    policies,
    mappings,
    fetchEmbeddingSettings,
    saveEmbeddingSettings,
    fetchProviders,
    saveAuthProvider,
    saveSecretProvider,
    fetchCustomFiles,
    openCustomFileModal,
    deleteCustomFile,
    fetchPolicies,
    openPolicyModal,
    deletePolicy,
    fetchMappings,
    openMappingModal,
    deleteMapping,
  } = useSettingsStore();

  // Initial load
  useEffect(() => {
    fetchEmbeddingSettings();
    fetchProviders();
    fetchCustomFiles();
    fetchPolicies();
    fetchMappings();
  }, [fetchEmbeddingSettings, fetchProviders, fetchCustomFiles, fetchPolicies, fetchMappings]);

  const handleToggleApproval = async (checked: boolean) => {
    if (embeddingSettings) {
      await saveEmbeddingSettings({
        ...embeddingSettings,
        requireManualApproval: checked,
      });
    }
  };

  return (
    <div id="view-settings" className="view-panel active">
      <div
        className="tester-tabs settings-sub-nav"
        style={{
          justifyContent: 'flex-start',
          gap: '15px',
          marginBottom: '25px',
          borderBottom: '1px solid var(--border-color)',
          paddingBottom: '10px',
          maxWidth: '800px',
          marginLeft: 'auto',
          marginRight: 'auto',
        }}
      >
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'search' ? 'active' : ''}`}
          onClick={() => setActiveSubview('search')}
        >
          <i className="fa-solid fa-brain"></i> Vector &amp; Search
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'security' ? 'active' : ''}`}
          onClick={() => setActiveSubview('security')}
        >
          <i className="fa-solid fa-shield-halved"></i> Security &amp; Approvals
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'identity' ? 'active' : ''}`}
          onClick={() => setActiveSubview('identity')}
        >
          <i className="fa-solid fa-id-card"></i> Identity &amp; Auth
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'secrets' ? 'active' : ''}`}
          onClick={() => setActiveSubview('secrets')}
        >
          <i className="fa-solid fa-vault"></i> Secret Providers
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'files' ? 'active' : ''}`}
          onClick={() => setActiveSubview('files')}
        >
          <i className="fa-solid fa-folder-open"></i> Prompts &amp; Resources
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'permissions' ? 'active' : ''}`}
          onClick={() => setActiveSubview('permissions')}
        >
          <i className="fa-solid fa-user-lock"></i> Access Control
        </button>
      </div>

      {/* Subview 1: Vector & Search */}
      {activeSubview === 'search' && (
        <VectorSearchTab
          key={embeddingSettings ? `${embeddingSettings.embeddingProvider}-${embeddingSettings.embeddingModelDir}-${embeddingSettings.embeddingApiUrl}` : 'loading'}
          settings={embeddingSettings}
          saveEmbeddingSettings={saveEmbeddingSettings}
        />
      )}

      {/* Subview 2: Security & Approvals */}
      {activeSubview === 'security' && (
        <SecurityApprovalsTab
          requireApproval={embeddingSettings?.requireManualApproval ?? false}
          onToggleApproval={handleToggleApproval}
        />
      )}

      {/* Subview 3: Identity & Auth */}
      {activeSubview === 'identity' && (
        <IdentityAuthTab
          key={authProviders.map((p) => `${p.providerName}-${p.isEnabled}`).join(',')}
          providers={authProviders}
          saveAuthProvider={saveAuthProvider}
        />
      )}

      {/* Subview 4: Secret Providers */}
      {activeSubview === 'secrets' && (
        <SecretProvidersTab
          key={secretProviders.map((p) => `${p.providerName}-${p.isEnabled}`).join(',')}
          providers={secretProviders}
          saveSecretProvider={saveSecretProvider}
        />
      )}

      {/* Subview 5: Prompts & Resources File Manager */}
      {activeSubview === 'files' && (
        <div id="subview-files" className="settings-subview active">
          <div className="glass-card settings-card" style={{ maxWidth: '800px', margin: '0 auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
              <h2>
                <i className="fa-solid fa-folder-open"></i> Prompts &amp; Resources File Manager
              </h2>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => openCustomFileModal()}>
                <i className="fa-solid fa-plus"></i> Create File
              </button>
            </div>
            <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
              Manage your own local JSON prompts and markdown/text resources. They will be registered directly under the <code>router</code> namespace.
            </p>
            <div className="custom-files-table-container" style={{ overflowX: 'auto' }}>
              <table className="custom-files-table" style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '14px' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)' }}>
                    <th style={{ padding: '10px' }}>Type</th>
                    <th style={{ padding: '10px' }}>Name</th>
                    <th style={{ padding: '10px' }}>Size</th>
                    <th style={{ padding: '10px' }}>Modified</th>
                    <th style={{ padding: '10px', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {customFiles.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="empty-state" style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)' }}>
                        No custom files found.
                      </td>
                    </tr>
                  ) : (
                    customFiles.map((file) => {
                      const formattedSize = (file.sizeBytes / 1024).toFixed(2) + ' KB';
                      const dateStr = new Date(file.lastModified).toLocaleString();
                      const typeLabel =
                        file.type === 'prompts' ? (
                          <span style={{ color: '#f59e0b' }}>
                            <i className="fa-solid fa-comments"></i> Prompt
                          </span>
                        ) : (
                          <span style={{ color: '#10b981' }}>
                            <i className="fa-solid fa-file-lines"></i> Resource
                          </span>
                        );

                      return (
                        <tr key={file.name} style={{ borderBottom: '1px solid var(--border-color)' }}>
                          <td style={{ padding: '12px 10px' }}>{typeLabel}</td>
                          <td style={{ padding: '12px 10px', fontFamily: 'monospace', fontWeight: 500 }}>{file.name}</td>
                          <td style={{ padding: '12px 10px', color: 'var(--text-muted)' }}>{formattedSize}</td>
                          <td style={{ padding: '12px 10px', color: 'var(--text-muted)' }}>{dateStr}</td>
                          <td style={{ padding: '12px 10px', textAlign: 'right' }}>
                            <button
                              className="btn btn-secondary btn-sm"
                              onClick={() => openCustomFileModal(file)}
                              style={{ marginRight: '5px' }}
                            >
                              <i className="fa-solid fa-edit"></i> Edit
                            </button>
                            <button
                              className="btn btn-danger btn-sm"
                              onClick={() => deleteCustomFile(file.type, file.name)}
                            >
                              <i className="fa-solid fa-trash"></i> Delete
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Subview 6: Access Control Policies */}
      {activeSubview === 'permissions' && (
        <div id="subview-permissions" className="settings-subview active">
          {/* Policies Card */}
          <div className="glass-card settings-card" style={{ maxWidth: '900px', margin: '0 auto 25px auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
              <h2>
                <i className="fa-solid fa-shield-halved"></i> Access Control Policies
              </h2>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => openPolicyModal()}>
                <i className="fa-solid fa-plus"></i> Create Policy
              </button>
            </div>
            <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
              Define target-specific allow/deny rules for users based on their active directory or OIDC groups. TargetId format: <code>server:&lt;id&gt;</code>, <code>tool:&lt;name&gt;</code>, <code>prompt:&lt;name&gt;</code>, <code>resource:&lt;uri&gt;</code>.
            </p>
            <div className="table-container" style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '14px' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)' }}>
                    <th style={{ padding: '10px' }}>Target ID</th>
                    <th style={{ padding: '10px' }}>Required Group</th>
                    <th style={{ padding: '10px' }}>Access</th>
                    <th style={{ padding: '10px', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {policies.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="empty-state" style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)' }}>
                        No policies configured. All targets are default-allowed.
                      </td>
                    </tr>
                  ) : (
                    policies.map((p) => {
                      const badgeLabel = p.isAllowed ? 'ALLOW' : 'DENY';
                      const badgeStyle: React.CSSProperties = p.isAllowed
                        ? { background: 'rgba(16, 185, 129, 0.1)', color: '#10b981', border: '1px solid rgba(16, 185, 129, 0.2)', padding: '2px 6px', borderRadius: '4px', fontSize: '11px', fontWeight: '600' }
                        : { background: 'rgba(239, 68, 68, 0.1)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.2)', padding: '2px 6px', borderRadius: '4px', fontSize: '11px', fontWeight: '600' };

                      return (
                        <tr key={p.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                          <td style={{ padding: '12px 10px', fontFamily: 'monospace', fontWeight: 500 }}>{p.targetId}</td>
                          <td style={{ padding: '12px 10px' }}>{p.requiredGroup}</td>
                          <td style={{ padding: '12px 10px' }}>
                            <span style={badgeStyle}>{badgeLabel}</span>
                          </td>
                          <td style={{ padding: '12px 10px', textAlign: 'right' }}>
                            <button
                              className="btn btn-secondary btn-sm"
                              onClick={() => openPolicyModal(p)}
                              style={{ marginRight: '5px' }}
                            >
                              <i className="fa-solid fa-edit"></i> Edit
                            </button>
                            <button className="btn btn-danger btn-sm" onClick={() => deletePolicy(p.id!)}>
                              <i className="fa-solid fa-trash"></i> Delete
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Group/SID Mappings Card */}
          <div className="glass-card settings-card" style={{ maxWidth: '900px', margin: '0 auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
              <h2>
                <i className="fa-solid fa-user-group"></i> Group &amp; SID Mappings
              </h2>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => openMappingModal()}>
                <i className="fa-solid fa-plus"></i> Create Mapping
              </button>
            </div>
            <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
              Map external Active Directory SIDs or OIDC PocketID groups to internal virtual groups for easier access control.
            </p>
            <div className="table-container" style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '14px' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)' }}>
                    <th style={{ padding: '10px' }}>External Group / SID</th>
                    <th style={{ padding: '10px' }}>Internal Group Name</th>
                    <th style={{ padding: '10px', textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {mappings.length === 0 ? (
                    <tr>
                      <td colSpan={3} className="empty-state" style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)' }}>
                        No mappings configured.
                      </td>
                    </tr>
                  ) : (
                    mappings.map((m) => (
                      <tr key={m.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <td style={{ padding: '12px 10px', fontFamily: 'monospace', fontWeight: 500 }}>{m.externalId}</td>
                        <td style={{ padding: '12px 10px' }}>{m.internalGroup}</td>
                        <td style={{ padding: '12px 10px', textAlign: 'right' }}>
                          <button
                            className="btn btn-secondary btn-sm"
                            onClick={() => openMappingModal(m)}
                            style={{ marginRight: '5px' }}
                          >
                            <i className="fa-solid fa-edit"></i> Edit
                          </button>
                          <button className="btn btn-danger btn-sm" onClick={() => deleteMapping(m.id!)}>
                            <i className="fa-solid fa-trash"></i> Delete
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
