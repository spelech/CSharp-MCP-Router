import React from 'react';
import { McpServer, useServerStore } from '../../stores/useServerStore';

interface PaginationToolbarProps {
  filtered: McpServer[];
}

export const PaginationToolbar: React.FC<PaginationToolbarProps> = ({ filtered }) => {
  const { groupBy, currentPage, pageSize, setCurrentPage, setPageSize } = useServerStore();

  let totalItems = filtered.length;
  let unitLabel = 'servers';

  if (groupBy !== 'none') {
    const groups = new Set<string>();
    filtered.forEach((server) => {
      let key = 'Uncategorized';
      if (groupBy === 'category') {
        key = server.categories?.[0] || 'Uncategorized';
      } else if (groupBy === 'status') {
        key = server.enabled ? server.connectionStatus || 'Disconnected' : 'Disabled';
      } else if (groupBy === 'type') {
        key = (server.type || 'SSE').toUpperCase();
      }
      groups.add(key);
    });
    totalItems = groups.size;
    unitLabel = 'groups';
  }

  const effectivePageSize = pageSize === 'all' ? totalItems : pageSize;
  const totalPages = Math.max(1, Math.ceil(totalItems / (effectivePageSize || 1)));

  let activePage = currentPage;
  if (activePage > totalPages) activePage = Math.max(1, totalPages);

  const start = totalItems > 0 ? (activePage - 1) * effectivePageSize + 1 : 0;
  const end = Math.min(start + effectivePageSize - 1, totalItems);

  return (
    <div
      className="pagination-toolbar"
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginTop: '15px',
        paddingTop: '12px',
        borderTop: '1px solid rgba(255, 255, 255, 0.05)',
        fontSize: '13px',
      }}
    >
      <div className="pagination-info" style={{ color: 'var(--text-muted)' }}>
        Showing {start}-{end} of {totalItems} {unitLabel}
        {unitLabel === 'groups' && ` (${filtered.length} servers)`}
      </div>
      <div className="pagination-controls" style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
        <button
          className="btn btn-secondary btn-sm"
          disabled={activePage <= 1}
          onClick={() => setCurrentPage(activePage - 1)}
        >
          <i className="fa-solid fa-chevron-left"></i> Prev
        </button>
        <span style={{ fontWeight: 600, color: 'var(--text-main)' }}>
          Page {activePage} of {totalPages}
        </span>
        <button
          className="btn btn-secondary btn-sm"
          disabled={activePage >= totalPages}
          onClick={() => setCurrentPage(activePage + 1)}
        >
          Next <i className="fa-solid fa-chevron-right"></i>
        </button>
        <select
          value={pageSize}
          onChange={(e) => setPageSize(e.target.value === 'all' ? 'all' : parseInt(e.target.value, 10))}
          className="form-select form-select-sm"
          style={{
            width: 'auto',
            padding: '4px 8px',
            background: 'rgba(0,0,0,0.2)',
            color: 'var(--text-main)',
            border: '1px solid var(--border-color)',
            borderRadius: '6px',
          }}
        >
          <option value="6">6 / page</option>
          <option value="12">12 / page</option>
          <option value="all">All</option>
        </select>
      </div>
    </div>
  );
};
