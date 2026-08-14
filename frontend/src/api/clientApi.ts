import { apiRequest } from '../shared/api/api';
import { RegisteredClient, NewClientResult } from '../shared/types';

export async function fetchClientsApi(): Promise<RegisteredClient[]> {
  const data = await apiRequest<RegisteredClient[]>('/api/clients');
  return data || [];
}

export async function registerClientApi(displayName: string, scopes: string[]): Promise<NewClientResult> {
  return apiRequest<NewClientResult>('/api/clients', {
    method: 'POST',
    body: { displayName, scopes }
  });
}

export async function deleteClientApi(id: string): Promise<void> {
  await apiRequest(`/api/clients/${id}`, { method: 'DELETE' });
}
