import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

export async function loadClients() {
    try {
        const clients = await apiRequest('/api/clients');
        renderClients(clients);
    } catch (error) {
        console.error('Error loading clients:', error);
    }
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
                    <button class="btn-icon btn-delete" title="Delete Client" onclick="window.deleteClient('${client.id}', '${escapeHtml(client.displayName)}')">
                        <i class="fa-solid fa-trash-can"></i>
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

export function openAddClientModal() {
    document.getElementById('client-form').reset();
    document.getElementById('client-form').style.display = 'block';
    document.getElementById('client-secret-result').style.display = 'none';
    document.getElementById('add-client-modal').style.display = 'flex';
}

export function closeClientModal() {
    document.getElementById('add-client-modal').style.display = 'none';
}

export async function handleClientSubmit(event) {
    event.preventDefault();
    const displayName = document.getElementById('client-name').value;
    const scopesInput = document.getElementById('client-scopes').value;
    
    const scopes = scopesInput
        ? scopesInput.split(',').map(s => s.trim()).filter(s => s.length > 0)
        : [];
        
    try {
        const result = await apiRequest('/api/clients', {
            method: 'POST',
            body: { displayName, scopes }
        });
        
        document.getElementById('client-form').style.display = 'none';
        document.getElementById('res-client-id').textContent = result.clientId;
        document.getElementById('res-client-secret').textContent = result.clientSecret;
        document.getElementById('client-secret-result').style.display = 'block';
        
        await loadClients();
    } catch (error) {
        alert(`Error registering client: ${error.message}`);
    }
}

export async function deleteClient(id, name) {
    if (!confirm(`Are you sure you want to delete the registered client '${name}'?`)) return;
    try {
        await apiRequest(`/api/clients/${id}`, {
            method: 'DELETE'
        });
        await loadClients();
    } catch (error) {
        alert(`Error deleting client: ${error.message}`);
    }
}
