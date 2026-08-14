import React from 'react';
import { useServerStore } from '../../stores/useServerStore';

export const StatsCard: React.FC = () => {
  const { servers } = useServerStore();

  const total = servers.length;
  const connected = servers.filter((s) => s.enabled && s.connectionStatus === 'Connected').length;
  const failed = servers.filter((s) => s.enabled && s.connectionStatus === 'Failed').length;
  const disabled = servers.filter((s) => !s.enabled).length;

  return (
    <div className="stats-grid">
      <div className="stat-card">
        <span className="stat-label">Total Servers</span>
        <span className="stat-value" id="stat-total-servers">{total}</span>
      </div>
      <div className="stat-card">
        <span className="stat-label">Connected</span>
        <span className="stat-value text-success" id="stat-connected-servers">{connected}</span>
      </div>
      <div className="stat-card">
        <span className="stat-label">Failed / Error</span>
        <span className="stat-value text-danger" id="stat-failed-servers">{failed}</span>
      </div>
      <div className="stat-card">
        <span className="stat-label">Disabled</span>
        <span className="stat-value text-muted" id="stat-disabled-servers">{disabled}</span>
      </div>
    </div>
  );
};
