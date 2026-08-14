import { apiRequest } from './api';
import { UserInfo, HealthInfo } from '../types';

export async function getCurrentUser(): Promise<UserInfo> {
  return apiRequest<UserInfo>('/api/me');
}

export async function getHealth(): Promise<HealthInfo> {
  return apiRequest<HealthInfo>('/health');
}
