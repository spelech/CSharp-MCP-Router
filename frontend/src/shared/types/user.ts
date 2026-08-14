export interface UserInfo {
  authenticated: boolean;
  username?: string;
  name?: string;
  email?: string;
  groups?: string[];
}

export interface HealthInfo {
  status: string;
  version: string;
}
