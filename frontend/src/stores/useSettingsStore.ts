import { create } from 'zustand';
import { showToast } from './useToastStore';
import {
  EmbeddingSettings,
  AuthProviderConfig,
  SecretProviderConfig,
  CustomFileMeta,
  AccessPolicy,
  GroupMapping,
  PendingApproval,
} from '../shared/types';
import {
  fetchEmbeddingSettingsApi,
  saveEmbeddingSettingsApi,
  fetchAuthProvidersApi,
  saveAuthProviderApi,
  fetchSecretProvidersApi,
  saveSecretProviderApi,
  fetchCustomFilesApi,
  fetchCustomFileContentApi,
  saveCustomFileApi,
  deleteCustomFileApi,
  fetchPendingApprovalsApi,
  actionApprovalApi,
} from '../api/settingsApi';
import {
  fetchPoliciesApi,
  savePolicyApi,
  deletePolicyApi,
  fetchMappingsApi,
  saveMappingApi,
  deleteMappingApi,
} from '../api/securityApi';

export type {
  EmbeddingSettings,
  AuthProviderConfig,
  SecretProviderConfig,
  CustomFileMeta,
  AccessPolicy,
  GroupMapping,
  PendingApproval,
};

interface SettingsStore {
  embeddingSettings: EmbeddingSettings | null;
  authProviders: AuthProviderConfig[];
  secretProviders: SecretProviderConfig[];
  customFiles: CustomFileMeta[];
  policies: AccessPolicy[];
  mappings: GroupMapping[];
  pendingApprovals: PendingApproval[];

  isLoadingSettings: boolean;
  isSavingSettings: boolean;

  // Active custom file modal editor state
  isCustomFileOpen: boolean;
  editingFileMeta: CustomFileMeta | null; // null for Create
  editingFileContent: string;
  activeFileModalTab: 'editor' | 'builder';

  // Policy Modal
  isPolicyModalOpen: boolean;
  editingPolicy: AccessPolicy | null;

  // Mapping Modal
  isMappingModalOpen: boolean;
  editingMapping: GroupMapping | null;

  // Actions
  fetchEmbeddingSettings: () => Promise<void>;
  saveEmbeddingSettings: (settings: EmbeddingSettings) => Promise<boolean>;

  fetchProviders: () => Promise<void>;
  saveAuthProvider: (provider: AuthProviderConfig) => Promise<void>;
  saveSecretProvider: (provider: SecretProviderConfig) => Promise<void>;

  fetchApprovals: () => Promise<void>;
  actionApproval: (id: string, approved: boolean) => Promise<void>;

  fetchCustomFiles: () => Promise<void>;
  fetchCustomFileContent: (type: 'prompts' | 'resources', name: string) => Promise<string>;
  saveCustomFile: (type: 'prompts' | 'resources', name: string, content: string) => Promise<boolean>;
  deleteCustomFile: (type: 'prompts' | 'resources', name: string) => Promise<void>;

  fetchPolicies: () => Promise<void>;
  savePolicy: (policy: AccessPolicy) => Promise<void>;
  deletePolicy: (id: string) => Promise<void>;

  fetchMappings: () => Promise<void>;
  saveMapping: (mapping: GroupMapping) => Promise<void>;
  deleteMapping: (id: string) => Promise<void>;

  // UI state actions
  openCustomFileModal: (meta?: CustomFileMeta) => Promise<void>;
  closeCustomFileModal: () => void;
  setEditingFileContent: (content: string) => void;
  setActiveFileModalTab: (tab: 'editor' | 'builder') => void;

  openPolicyModal: (policy?: AccessPolicy) => void;
  closePolicyModal: () => void;

  openMappingModal: (mapping?: GroupMapping) => void;
  closeMappingModal: () => void;
}

export const useSettingsStore = create<SettingsStore>((set, get) => ({
  embeddingSettings: null,
  authProviders: [],
  secretProviders: [],
  customFiles: [],
  policies: [],
  mappings: [],
  pendingApprovals: [],

  isLoadingSettings: false,
  isSavingSettings: false,

  isCustomFileOpen: false,
  editingFileMeta: null,
  editingFileContent: '',
  activeFileModalTab: 'editor',

  isPolicyModalOpen: false,
  editingPolicy: null,

  isMappingModalOpen: false,
  editingMapping: null,

  fetchEmbeddingSettings: async () => {
    set({ isLoadingSettings: true });
    try {
      const settings = await fetchEmbeddingSettingsApi();
      if (settings) {
        set({ embeddingSettings: settings });
      }
    } catch (e) {
      console.error('Failed to load settings:', e);
    } finally {
      set({ isLoadingSettings: false });
    }
  },

  saveEmbeddingSettings: async (settings) => {
    set({ isSavingSettings: true });
    try {
      const success = await saveEmbeddingSettingsApi(settings);
      if (success) {
        set({ embeddingSettings: settings });
        showToast('Settings saved successfully', 'success');
        return true;
      }
      return false;
    } catch (e: any) {
      console.error('Failed to save settings:', e);
      showToast(`Error saving settings: ${e.message}`, 'error');
      return false;
    } finally {
      set({ isSavingSettings: false });
    }
  },

  fetchProviders: async () => {
    try {
      const auth = await fetchAuthProvidersApi();
      const secrets = await fetchSecretProvidersApi();
      set({
        authProviders: auth || [],
        secretProviders: secrets || []
      });
    } catch (e) {
      console.warn('Providers not yet initialized or endpoints unavailable:', e);
    }
  },

  saveAuthProvider: async (provider) => {
    try {
      await saveAuthProviderApi(provider);
      get().fetchProviders();
    } catch (e: any) {
      showToast(`Failed to save Auth Provider: ${e.message}`, 'error');
      throw e;
    }
  },

  saveSecretProvider: async (provider) => {
    try {
      await saveSecretProviderApi(provider);
      get().fetchProviders();
    } catch (e: any) {
      showToast(`Failed to save Secret Provider: ${e.message}`, 'error');
      throw e;
    }
  },

  fetchApprovals: async () => {
    try {
      const approvals = await fetchPendingApprovalsApi();
      set({ pendingApprovals: approvals || [] });
    } catch (e) {
      console.error('Failed to fetch approvals:', e);
    }
  },

  actionApproval: async (id, approved) => {
    try {
      await actionApprovalApi(id, approved);
      showToast(approved ? 'Request approved' : 'Request denied', 'info');
      get().fetchApprovals();
    } catch (e: any) {
      showToast(`Approval action failed: ${e.message}`, 'error');
    }
  },

  fetchCustomFiles: async () => {
    try {
      const files = await fetchCustomFilesApi();
      set({ customFiles: files || [] });
    } catch (err) {
      console.error('Failed to fetch custom files:', err);
    }
  },

  fetchCustomFileContent: async (type, name) => {
    try {
      return await fetchCustomFileContentApi(type, name);
    } catch (err) {
      console.error('Failed to fetch custom file content:', err);
      return '';
    }
  },

  saveCustomFile: async (type, name, content) => {
    try {
      const success = await saveCustomFileApi(type, name, content);
      if (success) {
        showToast('File saved successfully', 'success');
        get().fetchCustomFiles();
        return true;
      }
      return false;
    } catch (err: any) {
      showToast(`Failed to save file: ${err.message}`, 'error');
      return false;
    }
  },

  deleteCustomFile: async (type, name) => {
    if (!window.confirm(`Are you sure you want to delete the custom file '${name}'? This action cannot be undone.`)) {
      return;
    }
    try {
      const success = await deleteCustomFileApi(type, name);
      if (success) {
        showToast('File deleted successfully', 'success');
        get().fetchCustomFiles();
      }
    } catch (err: any) {
      showToast(`Failed to delete file: ${err.message}`, 'error');
    }
  },

  fetchPolicies: async () => {
    try {
      const policies = await fetchPoliciesApi();
      set({ policies: policies || [] });
    } catch (err) {
      console.error('Failed to load policies:', err);
    }
  },

  savePolicy: async (policy) => {
    try {
      await savePolicyApi(policy);
      showToast('Policy saved successfully', 'success');
      set({ isPolicyModalOpen: false, editingPolicy: null });
      get().fetchPolicies();
    } catch (err: any) {
      showToast(`Failed to save policy: ${err.message}`, 'error');
    }
  },

  deletePolicy: async (id) => {
    if (!window.confirm('Are you sure you want to delete this access policy?')) return;
    try {
      await deletePolicyApi(id);
      showToast('Policy deleted successfully', 'success');
      get().fetchPolicies();
    } catch (err: any) {
      showToast(`Failed to delete policy: ${err.message}`, 'error');
    }
  },

  fetchMappings: async () => {
    try {
      const mappings = await fetchMappingsApi();
      set({ mappings: mappings || [] });
    } catch (err) {
      console.error('Failed to load mappings:', err);
    }
  },

  saveMapping: async (mapping) => {
    try {
      await saveMappingApi(mapping);
      showToast('Mapping saved successfully', 'success');
      set({ isMappingModalOpen: false, editingMapping: null });
      get().fetchMappings();
    } catch (err: any) {
      showToast(`Failed to save mapping: ${err.message}`, 'error');
    }
  },

  deleteMapping: async (id) => {
    if (!window.confirm('Are you sure you want to delete this group mapping?')) return;
    try {
      await deleteMappingApi(id);
      showToast('Mapping deleted successfully', 'success');
      get().fetchMappings();
    } catch (err: any) {
      showToast(`Failed to delete mapping: ${err.message}`, 'error');
    }
  },

  openCustomFileModal: async (meta) => {
    if (meta) {
      const content = await get().fetchCustomFileContent(meta.type, meta.name);
      set({
        isCustomFileOpen: true,
        editingFileMeta: meta,
        editingFileContent: content,
        activeFileModalTab: 'editor'
      });
    } else {
      // Create new starter prompts JSON
      const defaultContent = JSON.stringify({
        description: "My custom prompt description",
        arguments: [
          { name: "topic", description: "Topic to write about", required: true }
        ],
        messages: [
          {
            role: "user",
            content: {
              type: "text",
              text: "Write a short summary about {{topic}}."
            }
          }
        ]
      }, null, 2);
      set({
        isCustomFileOpen: true,
        editingFileMeta: null,
        editingFileContent: defaultContent,
        activeFileModalTab: 'editor'
      });
    }
  },

  closeCustomFileModal: () => set({ isCustomFileOpen: false, editingFileMeta: null, editingFileContent: '' }),
  setEditingFileContent: (content) => set({ editingFileContent: content }),
  setActiveFileModalTab: (tab) => set({ activeFileModalTab: tab }),

  openPolicyModal: (policy) => set({ isPolicyModalOpen: true, editingPolicy: policy || null }),
  closePolicyModal: () => set({ isPolicyModalOpen: false, editingPolicy: null }),

  openMappingModal: (mapping) => set({ isMappingModalOpen: true, editingMapping: mapping || null }),
  closeMappingModal: () => set({ isMappingModalOpen: false, editingMapping: null })
}));
