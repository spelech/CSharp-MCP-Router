import { apiRequest } from '../shared/api/api';
import { AccessPolicy, GroupMapping } from '../shared/types';

export async function fetchPoliciesApi(): Promise<AccessPolicy[]> {
  const data = await apiRequest<AccessPolicy[]>('/api/permissions/policies');
  return data || [];
}

export async function savePolicyApi(policy: AccessPolicy): Promise<void> {
  await apiRequest('/api/permissions/policies', {
    method: 'POST',
    body: policy
  });
}

export async function deletePolicyApi(id: string): Promise<void> {
  await apiRequest(`/api/permissions/policies/${id}`, { method: 'DELETE' });
}

export async function fetchMappingsApi(): Promise<GroupMapping[]> {
  const data = await apiRequest<GroupMapping[]>('/api/permissions/mappings');
  return data || [];
}

export async function saveMappingApi(mapping: GroupMapping): Promise<void> {
  await apiRequest('/api/permissions/mappings', {
    method: 'POST',
    body: mapping
  });
}

export async function deleteMappingApi(id: string): Promise<void> {
  await apiRequest(`/api/permissions/mappings/${id}`, { method: 'DELETE' });
}
