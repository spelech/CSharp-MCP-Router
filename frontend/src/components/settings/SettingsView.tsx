import React, { useEffect, useState } from 'react';
import { useSettingsStore } from '../../stores/useSettingsStore';

import { GeneralTab } from './GeneralTab';
import { SecurityTab } from './SecurityTab';
import { IdentityAuthTab } from './IdentityAuthTab';
import { SecretProvidersTab } from './SecretProvidersTab';
import { CustomFilesTab } from './CustomFilesTab';
import { AccessControlTab } from './AccessControlTab';

export const SettingsView: React.FC = () => {
  const [activeSubview, setActiveSubview] = useState<'search' | 'security' | 'identity' | 'secrets' | 'files' | 'permissions'>('search');

  const {
    embeddingSettings,
    authProviders,
    secretProviders,
    customFiles,
    policies,
    mappings,
    fetchEmbeddingSettings,
    saveEmbeddingSettings,
    fetchProviders,
    saveAuthProvider,
    saveSecretProvider,
    fetchCustomFiles,
    openCustomFileModal,
    deleteCustomFile,
    fetchPolicies,
    openPolicyModal,
    deletePolicy,
    fetchMappings,
    openMappingModal,
    deleteMapping,
  } = useSettingsStore();

  // Initial load
  useEffect(() => {
    fetchEmbeddingSettings();
    fetchProviders();
    fetchCustomFiles();
    fetchPolicies();
    fetchMappings();
  }, [fetchEmbeddingSettings, fetchProviders, fetchCustomFiles, fetchPolicies, fetchMappings]);

  const handleToggleApproval = async (checked: boolean) => {
    if (embeddingSettings) {
      await saveEmbeddingSettings({
        ...embeddingSettings,
        requireManualApproval: checked,
      });
    }
  };

  return (
    <div id="view-settings" className="view-panel active">
      <div
        className="tester-tabs settings-sub-nav"
        style={{
          justifyContent: 'flex-start',
          gap: '15px',
          marginBottom: '25px',
          borderBottom: '1px solid var(--border-color)',
          paddingBottom: '10px',
          maxWidth: '800px',
          marginLeft: 'auto',
          marginRight: 'auto',
        }}
      >
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'search' ? 'active' : ''}`}
          onClick={() => setActiveSubview('search')}
        >
          <i className="fa-solid fa-brain"></i> Vector &amp; Search
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'security' ? 'active' : ''}`}
          onClick={() => setActiveSubview('security')}
        >
          <i className="fa-solid fa-shield-halved"></i> Security &amp; Approvals
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'identity' ? 'active' : ''}`}
          onClick={() => setActiveSubview('identity')}
        >
          <i className="fa-solid fa-id-card"></i> Identity &amp; Auth
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'secrets' ? 'active' : ''}`}
          onClick={() => setActiveSubview('secrets')}
        >
          <i className="fa-solid fa-vault"></i> Secret Providers
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'files' ? 'active' : ''}`}
          onClick={() => setActiveSubview('files')}
        >
          <i className="fa-solid fa-folder-open"></i> Prompts &amp; Resources
        </button>
        <button
          type="button"
          className={`tester-tab-btn settings-tab-btn ${activeSubview === 'permissions' ? 'active' : ''}`}
          onClick={() => setActiveSubview('permissions')}
        >
          <i className="fa-solid fa-user-lock"></i> Access Control
        </button>
      </div>

      {/* Subview 1: Vector & Search */}
      {activeSubview === 'search' && (
        <GeneralTab
          key={embeddingSettings ? `${embeddingSettings.embeddingProvider}-${embeddingSettings.embeddingModelDir}-${embeddingSettings.embeddingApiUrl}` : 'loading'}
          settings={embeddingSettings}
          saveEmbeddingSettings={saveEmbeddingSettings}
        />
      )}

      {/* Subview 2: Security & Approvals */}
      {activeSubview === 'security' && (
        <SecurityTab
          requireApproval={embeddingSettings?.requireManualApproval ?? false}
          onToggleApproval={handleToggleApproval}
        />
      )}

      {/* Subview 3: Identity & Auth */}
      {activeSubview === 'identity' && (
        <IdentityAuthTab
          key={authProviders.map((p) => `${p.providerName}-${p.isEnabled}`).join(',')}
          providers={authProviders}
          saveAuthProvider={saveAuthProvider}
        />
      )}

      {/* Subview 4: Secret Providers */}
      {activeSubview === 'secrets' && (
        <SecretProvidersTab
          key={secretProviders.map((p) => `${p.providerName}-${p.isEnabled}`).join(',')}
          providers={secretProviders}
          saveSecretProvider={saveSecretProvider}
        />
      )}

      {/* Subview 5: Prompts & Resources File Manager */}
      {activeSubview === 'files' && (
        <CustomFilesTab
          customFiles={customFiles}
          openCustomFileModal={openCustomFileModal}
          deleteCustomFile={deleteCustomFile}
        />
      )}

      {/* Subview 6: Access Control Policies */}
      {activeSubview === 'permissions' && (
        <AccessControlTab
          policies={policies}
          mappings={mappings}
          openPolicyModal={openPolicyModal}
          deletePolicy={deletePolicy}
          openMappingModal={openMappingModal}
          deleteMapping={deleteMapping}
        />
      )}
    </div>
  );
};
