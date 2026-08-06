import React from 'react';
import { useServerStore } from '../../stores/useServerStore';

export const StatsCard: React.FC = () => {
  const { servers } = useServerStore();
  const totalServers = servers.length;
  const activeServers = servers.filter((s) => s.enabled).length;

  return (
    <div className="glass-card stats-card">
      <h2>
        <i className="fa-solid fa-chart-simple"></i> System Stats
      </h2>
      <div className="stats-grid">
        <div className="stat-box">
          <span className="stat-number">{totalServers}</span>
          <span className="stat-label">Total Servers</span>
        </div>
        <div className="stat-box">
          <span className="stat-number">{activeServers}</span>
          <span className="stat-label">Active Backends</span>
        </div>
      </div>
    </div>
  );
};
