import React from 'react';
import { useServerStore } from '../stores/useServerStore';

export const ServerInspectModal: React.FC = () => {
  const {
    isInspectOpen,
    inspectServer,
    inspectData,
    inspectLoading,
    inspectActiveTab,
    inspectSearchQuery,
    setInspectActiveTab,
    setInspectSearchQuery,
    closeInspectModal,
  } = useServerStore();

  if (!isInspectOpen || !inspectServer) return null;

  const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInspectSearchQuery(e.target.value);
  };

  const q = inspectSearchQuery.toLowerCase().trim();

  // Filter tools, resources, and prompts
  const filteredTools = (inspectData.tools || []).filter((t: any) => {
    if (!q) return true;
    return (t.name || '').toLowerCase().includes(q) || (t.description || '').toLowerCase().includes(q);
  });

  const filteredResources = (inspectData.resources || []).filter((r: any) => {
    if (!q) return true;
    const uri = (r.uri || r.uriTemplate || '').toLowerCase();
    const name = (r.name || '').toLowerCase();
    const desc = (r.description || '').toLowerCase();
    return uri.includes(q) || name.includes(q) || desc.includes(q);
  });

  const filteredPrompts = (inspectData.prompts || []).filter((p: any) => {
    if (!q) return true;
    return (p.name || '').toLowerCase().includes(q) || (p.description || '').toLowerCase().includes(q);
  });

  return (
    <div className="modal-backdrop" id="server-inspect-modal" style={{ display: 'flex' }}>
      <div className="modal-content" style={{ maxWidth: '900px', width: '90%', background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '12px' }}>
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <i className="fa-solid fa-cubes logo-icon" style={{ fontSize: '20px', color: 'var(--accent)' }}></i>
            <div>
              <h3 id="inspect-modal-title" style={{ margin: 0, fontSize: '18px', color: 'var(--text-main)' }}>
                Inspect Capabilities: {inspectServer.displayName}
              </h3>
              <span id="inspect-modal-subtitle" style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                Server ID: {inspectServer.id} • Type: {(inspectServer.type || 'SSE').toUpperCase()}
              </span>
            </div>
          </div>
          <button type="button" className="btn-icon" onClick={closeInspectModal} style={{ background: 'none', border: 'none', color: 'var(--text-main)', fontSize: '20px', cursor: 'pointer' }}>
            <i className="fa-solid fa-xmark"></i>
          </button>
        </div>

        <div style={{ padding: '15px 20px 0 20px', borderBottom: '1px solid var(--border-color)', display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'center', gap: '10px' }}>
          <div className="tester-tabs" id="inspect-tabs-bar" style={{ margin: 0 }}>
            <button
              type="button"
              className={`tester-tab-btn ${inspectActiveTab === 'tools' ? 'active' : ''}`}
              onClick={() => setInspectActiveTab('tools')}
            >
              <i className="fa-solid fa-wrench"></i> Tools <span className="server-badge" id="inspect-tools-count">{(inspectData.tools || []).length}</span>
            </button>
            <button
              type="button"
              className={`tester-tab-btn ${inspectActiveTab === 'resources' ? 'active' : ''}`}
              onClick={() => setInspectActiveTab('resources')}
            >
              <i className="fa-solid fa-file-invoice"></i> Resources <span className="server-badge" id="inspect-resources-count">{(inspectData.resources || []).length}</span>
            </button>
            <button
              type="button"
              className={`tester-tab-btn ${inspectActiveTab === 'prompts' ? 'active' : ''}`}
              onClick={() => setInspectActiveTab('prompts')}
            >
              <i className="fa-solid fa-comments"></i> Prompts <span className="server-badge" id="inspect-prompts-count">{(inspectData.prompts || []).length}</span>
            </button>
          </div>

          <div className="search-sort-bar" style={{ margin: 0 }}>
            <div className="search-input-wrapper" style={{ position: 'relative' }}>
              <i className="fa-solid fa-magnifying-glass search-icon" style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}></i>
              <input
                type="text"
                id="inspect-search-input"
                placeholder="Search capabilities..."
                value={inspectSearchQuery}
                onChange={handleSearch}
                style={{ paddingLeft: '30px', height: '32px', borderRadius: '6px', border: '1px solid var(--border-color)', background: 'rgba(0,0,0,0.2)', color: 'var(--text-main)', fontSize: '13px' }}
              />
            </div>
          </div>
        </div>

        <div style={{ padding: '20px', maxHeight: '500px', overflowY: 'auto' }}>
          {inspectLoading ? (
            <div className="empty-state">
              <i className="fa-solid fa-spinner fa-spin"></i> Fetching capabilities from server...
            </div>
          ) : (
            <>
              {/* Tools Panel */}
              {inspectActiveTab === 'tools' && (
                <div id="inspect-panel-tools" className="inspect-panel">
                  {filteredTools.length === 0 ? (
                    <div className="empty-state">
                      {inspectData.tools?.length === 0 ? 'No tools exposed by this server.' : `No tools matching "${inspectSearchQuery}".`}
                    </div>
                  ) : (
                    <div id="inspect-tools-list" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                      {filteredTools.map((t: any) => (
                        <div key={t.name} className="glass-card" style={{ padding: '12px 16px', margin: 0 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                            <span style={{ fontWeight: 700, fontFamily: 'monospace', color: 'var(--accent)' }}>
                              <i className="fa-solid fa-wrench"></i> {t.name}
                            </span>
                          </div>
                          <div style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '8px' }}>
                            {t.description || 'No description available.'}
                          </div>
                          {t.inputSchema && t.inputSchema.properties && (
                            <div style={{ fontSize: '11px', background: 'rgba(0,0,0,0.3)', padding: '8px 12px', borderRadius: '4px', fontFamily: 'monospace' }}>
                              <strong style={{ color: '#93c5fd' }}>Parameters:</strong> {Object.keys(t.inputSchema.properties).join(', ')}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Resources Panel */}
              {inspectActiveTab === 'resources' && (
                <div id="inspect-panel-resources" className="inspect-panel">
                  {filteredResources.length === 0 ? (
                    <div className="empty-state">
                      {inspectData.resources?.length === 0 ? 'No resources exposed by this server.' : `No resources matching "${inspectSearchQuery}".`}
                    </div>
                  ) : (
                    <div id="inspect-resources-list" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                      {filteredResources.map((r: any) => (
                        <div key={r.uri || r.uriTemplate || r.name} className="glass-card" style={{ padding: '12px 16px', margin: 0 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                            <span style={{ fontWeight: 700, fontFamily: 'monospace', color: 'var(--primary)' }}>
                              <i className="fa-solid fa-file-invoice"></i> {r.uri || r.uriTemplate || r.name}
                            </span>
                            {r.mimeType && <span className="server-badge">{r.mimeType}</span>}
                          </div>
                          <div style={{ fontSize: '13px', color: 'var(--text-muted)' }}>
                            {r.description || 'No description available.'}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Prompts Panel */}
              {inspectActiveTab === 'prompts' && (
                <div id="inspect-panel-prompts" className="inspect-panel">
                  {filteredPrompts.length === 0 ? (
                    <div className="empty-state">
                      {inspectData.prompts?.length === 0 ? 'No prompts exposed by this server.' : `No prompts matching "${inspectSearchQuery}".`}
                    </div>
                  ) : (
                    <div id="inspect-prompts-list" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                      {filteredPrompts.map((p: any) => (
                        <div key={p.name} className="glass-card" style={{ padding: '12px 16px', margin: 0 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                            <span style={{ fontWeight: 700, fontFamily: 'monospace', color: '#a855f7' }}>
                              <i className="fa-solid fa-comments"></i> {p.name}
                            </span>
                          </div>
                          <div style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '8px' }}>
                            {p.description || 'No description available.'}
                          </div>
                          {p.arguments && p.arguments.length > 0 && (
                            <div style={{ fontSize: '11px', background: 'rgba(0,0,0,0.3)', padding: '8px 12px', borderRadius: '4px', fontFamily: 'monospace' }}>
                              <strong style={{ color: '#c084fc' }}>Arguments:</strong> {p.arguments.map((a: any) => a.name + (a.required ? '*' : '')).join(', ')}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </>
          )}
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-secondary" onClick={closeInspectModal}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
