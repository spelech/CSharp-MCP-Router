import { create } from 'zustand';
import { showToast } from './useToastStore';
import { confirmAction } from './useConfirmStore';
import { AppKeyItem, AppKeyLimits, NewAppKeyResult, CreateAppKeyPayload } from '../shared/types';
import {
  fetchAppKeysApi,
  fetchAppKeyLimitsApi,
  createAppKeyApi,
  revokeAppKeyApi
} from '../api/appKeyApi';

export type { AppKeyItem, AppKeyLimits, NewAppKeyResult, CreateAppKeyPayload };

interface AppKeyStore {
  appKeys: AppKeyItem[];
  limits: AppKeyLimits | null;
  isLoading: boolean;
  isCreateModalOpen: boolean;
  createdResult: NewAppKeyResult | null;

  fetchAppKeys: () => Promise<void>;
  fetchLimits: () => Promise<void>;
  createAppKey: (payload: CreateAppKeyPayload) => Promise<void>;
  revokeAppKey: (id: string, name: string) => Promise<void>;
  openModal: () => void;
  closeModal: () => void;
}

export const useAppKeyStore = create<AppKeyStore>((set, get) => ({
  appKeys: [],
  limits: null,
  isLoading: false,
  isCreateModalOpen: false,
  createdResult: null,

  fetchAppKeys: async () => {
    set({ isLoading: true });
    try {
      const data = await fetchAppKeysApi();
      set({ appKeys: data || [], isLoading: false });
    } catch (e: any) {
      console.error('Error fetching app keys:', e);
      set({ isLoading: false });
    }
  },

  fetchLimits: async () => {
    try {
      const data = await fetchAppKeyLimitsApi();
      set({ limits: data });
    } catch (e: any) {
      console.error('Error fetching app key limits:', e);
    }
  },

  createAppKey: async (payload) => {
    try {
      const result = await createAppKeyApi(payload);
      set({ createdResult: result });
      showToast('App Key created successfully', 'success');
      get().fetchAppKeys();
      get().fetchLimits();
    } catch (e: any) {
      showToast(`Error creating App Key: ${e.message}`, 'error');
      throw e;
    }
  },

  revokeAppKey: async (id, name) => {
    const confirmed = await confirmAction({
      title: 'Revoke App Key',
      message: `Are you sure you want to revoke the App Key '${name}'? This cannot be undone.`,
      confirmText: 'Revoke Key',
      danger: true
    });
    if (!confirmed) return;
    try {
      await revokeAppKeyApi(id);
      showToast('App Key revoked successfully', 'success');
      get().fetchAppKeys();
      get().fetchLimits();
    } catch (e: any) {
      showToast(`Error revoking App Key: ${e.message}`, 'error');
    }
  },

  openModal: () => set({ isCreateModalOpen: true, createdResult: null }),
  closeModal: () => set({ isCreateModalOpen: false, createdResult: null })
}));
