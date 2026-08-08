import { create } from 'zustand';
import { apiRequest } from '../utils/api';

interface UserInfo {
  authenticated: boolean;
  username?: string;
  name?: string;
  email?: string;
  groups?: string[];
}

interface UserStore {
  user: UserInfo | null;
  version: string;
  isLoadingUser: boolean;
  loadUser: () => Promise<void>;
  loadVersion: () => Promise<void>;
}

export const useUserStore = create<UserStore>((set) => ({
  user: null,
  version: '4.2.17', // fallback default
  isLoadingUser: false,
  loadUser: async () => {
    set({ isLoadingUser: true });
    try {
      const data = await apiRequest<UserInfo>('/api/me');
      set({ user: data, isLoadingUser: false });
    } catch (e) {
      console.error('Error loading user profile:', e);
      set({ user: { authenticated: false }, isLoadingUser: false });
    }
  },
  loadVersion: async () => {
    try {
      const data = await apiRequest<{ version: string }>('/health');
      if (data && data.version) {
        set({ version: data.version });
      }
    } catch (e) {
      console.error('Error loading version:', e);
    }
  }
}));
