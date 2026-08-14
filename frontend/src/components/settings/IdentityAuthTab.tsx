import React, { useState } from 'react';
import { AuthProviderConfig } from '../../shared/types';

export interface IdentityAuthTabProps {
  providers: AuthProviderConfig[];
  saveAuthProvider: (provider: AuthProviderConfig) => Promise<void>;
}

export const IdentityAuthTab: React.FC<IdentityAuthTabProps> = ({ providers, saveAuthProvider }) => {
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
