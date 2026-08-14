import React, { useState } from 'react';
import { SecretProviderConfig } from '../../shared/types';

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
