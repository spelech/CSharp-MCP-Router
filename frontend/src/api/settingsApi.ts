import { apiRequest } from '../shared/api/api';
import {
  EmbeddingSettings,
  AuthProviderConfig,
  SecretProviderConfig,
  CustomFileMeta,
  PendingApproval
} from '../shared/types';

export async function fetchEmbeddingSettingsApi(): Promise<EmbeddingSettings | null> {
  const settings = await apiRequest<any>('/api/settings');
  if (!settings) return null;
  return {
    embeddingProvider: settings.embeddingProvider || 'local',
    embeddingModelDir: settings.embeddingModelDir || 'data/models',
    embeddingApiUrl: settings.embeddingApiUrl || '',
    embeddingApiModel: settings.embeddingApiModel || 'all-MiniLM-L6-v2',
    embeddingApiKey: settings.embeddingApiKey || '',
    requireManualApproval: settings.requireManualApproval || false,
  };
}

export async function saveEmbeddingSettingsApi(settings: EmbeddingSettings): Promise<boolean> {
  const payload = {
    embeddingProvider: settings.embeddingProvider,
    embeddingModelDir: settings.embeddingModelDir,
    embeddingApiUrl: settings.embeddingApiUrl,
    embeddingApiModel: settings.embeddingApiModel,
    embeddingApiKey: settings.embeddingApiKey,
    requireManualApproval: settings.requireManualApproval
  };
  const result = await apiRequest<{ success: boolean }>('/api/settings', {
    method: 'POST',
    body: payload
  });
  return !!(result && result.success);
}

export async function fetchAuthProvidersApi(): Promise<AuthProviderConfig[]> {
  const data = await apiRequest<AuthProviderConfig[]>('/api/providers/auth');
  return data || [];
}

export async function saveAuthProviderApi(provider: AuthProviderConfig): Promise<void> {
  await apiRequest('/api/providers/auth', {
    method: 'POST',
    body: provider
  });
}

export async function fetchSecretProvidersApi(): Promise<SecretProviderConfig[]> {
  const data = await apiRequest<SecretProviderConfig[]>('/api/providers/secrets');
  return data || [];
}

export async function saveSecretProviderApi(provider: SecretProviderConfig): Promise<void> {
  await apiRequest('/api/providers/secrets', {
    method: 'POST',
    body: provider
  });
}

export async function fetchCustomFilesApi(): Promise<CustomFileMeta[]> {
  const files = await apiRequest<CustomFileMeta[]>('/api/custom-files');
  return files || [];
}

export async function fetchCustomFileContentApi(type: 'prompts' | 'resources', name: string): Promise<string> {
  const res = await apiRequest<{ content: string }>(`/api/custom-files/${type}/${name}`);
  return res?.content || '';
}

export async function saveCustomFileApi(type: 'prompts' | 'resources', name: string, content: string): Promise<boolean> {
  const result = await apiRequest<{ success: boolean }>(`/api/custom-files/${type}/${name}`, {
    method: 'POST',
    body: { content }
  });
  return !!(result && result.success);
}

export async function deleteCustomFileApi(type: 'prompts' | 'resources', name: string): Promise<boolean> {
  const result = await apiRequest<{ success: boolean }>(`/api/custom-files/${type}/${name}`, {
    method: 'DELETE'
  });
  return !!(result && result.success);
}

export async function fetchPendingApprovalsApi(): Promise<PendingApproval[]> {
  const data = await apiRequest<PendingApproval[]>('/api/approvals');
  return data || [];
}

export async function actionApprovalApi(id: string, approved: boolean): Promise<void> {
  await apiRequest(`/api/approvals/${id}/action`, {
    method: 'POST',
    body: { approved }
  });
}
