import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

let logInterval = null;
let autoScroll = true;
const renderedLogIds = new Set();

export function initLogs() {
    const terminal = document.getElementById('logs-terminal');
    if (terminal) {
        terminal.innerHTML = '<div class="empty-state">Logs loading...</div>';
    }
    renderedLogIds.clear();
    setupLogsEvents();
    startLogging();
}

function startLogging() {
    loadLogs();
    logInterval = setInterval(loadLogs, 3000);
}

export function stopLogging() {
    if (logInterval) {
        clearInterval(logInterval);
        logInterval = null;
    }
}

async function loadLogs() {
    try {
        const logs = await apiRequest('/api/logs');
        renderLogs(logs);
    } catch (err) {
        console.error('Failed to load system logs:', err);
    }
}

function renderLogs(logs) {
    const terminal = document.getElementById('logs-terminal');
    if (!terminal) return;

    const levelFilter = document.getElementById('logs-level-filter').value;
    
    // Check if the server cleared the log buffer (e.g. response list has no overlap with already rendered items)
    if (renderedLogIds.size > 0 && logs.length > 0) {
        const hasOverlap = logs.some(log => renderedLogIds.has(log.id));
        if (!hasOverlap) {
            terminal.innerHTML = '';
            renderedLogIds.clear();
        }
    }

    const typeFilter = document.getElementById('logs-type-filter').value;

    const filtered = logs.filter(log => {
        const isRpc = log.message.startsWith('[JSON-RPC');
        if (typeFilter === 'system' && isRpc) return false;
        if (typeFilter === 'rpc' && !isRpc) return false;

        if (levelFilter === 'ALL') return true;
        if (levelFilter === 'INFO' && log.level >= 2) return true;
        if (levelFilter === 'WARNING' && log.level >= 3) return true;
        if (levelFilter === 'ERROR' && log.level >= 4) return true;
        return false;
    });

    if (filtered.length === 0) {
        if (renderedLogIds.size === 0) {
            terminal.innerHTML = '<div class="empty-state">No log entries matching filter.</div>';
        }
        return;
    }

    // Remove empty state message if we have actual logs to print
    const emptyState = terminal.querySelector('.empty-state');
    if (emptyState) {
        emptyState.remove();
    }

    // Filter only new log entries that haven't been rendered yet
    const newLogs = filtered.filter(log => !renderedLogIds.has(log.id));
    if (newLogs.length === 0) {
        return; // Nothing new to render
    }

    const fragment = document.createDocumentFragment();
    newLogs.forEach(log => {
        renderedLogIds.add(log.id);
        const time = new Date(log.timestamp).toLocaleTimeString();
        
        const logLine = document.createElement('div');
        logLine.className = 'log-line';

        if (typeFilter === 'rpc') {
            const match = log.message.match(/^\[JSON-RPC ([^\]]+)\]\s*(.*)$/);
            if (match) {
                const direction = match[1];
                let payload = match[2];
                try {
                    const obj = JSON.parse(payload);
                    payload = JSON.stringify(obj, null, 2);
                } catch { }
                const badgeClass = direction.includes('->') ? 'log-level-badge log-level-info' : 'log-level-badge log-level-warning';
                
                logLine.style.borderLeft = '2px solid var(--accent)';
                logLine.style.paddingLeft = '8px';
                logLine.style.marginBottom = '8px';
                logLine.innerHTML = `
                    <span class="log-time">[${time}]</span>
                    <span class="${badgeClass}" style="cursor:default;">${escapeHtml(direction)}</span>
                    <div class="log-msg" style="width: 100%;">
                        <pre style="margin: 4px 0 0 0; background: rgba(0,0,0,0.3); padding: 8px; border-radius: 6px; font-family: monospace; font-size: 11px; overflow-x: auto; color: #fff; border: 1px solid rgba(255,255,255,0.05); max-height: 250px;">${escapeHtml(payload)}</pre>
                    </div>
                `;
            } else {
                logLine.innerHTML = `
                    <span class="log-time">[${time}]</span>
                    <div class="log-msg">
                        <span>${escapeHtml(log.message)}</span>
                    </div>
                `;
            }
        } else {
            const levelName = getLogLevelName(log.level);
            const levelClass = `log-level-badge log-level-${levelName.toLowerCase()}`;
            const cleanCategory = log.category.split('.').pop();
            
            let exceptionHtml = '';
            if (log.exception) {
                exceptionHtml = `<div class="log-exception">${escapeHtml(log.exception)}</div>`;
            }
            
            logLine.innerHTML = `
                <span class="log-time">[${time}]</span>
                <span class="${levelClass}">${levelName}</span>
                <span class="log-category">${escapeHtml(cleanCategory)}:</span>
                <div class="log-msg">
                    <span>${escapeHtml(log.message)}</span>
                    ${exceptionHtml}
                </div>
            `;
        }
        fragment.appendChild(logLine);
    });

    terminal.appendChild(fragment);

    // Keep log output DOM size bounded (e.g. max 500 lines)
    while (terminal.childElementCount > 500) {
        terminal.firstElementChild.remove();
    }

    if (autoScroll) {
        terminal.scrollTop = terminal.scrollHeight;
    }
}

function getLogLevelName(level) {
    switch (level) {
        case 0: return 'TRACE';
        case 1: return 'DEBUG';
        case 2: return 'INFO';
        case 3: return 'WARNING';
        case 4: return 'ERROR';
        case 5: return 'CRITICAL';
        default: return 'UNKNOWN';
    }
}

function setupLogsEvents() {
    const typeFilter = document.getElementById('logs-type-filter');
    const filter = document.getElementById('logs-level-filter');
    const clearBtn = document.getElementById('btn-clear-logs');
    const scrollToggle = document.getElementById('logs-autoscroll');
    const terminal = document.getElementById('logs-terminal');

    typeFilter.addEventListener('change', () => {
        if (terminal) {
            terminal.innerHTML = '';
        }
        renderedLogIds.clear();
        loadLogs();
    });

    filter.addEventListener('change', () => {
        // Clear screen and redraw completely on filter change
        if (terminal) {
            terminal.innerHTML = '';
        }
        renderedLogIds.clear();
        loadLogs();
    });

    clearBtn.addEventListener('click', async () => {
        try {
            await apiRequest('/api/logs', { method: 'DELETE' });
            if (terminal) {
                terminal.innerHTML = '';
            }
            renderedLogIds.clear();
            await loadLogs();
        } catch (err) {
            console.error('Failed to clear logs:', err);
        }
    });

    scrollToggle.addEventListener('change', (e) => {
        autoScroll = e.target.checked;
        if (autoScroll && terminal) {
            terminal.scrollTop = terminal.scrollHeight;
        }
    });

    // Detect user scrolling up to temporarily pause auto-scroll
    if (terminal) {
        terminal.addEventListener('scroll', () => {
            // Check if scroll is near the bottom (within 25px threshold)
            const isAtBottom = terminal.scrollHeight - terminal.scrollTop - terminal.clientHeight < 25;
            if (!isAtBottom) {
                if (autoScroll) {
                    autoScroll = false;
                    scrollToggle.checked = false;
                }
            } else {
                if (!autoScroll) {
                    autoScroll = true;
                    scrollToggle.checked = true;
                }
            }
        });
    }
}
