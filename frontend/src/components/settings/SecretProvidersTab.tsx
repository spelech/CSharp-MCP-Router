import React, { useState } from 'react';
import { SecretProviderConfig } from '../../shared/types';
import { testVaultConnectionApi } from '../../api/settingsApi';

export interface SecretProvidersTabProps {
  providers: SecretProviderConfig[];
  saveSecretProvider: (provider: SecretProviderConfig) => Promise<void>;
}

export const SecretProvidersTab: React.FC<SecretProvidersTabProps> = ({ providers, saveSecretProvider }) => {
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
  const [secVaultAuthMethod, setSecVaultAuthMethod] = useState<'token' | 'approle'>(
    parsedVault.roleId || parsedVault.role_id ? 'approle' : 'token'
  );
  const [secVaultToken, setSecVaultToken] = useState(parsedVault.token || '');
  const [secVaultRoleId, setSecVaultRoleId] = useState(parsedVault.roleId || parsedVault.role_id || '');
  const [secVaultSecretId, setSecVaultSecretId] = useState(parsedVault.secretId || parsedVault.secret_id || '');
  const [secVaultPath, setSecVaultPath] = useState(parsedVault.mountPath || '');

  const [vaultTestStatus, setVaultTestStatus] = useState<{
    type: 'idle' | 'testing' | 'success' | 'error';
    message: string;
  }>({ type: 'idle', message: '' });

  const [secWinregEnabled, setSecWinregEnabled] = useState(winreg ? winreg.isEnabled : false);
  const [secWinregKey, setSecWinregKey] = useState(parsedWinreg.keyPath || '');

  const [secEnvEnabled, setSecEnvEnabled] = useState(env ? env.isEnabled : false);
  const [secEnvPrefix, setSecEnvPrefix] = useState(parsedEnv.prefix || '');

  const handleTestVault = async () => {
    setVaultTestStatus({ type: 'testing', message: 'Testing Vault connection...' });
    try {
      const res = await testVaultConnectionApi({
        address: secVaultAddress,
        authMethod: secVaultAuthMethod,
        token: secVaultToken,
        roleId: secVaultRoleId,
        secretId: secVaultSecretId,
        mountPath: secVaultPath,
      });

      if (res.success) {
        setVaultTestStatus({ type: 'success', message: res.message || 'Vault connected successfully!' });
      } else {
        setVaultTestStatus({ type: 'error', message: res.error || 'Vault connection failed.' });
      }
    } catch (err: any) {
      setVaultTestStatus({ type: 'error', message: err?.message || 'Error executing Vault test.' });
    }
  };

  const handleSaveSecretProviders = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const vaultConfig: Record<string, any> = {
        address: secVaultAddress,
        token: secVaultToken,
        mountPath: secVaultPath,
      };

      if (secVaultAuthMethod === 'approle') {
        delete vaultConfig.token;
        vaultConfig.roleId = secVaultRoleId;
        vaultConfig.secretId = secVaultSecretId;
        vaultConfig.authMethod = 'approle';
      }

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
      <div className="glass-card settings-card" style={{ maxWidth: '850px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-vault"></i> Secret Providers
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Configure external vault and registry secret providers for resolving downstream MCP tokens.
        </p>
        <form id="secret-providers-form" onSubmit={handleSaveSecretProviders}>
          <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.9fr 0.9fr', gap: '15px' }}>
            {/* Vault */}
            <div style={{ padding: '12px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                <h4 style={{ margin: 0, fontSize: '13px' }}>
                  <i className="fa-solid fa-lock"></i> Vault (KV v2)
                </h4>
                <label className="switch">
                  <input
                    type="checkbox"
                    id="sec-vault-enabled"
                    checked={secVaultEnabled}
                    onChange={(e) => setSecVaultEnabled(e.target.checked)}
                  />
                  <span className="slider"></span>
                </label>
              </div>
              <span className="badge badge-secondary" style={{ fontSize: '10px', marginBottom: '10px', display: 'inline-block' }}>
                HashiCorp Vault
              </span>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', marginTop: '6px' }}>
                <input
                  type="text"
                  placeholder="http://vault:8200"
                  value={secVaultAddress}
                  onChange={(e) => setSecVaultAddress(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />

                <div style={{ display: 'flex', gap: '10px', marginTop: '2px', marginBottom: '2px' }}>
                  <label style={{ fontSize: '10px', display: 'flex', alignItems: 'center', gap: '4px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="vault-auth-method"
                      value="token"
                      checked={secVaultAuthMethod === 'token'}
                      onChange={() => setSecVaultAuthMethod('token')}
                    />
                    Token Auth
                  </label>
                  <label style={{ fontSize: '10px', display: 'flex', alignItems: 'center', gap: '4px', cursor: 'pointer' }}>
                    <input
                      type="radio"
                      name="vault-auth-method"
                      value="approle"
                      checked={secVaultAuthMethod === 'approle'}
                      onChange={() => setSecVaultAuthMethod('approle')}
                    />
                    AppRole Auth
                  </label>
                </div>

                {secVaultAuthMethod === 'token' ? (
                  <input
                    type="password"
                    placeholder="Vault Token (optional)"
                    value={secVaultToken}
                    onChange={(e) => setSecVaultToken(e.target.value)}
                    style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                  />
                ) : (
                  <>
                    <input
                      type="text"
                      placeholder="Role ID"
                      value={secVaultRoleId}
                      onChange={(e) => setSecVaultRoleId(e.target.value)}
                      style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                    />
                    <input
                      type="password"
                      placeholder="Secret ID"
                      value={secVaultSecretId}
                      onChange={(e) => setSecVaultSecretId(e.target.value)}
                      style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                    />
                  </>
                )}

                <input
                  type="text"
                  placeholder="Mount Path (secret/data/)"
                  value={secVaultPath}
                  onChange={(e) => setSecVaultPath(e.target.value)}
                  style={{ fontSize: '11px', padding: '4px 8px', width: '100%', border: '1px solid var(--border-color)', background: 'var(--bg-dark)', color: 'var(--text-main)' }}
                />

                <div style={{ marginTop: '6px', display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <button
                    type="button"
                    id="btn-test-vault"
                    className="btn btn-secondary btn-sm"
                    onClick={handleTestVault}
                    disabled={vaultTestStatus.type === 'testing'}
                    style={{ fontSize: '10px', padding: '2px 6px' }}
                  >
                    <i className="fa-solid fa-plug"></i> {vaultTestStatus.type === 'testing' ? 'Testing...' : 'Test Vault'}
                  </button>
                  {vaultTestStatus.type !== 'idle' && (
                    <span
                      id="vault-test-feedback"
                      style={{
                        fontSize: '10px',
                        color: vaultTestStatus.type === 'success' ? '#10b981' : vaultTestStatus.type === 'error' ? '#ef4444' : 'var(--text-muted)'
                      }}
                    >
                      {vaultTestStatus.message}
                    </span>
                  )}
                </div>
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
                    id="sec-winreg-enabled"
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
                    id="sec-env-enabled"
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
            <button type="submit" id="btn-save-secrets" className="btn btn-primary btn-sm">
              <i className="fa-solid fa-floppy-disk"></i> Save Secret Config
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
