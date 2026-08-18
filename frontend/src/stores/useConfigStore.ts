import { create } from 'zustand';
import { apiRequest } from '../shared/api/api';

interface BrandingConfig {
  title: string;
  icon: string;
}

interface ConfigStore {
  branding: BrandingConfig | null;
  loadBranding: () => Promise<void>;
}

export const useConfigStore = create<ConfigStore>((set) => ({
  branding: null,
  loadBranding: async () => {
    try {
      const data = await apiRequest<any>('/api/config/branding');
      if (data) {
        set({ branding: data });
      }
    } catch (error) {
      console.error('Failed to load branding config', error);
    }
  }
}));
