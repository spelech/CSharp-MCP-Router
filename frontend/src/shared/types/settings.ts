export interface EmbeddingSettings {
  embeddingProvider: string;
  embeddingModelDir: string;
  embeddingApiUrl: string;
  embeddingApiModel: string;
  embeddingApiKey: string;
  requireManualApproval: boolean;
}

export interface AuthProviderConfig {
  id?: string;
  providerName: string;
  displayName: string;
  isEnabled: boolean;
  userHeader?: string;
  groupsHeader?: string;
}

export interface SecretProviderConfig {
  id?: string;
  providerName: string;
  displayName: string;
  isEnabled: boolean;
  configJson: string;
}

export interface CustomFileMeta {
  type: 'prompts' | 'resources';
  name: string;
  sizeBytes: number;
  lastModified: string;
}

export interface PendingApproval {
  id: string;
  toolName: string;
  arguments: string;
  sessionId: string;
}
