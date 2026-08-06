import React, { useEffect } from 'react';
import { useServerStore, McpServer } from '../stores/useServerStore';
import { useClientStore } from '../stores/useClientStore';
import { useSettingsStore } from '../stores/useSettingsStore';

import { StatsCard } from './dashboard/StatsCard';
import { ApprovalsCard } from './dashboard/ApprovalsCard';
import { RegisteredClientsCard } from './dashboard/RegisteredClientsCard';
import { ClientSetupGuide } from './dashboard/ClientSetupGuide';
import { ServerControlsToolbar } from './dashboard/ServerControlsToolbar';
import { ServerCard } from './dashboard/ServerCard';
import { PaginationToolbar } from './dashboard/PaginationToolbar';

export const DashboardView: React.FC = () => {
  const {
    servers,
    searchQuery,
    sortBy,
    groupBy,
    currentPage,
    pageSize,
    collapsedGroups,
    fetchServers,
    toggleGroupCollapse,
  } = useServerStore();

  const { fetchClients } = useClientStore();
  const { fetchApprovals } = useSettingsStore();

  useEffect(() => {
    fetchServers();
    fetchClients();
    fetchApprovals();

    const serverPoll = setInterval(() => fetchServers(), 10000);
    const approvalPoll = setInterval(() => fetchApprovals(), 2000);

    return () => {
      clearInterval(serverPoll);
      clearInterval(approvalPoll);
    };
  }, []);

  // Filter & Sort
  let filtered = servers.filter((s) => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    const nameMatch = (s.displayName || '').toLowerCase().includes(q);
    const idMatch = (s.id || '').toLowerCase().includes(q);
    const urlMatch = (s.url || '').toLowerCase().includes(q);
    const catMatch = (s.categories || []).some((c) => (c || '').toLowerCase().includes(q));
    return nameMatch || idMatch || urlMatch || catMatch;
  });

  filtered.sort((a, b) => {
    if (sortBy === 'name-asc') return (a.displayName || '').localeCompare(b.displayName || '');
    if (sortBy === 'name-desc') return (b.displayName || '').localeCompare(a.displayName || '');
    if (sortBy === 'type') return (a.type || '').localeCompare(b.type || '');
    if (sortBy === 'category') {
      const catA = a.categories?.[0] || 'Uncategorized';
      const catB = b.categories?.[0] || 'Uncategorized';
      return catA.localeCompare(catB);
    }
    // Default: status-priority
    const getPriority = (s: McpServer) => {
      if (!s.enabled) return 3;
      if (s.connectionStatus === 'Connected') return 2;
      return 1;
    };
    return getPriority(a) - getPriority(b);
  });

  const renderServerContent = () => {
    if (groupBy === 'none') {
      const totalItems = filtered.length;
      const effectivePageSize = pageSize === 'all' ? totalItems : pageSize;
      const totalPages = Math.ceil(totalItems / effectivePageSize);
      let activePage = currentPage;
      if (activePage > totalPages) activePage = Math.max(1, totalPages);

      const startIndex = (activePage - 1) * effectivePageSize;
      const endIndex = Math.min(startIndex + effectivePageSize, totalItems);
      const pageItems = filtered.slice(startIndex, endIndex);

      return pageItems.map((server) => (
        <ServerCard key={server.id} server={server} />
      ));
    } else {
      const groups: Record<string, McpServer[]> = {};
      filtered.forEach((server) => {
        let key = 'Uncategorized';
        if (groupBy === 'category') {
          key = server.categories && server.categories.length > 0 ? server.categories[0] : 'Uncategorized';
        } else if (groupBy === 'status') {
          key = server.enabled ? server.connectionStatus || 'Disconnected' : 'Disabled';
        } else if (groupBy === 'type') {
          key = (server.type || 'SSE').toUpperCase();
        }

        if (!groups[key]) groups[key] = [];
        groups[key].push(server);
      });

      const groupEntries = Object.entries(groups);
      const totalGroups = groupEntries.length;
      const effectivePageSize = pageSize === 'all' ? totalGroups : pageSize;
      const totalPages = Math.ceil(totalGroups / effectivePageSize);
      let activePage = currentPage;
      if (activePage > totalPages) activePage = Math.max(1, totalPages);

      const startIndex = (activePage - 1) * effectivePageSize;
      const endIndex = Math.min(startIndex + effectivePageSize, totalGroups);
      const pageGroupEntries = groupEntries.slice(startIndex, endIndex);

      return pageGroupEntries.map(([groupName, groupServers]) => {
        const groupId = encodeURIComponent(groupName.toLowerCase().replace(/\s+/g, '-'));
        const isCollapsed = collapsedGroups.includes(groupId);

        return (
          <div key={groupName}>
            <div
              className="server-group-header"
              onClick={() => toggleGroupCollapse(groupId)}
              style={{ cursor: 'pointer', userSelect: 'none' }}
            >
              <i className={`fa-solid ${isCollapsed ? 'fa-chevron-right' : 'fa-chevron-down'} group-toggle-icon`}></i>
              <i className="fa-solid fa-folder"></i>
              <span>{groupName}</span>
              <span className="server-badge" style={{ marginLeft: 'auto' }}>
                {groupServers.length}
              </span>
            </div>
            {!isCollapsed && (
              <div className="server-group-body">
                {groupServers.map((server) => (
                  <ServerCard key={server.id} server={server} />
                ))}
              </div>
            )}
          </div>
        );
      });
    }
  };

  return (
    <div id="view-dashboard" className="view-panel active">
      <main className="dashboard-main">
        {/* Left Panel */}
        <section className="left-panel">
          <StatsCard />
          <ApprovalsCard />
          <RegisteredClientsCard />
          <ClientSetupGuide />
        </section>

        {/* Right Panel */}
        <section className="right-panel">
          <div className="glass-card servers-card">
            <div className="card-header-btn">
              <h2>
                <i className="fa-solid fa-server"></i> Backend MCP Servers
              </h2>
              <div className="header-actions" style={{ display: 'flex', gap: '8px' }}>
                <button className="btn btn-secondary" onClick={() => fetchServers(true)}>
                  <i className="fa-solid fa-arrows-rotate"></i> Refresh
                </button>
                <button className="btn btn-primary" onClick={useServerStore.getState().openAddModal}>
                  <i className="fa-solid fa-plus"></i> Add Server
                </button>
              </div>
            </div>

            <ServerControlsToolbar />

            <div className="servers-list" id="servers-list">
              {servers.length === 0 ? (
                <div className="empty-state">No backend servers configured.</div>
              ) : filtered.length === 0 ? (
                <div className="empty-state">No servers matching search query.</div>
              ) : (
                renderServerContent()
              )}
            </div>

            {filtered.length > 0 && <PaginationToolbar filtered={filtered} />}
          </div>
        </section>
      </main>
    </div>
  );
};
export default DashboardView;
