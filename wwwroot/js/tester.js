import { apiRequest } from './api.js';
import { escapeHtml } from './utils.js';

let toolsList = [];
let currentTool = null;

export async function initTester() {
    setupTesterEvents();
    await loadTestTools();
}

async function loadTestTools() {
    try {
        const container = document.getElementById('tester-server');
        container.innerHTML = '<option value="">-- Choose Server --</option>';
        
        toolsList = await apiRequest('/api/test/tools');
        
        // Extract unique server names based on tool prefixes (or category classifications)
        const servers = new Set();
        // C# native tools are prefix-less or categorized
        servers.add('custom'); // C# native tools registry
        
        toolsList.forEach(t => {
            const parts = t.name.split('__');
            if (parts.length > 1) {
                servers.add(parts[0]);
            } else {
                servers.add('custom');
            }
        });

        Array.from(servers).sort().forEach(s => {
            const opt = document.createElement('option');
            opt.value = s;
            opt.textContent = s === 'custom' ? 'Native C# Registry (custom)' : `${s.toUpperCase()} Server`;
            container.appendChild(opt);
        });

    } catch (e) {
        console.error('Failed to load test tools list:', e);
    }
}

function setupTesterEvents() {
    const serverSelect = document.getElementById('tester-server');
    const toolSelect = document.getElementById('tester-tool');
    const searchBtn = document.getElementById('btn-semantic-search');
    
    serverSelect.addEventListener('change', () => {
        const server = serverSelect.value;
        toolSelect.innerHTML = '<option value="">-- Choose Tool --</option>';
        document.getElementById('dynamic-form-fields').innerHTML = '';
        currentTool = null;

        if (!server) return;

        const filtered = toolsList.filter(t => {
            if (server === 'custom') {
                return !t.name.includes('__');
            }
            return t.name.startsWith(server + '__');
        });

        filtered.sort((a,b) => a.name.localeCompare(b.name)).forEach(t => {
            const opt = document.createElement('option');
            opt.value = t.name;
            // Display clean name for SSE proxied tools
            const cleanName = t.name.includes('__') ? t.name.split('__')[1] : t.name;
            opt.textContent = cleanName;
            toolSelect.appendChild(opt);
        });
    });

    toolSelect.addEventListener('change', () => {
        const toolName = toolSelect.value;
        const fieldsContainer = document.getElementById('dynamic-form-fields');
        fieldsContainer.innerHTML = '';
        currentTool = null;

        if (!toolName) return;

        currentTool = toolsList.find(t => t.name === toolName);
        if (!currentTool) return;

        buildFormForSchema(currentTool.inputSchema, fieldsContainer);
        updateRawJsonTextarea();
    });

    // Form submission
    const form = document.getElementById('tester-form');
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await runToolCall();
    });

    // Semantic Search button
    searchBtn.addEventListener('click', async () => {
        const query = document.getElementById('semantic-search-query').value;
        if (!query) return;

        const resultsContainer = document.getElementById('semantic-search-results');
        resultsContainer.innerHTML = '<div class="loading-state"><i class="fa-solid fa-spinner fa-spin"></i> Searching...</div>';

        try {
            const results = await apiRequest('/api/test/semantic-search', {
                method: 'POST',
                body: { query }
            });

            if (results.length === 0) {
                resultsContainer.innerHTML = '<div class="empty-state">No matching tools found.</div>';
                return;
            }

            resultsContainer.innerHTML = results.map((tool, idx) => {
                const name = tool.name || 'unknown';
                const desc = tool.description || 'No description provided.';
                // Scoring is calculated by SemanticSearchService; higher means better match
                const score = 10 - idx; // Mock rank indicator if score is not returned inside the object directly
                return `
                    <div class="search-result-item">
                        <div class="search-result-header">
                            <span class="search-result-name">${escapeHtml(name)}</span>
                            <span class="search-result-score">Rank #${idx + 1}</span>
                        </div>
                        <span class="search-result-desc">${escapeHtml(desc)}</span>
                    </div>
                `;
            }).join('');

        } catch (err) {
            resultsContainer.innerHTML = `<div class="empty-state" style="color:var(--status-offline)">Search error: ${err.message}</div>`;
        }
    });
}

function buildFormForSchema(schema, container) {
    if (!schema || !schema.properties) {
        container.innerHTML = '<div class="empty-state">This tool takes no arguments.</div>';
        return;
    }

    const properties = schema.properties;
    const required = schema.required || [];

    for (const [key, prop] of Object.entries(properties)) {
        const fieldDiv = document.createElement('div');
        fieldDiv.className = 'param-field';

        const isRequired = required.includes(key);
        const reqAsterisk = isRequired ? ' <span style="color: var(--status-offline)">*</span>' : '';

        // Label
        const label = document.createElement('label');
        label.innerHTML = `${escapeHtml(key)}${reqAsterisk}`;
        fieldDiv.appendChild(label);

        // Inputs by Type
        let input;
        if (prop.type === 'boolean') {
            fieldDiv.className = 'param-field checkbox-field';
            
            const checkboxLabel = document.createElement('label');
            checkboxLabel.className = 'switch';
            
            input = document.createElement('input');
            input.type = 'checkbox';
            input.dataset.key = key;
            input.dataset.type = 'boolean';
            
            const slider = document.createElement('span');
            slider.className = 'slider';
            
            checkboxLabel.appendChild(input);
            checkboxLabel.appendChild(slider);
            
            fieldDiv.insertBefore(checkboxLabel, label); // Checkbox before text label
        } else if (prop.type === 'integer' || prop.type === 'number') {
            input = document.createElement('input');
            input.type = 'number';
            if (prop.type === 'integer') input.step = '1';
            input.dataset.key = key;
            input.dataset.type = 'number';
            input.required = isRequired;
            fieldDiv.appendChild(input);
        } else if (prop.type === 'array' || prop.type === 'object') {
            input = document.createElement('textarea');
            input.rows = 2;
            input.placeholder = prop.type === 'array' ? '["item1", "item2"]' : '{"key": "value"}';
            input.dataset.key = key;
            input.dataset.type = prop.type;
            input.required = isRequired;
            fieldDiv.appendChild(input);
        } else { // default to string/text
            input = document.createElement('input');
            input.type = 'text';
            input.dataset.key = key;
            input.dataset.type = 'string';
            input.required = isRequired;
            fieldDiv.appendChild(input);
        }

        // Add change handlers to synchronize form fields with raw JSON tab
        if (input) {
            input.addEventListener('input', updateRawJsonTextarea);
            input.addEventListener('change', updateRawJsonTextarea);
        }

        // Description text
        if (prop.description) {
            const desc = document.createElement('div');
            desc.className = 'field-desc';
            desc.textContent = prop.description;
            fieldDiv.appendChild(desc);
        }

        container.appendChild(fieldDiv);
    }
}

function updateRawJsonTextarea() {
    const rawTextarea = document.getElementById('tester-raw-json');
    const args = getArgumentsFromForm();
    rawTextarea.value = JSON.stringify(args, null, 2);
}

function getArgumentsFromForm() {
    const args = {};
    const inputs = document.querySelectorAll('#dynamic-form-fields [data-key]');
    
    inputs.forEach(input => {
        const key = input.dataset.key;
        const type = input.dataset.type;

        if (type === 'boolean') {
            args[key] = input.checked;
        } else if (type === 'number') {
            if (input.value !== '') {
                args[key] = Number(input.value);
            }
        } else if (type === 'array' || type === 'object') {
            if (input.value.trim() !== '') {
                try {
                    args[key] = JSON.parse(input.value);
                } catch {
                    args[key] = input.value; // Fallback to raw string if invalid JSON
                }
            }
        } else {
            if (input.value !== '') {
                args[key] = input.value;
            }
        }
    });

    return args;
}

async function runToolCall() {
    const serverId = document.getElementById('tester-server').value;
    const toolName = document.getElementById('tester-tool').value;
    if (!serverId || !toolName) return;

    const requestBlock = document.getElementById('jsonrpc-request');
    const responseBlock = document.getElementById('jsonrpc-response');
    
    requestBlock.textContent = 'Formatting request...';
    responseBlock.textContent = 'Executing...';

    // Get parameters depending on active tab
    let argumentsPayload = {};
    const formTabActive = document.querySelector('[data-tab="form"]').classList.contains('active');
    
    if (formTabActive) {
        argumentsPayload = getArgumentsFromForm();
    } else {
        const rawJsonText = document.getElementById('tester-raw-json').value;
        try {
            argumentsPayload = rawJsonText ? JSON.parse(rawJsonText) : {};
        } catch (e) {
            responseBlock.textContent = `Error parsing raw JSON inputs:\n${e.message}`;
            return;
        }
    }

    // Format target name (the server expects raw toolName or prefixed toolName based on proxy settings)
    const cleanToolName = toolName.includes('__') ? toolName.split('__')[1] : toolName;

    // Display simulated client request
    const clientRequestPayload = {
        jsonrpc: '2.0',
        id: 'client-call-id',
        method: 'tools/call',
        params: {
            name: toolName, // The router handles routing via this name
            arguments: argumentsPayload
        }
    };
    requestBlock.textContent = JSON.stringify(clientRequestPayload, null, 2);

    try {
        const result = await apiRequest('/api/test/call', {
            method: 'POST',
            body: {
                serverId,
                toolName: cleanToolName, // Call target server directly with clean name
                arguments: argumentsPayload
            }
        });

        responseBlock.textContent = JSON.stringify(result, null, 2);
    } catch (err) {
        responseBlock.textContent = `Call failed:\n${err.message}`;
    }
}
