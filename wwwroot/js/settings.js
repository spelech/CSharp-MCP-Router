import { apiRequest } from './api.js';

export async function initSettings() {
    setupSettingsEvents();
    await loadSettings();
    await initCustomFilesManager();
}

function setupSettingsEvents() {
    const providerSelect = document.getElementById('settings-provider');
    const localGroup = document.getElementById('settings-local-group');
    const apiGroup = document.getElementById('settings-api-group');
    const form = document.getElementById('settings-form');
    const approvalCheckbox = document.getElementById('settings-require-approval');

    providerSelect.addEventListener('change', () => {
        if (providerSelect.value === 'local') {
            localGroup.style.display = 'block';
            apiGroup.style.display = 'none';
        } else {
            localGroup.style.display = 'none';
            apiGroup.style.display = 'block';
        }
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await saveSettings();
    });

    approvalCheckbox.addEventListener('change', async () => {
        await saveSettings();
    });
}

async function loadSettings() {
    try {
        const settings = await apiRequest('/api/settings');
        if (settings) {
            document.getElementById('settings-provider').value = settings.embeddingProvider || 'local';
            document.getElementById('settings-model-dir').value = settings.embeddingModelDir || 'data/models';
            document.getElementById('settings-api-url').value = settings.embeddingApiUrl || '';
            document.getElementById('settings-api-model').value = settings.embeddingApiModel || 'all-MiniLM-L6-v2';
            document.getElementById('settings-api-key').value = settings.embeddingApiKey || '';
            document.getElementById('settings-require-approval').checked = settings.requireManualApproval || false;

            // Trigger change event to update group visibility
            document.getElementById('settings-provider').dispatchEvent(new Event('change'));
        }
    } catch (e) {
        console.error('Failed to load settings:', e);
    }
}

async function saveSettings() {
    const btn = document.getElementById('btn-save-settings');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Saving...';

    const payload = {
        embeddingProvider: document.getElementById('settings-provider').value,
        embeddingModelDir: document.getElementById('settings-model-dir').value,
        embeddingApiUrl: document.getElementById('settings-api-url').value,
        embeddingApiModel: document.getElementById('settings-api-model').value,
        embeddingApiKey: document.getElementById('settings-api-key').value,
        requireManualApproval: document.getElementById('settings-require-approval').checked
    };

    try {
        const result = await apiRequest('/api/settings', 'POST', payload);
        if (result && result.success) {
            btn.innerHTML = '<i class="fa-solid fa-check"></i> Saved!';
            btn.classList.remove('btn-primary');
            btn.style.backgroundColor = '#10b981'; // Green
            
            setTimeout(() => {
                btn.disabled = false;
                btn.innerHTML = originalText;
                btn.classList.add('btn-primary');
                btn.style.backgroundColor = '';
            }, 2000);
        } else {
            throw new Error('Save failed');
        }
    } catch (e) {
        console.error('Failed to save settings:', e);
        btn.innerHTML = '<i class="fa-solid fa-triangle-exclamation"></i> Error';
        btn.style.backgroundColor = '#ef4444'; // Red
        
        setTimeout(() => {
            btn.disabled = false;
            btn.innerHTML = originalText;
            btn.style.backgroundColor = '';
        }, 3000);
    }
}

// ==========================================
// Custom Files Manager Implementation
// ==========================================
let editingFileName = null;
let editingFileType = null;

async function initCustomFilesManager() {
    setupCustomFileEvents();
    await loadCustomFiles();
}

async function loadCustomFiles() {
    const tbody = document.getElementById('custom-files-tbody');
    if (!tbody) return;

    try {
        const files = await apiRequest('/api/custom-files');
        if (!files || files.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="empty-state" style="padding: 20px; text-align: center; color: var(--text-muted);">
                        No custom prompts or resources found. Click "Create File" to start.
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = files.map(file => {
            const formattedSize = (file.sizeBytes / 1024).toFixed(2) + ' KB';
            const dateStr = new Date(file.lastModified).toLocaleString();
            const typeLabel = file.type === 'prompts' 
                ? '<span style="color:#f59e0b;"><i class="fa-solid fa-comments"></i> Prompt</span>' 
                : '<span style="color:#10b981;"><i class="fa-solid fa-file-lines"></i> Resource</span>';

            return `
                <tr style="border-bottom: 1px solid var(--border);">
                    <td style="padding: 12px 10px;">${typeLabel}</td>
                    <td style="padding: 12px 10px; font-family: monospace; font-weight: 500;">${file.name}</td>
                    <td style="padding: 12px 10px; color: var(--text-muted);">${formattedSize}</td>
                    <td style="padding: 12px 10px; color: var(--text-muted);">${dateStr}</td>
                    <td style="padding: 12px 10px; text-align: right;">
                        <button class="btn btn-secondary btn-sm edit-file-btn" data-type="${file.type}" data-name="${file.name}" style="margin-right: 5px;">
                            <i class="fa-solid fa-edit"></i> Edit
                        </button>
                        <button class="btn btn-danger btn-sm delete-file-btn" data-type="${file.type}" data-name="${file.name}">
                            <i class="fa-solid fa-trash"></i> Delete
                        </button>
                    </td>
                </tr>
            `;
        }).join('');

        // Attach action events
        document.querySelectorAll('.edit-file-btn').forEach(btn => {
            btn.addEventListener('click', () => openEditCustomFile(btn.dataset.type, btn.dataset.name));
        });

        document.querySelectorAll('.delete-file-btn').forEach(btn => {
            btn.addEventListener('click', () => deleteCustomFile(btn.dataset.type, btn.dataset.name));
        });

    } catch (err) {
        console.error('Failed to load custom files:', err);
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="empty-state" style="padding: 20px; text-align: center; color: var(--status-offline);">
                    Error loading custom files: ${err.message}
                </td>
            </tr>`;
    }
}

function setupCustomFileEvents() {
    const createBtn = document.getElementById('btn-create-custom-file');
    const closeBtn = document.getElementById('btn-close-custom-file-modal');
    const cancelBtn = document.getElementById('btn-cancel-custom-file-modal');
    const modal = document.getElementById('custom-file-modal');
    const form = document.getElementById('custom-file-form');
    const typeSelect = document.getElementById('custom-file-type');
    const nameInput = document.getElementById('custom-file-name');
    const contentTextarea = document.getElementById('custom-file-content');

    if (!createBtn || !modal || !form) return;

    // Open in Create mode
    createBtn.addEventListener('click', () => {
        editingFileName = null;
        editingFileType = null;
        
        document.getElementById('custom-file-modal-title').innerHTML = '<i class="fa-solid fa-file-circle-plus"></i> Create Custom File';
        typeSelect.disabled = false;
        nameInput.disabled = false;
        
        typeSelect.value = 'prompts';
        nameInput.value = '';
        
        // Starter template for JSON prompt
        contentTextarea.value = JSON.stringify({
            description: "My custom prompt description",
            arguments: [
                { name: "topic", description: "Topic to write about", required: true }
            ],
            messages: [
                {
                    role: "user",
                    content: {
                        type: "text",
                        text: "Write a short summary about {{topic}}."
                    }
                }
            ]
        }, null, 2);
        
        modal.style.display = 'flex';
    });

    typeSelect.addEventListener('change', () => {
        if (editingFileName) return;
        
        if (typeSelect.value === 'prompts') {
            nameInput.placeholder = 'e.g. my-prompt.json';
            contentTextarea.value = JSON.stringify({
                description: "My custom prompt description",
                arguments: [
                    { name: "topic", description: "Topic to write about", required: true }
                ],
                messages: [
                    {
                        role: "user",
                        content: {
                            type: "text",
                            text: "Write a short summary about {{topic}}."
                        }
                    }
                ]
            }, null, 2);
        } else {
            nameInput.placeholder = 'e.g. todo.md';
            contentTextarea.value = "# Local Resource File\nEnter markdown content here.";
        }
    });

    const closeModal = () => {
        modal.style.display = 'none';
    };

    closeBtn.addEventListener('click', closeModal);
    cancelBtn.addEventListener('click', closeModal);

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const type = typeSelect.value;
        const name = nameInput.value.trim();
        const content = contentTextarea.value;

        if (!name) return;

        try {
            const targetName = editingFileName || name;
            const targetType = editingFileType || type;
            
            const result = await apiRequest(`/api/custom-files/${targetType}/${targetName}`, 'POST', { content });
            if (result && result.success) {
                closeModal();
                await loadCustomFiles();
            }
        } catch (err) {
            alert(`Failed to save file: ${err.message}`);
        }
    });
}

async function openEditCustomFile(type, name) {
    editingFileName = name;
    editingFileType = type;

    const modal = document.getElementById('custom-file-modal');
    const typeSelect = document.getElementById('custom-file-type');
    const nameInput = document.getElementById('custom-file-name');
    const contentTextarea = document.getElementById('custom-file-content');
    
    document.getElementById('custom-file-modal-title').innerHTML = `<i class="fa-solid fa-file-pen"></i> Edit Custom File: ${name}`;
    typeSelect.disabled = true;
    nameInput.disabled = true;

    typeSelect.value = type;
    nameInput.value = name;
    contentTextarea.value = 'Loading file contents...';

    modal.style.display = 'flex';

    try {
        const res = await apiRequest(`/api/custom-files/${type}/${name}`);
        if (res && res.content !== undefined) {
            contentTextarea.value = res.content;
        }
    } catch (err) {
        contentTextarea.value = `Error loading file: ${err.message}`;
    }
}

async function deleteCustomFile(type, name) {
    if (!confirm(`Are you sure you want to delete the custom file '${name}'? This action cannot be undone.`)) {
        return;
    }

    try {
        const result = await apiRequest(`/api/custom-files/${type}/${name}`, 'DELETE');
        if (result && result.success) {
            await loadCustomFiles();
        }
    } catch (err) {
        alert(`Failed to delete file: ${err.message}`);
    }
}
