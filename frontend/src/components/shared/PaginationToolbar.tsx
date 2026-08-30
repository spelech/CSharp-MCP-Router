import React from 'react';

interface PaginationToolbarProps {
  currentPage: number;
  pageSize: number | 'all';
  totalItems: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number | 'all') => void;
}

export const PaginationToolbar: React.FC<PaginationToolbarProps> = ({
  currentPage,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
}) => {
  if (totalItems === 0) return null;

  const effectivePageSize = pageSize === 'all' ? totalItems : pageSize;
  const totalPages = Math.max(1, Math.ceil(totalItems / effectivePageSize));
  const currentEffectivePage = Math.min(currentPage, totalPages);

  const startItem = totalItems === 0 ? 0 : (currentEffectivePage - 1) * effectivePageSize + 1;
  const endItem = pageSize === 'all' ? totalItems : Math.min(currentEffectivePage * effectivePageSize, totalItems);

  return (
    <div className="pagination-toolbar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '15px', padding: '10px 0', borderTop: '1px solid var(--border-color)', fontSize: '13px' }}>
      <div className="pagination-info" style={{ color: 'var(--text-muted)' }}>
        Showing <span style={{ color: 'var(--text-main)', fontWeight: 600 }}>{startItem}-{endItem}</span> of <span style={{ color: 'var(--text-main)', fontWeight: 600 }}>{totalItems}</span> items
      </div>

      <div className="pagination-controls" style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
        <div className="page-size-selector" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <label htmlFor="page-size-select" style={{ color: 'var(--text-muted)' }}>Per Page:</label>
          <select
            id="page-size-select"
            value={pageSize}
            onChange={(e) => {
              const val = e.target.value === 'all' ? 'all' : parseInt(e.target.value, 10);
              onPageSizeChange(val);
            }}
            style={{ padding: '4px 8px', borderRadius: '4px', background: 'var(--bg-dark)', color: 'var(--text-main)', border: '1px solid var(--border-color)', fontSize: '12px' }}
          >
            <option value={6}>6</option>
            <option value={12}>12</option>
            <option value={24}>24</option>
            <option value="all">All</option>
          </select>
        </div>

        {pageSize !== 'all' && totalPages > 1 && (
          <div className="page-nav" style={{ display: 'flex', alignItems: 'center', gap: '5px' }}>
            <button
              className="btn btn-secondary btn-sm"
              disabled={currentEffectivePage <= 1}
              onClick={() => onPageChange(currentEffectivePage - 1)}
              style={{ padding: '6px 10px', minWidth: '36px', minHeight: '36px', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}
              aria-label="Previous page"
            >
              <i className="fa-solid fa-chevron-left"></i>
            </button>
            <span style={{ margin: '0 8px', color: 'var(--text-muted)' }}>
              Page <strong style={{ color: 'var(--text-main)' }}>{currentEffectivePage}</strong> of <strong>{totalPages}</strong>
            </span>
            <button
              className="btn btn-secondary btn-sm"
              disabled={currentEffectivePage >= totalPages}
              onClick={() => onPageChange(currentEffectivePage + 1)}
              style={{ padding: '6px 10px', minWidth: '36px', minHeight: '36px', display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}
              aria-label="Next page"
            >
              <i className="fa-solid fa-chevron-right"></i>
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
