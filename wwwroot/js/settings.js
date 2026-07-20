import { apiRequest } from './api.js';

export async function initSettings() {
    setupSettingsEvents();
    await loadSettings();
}

function setupSettingsEvents() {
    const providerSelect = document.getElementById('settings-provider');
    const localGroup = document.getElementById('settings-local-group');
    const apiGroup = document.getElementById('settings-api-group');
    const form = document.getElementById('settings-form');

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
            // Visual alert (temporary change of save button color / icon)
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
