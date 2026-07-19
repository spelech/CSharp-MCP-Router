import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

export let allServers = [];

export async function loadServers() {
    try {
        allServers = await apiRequest('/api/servers');
        renderServers(allServers);
        updateStats(allServers);
        return allServers;
    } catch (error) {
        console.error('Error loading servers:', error);
        document.getElementById('servers-list').innerHTML = `
            <div class="loading-state">
                <i class="fa-solid fa-triangle-exclamation" style="color: var(--status-offline)"></i>
                <span>Error loading servers: ${error.message}</span>
            </div>
        `;
        return [];
    }
}

function renderServers(servers) {
    const list = document.getElementById('servers-list');
    if (servers.length === 0) {
        list.innerHTML = '<div class="empty-state">No backend servers configured.</div>';
        return;
    }
    
    list.innerHTML = servers.map(server => {
        const nameClass = server.enabled ? 'server-name' : 'server-name text-muted';
        const categoryBadge = server.category && server.category !== 'default' 
            ? `<span class="server-badge" style="background: rgba(59,130,246,0.1); color: var(--primary);">${escapeHtml(server.category)}</span>`
            : '';
            
        let statusBadge = '';
        let retryBtn = '';
        
        if (server.enabled) {
            const status = server.connectionStatus || 'Disconnected';
            if (status === 'Connected') {
                statusBadge = `<span class="server-badge badge-success"><span class="indicator online"></span> Connected</span>`;
            } else if (status === 'Connecting' || status === 'Retrying') {
                const attemptText = server.connectionAttempts > 0 ? ` (${server.connectionAttempts}/5)` : '';
                statusBadge = `<span class="server-badge badge-warning"><i class="fa-solid fa-spinner fa-spin"></i> ${status}${attemptText}</span>`;
            } else if (status === 'Failed') {
                const errMsg = server.connectionError ? escapeHtml(server.connectionError) : 'Connection failed';
                statusBadge = `<span class="server-badge badge-danger" title="${errMsg}"><i class="fa-solid fa-triangle-exclamation"></i> Failed</span>`;
                retryBtn = `
                    <button class="btn-icon btn-retry" title="Retry Connection (Attempts: ${server.connectionAttempts})" onclick="window.reconnectServer('${server.id}')" style="color: var(--accent);">
                        <i class="fa-solid fa-arrows-rotate"></i>
                    </button>
                `;
            } else {
                statusBadge = `<span class="server-badge badge-secondary">Disconnected</span>`;
                retryBtn = `
                    <button class="btn-icon btn-retry" title="Connect Server" onclick="window.reconnectServer('${server.id}')" style="color: var(--primary);">
                        <i class="fa-solid fa-plug"></i>
                    </button>
                `;
            }
        } else {
            statusBadge = `<span class="server-badge badge-secondary">Disabled</span>`;
        }

        return `
            <div class="server-item">
                <div class="server-info">
                    <div class="server-name-row">
                        <span class="${nameClass}">${escapeHtml(server.displayName)}</span>
                        <span class="server-badge">${escapeHtml(server.type.toUpperCase())}</span>
                        ${categoryBadge}
                        ${server.hasApiKey ? '<span class="server-badge badge-key"><i class="fa-solid fa-lock"></i> Secured</span>' : ''}
                        ${server.hidden ? '<span class="server-badge"><i class="fa-solid fa-eye-slash"></i> Hidden</span>' : ''}
                        ${statusBadge}
                    </div>
                    <span class="server-url">${escapeHtml(server.url)}</span>
                </div>
                <div class="server-actions">
                    ${retryBtn}
                    <button class="btn-icon btn-edit" title="Edit Server" onclick="window.openEditModal('${server.id}')">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>
                    <button class="btn-icon btn-delete" title="Delete Server" onclick="window.deleteServer('${server.id}', '${escapeHtml(server.displayName)}')">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                    <label class="switch">
                        <input type="checkbox" ${server.enabled ? 'checked' : ''} onchange="window.toggleServer('${server.id}', 'enabled', this.checked)">
                        <span class="slider"></span>
                    </label>
                </div>
            </div>
        `;
    }).join('');
}

export async function toggleServer(id, property, value) {
    try {
        const body = {};
        body[property] = value;
        await apiRequest(`/api/servers/${id}`, {
            method: 'PUT',
            body
        });
        await loadServers();
    } catch (error) {
        alert(`Error: ${error.message}`);
        await loadServers();
    }
}

export function openAddModal() {
    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-server"></i> Add MCP Server';
    document.getElementById('server-id').value = '';
    document.getElementById('server-form').reset();
    document.getElementById('server-enabled').checked = true;
    document.getElementById('server-hidden').checked = false;
    document.getElementById('server-modal').style.display = 'flex';
}

export function openEditModal(id) {
    const server = allServers.find(s => s.id === id);
    if (!server) return;

    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-server"></i> Edit MCP Server';
    document.getElementById('server-id').value = server.id;
    document.getElementById('server-name').value = server.displayName;
    document.getElementById('server-type').value = server.type;
    document.getElementById('server-category').value = server.category || 'default';
    document.getElementById('server-url').value = server.url;
    document.getElementById('server-key').value = '';
    document.getElementById('server-enabled').checked = server.enabled;
    document.getElementById('server-hidden').checked = server.hidden;
    document.getElementById('server-modal').style.display = 'flex';
}

export function closeModal() {
    document.getElementById('server-modal').style.display = 'none';
}

export async function saveServer(event) {
    event.preventDefault();
    const id = document.getElementById('server-id').value;
    const keyVal = document.getElementById('server-key').value;
    
    const server = {
        displayName: document.getElementById('server-name').value,
        type: document.getElementById('server-type').value,
        category: document.getElementById('server-category').value,
        url: document.getElementById('server-url').value,
        enabled: document.getElementById('server-enabled').checked,
        hidden: document.getElementById('server-hidden').checked
    };

    if (keyVal) {
        server.apiKey = keyVal;
    }

    try {
        if (id) {
            server.id = id;
            await apiRequest(`/api/servers/${id}`, {
                method: 'PUT',
                body: server
            });
        } else {
            await apiRequest('/api/servers', {
                method: 'POST',
                body: server
            });
        }
        closeModal();
        await loadServers();
    } catch (error) {
        alert(`Error saving server: ${error.message}`);
    }
}

export async function deleteServer(id, name) {
    if (!confirm(`Are you sure you want to delete the MCP server '${name}'?`)) return;
    try {
        await apiRequest(`/api/servers/${id}`, {
            method: 'DELETE'
        });
        await loadServers();
    } catch (error) {
        alert(`Error deleting server: ${error.message}`);
    }
}

function updateStats(servers) {
    document.getElementById('server-count').textContent = servers.length;
    document.getElementById('active-servers').textContent = servers.filter(s => s.enabled).length;
}

export async function reconnectServer(id) {
    try {
        await apiRequest(`/api/servers/${id}/reconnect`, {
            method: 'POST'
        });
        await loadServers();
    } catch (error) {
        console.error('Error triggering reconnect:', error);
    }
}
