document.addEventListener('DOMContentLoaded', () => {
    loadUser();
    loadServers();
    loadClients();
    
    // Poll servers status every 10s
    setInterval(loadServers, 10000);
});

async function loadUser() {
    try {
        const response = await fetch('/api/me');
        if (!response.ok) return;
        const data = await response.json();
        if (data.authenticated) {
            const display = document.getElementById('user-display');
            const container = document.getElementById('user-status-item');
            
            const isAdmin = data.groups && data.groups.includes('full_admin');
            const roleIcon = isAdmin 
                ? '<i class="fa-solid fa-user-shield" style="color: var(--accent); margin-right:4px;"></i>' 
                : '<i class="fa-solid fa-user" style="margin-right:4px;"></i>';
            const groupText = isAdmin ? 'Admin' : 'User';
            
            display.innerHTML = `${roleIcon} ${escapeHtml(data.name)} (${groupText})`;
            container.style.display = 'block';
        }
    } catch (error) {
        console.error('Error loading user profile:', error);
    }
}

async function loadServers() {
    try {
        const response = await fetch('/api/servers');
        if (!response.ok) throw new Error('Failed to fetch servers');
        const servers = await response.json();
        
        renderServers(servers);
        updateStats(servers);
    } catch (error) {
        console.error('Error loading servers:', error);
        document.getElementById('servers-list').innerHTML = `
            <div class="loading-state">
                <i class="fa-solid fa-triangle-exclamation" style="color: var(--status-offline)"></i>
                <span>Error loading servers: ${error.message}</span>
            </div>
        `;
    }
}

async function loadClients() {
    try {
        const response = await fetch('/api/clients');
        if (!response.ok) throw new Error('Failed to fetch clients');
        const clients = await response.json();
        
        renderClients(clients);
    } catch (error) {
        console.error('Error loading clients:', error);
    }
}

function renderServers(servers) {
    window.allServers = servers; // Cache for edit lookup
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
        return `
            <div class="server-item">
                <div class="server-info">
                    <div class="server-name-row">
                        <span class="${nameClass}">${escapeHtml(server.displayName)}</span>
                        <span class="server-badge">${escapeHtml(server.type.toUpperCase())}</span>
                        ${categoryBadge}
                        ${server.hasApiKey ? '<span class="server-badge badge-key"><i class="fa-solid fa-lock"></i> Secured</span>' : ''}
                        ${server.hidden ? '<span class="server-badge"><i class="fa-solid fa-eye-slash"></i> Hidden</span>' : ''}
                    </div>
                    <span class="server-url">${escapeHtml(server.url)}</span>
                </div>
                <div class="server-actions">
                    <button class="btn-icon btn-edit" title="Edit Server" onclick="openEditModal('${server.id}')">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>
                    <button class="btn-icon btn-delete" title="Delete Server" onclick="deleteServer('${server.id}', '${escapeHtml(server.displayName)}')">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                    <label class="switch">
                        <input type="checkbox" ${server.enabled ? 'checked' : ''} onchange="toggleServer('${server.id}', 'enabled', this.checked)">
                        <span class="slider"></span>
                    </label>
                </div>
            </div>
        `;
    }).join('');
}

function renderClients(clients) {
    const tbody = document.getElementById('clients-body');
    if (clients.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="empty-state">No clients registered yet. Add a manual connection in Gemini to register.</td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = clients.map(client => {
        const typeBadge = client.isDynamic
            ? '<span class="server-badge" style="background: rgba(16, 185, 129, 0.1); color: var(--accent);">Dynamic</span>'
            : '<span class="server-badge">Manual</span>';
            
        return `
            <tr>
                <td><strong>${escapeHtml(client.displayName)}</strong></td>
                <td><code style="font-family: 'JetBrains Mono', monospace; font-size:11px; background: rgba(255,255,255,0.05); padding:2px 6px; border-radius:4px; color: var(--accent);">${escapeHtml(client.clientId)}</code></td>
                <td>${typeBadge}</td>
                <td>
                    <button class="btn-icon btn-delete" title="Delete Client" onclick="deleteClient('${client.id}', '${escapeHtml(client.displayName)}')">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

async function toggleServer(id, property, value) {
    try {
        const body = {};
        body[property] = value;
        
        const response = await fetch(`/api/servers/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(body)
        });
        
        if (!response.ok) throw new Error('Failed to update server status');
        loadServers();
    } catch (error) {
        alert(`Error: ${error.message}`);
        loadServers();
    }
}

function openAddModal() {
    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-server"></i> Add MCP Server';
    document.getElementById('server-id').value = '';
    document.getElementById('server-form').reset();
    document.getElementById('server-enabled').checked = true;
    document.getElementById('server-hidden').checked = false;
    document.getElementById('server-modal').style.display = 'flex';
}

function openEditModal(id) {
    const server = window.allServers.find(s => s.id === id);
    if (!server) return;

    document.getElementById('modal-title').innerHTML = '<i class="fa-solid fa-server"></i> Edit MCP Server';
    document.getElementById('server-id').value = server.id;
    document.getElementById('server-name').value = server.displayName;
    document.getElementById('server-type').value = server.type;
    document.getElementById('server-category').value = server.category || 'default';
    document.getElementById('server-url').value = server.url;
    document.getElementById('server-key').value = ''; // Let's keep it empty, only update if typed
    document.getElementById('server-enabled').checked = server.enabled;
    document.getElementById('server-hidden').checked = server.hidden;
    document.getElementById('server-modal').style.display = 'flex';
}

function closeModal() {
    document.getElementById('server-modal').style.display = 'none';
}

async function saveServer(event) {
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
    } else if (id) {
        // For updates, pass undefined to keep key, or null to clear it.
        // We will keep existing key if empty field.
    } else {
        server.apiKey = null;
    }

    try {
        let response;
        if (id) {
            server.id = id;
            response = await fetch(`/api/servers/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(server)
            });
        } else {
            response = await fetch('/api/servers', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(server)
            });
        }

        if (!response.ok) throw new Error('Failed to save server settings');
        closeModal();
        loadServers();
    } catch (error) {
        alert(`Error saving server: ${error.message}`);
    }
}

async function deleteServer(id, name) {
    if (!confirm(`Are you sure you want to delete the MCP server '${name}'?`)) return;
    try {
        const response = await fetch(`/api/servers/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error('Failed to delete server');
        loadServers();
    } catch (error) {
        alert(`Error deleting server: ${error.message}`);
    }
}

function openAddClientModal() {
    document.getElementById('client-form').reset();
    document.getElementById('client-form').style.display = 'block';
    document.getElementById('client-secret-result').style.display = 'none';
    document.getElementById('add-client-modal').style.display = 'flex';
}

function closeClientModal() {
    document.getElementById('add-client-modal').style.display = 'none';
}

async function handleClientSubmit(event) {
    event.preventDefault();
    const displayName = document.getElementById('client-name').value;
    const scopesInput = document.getElementById('client-scopes').value;
    
    const scopes = scopesInput
        ? scopesInput.split(',').map(s => s.trim()).filter(s => s.length > 0)
        : [];
        
    try {
        const response = await fetch('/api/clients', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ displayName, scopes })
        });
        
        if (!response.ok) {
            const errText = await response.text();
            throw new Error(errText || 'Failed to register client');
        }
        
        const result = await response.json();
        
        document.getElementById('client-form').style.display = 'none';
        document.getElementById('res-client-id').textContent = result.clientId;
        document.getElementById('res-client-secret').textContent = result.clientSecret;
        document.getElementById('client-secret-result').style.display = 'block';
        
        loadClients();
    } catch (error) {
        alert(`Error registering client: ${error.message}`);
    }
}

async function deleteClient(id, name) {
    if (!confirm(`Are you sure you want to delete the registered client '${name}'?`)) return;
    try {
        const response = await fetch(`/api/clients/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error('Failed to delete client');
        loadClients();
    } catch (error) {
        alert(`Error deleting client: ${error.message}`);
    }
}

function updateStats(servers) {
    document.getElementById('server-count').textContent = servers.length;
    document.getElementById('active-servers').textContent = servers.filter(s => s.enabled).length;
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, "&amp;")
              .replace(/</g, "&lt;")
              .replace(/>/g, "&gt;")
              .replace(/"/g, "&quot;")
              .replace(/'/g, "&#039;");
}
