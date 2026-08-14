import React from 'react';
import { useSettingsStore } from '../../stores/useSettingsStore';

export const ApprovalsCard: React.FC = () => {
  const { pendingApprovals, actionApproval } = useSettingsStore();

  if (pendingApprovals.length === 0) return null;

  return (
    <div className="glass-card approvals-card" style={{ borderColor: 'var(--secondary)' }}>
      <h2>
        <i className="fa-solid fa-triangle-exclamation" style={{ color: 'var(--secondary)' }}></i> Pending Tool Approvals
      </h2>
      <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '15px' }}>
        The following tools require confirmation before execution.
      </p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
        {pendingApprovals.map((app) => (
          <div
            key={app.id}
            className="approval-item"
            style={{
              background: 'rgba(255,255,255,0.05)',
              padding: '12px',
              borderRadius: '8px',
              border: '1px solid var(--border-color)',
              borderLeft: '4px solid var(--secondary)',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '13px' }}>
              <strong style={{ color: 'var(--accent)' }}>{app.toolName}</strong>
              <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{app.sessionId}</span>
            </div>
            <pre style={{ fontSize: '11px', maxHeight: '100px', overflow: 'auto', background: 'rgba(0,0,0,0.3)', padding: '6px', borderRadius: '4px', color: '#fff', margin: 0 }}>
              {app.arguments}
            </pre>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '10px' }}>
              <button className="btn btn-secondary btn-sm" onClick={() => actionApproval(app.id, false)}>
                Deny
              </button>
              <button className="btn btn-primary btn-sm" onClick={() => actionApproval(app.id, true)}>
                Approve
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
