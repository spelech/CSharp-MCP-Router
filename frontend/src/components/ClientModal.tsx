import React, { useState } from 'react';
import { useClientStore } from '../stores/useClientStore';

export const ClientModal: React.FC = () => {
  const { isAddClientOpen, createdClientResult, registerClient, closeClientModal } = useClientStore();

  const [clientName, setClientName] = useState('');
  const [clientScopes, setClientScopes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isAddClientOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    const scopes = clientScopes
      ? clientScopes.split(',').map((s) => s.trim()).filter((s) => s.length > 0)
      : [];

    try {
      await registerClient(clientName, scopes);
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div id="add-client-modal" className="modal-backdrop" style={{ display: 'flex' }}>
      <div className="glass-card modal-card">
        <div className="modal-header">
          <h2>Register New Client</h2>
          <button className="btn-close" onClick={closeClientModal}>
            &times;
          </button>
        </div>

        {!createdClientResult ? (
          <form id="client-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="client-name">Client Name (Display Name)</label>
              <input
                type="text"
                id="client-name"
                placeholder="e.g. VSCode Extension"
                value={clientName}
                onChange={(e) => setClientName(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="client-scopes">Roles / Scopes (Comma-separated)</label>
              <input
                type="text"
                id="client-scopes"
                placeholder="e.g. admin, ha_read"
                value={clientScopes}
                onChange={(e) => setClientScopes(e.target.value)}
              />
              <small>Leave blank for default 'mcp_client' scope.</small>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeClientModal}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" id="btn-save-client" disabled={isSubmitting}>
                {isSubmitting ? 'Generating...' : 'Generate Client'}
              </button>
            </div>
          </form>
        ) : (
          <div
            id="client-secret-result"
            style={{
              padding: '10px',
              background: 'rgba(16, 185, 129, 0.1)',
              border: '1px solid var(--accent)',
              borderRadius: '8px',
            }}
          >
            <h4>Client Created Successfully!</h4>
            <p style={{ margin: '8px 0' }}>
              <strong>Client ID:</strong>{' '}
              <span className="code" style={{ wordBreak: 'break-all' }}>
                {createdClientResult.clientId}
              </span>
            </p>
            <p style={{ margin: '8px 0' }}>
              <strong>Client Secret:</strong>{' '}
              <span className="code" style={{ wordBreak: 'break-all' }}>
                {createdClientResult.clientSecret}
              </span>
            </p>
            <p style={{ color: 'var(--secondary)', fontSize: '13px', display: 'flex', alignItems: 'center', gap: '5px', margin: '12px 0 0 0' }}>
              <i className="fa-solid fa-triangle-exclamation"></i> Save this secret now. It will not be shown again.
            </p>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={closeClientModal}
              style={{ marginTop: '15px', width: '100%' }}
            >
              Close
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
