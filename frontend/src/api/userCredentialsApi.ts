import { apiRequest } from '../shared/api/api';

export interface UserCredential {
  serverId: string;
}

export async function fetchUserCredentialsApi(): Promise<UserCredential[]> {
  const data = await apiRequest<UserCredential[]>('/api/user/credentials');
  return data || [];
}

export async function saveUserCredentialApi(serverId: string, secretJson: string): Promise<void> {
  await apiRequest(`/api/user/credentials/${serverId}`, {
    method: 'POST',
    body: secretJson, // we want to send raw JSON string, or a JS object.
    headers: {
      'Content-Type': 'application/json'
    }
  });
}

export async function deleteUserCredentialApi(serverId: string): Promise<void> {
  await apiRequest(`/api/user/credentials/${serverId}`, {
    method: 'DELETE'
  });
}
