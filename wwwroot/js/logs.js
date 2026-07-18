import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

let logInterval = null;
let autoScroll = true;

export function initLogs() {
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
    const levelFilter = document.getElementById('logs-level-filter').value;
    
    const filtered = logs.filter(log => {
        if (levelFilter === 'ALL') return true;
        // Map C# LogLevel integers to string labels (0 = Trace, 1 = Debug, 2 = Info, 3 = Warning, 4 = Error, 5 = Critical)
        if (levelFilter === 'INFO' && log.level >= 2) return true;
        if (levelFilter === 'WARNING' && log.level >= 3) return true;
        if (levelFilter === 'ERROR' && log.level >= 4) return true;
        return false;
    });

    if (filtered.length === 0) {
        terminal.innerHTML = '<div class="empty-state">No log entries matching filter.</div>';
        return;
    }

    terminal.innerHTML = filtered.map(log => {
        const time = new Date(log.timestamp).toLocaleTimeString();
        const levelName = getLogLevelName(log.level);
        const levelClass = `log-level-badge log-level-${levelName.toLowerCase()}`;
        const cleanCategory = log.category.split('.').pop(); // Shorten namespace
        
        let exceptionHtml = '';
        if (log.exception) {
            exceptionHtml = `<div class="log-exception">${escapeHtml(log.exception)}</div>`;
        }

        return `
            <div class="log-line">
                <span class="log-time">[${time}]</span>
                <span class="${levelClass}">${levelName}</span>
                <span class="log-category">${escapeHtml(cleanCategory)}:</span>
                <div class="log-msg">
                    <span>${escapeHtml(log.message)}</span>
                    ${exceptionHtml}
                </div>
            </div>
        `;
    }).join('');

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
    const filter = document.getElementById('logs-level-filter');
    const clearBtn = document.getElementById('btn-clear-logs');
    const scrollToggle = document.getElementById('logs-autoscroll');

    filter.addEventListener('change', () => {
        loadLogs();
    });

    clearBtn.addEventListener('click', async () => {
        try {
            await apiRequest('/api/logs', { method: 'DELETE' });
            await loadLogs();
        } catch (err) {
            console.error('Failed to clear logs:', err);
        }
    });

    scrollToggle.addEventListener('change', (e) => {
        autoScroll = e.target.checked;
    });
}
