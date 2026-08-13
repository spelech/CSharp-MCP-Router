import React, { useRef, useEffect } from 'react';
import { useLogStore, LogEntry } from '../../stores/useLogStore';

export const LogsTerminalCard: React.FC = () => {
  const {
    logs,
    typeFilter,
    levelFilter,
    autoScroll,
    setTypeFilter,
    setLevelFilter,
    setAutoScroll,
    clearLogs,
  } = useLogStore();

  const terminalRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const terminal = terminalRef.current;
    if (terminal && autoScroll) {
      terminal.scrollTop = terminal.scrollHeight;
    }
  }, [logs, autoScroll]);

  const getFilteredLogs = () => {
    return logs.filter((log) => {
      const isRpc = log.message.startsWith('[JSON-RPC');
      if (typeFilter === 'system' && isRpc) return false;
      if (typeFilter === 'rpc' && !isRpc) return false;

      if (levelFilter === 'ALL') return true;
      if (levelFilter === 'INFO' && log.level >= 2) return true;
      if (levelFilter === 'WARNING' && log.level >= 3) return true;
      if (levelFilter === 'ERROR' && log.level >= 4) return true;
      return false;
    });
  };

  const handleTerminalScroll = () => {
    const terminal = terminalRef.current;
    if (!terminal) return;
    const isAtBottom = terminal.scrollHeight - terminal.scrollTop - terminal.clientHeight < 25;
    if (!isAtBottom && autoScroll) {
      setAutoScroll(false);
    } else if (isAtBottom && !autoScroll) {
      setAutoScroll(true);
    }
  };

  const getLogLevelName = (level: number) => {
    switch (level) {
      case 0: return 'TRACE';
      case 1: return 'DEBUG';
      case 2: return 'INFO';
      case 3: return 'WARNING';
      case 4: return 'ERROR';
      case 5: return 'CRITICAL';
      default: return 'UNKNOWN';
    }
  };

  return (
    <div className="tester-full-width">
      <div className="glass-card logs-viewer-card">
        <h2>
          <i className="fa-solid fa-list-check"></i> System Logs
        </h2>
        <div className="logs-controls">
          <div className="logs-filters">
            <select
              id="logs-type-filter"
              style={{ marginRight: '10px' }}
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value as any)}
            >
              <option value="system">System Logs</option>
              <option value="rpc">JSON-RPC Stream</option>
            </select>
            <select
              id="logs-level-filter"
              value={levelFilter}
              onChange={(e) => setLevelFilter(e.target.value as any)}
            >
              <option value="ALL">All Levels</option>
              <option value="INFO">Info & Above</option>
              <option value="WARNING">Warnings</option>
              <option value="ERROR">Errors</option>
            </select>
            <div className="checkbox-group">
              <label className="switch">
                <input
                  type="checkbox"
                  id="logs-autoscroll"
                  checked={autoScroll}
                  onChange={(e) => setAutoScroll(e.target.checked)}
                />
                <span className="slider"></span>
              </label>
              <span className="checkbox-label" style={{ fontSize: '11px' }}>
                Auto-scroll
              </span>
            </div>
          </div>
          <button type="button" className="btn btn-danger btn-sm" id="btn-clear-logs" onClick={clearLogs}>
            <i className="fa-solid fa-trash"></i> Clear Logs
          </button>
        </div>

        <div
          className="logs-terminal"
          id="logs-terminal"
          ref={terminalRef}
          onScroll={handleTerminalScroll}
        >
          {getFilteredLogs().length === 0 ? (
            <div className="empty-state">No log entries matching filter.</div>
          ) : (
            getFilteredLogs().map((log: LogEntry) => {
              const time = new Date(log.timestamp).toLocaleTimeString();
              if (typeFilter === 'rpc') {
                const match = log.message.match(/^\[JSON-RPC ([^\]]+)\]\s*(.*)$/);
                if (match) {
                  const direction = match[1];
                  let payload = match[2];
                  try {
                    payload = JSON.stringify(JSON.parse(payload), null, 2);
                  } catch {
                    // Ignore parsing errors for non-JSON content
                  }
                  const badgeClass = direction.includes('->') ? 'log-level-badge log-level-info' : 'log-level-badge log-level-warning';
                  return (
                    <div key={log.id} className="log-line" style={{ borderLeft: '2px solid var(--accent)', paddingLeft: '8px', marginBottom: '8px' }}>
                      <span className="log-time">[{time}]</span>
                      <span className={badgeClass} style={{ cursor: 'default' }}>{direction}</span>
                      <div className="log-msg" style={{ width: '100%' }}>
                        <pre style={{ margin: '4px 0 0 0', background: 'rgba(0,0,0,0.3)', padding: '8px', borderRadius: '6px', fontFamily: 'monospace', fontSize: '11px', overflowX: 'auto', color: '#fff', border: '1px solid rgba(255,255,255,0.05)', maxHeight: '250px' }}>{payload}</pre>
                      </div>
                    </div>
                  );
                }
              }

              const levelName = getLogLevelName(log.level);
              const levelClass = `log-level-badge log-level-${levelName.toLowerCase()}`;
              const cleanCategory = log.category.split('.').pop() || '';
              return (
                <div key={log.id} className="log-line">
                  <span className="log-time">[{time}]</span>
                  <span className={levelClass}>{levelName}</span>
                  <span className="log-category">{cleanCategory}:</span>
                  <div className="log-msg">
                    <span>{log.message}</span>
                    {log.exception && <div className="log-exception">{log.exception}</div>}
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
};
