import React from 'react';
import { AuthProviderConfig, SecretProviderConfig } from '../../shared/types';
import { IdentityAuthTab } from './IdentityAuthTab';
import { SecretProvidersTab } from './SecretProvidersTab';

export interface ProvidersTabProps {
  authProviders: AuthProviderConfig[];
  secretProviders: SecretProviderConfig[];
  saveAuthProvider: (provider: AuthProviderConfig) => Promise<void>;
  saveSecretProvider: (provider: SecretProviderConfig) => Promise<void>;
}

export const ProvidersTab: React.FC<ProvidersTabProps> = ({
  authProviders,
  secretProviders,
  saveAuthProvider,
  saveSecretProvider,
}) => {
  return (
    <div className="providers-tab-container" style={{ display: 'flex', flexDirection: 'column', gap: '25px' }}>
      <IdentityAuthTab
        providers={authProviders}
        saveAuthProvider={saveAuthProvider}
      />
      <SecretProvidersTab
        providers={secretProviders}
        saveSecretProvider={saveSecretProvider}
      />
    </div>
  );
};
