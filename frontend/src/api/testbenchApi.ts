import { apiRequest } from '../shared/api/api';
import { ToolItem, PromptItem, ResourcesData, LogEntry } from '../shared/types';

export async function fetchTestToolsApi(): Promise<ToolItem[]> {
  const data = await apiRequest<ToolItem[]>('/api/test/tools');
  return data || [];
}

export async function fetchTestPromptsApi(): Promise<PromptItem[]> {
  const data = await apiRequest<PromptItem[]>('/api/test/prompts');
  return data || [];
}

export async function fetchTestResourcesApi(): Promise<ResourcesData> {
  const data = await apiRequest<ResourcesData>('/api/test/resources');
  return data || { resources: [], templates: [] };
}

export async function fetchLogsApi(): Promise<LogEntry[]> {
  const data = await apiRequest<LogEntry[]>('/api/logs');
  return data || [];
}

export async function clearLogsApi(): Promise<void> {
  await apiRequest('/api/logs', { method: 'DELETE' });
}
