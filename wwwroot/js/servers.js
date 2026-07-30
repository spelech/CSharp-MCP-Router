import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

export let allServers = [];
let currentPage = 1;
let pageSize = 6;
let searchQuery = '';
let sortBy = 'status-priority';
let groupBy = 'none';
let collapsedGroups = new Set();

export function toggleServerGroup(groupId) {
    if (collapsedGroups.has(groupId)) {
        collapsedGroups.delete(groupId);
    } else {
        collapsedGroups.add(groupId);
    }
    const body = document.getElementById(`group-body-${groupId}`);
    const icon = document.getElementById(`group-icon-${groupId}`);
    if (body) {
        body.style.display = collapsedGroups.has(groupId) ? 'none' : 'block';
    }
    if (icon) {
        icon.className = collapsedGroups.has(groupId) ? 'fa-solid fa-chevron-right group-toggle-icon' : 'fa-solid fa-chevron-down group-toggle-icon';
    }
}
window.toggleServerGroup = toggleServerGroup;

export function onServerSearchInput(val) {
    searchQuery = (val || '').toLowerCase().trim();
    currentPage = 1;
    renderServers(allServers);
}

export function onServerSortChange(val) {
    sortBy = val;
    currentPage = 1;
    renderServers(allServers);
}

export function onServerGroupChange(val) {
    groupBy = val;
    currentPage = 1;
    renderServers(allServers);
}

export function changeServerPage(delta) {
    currentPage += delta;
    renderServers(allServers);
}

export function changeServerPageSize(newSize) {
    pageSize = newSize === 'all' ? 'all' : parseInt(newSize, 10);
    currentPage = 1;
    renderServers(allServers);
}

window.onServerSearchInput = onServerSearchInput;
window.onServerSortChange = onServerSortChange;
window.onServerGroupChange = onServerGroupChange;
window.changeServerPage = changeServerPage;
window.changeServerPageSize = changeServerPageSize;

export async function loadServers(refresh = false) {
    try {
        if (refresh) {
            try { await apiRequest('/api/servers/reconnect-all', { method: 'POST' }); } catch {}
        }
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
        updatePaginationInfo(0, 0, 0, 1, 1);
        return;
    }
    
    // 1. Search Filter
    let filtered = servers.filter(s => {
        if (!searchQuery) return true;
        const nameMatch = (s.displayName || '').toLowerCase().includes(searchQuery);
        const idMatch = (s.id || '').toLowerCase().includes(searchQuery);
        const urlMatch = (s.url || '').toLowerCase().includes(searchQuery);
        const catMatch = (s.categories || []).some(c => (c || '').toLowerCase().includes(searchQuery));
        return nameMatch || idMatch || urlMatch || catMatch;
    });

    if (filtered.length === 0) {
        list.innerHTML = `<div class="empty-state">No servers matching search query "${escapeHtml(searchQuery)}".</div>`;
        updatePaginationInfo(0, 0, 0, 1, 1);
        return;
    }

    // 2. Sort
    filtered.sort((a, b) => {
        if (sortBy === 'name-asc') return (a.displayName || '').localeCompare(b.displayName || '');
        if (sortBy === 'name-desc') return (b.displayName || '').localeCompare(a.displayName || '');
        if (sortBy === 'type') return (a.type || '').localeCompare(b.type || '');
        if (sortBy === 'category') {
            const catA = (a.categories && a.categories[0]) ? a.categories[0] : 'Uncategorized';
            const catB = (b.categories && b.categories[0]) ? b.categories[0] : 'Uncategorized';
            return catA.localeCompare(catB);
        }
        // Default: status-priority (Disconnected/Failed enabled servers first, connected next, disabled last)
        const getPriority = (s) => {
            if (!s.enabled) return 3;
            if (s.connectionStatus === 'Connected') return 2;
            return 1;
        };
        return getPriority(a) - getPriority(b);
    });

    // 3. Grouping & Pagination
    if (groupBy === 'none') {
        const totalItems = filtered.length;
        const effectivePageSize = pageSize === 'all' ? totalItems : pageSize;
        const totalPages = Math.max(1, Math.ceil(totalItems / (effectivePageSize || 1)));

        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        const startIndex = (currentPage - 1) * effectivePageSize;
        const endIndex = Math.min(startIndex + effectivePageSize, totalItems);
        const pageItems = filtered.slice(startIndex, endIndex);

        updatePaginationInfo(startIndex + 1, endIndex, totalItems, currentPage, totalPages, 'servers');
        list.innerHTML = pageItems.map(server => renderSingleServerCard(server)).join('');
    } else {
        const groups = {};
        filtered.forEach(server => {
            let key = 'Uncategorized';
            if (groupBy === 'category') {
                key = (server.categories && server.categories.length > 0) ? server.categories[0] : 'Uncategorized';
            } else if (groupBy === 'status') {
                key = server.enabled ? (server.connectionStatus || 'Disconnected') : 'Disabled';
            } else if (groupBy === 'type') {
                key = (server.type || 'SSE').toUpperCase();
            }
            if (!groups[key]) groups[key] = [];
            groups[key].push(server);
        });

        const groupEntries = Object.entries(groups);
        const totalGroups = groupEntries.length;
        const effectivePageSize = pageSize === 'all' ? totalGroups : pageSize;
        const totalPages = Math.max(1, Math.ceil(totalGroups / (effectivePageSize || 1)));

        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        const startIndex = (currentPage - 1) * effectivePageSize;
        const endIndex = Math.min(startIndex + effectivePageSize, totalGroups);
        const pageGroupEntries = groupEntries.slice(startIndex, endIndex);

        updatePaginationInfo(startIndex + 1, endIndex, totalGroups, currentPage, totalPages, 'groups', filtered.length);

        let html = '';
        for (const [groupName, groupServers] of pageGroupEntries) {
            const groupId = encodeURIComponent(groupName.toLowerCase().replace(/\s+/g, '-'));
            const isCollapsed = collapsedGroups.has(groupId);
            const iconClass = isCollapsed ? 'fa-solid fa-chevron-right group-toggle-icon' : 'fa-solid fa-chevron-down group-toggle-icon';
            const bodyStyle = isCollapsed ? 'display: none;' : 'display: block;';

            html += `
                <div class="server-group-header" onclick="window.toggleServerGroup('${groupId}')" style="cursor: pointer; user-select: none;">
                    <i class="${iconClass}" id="group-icon-${groupId}"></i>
                    <i class="fa-solid fa-folder"></i>
                    <span>${escapeHtml(groupName)}</span>
                    <span class="server-badge" style="margin-left: auto;">${groupServers.length}</span>
                </div>
                <div class="server-group-body" id="group-body-${groupId}" style="${bodyStyle}">
                    ${groupServers.map(server => renderSingleServerCard(server)).join('')}
                </div>
            `;
        }
        list.innerHTML = html;
    }
}

function renderSingleServerCard(server) {
    const isDisconnected = server.enabled && server.connectionStatus !== 'Connected';
    const itemClass = isDisconnected ? 'server-item server-disconnected-pulse' : 'server-item';
    const nameClass = server.enabled ? 'server-name' : 'server-name text-muted';
    const categoryBadge = (server.categories && server.categories.length > 0)
        ? server.categories.map(cat => `<span class="server-badge" style="background: rgba(59,130,246,0.1); color: var(--primary);">${escapeHtml(cat)}</span>`).join('')
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
        <div class="${itemClass}">
            <div class="server-info">
                <div class="server-name-row">
                    <span class="${nameClass}">${escapeHtml(server.displayName)}</span>
                    <span class="server-badge">${escapeHtml((server.type || 'SSE').toUpperCase())}</span>
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

export function onAuthShapeChange(shape) {
    const customHeaderGroup = document.getElementById('group-custom-header-name');
    if (customHeaderGroup) {
        if (shape === 'custom-header' || shape === 'query') {
            customHeaderGroup.style.display = 'block';
        } else {
            customHeaderGroup.style.display = 'none';
        }
    }
}
window.onAuthShapeChange = onAuthShapeChange;

export function openModal() {
    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-plus"></i> Add MCP Server';
    document.getElementById('server-id').value = '';
    document.getElementById('server-name').value = '';
    document.getElementById('server-type').value = 'sse';
    document.getElementById('server-category').value = 'infrastructure';
    document.getElementById('server-url').value = '';
    document.getElementById('server-key').value = '';
    document.getElementById('server-secret-provider').value = 'None';
    document.getElementById('server-secret-key').value = '';
    document.getElementById('server-auth-shape').value = 'bearer';
    document.getElementById('server-custom-header-name').value = '';
    onAuthShapeChange('bearer');
    document.getElementById('server-enabled').checked = true;
    document.getElementById('server-hidden').checked = false;
    document.getElementById('server-modal').style.display = 'flex';
}
export const openAddModal = openModal;

export function editServer(id) {
    const server = allServers.find(s => s.id === id);
    if (!server) return;

    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-pen"></i> Edit MCP Server';
    document.getElementById('server-id').value = server.id;
    document.getElementById('server-name').value = server.displayName;
    document.getElementById('server-type').value = server.type || 'sse';
    document.getElementById('server-category').value = server.categories ? server.categories.join(', ') : 'default';
    document.getElementById('server-url').value = server.url;
    document.getElementById('server-key').value = '';
    document.getElementById('server-secret-provider').value = server.secretProvider || 'None';
    document.getElementById('server-secret-key').value = server.secretItemKey || '';
    document.getElementById('server-auth-shape').value = server.authShape || 'bearer';
    document.getElementById('server-custom-header-name').value = server.customHeaderName || '';
    onAuthShapeChange(server.authShape || 'bearer');
    document.getElementById('server-enabled').checked = server.enabled;
    document.getElementById('server-hidden').checked = server.hidden;
    document.getElementById('server-modal').style.display = 'flex';
}
export const openEditModal = editServer;

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
        categories: document.getElementById('server-category').value.split(',').map(s => s.trim()).filter(Boolean),
        url: document.getElementById('server-url').value,
        secretProvider: document.getElementById('server-secret-provider').value,
        secretItemKey: document.getElementById('server-secret-key').value,
        authShape: document.getElementById('server-auth-shape').value,
        customHeaderName: document.getElementById('server-custom-header-name').value,
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

function updatePaginationInfo(start, end, total, page, totalPages, unitLabel = 'servers', serverCount = null) {
    const rangeEl = document.getElementById('pagination-range');
    const totalEl = document.getElementById('pagination-total');
    const pageNumEl = document.getElementById('pagination-page-num');
    const prevBtn = document.getElementById('btn-prev-page');
    const nextBtn = document.getElementById('btn-next-page');

    if (rangeEl) rangeEl.textContent = total > 0 ? `${start}-${end}` : '0-0';
    if (totalEl) {
        if (unitLabel === 'groups' && serverCount !== null) {
            totalEl.textContent = `${total} groups (${serverCount} servers)`;
        } else {
            totalEl.textContent = `${total} ${unitLabel}`;
        }
    }
    if (pageNumEl) pageNumEl.textContent = `Page ${page || 1} of ${totalPages || 1}`;
    if (prevBtn) prevBtn.disabled = (page <= 1);
    if (nextBtn) nextBtn.disabled = (page >= totalPages);
}
