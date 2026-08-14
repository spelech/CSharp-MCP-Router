export interface AppKeyItem {
  id: string;
  name: string;
  username: string;
  keyPrefix: string;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
}

export interface AppKeyLimits {
  globalMax: number;
  userMax: number;
  totalActiveKeys: number;
  userActiveKeys: number;
  isLimitReached: boolean;
}

export interface NewAppKeyResult {
  id: string;
  name: string;
  username: string;
  keyPrefix: string;
  plaintextKey: string;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
}

export interface CreateAppKeyPayload {
  name: string;
  username?: string;
  scopes: string[];
  expiresInDays?: number;
}
