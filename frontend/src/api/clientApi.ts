import { apiRequest } from '../shared/api/api';
import { RegisteredClient, NewClientResult } from '../shared/types';

export async function fetchClientsApi(): Promise<RegisteredClient[]> {
  const data = await apiRequest<RegisteredClient[]>('/api/clients');
  return data || [];
}

export async function registerClientApi(
  displayName: string,
  scopes: string[],
  redirectUris?: string[],
  grantTypes?: string[],
  clientType?: 'confidential' | 'public',
  expiresInDays?: number
): Promise<NewClientResult> {
  return apiRequest<NewClientResult>('/api/clients', {
    method: 'POST',
    body: {
      displayName,
      scopes,
      redirectUris,
      grantTypes,
      clientType,
      expiresInDays
    }
  });
}

export async function deleteClientApi(id: string): Promise<void> {
  await apiRequest(`/api/clients/${id}`, { method: 'DELETE' });
}
