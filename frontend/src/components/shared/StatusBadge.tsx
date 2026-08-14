import React from 'react';

interface StatusBadgeProps {
  status: 'online' | 'connected' | 'connecting' | 'failed' | 'disabled' | 'disconnected' | 'warning' | string;
  label?: string;
  showIndicator?: boolean;
  className?: string;
  title?: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  label,
  showIndicator = false,
  className = '',
  title
}) => {
  const norm = status.toLowerCase();
  let badgeClass = 'server-badge';
  let indicator: React.ReactNode = null;

  if (norm === 'online' || norm === 'connected') {
    badgeClass += ' badge-success';
    if (showIndicator) {
      indicator = <span className="indicator online"></span>;
    }
  } else if (norm === 'connecting' || norm === 'retrying' || norm === 'warning') {
    badgeClass += ' badge-warning';
    indicator = <i className="fa-solid fa-spinner fa-spin"></i>;
  } else if (norm === 'failed' || norm === 'error') {
    badgeClass += ' badge-danger';
    indicator = <i className="fa-solid fa-triangle-exclamation"></i>;
  } else {
    badgeClass += ' badge-secondary';
  }

  const displayText = label || status;

  return (
    <span className={`${badgeClass} ${className}`.trim()} title={title}>
      {indicator} {displayText}
    </span>
  );
};
