import React from 'react';

export const BackupsTab: React.FC = () => {
  return (
    <div id="subview-backups" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '800px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-database"></i> Database &amp; System Maintenance
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Manage gateway configuration snapshots, export system state, and inspect persistence storage metrics.
        </p>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
          <div style={{ padding: '15px', background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border-color)', borderRadius: '8px' }}>
            <h4 style={{ margin: '0 0 8px 0' }}><i className="fa-solid fa-file-export"></i> Configuration Export</h4>
            <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: 0 }}>
              Export server configurations, registered OAuth clients, and group policies as a consolidated JSON backup.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
