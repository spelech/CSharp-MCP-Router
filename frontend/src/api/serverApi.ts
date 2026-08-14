import { apiRequest } from '../shared/api/api';
import { McpServer, InspectCapabilityData, ServerPayload } from '../shared/types';

export async function fetchServersApi(): Promise<McpServer[]> {
  const data = await apiRequest<McpServer[]>('/api/servers');
  return data || [];
}

export async function reconnectAllServersApi(): Promise<void> {
  await apiRequest('/api/servers/reconnect-all', { method: 'POST' });
}

export async function toggleServerEnabledApi(id: string, enabled: boolean): Promise<void> {
  await apiRequest(`/api/servers/${id}`, {
    method: 'PUT',
    body: { enabled }
  });
}

export async function reconnectServerApi(id: string): Promise<void> {
  await apiRequest(`/api/servers/${id}/reconnect`, { method: 'POST' });
}

export async function createServerApi(serverData: ServerPayload | Partial<McpServer>): Promise<void> {
  await apiRequest('/api/servers', {
    method: 'POST',
    body: serverData
  });
}

export async function updateServerApi(id: string, serverData: ServerPayload | Partial<McpServer>): Promise<void> {
  await apiRequest(`/api/servers/${id}`, {
    method: 'PUT',
    body: serverData
  });
}

export async function deleteServerApi(id: string): Promise<void> {
  await apiRequest(`/api/servers/${id}`, { method: 'DELETE' });
}

export async function inspectServerApi(id: string): Promise<InspectCapabilityData> {
  const data = await apiRequest<InspectCapabilityData>(`/api/servers/${id}/inspect`);
  return data || { tools: [], resources: [], prompts: [] };
}
