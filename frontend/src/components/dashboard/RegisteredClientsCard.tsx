import React from 'react';
import { useClientStore } from '../../stores/useClientStore';

export const RegisteredClientsCard: React.FC = () => {
  const { clients, deleteClient, openAddClientModal } = useClientStore();

  return (
    <div className="glass-card dcr-card">
      <div className="card-header-btn">
        <h2>
          <i className="fa-solid fa-key"></i> Registered Clients
        </h2>
        <button className="btn btn-secondary btn-sm" onClick={openAddClientModal}>
          <i className="fa-solid fa-plus"></i> New Client
        </button>
      </div>
      <div className="table-container">
        <table id="clients-table">
          <thead>
            <tr>
              <th>Client Name</th>
              <th>Client ID</th>
              <th>Type</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {clients.length === 0 ? (
              <tr>
                <td colSpan={4} className="empty-state">
                  No clients registered yet. Add a manual connection in Gemini to register.
                </td>
              </tr>
            ) : (
              clients.map((client) => (
                <tr key={client.id}>
                  <td>
                    <strong>{client.displayName}</strong>
                  </td>
                  <td>
                    <code style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: '11px', background: 'rgba(255,255,255,0.05)', padding: '2px 6px', borderRadius: '4px', color: 'var(--accent)' }}>
                      {client.clientId}
                    </code>
                  </td>
                  <td>
                    {client.isDynamic ? (
                      <span className="server-badge" style={{ background: 'rgba(16, 185, 129, 0.1)', color: 'var(--accent)' }}>
                        Dynamic
                      </span>
                    ) : (
                      <span className="server-badge">Manual</span>
                    )}
                  </td>
                  <td>
                    <button
                      className="btn-icon btn-delete"
                      title="Delete Client"
                      onClick={() => deleteClient(client.id, client.displayName)}
                    >
                      <i className="fa-solid fa-trash-can"></i>
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
