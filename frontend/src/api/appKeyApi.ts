import { apiRequest } from '../shared/api/api';
import { AppKeyItem, AppKeyLimits, NewAppKeyResult, CreateAppKeyPayload } from '../shared/types';

export async function fetchAppKeysApi(): Promise<AppKeyItem[]> {
  const data = await apiRequest<AppKeyItem[]>('/api/appkeys');
  return data || [];
}

export async function fetchAppKeyLimitsApi(): Promise<AppKeyLimits> {
  return apiRequest<AppKeyLimits>('/api/appkeys/limits');
}

export async function createAppKeyApi(payload: CreateAppKeyPayload): Promise<NewAppKeyResult> {
  return apiRequest<NewAppKeyResult>('/api/appkeys', {
    method: 'POST',
    body: payload
  });
}

export async function revokeAppKeyApi(id: string): Promise<void> {
  await apiRequest(`/api/appkeys/${id}`, { method: 'DELETE' });
}
