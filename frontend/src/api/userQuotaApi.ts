import { apiRequest } from '../shared/api/api';
import { UserQuota } from '../shared/types';

export async function fetchUserQuotasApi(): Promise<UserQuota[]> {
  const data = await apiRequest<UserQuota[]>('/api/appkeys/quotas');
  return data || [];
}

export async function setUserQuotaApi(
  username: string,
  maxKeys: number
): Promise<{ success: boolean; username: string; maxKeys: number }> {
  return apiRequest<{ success: boolean; username: string; maxKeys: number }>('/api/appkeys/quotas', {
    method: 'POST',
    body: { username, maxKeys }
  });
}

export async function deleteUserQuotaApi(username: string): Promise<void> {
  await apiRequest(`/api/appkeys/quotas/${encodeURIComponent(username)}`, {
    method: 'DELETE'
  });
}
