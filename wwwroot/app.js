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
    const list = document.getElementById('servers-list');
    if (servers.length === 0) {
        list.innerHTML = '<div class="empty-state">No backend servers configured.</div>';
        return;
    }
    
    list.innerHTML = servers.map(server => {
        // Exclude internal-only helpers or display them nicely
        const nameClass = server.enabled ? 'server-name' : 'server-name text-muted';
        return `
            <div class="server-item">
                <div class="server-info">
                    <div class="server-name-row">
                        <span class="${nameClass}">${escapeHtml(server.displayName)}</span>
                        <span class="server-badge">${escapeHtml(server.type.toUpperCase())}</span>
                        ${server.hasApiKey ? '<span class="server-badge badge-key"><i class="fa-solid fa-lock"></i> Secured</span>' : ''}
                        ${server.hidden ? '<span class="server-badge"><i class="fa-solid fa-eye-slash"></i> Hidden</span>' : ''}
                    </div>
                    <span class="server-url">${escapeHtml(server.url)}</span>
                </div>
                <div class="server-actions">
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
                <td colspan="3" class="empty-state">No dynamic clients registered yet. Connect Gemini Spark to register.</td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = clients.map(client => {
        const date = new Date(client.createdAt).toLocaleDateString(undefined, {
            year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        });
        return `
            <tr>
                <td><strong>${escapeHtml(client.clientName)}</strong></td>
                <td><code style="font-family: 'JetBrains Mono', monospace; font-size:11px; background: rgba(255,255,255,0.05); padding:2px 6px; border-radius:4px; color: var(--accent);">${escapeHtml(client.clientId)}</code></td>
                <td>${date}</td>
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
        
        // Refresh stats/servers list
        loadServers();
    } catch (error) {
        alert(`Error: ${error.message}`);
        loadServers(); // Revert toggle visually
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
