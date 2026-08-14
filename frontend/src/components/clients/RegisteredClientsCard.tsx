import React, { useEffect } from 'react';
import { useClientStore } from '../../stores/useClientStore';

export const RegisteredClientsCard: React.FC = () => {
  const { clients, fetchClients, deleteClient, openAddClientModal } = useClientStore();

  useEffect(() => {
    fetchClients();
  }, [fetchClients]);

  return (
    <div className="glass-card dcr-card">
      <div className="card-header-btn">
        <h2>
          <i className="fa-solid fa-desktop"></i> Dynamic Client Registration (RFC 7591)
        </h2>
        <button className="btn btn-primary btn-sm" id="btn-add-client" onClick={openAddClientModal}>
          <i className="fa-solid fa-plus"></i> Register Client
        </button>
      </div>

      <div className="table-container">
        <table id="clients-table">
          <thead>
            <tr>
              <th>Client Name</th>
              <th>Client ID</th>
              <th>Type</th>
              <th>Scopes</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {clients.length === 0 ? (
              <tr>
                <td colSpan={5} className="empty-state">
                  No registered clients found.
                </td>
              </tr>
            ) : (
              clients.map((c) => (
                <tr key={c.id}>
                  <td>{c.displayName}</td>
                  <td>
                    <span className="code">{c.clientId}</span>
                  </td>
                  <td>
                    <span className="server-badge">{c.isDynamic ? 'Dynamic' : 'Static'}</span>
                  </td>
                  <td>
                    <span className="server-badge">{c.scopes ? c.scopes.join(', ') : 'mcp_client'}</span>
                  </td>
                  <td>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => deleteClient(c.id, c.displayName)}
                    >
                      Delete
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
