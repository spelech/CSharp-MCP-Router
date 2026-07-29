export async function apiRequest(url, options = {}) {
    const defaultHeaders = {
        'Content-Type': 'application/json'
    };
    
    options.headers = {
        ...defaultHeaders,
        ...options.headers
    };

    if (options.body && typeof options.body === 'object') {
        options.body = JSON.stringify(options.body);
    }

    const response = await fetch(url, options);
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `Request failed with status ${response.status}`);
    }

    if (response.status === 204) return null;
    return response.json();
}

export async function getSecretProviders() {
    return await apiRequest('/api/providers/secrets');
}

export async function saveSecretProvider(providerData) {
    return await apiRequest('/api/providers/secrets', {
        method: 'POST',
        body: providerData
    });
}

export async function getAuthProviders() {
    return await apiRequest('/api/providers/auth');
}

export async function saveAuthProvider(authData) {
    return await apiRequest('/api/providers/auth', {
        method: 'POST',
        body: authData
    });
}
