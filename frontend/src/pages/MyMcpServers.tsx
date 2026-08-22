import React, { useEffect, useState } from 'react';
import { McpServer } from '../shared/types';
import { fetchServersApi } from '../api/serverApi';
import { fetchUserCredentialsApi, saveUserCredentialApi, UserCredential } from '../api/userCredentialsApi';
import { showToast } from '../stores/useToastStore';

export const MyMcpServers: React.FC = () => {
  const [servers, setServers] = useState<McpServer[]>([]);
  const [credentials, setCredentials] = useState<UserCredential[]>([]);
  const [loading, setLoading] = useState(true);
  
  const [editingServer, setEditingServer] = useState<McpServer | null>(null);
  const [secretJson, setSecretJson] = useState('');

  const loadData = async () => {
    setLoading(true);
    try {
      const [serversData, credsData] = await Promise.all([
        fetchServersApi(),
        fetchUserCredentialsApi()
      ]);
      setServers(serversData.filter(s => s.secretProvider === 'UserProvided'));
      setCredentials(credsData);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadData();
  }, []);

  const handleEdit = (server: McpServer) => {
    setEditingServer(server);
    setSecretJson('{\n  "apiKey": ""\n}'); // default template
  };

  const handleSave = async () => {
    if (!editingServer) return;
    try {
      // Validate JSON
      JSON.parse(secretJson);
      
      await saveUserCredentialApi(editingServer.id, secretJson);
      setEditingServer(null);
      await loadData();
    } catch {
      showToast('Invalid JSON or failed to save.', 'error');
    }
  };

  if (loading) {
    return <div id="view-my-mcp-servers" className="view-panel active"><div className="glass-card dcr-card">Loading...</div></div>;
  }

  return (
    <div id="view-my-mcp-servers" className="view-panel active">
      <div className="glass-card dcr-card">
        <div className="card-header-btn"><h2><i className="fa-solid fa-server"></i> My MCP Servers (User Provided Auth)</h2></div>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {servers.length === 0 && (
                <tr><td colSpan={3}>No user-provided servers available.</td></tr>
              )}
              {servers.map(server => {
                const isConfigured = credentials.some(c => (typeof c === 'string' ? c === server.id : c.serverId === server.id));
                return (
                  <tr key={server.id}>
                    <td>{server.displayName || server.id}</td>
                    <td>
                      {isConfigured ? (
                        <span style={{ color: 'var(--success-color)' }}>Auth Configured</span>
                      ) : (
                        <span style={{ color: 'var(--danger-color)' }}>Auth Missing</span>
                      )}
                    </td>
                    <td>
                      <button className="btn-icon" onClick={() => handleEdit(server)}>
                        <i className="fa-solid fa-pen"></i> Edit Auth
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {editingServer && (
        <div className="modal-backdrop" style={{ display: 'flex' }} onClick={() => setEditingServer(null)}>
          <div className="glass-card modal-card" style={{ maxWidth: '540px' }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-header"><h2><i className="fa-solid fa-lock"></i> Edit Auth for {editingServer.displayName}</h2><button className="btn-close" onClick={() => setEditingServer(null)}>&times;</button></div>
            <div className="form-group">
              <label>Credentials (JSON format)</label>
              <textarea 
                className="form-control" 
                style={{ height: '150px', fontFamily: 'monospace' }}
                value={secretJson}
                onChange={e => setSecretJson(e.target.value)}
              />
            </div>
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setEditingServer(null)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleSave}>Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
