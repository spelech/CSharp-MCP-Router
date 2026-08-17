export interface EmbeddingSettings {
  embeddingProvider: string;
  embeddingModelDir: string;
  embeddingApiUrl: string;
  embeddingApiModel: string;
  embeddingApiKey: string;
}

export interface AuthProviderConfig {
  id?: string;
  providerName: string;
  displayName: string;
  isEnabled: boolean;
  userHeader?: string;
  groupsHeader?: string;
  configJson?: string;
  isDecryptionFailed?: boolean;
}

export interface SecretProviderConfig {
  id?: string;
  providerName: string;
  displayName: string;
  isEnabled: boolean;
  configJson: string;
  isDecryptionFailed?: boolean;
}

export interface CustomFileMeta {
  type: 'prompts' | 'resources';
  name: string;
  sizeBytes: number;
  lastModified: string;
}
