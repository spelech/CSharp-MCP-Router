import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

let promptsList = [];
let resourcesData = { resources: [], templates: [] };

export async function initPromptsAndResourcesTester() {
    setupTabNavigation();
    setupPromptsEvents();
    setupResourcesEvents();
    await Promise.all([loadPrompts(), loadResources()]);
}

function setupTabNavigation() {
    const navButtons = document.querySelectorAll('.tb-nav-btn');
    const panels = document.querySelectorAll('.tb-view-panel');
    
    navButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            navButtons.forEach(b => b.classList.remove('active'));
            panels.forEach(p => {
                p.classList.remove('active');
                p.style.display = 'none';
            });
            
            btn.classList.add('active');
            const target = btn.dataset.tbView;
            const targetPanel = document.getElementById(target);
            if (targetPanel) {
                targetPanel.classList.add('active');
                targetPanel.style.display = 'block';
            }
        });
    });
}

// ==========================================
// Prompts Tab Implementation
// ==========================================
async function loadPrompts() {
    try {
        promptsList = await apiRequest('/api/test/prompts');
        const serverSelect = document.getElementById('tester-prompt-server');
        if (!serverSelect) return;

        serverSelect.innerHTML = '<option value="">-- Choose Server --</option>';
        
        const servers = new Set();
        promptsList.forEach(p => {
            const parts = p.name.split('__');
            if (parts.length > 1 && parts[0].startsWith('router')) {
                servers.add('router');
            } else if (parts.length > 1) {
                servers.add(parts[0]);
            } else {
                servers.add('router');
            }
        });

        Array.from(servers).sort().forEach(s => {
            const opt = document.createElement('option');
            opt.value = s;
            opt.textContent = s === 'router' ? 'Built-in Meta Workflows (router)' : `${s.toUpperCase()} Server`;
            serverSelect.appendChild(opt);
        });
    } catch (e) {
        console.error('Failed to load test prompts:', e);
    }
}

function setupPromptsEvents() {
    const serverSelect = document.getElementById('tester-prompt-server');
    const promptSelect = document.getElementById('tester-prompt-name');
    const form = document.getElementById('prompt-tester-form');

    if (!serverSelect || !promptSelect || !form) return;

    serverSelect.addEventListener('change', () => {
        const server = serverSelect.value;
        promptSelect.innerHTML = '<option value="">-- Choose Prompt --</option>';
        document.getElementById('prompt-dynamic-fields').innerHTML = '<div class="empty-state">Select a prompt to generate arguments.</div>';
        
        if (!server) return;

        const filtered = promptsList.filter(p => {
            if (server === 'router') {
                return !p.name.includes('__') || p.name.startsWith('router__');
            }
            return p.name.startsWith(server + '__');
        });

        filtered.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.name;
            const cleanName = p.name.includes('__') ? p.name.split('__')[1] : p.name;
            opt.textContent = cleanName;
            promptSelect.appendChild(opt);
        });
    });

    promptSelect.addEventListener('change', () => {
        const promptName = promptSelect.value;
        const container = document.getElementById('prompt-dynamic-fields');
        container.innerHTML = '';

        if (!promptName) {
            container.innerHTML = '<div class="empty-state">Select a prompt to generate arguments.</div>';
            return;
        }

        const prompt = promptsList.find(p => p.name === promptName);
        if (!prompt || !prompt.arguments || prompt.arguments.length === 0) {
            container.innerHTML = '<div class="empty-state">This prompt takes no arguments.</div>';
            return;
        }

        prompt.arguments.forEach(arg => {
            const fieldDiv = document.createElement('div');
            fieldDiv.className = 'param-field';
            
            const reqText = arg.required ? ' <span style="color: var(--status-offline)">*</span>' : '';
            const label = document.createElement('label');
            label.innerHTML = `${escapeHtml(arg.name)}${reqText}`;
            fieldDiv.appendChild(label);

            const input = document.createElement('input');
            input.type = 'text';
            input.dataset.argName = arg.name;
            input.required = arg.required || false;
            fieldDiv.appendChild(input);

            if (arg.description) {
                const desc = document.createElement('div');
                desc.className = 'field-desc';
                desc.textContent = arg.description;
                fieldDiv.appendChild(desc);
            }

            container.appendChild(fieldDiv);
        });
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await runPromptGet();
    });
}

async function runPromptGet() {
    const serverSelect = document.getElementById('tester-prompt-server').value;
    const promptName = document.getElementById('tester-prompt-name').value;
    if (!serverSelect || !promptName) return;

    const requestBlock = document.getElementById('jsonrpc-request');
    const responseBlock = document.getElementById('jsonrpc-response');
    
    requestBlock.textContent = 'Formatting request...';
    responseBlock.textContent = 'Executing...';

    // Collect arguments
    const args = {};
    document.querySelectorAll('#prompt-dynamic-fields [data-arg-name]').forEach(input => {
        if (input.value !== '') {
            args[input.dataset.argName] = input.value;
        }
    });

    // Display simulated client request
    const clientRequestPayload = {
        jsonrpc: '2.0',
        id: 'client-call-id',
        method: 'prompts/get',
        params: {
            name: promptName,
            arguments: args
        }
    };
    requestBlock.textContent = JSON.stringify(clientRequestPayload, null, 2);

    // Format clean prompt name for downstream execution
    const cleanPromptName = promptName.includes('__') ? promptName.split('__')[1] : promptName;

    try {
        const result = await apiRequest('/api/test/prompts/get', {
            method: 'POST',
            body: {
                serverId: serverSelect,
                promptName: cleanPromptName,
                arguments: args
            }
        });
        responseBlock.textContent = JSON.stringify(result, null, 2);
    } catch (err) {
        responseBlock.textContent = `Prompt execution failed:\n${err.message}`;
    }
}

// ==========================================
// Resources Tab Implementation
// ==========================================
async function loadResources() {
    try {
        resourcesData = await apiRequest('/api/test/resources');
        const serverSelect = document.getElementById('tester-resource-server');
        if (!serverSelect) return;

        serverSelect.innerHTML = '<option value="">-- Choose Server --</option>';

        const servers = new Set();
        // Add local virtual logs server/router
        servers.add('router');

        if (resourcesData.resources) {
            resourcesData.resources.forEach(r => {
                if (r.uri) {
                    const parsed = parseUriServer(r.uri);
                    if (parsed) servers.add(parsed);
                }
            });
        }

        Array.from(servers).sort().forEach(s => {
            const opt = document.createElement('option');
            opt.value = s;
            opt.textContent = s === 'router' ? 'Built-in Logs & Router state' : `${s.toUpperCase()} Server`;
            serverSelect.appendChild(opt);
        });
    } catch (e) {
        console.error('Failed to load test resources:', e);
    }
}

function parseUriServer(uri) {
    if (uri.startsWith('router://') || uri.startsWith('logs://')) return 'router';
    try {
        const parsed = new URL(uri);
        if (parsed.protocol === 'mcp:') return parsed.hostname;
    } catch {}
    return null;
}

function setupResourcesEvents() {
    const serverSelect = document.getElementById('tester-resource-server');
    const resourceSelect = document.getElementById('tester-resource-name');
    const uriInput = document.getElementById('tester-resource-uri');
    const form = document.getElementById('resource-tester-form');

    if (!serverSelect || !resourceSelect || !uriInput || !form) return;

    serverSelect.addEventListener('change', () => {
        const server = serverSelect.value;
        resourceSelect.innerHTML = '<option value="">-- Choose Resource / Template --</option>';
        uriInput.value = '';

        if (!server) return;

        // Add built-in status and metrics resources for 'router'
        if (server === 'router') {
            const statusOpt = document.createElement('option');
            statusOpt.value = 'router://status';
            statusOpt.dataset.type = 'resource';
            statusOpt.textContent = 'Router Status (router://status)';
            resourceSelect.appendChild(statusOpt);

            const metricsOpt = document.createElement('option');
            metricsOpt.value = 'router://metrics';
            metricsOpt.dataset.type = 'resource';
            metricsOpt.textContent = 'Router Performance Metrics (router://metrics)';
            resourceSelect.appendChild(metricsOpt);

            // Add logs templates
            if (resourcesData.templates) {
                resourcesData.templates.forEach(t => {
                    const opt = document.createElement('option');
                    opt.value = t.uriTemplate;
                    opt.dataset.type = 'template';
                    opt.textContent = `[Template] ${t.name} (${t.uriTemplate})`;
                    resourceSelect.appendChild(opt);
                });
            }
        } else {
            // Filter dynamic server resources
            if (resourcesData.resources) {
                resourcesData.resources.forEach(r => {
                    const s = parseUriServer(r.uri);
                    if (s === server) {
                        const opt = document.createElement('option');
                        opt.value = r.uri;
                        opt.dataset.type = 'resource';
                        opt.textContent = `${r.name} (${r.uri})`;
                        resourceSelect.appendChild(opt);
                    }
                });
            }
        }
    });

    resourceSelect.addEventListener('change', () => {
        const selectedValue = resourceSelect.value;
        const selectedOpt = resourceSelect.options[resourceSelect.selectedIndex];
        
        if (!selectedValue) {
            uriInput.value = '';
            return;
        }

        const isTemplate = selectedOpt.dataset.type === 'template';
        if (isTemplate) {
            // Provide example with default parameter
            uriInput.value = selectedValue.replace('{server_name}', 'mcp-router');
        } else {
            uriInput.value = selectedValue;
        }
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await runResourceRead();
    });
}

async function runResourceRead() {
    const uri = document.getElementById('tester-resource-uri').value;
    if (!uri) return;

    const requestBlock = document.getElementById('jsonrpc-request');
    const responseBlock = document.getElementById('jsonrpc-response');
    
    requestBlock.textContent = 'Formatting request...';
    responseBlock.textContent = 'Executing...';

    // Display simulated client request
    const clientRequestPayload = {
        jsonrpc: '2.0',
        id: 'client-call-id',
        method: 'resources/read',
        params: {
            uri: uri
        }
    };
    requestBlock.textContent = JSON.stringify(clientRequestPayload, null, 2);

    try {
        const result = await apiRequest('/api/test/resources/read', {
            method: 'POST',
            body: { uri }
        });
        responseBlock.textContent = JSON.stringify(result, null, 2);
    } catch (err) {
        responseBlock.textContent = `Resource read failed:\n${err.message}`;
    }
}
