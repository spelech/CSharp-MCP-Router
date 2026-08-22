import React, { useEffect } from 'react';
import { useAppKeyStore } from '../../stores/useAppKeyStore';
import { showToast } from '../../stores/useToastStore';

export const AppKeysCard: React.FC = () => {
  const { appKeys, limits, fetchAppKeys, fetchLimits, revokeAppKey, openModal } = useAppKeyStore();

  useEffect(() => {
    fetchAppKeys();
    fetchLimits();
  }, [fetchAppKeys, fetchLimits]);

  const copyConfigSnippet = (keyPrefix: string) => {
    const sampleKey = `${keyPrefix}...[YOUR_FULL_KEY]`;
    const snippet = JSON.stringify({
      mcpServers: {
        "mcp-router": {
          url: "http://10.0.0.10:8026/sse",
          type: "sse",
          trust: true,
          headers: {
            "X-App-Key": sampleKey
          }
        }
      }
    }, null, 2);
    navigator.clipboard.writeText(snippet);
    showToast('Copied sample mcp_config.json snippet to clipboard!', 'success');
  };

  return (
    <div className="glass-card dcr-card">
      <div className="card-header-btn">
        <div>
          <h2>
            <i className="fa-solid fa-key"></i> App Keys
          </h2>
          {limits && (
            <small style={{ color: 'var(--secondary)', display: 'block', marginTop: '2px' }}>
              {limits.userMax > 0 ? (
                <>User Quota: <strong>{limits.userActiveKeys} / {limits.userMax}</strong> Keys Used &bull; Global: {limits.globalMax > 0 ? `${limits.totalActiveKeys} / ${limits.globalMax}` : 'Unlimited'}</>
              ) : (
                <>Active Keys: <strong>{limits.userActiveKeys}</strong> &bull; Quota: Unlimited</>
              )}
            </small>
          )}
        </div>
        <button className="btn btn-primary btn-sm" onClick={openModal} disabled={!!limits?.isLimitReached}>
          <i className="fa-solid fa-plus"></i> Create App Key
        </button>
      </div>

      <div className="table-container">
        <table id="appkeys-table">
          <thead>
            <tr>
              <th>Key Name</th>
              <th>Prefix</th>
              <th>Owner</th>
              <th>Scopes</th>
              <th>Expires</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {appKeys.length === 0 ? (
              <tr>
                <td colSpan={7} className="empty-state">
                  No App Keys active. Click "+ Create App Key" to generate a credential for CLI or IDE tools.
                </td>
              </tr>
            ) : (
              appKeys.map((key) => (
                <tr key={key.id}>
                  <td><strong>{key.name}</strong></td>
                  <td>
                    <code style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: '11px', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px', color: 'var(--accent)' }}>
                      {key.keyPrefix}...
                    </code>
                  </td>
                  <td>{key.username}</td>
                  <td>
                    {key.scopes && key.scopes.length > 0 ? (
                      key.scopes.map((s, idx) => (
                        <span key={idx} className="server-badge" style={{ background: 'rgba(249, 115, 22, 0.1)', color: 'var(--accent)', marginRight: '4px' }}>
                          {s}
                        </span>
                      ))
                    ) : (
                      <span className="server-badge">all</span>
                    )}
                  </td>
                  <td>
                    {key.expiresAt ? (
                      new Date(key.expiresAt) < new Date() ? (
                        <span style={{ color: '#ef4444', fontWeight: 600 }}>Expired</span>
                      ) : (
                        <span>{new Date(key.expiresAt).toLocaleDateString()}</span>
                      )
                    ) : (
                      <span style={{ color: 'var(--secondary)' }}>Never</span>
                    )}
                  </td>
                  <td>{new Date(key.createdAt).toLocaleDateString()}</td>
                  <td>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => copyConfigSnippet(key.keyPrefix)}
                      title="Copy MCP Config Snippet"
                      style={{ marginRight: '6px' }}
                    >
                      <i className="fa-solid fa-code"></i> Config
                    </button>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => revokeAppKey(key.id, key.name)}
                      title="Revoke Key"
                    >
                      <i className="fa-solid fa-trash"></i> Revoke
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
