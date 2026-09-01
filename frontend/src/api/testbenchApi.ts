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

export async function executeToolApi(serverId: string, toolName: string, args: Record<string, any>): Promise<any> {
  return await apiRequest('/api/test/call', {
    method: 'POST',
    body: {
      serverId,
      toolName,
      name: toolName,
      arguments: args
    }
  });
}

export async function getPromptApi(serverId: string, promptName: string, args: Record<string, any>): Promise<any> {
  return await apiRequest('/api/test/prompts/get', {
    method: 'POST',
    body: {
      serverId,
      promptName,
      name: promptName,
      arguments: args
    }
  });
}

export async function readResourceApi(serverId: string, uri: string): Promise<any> {
  return await apiRequest('/api/test/resources/read', {
    method: 'POST',
    body: {
      serverId,
      uri
    }
  });
}

export async function semanticSearchApi(query: string): Promise<any[]> {
  const data = await apiRequest<any[]>('/api/test/semantic-search', {
    method: 'POST',
    body: { query }
  });
  return data || [];
}
