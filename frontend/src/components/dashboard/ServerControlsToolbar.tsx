import React from 'react';
import { useServerStore } from '../../stores/useServerStore';

export const ServerControlsToolbar: React.FC = () => {
  const { searchQuery, sortBy, groupBy, setSearchQuery, setSortBy, setGroupBy } = useServerStore();

  return (
    <div
      className="server-controls-toolbar"
      style={{
        display: 'flex',
        gap: '10px',
        marginBottom: '15px',
        flexWrap: 'wrap',
        alignItems: 'center',
        background: 'rgba(255,255,255,0.02)',
        padding: '10px 12px',
        borderRadius: '8px',
        border: '1px solid rgba(255,255,255,0.04)',
      }}
    >
      <div style={{ position: 'relative', flex: 1, minWidth: '180px' }}>
        <i
          className="fa-solid fa-magnifying-glass"
          style={{
            position: 'absolute',
            left: '10px',
            top: '50%',
            transform: 'translateY(-50%)',
            color: 'var(--text-muted)',
            fontSize: '12px',
          }}
        ></i>
        <input
          type="text"
          placeholder="Search servers, URLs, or categories..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{
            paddingLeft: '30px',
            fontSize: '12px',
            height: '32px',
            width: '100%',
            border: '1px solid var(--border-color)',
            background: 'rgba(0,0,0,0.2)',
            color: 'var(--text-main)',
            borderRadius: '6px',
          }}
        />
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <span style={{ fontSize: '11px', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
          <i className="fa-solid fa-sort"></i> Sort:
        </span>
        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value)}
          className="form-select form-select-sm"
          style={{
            padding: '4px 8px',
            fontSize: '12px',
            background: 'rgba(0,0,0,0.2)',
            color: 'var(--text-main)',
            border: '1px solid var(--border-color)',
            borderRadius: '6px',
          }}
        >
          <option value="status-priority">Status (Errors First)</option>
          <option value="name-asc">Name (A-Z)</option>
          <option value="name-desc">Name (Z-A)</option>
          <option value="type">Type (SSE / HTTP)</option>
          <option value="category">Category</option>
        </select>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <span style={{ fontSize: '11px', color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
          <i className="fa-solid fa-layer-group"></i> Group:
        </span>
        <select
          value={groupBy}
          onChange={(e) => setGroupBy(e.target.value)}
          className="form-select form-select-sm"
          style={{
            padding: '4px 8px',
            fontSize: '12px',
            background: 'rgba(0,0,0,0.2)',
            color: 'var(--text-main)',
            border: '1px solid var(--border-color)',
            borderRadius: '6px',
          }}
        >
          <option value="none">None</option>
          <option value="category">Category</option>
          <option value="status">Status</option>
          <option value="type">Type</option>
        </select>
      </div>
    </div>
  );
};
