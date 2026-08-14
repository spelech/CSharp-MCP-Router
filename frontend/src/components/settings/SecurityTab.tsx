import React from 'react';

export interface SecurityTabProps {
  requireApproval: boolean;
  onToggleApproval: (checked: boolean) => void;
}

export const SecurityTab: React.FC<SecurityTabProps> = ({ requireApproval, onToggleApproval }) => {
  return (
    <div id="subview-security" className="settings-subview active">
      <div className="glass-card settings-card" style={{ maxWidth: '600px', margin: '0 auto' }}>
        <h2>
          <i className="fa-solid fa-shield-halved"></i> Security &amp; Safety Controls
        </h2>
        <p className="description" style={{ color: 'var(--text-muted)', fontSize: '13px', marginBottom: '20px' }}>
          Enforce human-in-the-loop validation for executing destructive or sensitive tools.
        </p>
        <div className="form-group">
          <div className="checkbox-group" style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <label className="switch">
              <input
                type="checkbox"
                id="settings-require-approval"
                checked={requireApproval}
                onChange={(e) => onToggleApproval(e.target.checked)}
              />
              <span className="slider"></span>
            </label>
            <span className="checkbox-label" style={{ fontWeight: 500 }}>
              Require Manual Approval for Dangerous Tools
            </span>
          </div>
          <small style={{ color: 'var(--text-muted)', display: 'block', marginTop: '5px', marginLeft: '50px' }}>
            When enabled, tool execution requests targeting databases, docker containers, files, or smart devices will prompt the user in the UI before running.
          </small>
        </div>
      </div>
    </div>
  );
};
