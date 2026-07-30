import { apiRequest, getSecretProviders, saveSecretProvider, getAuthProviders, saveAuthProvider } from './api.js';

export async function initSettings() {
    setupSubNavEvents();
    setupSettingsEvents();
    setupProviderEvents();
    await loadSettings();
    await loadProviders();
    await initCustomFilesManager();
}

function setupSubNavEvents() {
    const subNavBtns = document.querySelectorAll('.settings-tab-btn');
    subNavBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.getAttribute('data-subview');
            document.querySelectorAll('.settings-tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.settings-subview').forEach(v => v.classList.remove('active'));

            btn.classList.add('active');
            const targetView = document.getElementById(targetId);
            if (targetView) targetView.classList.add('active');
        });
    });
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

function setupProviderEvents() {
    const authForm = document.getElementById('auth-providers-form');
    if (authForm) {
        authForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                await saveAuthProvider({
                    providerName: 'ActiveDirectory',
                    displayName: 'Active Directory',
                    isEnabled: document.getElementById('auth-ad-enabled').checked
                });
                await saveAuthProvider({
                    providerName: 'PocketID_TinyAuth',
                    displayName: 'PocketID / TinyAuth OIDC',
                    userHeader: document.getElementById('auth-user-header').value,
                    groupsHeader: document.getElementById('auth-groups-header').value,
                    isEnabled: document.getElementById('auth-oidc-enabled').checked
                });
                alert('Auth Provider configurations saved successfully!');
            } catch (err) {
                alert('Failed to save Auth Providers: ' + err.message);
            }
        });
    }

    const secretForm = document.getElementById('secret-providers-form');
    if (secretForm) {
        secretForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const vaultConfig = {
                    address: document.getElementById('secret-vault-address').value,
                    token: document.getElementById('secret-vault-token').value,
                    mountPath: document.getElementById('secret-vault-path').value
                };

                const winregConfig = {
                    keyPath: document.getElementById('secret-winreg-key').value
                };

                const envConfig = {
                    prefix: document.getElementById('secret-env-prefix').value
                };

                await saveSecretProvider({
                    providerName: 'Vault',
                    displayName: 'HashiCorp Vault (KV v2)',
                    configJson: JSON.stringify(vaultConfig),
                    isEnabled: document.getElementById('secret-vault-enabled').checked
                });
                await saveSecretProvider({
                    providerName: 'WindowsRegistry',
                    displayName: 'Windows Registry (DPAPI)',
                    configJson: JSON.stringify(winregConfig),
                    isEnabled: document.getElementById('secret-winreg-enabled').checked
                });
                await saveSecretProvider({
                    providerName: 'Environment',
                    displayName: 'Container Environment',
                    configJson: JSON.stringify(envConfig),
                    isEnabled: document.getElementById('secret-env-enabled').checked
                });
                alert('Secret Provider configurations saved successfully!');
            } catch (err) {
                alert('Failed to save Secret Providers: ' + err.message);
            }
        });
    }
}

async function loadProviders() {
    try {
        const authProviders = await getAuthProviders();
        if (Array.isArray(authProviders)) {
            const ad = authProviders.find(p => p.providerName === 'ActiveDirectory');
            if (ad) document.getElementById('auth-ad-enabled').checked = ad.isEnabled;

            const oidc = authProviders.find(p => p.providerName === 'PocketID_TinyAuth');
            if (oidc) {
                document.getElementById('auth-oidc-enabled').checked = oidc.isEnabled;
                if (oidc.userHeader) document.getElementById('auth-user-header').value = oidc.userHeader;
                if (oidc.groupsHeader) document.getElementById('auth-groups-header').value = oidc.groupsHeader;
            }
        }

        const secretProviders = await getSecretProviders();
        if (Array.isArray(secretProviders)) {
            const vault = secretProviders.find(p => p.providerName === 'Vault');
            if (vault) {
                document.getElementById('secret-vault-enabled').checked = vault.isEnabled;
                if (vault.configJson) {
                    try {
                        const cfg = JSON.parse(vault.configJson);
                        if (cfg.address) document.getElementById('secret-vault-address').value = cfg.address;
                        if (cfg.token) document.getElementById('secret-vault-token').value = cfg.token;
                        if (cfg.mountPath) document.getElementById('secret-vault-path').value = cfg.mountPath;
                    } catch (_) {}
                }
            }

            const winreg = secretProviders.find(p => p.providerName === 'WindowsRegistry');
            if (winreg) {
                document.getElementById('secret-winreg-enabled').checked = winreg.isEnabled;
                if (winreg.configJson) {
                    try {
                        const cfg = JSON.parse(winreg.configJson);
                        if (cfg.keyPath) document.getElementById('secret-winreg-key').value = cfg.keyPath;
                    } catch (_) {}
                }
            }

            const env = secretProviders.find(p => p.providerName === 'Environment');
            if (env) {
                document.getElementById('secret-env-enabled').checked = env.isEnabled;
                if (env.configJson) {
                    try {
                        const cfg = JSON.parse(env.configJson);
                        if (cfg.prefix) document.getElementById('secret-env-prefix').value = cfg.prefix;
                    } catch (_) {}
                }
            }
        }
    } catch (e) {
        console.warn('Providers not yet initialized or endpoints unavailable:', e);
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
let activeModalTab = 'editor';

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

    const tabEditor = document.getElementById('btn-custom-file-tab-editor');
    const tabBuilder = document.getElementById('btn-custom-file-tab-builder');
    const panelEditor = document.getElementById('custom-file-panel-editor');
    const panelBuilder = document.getElementById('custom-file-panel-builder');
    const tabsBar = document.getElementById('custom-file-tabs-bar');

    const addArgBtn = document.getElementById('btn-builder-add-arg');
    const addMsgBtn = document.getElementById('btn-builder-add-msg');

    if (!createBtn || !modal || !form) return;

    // Toggle Tabs
    function setModalTab(tab) {
        if (tab === 'builder') {
            // Attempt to parse JSON content to populate builder UI
            try {
                const parsed = JSON.parse(contentTextarea.value);
                document.getElementById('builder-prompt-desc').value = parsed.description || '';
                
                // Populate args
                const argsList = document.getElementById('builder-args-list');
                argsList.innerHTML = '';
                if (Array.isArray(parsed.arguments)) {
                    parsed.arguments.forEach(arg => {
                        addBuilderArgRow(arg.name, arg.description, arg.required);
                    });
                }

                // Populate messages
                const msgsList = document.getElementById('builder-msgs-list');
                msgsList.innerHTML = '';
                if (Array.isArray(parsed.messages)) {
                    parsed.messages.forEach(msg => {
                        const txt = msg.content && (msg.content.text !== undefined) ? msg.content.text : (msg.content || '');
                        addBuilderMsgRow(msg.role, txt);
                    });
                }
            } catch (err) {
                alert(`Invalid JSON format in raw code editor. Please fix syntax errors before switching to Prompt Builder. Details: ${err.message}`);
                return;
            }

            tabEditor.classList.remove('active');
            tabBuilder.classList.add('active');
            panelEditor.style.display = 'none';
            panelBuilder.style.display = 'block';
            activeModalTab = 'builder';
        } else {
            // Compile Builder values into Raw Editor JSON content
            if (activeModalTab === 'builder') {
                compileBuilderToJson();
            }
            tabBuilder.classList.remove('active');
            tabEditor.classList.add('active');
            panelBuilder.style.display = 'none';
            panelEditor.style.display = 'block';
            activeModalTab = 'editor';
        }
    }

    tabEditor.addEventListener('click', () => setModalTab('editor'));
    tabBuilder.addEventListener('click', () => setModalTab('builder'));

    addArgBtn.addEventListener('click', () => addBuilderArgRow());
    addMsgBtn.addEventListener('click', () => addBuilderMsgRow());

    // Open in Create mode
    createBtn.addEventListener('click', () => {
        editingFileName = null;
        editingFileType = null;
        activeModalTab = 'editor';
        
        document.getElementById('custom-file-modal-title').innerHTML = '<i class="fa-solid fa-file-circle-plus"></i> Create Custom File';
        typeSelect.disabled = false;
        nameInput.disabled = false;
        
        typeSelect.value = 'prompts';
        nameInput.value = '';
        tabsBar.style.display = 'flex';
        setModalTab('editor');
        
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
            tabsBar.style.display = 'flex';
            setModalTab('editor');
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
            tabsBar.style.display = 'none';
            setModalTab('editor');
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
        
        if (activeModalTab === 'builder') {
            compileBuilderToJson();
        }

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

function addBuilderArgRow(name = '', description = '', required = false) {
    const list = document.getElementById('builder-args-list');
    if (!list) return;

    const row = document.createElement('div');
    row.className = 'form-row builder-arg-row';
    row.style.alignItems = 'center';
    row.style.gap = '8px';
    row.style.marginBottom = '6px';
    
    row.innerHTML = `
        <input type="text" placeholder="Arg Name" class="arg-name" value="${escapeHtmlForBuilder(name)}" style="flex: 2; height: 32px; font-size: 13px;" required>
        <input type="text" placeholder="Description" class="arg-desc" value="${escapeHtmlForBuilder(description)}" style="flex: 3; height: 32px; font-size: 13px;">
        <label style="display: flex; align-items: center; gap: 4px; font-size: 12px; cursor: pointer; white-space: nowrap; margin-bottom: 0;">
            <input type="checkbox" class="arg-req" ${required ? 'checked' : ''}> Req
        </label>
        <button type="button" class="btn btn-danger btn-sm btn-remove-row" style="padding: 4px 8px; height: 32px;"><i class="fa-solid fa-trash"></i></button>
    `;

    row.querySelector('.btn-remove-row').addEventListener('click', () => row.remove());
    list.appendChild(row);
}

function addBuilderMsgRow(role = 'user', text = '') {
    const list = document.getElementById('builder-msgs-list');
    if (!list) return;

    const row = document.createElement('div');
    row.className = 'builder-msg-row';
    row.style.display = 'flex';
    row.style.flexDirection = 'column';
    row.style.gap = '6px';
    row.style.border = '1px solid var(--border)';
    row.style.padding = '8px';
    row.style.borderRadius = '6px';
    row.style.background = 'rgba(255,255,255,0.02)';
    row.style.marginBottom = '8px';

    row.innerHTML = `
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <select class="msg-role" style="width: 120px; height: 28px; font-size: 12px; padding: 2px;">
                <option value="user" ${role === 'user' ? 'selected' : ''}>User</option>
                <option value="assistant" ${role === 'assistant' ? 'selected' : ''}>Assistant</option>
            </select>
            <button type="button" class="btn btn-danger btn-sm btn-remove-row" style="padding: 2px 6px; font-size: 11px;"><i class="fa-solid fa-trash"></i> Delete</button>
        </div>
        <textarea placeholder="Message content..." class="msg-text" rows="3" style="width: 100%; font-size: 12px; padding: 6px; background: rgba(0,0,0,0.2); border: 1px solid var(--border); color: #fff; border-radius: 4px; resize: vertical;" required>${escapeHtmlForBuilder(text)}</textarea>
    `;

    row.querySelector('.btn-remove-row').addEventListener('click', () => row.remove());
    list.appendChild(row);
}

function compileBuilderToJson() {
    const desc = document.getElementById('builder-prompt-desc').value;
    const argRows = document.querySelectorAll('.builder-arg-row');
    const msgRows = document.querySelectorAll('.builder-msg-row');

    const promptObj = {
        description: desc,
        arguments: [],
        messages: []
    };

    argRows.forEach(row => {
        const name = row.querySelector('.arg-name').value.trim();
        const description = row.querySelector('.arg-desc').value.trim();
        const required = row.querySelector('.arg-req').checked;

        if (name) {
            promptObj.arguments.push({ name, description, required });
        }
    });

    msgRows.forEach(row => {
        const role = row.querySelector('.msg-role').value;
        const text = row.querySelector('.msg-text').value;

        promptObj.messages.push({
            role: role,
            content: {
                type: "text",
                text: text
            }
        });
    });

    document.getElementById('custom-file-content').value = JSON.stringify(promptObj, null, 2);
}

function escapeHtmlForBuilder(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

async function openEditCustomFile(type, name) {
    editingFileName = name;
    editingFileType = type;
    activeModalTab = 'editor';

    const modal = document.getElementById('custom-file-modal');
    const typeSelect = document.getElementById('custom-file-type');
    const nameInput = document.getElementById('custom-file-name');
    const contentTextarea = document.getElementById('custom-file-content');
    const tabsBar = document.getElementById('custom-file-tabs-bar');
    
    document.getElementById('custom-file-modal-title').innerHTML = `<i class="fa-solid fa-file-pen"></i> Edit Custom File: ${name}`;
    typeSelect.disabled = true;
    nameInput.disabled = true;

    typeSelect.value = type;
    nameInput.value = name;
    contentTextarea.value = 'Loading file contents...';

    if (type === 'prompts') {
        tabsBar.style.display = 'flex';
    } else {
        tabsBar.style.display = 'none';
    }

    // Always reset tab classes on load
    document.getElementById('btn-custom-file-tab-builder').classList.remove('active');
    document.getElementById('btn-custom-file-tab-editor').classList.add('active');
    document.getElementById('custom-file-panel-builder').style.display = 'none';
    document.getElementById('custom-file-panel-editor').style.display = 'block';

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
